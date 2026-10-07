# Standalone Demo First

BDFR LogonUI is developed in two intentionally separated stages.

## Stage A — normal executable

`BDFR.LogonUI.Demo.exe` is a normal desktop application. It is the mandatory first test surface.

It does **not**:

- replace the Windows lock screen;
- register a Credential Provider;
- modify LogonUI/Winlogon;
- modify authentication policy;
- require administrator privileges;
- prevent the user from closing the program.

It can therefore be tested like any other application. The user can run it, edit the layout, close it, and remove the published files.

## Current standalone capabilities

- Live clock.
- Persian date as the primary calendar.
- Gregorian date with Latin 0–9 digits.
- Hijri date with Arabic-Indic digits and smaller secondary typography.
- 6×7 month grid with Persian/Gregorian/Hijri day values.
- Live CPU percentage using Windows `GetSystemTimes`.
- Live RAM usage using `GlobalMemoryStatusEx`.
- Live battery/AC state using `GetSystemPowerStatus`; desktops without a battery show `N/A`.
- Network availability summary.
- Reusable BDFR semicircular gauges based on the canonical design reference.
- CPU, RAM and Battery gauges are **independent widgets** and can each be moved/resized.
- Calendar, clock, notifications, quick status, system header and unlock hint are movable/resizable widgets.
- Layout save, reset and edit-lock workflow.
- Custom wallpaper selection.
- Elena seasonal-background mode.
- Glass-style surfaces and edit grid.
- F11 full-screen test mode.

### Demo workflow

1. Launch the executable.
2. The app opens in a maximized lock-screen-style preview.
3. Press **F11** to toggle full-screen preview if desired.
4. Click **ویرایش چیدمان**.
5. Drag any widget to a new position.
6. Resize a widget from its lower-right handle.
7. CPU, RAM and Battery gauges may be resized and positioned independently.
8. Hold **Shift** while resizing to allow a free aspect ratio.
9. Click **ذخیره** to save without leaving edit mode.
10. Click **پایان و قفل ویرایش** to save and hide editing handles.
11. Use **بازنشانی** to restore the built-in layout.
12. Use **خروج** or Alt+F4 to exit.

When a saved layout exists, the next launch starts with the layout locked.

Layout geometry is stored under:

`%LocalAppData%\BDFR\LogonUI\layout.demo.json`

### Backgrounds and Elena Mode

Custom backgrounds selected from the toolbar are copied into:

`%LocalAppData%\BDFR\LogonUI\Backgrounds`

Elena Mode automatically selects the current Persian-season background when a matching raster file exists.

Recognized seasonal names include:

- `Spring_16x9.jpg`
- `Summer_16x9.jpg`
- `Autumn_16x9.jpg`
- `Winter_16x9.jpg`

The same names are accepted with `.jpeg`, `.png`, or `.bmp`. A simple `Spring.jpg` / `Summer.jpg` / etc. fallback name is also recognized.

Use **پوشه فصل‌ها** in the toolbar to open the folder directly.

The naming and behavior intentionally match the Anahita/BDFR Persian Calendar seasonal-background concept. The standalone demo does not read Anahita's database directly; calendar/event data will move through the broker/provider contract so authentication-adjacent UI does not depend on another app's internal storage.

## Build output

Every successful push build is packaged as a self-contained Windows x64 ZIP and published as a prerelease with a SHA-256 checksum.

The workflow uses concurrency cancellation so only the newest push in a rapid development batch proceeds to permanent release publication.

## Stage B — Windows integration

Only after the standalone experience is stable do we start sign-in integration.

The integration stage must reuse shared widget/layout concepts without making authentication depend on the demo executable.

"Replacement" in this project means replacing/customizing the *experience through supported integration paths*. It does not mean replacing or patching `LogonUI.exe`.
