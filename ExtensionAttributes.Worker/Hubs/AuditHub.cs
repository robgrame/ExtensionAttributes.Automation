using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ExtensionAttributes.Automation.WorkerSvc.Services;

namespace ExtensionAttributes.Automation.WorkerSvc.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time audit log updates
    /// </summary>
    public class AuditHub : Hub
    {
        private readonly ILogger<AuditHub> _logger;

        public AuditHub(ILogger<AuditHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected to AuditHub: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected from AuditHub: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Send audit event to all connected clients
        /// </summary>
        public async Task BroadcastAuditEvent(AuditEvent auditEvent)
        {
            await Clients.All.SendAsync("ReceiveAuditEvent", auditEvent);
        }

        /// <summary>
        /// Send configuration update notification
        /// </summary>
        public async Task BroadcastConfigurationUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveConfigurationUpdate", message);
        }
    }

    /// <summary>
    /// Audit event DTO for SignalR
    /// </summary>
    public class AuditEvent
    {
        public string EventId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? ExtensionAttribute { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? User { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
