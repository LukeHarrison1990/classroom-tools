@echo off
rem Builds ClassroomToolkit.exe with the C# compiler that is part of Windows.
rem Nothing needs installing. Double-click this file, or run it from a command prompt.
rem   build.bat           build ClassroomToolkit.exe
rem   build.bat release   also make release\ClassroomToolkit-<version>.zip
setlocal
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find csc.exe - .NET Framework 4 is missing from this computer.
  if "%1"=="" pause
  exit /b 1
)
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:app.ico ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll ^
  /out:ClassroomToolkit.exe src\*.cs
if errorlevel 1 (
  echo.
  echo Build failed - see the messages above.
  if "%1"=="" pause
  exit /b 1
)
echo Built ClassroomToolkit.exe
if /i "%1"=="release" goto release
pause
exit /b 0

:release
for /f "delims=" %%v in ('powershell -NoProfile -Command "(Get-Item 'ClassroomToolkit.exe').VersionInfo.ProductVersion"') do set VERSION=%%v
if exist release rmdir /s /q release
mkdir release\ClassroomToolkit
copy /y ClassroomToolkit.exe release\ClassroomToolkit\ >nul
copy /y docs\README.txt release\ClassroomToolkit\README.txt >nul
copy /y LICENSE release\ClassroomToolkit\LICENSE.txt >nul
powershell -NoProfile -Command "Compress-Archive -Path 'release\ClassroomToolkit\*' -DestinationPath 'release\ClassroomToolkit-%VERSION%.zip' -Force"
echo Made release\ClassroomToolkit-%VERSION%.zip
exit /b 0
