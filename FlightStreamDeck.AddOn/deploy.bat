taskkill -f -t -im StreamDeck.exe -fi "status eq running"
taskkill -f -t -im FlightStreamDeck.AddOn.exe -fi "status eq running"
XCOPY "." "%appdata%\Elgato\StreamDeck\Plugins\tech.flighttracker.streamdeck.sdPlugin\" /S /E /Y /F
::START /d "%ProgramW6432%\Elgato\StreamDeck" StreamDeck.exe