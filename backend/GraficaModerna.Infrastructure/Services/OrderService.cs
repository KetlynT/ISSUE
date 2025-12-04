using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEnumerable<IShippingService> _shippingServices;

    // NOVO: Injeção do PaymentService para realizar reembolsos
    private readonly IPaymentService _paymentService;

    public OrderService(
        IUnitOfWork uow,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        IEnumerable<IShippingService> shippingServices,
        IPaymentService paymentService) // Injetado aqui
    {
        _uow = uow;
        _emailService = emailService;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _shippingServices = shippingServices;
        _paymentService = paymentService;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode, decimal frontendShippingCost, string shippingMethod)
    {
        var cart = await _uow.Carts.GetByUserIdAsync(userId);
        if (cart == null || !cart.Items.Any()) throw new Exception("Carrinho vazio.");

        var shippingItems = cart.Items.Select(i => new ShippingItemDto
        {
            ProductId = i.ProductId,
            Weight = i.Product!.Weight,
            Width = i.Product.Width,
            Height = i.Product.Height,
            Length = i.Product.Length,
            Quantity = i.Quantity
        }).ToList();

        decimal verifiedShippingCost = 0;

        var shippingTasks = _shippingServices.Select(s => s.CalculateAsync(addressDto.ZipCode, shippingItems));
        var shippingResults = await Task.WhenAll(shippingTasks);
        var allOptions = shippingResults.SelectMany(x => x).ToList();

        var selectedOption = allOptions.FirstOrDefault(o => o.Name == shippingMethod);

        if (selectedOption != null) verifiedShippingCost = selectedOption.Price;
        else throw new Exception("O método de envio selecionado não está mais disponível ou é inválido.");

        using var transaction = await _uow.BeginTransactionAsync();
        try
        {
            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                if (item.Product == null) continue;
                item.Product.DebitStock(item.Quantity);
                subTotal += item.Quantity * item.Product.Price;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
            }

            decimal discount = 0;
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _uow.Coupons.GetByCodeAsync(couponCode);
                if (coupon != null && coupon.IsValid())
                    discount = subTotal * (coupon.DiscountPercentage / 100m);
            }

            decimal totalAmount = (subTotal - discount) + verifiedShippingCost;

            var formattedAddress = $"{addressDto.Street}, {addressDto.Number} - {addressDto.Complement} - {addressDto.Neighborhood}, {addressDto.City}/{addressDto.State} (Ref: {addressDto.Reference}) - A/C: {addressDto.ReceiverName} - Tel: {addressDto.PhoneNumber}";
            var clientIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = formattedAddress,
                ShippingZipCode = addressDto.ZipCode,
                ShippingCost = verifiedShippingCost,
                ShippingMethod = shippingMethod,
                Status = "Pendente",
                OrderDate = DateTime.UtcNow,
                SubTotal = subTotal,
                Discount = discount,
                TotalAmount = totalAmount,
                AppliedCoupon = !string.IsNullOrEmpty(couponCode) ? couponCode.ToUpper() : null,
                Items = orderItems,
                CustomerIp = clientIp
            };

            await _uow.Orders.AddAsync(order);
            await _uow.Carts.ClearCartAsync(cart.Id);

            await _uow.CommitAsync();
            await transaction.CommitAsync();

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null) _ = _emailService.SendEmailAsync(user.Email!, "Pedido Confirmado", $"Seu pedido #{order.Id} foi recebido.");
            }
            catch { }

            return MapToDto(order);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task PayOrderAsync(Guid orderId, string userId)
    {
        // ... (Mantém inalterado, usado apenas para pagamentos manuais se houver)
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null || order.UserId != userId) throw new Exception("Erro.");
        order.Status = "Pago";
        await _uow.Orders.UpdateAsync(order);
        await _uow.CommitAsync();
    }

    public async Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null || order.Status == "Pago") return;

        order.Status = "Pago";
        order.StripePaymentIntentId = transactionId; // Salva o ID da transação para reembolso futuro

        await _uow.Orders.UpdateAsync(order);
        await _uow.CommitAsync();
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, string status, string? trackingCode)
    {
        // Método simplificado (pode ser usado pelo AdminController legado)
        // Redireciona para o método mais completo para garantir a lógica de reembolso
        var dto = new UpdateOrderStatusDto(status, trackingCode, null, null);
        await UpdateAdminOrderAsync(orderId, dto);
    }

    // --- LÓGICA CENTRAL DE ATUALIZAÇÃO DO ADMIN ---
    public async Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) throw new Exception("Pedido não encontrado");

        // 1. Lógica de Logística Reversa ("Aguardando Devolução")
        if (dto.Status == "Aguardando Devolução")
        {
            if (!string.IsNullOrEmpty(dto.ReverseLogisticsCode))
                order.ReverseLogisticsCode = dto.ReverseLogisticsCode;

            // Se o admin não mandou instrução personalizada, coloca a genérica
            order.ReturnInstructions = !string.IsNullOrEmpty(dto.ReturnInstructions)
                ? dto.ReturnInstructions
                : "Embale o produto na caixa original. Cole o código de postagem na caixa e leve a uma agência dos Correios em até 7 dias.";
        }

        // 2. Lógica de Reembolso Automático ("Reembolsado" ou "Cancelado")
        if (dto.Status == "Reembolsado" || dto.Status == "Cancelado")
        {
            // Verifica se tem ID de pagamento do Stripe (ou seja, foi pago via Stripe)
            if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                try
                {
                    // Chama o Stripe para devolver o dinheiro
                    await _paymentService.RefundPaymentAsync(order.StripePaymentIntentId);

                    // Adiciona nota nas instruções para registro
                    order.ReturnInstructions += " [Sistema: Reembolso processado automaticamente no Stripe]";
                }
                catch (Exception ex)
                {
                    // Se falhar o reembolso no Stripe, não aborta a mudança de status, 
                    // mas avisa ou loga. Aqui, vamos adicionar ao histórico do pedido.
                    order.ReturnInstructions += $" [ERRO: Falha ao processar reembolso automático no Stripe: {ex.Message}. Realize manualmente no dashboard do Stripe.]";
                }
            }
        }

        // Atualiza campos padrão
        if (dto.Status == "Entregue" && order.Status != "Entregue")
            order.DeliveryDate = DateTime.UtcNow;

        order.Status = dto.Status;

        if (!string.IsNullOrEmpty(dto.TrackingCode))
            order.TrackingCode = dto.TrackingCode;

        await _uow.Orders.UpdateAsync(order);
        await _uow.CommitAsync();
    }

    // ... (Resto dos métodos: RequestRefundAsync, GetUserOrdersAsync, etc. permanecem iguais)
    public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _uow.Orders.GetByUserIdAsync(userId);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _uow.Orders.GetAllAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task RequestRefundAsync(Guid orderId, string userId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null || order.UserId != userId) throw new Exception("Pedido não encontrado.");

        // Regras de validação de prazo...
        order.Status = "Reembolso Solicitado";
        await _uow.Orders.UpdateAsync(order);
        await _uow.CommitAsync();
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderDate,
            order.DeliveryDate,
            order.SubTotal,
            order.Discount,
            order.ShippingCost,
            order.TotalAmount,
            order.Status,
            order.TrackingCode,
            order.ReverseLogisticsCode,
            order.ReturnInstructions,
            order.ShippingAddress,
            order.Items.Select(i => new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)).ToList()
        );
    }
}