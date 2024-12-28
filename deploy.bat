@echo off
taskkill -f -t -im StreamDeck.exe -fi "status eq running"
timeout 2
rd build /s /q
mkdir build
dotnet publish FlightStreamDeck.AddOn\FlightStreamDeck.AddOn.csproj -c Release -r win-x64 --self-contained
if %errorlevel% neq 0 exit /b %errorlevel%
XCopy FlightStreamDeck.AddOn\bin\Release\net9.0-windows\win-x64\publish build\tech.flighttracker.streamdeck.sdPlugin /e /h /c /i
if %errorlevel% neq 0 exit /b %errorlevel%

XCopy build\tech.flighttracker.streamdeck.sdPlugin "%appdata%\Elgato\StreamDeck\Plugins\tech.flighttracker.streamdeck.sdPlugin" /e /h /c /i /y
if %errorlevel% neq 0 exit /b %errorlevel%

START /d "%ProgramW6432%\Elgato\StreamDeck" StreamDeck.exe
