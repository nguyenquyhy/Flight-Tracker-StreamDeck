## MobiFlight Integration Guide

MobiFlight is a powerful tool that significantly extends the capabilities of the Flight Tracker plugin. Due to its extensive feature set, setting it up can feel confusing at first. This guide will give you more context on what it can do, and walk you through the process step-by-step.

### 1. Why MobiFlight?

If you've used the Stream Deck plugin for a while, you may have noticed that default [SimConnect Events](https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Event_IDs.htm) work well for simpler GA aircraft but fail on more complex aircraft like those from FBW, A2A, or PMDG. These sophisticated aircraft implement custom internal systems and ignore many default SimConnect events. For instance, the `TOGGLE_MASTER_BATTERY` event works in a Cessna 152 but does nothing in the FBW A380X.

For these complex aircraft, custom variables called L:Vars are often used to control internal systems. For example, the FBW A380X uses the L:Var `L:A32NX_OVHD_ELEC_BAT_1_PB_IS_AUTO` and `L:A32NX_OVHD_ELEC_BAT_2_PB_IS_AUTO` to control its battery switches. These L:Vars are specific to FBW aircraft, so they won't work with aircraft from other developers.

MobiFlight helps by providing a WASM module that acts as a bridge between SimConnect events and L:Var manipulation, allowing you to control these custom systems. For example:

> A380_OVHD_ELEC_Battery_APU_Button_Toggle#(L:A32NX_OVHD_ELEC_BAT_APU_PB_IS_AUTO) ! (>L:A32NX_OVHD_ELEC_BAT_APU_PB_IS_AUTO)

This line defines a new SimConnect event called `A380_OVHD_ELEC_Battery_APU_Button_Toggle` that toggles the value of the L:Var `L:A32NX_OVHD_ELEC_BAT_APU_PB_IS_AUTO`. You can use this event in a Generic Flight Tracker button to control the APU battery of the FBW A380X.

### 2. How to set MobiFlight up for the Flight Tracker plugin?

Though this feature in MobiFlight is now marked as legacy, it still works well for our purposes. Here is how you can set that up:

1. Install MobiFlight from https://www.mobiflight.com/ (currently at version 10.4.0)
2. Run MobiFlight Connector after the installation
3. Install the MSFS WASM Module and update events.txt when prompted. If you don't see any prompt, you can find them under `Extras > MSFS2020` menu.
4. Yyou should now see a folder `mobiflight-event-module` in your Community folder, with `modules\events.txt` file inside.
5. Open the `events.txt` to see all the existing mapping between SimConnect events and L:Vars. Find the line with the desired mapping. 
6. Take note of the name before the first `#` in the line
7. In a Generic button of Flight Tracker plugin, add `MobiFlight.` prefix to the name above, and use it in `Toggle event` or `Hold event` field.

For example:
- You may find this line `A380_OVHD_ELEC_Battery_APU_Button_Toggle#(L:A32NX_OVHD_ELEC_BAT_APU_PB_IS_AUTO) ! (>L:A32NX_OVHD_ELEC_BAT_APU_PB_IS_AUTO)` in `events.txt`
- `A380_OVHD_ELEC_Battery_APU_Button_Toggle` is the preset name
- In Flight Tracker plugin, use `MobiFlight.A380_OVHD_ELEC_Battery_APU_Button_Toggle`

We've included a quick GNS 530 example for the knobs and basic buttons [here](https://github.com/nguyenquyhy/Flight-Tracker-StreamDeck/tree/master/Assets/Starter%20Profiles/MobiFlight-GNS-530.streamDeckProfile)

Note: You don't need to press Run in MobiFlight Connector. You don't even need to run MobiFlight Connector at all after the initial setup.

### 3. How to add more mapping to the events list?

There are 2 ways to add more mappings if you already know the L:Var.

#### Method 1

All the current mapping that you can see in events.txt are collected from https://hubhop.mobiflight.com. 

1. Create a new account or login at https://hubhop.mobiflight.com
1. Press Add preset and fill in all the necessary field. I would recommend you to take reference of the existing presets of the same aircraft and try to follow the naming convention.
1. Open MobiFlight Connector
1. Go to `Extras > MSFS2020 > Update events.txt (legacy)`
1. Restart MSFS

#### Method 2

If you prefer not to share the preset with the community, you can add the mapping locally:

1. Open `mobiflight-event-module\modules` in your Community folder. You should see `events.txt` and `events.user.txt.inactive` files.
1. Do not modify `events.txt` directly as this file will be overwritten by MobiFlight Connector updates. Instead, rename `events.user.txt.inactive` to `events.user.txt` and add your new mappings/presets there.
1. Restart MSFS

### 4. How do I see the L:Var value in the Flight Tracker buttons?

Thanks to a recent update from Asobo, SimConnect can now read L-Vars directly, allowing you to use them in the `Feedback value` or `Display value` fields. Just enter the L:Var name (e.g., `L:A32NX_OVHD_ELEC_BAT_APU_PB_IS_AUTO`) with `L:` prefix.

Here is an example of the setup for Battery 1 button on the FBW A380X:
- Header: `BAT 1`
- Toggle event: `MobiFlight.A380_OVHD_ELEC_Battery_1_Button_Toggle`
- Feedback value: `L:A32NX_OVHD_ELEC_BAT_APU_PB_IS_AUTO`

### 5. How do I find the L:Vars that are not in HupHop?

You can use tools like FSUIPC (free version) to see the L:Vars: 
1. Load in to the aircraft in MSFS
2. In FSUIPC, click on menu `Add-ons > WASM > List Lvars`