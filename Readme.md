# 🔧 Extension Attributes Automation Worker Service

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

- **.NET 9.0** Runtime/SDK
- **Windows Server 2019** or higher (for AD integration)
- **Active Directory access** (if used)
- **Microsoft Graph API permissions** for Entra AD and Intune
- **Certificate or Client Secret** for Azure authentication

### Quick Installation

1. **Clone the repository:**
```bash
git clone https://github.com/yourusername/ExtensionAttributes.Automation.git
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
        "regex": "(?<=OU=)(?
