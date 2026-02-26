using MailKit.Net.Smtp;
using MimeKit;

namespace Teste.Services;

/// <summary>
/// Serviço real de envio de email usando MailKit.
/// Suporta Gmail, Outlook, ou qualquer servidor SMTP.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken ct = default)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var smtpUser = _config["Email:SmtpUser"] ?? "";
        var smtpPass = _config["Email:SmtpPass"] ?? "";
        var fromName = _config["Email:FromName"] ?? "Sistema de Pedidos";
        var fromEmail = _config["Email:FromEmail"] ?? smtpUser;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(smtpUser, smtpPass, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Email enviado para {Email}.", toEmail);
    }
}