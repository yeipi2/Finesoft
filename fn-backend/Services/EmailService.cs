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

    public async Task<bool> SendQuoteEmailAsync(string toEmail, string clientName, string quoteNumber, byte[] pdfBytes)
    {
        try
        {
            var from = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"];
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];

            using var message = new MailMessage();
            message.From = new MailAddress(from!, fromName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = $"Cotización {quoteNumber} - Finesoft";
            message.IsBodyHtml = true;

            // Adjuntar el logo como recurso embebido con CID
            var logoPath = GetLogoPath();
            LinkedResource? logoResource = null;

            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                logoResource = new LinkedResource(logoPath, "image/png")
                {
                    ContentId = "logo-finesoft"
                };
            }

            // Crear el HTML con referencia CID
            var htmlBody = GetEmailHtml(clientName, quoteNumber, logoResource != null);

            // Crear AlternateView para el HTML
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

            if (logoResource != null)
            {
                htmlView.LinkedResources.Add(logoResource);
            }

            message.AlternateViews.Add(htmlView);

            // Adjuntar PDF
            using var pdfStream = new MemoryStream(pdfBytes);
            var pdfAttachment = new Attachment(pdfStream, $"Cotizacion-{quoteNumber}.pdf", "application/pdf");
            message.Attachments.Add(pdfAttachment);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort);
            smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(message);

            _logger.LogInformation("✅ Email enviado exitosamente a {Email} - Cotización {QuoteNumber}",
                toEmail, quoteNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email para cotización {QuoteNumber} a {Email}",
                quoteNumber, toEmail);
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

            foreach (var logoPath in possiblePaths)
            {
                _logger.LogInformation("🔍 Buscando logo en: {LogoPath}", logoPath);

                if (File.Exists(logoPath))
                {
                    _logger.LogInformation("✅ Logo encontrado en: {LogoPath}", logoPath);
                    return logoPath;
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

    private string GetEmailHtml(string clientName, string quoteNumber, bool hasLogo)
    {
        // Si hay logo, usar CID, si no, mostrar texto
        var logoHtml = hasLogo
            ? @"<img src=""cid:logo-finesoft"" alt=""Finesoft"" style=""max-width: 180px; height: auto; display: block; margin: 0 auto;"">"
            : @"<div style=""font-size: 32px; font-weight: bold; color: #667eea; letter-spacing: -1px;"">FINESOFT</div>";
        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif; background-color: #f7f7f7;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f7f7f7; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    
                    <!-- Header with Logo -->
                    <tr>
                        <td style=""padding: 40px 40px 30px 40px; text-align: center; border-bottom: 2px solid #333333;"">
                            {logoHtml}
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <p style=""margin: 0 0 24px 0; font-size: 16px; color: #333333; line-height: 1.5;"">
                                Hola <strong>{clientName}</strong>,
                            </p>

                            <!-- Quote Badge -->
                            <div style=""background: #f3f0ff; padding: 20px; border-radius: 8px; margin-bottom: 24px; text-align: center;"">
                                <div style=""color: #667eea; font-size: 14px; font-weight: 600; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;"">
                                    Nueva Cotización
                                </div>
                                <div style=""font-size: 24px; font-weight: bold; color: #333333;"">
                                    {quoteNumber}
                                </div>
                            </div>

                            <p style=""margin: 0 0 20px 0; font-size: 15px; color: #666666; line-height: 1.6;"">
                                Te enviamos la cotización que solicitaste. Hemos preparado una propuesta 
                                detallada con los servicios que necesitas.
                            </p>

                            <!-- Details List -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 24px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 2px solid #222222;"">
                                        <span style=""color: #667eea; margin-right: 8px;"">✓</span>
                                        <span style=""color: #666666; font-size: 14px;"">Descripción de servicios</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 2px solid #222222;"">
                                        <span style=""color: #667eea; margin-right: 8px;"">✓</span>
                                        <span style=""color: #666666; font-size: 14px;"">Precios y cantidades</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 2px solid #222222;"">
                                        <span style=""color: #667eea; margin-right: 8px;"">✓</span>
                                        <span style=""color: #666666; font-size: 14px;"">Términos comerciales</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #667eea; margin-right: 8px;"">✓</span>
                                        <span style=""color: #666666; font-size: 14px;"">Vigencia de la oferta</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Attachment Note -->
                            <div style=""background: #fff4e6; border-left: 4px solid #ff9800; padding: 16px; border-radius: 4px; margin: 24px 0;"">
                                <p style=""margin: 0; font-size: 14px; color: #663c00;"">
                                    <strong>📎 Documento adjunto:</strong> Encontrarás el PDF completo con todos los detalles.
                                </p>
                            </div>

                            <p style=""margin: 24px 0 0 0; font-size: 15px; color: #666666; line-height: 1.6;"">
                                Estamos disponibles para resolver cualquier duda. 
                                Puedes responder directamente a este correo.
                            </p>

                            <!-- Signature -->
                            <div style=""margin-top: 40px; padding-top: 24px; border-top: 2px solid #333333;"">
                                <p style=""margin: 0 0 4px 0; font-size: 15px; color: #333333; font-weight: 600;"">
                                    Equipo Finesoft
                                </p>
                                <p style=""margin: 0; font-size: 14px; color: #999999;"">
                                    Soluciones Tecnológicas
                                </p>
                            </div>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #fafafa; padding: 32px 40px; text-align: center; border-top: 2px solid #333333;"">
                            <div style=""font-size: 14px; font-weight: 600; color: #667eea; margin-bottom: 12px;"">
                                FINESOFT
                            </div>
                            <p style=""margin: 0 0 8px 0; font-size: 13px; color: #999999; line-height: 1.6;"">
                                Blvd. Juan de Dios Batiz #145 PTE<br>
                                Ciudad Juárez, Chihuahua<br>
                                Tel: (668) 817-1400
                            </p>
                            <p style=""margin: 12px 0 0 0;"">
                                <a href=""mailto:informes@finesoft.com.mx"" style=""color: #667eea; text-decoration: none; font-size: 13px;"">
                                    informes@finesoft.com.mx
                                </a>
                            </p>
                            <p style=""margin: 20px 0 0 0; font-size: 12px; color: #cccccc;"">
                                © 2026 Finesoft. Todos los derechos reservados.
                            </p>
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