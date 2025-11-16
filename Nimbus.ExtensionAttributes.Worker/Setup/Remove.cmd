@echo off
set SERVICE_NAME=ACIWorkerSvc

echo.
echo 0. Installing service...

echo.
echo Service has successfully deleted.
exit 0

:error
echo Unable to remove the service. Error code: %ERRORLEVEL%. Make sure to run this script as ADMINISTRATOR. 1>&2
echo.
exit 1