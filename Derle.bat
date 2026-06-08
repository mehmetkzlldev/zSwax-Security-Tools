@echo off
chcp 65001 >nul
title Swax Secure Boot - Derleyici
echo.
echo  Swax Secure Boot derleniyor...
echo.

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
  echo  HATA: csc.exe bulunamadi: %CSC%
  pause
  exit /b 1
)

set "ICONOPT="
if exist "app.ico" set "ICONOPT=/win32icon:app.ico"

"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu ^
  /out:"zSwax.exe" /win32manifest:"app.manifest" %ICONOPT% ^
  /reference:System.dll /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll /reference:System.Core.dll ^
  "zSwax.cs"

if errorlevel 1 (
  echo.
  echo  Derleme BASARISIZ.
  pause
  exit /b 1
)

echo.
echo  Tamam: zSwax.exe olusturuldu.
echo.
pause
