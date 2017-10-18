@echo off
.nuget\NuGet.exe restore -PackagesDirectory packages
setlocal
cd fcs

.paket\paket.exe restore
if errorlevel 1 (
  endlocal
  exit /b %errorlevel%
)

packages\FAKE\tools\FAKE.exe build.fsx %*
if errorlevel 1 (
  endlocal
  exit /b %errorlevel%
)
endlocal
exit /b 0
