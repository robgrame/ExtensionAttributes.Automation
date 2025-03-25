# Extension Attributes Automation Worker :rocket:

Have you ever needed to automate the management of computer extension attributes in Entra AD? Look no further! The Extension Attributes Automation Worker Service is here to help you streamline this process.

This service is designed to automate the management of extension attributes in Entra AD, reducing manual effort and potential errors. It leverages the power of .NET Core and widely adopted NuGet packages to provide a robust solution for your automation needs.
This project is part of a larger automation framework that aims to simplify data processing tasks, making it easier for you to manage your Entra AD environment.

## Table of Contents :clipboard:
- [Overview](#overview)
- [Features](#features)

- [Requirements](#requirements)
  - [.NET Core 9.0 or later](#net-core-90-or-later)
  - [Entra AD App Registration](#entra-ad-app-registration)]
- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)
- [Changelog](#changelog)
- [Known Issues](#known-issues)
- [Future Work](#future-work)
- [References](#references)
- [Support](#support)
- [FAQ](#faq)
- [Feedback](#feedback)

## Overview :briefcase:
Entra AD (Active Directory) is a cloud-based identity and access management service from Microsoft. It allows organizations to manage user identities and access to resources securely. Extension attributes are custom attributes that can be added to Entra AD objects, such as users and computers, to store additional information.
These attributes can be used for various purposes, such as storing metadata or integrating with other systems or more simply to store create dynamic groups based on these attributes.

Entra AD Connect is a tool that synchronizes on-premises directories with Entra AD, allowing organizations to manage their identities in a hybrid environment. However, it has limitations when it comes to synchronizing certain attributes, such as computer extension attributes.

Unfortunately, Entra AD Connect does not synchronize computer extension attributes, which can lead to manual effort and potential errors when managing these attributes.
The Extension Attributes Automation Worker Service automates the management of these attributes, ensuring that they are kept up to date and reducing the need for manual intervention.
Values for extension attributes can be set based on existing AD computer object attributes, such as the distinguuished name, location, or other relevant information already present in the AD computer object.

The Extension Attributes Automation Worker Service is a an application designed to work mainly as a Windows service to automate the process of managing computer extension attributes in Entra AD.
This service is part of a larger automation framework that aims to streamline data processing tasks.
This project is built in C# and the .NET Core 9.0 framework leveraging largely adopted NuGet packages like Microsoft.Extensions.Hosting, Quartz.Net and Serilog.

## Features :star:
- The solution can run either as standalone console application, either running as a Windows service, allowing it to operate in the background without user intervention
- It can be configured to run at specified intervals, ensuring that extension attributes are updated regularly leveraging Quartz.Net, a very efficient and largely adopted NuGet package
- Given the nature of the application, it is designed to be run on a Windows server, but it can also be run on any machine that supports .NET Core 9.0
- This solution aims to fill the gap of the Entra AD Connect to synchronize computer extension attributes. To do that it automates the management of extension attributes in Entra AD, leveraging current AD Computer object attributes, reducing manual effort and potential errors.
- The service is designed to be extensible, allowing for future enhancements and additional features.


## Requirements :heavy_check_mark:

To run this solution, you need to have the following prerequisites in place:
- [.NET Core 9.0 or later](#netcore9)
- [Entra AD App Registration](#entra-ad-app-registration)]

### .NET Core 9.0 or later :heavy_check_mark:
To run this service, you need to have .NET Core 9.0 or later installed on your machine. You can download the latest version of .NET Core from the official [.NET website](https://dotnet.microsoft.com/download).
1. Download the installer for your operating system.
1. Run the installer and follow the instructions to install .NET Core.
1. Verify the installation by opening a terminal and running the following command:
    ```sh
    dotnet --version
    ```

   the expected output should be similar to:
   ```sh
   9.0.201
   ```
   If you see a version number, it means .NET Core is installed correctly. If you see an error message, please check the installation instructions again or refer to the [official documentation](https://docs.microsoft.com/en-us/dotnet/core/install/).
   This should display the version of .NET Core installed on your machine.

### Entra AD App Registration :heavy_check_mark:
To use this service, you need to create an Entra AD App Registration with the necessary permissions to manage extension attributes. Follow these steps:
1. Go to the [Azure portal](https://portal.azure.com/).
1. Navigate to "Azure Active Directory" > "App registrations".
1. Click on "New registration".
1. Fill in the required fields:
   1. Name: Enter a name for your app registration (e.g., "ExtensionAttributesAutomationWorker").
   1. Supported account types: Choose the appropriate option based on your organization's needs.
   1. Redirect URI: Leave this blank for now.

   1. Click "Register" to create the app registration.

   1. After the app registration is created, navigate to "Certificates & secrets".
    
   To authenticate the service, you can either create a client secret or upload a certificate. You can choose one of the following methods:

   ##### Create a Client Secret :heavy_check_mark:

   1. Click on "New client secret" to create a new client secret.

   1. Fill in the required fields:
      1. Description: Enter a description for the client secret (e.g., "Worker Service Secret").
      1. Expires: Choose an expiration period for the secret.

      1. Click "Add" to create the client secret.

      1. Copy the value of the client secret and store it securely, as you will need it later.
   
    ##### Upload a Certificate :heavy_check_mark:


    ##### Create a self-signed certificate :heavy_check_mark:
    1. Open PowerShell as an administrator.
    1. Run the following command to create a self-signed certificate:
    ```powershell

    $cert = New-SelfSignedCertificate -DnsName "ExtensionAttributesAutomationWorker" -CertStoreLocation "cert:\LocalMachine\My"
    ```
    This command creates a self-signed certificate with the specified name and stores it in the local machine's certificate store.
    Important to note that the machine holds the private key of the certificate, so you need to export the certificate with the private key to a .pfx file:


    1. Export the certificate to a .cer file:

    ```powershell

    $certPath = "C:\path\to\your\certificate.cer"
    Export-Certificate -Cert $cert -FilePath $certPath
    ```

   1. This command exports the certificate to the specified file path.

   1. Upload the certificate to the Azure portal:

   1. Go back to the Azure portal and navigate to your app registration.

   1. Click on "Certificates & secrets" in the left menu.

   1. Click on "Upload certificate" and select the .cer file you exported earlier.
   1. Click "Add" to upload the certificate.

   #### Grant API Permissions :heavy_check_mark:

      1. Navigate to "API permissions" and click on "Add a permission".

      1. Choose "Microsoft Graph" and select "Application permissions".

      1. Search for the following permissions and select them:
      
         `Device.ReadWrite.All`

         1. Click "Add permissions" to add the selected permissions.

         1. Click on "Grant admin consent for [Your Organization]" to grant the permissions.

         1. Confirm the action when prompted.

    1. Remove any unnecessary permissions to ensure the principle of least privilege is followed.
       1. For example, if you see the following permission, remove it:
        
          `User.Read.All`

       1. Click on the three dots (ellipsis) next to the permission.
       1. Select the permission and click on "Remove permission" to remove it.

    1. Navigate to "Overview" and copy the "Application (client) ID" and "Directory (tenant) ID". You will need these values later.

    1. Store the "Application (client) ID", "Directory (tenant) ID", and the client secret securely, as you will need them to configure the service.


## Usage :hammer_and_wrench:
To use this solution, as already mentioned, you can run it either as a standalone console application or as a Windows service. The service can be configured to run at specified intervals, ensuring that extension attributes are updated regularly.

Before proceeding, make sure you have the following information ready:
- The "Application (client) ID" and "Directory (tenant) ID" from the Entra AD App Registration.
- The client secret or the certificate thumbprint you created earlier.
- The Entra AD tenant ID.
- The extension attribute names you want to manage (e.g., `extensionAttribute1`, `extensionAttribute2`, etc.).
- The values you want to set for the extension attributes based on existing AD computer object attributes.
- The desired interval for the service to run (e.g., every 5 minutes, every hour, etc.).
- The desired logging level (e.g., Information, Warning, Error).
- The desired logging output folder path.
- The desired logging output file name prefix.
- The desired export path for the CSV files keeping the history of the changes made to the extension attributes.
- The desired export file name prefix for the CSV files.


All of the above information can be configured in ad-hoc configuration files located in the project root directory. These files are structured in a way that allows you to easily modify the settings without needing to change the code.
The solution includes three configuration files to manage different aspects of the solution without inadvertently modifying unwanted configuration files:

- `appsettings.json`: Contains the main configuration settings for the service, including Entra AD App Registration details, extension attribute names, etc.
- `logging.json`: Contains the logging configuration settings, including the desired logging level and output folder path.
- `schedule.json`: Contains the scheduling configuration settings, including the desired interval for the service to run, based on Quartz.NET cron expressions.

### appsettings.json :file_folder:
This file contains the main configuration settings for the service. You can modify the values as needed to match your environment and requirements.

- *AppSettings*: Contains the main settings for the service, including:
    - *ExportPath*: The path where the CSV files will be exported. Make sure this path exists and is accessible by the service.
    - *ExportFileNamePrefix*: The prefix for the exported CSV files. The service will append a timestamp to this prefix to create unique file names.
    - *ExtensionAttributeMappings*: An array of mappings between Entra AD extension attributes and existing AD computer object attributes. Each mapping includes:
        - *extensionAttribute*: The name of the extension attribute in Entra AD (e.g., `extensionAttribute1`, `extensionAttribute2`, etc.).
        - *computerAttribute*: The name of the existing AD computer object attribute to use as the source for the extension attribute value (e.g., `distinguishedName`, `company`, etc.).
        - *regex*: A regular expression pattern to extract a specific value from the existing AD computer object attribute. This is optional and can be left empty if not needed.

- *EntraADHelperSettings*: Contains the settings for the Entra AD helper, including:
    - *TokenEndpoint*: The endpoint for obtaining an access token for Entra AD.
    - *TokenExpirationBuffer*: The buffer time in minutes before the token expires.
    - *ClientId*: The client ID of the Entra AD App Registration.
    - *ClientSecret*: The client secret of the Entra AD App Registration. Leave this empty if using a certificate.
    - *TenantId*: The tenant ID of your Entra AD.
    - *UseClientSecret*: Set to `true` if using a client secret, `false` if using a certificate.
    - *CertificateThumbprint*: The thumbprint of the certificate used for authentication. Leave this empty if using a client secret.
    - *AttributesToLoad*: An array of attributes to load from Entra AD. Modify this list based on your requirements.
    - *PageSize*: The number of records to retrieve per page when querying Entra AD.
    - *ClientTimeout*: The timeout value for the Entra AD API requests.
- *ADHelperSettings*: Contains the settings for the AD helper, including:
    - *RootOrganizationaUnitDN*: The distinguished name of the root organizational unit in your AD.
    - *AttributesToLoad*: An array of attributes to load from AD. Modify this list based on your requirements.
    - *PageSize*: The number of records to retrieve per page when querying AD.
    - *ClientTimeout*: The timeout value for the AD API requests.
    - *ExcludedOUs*: An array of organizational units to exclude from processing. Modify this list based on your requirements.


    For complete configuration, you can use the following example as a reference:



 ```json
    {
      "AppSettings": {
        "ExportPath": "C:\\Windows\\Temp\\RGP\\ExtensionAttributes\\Export",
        "ExportFileNamePrefix": "RGP.DevicesProcessed",
        "ExtensionAttributeMappings": [
          {
            "extensionAttribute": "extensionAttribute1",
            "computerAttribute": "distinguishedName",
            "regex": "(?<=OU=)(?<departmentOUName>[^,]+)(?=,OU=(?i:Locations))"
          },
          {
            "extensionAttribute": "extensionAttribute2",
            "computerAttribute": "company",
            "regex": ""
          },
          {
            "extensionAttribute": "extensionAttribute3",
            "computerAttribute": "location",
            "regex": ""
          },
          {
            "extensionAttribute": "extensionAttribute4",
            "computerAttribute": "department",
            "regex": ""
          }
        ]
      },
      "EntraADHelperSettings": {
        "TokenEndpoint": "https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
        "TokenExpirationBuffer": 15,
        "ClientId": "d0cb9f7a-742b-47c0-3212-1f9bcc8322c9",
        "ClientSecret": "",
        "TenantId": "d6dbad84-5922-4700-1234-c7068c37c884",
        "UseClientSecret": false,
        "CertificateThumbprint": "95fc8568eb1c4adc19f339fc75ae52a90bf4efdf",
        "AttributesToLoad": [
          "id",
          "deviceId",
          "accountEnabled",
          "approximateLastSignInDateTime",
          "displayName",
          "trustType",
          "location",
          "department"
        ],
        "PageSize": 1000,
        "ClientTimeout": 60000
      },
      "ADHelperSettings": {
        "RootOrganizationaUnitDN": "OU=Locations,OU=Computers,OU=LAB,DC=msintune,DC=lab",
        "AttributesToLoad": [
          "cn",
          "distinguishedName",
          "operatingSystem",
          "operatingSystemVersion"
        ],
        "PageSize": 1000,
        "ClientTimeout": 30000,
        "ExcludedOUs": [
          "OU=CESENA,OU=Locations,OU=Computers,OU=LAB,DC=msintune,DC=lab",
          "OU=FORLI,OU=Locations,OU=Computers,OU=LAB,DC=msintune,DC=lab"
        ]
      }
    }
   ```
   ### logging.json :file_folder:
   This file contains the logging configuration settings for the service. You can modify the values as needed to match your environment and requirements.
   The logging configuration is based on Serilog, a popular logging library for .NET applications. The configuration includes settings for the logging level, output folder path, and file name prefix.
   - *MinimumLevel*: The minimum logging level for the service. You can set this to `Information`, `Warning`, `Error`, etc., based on your needs.

   - *WriteTo*: The output settings for the logs. In this case, it is configured to write logs to a file.

   - *File*: The file settings for the log output. You can modify the following properties:
     - *Path*: The path where the log files will be saved. Make sure this path exists and is accessible by the service.
     - *FileNamePrefix*: The prefix for the log file names. The service will append a timestamp to this prefix to create unique file names.
     - *RollingInterval*: The interval for rolling over the log files. You can set this to `Day`, `Hour`, etc., based on your needs.
     - *RetainedFileCountLimit*: The maximum number of log files to retain. Older files will be deleted when this limit is reached.

     For convenience we recommend not to change the configuration except for log file path and eventually the FileNamePrefix but leave remainder configuration as is.
     In addition set the `rollingInterval` to `Day` and the `retainedFileCountLimit` to 5, so that you can keep a history of the last 5 days of logs.
     For complete configuration, you can use the following example as a reference:

     ```json
        {
          "Serilog": {
            "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
            "MinimumLevel": {
              "Default": "Debug",
              "Override": {
                "Microsoft": "Warning",
                "System": "Warning",
                "Azure.Identity": "Information",
                "Quartz": "Debug",
                "ADHelper": "Information",
                "EntraADHelper": "Information"

                }
            },
            "Enrich": [ "FromLogContext", "WithMachineName", "WithProcessId", "WithThreadId" ],
            "WriteTo": [
                {
                "Name": "Console",
                "Args": {
                    "theme": "Serilog.Sinks.SystemConsole.Themes.SystemConsoleTheme::Literate, Serilog.Sinks.Console",
                    "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] \t [{SourceContext}] {Message:lj} {NewLine}{Exception}"

                }

                },
                {
                "Name": "File",
                "Args": {
                    "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} \t [{Level}] \t [{SourceContext}] \t {Properties} {Message}{NewLine}{Exception}",
                    "path": "C:\\Temp\\Automation\\RGP.Automation.Worker.log",
                    "encoding": "System.Text.UTF8Encoding", // utf-8, utf-16, utf-32"
                    "rollingInterval": "Day",
                    "rollOnFileSizeLimit": true,
                    "retainedFileCountLimit": 5,
                    "fileSizeLimitBytes": 10485760,
                    "flushToDiskInterval": 1
                }
                }
            ]
            }
        }
     ```

   ### schedule.json :file_folder:
   This file contains the scheduling configuration settings for the service. You can modify the values as needed to match your environment and requirements.
   The scheduling configuration is based on Quartz.NET, a powerful and flexible scheduling library for .NET applications. The configuration includes settings for the desired interval for the service to run, based on Quartz.NET cron expressions.
   - *CronExpression*: The cron expression that defines the schedule for the service. You can modify this expression to set the desired interval for the service to run. For example:
     - `0 0/5 * * * ?` - Every 5 minutes
     - `0 0 12 * * ?` - Every day at noon
     - `0 0 8-18 ? * MON-FRI` - Every hour from 8 AM to 6 PM on weekdays
     For complete configuration, you can use the following example as a reference:
     ```json
        {
          "Quartz": {
            "CronExpression": "0 0/5 * * * ?"
          }
        }
     ```
     For a complete list of cron expressions and their meanings, you can refer to:
     - [Quartz.NET Cron Trigger documentation](https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/crontrigger.html)
     - [CronMaker](https://www.cronmaker.com/) website
     - [Cron Expression Generator & Explainer - Quartz](https://freeformatter.com/cron-expression-generator-quartz.html)
     
     For reference, the complete configuration for the Quartz.NET scheduler can be found in the `schedule.json` file. This includes settings for the Quartz scheduler instance, thread pool, job store, and job details.
        ```json
             {
          "Quartz": {
            "QuartzScheduler": {
              "quartz.scheduler.instanceName": "RGP.ExtensionAttributes.Automation.Worker",
              "quartz.scheduler.instanceId": "AUTO",
              "quartz.threadPool.type": "Quartz.Simpl.SimpleThreadPool, Quartz",
              "quartz.threadPool.threadCount": "10",
              "quartz.threadPool.threadPriority": "Normal",
              "quartz.jobStore.misfireThreshold": "60000",
              "quartz.jobStore.type": "Quartz.Simpl.RAMJobStore, Quartz",
              "quartz.jobStore.clustered": "false"
            },
            "QuartzJobs": [
              {
                "JobName": "SetComputerExtensionAttributeJob",
                "JobDescription": "Set computer ExtensionAttribute to the name of parent OU",
                "JobGroup": "SetComputerExtensionAttributeGroup",
                "JobType": "RGP.Automation.Worker.Jobs.SetComputerExtensionAttributeJob, RGP.Automation.Worker",
                "TriggerName": "SetComputerExtensionAttributeTrigger",
                "TriggerGroup": "SetComputerExtensionAttributeTriggerGroup",
                "CronExpression": "* 0/5 * ? * * *"
              }
            ]
          }
        }
        ```
## Installation :floppy_disk:

### Building the Project :hammer_and_wrench:

1. Clone the repository:
    ```sh
    git clone https://github.com/robgrame/ExtensionAttributes.Worker.git
    ```
1. Download the project from GitHub.
1. Uncompress the downloaded file into a folder.
1. Copy the contents of the folder into a desired location on your machine.
1. Open a terminal and navigate to the project directory.
1. Build the project:

    ```sh
    dotnet build .\ExtensionAttributes.Automation.sln
    ```
1. Upon successful build, navigate to the `bin\Debug\net9.0` directory.
1. You should see a bunch of files including the following files:

    - `ExtensionAttributes.WorkerSvc.exe`
    - `appsettings.json`
    - `logging.json`
    - `schedule.json`

  1. modify the configuration files as needed to match your environment and requirements as described in the [Usage](#usage) section.
  1. Copy the content of the `\bin` folder to the desired location where you want to run the service.

### Running as a Console Application :computer:

To run the solution as a console application, follow these steps:
1. Open a terminal.
1. Navigate to the directory where the binary files are located.
1. Run the following command to start the service:
    ```sh
    .\ExtensionAttributes.WorkerSvc.exe -c
    ```

    1. The application will start running in the console, and you will see log messages indicating its running activity.

    1. To stop the application, press `Ctrl + C` in the console window.

    1. The application will stop running, and you will see log messages indicating its stopping activity.

    1. To run the application in the background, you should use the `-s` option as described in the [Running as a Windows Service](#running-as-a-windows-service) section.


### Running as a Windows Service :white_check_mark:
To run the solution as a Windows service, follow these steps:
1. Open a terminal with administrative privileges.
1. Navigate to the directory where the binary files are located
1. Change current direcory to `\Setup`:
    ```sh
    cd .\Setup
    ```
1. Within `\Setup` folder edit the `Install.cmd` file to set the desired service name and display name. You can also modify the service description and other settings as needed, such as the installation destination and service principal.
      ```sh
        @echo off
        set INSTALL_DESTINATION=%programfiles%\RGP\ExtensionAttributes.Worker
        set SERVICE_NAME=ExtensionAttributesWorkerSvc
        set SERVICE_DISPLAY_NAME=Extension Attributes WorkerSvc
        SET SERVICE_DESCRIPTION=Set Entra AD Device extensionAttributes based on AD Computer attributes
        set PRINCIPAL=LocalSystem
        set EXE_NAME=ExtensionAttributes.WorkerSvc.exe -s

        echo.
        echo 0. Installing service...
        sc.exe create %SERVICE_NAME% binpath= "%INSTALL_DESTINATION%\%EXE_NAME%" obj= %PRINCIPAL% DisplayName= "%SERVICE_DISPLAY_NAME%" start= auto
        sc.exe description %SERVICE_NAME% "%SERVICE_DESCRIPTION%"
        if ERRORLEVEL 1 goto error
        sc.exe failure %SERVICE_NAME% reset=0 actions=restart/60000/restart/60000/run/1000
        net start %SERVICE_NAME%
        echo.
        echo Installation completed and service started.
        exit 0

        :error
        echo Unable to install service. Error code: %ERRORLEVEL%. Make sure to run this script as ADMINISTRATOR. 1>&2
        echo.
        exit 1
        ```
1. When successfully modified `Install.cmd` batch file run the following command to install the service:
    ```sh

    .\Install.cmd
    ```




## Contributing :handshake:
Contributions are welcome! Please follow these steps to contribute:

1. Fork the repository.
2. Create a new branch:
    ```sh
    git checkout -b feature-branch
    ```
3. Make your changes and commit them:
    ```sh
    git commit -m "Description of changes"
    ```
4. Push to the branch:
    ```sh
    git push origin feature-branch
    ```
5. Create a pull request.

## License :card_file_box:
This project is licensed under the GPL. See the GPL license details file for more details.

## Contact :mailbox_with_no_mail:
For any questions or feedback, you can reach me at roberto@gramellini.net

## Changelog :scroll:

- **Version 1.0.0**: Initial release of the Extension Attributes Automation Worker Service.
- **Version 1.0.1**: Added support for running as a Windows service and improved logging capabilities.
- **Version 1.0.2**: Fixed issues with Entra AD token expiration and improved error handling.

## Known Issues :warning:
- The service may not handle all edge cases when processing extension attributes. Please report any issues you encounter.


## Future Work :rocket:
- Add support for additional extension attributes and custom mappings.
- Implement a user interface for easier configuration and management of the service.
- Add support for other platforms (e.g., Linux) to run the service.
- Implement a notification system to alert users of any issues or errors encountered by the service.
- Add support for more advanced scheduling options, such as event-based triggers or custom intervals.
- Implement a backup and restore feature for extension attributes.
- Add support for monitoring and reporting on the status of extension attributes.
- Implement a web-based dashboard for real-time monitoring and management of extension attributes.
- Add support for integration with other automation frameworks or tools.
- Implement a plugin system to allow for custom extensions and functionality.
- Add support for multi-tenancy to allow the service to manage extension attributes across multiple Entra AD tenants.
- Implement a REST API for programmatic access to the service and its functionality.
- Add support for additional authentication methods, such as OAuth2 or OpenID Connect.
- Implement a more robust error handling and retry mechanism for API calls.
- Add support for logging to external systems, such as Azure Monitor or Splunk.
- Implement a configuration management system to allow for dynamic updates to the service configuration without restarting the service.
- Add support for localization and internationalization to allow the service to be used in different languages and regions.
- Implement a testing framework to ensure the service is thoroughly tested and validated before deployment.


## References :books:

- [Microsoft Graph API documentation](https://docs.microsoft.com/en-us/graph/api/overview?view=graph-rest-1.0)
- [Quartz.NET documentation](https://www.quartz-scheduler.net/documentation/quartz-3.x/)
- [Serilog documentation](https://serilog.net/)
- [Microsoft Entra AD documentation](https://docs.microsoft.com/en-us/azure/active-directory/)
- [Microsoft Entra AD Connect documentation](https://docs.microsoft.com/en-us/azure/active-directory/hybrid/deploy/plan-connect)
- [Microsoft Entra AD App Registration documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Microsoft Entra AD extension attributes documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-extensions)
- [Microsoft Entra AD Graph API documentation](https://docs.microsoft.com/en-us/previous-versions/azure/gg982991(v=azure.100))
- [Microsoft Entra AD PowerShell documentation](https://docs.microsoft.com/en-us/powershell/azure/new-azureps-module-az?view=azps-10.13.0)
- [Microsoft Entra AD Graph API Explorer](https://developer.microsoft.com/en-us/graph/graph-explorer)
- [Microsoft Entra AD Graph API SDK for .NET](https://www.nuget.org/packages/Microsoft.Graph/)


## Support :question
If you encounter any issues or have questions about the service, please open an issue on the GitHub repository. We will do our best to address your concerns and provide assistance.

## FAQ :question:
- **Q: What is the purpose of this service?**
  - A: The Extension Attributes Automation Worker Service automates the management of computer extension attributes in Entra AD, reducing manual effort and potential errors.

  - **Q: How does the service work?**

  - A: The service retrieves existing AD computer object attributes and sets the corresponding extension attributes in Entra AD based on the configured mappings.

  - **Q: Can I run the service on any machine?**

  - A: The service is designed to run on Windows machines, but it can also be run on any machine that supports .NET Core 9.0.

  - **Q: How do I configure the service?**

  - A: You can configure the service using the `appsettings.json`, `logging.json`, and `schedule.json` files. Modify the values as needed to match your environment and requirements.

  - **Q: How do I run the service?**

  - A: You can run the service as a console application or as a Windows service. Follow the instructions in the [Usage](#usage) section for more details.

  - **Q: How do I stop the service?**

  - A: If running as a console application, press `Ctrl + C` in the console window. If running as a Windows service, you can stop it using the Services management console or by running the command `net stop ExtensionAttributesWorkerSvc` in a terminal with administrative privileges.

  - **Q: How do I check the logs?**

  - A: The logs are saved in the specified output folder path in the `logging.json` file. You can open the log files using any text editor to view the logs.

  - **Q: How do I contribute to the project?**

  - A: Contributions are welcome! Please follow the instructions in the [Contributing](#contributing) section to submit your changes.

  - **Q: How do I report an issue?**

  - A: If you encounter any issues or have questions about the service, please open an issue on the GitHub repository. We will do our best to address your concerns and provide assistance.

  - **Q: How do I get support?**

  - A: If you need support, please open an issue on the GitHub repository or contact me at roberto@gramellini.net

  
- ## Feedback :speech_balloon:

  We welcome your feedback! If you have any suggestions, comments, or questions about the service, please feel free to reach out to us. Your feedback is valuable and helps us improve the service for everyone.

  Thank you for using the Extension Attributes Automation Worker Service! We hope it helps you automate the management of computer extension attributes in Entra AD and simplifies your workflow.
