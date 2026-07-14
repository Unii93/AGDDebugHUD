# AGDDebugHUD - Plugin & API Guide

AGDDebugHUD is a BepInEx plugin that displays a real-time debug overlay in the top-left corner of the screen. 
It comes with built-in player stats and a public API so your own plugins/mods can register custom debug lines.

*Compatible with BepInEx plugins and Official API mods.*  

---
# Images

![](https://github.com/Unii93/AGDDebugHUD/raw/a11bb3314f63e042d72adacc13d74222ef71b17b/Images/Standing.png)
![](https://github.com/Unii93/AGDDebugHUD/raw/a11bb3314f63e042d72adacc13d74222ef71b17b/Images/Shark.png)
![](https://github.com/Unii93/AGDDebugHUD/raw/a11bb3314f63e042d72adacc13d74222ef71b17b/Images/Air.png)
---

## Built-in display

Once installed, the HUD appears automatically when the `GameSingleton` awakens. Press `F3` (configurable) to toggle the overlay on/off.

### Game

| Line | Content |
|---|---|
| FPS | Frame time in ms + FPS |
| HUD Time | HUD processing time in ms + FPS impact |

### Network

| Line | Content |
|---|---|
| Ping | Current network ping in ms |

### Movement

| Line | Content |
|---|---|
| Velocity | Current movement speed in m/s + X/Y/Z components |
| Position | World-space coordinates |

### Physics

| Line | Content |
|---|---|
| Ragdoll State | Whether the player is currently ragdolled |

### Status

| Line | Content |
|---|---|
| State | Grounded, Aiming, Alive, NoClip booleans |
| Status Effects | Count and names of active status effects (e.g. Invincible, RecentlyHardHit) |

All default values are sourced from `SessionManager.Instance.LocalPlayer`. Each section and individual line can be disabled via the BepInEx config file.

---

## Configuration

All settings live in `BepInEx/config/com.unii.debugHud.cfg`.

| Setting | Default | Description |
|---|---|---|
| `Enabled` | `true` | Master toggle for the entire HUD |
| `ToggleKey` | `F3` | Key to show/hide the overlay |

Section and line toggles (all default `true`):

- `EnableGameSection` / `EnableFPS` / `EnableHUDFPS`
- `EnableNetworkSection` / `EnablePing`
- `EnableMovementSection` / `EnableVelocity` / `EnablePosition`
- `EnablePhysicsSection` / `EnableRagdollState`
- `EnableStatusSection` / `EnableState` / `EnableStatusEffects`

Disabling a section hides all its lines regardless of their individual toggles.

---

## Compatibility

This plugin is made to be as easy to implement as possible.  
It is natively compatible with **BepInEx** plugins AND mods using the **Official** modding API.

## API reference

The public API lives in the static class `AGDDebugHUD.DebugHUDAPI` (assembly `AGDDebugHUD`).

### `RegisterHeader(string label)`

Creates a **section header** — a bold, gold-colored label displayed above grouped lines. Returns a `Header` object to pass into `RegisterCustomLine`.

- **`label`** - display text for the header (e.g. `"--- MyPlugin ---"`).

### `RegisterCustomLine(string label, Func<string> getter)`
### `RegisterCustomLine(string label, Func<string> getter, Header header)`

Registers a new line in the overlay. The `label` serves as both the display name and a unique key.

- **`label`** - unique identifier for the line. Also used as the GameObject name.
- **`getter`** - a `Func<string>` called every frame (during `Update`) to fetch the current value. Return `null` to hide the line entirely.
- **`header`** (optional) - the `Header` returned by `RegisterHeader`. Lines sharing the same header appear grouped together under it.

> Best practice is to register (`RegisterCustomLine`) during your own plugin's `Awake`.

### `EnableLine(string label)`

Re-enables a line that was previously disabled.

### `DisableLine(string label)`

Hides the line (GameObject set inactive). The getter is not called while disabled.

### `FreezeLine(string label)`

Keeps the last visible text on screen but stops calling the getter.

---

## How to use from your own plugin

Your plugin does **not** need a hard reference to `AGDDebugHUD.dll`.
Use reflection to call the API, this avoids compile-time dependency and keeps your plugin perfectly functional even if the HUD isn't installed.

### Generic template (line only)

BepInEx:
```csharp
private void RegisterDebugHUD()
{
    var apiType = Type.GetType("AGDDebugHUD.DebugHUDAPI, AGDDebugHUD");
    if (apiType == null) return; // HUD not installed
    var registerMethod = apiType.GetMethod("RegisterCustomLine");
    if (registerMethod == null) return;

    Func<string> myGetter = () => { /* your logic */ return "MyLabel: someValue"; };
    registerMethod.Invoke(null, new object[] { "MyLabel", myGetter });
}
```

### Generic template (header + lines)

```csharp
private void RegisterDebugHUD()
{
    var apiType = Type.GetType("AGDDebugHUD.DebugHUDAPI, AGDDebugHUD");
    if (apiType == null) return;
    var registerHeaderMethod = apiType.GetMethod("RegisterHeader");
    var registerLineMethod = apiType.GetMethod("RegisterCustomLine");
    if (registerHeaderMethod == null || registerLineMethod == null) return;

    var header = registerHeaderMethod.Invoke(null, new object[] { "--- MyPlugin ---" });

    Func<string> myGetter1 = () => { /* your logic */ return "MyLabel: someValue"; };
    registerLineMethod.Invoke(null, new object[] { "MyPlugin_1", myGetter1, header });

	Func<string> myGetter2 = () => { /* your logic */ return "MyLabel: someValue"; };
    registerLineMethod.Invoke(null, new object[] { "MyPlugin_2", myGetter2, header });
}
```

### OneLine template

```csharp
private void RegisterDebugHUD()
{  
    Type.GetType("AGDDebugHUD.DebugHUDAPI, AGDDebugHUD")?
	.GetMethod("RegisterCustomLine")?
	.Invoke(null, new object[] { "Label/Name", (Func<string>)(() => { /* your logic */ return "MyLabel: someValue"; }) });
}
```

### Real example: custom entity tracking

```csharp
private void RegisterDebugHUD()
{
    var apiType = Type.GetType("AGDDebugHUD.DebugHUDAPI, AGDDebugHUD");
    if (apiType == null) return;
    var registerHeaderMethod = apiType.GetMethod("RegisterHeader");
    var registerLineMethod = apiType.GetMethod("RegisterCustomLine");
    if (registerHeaderMethod == null || registerLineMethod == null) return;
	
	var rb = GetComponent<Rigidbody>();

    var header = registerHeaderMethod.Invoke(null, new object[] { "--- MyMod - Entity ---" });

    // Position
    Func<string> posGetter = () =>
    {
        var p = transform.position;
        return string.Format("Pos: ({0:F2}, {1:F2}, {2:F2})", p.x, p.y, p.z);
    };
    registerLineMethod.Invoke(null, new object[] { "MyMod_Position", posGetter, header });

    // Velocity
    Func<string> velGetter = () =>
    {
        if (rb == null) return null;
        var v = rb.linearVelocity;
        return string.Format("Vel: {0:F3} m/s (X:{1:F2} Y:{2:F2} Z:{3:F2})", v.magnitude, v.x, v.y, v.z);
    };
    registerLineMethod.Invoke(null, new object[] { "MyMod_Velocity", velGetter, header });

    // Custom state
    Func<string> stateGetter = () =>
    {
        return string.Format("Last Used By: {0}", lastPlayerUse);
    };
    registerLineMethod.Invoke(null, new object[] { "MyMod_LastUse", stateGetter, header });
}
```

---

## Using from an Official API mod

Official API mods (implementing `AGD.ModAPI.IModPlugin`) can use the same reflection pattern from `OnLoad`:

```csharp
using AGD.ModAPI;

public class MyModPlugin : IModPlugin
{
    public void OnLoad(IModContext context)
    {
        var apiType = Type.GetType("AGDDebugHUD.DebugHUDAPI, AGDDebugHUD");
        if (apiType == null) return;
        var registerHeaderMethod = apiType.GetMethod("RegisterHeader");
        var registerLineMethod = apiType.GetMethod("RegisterCustomLine");
        if (registerHeaderMethod == null || registerLineMethod == null) return;

        var header = registerHeaderMethod.Invoke(null, new object[] { "--- MyMod ---" });

        Func<string> myGetter = () => string.Format("My Value: {0}", someValue);
        registerLineMethod.Invoke(null, new object[] { "MyMod_Value", myGetter, header });
    }

    public void OnUnload() { }
}
```

---

## Lifecycle notes

1. **Injection** - `GameSingleton.Awake` -> `InjectDebugHUD` creates the HUD `GameObject` (`AGD_DebugHUD`).
2. **Initialization** - `DebugHUDBehaviour.Awake` builds the Canvas/TextMeshPro UI, then calls `DebugHUDAPI.Initialize`, which creates TMP GameObjects for any lines already registered.
3. **Per-frame** - `DebugHUDBehaviour.Update` refreshes built-in lines, then calls `DebugHUDAPI.RefreshAll()` which invokes all registered getters.
4. **Late registrations** - If `RegisterCustomLine` is called after `Initialize`, the line gets created immediately.

---

## Tips

- **Label naming** - use descriptive, unique names (e.g. `"MyPlugin_PlayerGold"`). Labels become GameObject names and serve as the API key for `EnableLine`/`DisableLine`/`FreezeLine`.
- **Headers** - use `RegisterHeader` to group related lines together. Headers render as bold gold text and make the overlay easier to scan.
- **Performance** - getters run every frame, so keep them cheap. Avoid `FindObjectOfType` or heavy LINQ inside getters - cache references in your own `Awake`/`Start`.
- **Hiding a line** - return `null` from the getter (line disappears until next call) vs. `DisableLine` (line disappears until `EnableLine` is called).
- **No HUD installed?** - the reflection guard (`Type.GetType(...) == null`) means your plugin continues to work normally - the debug lines are simply absent.

---

## Installation

1. Install BepInEx.
2. Copy `AGDDebugHUD.dll` into `BepInEx/plugins/`.
3. Launch the game - you should see `[[AGDDebugHUD]] Debug HUD injected successfully.` in the BepInEx console and the overlay in the top-left corner.
4. That's it.

## Want to contact me ?
You can reach me through the [Official AGD Discord server](https://discord.com/invite/PTvpYGmtGK) @unii_le_chat or through the [GitHub page](https://github.com/Unii93/AGDDebugHUD).