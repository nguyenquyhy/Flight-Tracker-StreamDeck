## User Guide for Flight Tracker Stream Deck Plugin

### Installation

1. Download latest version from https://github.com/nguyenquyhy/Flight-Tracker-StreamDeck/releases
1. Double click the file `tech.flighttracker.streamdeck.streamDeckPlugin` to install
   - If you manually created the folder `tech.flighttracker.streamdeck.sdPlugin` previously in `%appdata%\Elgato\StreamDeck\Plugins`, there will be an error saying "This custom action is already installed." although you won't see it in More Actions list. In this case you will need to manually remove that folder before trying again.
1. Accept the prompt to install the plugin and the profiles. The profile is a hidden numpad, allowing you to enter COM/NAV frequency.
1. You should now see Flight Tracker group in your list of button.

### Usage

#### Notes

At the moment, there are 2 groups of buttons:

| Group | Pro | Con |
|-------|-----|-----|
| **Generic** | Potentially work with any SimConnect variables of the correct type | Need to look up the variable's and event's name |
| **Prebuilt** | Specifically designed for certain variables, so setting up is mostly just drag-and-drop<br />Has special functionality such as holding COM/NAV button to open numpad | Large number of buttons in the button list<br />Require development effort to add more buttons with similar functionality |

We are looking at combining the 2 groups to simplify the experience. This might lead to the removal of some existing buttons, but there will certainly be replacement, and hopefully it won't take too much time to setup those buttons again.

#### Buttons

##### Generic Toggle Button

This is the most powerful button in the plugin. However, with great power comes great responsibility: you have to do a bit of setting up for the parameters:

| Parameter | Description | Example |
|-----------|-------------|---------|
| Title | This is the built-in title of any Stream Deck button. We hide this by default. You should consider using the next parameter instead. | *Empty* |
| Header | This is similar to title but with a pre-defined font, size and position that looks nice on the button. | HDG |
| Toggle value | The event that get triggered when the button is clicked. <br />You can find the event ID from http://www.prepar3d.com/SDKv2/LearningCenter/utilities/variables/event_ids.html or MSFS SDK docs. | KEY_AP_PANEL_HEADING_HOLD |
| Feedback value | The sim variable (Bool unit) that indicate if the button is *active* or not. Active state will show a green light on the button if `Display value` below is not set, or a green number otherwise.<br />You can find the variables at http://www.prepar3d.com/SDKv2/LearningCenter/utilities/variables/simulation_variables.html or MSFS SDK docs. | AUTOPILOT HEADING LOCK |
| Display value | The sim variable (any numeric unit) to display as a number below header.<br />You can find the variables at http://www.prepar3d.com/SDKv2/LearningCenter/utilities/variables/simulation_variables.html or MSFS SDK docs.  | AUTOPILOT HEADING LOCK DIR |

##### Generic Gauge

This button is very similar to Generic Toggle Button except from it does not have an active state, hence no `Feedback value` parameter is needed.

`Minimal value` and `Maximum value` are compulsory, indicating the range of the `Display value`.

##### Other buttons

| Button | Display | Tap | Hold |
|--------|---------|-----|------|
| NAV & COM | Active & standby frequency | Swap frequencies | Show numpad to enter frequency |
| AP Master | AP Master status | Toggle AP Master | |
| AP Heading | AP Heading status & value | Toggle AP Heading | Sync current heading |
| AP Nav | AP Nav status | Toggle AP Nav |
| AP Approach | AP Approach status | Toggle AP Aproach |
| AP Altitude | AP Altitude status | Toggle AP Altitude |
| AP Heading/Altitude Increase/Decrease | Static +/- sign | Increase/Decrease by 1 | Increase/Decrease by 10

### Known Issues

- If you spam the same buttons too quickly, SimConnect will get error and stop responding to any further command. The plugin will try to automatically reconnect. 
  - However, if you see the message "Connected to Flight Simulator" flashing constantly in the sim, the plugin might be in a infinitely retry loop. In this case, close Stream Deck software on your PC (which will kill the plugin), wait a couple of seconds for all the SimConnect connections to close, and re-open Stream Deck.