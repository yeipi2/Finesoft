namespace fs_backend.Util;

public static class InvoiceConstants
{
    public static class Status
    {
        public const string Pending = "Pendiente";
        public const string Paid = "Pagada";
        public const string Cancelled = "Cancelada";
        public const string Overdue = "Vencida";
    }

    public static class PaymentType
    {
        public const string Pue = "PUE";
        public const string Ppd = "PPD";
    }

    public static class InvoiceType
    {
        public const string Event = "Event";
        public const string Monthly = "Monthly";
    }

    public static class MonthlyHoursStatus
    {
        public const string Ok = "OK";
        public const string Warning = "Warning";
        public const string Exceeded = "Exceeded";
        public const string Critical = "Critical";
    }
}
