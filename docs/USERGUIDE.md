## User Guide for Flight Tracker Stream Deck Plugin

### Installation

1. Download latest version from https://github.com/nguyenquyhy/Flight-Tracker-StreamDeck/releases
1. Double click the file `tech.flighttracker.streamdeck.streamDeckPlugin` to install
   - If you manually created the folder `tech.flighttracker.streamdeck.sdPlugin` previously in `%appdata%\Elgato\StreamDeck\Plugins`, there will be an error saying "This custom action is already installed." although you won't see it in More Actions list. In this case you will need to manually remove that folder before trying again.
1. Accept the prompt to install the plugin and the profiles. The profile is a special numpad, allowing you to enter COM/NAV frequency.
1. You should now see Flight Tracker group in your list of buttons.

### Usage

#### Notes

At the moment, there are 2 groups of buttons:

| Group | Pro | Con |
|-------|-----|-----|
| **Generic** | Potentially work with any SimConnect variables of the correct type | Need to look up the variable's and event's name |
| **Preset** | Specifically designed for certain variables, so setting up is just drag-and-drop and select its function<br />Has special functionality such as holding COM/NAV button to open numpad | Require development effort to add more buttons with similar functionality |

We will maintain and improve both groups to facilitate the widest audience with different levels of experience in flight simming.

#### Buttons

##### Generic Toggle Button

![Sample NAV/COM buttons](sample_generic.png)

This is the most powerful button in the plugin. It can trigger a SimConnect event, or show a SimConnect variable or both at the same time.

However, with great power comes great responsibility: you have to do a bit of setting up for the parameters.

| Parameter | Description | Example |
|-----------|-------------|---------|
| Title | This is the built-in title of any Stream Deck button. We hide this by default. You should consider using the next parameter instead. | *Empty* |
| Header | This is similar to title but with a pre-defined font, size and position that looks nice on the button. | HDG |
| Toggle event | The SimConnect event that triggers when the button is tapped on.<br />You can find a list of tested event IDs in [FlightStreamDeck.Logics/KnownEvents.cs](/FlightStreamDeck.Logics/KnownEvents.cs), but you are not limited to that list. Any valid SimConnect event ID should work. Explanation of each ID can be found in [SDK Documentation/Event IDs](https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Event_IDs.htm). | AP_PANEL_HEADING_HOLD |
| Toggle parameter | The parameter to pass along with the event. This can be a number or a SimConnect variable. If using a variable, the value of the variable will be passed to the event. | 1 <BR/> PLANE HEADING DEGREES MAGNETIC |
| Hold event & parameter | Similar to Toggle event & parameter, but will trigger when you hold the button. | |
| Feedback value | <p>The SimConnect variable that indicates if the button is *active* or not. Active state will show a green light or a green number (if `Display value` below is set) on the button.</p><p><em>Please read below this table for information on supporting variables.</em></p><p>You can also use some comparison operators such as "==", "!=", ">", "<", ">=", "<=" between a variable and a value or between 2 variables.</p> | AUTOPILOT HEADING LOCK<br />FLAPS HANDLE INDEX==2 |
| Display value | <p>The SimConnect variable (any numeric unit) to display as a number below the header.</p><p><em>Please read below this table for information on supporting variables.</em></p>  | AUTOPILOT HEADING LOCK DIR |

**Supporting Variables**

Right now, the plugin allows only variables inside this list [FlightStreamDeck.Logics/KnownVariables.cs](/FlightStreamDeck.Logics/KnownVariables.cs). Explanation for each variable can be found in [SDK Documentation/Simulation Variables](https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm).

From Sim Update 12, you can also use L-Var in Feedback value and Display value without any restriction. L-Var must be prefixed with `L:` (e.g. `L:A32NX_OVHD_ELEC_BAT_1_PB_IS_AUTO`).

---

This button allows you to choose custom button images for active and inactive states. Please check out Custom images section below for more details.

Example generic toggle button that displays the current value of the autopilot heading bug, and syncs the heading bug with the aircraft's current heading when pushed:

| Parameter | Value |
|-----------|-------|
| Title | *Empty* |
| Header | HDG |
| Toggle event | HEADING_BUG_SET |
| Toggle parameter | PLANE HEADING DEGREES MAGNETIC |
| Feedback value | *Empty* |
| Display value | AUTOPILOT HEADING LOCK DIR |

##### Preset Toggle Button

This button toggles some preset functions without much setting up.

![Sample NAV/COM buttons](sample_preset.png)

| Function Parameter | Display | Tap | Hold |
|--------------------|---------|-----|------|
| Avionics Master | Avionics Master status | Toggle Avionics Master | |
| AP Master | AP Master status | Toggle AP Master | |
| AP Heading | AP Heading status & value | Toggle AP Heading | Sync current heading |
| AP Nav | AP Nav status | Toggle AP Nav | |
| AP Altitude | AP Altitude status | Toggle AP Altitude | |
| AP V/S | AP V/S status | Toggle AP V/S| |
| AP Approach | AP Approach status | Toggle AP Approach | |

