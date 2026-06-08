@echo off
chcp 65001 >nul
title zSwax - Defender Izin (Exclusion)

:: Yonetici degilse kendini yukselt
net session >nul 2>&1
if %errorlevel% neq 0 (
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

set "DIR=%~dp0"
if "%DIR:~-1%"=="\" set "DIR=%DIR:~0,-1%"

echo.
echo  Bu klasor Windows Defender taramasindan haric tutulacak (exclusion):
echo    %DIR%
echo.
echo  Boylece zSwax.exe yanlis pozitif yuzunden silinmez.
echo.
echo  NOT: Kurcalama Korumasi (Tamper Protection) ACIKSA bu islem ENGELLENIR.
echo       O durumda once Windows Guvenligi'nden Tamper'i kapatman gerekir,
echo       ya da Windows Guvenligi ^> Koruma gecmisi ^> Izin ver kullan.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command "$d='%DIR%'; try { Add-MpPreference -ExclusionPath $d -ErrorAction Stop } catch {}; $ex=(Get-MpPreference).ExclusionPath; if ($ex ^| Where-Object { $_.TrimEnd([char]92) -ieq $d }) { Write-Host '  [OK] Izin eklendi. zSwax.exe artik silinmeyecek.' -ForegroundColor Green } else { Write-Host '  [!] Eklenemedi (muhtemelen Tamper acik). Yukaridaki notu uygula.' -ForegroundColor Yellow }"

echo.
pause
