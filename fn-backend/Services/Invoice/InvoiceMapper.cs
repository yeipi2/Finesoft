using fs_backend.DTO;
using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class InvoiceMapper : IInvoiceMapper
{
    private readonly UserManager<IdentityUser> _userManager;

    public InvoiceMapper(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<InvoiceDetailDto> MapToDetailDtoAsync(Invoice invoice)
    {
        var creatorUser = await _userManager.FindByIdAsync(invoice.CreatedByUserId);
        var totalPaid = invoice.Payments?.Sum(p => p.Amount) ?? 0m;

        var balance = invoice.Status switch
        {
            InvoiceConstants.Status.Paid => 0m,
            InvoiceConstants.Status.Cancelled => 0m,
            _ => invoice.Total - totalPaid
        };

        var dto = new InvoiceDetailDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            ClientId = invoice.ClientId,
            ClientName = invoice.Client?.CompanyName ?? string.Empty,
            ClientEmail = invoice.Client?.Email ?? string.Empty,
            ClientRFC = invoice.Client?.RFC ?? string.Empty,
            QuoteId = invoice.QuoteId,
            QuoteNumber = invoice.Quote?.QuoteNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            InvoiceType = invoice.InvoiceType,
            Status = invoice.Status,
            PaymentMethod = invoice.PaymentMethod,
            PaymentType = invoice.PaymentType,
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedByUserName = creatorUser?.UserName ?? string.Empty,
            Subtotal = invoice.Subtotal,
            Tax = invoice.Tax,
            Total = invoice.Total,
            TicketCount = invoice.Items?.Count(i => i.TicketId.HasValue && i.TicketId > 0) ?? 0,
            PaidAmount = invoice.Status == InvoiceConstants.Status.Paid ? invoice.Total : totalPaid,
            Balance = balance,
            PaidDate = invoice.PaidDate,
            Notes = invoice.Notes,
            CancellationReason = invoice.CancellationReason,
            CancelledDate = invoice.CancelledDate
        };

        if (invoice.Items != null && invoice.Items.Any())
        {
            dto.Items = invoice.Items.Select(i => new InvoiceItemDetailDto
            {
                Id = i.Id,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                ServiceId = null,
                ServiceName = null,
                TicketId = i.TicketId,
                TicketTitle = i.Ticket?.Title,
                TicketDescription = i.Ticket?.Description,
                TicketClientName = i.Ticket?.Project?.Client?.CompanyName,
                TicketProjectName = i.Ticket?.Project?.Name,
                TicketActualHours = i.Ticket?.ActualHours,
                TicketHourlyRate = i.Ticket?.Project?.HourlyRate
            }).ToList();
        }

        if (invoice.Payments != null && invoice.Payments.Any())
        {
            var userIds = invoice.Payments
                .Select(p => p.RecordedByUserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var usersById = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            foreach (var payment in invoice.Payments)
            {
                usersById.TryGetValue(payment.RecordedByUserId, out var paymentUserName);

                dto.Payments.Add(new InvoicePaymentDto
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    Reference = payment.Reference,
                    Notes = payment.Notes,
                    RecordedByUserId = payment.RecordedByUserId,
                    RecordedByUserName = paymentUserName,
                    ReceiptPath = payment.ReceiptPath,
                    ReceiptFileName = payment.ReceiptFileName,
                    ReceiptContentType = payment.ReceiptContentType,
                    ReceiptSize = payment.ReceiptSize,
                    ReceiptUploadedAt = payment.ReceiptUploadedAt
                });
            }
        }

        return dto;
    }
}
