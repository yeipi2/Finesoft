using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fs_backend.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendQuoteEmailAsync(string toEmail, string clientName, string quoteNumber, byte[] pdfBytes, string publicToken)
    {
        try
        {
            var from = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"];
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];

            var baseUrl = _configuration["App:FrontendUrl"] ?? "https://localhost:7204";
            var responseUrl = $"{baseUrl}/responder-cotizacion/{publicToken}";

            using var message = new MailMessage();
            message.From = new MailAddress(from!, fromName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = $"Cotización {quoteNumber} — Finesoft";
            message.IsBodyHtml = true;

            var logoPath = GetLogoPath();
            LinkedResource? logoResource = null;

            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                logoResource = new LinkedResource(logoPath, "image/png")
                {
                    ContentId = "logo-finesoft"
                };
            }

            var htmlBody = GetEmailHtml(clientName, quoteNumber, responseUrl, logoResource != null);
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

            if (logoResource != null)
                htmlView.LinkedResources.Add(logoResource);

            message.AlternateViews.Add(htmlView);

            using var pdfStream = new MemoryStream(pdfBytes);
            var pdfAttachment = new Attachment(pdfStream, $"Cotizacion-{quoteNumber}.pdf", "application/pdf");
            message.Attachments.Add(pdfAttachment);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort);
            smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(message);

            _logger.LogInformation("✅ Email enviado a {Email} — Cotización {QuoteNumber}", toEmail, quoteNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email — Cotización {QuoteNumber} a {Email}", quoteNumber, toEmail);
            return false;
        }
    }

    private string? GetLogoPath()
    {
        try
        {
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "LogoFinesoftt.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "LogoFinesoftt.png"),
                @"D:\Imprimir\fineprojectweb\fs-front\wwwroot\images\LogoFinesoftt.png",
                Path.Combine(Directory.GetCurrentDirectory(), "..", "fs-front", "wwwroot", "images", "LogoFinesoftt.png")
            };

            foreach (var path in possiblePaths)
            {
                _logger.LogInformation("🔍 Buscando logo en: {Path}", path);
                if (File.Exists(path))
                {
                    _logger.LogInformation("✅ Logo encontrado en: {Path}", path);
                    return path;
                }
            }

            _logger.LogWarning("⚠️ Logo no encontrado en ninguna ubicación");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al buscar el logo");
            return null;
        }
    }

    private string GetEmailHtml(string clientName, string quoteNumber, string responseUrl, bool hasLogo)
    {
        var logoHtml = hasLogo
            ? @"<img src=""cid:logo-finesoft"" alt=""Finesoft"" style=""max-width:140px;height:auto;display:block;"">"
            : @"<span style=""font-family:'DM Mono',monospace;font-size:15px;font-weight:500;color:#7c3aed;letter-spacing:0.5px;"">FINESOFT</span>";

        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <link href=""https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600&family=DM+Mono:wght@500&display=swap"" rel=""stylesheet"">
</head>
<body style=""margin:0;padding:0;background:#f4f4f5;font-family:'DM Sans',Helvetica,Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f4f5;padding:36px 16px;"">
    <tr>
      <td align=""center"">
        <table width=""500"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e4e4e7;box-shadow:0 1px 4px rgba(0,0,0,0.06);"">

          <!-- Header con Logo -->
          <tr>
            <td style=""padding:22px 32px;border-bottom:1px solid #f0f0f0;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td>{logoHtml}</td>
                  <td align=""right"">
                    <span style=""background:#f4f4f5;color:#71717a;font-size:11px;font-weight:600;padding:4px 10px;border-radius:20px;letter-spacing:0.5px;font-family:'DM Mono',monospace;"">COTIZACIÓN</span>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:28px 32px;"">

              <p style=""margin:0 0 4px;font-size:13px;color:#71717a;"">Hola,</p>
              <p style=""margin:0 0 24px;font-size:21px;font-weight:600;color:#18181b;"">{clientName} 👋</p>

              <!-- Quote number box -->
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#fafafa;border:1px solid #e4e4e7;border-radius:10px;margin-bottom:22px;"">
                <tr>
                  <td style=""padding:14px 20px;"">
                    <span style=""font-size:11px;font-weight:600;color:#a1a1aa;letter-spacing:1px;text-transform:uppercase;display:block;margin-bottom:4px;"">Número de cotización</span>
                    <span style=""font-family:'DM Mono',monospace;font-size:19px;font-weight:500;color:#18181b;"">{quoteNumber}</span>
                  </td>
                </tr>
              </table>

              <p style=""margin:0 0 22px;font-size:14px;color:#71717a;line-height:1.65;"">
                Adjunto encontrarás el PDF con todos los detalles de tu cotización. Haz clic abajo para revisarla y responder.
              </p>

              <!-- Attachment note -->
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#fafafa;border:1px solid #e4e4e7;border-radius:8px;margin-bottom:24px;"">
                <tr>
                  <td style=""padding:12px 18px;"">
                    <span style=""font-size:13px;color:#71717a;"">📎 <strong style=""color:#3f3f46;"">Cotizacion-{quoteNumber}.pdf</strong> adjunto a este correo</span>
                  </td>
                </tr>
              </table>

              <!-- CTA Button -->
              <table cellpadding=""0"" cellspacing=""0"" width=""100%"">
                <tr>
                  <td align=""center"">
                    <a href=""{responseUrl}"" style=""display:inline-block;padding:13px 40px;background:#7c3aed;color:#fff;font-size:14px;font-weight:600;text-decoration:none;border-radius:8px;letter-spacing:0.2px;"">
                      Ver y responder cotización →
                    </a>
                  </td>
                </tr>
              </table>

              <p style=""margin:18px 0 0;font-size:12px;color:#a1a1aa;text-align:center;"">
                ¿Dudas? Responde este correo directamente
              </p>

            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""padding:16px 32px;border-top:1px solid #f0f0f0;background:#fafafa;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td><span style=""font-size:12px;color:#a1a1aa;"">© 2026 Finesoft · Los Mochis, Sin.</span></td>
                  <td align=""right"">
                    <a href=""mailto:informes@finesoft.com.mx"" style=""font-size:12px;color:#7c3aed;text-decoration:none;"">informes@finesoft.com.mx</a>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}