namespace PizzeriaPadua.Core.Data
{
    public class StoreSettings
    {
        public int Id { get; set; }
        public string OpeningTime { get; set; } = "11:00";
        public string ClosingTime { get; set; } = "23:00";
        public string Address { get; set; } = "";
        public string CompanyName { get; set; } = "Pizzería Padua S.A. de C.V.";
        public string Rfc { get; set; } = "PPA260101🍕MX";
        public string Currency { get; set; } = "MXN";
        public decimal DefaultTaxRate { get; set; } = 0.16m;
        public string RoundingRule { get; set; } = "Ninguno";
        public bool TipsEnabled { get; set; } = true;
        public bool AllowCloseWithOpenAccounts { get; set; } = false;
        public decimal MaxCashierDiscountPercent { get; set; } = 10m;
        public string ReturnPolicyText { get; set; } = "Conserve su ticket. Reclamaciones o devoluciones el mismo día de consumo.";
    }
}
