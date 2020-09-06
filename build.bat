rd build /s /q
mkdir build
dotnet publish FlightStreamDeck.AddOn\FlightStreamDeck.AddOn.csproj -c Release -r win10-x64 --self-contained
XCopy FlightStreamDeck.AddOn\bin\Release\netcoreapp3.1\win10-x64\publish build\tech.flighttracker.streamdeck.sdPlugin /e /h /c /i
cd build
DistributionTool.exe -b -i tech.flighttracker.rmroc451.streamdeck.sdPlugin -o .
cd ..