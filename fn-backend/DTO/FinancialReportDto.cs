namespace fs_backend.DTO;

public class FinancialReportDto
{
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalOverdue { get; set; }
    public int InvoicesCount { get; set; }
    public int PaidInvoicesCount { get; set; }
    public int PendingInvoicesCount { get; set; }
    public decimal AverageInvoiceAmount { get; set; }
    public decimal AveragePaymentTime { get; set; }
}