# ?? **CMTrace Logging Integration Guide**

## ?? **Overview**

Your **Extension Attributes Automation Worker Service** now supports **CMTrace formatting** - the industry standard for enterprise logging used by Microsoft System Center, Configuration Manager, and Intune administrators.

## ? **Features Added**

### **?? Custom CMTrace Formatter**
- ? **Perfect CMTrace Format** - Compatible with CMTrace.exe and OneTrace
- ? **Component-Based Logging** - Each service component has its own identifier
- ? **XML-Safe Content** - Proper escaping of special characters
- ? **Threaded Logging** - Thread ID tracking for concurrent operations
- ? **Source File Tracking** - File and line number information when available

### **?? Dual Logging Support**
- ? **Standard Logs** - Human-readable structured logs
- ? **CMTrace Logs** - Enterprise-compatible format
- ? **Console Output** - Development-friendly colorized output
- ? **Component Separation** - Different components can be filtered/analyzed separately

## ?? **Usage**

### **?? Configuration**

#### **Option 1: appsettings.json Configuration**
```json
{
  "Logging": {
    "CMTrace": {
      "Enabled": true,
      "Path": "C:\\Temp\\Automation\\Logs",
      "ApplicationName": "ExtensionAttributesWorker"
    }
  }
}
```

#### **Option 2: Use Pre-configured Settings**
Copy `appsettings.CMTrace.json` to `appsettings.json` or use environment-specific config:
```cmd
# Use CMTrace configuration
copy appsettings.CMTrace.json appsettings.Production.json
```

### **?? Log Output Locations**

#### **Standard Logs**
```
C:\Temp\Automation\Logs\ExtensionAttributes.Structured.log
```
- Human-readable format
- Good for development and basic troubleshooting

#### **CMTrace Logs**
```
C:\Temp\Automation\Logs\ExtensionAttributes.CMTrace.log
```
- Enterprise CMTrace format
- Perfect for System Center administrators
- Compatible with CMTrace.exe and OneTrace

### **?? Viewing CMTrace Logs**

#### **Option 1: CMTrace.exe (Classic)**
1. Download CMTrace.exe from Microsoft
2. Open: `C:\Temp\Automation\Logs\ExtensionAttributes.CMTrace.log`
3. Filter by component, severity, or time range

#### **Option 2: OneTrace (Modern)**
1. Available with Windows 10/11 or downloadable
2. Advanced filtering and analysis capabilities
3. Real-time log monitoring

#### **Option 3: Visual Studio Code**
Install CMTrace extension for VS Code to view logs with syntax highlighting.

## ?? **Component Organization**

Your logs are organized by component for easy filtering:

| Component | Description | Example Log Entries |
|-----------|-------------|---------------------|
| **IntuneHelper** | Intune device operations | Device retrieval, app inventory, compliance checks |
| **EntraADHelper** | Entra AD operations | Device updates, extension attribute writes |
| **ADHelper** | Active Directory operations | Computer object queries, OU searches |
| **UnifiedExtensionAttributeHelper** | Main processing logic | Device processing workflows, mapping execution |
| **QuartzJob** | Scheduled job execution | Job triggers, completion status |
| **HealthCheck** | System health monitoring | Component health, dependency checks |
| **GraphApiAuth** | Authentication handling | Token acquisition, renewal, errors |

## ?? **Sample CMTrace Log Entries**

### **? Successful Operation**
```xml
<![LOG[Successfully retrieved device details - Name: DESKTOP-ABC123, OS: Windows, Compliance: Compliant]LOG]!><time="14:30:25.123+100" date="01-15-2025" component="IntuneHelper" context="" type="1" thread="1234" file="IntuneHelper.cs:67">
```

### **?? Warning**
```xml
<![LOG[Rate limited for REST API call: /beta/deviceManagement/managedDevices - Status: TooManyRequests, Retry-After: 30s]LOG]!><time="14:30:26.456+100" date="01-15-2025" component="IntuneHelper" context="" type="2" thread="1234" file="IntuneHelper.cs:234">
```

### **? Error**
```xml
<![LOG[Authentication failed for REST API call: /beta/deviceManagement/managedDevices - Status: Unauthorized, Duration: 1250ms]LOG]!><time="14:30:27.789+100" date="01-15-2025" component="IntuneHelper" context="" type="3" thread="1234" file="IntuneHelper.cs:189">
```

## ??? **Log Levels & Types**

| Serilog Level | CMTrace Type | Description | Usage |
|---------------|--------------|-------------|--------|
| **Verbose** | 1 (Info) | Detailed execution flow | Development debugging |
| **Debug** | 1 (Info) | Diagnostic information | Troubleshooting |
| **Information** | 1 (Info) | General operational info | Normal operations |
| **Warning** | 2 (Warning) | Non-critical issues | Attention needed |
| **Error** | 3 (Error) | Error conditions | Immediate attention |
| **Fatal** | 3 (Error) | Critical failures | System failure |

## ?? **Advanced Configuration**

### **Custom Component Logging**
```csharp
// In your service classes
var componentLogger = Log.ForContext("Component", "MyCustomComponent");
componentLogger.Information("Custom component operation completed");
```

### **Performance Monitoring**
```csharp
// Built-in performance tracking in REST API calls
_cmtraceLogger.Information("Graph API REST call succeeded: {Method} {Endpoint} - Status: {StatusCode}, Duration: {Duration}ms", 
    method, endpoint, response.StatusCode, stopwatch.ElapsedMilliseconds);
```

### **Operation Summaries**
```csharp
// Structured operation summaries
LoggingService.LogOperationSummary("ExtensionAttributeProcessing", 150, 147, 3, TimeSpan.FromMinutes(5));
```

## ?? **Benefits for Enterprise Environments**

### **?? System Center Integration**
- **Configuration Manager** - Familiar log format for ConfigMgr admins
- **Intune Administration** - Same log format as Intune management tools
- **SCCM Troubleshooting** - Consistent with existing enterprise tools

### **?? Advanced Troubleshooting**
- **Component Filtering** - Isolate specific service components
- **Time-based Analysis** - Precise timestamp correlation
- **Thread Tracking** - Follow concurrent operations
- **Performance Metrics** - Built-in duration tracking for API calls

### **?? Team Collaboration**
- **Standardized Format** - Same format across all Microsoft enterprise tools
- **Easy Knowledge Transfer** - Familiar to System Center administrators
- **Consistent Terminology** - Same log structure as ConfigMgr/Intune

## ?? **Getting Started**

### **Step 1: Enable CMTrace Logging**
```json
{
  "Logging": {
    "CMTrace": {
      "Enabled": true
    }
  }
}
```

### **Step 2: Run Your Service**
```cmd
ExtensionAttributes.WorkerSvc.exe --service
```

### **Step 3: Open Logs in CMTrace**
1. Download CMTrace.exe
2. Open: `C:\Temp\Automation\Logs\ExtensionAttributes.CMTrace.log`
3. Set up filters for specific components

### **Step 4: Monitor Operations**
- Filter by **IntuneHelper** for device operations
- Filter by **EntraADHelper** for extension attribute updates
- Filter by **Type 3** for errors only
- Filter by time range for specific incidents

## ?? **Result**

Your **Extension Attributes Automation Worker Service** now provides **enterprise-grade logging** that integrates seamlessly with existing Microsoft System Center administration workflows! ???

Perfect for:
- ?? **Enterprise IT Departments**
- ?? **System Center Administrators** 
- ?? **Intune Device Management Teams**
- ?? **DevOps and Monitoring Teams**