This button allows you to choose custom button images for active and inactive states. 
The image should be of size 72x72 pixel (or 144x144 for higher res decks) and should be in PNG format.

##### Preset Increase/Decrease Button

This button allows you to increase/decrease certain values in the sim.

![Sample NAV/COM buttons](sample_incdec.png)

| Function Parameter | Tap | Hold |
|--------------------|-----|------|
| AP Heading | Increase/Decrease by 1 | Increase/Decrease by 10
| AP Altitude | Increase/Decrease by 100 | Increase/Decrease by 1000
| AP V/S | Increase/Decrease by 100 | Increase/Decrease by 100

##### NAV & COM

This button shows active & standby frequency, allows you to swap standby frequency and enter the frequency with a numpad.

![Sample NAV/COM buttons](sample_comnav.png)

Interactions:
- Tap: Swap frequencies
- Hold: Show numpad to enter frequency. 
  - This switches your Stream Deck to a Numpad profile that is bundled with the plugin. If the profile is not installed yet, Stream Deck will ask you to install the profile when the plugin is installed or when you hold this button.
  - This does not work on the Stream Deck Mini due to the limited number of buttons.

##### Generic Gauge

This button is very similar to Generic Toggle Button except from it does not have an active state, hence no `Feedback value` parameter is needed.

![Sample NAV/COM buttons](sample_custom_gauge.png)

There are 2 types of generic gauge at the moment: Simple and Custom. Simple allows you to display a value in an arc gauge while Custom allows you to display up to 2 values with customizable colors.

###### Common Parameters

- `Minimum` and `Maximum` indicate the range of the `Display value`. You can input a number or a SimConnect variable in those fields.

###### Notes for Simple Gauge

- `Sub value` shows a small number below the main display value.  In my example for Indicated Altitude, the inches of MG displayed by adding `KOHLSMAN_SETTING_HG` to this setting.

###### Notes for Custom Gauge

- Having a `Minimum` greater than the `Maximum value` will flip the way the value resolves on the graph (see two trim gauges above).
- Setting `Display absolute value` to 'Yes' will display the value without a negative sign (e.g. right column trim gauges).
- The gauge may be cusom color coded, and knows basic colors from [System.Drawing.Colors](https://docs.microsoft.com/en-us/dotnet/api/system.drawing.color?view=net-5.0#properties).
  - The default custom gauge that displays is color coded like the fuel gauge and has all the properties in it for a generic 2 tank aircraft.
- `Chevron Size` and `Thickness` allows you to further customize the display.
- If you do not input `Header` or `Bottom Header`, the respective value and chevron won't show up.  If you want a blank chevron, simply put a blank space in header fields.

#### Custom Images

Custom active/inactive images should be of size 72x72 pixel (or 144x144 for Stream Deck XL) and should be in PNG format.
There are 2 ways to use custom images in the plugins.

| Embed | Link |
|-------|------|
| The whole image is stored in the profile. | The path to the image is stored in the profile. |
| Profile export with custom images works on any PCs. | The importing PCs need to have the custom images in the same folder. |
| Needs to have the original image or to change back to Link mode to edit | Is easier to edit the image and immediately see the change |
| Profile is heavier and might reach some limitation of Stream Deck. | Profile is very light. |

The plugin also allows you to switch between the 2 modes by clicking on the other mode and click OK on the prompt.

### MobiFlight WASM Module Integration

[MobiFlight](https://www.mobiflight.com/en/download.html) has put some awesome work together to allow MSFS users to have access to some events that we haven't gotten in the SDK yet!  One of the most requested things yet has been GPS/G1000/ETC integration with the streamdeck plugin, but the SDK has been lagging behind.

As this section is a bit more advanced, I have split the instruction into a separate guide [here](MOBIFLIGHT.md).

Go show MobiFlight some love over in their [forum post](https://forums.flightsimulator.com/t/simconnect-and-gps-event-ids/308990/16). Tell them the folks from Flight-Tracker-StreamDeck <3 their work!

### Known Issues

- If you spam the same buttons too quickly, SimConnect will get error and stop responding to any further command. The plugin will try to automatically reconnect. 
  - However, if you see the message "Connected to Flight Simulator" flashing constantly in the sim, the plugin might be in a infinitely retry loop. In this case, close Stream Deck software on your PC (which will kill the plugin), wait a couple of seconds for all the SimConnect connections to close, and re-open Stream Deck.
- When you setup new generic buttons or COM/NAV button, the registration between the plugin and SimConnect might get messed up and the plugin stops receiving data. In this case, you'll need to restart Stream Deck software.
- If your gauge looks delayed or the dial feels laggy (take a few seconds to register the change), you have most likely plugged the Stream Deck into a USB 2.0 port or some slow speed hub. Make sure the connection between the Stream Deck and the PC is USB 3.0 or higher.