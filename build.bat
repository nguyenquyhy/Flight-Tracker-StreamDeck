rd build /s /q
mkdir build
dotnet publish FlightStreamDeck.AddOn\FlightStreamDeck.AddOn.csproj -c Release -r win-x64 --self-contained
XCopy FlightStreamDeck.AddOn\bin\Release\net9.0-windows\win-x64\publish build\tech.flighttracker.streamdeck.sdPlugin /e /h /c /i
cd build
DistributionTool.exe -b -i tech.flighttracker.streamdeck.sdPlugin -o .
cd ..