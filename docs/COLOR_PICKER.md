# Color Picker and Season Carousel

## Color picker decision

BDFR LogonUI uses a native WPF color-wheel picker inspired by the supplied circular reference UI.

The project intentionally does not embed `evoluteur/colorpicker` because it is a JavaScript/jQuery UI widget and would introduce a web runtime dependency into a desktop experience that may later operate close to the Windows sign-in boundary.

The native picker provides:

- circular hue ring;
- inner saturation/value field;
- live color preview;
- HEX input;
- RGB input;
- reusable picker for every theme and gauge color property.

## Season Carousel

The former user-facing **Elena** label is now **Season Carousel**.

Season Carousel keeps the existing automatic Persian-season background selection behavior while using clearer product terminology.

The toolbar no longer exposes a **season folder** button.
