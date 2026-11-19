@echo off
rem Copyright (c) 2026 Luke Harrison
rem SPDX-License-Identifier: MIT
rem
rem Builds ClassroomToolkit.exe with the C# compiler that is part of Windows.
rem Nothing needs installing. Double-click this file, or run it from a command prompt:
rem
rem   build.bat           build ClassroomToolkit.exe
rem   build.bat test      build, then build and run the tests in tests\
rem   build.bat release   build, then make release\ClassroomToolkit-<version>.zip
setlocal
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find csc.exe - .NET Framework 4 is missing from this computer.
  if "%1"=="" pause
  exit /b 1
)
set REFS=/r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll
set ICON=/win32icon:assets\app.ico /resource:assets\app.ico,app.ico

"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu %ICON% %REFS% /out:ClassroomToolkit.exe src\*.cs
if errorlevel 1 (
  echo.
  echo Build failed - see the messages above.
  if "%1"=="" pause
  exit /b 1
)
echo Built ClassroomToolkit.exe
if /i "%1"=="test" goto test
if /i "%1"=="release" goto release
pause
exit /b 0

:test
"%CSC%" /nologo /target:exe /platform:anycpu %ICON% %REFS% /main:ClassroomToolkit.Tests /out:tests\Tests.exe src\*.cs tests\Tests.cs
if errorlevel 1 (
  echo Test build failed - see the messages above.
  exit /b 1
)
rem The tests run in portable mode (tests\Data), so your own settings and lists are untouched.
tests\Tests.exe
if errorlevel 1 (
  echo Tests FAILED.
  exit /b 1
)
echo All tests passed.
exit /b 0

:release
for /f "delims=" %%v in ('powershell -NoProfile -Command "(Get-Item 'ClassroomToolkit.exe').VersionInfo.ProductVersion"') do set VERSION=%%v
if exist release rmdir /s /q release
mkdir release\ClassroomToolkit
copy /y ClassroomToolkit.exe release\ClassroomToolkit\ >nul
copy /y docs\package-readme.txt release\ClassroomToolkit\README.txt >nul
copy /y LICENSE release\ClassroomToolkit\LICENSE.txt >nul
powershell -NoProfile -Command "Compress-Archive -Path 'release\ClassroomToolkit\*' -DestinationPath 'release\ClassroomToolkit-%VERSION%.zip' -Force"
echo Made release\ClassroomToolkit-%VERSION%.zip
exit /b 0
