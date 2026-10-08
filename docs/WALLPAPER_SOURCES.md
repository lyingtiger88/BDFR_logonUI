# Wallpaper sources and Season Carousel

BDFR LogonUI deliberately separates manual wallpaper selection from automatic seasonal imagery.

## Manual wallpaper gallery

The lock experience never exposes an unrestricted file picker.

It reads thumbnails only from:

- `wallpaper` beside `BDFR.LogonUI.Demo.exe`;
- `%USERPROFILE%\Pictures\LockScreen`.

The user can select one of those thumbnails. Arbitrary paths are rejected by the background service.

## Season Carousel

Season Carousel does **not** use the LogonUI wallpaper folders.

Its source is Anahita Calendar's own seasonal gallery:

`%LOCALAPPDATA%\Programs\Anahita\picture\theme\season backgrounds`

This matches Anahita's `ThemeService.SeasonalBackgroundsRoot`.

The carousel follows the same naming convention as Anahita:

- `Spring_1.*`, `Spring_2.*`, ...
- `Summer_1.*`, ...
- `Autumn_1.*`, ...
- `Winter_1.*`, ...

and advances every 60 seconds while enabled.

An optional development override can be supplied with:

`BDFR_ANAHITA_SEASON_GALLERY`

No database access or arbitrary browsing is involved.
