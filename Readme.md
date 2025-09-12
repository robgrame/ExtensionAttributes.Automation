# RGP Extension Attributes Automation Worker Service

A complete and highly resilient solution for automating Microsoft Entra AD (Azure AD) Extension Attributes management based on information from Active Directory and/or Microsoft Intune.

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Health Checks](https://img.shields.io/badge/Health%20Checks-✅-brightgreen.svg)]()
[![Retry Logic](https://img.shields.io/badge/Polly%20Resilience-✅-brightgreen.svg)]()
[![Notifications](https://img.shields.io/badge/Multi--Channel%20Alerts-✅-brightgreen.svg)]()
[![Web Dashboard](https://img.shields.io/badge/Web%20Dashboard-✅-brightgreen.svg)]()

## 📋 Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Web Dashboard & API](#web-dashboard--api)
- [Health Checks and Monitoring](#health-checks-and-monitoring)
- [Configuration Examples](#configuration-examples)
- [Available Properties](#available-properties)
- [Resilience and Retry Logic](#resilience-and-retry-logic)
- [Multi-Channel Notifications](#multi-channel-notifications)
- [Troubleshooting](#troubleshooting)
- [Testing and Debug](#testing-and-debug)
- [Contributing](#contributing)
- [License](#license)

## 🔍 Overview

The **RGP Extension Attributes Automation Worker Service** is a powerful and highly resilient tool that automates the synchronization of Microsoft Entra AD Extension Attributes using data from:

- **Active Directory on-premise** - AD computer attributes
- **Microsoft Intune** - Hardware, software, and compliance information from managed devices

The solution supports regular expressions for extracting specific values, default values, a unified configuration that prevents collisions, **comprehensive monitoring**, **automatic retry**, **multi-channel notifications**, and a **web dashboard with REST API**.

## ✨ Key Features

### 🎯 **Unified Configuration**
- **Single configuration section** for all Extension Attributes
- **`dataSource` field** to specify whether to use Active Directory or Intune
- **Automatic collision prevention** - impossible to configure the same Extension Attribute with multiple sources

### 🚀 **Multiple Data Sources**
- **Active Directory**: Uses AD computer attributes like OU, company, location, department
- **Microsoft Intune**: Uses device information like manufacturer, model, compliance state, storage info

### 🔧 **Advanced Processing**
- **Regular Expressions**: Extract specific parts from attribute values
- **Default Values**: Automatic fallback when data is not available
- **Concurrent Processing**: Efficient handling of thousands of devices
- **Detailed Logging**: Complete operation tracking with Serilog
- **🆕 Single Device Processing**: Debug and test on specific devices

### 🔄 **Execution Modes**
- **Windows Service**: Automatic scheduled execution in background
- **Console Application**: Manual execution for testing and debugging
- **🆕 Device-Specific**: Processing individual devices for troubleshooting
- **🆕 Device by ID**: Processing via Entra AD Device ID
- **🆕 Web Dashboard**: Interactive web interface with real-time monitoring

### 📅 **Flexible Scheduling**
- **Quartz.NET Integration**: Advanced scheduling with CRON expressions
- **Separate Jobs**: Ability to schedule AD and Intune independently
- **Unified Job**: Combined processing of all sources

### 🩺 **Health Checks and Monitoring**
- **4 Integrated Health Checks**: Configuration, Entra AD, Active Directory, Intune
- **Real-time monitoring** of service status
- **Detailed metrics** for each component
- **Automatic alerting** for critical issues

### 🔄 **Resilience and Retry Logic**
- **Polly Integration**: Automatic retry for transient errors
- **Circuit Breaker**: Prevention of cascading failures
- **Graph API Throttling**: Intelligent handling of Microsoft rate limits
- **Exponential Backoff**: Optimization of retry strategies

### 📢 **Multi-Channel Notification System**
- **Microsoft Teams**: Notifications via webhook with formatted cards
- **Slack**: Structured messages with attachments
- **Email**: Support for SMTP/SendGrid/Azure Communication Services
- **Intelligent Alerting**: Configurable thresholds to avoid spam

### 🌐 **Web Dashboard & REST API**
- **🆕 Interactive Dashboard**: Real-time monitoring and device processing
- **🆕 REST API**: Comprehensive endpoints for system integration
- **🆕 Health Checks UI**: Visual health status monitoring
- **🆕 Swagger Documentation**: Complete API documentation
- **🆕 Remote Device Processing**: Process devices via web interface

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Entra AD (Azure AD)                                │
│                       ┌─────────────────────┐                               │
│                       │  Extension          │                               │
│                       │  Attributes 1-15    │                               │
│                       └─────────────────────┘                               │
└─────────────────────────────────┬───────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    RGP Extension Attributes Worker                          │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │               🌐 Web Dashboard & REST API                              │ │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────────┐ │ │
│  │  │  Status API     │ │   Health UI     │ │     Device Processing       │ │ │
│  │  └─────────────────┘ └─────────────────┘ └─────────────────────────────┘ │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                  UnifiedExtensionAttributeHelper                       │ │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────────┐ │ │
│  │  │   AD Helper     │ │  Intune Helper  │ │    Notification Service     │ │ │
│  │  └─────────────────┘ └─────────────────┘ └─────────────────────────────┘ │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                        Health Checks                                   │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐ │ │
│  │  │    Config   │ │  Entra AD   │ │     AD      │ │      Intune         │ │ │
│  │  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────────────┘ │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                      Polly Resilience                                  │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐ │ │
│  │  │   Retry     │ │Circuit Break│ │   Timeout   │ │  Graph Throttling   │ │ │
│  │  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────────────┘ │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────┬─────────────────┬─────────────────┬───────────────────┘
                      │                 │                 │
                      ▼                 ▼                 ▼
        ┌─────────────────────┐ ┌─────────────────┐ ┌─────────────────────┐
        │   Active Directory  │ │ Microsoft Intune│ │    Notifications    │
        │                     │ │                 │ │                     │
        │ • Computer Objects  │ │ • Device Info   │ │ • Teams Webhooks    │
        │ • OU Structure      │ │ • Hardware Info │ │ • Slack Integration │
        │ • Attributes        │ │ • Compliance    │ │ • Email SMTP        │
        └─────────────────────┘ └─────────────────┘ └─────────────────────┘
```

## 🚀 Installation

### Prerequisites

- **.NET 9.0** Runtime/SDK
- **Windows Server 2019** or higher (for AD integration)
- **Active Directory access** (if used)
- **Microsoft Graph API permissions** for Entra AD and Intune
- **Certificate or Client Secret** for Azure authentication

### Quick Installation

1. **Clone the repository:**
```bash
git clone https://github.com/robgrame/ExtensionAttributes.Automation.git
cd ExtensionAttributes.Automation
```

2. **Build the solution:**
```bash
dotnet build --configuration Release
```

3. **Install as Windows Service:**
```cmd
cd ExtensionAttributes.Worker\bin\Release\net9.0
ExtensionAttributes.WorkerSvc.exe --service
```

## ⚙️ Configuration

### Base Configuration

The `appsettings.json` file contains all necessary configurations:

```json
{
  "AppSettings": {
    "ExportPath": "C:\\Temp\\Automation\\Export",
    "ExportFileNamePrefix": "RGP.DevicesProcessed",
    "DataSources": {
      "EnableActiveDirectory": true,
      "EnableIntune": true,
      "PreferredDataSource": "Both"
    },
    "ExtensionAttributeMappings": [
      {
        "extensionAttribute": "extensionAttribute1",
        "sourceAttribute": "distinguishedName",
        "dataSource": "ActiveDirectory",
        "regex": "(?<=OU=)(?<departmentOUName>[^,]+)(?=,OU=(?i:Locations))",
        "defaultValue": "Unknown Department",
        "useHardwareInfo": false,
        "propertyPath": ""
      }
    ]
  }
}
```

### Extension Attributes Configuration

Each mapping is defined with these parameters:

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `extensionAttribute` | string | Target Extension Attribute (1-15) | `"extensionAttribute5"` |
| `sourceAttribute` | string | Source attribute (AD or Intune) | `"manufacturer"`, `"distinguishedName"` |
| `dataSource` | enum | Data source: `"ActiveDirectory"` or `"Intune"` | `"Intune"` |
| `regex` | string | Regular expression for value extraction | `"^(\\d+\\.\\d+)"` |
| `defaultValue` | string | Default value if attribute is empty | `"Unknown"` |
| `useHardwareInfo` | boolean | Use detailed hardware information (Intune) | `false` |
| `propertyPath` | string | Path for nested properties (future use) | `""` |

### 🆕 Notification Configuration

```json
{
  "NotificationSettings": {
    "EnableEmailNotifications": false,
    "EnableTeamsNotifications": true,
    "EnableSlackNotifications": false,
    "DefaultEmailRecipient": "admin@company.com",
    "TeamsWebhookUrl": "https://outlook.office.com/webhook/...",
    "SlackWebhookUrl": "https://hooks.slack.com/services/...",
    "SlackAlertChannel": "alerts",
    "FailedDevicesThreshold": 10,
    "ConsecutiveFailuresThreshold": 3,
    "HealthCheckFailureThreshold": "00:05:00"
  }
}
```

### Data Sources Configuration

```json
{
  "DataSources": {
    "EnableActiveDirectory": true,    // Enable AD mappings
    "EnableIntune": true,            // Enable Intune mappings
    "PreferredDataSource": "Both"    // "ActiveDirectory", "Intune", "Both"
  }
}
```

### Azure Authentication

```json
{
  "EntraADHelperSettings": {
    "ClientId": "your-client-id",
    "TenantId": "your-tenant-id",
    "UseClientSecret": false,
    "CertificateThumbprint": "cert-thumbprint"
  }
}
```

### Active Directory Configuration

```json
{
  "ADHelperSettings": {
    "RootOrganizationaUnitDN": "OU=Computers,DC=company,DC=com",
    "AttributesToLoad": ["cn", "distinguishedName", "company", "department"],
    "ExcludedOUs": [
      "OU=Disabled,OU=Computers,DC=company,DC=com"
    ]
  }
}
```

### Intune Configuration

```json
{
  "IntuneHelperSettings": {
    "MaxConcurrentRequests": 10,
    "EnableHardwareInfoRetrieval": true,
    "EnableSoftwareInfoRetrieval": false,
    "PageSize": 1000,
    "ClientTimeout": 60000
  }
}
```

## 🎮 Usage

### Console Mode (Testing and Debug)

```bash
# Single execution of all devices
ExtensionAttributes.WorkerSvc.exe --console

# Show complete help
ExtensionAttributes.WorkerSvc.exe --help

# 🆕 Process specific device by name
ExtensionAttributes.WorkerSvc.exe --device COMPUTER-NAME

# 🆕 Process specific device by Entra AD Device ID
ExtensionAttributes.WorkerSvc.exe --deviceid "abc123-def456-ghi789"

# 🆕 Start web dashboard mode
ExtensionAttributes.WorkerSvc.exe --web
```

### Windows Service Mode

```bash
# Install and start as service
ExtensionAttributes.WorkerSvc.exe --service
```

### 🆕 Web Dashboard Mode

```bash
# Start web dashboard with API
ExtensionAttributes.WorkerSvc.exe --web
```

The web dashboard will be available at:
- **Main Dashboard**: http://localhost:5000
- **Health Checks UI**: http://localhost:5000/health-ui
- **API Documentation**: http://localhost:5000/api-docs

### Scheduling

Scheduling configuration is defined in `schedule.json`:

```json
{
  "QuartzJobs": [
    {
      "JobName": "SetUnifiedExtensionAttributeJob",
      "JobDescription": "Process Extension Attributes from AD and Intune",
      "CronExpression": "0 0/5 * ? * * *"  // Every 5 minutes
    }
  ]
}
```

## 🌐 Web Dashboard & API

The solution includes a comprehensive web dashboard and REST API for monitoring and managing the extension attributes automation.

### Dashboard Features

- **🎛️ Real-time System Status**: Live health check monitoring
- **📊 System Information**: Application metrics, memory usage, uptime
- **⚙️ Configuration View**: Extension attribute mappings overview
- **🔧 Device Processing**: Process individual devices via web interface
- **📈 Health Checks Detail**: Detailed status of all components
- **🔄 Auto-refresh**: Automatic updates every 30 seconds

### REST API Endpoints

#### System Status
- `GET /api/status/health` - Get overall system health
- `GET /api/status/info` - Get system information and statistics
- `GET /api/status/mappings` - Get extension attribute mappings

#### Device Processing
- `POST /api/status/process-device/{deviceName}` - Process device by name
- `POST /api/status/process-device-by-id/{deviceId}` - Process device by ID

#### Health Checks
- `GET /health` - Health check endpoint (JSON format)
- `GET /health-ui` - Visual health checks dashboard

### API Response Examples

**System Health Response:**
```json
{
  "status": "Healthy",
  "totalDuration": 245.67,
  "timestamp": "2025-01-13T10:30:00Z",
  "checks": [
    {
      "name": "configuration",
      "status": "Healthy",
      "duration": 12.34,
      "description": "Configuration validation successful"
    }
  ]
}
```

**Device Processing Response:**
```json
{
  "deviceName": "DESKTOP-ABC123",
  "processed": true,
  "timestamp": "2025-01-13T10:30:00Z",
  "message": "Device processed successfully"
}
```

### Dashboard Screenshots

The web dashboard provides:
- **Color-coded health status** indicators
- **Interactive device processing** forms
- **Real-time metrics** with auto-refresh
- **Responsive design** for mobile and desktop
- **Professional styling** with Bootstrap 5

## 🩺 Health Checks and Monitoring

The system includes **4 integrated health checks** that continuously monitor the status of all components:

### Available Health Checks

| Health Check | Description | Verification |
|--------------|-------------|--------------|
| **Configuration** | Validates application configuration | Mappings, data sources, required parameters |
| **Entra AD** | Tests Graph API connectivity | Authentication, permissions, reachability |
| **Active Directory** | Tests AD on-premise connection | LDAP binding, OU access, credentials |
| **Intune** | Verifies access to managed devices | Graph API Intune, device management |

### Health Check States

- 🟢 **Healthy** - Service functioning correctly
- 🟡 **Degraded** - Service functioning with warnings
- 🔴 **Unhealthy** - Service not functioning, requires intervention

### Included Metrics

```json
{
  "status": "Healthy",
  "timestamp": "2025-01-13T10:30:00Z",
  "deviceCount": 1250,
  "adEnabled": true,
  "intuneEnabled": true,
  "mappingCount": 10
}
```

## 💡 Configuration Examples

### Example 1: Department from Active Directory OU

```json
{
  "extensionAttribute": "extensionAttribute1",
  "sourceAttribute": "distinguishedName", 
  "dataSource": "ActiveDirectory",
  "regex": "OU=([^,]+),OU=Departments",
  "defaultValue": "No Department"
}
```

**Input**: `CN=PC001,OU=IT,OU=Departments,DC=company,DC=com`  
**Output**: `IT`

### Example 2: Manufacturer from Intune

```json
{
  "extensionAttribute": "extensionAttribute5",
  "sourceAttribute": "manufacturer",
  "dataSource": "Intune", 
  "defaultValue": "Unknown Manufacturer"
}
```

**Input**: Device manufacturer from Intune  
**Output**: `Dell Inc.`, `HP`, `Microsoft Corporation`

### Example 3: Formatted OS Version

```json
{
  "extensionAttribute": "extensionAttribute10",
  "sourceAttribute": "osversion",
  "dataSource": "Intune",
  "regex": "^(\\d+\\.\\d+)",
  "defaultValue": "Unknown"
}
```

**Input**: `10.0.19045.3570`  
**Output**: `10.0`

### Example 4: Storage in GB

```json
{
  "extensionAttribute": "extensionAttribute8",
  "sourceAttribute": "totalstoragegb", 
  "dataSource": "Intune",
  "defaultValue": "0"
}
```

**Output**: `256`, `512`, `1024` (GB)

## 📊 Available Properties

### Active Directory Properties

| Property | Description | Example |
|----------|-------------|---------|
| `distinguishedName` | Complete computer DN | `CN=PC001,OU=IT,DC=company,DC=com` |
| `company` | Company attribute | `ACME Corporation` |
| `department` | Department attribute | `IT Department` |
| `location` | Location attribute | `Milan, Italy` |
| `description` | Description attribute | `Development Workstation` |

### Intune Device Properties

#### Basic Information
| Property | Description | Example |
|----------|-------------|---------|
| `devicename` | Device name | `DESKTOP-ABC123` |
| `manufacturer` | Manufacturer | `Dell Inc.`, `HP`, `Microsoft Corporation` |
| `model` | Model | `OptiPlex 7090`, `Surface Pro 8` |
| `serialnumber` | Serial number | `ABC123DEF456` |

#### Operating System  
| Property | Description | Example |
|----------|-------------|---------|
| `operatingsystem` | OS | `Windows` |
| `osversion` | OS version | `10.0.19045.3570` |

#### Compliance and Management
| Property | Description | Example |
|----------|-------------|---------|
| `compliancestate` | Compliance state | `Compliant`, `NonCompliant`, `Unknown` |
| `manageddeviceownertype` | Ownership type | `Corporate`, `Personal` |
| `managementagent` | Management agent | `MDM` |

#### Date and Synchronization
| Property | Description | Format |
|----------|-------------|--------|
| `lastsyncdate` | Last sync date | `2025-01-01` |
| `lastsynctime` | Last sync time | `14:30:15` |
| `lastsyncfull` | Complete date/time | `2025-01-01 14:30:15` |
| `enrolleddate` | Enrollment date | `2024-12-15` |

#### Storage
| Property | Description | Example |
|----------|-------------|---------|
| `totalstorage` | Formatted total storage | `256.00 GB` |
| `totalstoragegb` | Total storage in GB | `256` |
| `freestorage` | Formatted free storage | `128.50 GB` |  
| `freestoragegb` | Free storage in GB | `128` |

#### Identifiers
| Property | Description | Example |
|----------|-------------|---------|
| `deviceid` | Intune device ID | `abc123-def456-ghi789` |
| `azureaddeviceid` | Entra AD ID | `xyz789-uvw456-rst123` |
| `userprincipalname` | User UPN | `user@company.com` |

#### Network and Telephony
| Property | Description | Example |
|----------|-------------|---------|
| `phonenumber` | Phone number | `+39 123 456 7890` |
| `wifimacaddress` | WiFi MAC | `AA:BB:CC:DD:EE:FF` |
| `imei` | IMEI | `123456789012345` |
| `subscribercarrier` | Carrier | `Vodafone IT` |

## 🔄 Resilience and Retry Logic

The system uses **Polly** to implement advanced resilience strategies:

### Retry Policies

#### 🔄 **HTTP Retry Policy**
- **3 attempts** with exponential backoff (2s, 4s, 8s)
- **Transient error handling**: 5XX, 408, 429
- **Detailed logging** of each attempt

#### ⚡ **Graph API Throttling Policy**
- **5 attempts** with jitter to avoid thundering herd
- **Respect Retry-After header** from Microsoft
- **Intelligent handling** of rate limiting

#### 🔌 **Circuit Breaker Policy**
- **5 consecutive errors** open the circuit
- **30 seconds** break duration
- **Half-open testing** for automatic recovery

#### ⏱️ **Timeout Policy**
- **30 seconds** default timeout
- **60 seconds** for complex operations
- **Cancellation support** for cleanup

### Implementation Example

```csharp
// Automatic retry with exponential backoff
var result = await PollyPolicies.GetGraphApiPolicy(logger)
    .ExecuteAsync(async () => 
    {
        return await graphClient.Devices.GetAsync();
    });
```

## 📢 Multi-Channel Notifications

The multi-channel notification system automatically sends alerts for critical events:

### Supported Channels

#### 📧 **Email Notifications**
- **Native SMTP** with authentication
- **SendGrid and Azure Communication Services** support
- **Customizable HTML templates**
- **Attachment support** for reports

#### 🔔 **Microsoft Teams**
- **Webhook integration** with Office 365
- **Formatted Adaptive Cards**
- **Action buttons** for quick response
- **Threaded conversations** for follow-up

#### 💬 **Slack Integration**
- **Incoming webhooks** with rich formatting
- **Slack attachments** with colors and icons
- **Configurable channel routing**
- **Customizable bot persona**

### Notification Triggers

| Event | Severity | Channels | Example |
|-------|----------|----------|---------|
| **Service Startup** | Info | Teams | "Extension Attributes Worker started successfully" |
| **Health Check Failure** | Warning | Teams, Slack | "Active Directory health check failed" |
| **Multiple Device Failures** | Critical | All Channels | "Failed to process 15+ devices" |
| **Authentication Errors** | Critical | All Channels | "Graph API authentication expired" |
| **Configuration Issues** | Error | Email, Teams | "Invalid extension attribute mapping detected" |

### Configuration Example

```json
{
  "NotificationSettings": {
    "EnableTeamsNotifications": true,
    "TeamsWebhookUrl": "https://outlook.office.com/webhook/abc123...",
    "FailedDevicesThreshold": 10,
    "ConsecutiveFailuresThreshold": 3
  }
}
```

## 🔧 Troubleshooting

### Common Issues

#### 1. **Azure Authentication Failed**
```
Error: Certificate with thumbprint XXX not found
```
**Solution**: Verify that the certificate is installed in the LocalMachine store and that the application has permissions to access it.

#### 2. **Device Not Found in Intune**
```
Warning: No corresponding Intune device found for Entra device: COMPUTER-NAME
```
**Solution**: The device might not be enrolled in Intune or have a different name. Check enrollment status.

#### 3. **Extension Attribute Not Updated**
```
Error: Failed to update extensionAttribute5 for device COMPUTER-NAME
```
**Solution**: Verify Graph API permissions. Required permissions include `Device.ReadWrite.All`.

#### 4. **Graph API Throttling**
```
Warning: Graph API throttled. Retry 3/5 after 8s (Retry-After header)
```
**Solution**: The system handles throttling automatically. Verify that `MaxConcurrentRequests` are not too high.

#### 5. **Health Check Failures**
```
Error: Active Directory health check failed - unable to connect
```
**Solution**: Verify network connectivity, service account credentials, and firewall rules.

### 🆕 Advanced Debug

#### Single Device Debug
```bash
# Debug by device name
ExtensionAttributes.WorkerSvc.exe --device "DESKTOP-ABC123"

# Debug by Device ID
ExtensionAttributes.WorkerSvc.exe --deviceid "abc123-def456-ghi789"

# Start web dashboard for interactive debugging
ExtensionAttributes.WorkerSvc.exe --web
```

#### Configurable Logging
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "UnifiedExtensionAttributeHelper": "Debug",
        "PollyPolicies": "Information",
        "NotificationService": "Debug"
      }
    }
  }
}
```

### Logging and Debug

The service uses **Serilog** for structured logging with multiple destinations:

- **Console**: During console execution with colors
- **File**: `C:\Temp\Automation\RGP.Automation.Worker.log` with rolling
- **Windows Event Log**: When running as service
- **🆕 Structured JSON**: For integration with log analyzers

### Required Permissions

#### Microsoft Graph API
- `Device.Read.All` - Read Entra AD devices
- `Device.ReadWrite.All` - Write Extension Attributes
- `DeviceManagementManagedDevices.Read.All` - Read Intune devices

#### Active Directory
- **Read**: Access to computer objects in specified OU
- **Execute**: Service account with AD access rights

## 🧪 Testing and Debug

### Automated Testing

The system includes various strategies for testing and validation:

#### Unit Testing
```bash
# Run unit tests
dotnet test

# Test with coverage
dotnet test --collect:"XPlat Code Coverage"
```

#### Integration Testing
```bash
# Test health checks
curl http://localhost:5000/health

# Test single device
ExtensionAttributes.WorkerSvc.exe --device TEST-DEVICE

# Test web dashboard
ExtensionAttributes.WorkerSvc.exe --web
```

#### Performance Testing
- **Load testing** with hundreds of devices
- **Memory profiling** for memory leaks
- **Concurrency testing** for race conditions

### Production Monitoring

#### Key Metrics
- **Device Processing Rate**: devices/minute
- **Success Rate**: percentage of success
- **API Response Time**: Graph API latency
- **Health Check Status**: component status

#### Dashboard Recommendations
- **Grafana + Prometheus** for time-series metrics
- **Application Insights** for Azure environments
- **Custom PowerBI** reports for business metrics
- **🆕 Built-in Web Dashboard** for real-time monitoring

## 🤝 Contributing

Contributions are always welcome! To contribute:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/AmazingFeature`)
3. **Commit** your changes (`git commit -m 'Add some AmazingFeature'`)
4. **Push** to the branch (`git push origin feature/AmazingFeature`)  
5. **Open** a Pull Request

### Guidelines

- Follow C#/.NET coding conventions
- Add unit tests for new functionality
- Update documentation if necessary
- Use descriptive commit messages
- **🆕 Include health checks** for new components
- **🆕 Implement retry logic** for remote operations
- **🆕 Add API endpoints** for new features when appropriate

### Development Setup

```bash
# Clone repository
git clone https://github.com/robgrame/ExtensionAttributes.Automation.git

# Setup environment
dotnet restore
dotnet build

# Run tests
dotnet test

# Local execution
cd ExtensionAttributes.Worker
dotnet run -- --console

# Start web dashboard
dotnet run -- --web
```

## 📄 License

This project is distributed under GPL v3 license. See the `LICENSE` file for complete details.

```
Copyright (c) 2025 RGP Bytes
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, version 3 of the License.
```

## 🆘 Support

- **Issues**: [GitHub Issues](https://github.com/robgrame/ExtensionAttributes.Automation/issues)
- **Discussions**: [GitHub Discussions](https://github.com/robgrame/ExtensionAttributes.Automation/discussions)
- **Email**: support@rgpbytes.com
- **🆕 Teams**: Automatic notifications for critical issues
- **🆕 Web Dashboard**: Real-time status monitoring

---

## 📈 Roadmap

### 🎯 Current Version (v1.3)
- ✅ **Active Directory Support** - Complete mappings from AD attributes
- ✅ **Microsoft Intune Support** - Integration with device management
- ✅ **Unified Configuration** - Single config for all mappings
- ✅ **Windows Service** - Scheduled background processing
- ✅ **Quartz.NET Scheduling** - Advanced CRON expressions
- ✅ **🆕 Health Checks** - Monitoring of all components
- ✅ **🆕 Retry Logic** - Resilience with Polly policies  
- ✅ **🆕 Multi-Channel Notifications** - Teams, Slack, Email
- ✅ **🆕 Single Device Processing** - Debug and troubleshooting
- ✅ **🆕 Web Dashboard & REST API** - Interactive monitoring and management

### 🚀 Upcoming Versions

#### **v1.4** - Advanced Analytics
- 🔄 **Advanced Reporting** - Excel, PDF, custom exports
- 🔄 **Analytics Dashboard** - Trends and statistics
- 🔄 **Performance Metrics** - Deep performance insights
- 🔄 **Audit Logging** - Compliance and change tracking

#### **v1.5** - Enterprise Features
- 🔄 **Azure DevOps Integration** - Pipeline automation
- 🔄 **Configuration Management** - Environment-specific configs
- 🔄 **Role-Based Access** - Security and permissions
- 🔄 **Multi-Tenant Support** - Enterprise scalability

#### **v1.6** - Enhanced Web Features
- 🔄 **Configuration Editor** - Web-based mapping management
- 🔄 **Real-time Notifications** - Live updates in dashboard
- 🔄 **Bulk Device Operations** - Process multiple devices
- 🔄 **Historical Data** - Trending and historical analysis

#### **v2.0** - AI & Machine Learning
- 🔄 **Predictive Analytics** - ML-based device insights
- 🔄 **Anomaly Detection** - Automatic issue identification
- 🔄 **Smart Recommendations** - AI-powered optimization
- 🔄 **Natural Language** - Query devices with NLP

### 🎯 Performance Targets (v1.4)

| Metric | Target | Current |
|--------|--------|---------|
| **Device Processing Rate** | 1500 devices/min | 1000 devices/min |
| **API Response Time** | <150ms | <200ms |
| **Health Check Frequency** | 15s | 30s |
| **Success Rate** | >99.5% | >99% |
| **Web Dashboard Load Time** | <2s | <3s |

---

## 🏆 Acknowledgments

Special thanks to:

- **Microsoft Graph Team** - For excellent APIs and documentation
- **Polly Contributors** - For the resilience library
- **Quartz.NET Team** - For robust job scheduling
- **Serilog Community** - For structured logging
- **ASP.NET Core Team** - For the web framework
- **Bootstrap Team** - For the UI framework
- **Open Source Community** - For feedback and contributions

---

**Developed with ❤️, ☕, and lots of patience by [RGP Bytes](https://rgpbytes.com)**

*"Making device management automation reliable, one extension attribute at a time."*
