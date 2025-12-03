using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GraficaModerna.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderService(IUnitOfWork uow, IEmailService emailService, UserManager<ApplicationUser> userManager)
    {
        _uow = uow;
        _emailService = emailService;
        _userManager = userManager;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode, decimal shippingCost, string shippingMethod)
    {
        using var transaction = await _uow.BeginTransactionAsync();
        try
        {
            var cart = await _uow.Carts.GetByUserIdAsync(userId);
            if (cart == null || !cart.Items.Any()) throw new Exception("Carrinho vazio.");

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

            decimal totalAmount = (subTotal - discount) + shippingCost;

            var formattedAddress = $"{addressDto.Street}, {addressDto.Number}";
            if (!string.IsNullOrWhiteSpace(addressDto.Complement)) formattedAddress += $" - {addressDto.Complement}";
            formattedAddress += $" - {addressDto.Neighborhood}, {addressDto.City}/{addressDto.State}";
            if (!string.IsNullOrWhiteSpace(addressDto.Reference)) formattedAddress += $" (Ref: {addressDto.Reference})";
            formattedAddress += $" - A/C: {addressDto.ReceiverName} - Tel: {addressDto.PhoneNumber}";

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = formattedAddress,
                ShippingZipCode = addressDto.ZipCode,
                ShippingCost = shippingCost,
                ShippingMethod = shippingMethod,
                Status = "Pendente",
                OrderDate = DateTime.UtcNow,
                SubTotal = subTotal,
                Discount = discount,
                TotalAmount = totalAmount,
                AppliedCoupon = !string.IsNullOrEmpty(couponCode) ? couponCode.ToUpper() : null,
                Items = orderItems
            };

            await _uow.Orders.AddAsync(order);
            await _uow.Carts.ClearCartAsync(cart.Id);

            await _uow.CommitAsync();
            await transaction.CommitAsync();

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null) _ = _emailService.SendEmailAsync(user.Email!, "Pedido Confirmado", $"Seu pedido #{order.Id} foi recebido. Total: {totalAmount:C}");
            }
            catch { }

            return MapToDto(order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task PayOrderAsync(Guid orderId, string userId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) throw new Exception("Pedido não encontrado.");
        if (order.UserId != userId) throw new Exception("Acesso negado.");
        if (order.Status != "Pendente") throw new Exception($"Status inválido: {order.Status}");

        order.Status = "Pago";
        await _uow.Orders.UpdateAsync(order);
        await _uow.CommitAsync();
    }

    public async Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null || order.Status == "Pago") return;

        order.Status = "Pago";
        await _uow.Orders.UpdateAsync(order);
        await _uow.CommitAsync();
    }

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

    // ATUALIZADO: Agora aceita dados extras (Logística Reversa)
    // Note: Precisa atualizar a assinatura na Interface IOrderService também se tiver mudado os parametros, 
    // mas aqui usamos o DTO no controller, então o método recebe os dados soltos ou DTO. 
    // Vou manter a assinatura original e adicionar os parametros opcionais ou usar o DTO vindo do controller.
    // No seu controller você passa (id, status, tracking). Vamos ajustar a implementação para receber tudo.
    // Assinatura no Controller: UpdateOrderStatusAsync(id, dto.Status, dto.TrackingCode)
    // Vamos mudar a assinatura aqui para receber os novos campos.

    public async Task UpdateOrderStatusAsync(Guid orderId, string status, string? trackingCode)
    {
        // Este método é chamado pelo Controller antigo.
        // Para suportar os novos campos, o ideal é criar uma sobrecarga ou alterar a interface.
        // Como você pediu o código "pronto", vou alterar este método para aceitar os opcionais 
        // e você deve alterar a chamada no Controller se necessário, mas vou mostrar o método completo aqui.

        // ATENÇÃO: Para funcionar com o DTO novo do Controller, o Controller precisa passar esses dados.
        // Vou assumir que você vai atualizar o Controller (não solicitado nesta leva, mas necessário).
        // Ou melhor: Vou criar um método mais robusto.

        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order != null)
        {
            if (status == "Entregue" && order.Status != "Entregue")
                order.DeliveryDate = DateTime.UtcNow;

            order.Status = status;
            if (!string.IsNullOrEmpty(trackingCode)) order.TrackingCode = trackingCode;

            await _uow.Orders.UpdateAsync(order);
            await _uow.CommitAsync();
        }
    }

    // SOBRECARGA PARA O ADMIN (Logística Reversa)
    public async Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) throw new Exception("Pedido não encontrado");

        if (dto.Status == "Entregue" && order.Status != "Entregue")
            order.DeliveryDate = DateTime.UtcNow;

        order.Status = dto.Status;

        if (!string.IsNullOrEmpty(dto.TrackingCode))
            order.TrackingCode = dto.TrackingCode;

        if (!string.IsNullOrEmpty(dto.ReverseLogisticsCode))
            order.ReverseLogisticsCode = dto.ReverseLogisticsCode;

        if (!string.IsNullOrEmpty(dto.ReturnInstructions))
            order.ReturnInstructions = dto.ReturnInstructions;

        await _uow.Orders.UpdateAsync(order);
        await _uow.CommitAsync();
    }

    public async Task RequestRefundAsync(Guid orderId, string userId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null || order.UserId != userId) throw new Exception("Pedido não encontrado.");

        var allowedStatuses = new[] { "Pago", "Enviado", "Entregue" };
        if (!allowedStatuses.Contains(order.Status))
            throw new Exception($"Status ({order.Status}) não permite solicitação.");

        if (order.Status == "Entregue")
        {
            if (!order.DeliveryDate.HasValue)
                throw new Exception("Data de entrega não registrada.");

            var deadline = order.DeliveryDate.Value.AddDays(7);
            if (DateTime.UtcNow > deadline)
                throw new Exception("Prazo de 7 dias expirado.");
        }

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
            order.TotalAmount,
            order.Status,
            order.TrackingCode,
            // Mapeia novos campos
            order.ReverseLogisticsCode,
            order.ReturnInstructions,
            order.ShippingAddress,
            order.Items.Select(i => new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)).ToList()
        );
    }
}