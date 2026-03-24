# Web Interface Enhancement Guide

## Overview

The Extension Attributes Automation Worker Service now includes a rich web interface built with ASP.NET Core MVC and Razor Pages. The interface provides real-time monitoring, configuration management, and audit tracking capabilities.

## New Features

### 1. **Splash/Landing Page** (`/`)
- Modern welcome page with application overview
- Quick access to all major features
- Live system statistics
- Feature highlights and quick links

### 2. **Login Page** (`/Home/Login`)
- Authentication interface (currently demo mode)
- Designed for Windows Authentication / Entra AD integration
- Clean, modern design with gradient background
- Automatic redirection for authenticated users

### 3. **Real-Time Dashboard** (`/Home/Dashboard`)
- Live event stream using **SignalR**
- Real-time statistics (total events, success/failure rates, devices affected)
- WebSocket-based updates for instant notifications
- Event filtering and sorting capabilities
- Click-through to detailed event information

### 4. **Configuration Summary** (`/Home/Configuration`)
- Visual display of all configuration settings
- Data source status (Active Directory, Intune)
- Extension attribute mapping details
- Export and audit configuration
- Quick action buttons

### 5. **About Page** (`/Home/About`)
- System information and version details
- Component list and technology stack
- Feature overview
- Documentation links and API endpoints
- Real-time uptime calculation

### 6. **Change Detail Page** (`/Home/ChangeDetail?id={eventId}`)
- Detailed view of individual audit events
- Shows complete event information including:
  - Event metadata (ID, type, severity, timestamp)
  - Device and attribute details
  - Value changes (old vs new)
  - Error messages and duration
- Related events for the same device/attribute
- Print-friendly layout

## Technical Implementation

### SignalR Hub
- **Hub Location**: `/hubs/audit`
- **Events**:
  - `ReceiveAuditEvent`: Broadcasts new audit events in real-time
  - `ReceiveConfigurationUpdate`: Notifies clients of configuration changes

### MVC Controllers

#### HomeController
- `Index`: Landing page
- `Login`: Authentication page
- `Dashboard`: Real-time monitoring
- `Configuration`: Configuration summary
- `About`: System information
- `ChangeDetail`: Event details

### Integration with Audit Logging
The `AuditLogger` service now automatically broadcasts events to connected SignalR clients:
```csharp
// Events are automatically sent to all connected clients
await _hubContext.Clients.All.SendAsync("ReceiveAuditEvent", auditEvent);
```

## Running the Web Interface

### Command Line
```bash
# Run in web/dashboard mode
dotnet run -- --web

# Or use the alias
dotnet run -- --dashboard
dotnet run -- -w
```

### Access URLs
Once running, the following URLs are available:

- **Home**: `http://localhost:5000/`
- **Dashboard**: `http://localhost:5000/Home/Dashboard`
- **Configuration**: `http://localhost:5000/Home/Configuration`
- **About**: `http://localhost:5000/Home/About`
- **API Docs**: `http://localhost:5000/api-docs`
- **Health UI**: `http://localhost:5000/health-ui`
- **SignalR Hub**: `ws://localhost:5000/hubs/audit`

### Existing Static Pages
The original static HTML pages are still available:
- **Legacy Dashboard**: `http://localhost:5000/index.html`
- **Audit Logs**: `http://localhost:5000/audit.html`

## Architecture

### Frontend Stack
- **Bootstrap 5**: UI framework
- **Font Awesome 6**: Icons
- **SignalR Client**: Real-time communication
- **Vanilla JavaScript**: Interactivity

### Backend Stack
- **ASP.NET Core 9.0**: Web framework
- **MVC + Razor**: View engine
- **SignalR**: Real-time messaging
- **Entity Framework**: (Future: database integration)

## SignalR Usage Example

### JavaScript Client
```javascript
// Connect to SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/audit")
    .withAutomaticReconnect()
    .build();

// Listen for audit events
connection.on("ReceiveAuditEvent", function (event) {
    console.log("New event:", event);
    // Update UI with the event
});

// Start connection
await connection.start();
```

### Server-Side Broadcasting
```csharp
// In AuditLogger.cs
await _hubContext.Clients.All.SendAsync("ReceiveAuditEvent", auditEvent);
```

## Customization

### Styling
The interface uses CSS custom properties for easy theming:
```css
:root {
    --primary-gradient: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    --success-color: #28a745;
    --danger-color: #dc3545;
    --warning-color: #ffc107;
    --info-color: #17a2b8;
}
```

### Views Location
All Razor views are located in:
```
ExtensionAttributes.Worker/Views/
├── Home/
│   ├── Index.cshtml          (Landing page)
│   ├── Login.cshtml          (Login page)
│   ├── Dashboard.cshtml      (Real-time dashboard)
│   ├── Configuration.cshtml  (Configuration summary)
│   ├── About.cshtml          (About page)
│   ├── ChangeDetail.cshtml   (Event details)
│   └── Error.cshtml          (Error page)
├── Shared/
│   └── _Layout.cshtml        (Master layout)
├── _ViewStart.cshtml
└── _ViewImports.cshtml
```

## Future Enhancements

Potential improvements include:
- [ ] User authentication with Entra AD
- [ ] Role-based access control (RBAC)
- [ ] Database persistence for audit logs
- [ ] Advanced filtering and search
- [ ] Export to multiple formats (PDF, Excel)
- [ ] Custom dashboards and widgets
- [ ] Email notifications
- [ ] Mobile-responsive improvements

## Security Considerations

- Currently in demo mode for authentication
- In production, implement:
  - Windows Authentication
  - Entra AD (Azure AD) authentication
  - HTTPS enforcement
  - CSRF protection (built-in with ASP.NET Core)
  - Content Security Policy headers
  - Rate limiting for API endpoints

## Troubleshooting

### SignalR Connection Issues
- Ensure the web application is running in `--web` mode
- Check browser console for connection errors
- Verify CORS settings if accessing from different origin

### Views Not Loading
- Ensure all `.cshtml` files are present in the Views folder
- Check `_ViewImports.cshtml` for correct namespaces
- Verify the build includes the Views folder

### CSS/JavaScript Not Loading
- Check that static files middleware is configured
- Ensure CDN links are accessible
- Verify wwwroot folder contains static assets

## Support

For issues, questions, or contributions:
- GitHub Issues: [Repository Issues Page]
- Documentation: See other markdown files in the repository
- API Documentation: Available at `/api-docs` when running

---

**Built with ❤️ by the Extension Attributes Automation Community**
