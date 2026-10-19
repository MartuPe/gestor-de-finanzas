namespace Finanzas.Domain.Accounts;

public enum CreditLineType
{
    /// <summary>Tarjeta de crédito: tiene fecha de cierre y de vencimiento.</summary>
    Card,

    /// <summary>Crédito en cuotas sin tarjeta, como Mercado Crédito: solo tiene vencimiento.</summary>
    InstallmentCredit,
}
