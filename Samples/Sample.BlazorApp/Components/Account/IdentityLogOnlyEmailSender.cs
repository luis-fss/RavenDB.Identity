using System.Web;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Sample.BlazorApp.Components.Account;

/// <summary>
/// The <see cref="IEmailSender"/> that only add log messages in <see cref="SendEmailAsync(string, string, string)"/>.
/// It is used to provide a development experience where the email confirmation link is only put in the logs rather than sent via an email.
/// </summary>
public sealed class IdentityLogOnlyEmailSender : IEmailSender
{
    private readonly ILogger<IdentityLogOnlyEmailSender> _logger;

    public IdentityLogOnlyEmailSender(ILogger<IdentityLogOnlyEmailSender> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// This method only logs the messages. It should be replaced by a custom implementation in production.
    /// </summary>
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = HttpUtility.HtmlDecode(htmlMessage);
        _logger.LogInformation("Email send request to {Email}, subject {Subject} and message {Message}", email, subject, message);
        return Task.CompletedTask;
    }
}