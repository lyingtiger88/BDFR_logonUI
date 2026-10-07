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

### Demo workflow

1. Launch the executable.
2. The app opens in a maximized lock-screen-style preview.
3. Press **F11** to toggle full-screen preview if desired.
4. Enable **حالت ویرایش**.
5. Drag widgets to new positions.
6. Resize widgets from their lower-right handle.
7. Click **پایان و قفل ویرایش** to save the layout and hide editing handles.
8. Use **بازنشانی** to restore the built-in layout.
9. Use **خروج از تست** or Alt+F4 to exit.

Layout state is stored under the current user's LocalAppData folder and contains presentation geometry only.

## Stage B — Windows integration

Only after the standalone experience is stable do we start sign-in integration.

The integration stage must reuse shared widget/layout concepts without making authentication depend on the demo executable.

"Replacement" in this project means replacing/customizing the *experience through supported integration paths*. It does not mean replacing or patching `LogonUI.exe`.
