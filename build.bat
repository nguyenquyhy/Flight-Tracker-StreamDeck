rd build /s /q
mkdir build
XCopy DistributionTool.exe build\DistributionTool.exe*
dotnet publish FlightStreamDeck.AddOn\FlightStreamDeck.AddOn.csproj -c Release -r win10-x64 --self-contained
XCopy FlightStreamDeck.AddOn\bin\Release\net7.0-windows\win10-x64\publish build\tech.flighttracker.streamdeck.sdPlugin /e /h /c /i
cd build
DistributionTool.exe -b -i tech.flighttracker.streamdeck.sdPlugin -o .
cd ..
pause