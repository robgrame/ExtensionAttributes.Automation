@echo off
set INSTALL_DESTINATION=R:\Release\RGP\ExtensionAttributesWorker
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