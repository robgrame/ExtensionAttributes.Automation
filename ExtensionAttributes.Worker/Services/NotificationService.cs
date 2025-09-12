using Microsoft.Extensions.Logging;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Services
{
    public interface INotificationService
    {
        Task SendEmailAsync(string subject, string body, string? recipient = null);
        Task SendTeamsMessageAsync(string message, string? webhookUrl = null);
        Task SendSlackMessageAsync(string message, string channel = "general", string? webhookUrl = null);
        Task SendCriticalAlertAsync(string title, string message, Dictionary<string, object>? data = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NotificationSettings _settings;

        public NotificationService(ILogger<NotificationService> logger, IHttpClientFactory httpClientFactory, Microsoft.Extensions.Options.IOptions<NotificationSettings> settings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string subject, string body, string? recipient = null)
        {
            try
            {
                // For now, this is a placeholder implementation
                // In a real implementation, you would integrate with:
                // - Azure Communication Services
                // - SendGrid
                // - SMTP server
                // - Microsoft Graph (for Outlook/Exchange)

                var emailRecipient = recipient ?? _settings.DefaultEmailRecipient;
                
                _logger.LogInformation("?? EMAIL NOTIFICATION: {Subject}", subject);
                _logger.LogInformation("To: {Recipient}", emailRecipient);
                _logger.LogInformation("Body: {Body}", body);

                // TODO: Implement actual email sending
                // Example with System.Net.Mail:
                // using var smtpClient = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort);
                // await smtpClient.SendMailAsync(new MailMessage(from, to, subject, body));

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification: {Error}", ex.Message);
            }
        }

        public async Task SendTeamsMessageAsync(string message, string? webhookUrl = null)
        {
            try
            {
                var webhook = webhookUrl ?? _settings.TeamsWebhookUrl;
                
                if (string.IsNullOrEmpty(webhook))
                {
                    _logger.LogWarning("Teams webhook URL not configured, skipping Teams notification");
                    return;
                }

                var httpClient = _httpClientFactory.CreateClient();
                
                var teamsMessage = new
                {
                    text = message,
                    title = "Extension Attributes Automation",
                    themeColor = "ff0000", // Red for alerts
                    sections = new[]
                    {
                        new
                        {
                            activityTitle = "Extension Attributes Worker Service",
                            activitySubtitle = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                            text = message,
                            facts = new[]
                            {
                                new { name = "Service", value = "Extension Attributes Automation" },
                                new { name = "Timestamp", value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                                new { name = "Server", value = Environment.MachineName }
                            }
                        }
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(teamsMessage);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(webhook, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("? Teams notification sent successfully");
                }
                else
                {
                    _logger.LogError("? Failed to send Teams notification. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Teams notification: {Error}", ex.Message);
            }
        }

        public async Task SendSlackMessageAsync(string message, string channel = "general", string? webhookUrl = null)
        {
            try
            {
                var webhook = webhookUrl ?? _settings.SlackWebhookUrl;
                
                if (string.IsNullOrEmpty(webhook))
                {
                    _logger.LogWarning("Slack webhook URL not configured, skipping Slack notification");
                    return;
                }

                var httpClient = _httpClientFactory.CreateClient();
                
                var slackMessage = new
                {
                    channel = $"#{channel}",
                    username = "Extension Attributes Bot",
                    icon_emoji = ":robot_face:",
                    text = message,
                    attachments = new[]
                    {
                        new
                        {
                            color = "danger",
                            title = "Extension Attributes Automation Alert",
                            text = message,
                            fields = new[]
                            {
                                new { title = "Service", value = "Extension Attributes Worker", @short = true },
                                new { title = "Server", value = Environment.MachineName, @short = true },
                                new { title = "Timestamp", value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"), @short = false }
                            },
                            footer = "Extension Attributes Automation",
                            ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        }
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(slackMessage);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(webhook, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("? Slack notification sent successfully");
                }
                else
                {
                    _logger.LogError("? Failed to send Slack notification. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Slack notification: {Error}", ex.Message);
            }
        }

        public async Task SendCriticalAlertAsync(string title, string message, Dictionary<string, object>? data = null)
        {
            try
            {
                _logger.LogCritical("?? CRITICAL ALERT: {Title} - {Message}", title, message);
                
                var alertMessage = $"?? **CRITICAL ALERT**: {title}\n\n{message}";
                
                if (data != null && data.Any())
                {
                    alertMessage += "\n\n**Additional Information:**\n";
                    foreach (var kvp in data)
                    {
                        alertMessage += $"• **{kvp.Key}**: {kvp.Value}\n";
                    }
                }

                alertMessage += $"\n**Server**: {Environment.MachineName}";
                alertMessage += $"\n**Timestamp**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}";

                // Send to all configured notification channels
                var tasks = new List<Task>();

                if (_settings.EnableEmailNotifications)
                {
                    tasks.Add(SendEmailAsync($"CRITICAL: {title}", alertMessage));
                }

                if (_settings.EnableTeamsNotifications)
                {
                    tasks.Add(SendTeamsMessageAsync(alertMessage));
                }

                if (_settings.EnableSlackNotifications)
                {
                    tasks.Add(SendSlackMessageAsync(alertMessage, _settings.SlackAlertChannel ?? "alerts"));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send critical alert: {Error}", ex.Message);
            }
        }
    }

    public class NotificationSettings
    {
        public bool EnableEmailNotifications { get; set; } = false;
        public bool EnableTeamsNotifications { get; set; } = false;
        public bool EnableSlackNotifications { get; set; } = false;
        
        public string? DefaultEmailRecipient { get; set; }
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        
        public string? TeamsWebhookUrl { get; set; }
        
        public string? SlackWebhookUrl { get; set; }
        public string? SlackAlertChannel { get; set; } = "alerts";
        
        // Thresholds for automatic alerts
        public int FailedDevicesThreshold { get; set; } = 10;
        public int ConsecutiveFailuresThreshold { get; set; } = 3;
        public TimeSpan HealthCheckFailureThreshold { get; set; } = TimeSpan.FromMinutes(5);
    }
}