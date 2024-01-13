## Flight Tracker Stream Deck Plugin

This is a plugin for Elgato Stream Deck to interface with flight simulators via SimConnect.

The current code target x64 and the latest SimConnect from Microsoft Flight Simulator. 
However, if you compile for x86 and reference SimConnect SDK from FSX, this plugin should also work with FSX and P3D.

### For Users

If you just want to use the plugin with your Stream Deck, take a look at [User Guide](docs/USERGUIDE.md).

### For Developers

#### Manual Installation

Copy the output to `%appdata%\Elgato\StreamDeck\Plugins\tech.flighttracker.streamdeck.sdPlugin` and restart Stream Deck software. You can find a deploy.bat file that does the same thing.

Added the Elgato DistributionTool. Build.dat should build the files into the Build Folder, and the tool, now copied into the build folder, creates the plugin. Double click the .streamdeck file to import.
No need to empty the build folder on subsequent builds, it is nuked every time.

### TODO

- [ ] Data list for generic button
- [ ] Sample profile
