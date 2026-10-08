# Widget Manager

BDFR LogonUI supports adding and removing dashboard widgets without rebuilding the application.

## Open the manager

Use the **ویجت‌ها** button in the main toolbar.

The manager lists every built-in dashboard widget:

- calendar;
- clock and dates;
- notifications;
- system status header;
- CPU gauge;
- RAM gauge;
- battery gauge;
- organizational message;
- quick status;
- unlock hint.

Each item can be:

- **حذف از صفحه** — hidden from the dashboard;
- **افزودن به صفحه** — restored to the dashboard.

Removed widgets are not deleted from the application. They remain available in the manager and keep their saved size/position.

## Fast remove in Edit Mode

When layout Edit Mode is active, every visible widget has a red **×** button in its upper-left corner.

Clicking it removes that widget from the page immediately.

Use **ویجت‌ها** to restore it.

## Persistence

Widget visibility is stored together with geometry in:

`%LocalAppData%\BDFR\LogonUI\layout.demo.json`

Older layout files without visibility data remain compatible and default widgets to visible.

## Reset

**بازنشانی** restores:

- default widget positions;
- default widget sizes;
- all built-in widgets to visible.

## Organization message

The organizational-message widget has its own content enable/schedule state in addition to dashboard visibility.

If the widget is removed from the dashboard, its source file remains untouched.
