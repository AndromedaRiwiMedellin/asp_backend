using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace asp_backend.Services;

public interface IEmailService
{
    Task SendTicketEmailAsync(string toEmail, string customerName, string eventTitle, DateTime? eventDate, string seatNumber, string qrCode, string ticketId);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendTicketEmailAsync(string toEmail, string customerName, string eventTitle, DateTime? eventDate, string seatNumber, string qrCode, string ticketId)
    {
        try
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPortString = _config["Smtp:Port"];
            var smtpUser = _config["Smtp:Username"];
            var smtpPass = _config["Smtp:Password"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortString))
            {
                _logger.LogWarning("SMTP no está configurado. Saltando envío de correo de boleta a {Email}", toEmail);
                return;
            }

            int smtpPort = int.Parse(smtpPortString);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Orbix POS", smtpUser));
            message.To.Add(new MailboxAddress(customerName ?? "Cliente", toEmail));
            message.Subject = $"¡Tu boleta para {eventTitle} está lista!";

            var builder = new BodyBuilder();
            
            var formattedDate = eventDate?.ToString("dd/MM/yyyy HH:mm") ?? "Fecha por confirmar";
            var seatText = !string.IsNullOrEmpty(seatNumber) ? $"<strong>Asiento:</strong> {seatNumber}<br/>" : "";

            builder.HtmlBody = $@"
            <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 10px; overflow: hidden;"">
                <div style=""background-color: #0a192f; color: #64ffda; padding: 20px; text-align: center;"">
                    <h1 style=""margin: 0; font-size: 24px;"">Orbix POS</h1>
                </div>
                <div style=""padding: 30px; background-color: #ffffff; color: #333;"">
                    <h2 style=""color: #0a192f; margin-top: 0;"">¡Hola {customerName}!</h2>
                    <p>Gracias por tu compra. Aquí tienes los detalles de tu boleta:</p>
                    
                    <div style=""background-color: #f0fdfa; border-left: 4px solid #14b8a6; padding: 15px; margin: 20px 0; border-radius: 4px;"">
                        <h3 style=""margin-top: 0; color: #0f766e;"">{eventTitle}</h3>
                        <p style=""margin-bottom: 5px;""><strong>Fecha:</strong> {formattedDate}</p>
                        {seatText}
                        <p style=""margin-bottom: 0;""><strong>Ticket ID:</strong> {ticketId}</p>
                    </div>

                    <div style=""text-align: center; margin: 30px 0;"">
                        <p style=""font-size: 14px; color: #666; margin-bottom: 10px;"">Presenta este código QR en la entrada:</p>
                        <!-- Aquí podrías usar una API pública para generar el QR en el correo, por ejemplo goqr.me -->
                        <img src=""https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={qrCode}"" alt=""QR Code"" style=""border: 1px solid #ddd; padding: 10px; border-radius: 8px;"" />
                    </div>
                </div>
                <div style=""background-color: #f8f9fa; padding: 15px; text-align: center; color: #888; font-size: 12px;"">
                    <p style=""margin: 0;"">Este es un correo automático. Por favor no respondas a esta dirección.</p>
                    <p style=""margin: 5px 0 0;"">&copy; {DateTime.Now.Year} Orbix Events. Todos los derechos reservados.</p>
                </div>
            </div>";

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Correo enviado exitosamente a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de boleta a {Email}", toEmail);
        }
    }
}
