# RGP Extension Attributes Automation Worker Service

Una soluzione completa per l'automazione della gestione degli Extension Attributes di Microsoft Entra AD (Azure AD) basata su informazioni provenienti da Active Directory e/o Microsoft Intune.

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

## 📋 Indice

- [Panoramica](#panoramica)
- [Caratteristiche Principali](#caratteristiche-principali)
- [Architettura](#architettura)
- [Installazione](#installazione)
- [Configurazione](#configurazione)
- [Utilizzo](#utilizzo)
- [Esempi di Configurazione](#esempi-di-configurazione)
- [Proprietà Disponibili](#proprietà-disponibili)
- [Risoluzione Problemi](#risoluzione-problemi)
- [Contribuire](#contribuire)
- [Licenza](#licenza)

## 🔍 Panoramica

Il **RGP Extension Attributes Automation Worker Service** è uno strumento potente che automatizza la sincronizzazione degli Extension Attributes di Microsoft Entra AD utilizzando dati provenienti da:

- **Active Directory on-premise** - Attributi dei computer AD
- **Microsoft Intune** - Informazioni hardware, software e compliance dei dispositivi gestiti

La soluzione supporta espressioni regolari per l'estrazione di valori specifici, valori di default e una configurazione unificata che previene collisioni tra le diverse sorgenti dati.

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

### 🔄 **Modalità di Esecuzione**
- **Windows Service**: Esecuzione automatica schedulata in background
- **Console Application**: Esecuzione manuale per test e debug
- **Device-Specific**: Elaborazione di singoli dispositivi (in sviluppo)

### 📅 **Scheduling Flessibile**
- **Quartz.NET Integration**: Scheduling avanzato con espressioni CRON
- **Job Separati**: Possibilità di schedulare AD e Intune indipendentemente
- **Job Unificato**: Processamento combinato di tutte le sorgenti

## 🏗️ Architettura

```
┌─────────────────────────────────────────────────────────────┐
│                    Entra AD (Azure AD)                      │
│                  ┌─────────────────────┐                    │
│                  │  Extension          │                    │
│                  │  Attributes 1-15    │                    │
│                  └─────────────────────┘                    │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              RGP Extension Attributes Worker                 │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │           UnifiedExtensionAttributeHelper               │ │
│  │  ┌─────────────────┐    ┌─────────────────────────────┐ │ │
│  │  │   AD Helper     │    │      Intune Helper          │ │ │
│  │  └─────────────────┘    └─────────────────────────────┘ │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────┬───────────────┬───────────────────────┘
                      │               │
                      ▼               ▼
        ┌─────────────────────┐  ┌─────────────────────┐
        │   Active Directory  │  │  Microsoft Intune   │
        │                     │  │                     │
        │ • Computer Objects  │  │ • Device Info       │
        │ • OU Structure      │  │ • Hardware Details  │
        │ • Attributes        │  │ • Compliance State  │
        └─────────────────────┘  └─────────────────────┘
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
# Esecuzione singola
ExtensionAttributes.WorkerSvc.exe --console

# Mostra help
ExtensionAttributes.WorkerSvc.exe --help

# Per dispositivo specifico (in sviluppo)
ExtensionAttributes.WorkerSvc.exe --device COMPUTER-NAME
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

#### 4. **Regex Non Funzionante**
```
Warning: Regex pattern 'XXX' did not match value 'YYY'
```
**Soluzione**: Testa l'espressione regolare con un tool online come regex101.com.

### Logging e Debug

Il servizio utilizza Serilog per logging strutturato. I log sono disponibili in:

- **Console**: Durante l'esecuzione console
- **File**: `C:\Temp\Automation\RGP.Automation.Worker.log` 
- **Windows Event Log**: Quando eseguito come servizio

Livelli di logging configurabili in `logging.json`:

```json
{
  "MinimumLevel": {
    "Default": "Information",    // Per produzione
    "Override": {
      "UnifiedExtensionAttributeHelper": "Debug"  // Per debug specifico
    }
  }
}
```

### Permessi Richiesti

#### Microsoft Graph API
- `Device.Read.All` - Lettura dispositivi Entra AD  
- `Device.ReadWrite.All` - Scrittura Extension Attributes
- `DeviceManagementManagedDevices.Read.All` - Lettura dispositivi Intune

#### Active Directory
- **Lettura**: Accesso agli oggetti computer nell'OU specificata
- **Esecuzione**: Account di servizio con diritti di accesso AD

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

---

## 📈 Roadmap

### Versione Corrente (v1.1)
- ✅ Supporto Active Directory
- ✅ Supporto Microsoft Intune  
- ✅ Configurazione unificata
- ✅ Windows Service
- ✅ Scheduling con Quartz.NET

### Prossime Versioni
- 🔄 **v1.2**: Elaborazione per singolo dispositivo
- 🔄 **v1.3**: Interfaccia web di gestione
- 🔄 **v1.4**: Supporto Azure DevOps integration
- 🔄 **v1.5**: API REST per integrazione esterna

---

**Sviluppato con ❤️ da [RGP Bytes](https://rgpbytes.com)**
