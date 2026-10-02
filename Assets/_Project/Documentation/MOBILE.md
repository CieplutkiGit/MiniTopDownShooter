# Mobile Controls

The mobile input layer feeds the same player state and weapon APIs as keyboard, mouse, and gamepad input. No mobile-only gameplay controller is required.

## Components

### MobileInputState

Add one MobileInputState to the gameplay UI or another persistent gameplay object.

It stores current virtual input and one-shot button events. PlayerController and PauseInputController can reference it explicitly. If the reference is empty, they search the scene for one.

Explicit references are recommended in final scenes.

### MobileJoystick

Use two joysticks: Move on the left and Look on the right.

Each joystick supports a configurable dead zone, normalized output, configurable handle travel, and independent multi-touch pointer ownership.

The look joystick can optionally fire while aiming. Fire Threshold controls when aim magnitude becomes a held trigger.

### MobileActionButton

Use MobileActionButton for Fire, Reload, Next Weapon, Previous Weapon, and Pause.

A dedicated Fire button is optional when the look joystick has Fire While Aiming enabled.

Fire is a held action. Reload, weapon switching, and pause are one-shot actions.

### SafeAreaFitter

Place the actual control layout under a RectTransform with SafeAreaFitter.

It updates anchors when Screen.safeArea or screen dimensions change, supporting notches, rounded corners, orientation changes, and other unsafe display regions.

### MobileControlsVisibility

Use MobileControlsVisibility when the same scene supports desktop and mobile.

Mobile builds show the configured controls root, desktop builds hide it, and the editor can force controls visible for testing.

Keep MobileInputState outside any root that may be disabled if another system still needs to consume a queued Pause action.

## Recommended hierarchy

- Canvas
  - MobileInputState object
  - SafeArea
    - LeftJoystick
    - RightJoystick
    - ReloadButton
    - NextWeaponButton
    - PreviousWeaponButton
    - PauseButton
    - OptionalFireButton

Put SafeAreaFitter on the SafeArea object. The visual controls under it can be assigned to MobileControlsVisibility.

## Setup

1. Create a Canvas and EventSystem.
2. Add one MobileInputState.
3. Add a left MobileJoystick set to Move.
4. Add a right MobileJoystick set to Look.
5. Enable Fire While Aiming on the look stick or add a Fire action button.
6. Add a Pause action button.
7. Add Reload and weapon-switch buttons if the demo uses those features.
8. Put the layout under SafeAreaFitter.
9. Assign the same MobileInputState to both joysticks and buttons.
10. Assign it to PlayerController and PauseInputController, or let them use the scene fallback.
11. Run Tools > Mini Top Down Shooter > Validate Open Scene.

## Input coexistence

InputReader chooses the stronger movement vector between physical and touch input.

Aim priority is keyboard/gamepad look, mobile look joystick, then mouse pointer projection.

Fire combines the physical twin-stick threshold, mouse/gamepad Fire action, mobile look-to-fire, and the optional mobile Fire button.

This lets one scene be tested with touch, mouse, keyboard, and gamepad without swapping gameplay scripts.

## Testing checklist

Test two simultaneous touches, aim/fire while moving, optional Fire button, reload, weapon switching, pause/resume, death/respawn while touching controls, orientation changes if supported, safe areas, multiple aspect ratios, and low-end device performance.
