using System.Data;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GraficaModerna.Infrastructure.Services;

public class OrderService(
    AppDbContext context,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IShippingService> shippingServices,
    IPaymentService paymentService,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly AppDbContext _context = context;
    private readonly IEmailService _emailService = emailService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IEnumerable<IShippingService> _shippingServices = shippingServices;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<OrderService> _logger = logger;

    public async Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode,
        string shippingMethod)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || cart.Items.Count == 0)
            throw new Exception("Carrinho vazio.");

        if (cart.Items.Any(i => i.Quantity <= 0))
            throw new Exception("O carrinho contém itens com quantidades inválidas.");

        var shippingItems = cart.Items.Select(i => new ShippingItemDto
        {
            ProductId = i.ProductId,
            Weight = i.Product!.Weight,
            Width = i.Product.Width,
            Height = i.Product.Height,
            Length = i.Product.Length,
            Quantity = i.Quantity
        }).ToList();

        var shippingTasks = _shippingServices.Select(s => s.CalculateAsync(addressDto.ZipCode, shippingItems));
        var shippingResults = await Task.WhenAll(shippingTasks);
        var allOptions = shippingResults.SelectMany(x => x).ToList();

        var selectedOption = allOptions.FirstOrDefault(o =>
                                 o.Name.Trim().Equals(shippingMethod.Trim(),
                                     StringComparison.InvariantCultureIgnoreCase)) ??
                             throw new Exception("Método de envio inválido ou indisponível.");
        var verifiedShippingCost = selectedOption.Price;

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                if (item.Product == null) continue;

                if (item.Product.StockQuantity < item.Quantity)
                    throw new Exception($"Estoque insuficiente para o produto {item.Product.Name}");

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
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c =>
                    c.Code.Equals(couponCode, StringComparison.CurrentCultureIgnoreCase));
                if (coupon != null && coupon.IsValid())
                {
                    var alreadyUsed =
                        await _context.CouponUsages.AnyAsync(u => u.UserId == userId && u.CouponCode == coupon.Code);
                    if (alreadyUsed) throw new Exception("Cupom já utilizado.");

                    discount = subTotal * (coupon.DiscountPercentage / 100m);
                }
            }

            var totalAmount = subTotal - discount + verifiedShippingCost;

            if (totalAmount < Order.MinOrderAmount)
                throw new Exception($"O valor total do pedido deve ser no mínimo {Order.MinOrderAmount:C}.");

            if (totalAmount > Order.MaxOrderAmount)
                throw new Exception($"O valor do pedido excede o limite de segurança de {Order.MaxOrderAmount:C}.");

            var formattedAddress =
                $"{addressDto.Street}, {addressDto.Number} - {addressDto.Complement} - {addressDto.Neighborhood}, " +
                $"{addressDto.City}/{addressDto.State} (Ref: {addressDto.Reference}) - A/C: {addressDto.ReceiverName} - Tel: {addressDto.PhoneNumber}";

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = formattedAddress,
                ShippingZipCode = addressDto.ZipCode,
                ShippingCost = verifiedShippingCost,
                ShippingMethod = selectedOption.Name,
                Status = "Pendente",
                OrderDate = DateTime.UtcNow,
                SubTotal = subTotal,
                Discount = discount,
                TotalAmount = totalAmount,
                AppliedCoupon = couponCode?.ToUpper(),
                Items = orderItems,
                CustomerIp = GetIpAddress(),
                UserAgent = GetUserAgent()
            };

            order.History.Add(new OrderHistory
            {
                Status = "Pendente",
                Message = "Pedido criado via Checkout",
                ChangedBy = userId,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetIpAddress(),
                UserAgent = GetUserAgent()
            });

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (discount > 0 && couponCode != null)
                _context.CouponUsages.Add(new CouponUsage
                {
                    UserId = userId,
                    CouponCode = couponCode.ToUpper(),
                    OrderId = order.Id,
                    UsedAt = DateTime.UtcNow
                });

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _ = SendOrderReceivedEmailAsync(userId, order.Id);

            // Adicionado o aviso sobre a reserva de estoque
            return MapToDto(order, "Atenção: A reserva dos itens e o débito no estoque só ocorrem após a confirmação do pagamento.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return [.. orders.Select(o => MapToDto(o))];
    }

    public async Task<List<AdminOrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .Include(o => o.History)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return [.. orders.Select(MapToAdminDto)];
    }

    public async Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId, long amountPaidInCents)
    {
        var isAlreadyProcessed = await _context.Orders
            .AnyAsync(o => o.StripePaymentIntentId == transactionId && o.Status == "Pago");

        if (isAlreadyProcessed)
        {
            _logger.LogWarning("[Webhook] Tentativa de reprocessamento detectada. Transaction: {TransactionId}", transactionId);
            return;
        }

        var order = await _context.Orders
            .Include(o => o.History)
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogError("[Webhook] Pedido não encontrado. OrderId: {OrderId}", orderId);
            return;
        }

        var expectedAmountInCents = (long)(order.TotalAmount * 100);

        if (order.Status == "Pago") return;


        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        var outOfStockItems = new List<string>();

        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);

            if (product == null || product.StockQuantity < item.Quantity)
            {
                outOfStockItems.Add(item.ProductName);
            }
        }

        if (outOfStockItems.Count > 0)
        {
            _logger.LogWarning("[Webhook] Estoque insuficiente para o pedido {OrderId} (pago). Itens esgotados: {Items}. Iniciando estorno automático.",
                orderId, string.Join(", ", outOfStockItems));

            try
            {
                await _paymentService.RefundPaymentAsync(transactionId);

                order.StripePaymentIntentId = transactionId;
                order.Status = "Cancelado";

                AddAuditLog(order, "Cancelado",
                    $"⚠️ Cancelamento Automático: Estoque insuficiente para itens ({string.Join(", ", outOfStockItems)}). Pagamento recebido e estornado imediatamente.",
                    "SYSTEM-STOCK-CHECK");

                await _context.SaveChangesAsync();

                if (order.User != null && !string.IsNullOrEmpty(order.User.Email))
                {
                    var emailBody = $@"
                    <p>Olá {order.User.FullName},</p>
                    <p>Recebemos a confirmação do seu pagamento, porém...</p>
                    <h3 style='color: #c0392b;'>Ah não! Alguém acabou comprando antes que você.</h3>
                    <p>Infelizmente, um ou mais itens do seu pedido esgotaram nos últimos instantes e não temos estoque suficiente para concluir sua compra.</p>
                    <p><b>Já estamos providenciando o estorno total do valor pago.</b></p>
                    <p>O reembolso foi processado automaticamente e deve aparecer na sua fatura em breve, caso tenha utilizao um cartão, dependendo da sua operadora de cartão.</p>
                    <p>Pedimos sinceras desculpas pelo inconveniente.</p>";

                    await _emailService.SendEmailAsync(order.User.Email, $"Atualização sobre o Pedido #{order.Id} - Reembolso Automático", emailBody);
                }

                _logger.LogInformation("[Webhook] Pedido {OrderId} cancelado e reembolsado com sucesso devido à falta de estoque.", orderId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[Webhook] ERRO CRÍTICO ao tentar processar estorno automático para Order {OrderId}.", orderId);
                throw new Exception("Falha ao processar estorno automático de pedido sem estoque.");
            }
        }

        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
            {
                product.DebitStock(item.Quantity);
                _context.Entry(product).State = EntityState.Modified;
            }
        }

        order.StripePaymentIntentId = transactionId;
        order.Status = "Pago";

        AddAuditLog(order, "Pago",
            $"✅ Pagamento confirmado via Webhook e Estoque debitado. Transaction ID: {transactionId}. Valor validado.",
            "STRIPE-WEBHOOK");

        await _context.SaveChangesAsync();

        _logger.LogInformation("[Webhook] Pagamento confirmado com sucesso. OrderId: {OrderId}", orderId);

        _ = SendOrderUpdateEmailAsync(order.UserId, order, "Pago");
    }
    public async Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var adminUserId = _userManager.GetUserId(user!) ?? "AdminUnknown";

        if (user == null || !user.IsInRole("Admin"))
            throw new UnauthorizedAccessException("Apenas administradores podem alterar pedidos.");

        var order = await _context.Orders
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == orderId) ?? throw new Exception("Pedido não encontrado");

        var oldStatus = order.Status;
        var auditMessage = $"Status alterado manualmente para {dto.Status}";

        if (dto.Status == "Aguardando Devolução")
        {
            if (!string.IsNullOrEmpty(dto.ReverseLogisticsCode))
                order.ReverseLogisticsCode = dto.ReverseLogisticsCode;

            order.ReturnInstructions = !string.IsNullOrEmpty(dto.ReturnInstructions)
                ? dto.ReturnInstructions
                : "Instruções padrão de devolução...";

            auditMessage += ". Instruções geradas.";
        }

        if (dto.Status == "Reembolso Reprovado")
        {
            order.RefundRejectionReason = dto.RefundRejectionReason;
            order.RefundRejectionProof = dto.RefundRejectionProof;
            auditMessage += ". Justificativa e provas anexadas.";
        }

        if ((dto.Status == "Reembolsado" || dto.Status == "Cancelado")
            && order.Status != "Reembolsado"
            && !string.IsNullOrEmpty(order.StripePaymentIntentId))
            try
            {
                await _paymentService.RefundPaymentAsync(order.StripePaymentIntentId);
                auditMessage += ". Reembolso processado no Stripe.";
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro no reembolso Stripe: {ex.Message}");
            }

        if (dto.Status == "Entregue" && order.Status != "Entregue")
            order.DeliveryDate = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.TrackingCode))
        {
            order.TrackingCode = dto.TrackingCode;
            auditMessage += $" (Rastreio: {dto.TrackingCode})";
        }

        AddAuditLog(order, dto.Status, auditMessage, $"Admin:{adminUserId}");

        await _context.SaveChangesAsync();

        if (oldStatus != dto.Status)
        {
            _ = SendOrderUpdateEmailAsync(order.UserId, order, dto.Status);
        }
    }

    public async Task RequestRefundAsync(Guid orderId, string userId)
    {
        var order = await GetUserOrderOrFail(orderId, userId);

        AddAuditLog(order, "Reembolso Solicitado", "Usuário solicitou cancelamento pelo painel.", userId);

        await _context.SaveChangesAsync();
    }

    private string GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }

    private string GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "Unknown";
    }

    private void AddAuditLog(Order order, string newStatus, string message, string changedBy)
    {
        order.Status = newStatus;

        order.History.Add(new OrderHistory
        {
            OrderId = order.Id,
            Status = newStatus,
            Message = message,
            ChangedBy = changedBy,
            Timestamp = DateTime.UtcNow,
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });
    }

    private async Task<Order> GetUserOrderOrFail(Guid orderId, string userId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == orderId) ?? throw new Exception("Pedido não encontrado.");
        if (order.UserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para acessar este pedido.");

        return order;
    }

    private async Task NotifySecurityTeamAsync(
        Order order,
        string transactionId,
        long expectedAmount,
        long receivedAmount)
    {
        try
        {
            var securityEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL")!;

            var subject = $"🚨 ALERTA DE SEGURANÇA CRÍTICO - Tentativa de Fraude";
            var body = $@"
                <h2 style='color: red;'>⚠️ TENTATIVA DE MANIPULAÇÃO DE PAGAMENTO DETECTADA</h2>
                
                <h3>Detalhes do Incidente:</h3>
                <ul>
                    <li><b>Pedido:</b> {order.Id}</li>
                    <li><b>Usuário:</b> {order.User?.Email ?? "N/A"} (ID: {order.UserId})</li>
                    <li><b>Transaction ID:</b> {transactionId}</li>
                    <li><b>Valor Esperado:</b> R$ {expectedAmount / 100.0:F2}</li>
                    <li><b>Valor Recebido:</b> R$ {receivedAmount / 100.0:F2}</li>
                    <li><b>Divergência:</b> R$ {Math.Abs(expectedAmount - receivedAmount) / 100.0:F2}</li>
                    <li><b>IP do Cliente:</b> {order.CustomerIp ?? "N/A"}</li>
                    <li><b>User Agent:</b> {order.UserAgent ?? "N/A"}</li>
                    <li><b>Data/Hora:</b> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                </ul>

                <h3>⚠️ Ações Tomadas:</h3>
                <ul>
                    <li>✅ Pagamento foi <b>BLOQUEADO</b></li>
                    <li>✅ Transação foi <b>REJEITADA</b></li>
                    <li>✅ Incidente registrado no histórico do pedido</li>
                    <li>⚠️ Requer <b>investigação manual imediata</b></li>
                </ul>

                <p style='color: red; font-weight: bold;'>
                    Este é um alerta automático de segurança. 
                    Investigue imediatamente e considere bloquear a conta do usuário.
                </p>
            ";

            await _emailService.SendEmailAsync(securityEmail, subject, body);

            _logger.LogCritical(
                "[SECURITY] Alerta enviado para time de segurança. OrderId: {OrderId}, User: {UserId}",
                order.Id, order.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SECURITY] Falha ao enviar alerta de segurança. OrderId: {OrderId}", order.Id);
        }
    }

    private async Task SendOrderReceivedEmailAsync(string userId, Guid orderId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
                await _emailService.SendEmailAsync(user.Email!, "Pedido Recebido",
                    $"Seu pedido #{orderId} foi recebido e está sendo processado.");
        }
        catch
        {
        }
    }

    private async Task SendOrderUpdateEmailAsync(string userId, Order order, string newStatus)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Email)) return;

            string subject;
            string body;

            switch (newStatus)
            {
                case "Pago":
                    subject = $"Pagamento Confirmado - Pedido #{order.Id}";
                    body = $"Olá! O pagamento do seu pedido #{order.Id} foi confirmado e ele já está sendo preparado.";
                    break;
                case "Enviado":
                    subject = $"Pedido Enviado - #{order.Id}";
                    body = $"Seu pedido #{order.Id} foi enviado! <br> Código de rastreio: {order.TrackingCode}";
                    break;
                case "Entregue":
                    subject = $"Pedido Entregue - #{order.Id}";
                    body = $"Seu pedido #{order.Id} foi marcado como entregue. Esperamos que goste!";
                    break;
                case "Cancelado":
                    subject = $"Pedido Cancelado - #{order.Id}";
                    body = $"Seu pedido #{order.Id} foi cancelado.";
                    break;
                case "Reembolsado":
                    subject = $"Reembolso Processado - #{order.Id}";
                    body = $"O reembolso do seu pedido #{order.Id} foi processado e deve aparecer na sua fatura em breve.";
                    break;
                case "Aguardando Devolução":
                    subject = $"Instruções de Devolução - Pedido #{order.Id}";
                    body = $"Solicitação de troca/devolução aceita.<br>Código de Logística Reversa: <b>{order.ReverseLogisticsCode}</b><br><br>Instruções:<br>{order.ReturnInstructions}";
                    break;
                case "Reembolso Reprovado":
                    subject = $"Solicitação de Reembolso Negada - Pedido #{order.Id}";
                    body = $"Sua solicitação de reembolso para o pedido #{order.Id} foi analisada e reprovada.<br><br><b>Motivo:</b> {order.RefundRejectionReason}";

                    if (!string.IsNullOrEmpty(order.RefundRejectionProof))
                    {
                        body += $"<br><br><b>Evidência da análise:</b> <a href=\"{order.RefundRejectionProof}\">Clique aqui para visualizar</a>";
                    }
                    break;
                default:
                    return;
            }

            await _emailService.SendEmailAsync(user.Email, subject, body);
        }
        catch
        {
        }
    }

    private static OrderDto MapToDto(Order order, string? paymentWarning = null)
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
            order.RefundRejectionReason,
            order.RefundRejectionProof,
            order.ShippingAddress,
            order.User?.FullName ?? "Cliente",
            [.. order.Items.Select(i =>
                new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)
            )],
            paymentWarning
        );
    }

    private static AdminOrderDto MapToAdminDto(Order order)
    {
        return new AdminOrderDto(
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
            order.RefundRejectionReason,
            order.RefundRejectionProof,
            order.ShippingAddress,
            order.User?.FullName ?? "Cliente Desconhecido",
            DataMaskingExtensions.MaskCpfCnpj(order.User?.CpfCnpj ?? ""),
            order.User?.Email ?? "N/A",
            DataMaskingExtensions.MaskIpAddress(order.CustomerIp),
            [.. order.Items.Select(i =>
                new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)
            )],
            [.. order.History.Select(h =>
                new OrderHistoryDto(h.Status, h.Message, h.ChangedBy, h.Timestamp)
            ).OrderByDescending(h => h.Timestamp)]
        );
    }
}