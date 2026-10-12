# Rust Gamma Pro

Hardware monitor gamma switcher for Rust (NVIDIA and AMD).

![Platform](assets/badges/platform.svg)
![GPU](assets/badges/gpu.svg)
![Easy Anti-Cheat](assets/badges/anticheat.svg)
![License](assets/badges/license.svg)
[![Rust Gamma downloads per week](assets/badges/sourceforge_downloads.svg)](https://sourceforge.net/projects/rust-gamma/files/latest/download)

[![Download Rust Gamma](assets/badges/sourceforge_download.svg)](https://sourceforge.net/projects/rust-gamma/files/latest/download)

---

<p align="left">
  Language: &nbsp;
  <a href="README_RU.md"><kbd>&nbsp; Русский &nbsp;</kbd></a>
  &nbsp;
  <kbd><b>&nbsp; English (Active) &nbsp;</b></kbd>
</p>

---

## About

In Rust, nighttime visibility is intentionally low. Players often minimize the game to manually adjust gamma sliders in the NVIDIA Control Panel or AMD Adrenalin Software, and then restore settings when daytime returns.

Rust Gamma Pro automates this process directly through the Windows Hardware Display Calibration interface (Hardware LUT via `SetDeviceGammaRamp`). It allows one-key toggling between night and day gamma levels without leaving the game or tabbing out.

---

## Features

- **Instant toggle:** Switch between day and night gamma in 1 millisecond with a single hotkey (default: `F11`).
- **Easy Anti-Cheat (EAC) safe:** Zero process memory injection, zero DirectX hooks, zero game file alterations.
- **Smart Rust detection:** Automatically detects `RustClient.exe` and keeps the hotkey active only while gaming.
- **Target display selection:** Changes gamma specifically on the monitor running Rust, leaving secondary monitors (Discord, browser) untouched.
- **Fail-safe restoration:** Automatically resets monitor gamma to 1.00 when the game exits, crashes, or when Windows shuts down.
- **Hardware compatibility:** Works natively on NVIDIA GeForce, AMD Radeon, and Intel graphics cards.
- **Audio feedback:** High-pitch beep for Night mode and low-pitch beep for Day mode.
- **Full RivaTuner-style color control:** Independent sliders for Brightness (-100 to +100), Contrast (-100 to +100), and Gamma (0.50x to 3.00x) with live LUT curve graph preview.
- **Settings window:** Interactive color controls, live curve preview, 2-second test button, one-click profile reset, configurable hotkeys, and Windows autostart option.
- **Bilingual interface:** Instant toggle between English and Russian (EN / RU).
- **Zero overhead:** Standalone ~180 KB executable, 0.0% CPU usage, and <10 MB RAM consumption.

---

## Anti-Cheat and EAC Policy

Easy Anti-Cheat enforces bans against memory tampering, DLL injections, hook-based shader injectors (such as ReShade), and recoil mouse-movement scripts.

Rust Gamma Pro operates exclusively at the Windows display driver layer using Microsoft's standard `SetDeviceGammaRamp` API. To the operating system and the anti-cheat, this is identical to adjusting brightness buttons on your physical monitor or using Windows Night Light.

---

## Getting Started

1. Download the latest release: [RustGammaPro.exe](https://github.com/helliong/rust-gamma/releases).
2. Launch the application to open the Settings window:
   - Set your preferred nighttime gamma (recommended: `1.45 - 1.60`).
   - Click **Test (2 sec)** to preview the brightness curve.
   - Click **Save & Minimize to Tray**.
3. Launch Rust and press `F11` whenever night falls.

---

## Default Controls

| Action | Hotkey | Audio Feedback |
| :--- | :---: | :---: |
| **Night Mode (Gamma 1.5)** | `F11` | High pitch beep (900 Hz) |
| **Day Mode (Gamma 1.0)** | `F11` | Low pitch beep (450 Hz) |
| **Open Settings** | Double-click tray icon | None |

*The hotkey can be re-bound in the Settings window (F10, F9, F8, NumPad *, Scroll Lock, etc.).*

---

## Building from Source

Built using native C# with Win32 interop (.NET Framework 4.0+). Can be compiled out of the box using Windows' built-in compiler without installing Visual Studio or third-party SDKs:

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:app.ico /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll /out:RustGammaPro.exe RustGamma.cs
```

---

## Links and Feedback

- Website: [helliong.space](https://helliong.space)
- Repository: [github.com/helliong/rust-gamma](https://github.com/helliong/rust-gamma)
- SourceForge: [sourceforge.net/projects/rust-gamma](https://sourceforge.net/projects/rust-gamma/)
- Bug reports and issues: [github.com/helliong/rust-gamma/issues](https://github.com/helliong/rust-gamma/issues)
