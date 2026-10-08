# Organizational message widget

BDFR LogonUI can display an organization-controlled information/message card on the lock dashboard.

The card is a normal editable widget:

- movable;
- resizable;
- saved with the rest of the layout;
- hidden from resize handles after layout lock.

## Source priority

The reader uses the first existing source in this order:

1. Machine-managed file:

   `%ProgramData%\BDFR\LogonUI\organization-message.txt`

2. Per-user test/override file:

   `%LocalAppData%\BDFR\LogonUI\organization-message.txt`

3. Portable sidecar shipped beside the standalone executable:

   `organization-message.txt`

This allows an IT administrator to deploy a machine-wide message while keeping portable/test builds easy to edit without administrator rights.

## File format

The file is UTF-8 text:

```text
Enabled=true
Organization=نام سازمان
Title=اطلاعیه سازمانی
Footer=واحد روابط عمومی
Priority=Normal
ValidFrom=
ValidUntil=

---MESSAGE---
متن اصلی پیام در این قسمت نوشته می‌شود.
پیام می‌تواند چند خط داشته باشد.
```

Supported priorities:

- `Normal`
- `Important`
- `Critical`

The widget changes its status accent according to the selected priority.

## Optional scheduling

`ValidFrom` and `ValidUntil` are optional.

If supplied, the widget is shown only inside that date/time window.

Example:

```text
ValidFrom=2026-10-10 08:00
ValidUntil=2026-10-10 18:00
```

Set:

`Enabled=false`

to suppress the message entirely.

## Live reload

The standalone dashboard checks the active text file approximately every 5 seconds.

Changing and saving the text file therefore does not require rebuilding or restarting BDFR LogonUI.

## UI

Use the **پیام سازمانی** toolbar button to open the currently editable source in Notepad.

The default widget is placed below the notifications area and can be repositioned/resized in Edit Mode.
