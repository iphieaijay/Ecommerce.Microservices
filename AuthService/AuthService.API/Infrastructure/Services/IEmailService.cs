namespace AuthService.Infrastructure.Services
{

    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
        Task SendPasswordResetAsync(string email, string resetLink);
        Task SendPasswordChangedNotificationAsync(string email, string userName);
        Task SendWelcomeEmailAsync(string email, string userName);
    }

}
