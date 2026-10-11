namespace FamilyBudget.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task<bool> SendInvitationEmailAsync(string toEmail, string familyName, string inviteLink, string inviterName);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
    Task<bool> SendBillReminderEmailAsync(string toEmail, string billName, decimal amount, DateTime dueDate, string currencySymbol);
}
