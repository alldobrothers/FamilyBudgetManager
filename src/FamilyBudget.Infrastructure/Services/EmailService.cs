using System.Net;
using System.Net.Mail;
using FamilyBudget.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyBudget.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
        var smtpUser = _configuration["EmailSettings:SmtpUsername"];
        var smtpPass = _configuration["EmailSettings:SmtpPassword"];
        var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@familybudget.local";
        var senderName = _configuration["EmailSettings:SenderName"] ?? "Family Budget Manager";

        // If no SMTP host or password configured, honestly log and skip external transmission
        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpPass) || smtpHost.Contains("sendgrid.net") && smtpPass.Contains("YourSendGridApiKeyHere"))
        {
            _logger.LogInformation(
                "[EMAIL DISPATCHER (SIMULATED - NO SMTP CONFIGURED)]: To={To}, Subject='{Subject}'. Body Preview:\n{Body}",
                toEmail, subject, htmlBody);
            return true;
        }

        try
        {
            int port = int.TryParse(smtpPortStr, out var p) ? p : 587;
            using var client = new SmtpClient(smtpHost, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Successfully dispatched email via SMTP to {To}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch email via SMTP to {To}", toEmail);
            return false;
        }
    }

    public Task<bool> SendInvitationEmailAsync(string toEmail, string familyName, string inviteLink, string inviterName)
    {
        var subject = $"Invitation to join {familyName} on Family Budget Manager";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                <h2 style='color: #0f172a;'>You've been invited to {familyName}</h2>
                <p>Hello,</p>
                <p><strong>{inviterName}</strong> has invited you to join their household finances on Family Budget Manager.</p>
                <div style='margin: 30px 0; text-align: center;'>
                    <a href='{inviteLink}' style='background-color: #059669; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Accept Invitation</a>
                </div>
                <p style='color: #64748b; font-size: 13px;'>Or copy and paste this link in your browser:<br/><a href='{inviteLink}'>{inviteLink}</a></p>
                <p style='color: #94a3b8; font-size: 12px;'>This invitation expires in 7 days.</p>
            </div>";
        return SendEmailAsync(toEmail, subject, body);
    }

    public Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var subject = "Reset your Family Budget Manager password";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                <h2 style='color: #0f172a;'>Password Reset Request</h2>
                <p>You recently requested to reset your password for Family Budget Manager.</p>
                <div style='margin: 30px 0; text-align: center;'>
                    <a href='{resetLink}' style='background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Reset Password</a>
                </div>
                <p style='color: #64748b; font-size: 13px;'>If you did not request this reset, you can safely ignore this email.</p>
            </div>";
        return SendEmailAsync(toEmail, subject, body);
    }

    public Task<bool> SendBillReminderEmailAsync(string toEmail, string billName, decimal amount, DateTime dueDate, string currencySymbol)
    {
        var subject = $"Bill Reminder: {billName} is due soon";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                <h2 style='color: #b45309;'>Upcoming Bill Reminder</h2>
                <p>Your recurring household bill <strong>{billName}</strong> for <strong>{currencySymbol}{amount:N2}</strong> is scheduled for payment on <strong>{dueDate:dd MMM yyyy}</strong>.</p>
                <p>Please log in to Family Budget Manager to mark it as paid once settled.</p>
            </div>";
        return SendEmailAsync(toEmail, subject, body);
    }
}
