using System.ComponentModel;

namespace GraficaModerna.Domain.Enums;

public enum OrderStatus
{
    [Description("Pendente")]
    Pendente,

    [Description("Pago")]
    Pago,

    [Description("Enviado")]
    Enviado,

    [Description("Entregue")]
    Entregue,

    [Description("Cancelado")]
    Cancelado,

    [Description("Reembolso Solicitado")]
    ReembolsoSolicitado,

    [Description("Aguardando Devolução")]
    AguardandoDevolucao,

    [Description("Reembolsado")]
    Reembolsado,

    [Description("Reembolsado Parcialmente")]
    ReembolsadoParcialmente,

    [Description("Reembolso Reprovado")]
    ReembolsoReprovado
}