taskkill -f -t -im StreamDeck.exe -fi "status eq running"
XCOPY "." "%appdata%\Elgato\StreamDeck\Plugins\tech.flighttracker.streamdeck.sdPlugin\" /S /E /Y
START /d "%ProgramW6432%\Elgato\StreamDeck" StreamDeck.exe