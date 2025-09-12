# RGP Extension Attributes Automation Worker Service

Una soluzione completa e altamente resiliente per l'automazione della gestione degli Extension Attributes di Microsoft Entra AD (Azure AD) basata su informazioni provenienti da Active Directory e/o Microsoft Intune.

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Health Checks](https://img.shields.io/badge/Health%20Checks-✅-brightgreen.svg)]()
[![Retry Logic](https://img.shields.io/badge/Polly%20Resilience-✅-brightgreen.svg)]()
[![Notifications](https://img.shields.io/badge/Multi--Channel%20Alerts-✅-brightgreen.svg)]()

## 📋 Indice

- [Panoramica](#panoramica)
- [Caratteristiche Principali](#caratteristiche-principali)
- [Architettura](#architettura)
- [Installazione](#installazione)
- [Configurazione](#configurazione)
- [Utilizzo](#utilizzo)
- [Health Checks e Monitoring](#health-checks-e-monitoring)
- [Esempi di Configurazione](#esempi-di-configurazione)
- [Proprietà Disponibili](#proprietà-disponibili)
- [Resilienza e Retry Logic](#resilienza-e-retry-logic)
- [Sistema di Notifiche](#sistema-di-notifiche)
- [Risoluzione Problemi](#risoluzione-problemi)
- [Testing e Debug](#testing-e-debug)
- [Contribuire](#contribuire)
- [Licenza](#licenza)

## 🔍 Panoramica

Il **RGP Extension Attributes Automation Worker Service** è uno strumento potente e altamente resiliente che automatizza la sincronizzazione degli Extension Attributes di Microsoft Entra AD utilizzando dati provenienti da:

- **Active Directory on-premise** - Attributi dei computer AD
- **Microsoft Intune** - Informazioni hardware, software e compliance dei dispositivi gestiti

La soluzione supporta espressioni regolari per l'estrazione di valori specifici, valori di default, una configurazione unificata che previene collisioni, **monitoring completo**, **retry automatico**, e **notifiche multi-canale**.

## ✨ Caratteristiche Principali

### 🎯 **Configurazione Unificata**
- **Una sola sezione di configurazione** per tutti gli Extension Attributes
- **Campo `dataSource`** per specificare se utilizzare Active Directory o Intune
- **Prevenzione automatica delle collisioni** - impossibile configurare lo stesso Extension Attribute con più sorgenti

### 🚀 **Sorgenti Dati Multiple**
- **Active Directory**: Utilizza attributi dei computer AD come OU, company, location, department
- **Microsoft Intune**: Utilizza informazioni dei dispositivi come manufacturer, model, compliance state, storage info

### 🔧 **Elaborazione Avanzata**
- **Espressioni Regolari**: Estrazione di parti specifiche dai valori degli attributi
- **Valori di Default**: Fallback automatico quando i dati non sono disponibili  
- **Elaborazione Concorrente**: Gestione efficiente di migliaia di dispositivi
- **Logging Dettagliato**: Tracciamento completo delle operazioni con Serilog
- **🆕 Elaborazione Dispositivo Singolo**: Debug e test su dispositivi specifici

### 🔄 **Modalità di Esecuzione**
- **Windows Service**: Esecuzione automatica schedulata in background
- **Console Application**: Esecuzione manuale per test e debug
- **🆕 Device-Specific**: Elaborazione di singoli dispositivi per troubleshooting
- **🆕 Device by ID**: Elaborazione tramite Entra AD Device ID

### 📅 **Scheduling Flessibile**
- **Quartz.NET Integration**: Scheduling avanzato con espressioni CRON
- **Job Separati**: Possibilità di schedulare AD e Intune indipendentemente
- **Job Unificato**: Processamento combinato di tutte le sorgenti

### 🩺 **Health Checks e Monitoring**
- **4 Health Checks integrati**: Configuration, Entra AD, Active Directory, Intune
- **Monitoraggio real-time** dello stato dei servizi
- **Metriche dettagliate** per ogni componente
- **Alerting automatico** per problemi critici

### 🔄 **Resilienza e Retry Logic**
- **Polly Integration**: Retry automatico per errori transitori
- **Circuit Breaker**: Prevenzione cascading failures
- **Graph API Throttling**: Gestione intelligente dei rate limits Microsoft
- **Exponential Backoff**: Ottimizzazione delle retry strategies

### 📢 **Sistema di Notifiche Multi-Canale**
- **Microsoft Teams**: Notifiche via webhook con card formattate
- **Slack**: Messaggi strutturati con attachments
- **Email**: Supporto SMTP/SendGrid/Azure Communication Services
- **Alerting Intelligente**: Soglie configurabili per evitare spam

## 🏗️ Architettura

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

## 🚀 Installazione

### Prerequisiti

- **.NET 9.0** Runtime/SDK
- **Windows Server 2019** o superiore (per AD integration)
- **Accesso Active Directory** (se utilizzato)
- **Microsoft Graph API permissions** per Entra AD e Intune
- **Certificato o Client Secret** per autenticazione Azure

### Installazione Rapida

1. **Clona il repository:**
```bash
git clone https://github.com/robgrame/ExtensionAttributes.Automation.git
cd ExtensionAttributes.Automation
```

2. **Compila la soluzione:**
```bash
dotnet build --configuration Release
```

3. **Installa come Windows Service:**
```cmd
cd ExtensionAttributes.Worker\bin\Release\net9.0
ExtensionAttributes.WorkerSvc.exe --service
```

## ⚙️ Configurazione

### Configurazione Base

Il file `appsettings.json` contiene tutte le configurazioni necessarie:

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

### Configurazione Extension Attributes

Ogni mapping è definito con questi parametri:

| Parametro | Tipo | Descrizione | Esempio |
|-----------|------|-------------|---------|
| `extensionAttribute` | string | Extension Attribute di destinazione (1-15) | `"extensionAttribute5"` |
| `sourceAttribute` | string | Attributo sorgente (AD o Intune) | `"manufacturer"`, `"distinguishedName"` |
| `dataSource` | enum | Sorgente dati: `"ActiveDirectory"` o `"Intune"` | `"Intune"` |
| `regex` | string | Espressione regolare per estrazione valori | `"^(\\d+\\.\\d+)"` |
| `defaultValue` | string | Valore di default se attributo vuoto | `"Unknown"` |
| `useHardwareInfo` | boolean | Usa informazioni hardware dettagliate (Intune) | `false` |
| `propertyPath` | string | Percorso per proprietà annidate (futuro uso) | `""` |

### 🆕 Configurazione Notifiche

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

### Abilitazione Sorgenti Dati

```json
{
  "DataSources": {
    "EnableActiveDirectory": true,    // Abilita mappings AD
    "EnableIntune": true,            // Abilita mappings Intune
    "PreferredDataSource": "Both"    // "ActiveDirectory", "Intune", "Both"
  }
}
```

### Autenticazione Azure

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

### Configurazione Active Directory

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

### Configurazione Intune

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

## 🎮 Utilizzo

### Modalità Console (Test e Debug)

```bash
# Esecuzione singola di tutti i dispositivi
ExtensionAttributes.WorkerSvc.exe --console

# Mostra help completo
ExtensionAttributes.WorkerSvc.exe --help

# 🆕 Elaborazione dispositivo specifico per nome
ExtensionAttributes.WorkerSvc.exe --device COMPUTER-NAME

# 🆕 Elaborazione dispositivo specifico per ID Entra AD
ExtensionAttributes.WorkerSvc.exe --deviceid "abc123-def456-ghi789"
```

### Modalità Windows Service

```bash
# Installa e avvia come servizio
ExtensionAttributes.WorkerSvc.exe --service
```

### Scheduling

La configurazione di scheduling è definita in `schedule.json`:

```json
{
  "QuartzJobs": [
    {
      "JobName": "SetUnifiedExtensionAttributeJob",
      "JobDescription": "Elabora Extension Attributes da AD e Intune",
      "CronExpression": "0 0/5 * ? * * *"  // Ogni 5 minuti
    }
  ]
}
```

## 🩺 Health Checks e Monitoring

Il sistema include **4 health checks integrati** che monitorano continuamente lo stato di tutti i componenti:

### Health Checks Disponibili

| Health Check | Descrizione | Verifica |
|--------------|-------------|----------|
| **Configuration** | Valida la configurazione dell'applicazione | Mappings, sorgenti dati, parametri obbligatori |
| **Entra AD** | Testa la connettività Graph API | Autenticazione, permessi, raggiungibilità |
| **Active Directory** | Testa la connessione AD on-premise | Binding LDAP, accesso OU, credenziali |
| **Intune** | Verifica l'accesso ai dispositivi gestiti | Graph API Intune, device management |

### Stati Health Check

- 🟢 **Healthy** - Servizio funzionante correttamente
- 🟡 **Degraded** - Servizio funzionante con avvertimenti
- 🔴 **Unhealthy** - Servizio non funzionante, richiede intervento

### Metriche Incluse

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

## 💡 Esempi di Configurazione

### Esempio 1: Dipartimento da Active Directory OU

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

### Esempio 2: Produttore da Intune

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

### Esempio 3: Versione OS Formattata

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

### Esempio 4: Storage in GB

```json
{
  "extensionAttribute": "extensionAttribute8",
  "sourceAttribute": "totalstoragegb", 
  "dataSource": "Intune",
  "defaultValue": "0"
}
```

**Output**: `256`, `512`, `1024` (GB)

## 📊 Proprietà Disponibili

### Active Directory Properties

| Proprietà | Descrizione | Esempio |
|-----------|-------------|---------|
| `distinguishedName` | DN completo del computer | `CN=PC001,OU=IT,DC=company,DC=com` |
| `company` | Company attribute | `ACME Corporation` |
| `department` | Department attribute | `IT Department` |
| `location` | Location attribute | `Milan, Italy` |
| `description` | Description attribute | `Development Workstation` |

### Intune Device Properties

#### Informazioni Base
| Proprietà | Descrizione | Esempio |
|-----------|-------------|---------|
| `devicename` | Nome dispositivo | `DESKTOP-ABC123` |
| `manufacturer` | Produttore | `Dell Inc.`, `HP`, `Microsoft Corporation` |
| `model` | Modello | `OptiPlex 7090`, `Surface Pro 8` |
| `serialnumber` | Numero seriale | `ABC123DEF456` |

#### Sistema Operativo  
| Proprietà | Descrizione | Esempio |
|-----------|-------------|---------|
| `operatingsystem` | OS | `Windows` |
| `osversion` | Versione OS | `10.0.19045.3570` |

#### Compliance e Gestione
| Proprietà | Descrizione | Esempio |
|-----------|-------------|---------|
| `compliancestate` | Stato compliance | `Compliant`, `NonCompliant`, `Unknown` |
| `manageddeviceownertype` | Tipo ownership | `Corporate`, `Personal` |
| `managementagent` | Agente gestione | `MDM` |

#### Date e Sincronizzazione
| Proprietà | Descrizione | Formato |
|-----------|-------------|---------|
| `lastsyncdate` | Data ultimo sync | `2025-01-01` |
| `lastsynctime` | Ora ultimo sync | `14:30:15` |
| `lastsyncfull` | Data/ora completa | `2025-01-01 14:30:15` |
| `enrolleddate` | Data enrollment | `2024-12-15` |

#### Storage
| Proprietà | Descrizione | Esempio |
|-----------|-------------|---------|
| `totalstorage` | Storage totale formattato | `256.00 GB` |
| `totalstoragegb` | Storage totale in GB | `256` |
| `freestorage` | Storage libero formattato | `128.50 GB` |  
| `freestoragegb` | Storage libero in GB | `128` |

#### Identificatori
| Proprietà | Descrizione | Esempio |
|-----------|-------------|---------|
| `deviceid` | ID dispositivo Intune | `abc123-def456-ghi789` |
| `azureaddeviceid` | ID Entra AD | `xyz789-uvw456-rst123` |
| `userprincipalname` | UPN utente | `user@company.com` |

#### Rete e Telefonia
| Proprietà | Descrizione | Esempio |
|-----------|-------------|---------|
| `phonenumber` | Numero telefono | `+39 123 456 7890` |
| `wifimacaddress` | MAC WiFi | `AA:BB:CC:DD:EE:FF` |
| `imei` | IMEI | `123456789012345` |
| `subscribercarrier` | Operatore | `Vodafone IT` |

## 🔄 Resilienza e Retry Logic

Il sistema utilizza **Polly** per implementare strategie di resilienza avanzate:

### Retry Policies

#### 🔄 **HTTP Retry Policy**
- **3 tentativi** con exponential backoff (2s, 4s, 8s)
- **Gestione errori transienti**: 5XX, 408, 429
- **Logging dettagliato** di ogni tentativo

#### ⚡ **Graph API Throttling Policy**
- **5 tentativi** con jitter per evitare thundering herd
- **Rispetto Retry-After header** Microsoft
- **Gestione intelligente** del rate limiting

#### 🔌 **Circuit Breaker Policy**
- **5 errori consecutivi** aprono il circuito
- **30 secondi** di break duration
- **Half-open testing** per recovery automatico

#### ⏱️ **Timeout Policy**
- **30 secondi** timeout di default
- **60 secondi** per operazioni complesse
- **Cancellation support** per cleanup

### Esempio di Implementazione

```csharp
// Retry automatico con exponential backoff
var result = await PollyPolicies.GetGraphApiPolicy(logger)
    .ExecuteAsync(async () => 
    {
        return await graphClient.Devices.GetAsync();
    });
```

## 📢 Sistema di Notifiche

Il sistema di notifiche multi-canale invia automaticamente alert per eventi critici:

### Canali Supportati

#### 📧 **Email Notifications**
- **SMTP nativo** con autenticazione
- **Supporto SendGrid** e Azure Communication Services
- **Template HTML** personalizzabili
- **Attachment support** per report

#### 🔔 **Microsoft Teams**
- **Webhook integration** con Office 365
- **Adaptive Cards** formattate
- **Action buttons** per quick response
- **Threaded conversations** per follow-up

#### 💬 **Slack Integration**
- **Incoming webhooks** con rich formatting
- **Slack attachments** con colori e icone
- **Channel routing** configurabile
- **Bot persona** personalizzabile

### Trigger di Notifica

| Evento | Severità | Canali | Esempio |
|--------|----------|--------|---------|
| **Service Startup** | Info | Teams | "Extension Attributes Worker started successfully" |
| **Health Check Failure** | Warning | Teams, Slack | "Active Directory health check failed" |
| **Multiple Device Failures** | Critical | All Channels | "Failed to process 15+ devices" |
| **Authentication Errors** | Critical | All Channels | "Graph API authentication expired" |
| **Configuration Issues** | Error | Email, Teams | "Invalid extension attribute mapping detected" |

### Esempio di Configurazione

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

## 🔧 Risoluzione Problemi

### Problemi Comuni

#### 1. **Autenticazione Azure Fallita**
```
Error: Certificate with thumbprint XXX not found
```
**Soluzione**: Verifica che il certificato sia installato nel LocalMachine store e che l'applicazione abbia i permessi per accedervi.

#### 2. **Dispositivo Non Trovato in Intune**
```
Warning: No corresponding Intune device found for Entra device: COMPUTER-NAME
```
**Soluzione**: Il dispositivo potrebbe non essere enrollato in Intune o avere un nome diverso. Verifica lo stato di enrollment.

#### 3. **Extension Attribute Non Aggiornato**
```
Error: Failed to update extensionAttribute5 for device COMPUTER-NAME
```
**Soluzione**: Verifica i permessi Graph API. Sono necessari i permessi `Device.ReadWrite.All`.

#### 4. **Graph API Throttling**
```
Warning: Graph API throttled. Retry 3/5 after 8s (Retry-After header)
```
**Soluzione**: Il sistema gestisce automaticamente il throttling. Verifica che i `MaxConcurrentRequests` non siano troppo alti.

#### 5. **Health Check Failures**
```
Error: Active Directory health check failed - unable to connect
```
**Soluzione**: Verifica connettività di rete, credenziali del service account, e firewall rules.

### 🆕 Debug Avanzato

#### Debug Singolo Dispositivo
```bash
# Debug per nome dispositivo
ExtensionAttributes.WorkerSvc.exe --device "DESKTOP-ABC123"

# Debug per Device ID
ExtensionAttributes.WorkerSvc.exe --deviceid "abc123-def456-ghi789"
```

#### Logging Configurabile
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

### Logging e Debug

Il servizio utilizza **Serilog** per logging strutturato con multiple destinazioni:

- **Console**: Durante l'esecuzione console con colori
- **File**: `C:\Temp\Automation\RGP.Automation.Worker.log` con rolling
- **Windows Event Log**: Per esecuzione come servizio
- **🆕 Structured JSON**: Per integrazione con log analyzers

### Permessi Richiesti

#### Microsoft Graph API
- `Device.Read.All` - Lettura dispositivi Entra AD  
- `Device.ReadWrite.All` - Scrittura Extension Attributes
- `DeviceManagementManagedDevices.Read.All` - Lettura dispositivi Intune

#### Active Directory
- **Lettura**: Accesso agli oggetti computer nell'OU specificata
- **Esecuzione**: Account di servizio con diritti di accesso AD

## 🧪 Testing e Debug

### Test Automatici

Il sistema include diverse strategie per testing e validazione:

#### Unit Testing
```bash
# Esecuzione test unitari
dotnet test

# Test con coverage
dotnet test --collect:"XPlat Code Coverage"
```

#### Integration Testing
```bash
# Test health checks
curl http://localhost:5000/health

# Test singolo dispositivo
ExtensionAttributes.WorkerSvc.exe --device TEST-DEVICE
```

#### Performance Testing
- **Load testing** con centinaia di dispositivi
- **Memory profiling** per memory leaks
- **Concurrency testing** per race conditions

### Monitoring in Produzione

#### Metriche Chiave
- **Device Processing Rate**: dispositivi/minuto
- **Success Rate**: percentuale di successo
- **API Response Time**: latenza Graph API
- **Health Check Status**: stato dei componenti

#### Dashboard Recommendations
- **Grafana + Prometheus** per metriche time-series
- **Application Insights** per Azure environments
- **Custom PowerBI** reports per business metrics

## 🤝 Contribuire

Contributi sono sempre benvenuti! Per contribuire:

1. **Fork** il repository
2. **Crea** un branch per la tua feature (`git checkout -b feature/AmazingFeature`)
3. **Commit** le modifiche (`git commit -m 'Add some AmazingFeature'`)
4. **Push** al branch (`git push origin feature/AmazingFeature`)  
5. **Apri** una Pull Request

### Linee Guida

- Segui le convenzioni di coding C#/.NET
- Aggiungi test unitari per nuove funzionalità
- Aggiorna la documentazione se necessario
- Usa messaggi di commit descrittivi
- **🆕 Includi health checks** per nuovi componenti
- **🆕 Implementa retry logic** per operazioni remote

### Development Setup

```bash
# Clone del repository
git clone https://github.com/robgrame/ExtensionAttributes.Automation.git

# Setup environment
dotnet restore
dotnet build

# Esecuzione test
dotnet test

# Esecuzione locale
cd ExtensionAttributes.Worker
dotnet run -- --console
```

## 📄 Licenza

Questo progetto è distribuito sotto licenza GPL v3. Vedi il file `LICENSE` per dettagli completi.

```
Copyright (c) 2025 RGP Bytes
Questo programma è software libero: puoi redistribuirlo e/o modificarlo
sotto i termini della GNU General Public License come pubblicata dalla
Free Software Foundation, versione 3 della Licenza.
```

## 🆘 Supporto

- **Issues**: [GitHub Issues](https://github.com/robgrame/ExtensionAttributes.Automation/issues)
- **Discussions**: [GitHub Discussions](https://github.com/robgrame/ExtensionAttributes.Automation/discussions)
- **Email**: support@rgpbytes.com
- **🆕 Teams**: Notifiche automatiche per problemi critici
- **🆕 Health Dashboard**: Monitoring real-time status

---

## 📈 Roadmap

### 🎯 Versione Corrente (v1.2)
- ✅ **Supporto Active Directory** - Mappings completi da attributi AD
- ✅ **Supporto Microsoft Intune** - Integration con device management
- ✅ **Configurazione unificata** - Single config per tutti i mappings
- ✅ **Windows Service** - Background processing schedulato
- ✅ **Scheduling con Quartz.NET** - CRON expressions avanzate
- ✅ **🆕 Health Checks** - Monitoring di tutti i componenti
- ✅ **🆕 Retry Logic** - Resilienza con Polly policies  
- ✅ **🆕 Multi-Channel Notifications** - Teams, Slack, Email
- ✅ **🆕 Single Device Processing** - Debug e troubleshooting

### 🚀 Prossime Versioni

#### **v1.3** - Web Dashboard & API
- 🔄 **Web Dashboard** - Interfaccia monitoring real-time
- 🔄 **REST API** - Endpoints per integrazione esterna
- 🔄 **Health Check UI** - Dashboard health status
- 🔄 **Configuration UI** - Web-based configuration management

#### **v1.4** - Advanced Analytics
- 🔄 **Advanced Reporting** - Excel, PDF, custom exports
- 🔄 **Analytics Dashboard** - Trends e statistiche
- 🔄 **Performance Metrics** - Deep insights sui performance
- 🔄 **Audit Logging** - Compliance e change tracking

#### **v1.5** - Enterprise Features
- 🔄 **Azure DevOps Integration** - Pipeline automation
- 🔄 **Configuration Management** - Environment-specific configs
- 🔄 **Role-Based Access** - Security e permissions
- 🔄 **Multi-Tenant Support** - Enterprise scalability

#### **v2.0** - AI & Machine Learning
- 🔄 **Predictive Analytics** - ML-based device insights
- 🔄 **Anomaly Detection** - Automatic issue identification
- 🔄 **Smart Recommendations** - AI-powered optimization
- 🔄 **Natural Language** - Query devices with NLP

### 🎯 Performance Targets (v1.3)

| Metrica | Target | Attuale |
|---------|--------|---------|
| **Device Processing Rate** | 1000 devices/min | 500 devices/min |
| **API Response Time** | <200ms | <300ms |
| **Health Check Frequency** | 30s | 60s |
| **Success Rate** | >99% | >95% |
| **Retry Success Rate** | >90% | >85% |

---

## 🏆 Riconoscimenti

Ringraziamenti speciali a:

- **Microsoft Graph Team** - Per le eccellenti API e documentazione
- **Polly Contributors** - Per la libreria di resilienza
- **Quartz.NET Team** - Per il robust job scheduling
- **Serilog Community** - Per il structured logging
- **Open Source Community** - Per feedback e contributi

---

**Sviluppato con ❤️, ☕, e tanta pazienza da [RGP Bytes](https://rgpbytes.com)**

*"Making device management automation reliable, one extension attribute at a time."*
