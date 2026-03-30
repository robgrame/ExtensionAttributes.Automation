# 🔧 Extension Attributes Automation Worker Service

A complete and highly resilient solution for automating Microsoft Entra AD (Azure AD) Extension Attributes management based on information from Active Directory and/or Microsoft Intune.

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
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
- [Documentation](#documentation)
- [License](#license)

## 🔍 Overview

The **Extension Attributes Automation Worker Service** is a powerful and highly resilient tool that automates the synchronization of Microsoft Entra AD Extension Attributes using data from:

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
│                Extension Attributes Automation Worker                       │
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

- **.NET 10.0** Runtime/SDK (LTS — supported until November 2028)
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
cd Nimbus.ExtensionAttributes.Worker\bin\Release\net10.0-windows
Nimbus.ExtensionAttributes.WorkerSvc.exe --service
```

## ⚙️ Configuration

### Base Configuration

The `appsettings.json` file contains all necessary configurations:

```json
{
  "AppSettings": {
    "ExportPath": "C:\\Temp\\Automation\\Export",
    "ExportFileNamePrefix": "DevicesProcessed",
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
        "regex": "(?<=OU=)(?<value>[^,]+)(?=,OU=(?i:Locations))",
        "defaultValue": "Unknown Department"
      },
      {
        "extensionAttribute": "extensionAttribute5",
        "sourceAttribute": "manufacturer",
        "dataSource": "Intune",
        "useHardwareInfo": true,
        "defaultValue": "Unknown"
      }
    ]
  },
  "EntraADHelperSettings": {
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "UseClientSecret": false,
    "CertificateThumbprint": "<your-cert-thumbprint>",
    "PageSize": 1000
  },
  "ADHelperSettings": {
    "RootOrganizationalUnitDN": "OU=Computers,DC=contoso,DC=com",
    "PageSize": 1000
  }
}
```

## 🧪 Testing and Debug

```bash
# Run all tests
dotnet test

# Run in console mode with dry-run
dotnet run --project Nimbus.ExtensionAttributes.Worker -- --dry-run

# Process a specific device
dotnet run --project Nimbus.ExtensionAttributes.Worker -- --device "WORKSTATION01"

# Run the web dashboard
dotnet run --project Nimbus.ExtensionAttributes.Worker -- --webapp
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📚 Documentation

Detailed guides are available in the [`docs/`](docs/) folder:

| Guide | Description |
|-------|-------------|
| [Architecture Diagram](docs/architecture.md) | Mermaid diagram of the complete solution architecture |
| [Authentication Setup](docs/AUTHENTICATION_SETUP_COMPLETE.md) | Azure AD app registration, certificate and client secret configuration |
| [CMTrace Logging Guide](docs/CMTRACE_LOGGING_GUIDE.md) | SCCM/CMTrace-compatible logging configuration and usage |
| [Implementation Summary](docs/IMPLEMENTATION_SUMMARY.md) | Technical overview of the architecture and implementation details |
| [Service Web Integration](docs/SERVICE_WEB_INTEGRATION_GUIDE.md) | REST API endpoints, SignalR integration, and service hooks |
| [Web Interface Guide](docs/WEB_INTERFACE_GUIDE.md) | Web dashboard setup, features, and usage instructions |

## 📄 License

This project is licensed under the **GNU General Public License v3.0** — see the [LICENSE.txt](LICENSE.txt) file for details.
