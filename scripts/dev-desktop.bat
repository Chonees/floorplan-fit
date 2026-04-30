@echo off
setlocal

cd /d "%~dp0.."

title Floorplan Fit Desktop Dev

echo.
echo ==============================================
echo   Floorplan Fit - Desktop Dev Watch Launcher
echo ==============================================
echo.
echo Repo: %CD%
echo Project: src\FloorplanFit.Desktop\FloorplanFit.Desktop.csproj
echo.
echo This launcher runs the Desktop app from source using dotnet watch,
echo so you are always working against the latest code.
echo.

tasklist /FI "IMAGENAME eq FloorplanFit.Desktop.exe" | find /I "FloorplanFit.Desktop.exe" >nul
if not errorlevel 1 (
  echo WARNING: FloorplanFit.Desktop is already running.
  echo If dotnet watch cannot rebuild because DLLs are locked,
  echo close the existing app instance and run this launcher again.
  echo.
)

echo Press Ctrl+C to stop.
echo.

dotnet watch --non-interactive %* run --project "src\FloorplanFit.Desktop\FloorplanFit.Desktop.csproj"
