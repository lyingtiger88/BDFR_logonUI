# Gauge style gallery

BDFR LogonUI includes 17 live vector gauge styles inspired by the supplied dashboard reference sheet.

The styles are not bitmap skins. Each preview and each live CPU/RAM/Battery widget uses the same `BDFRGauge` renderer, so:

- telemetry remains live;
- resize remains responsive;
- theme colors remain customizable;
- needle/ticks/arcs scale with the widget;
- the selected style persists between launches.

## Styles

1. Reference Arc
2. Round Dense
3. Round Bold
4. Round Minimal
5. Half Clean
6. Half Segmented
7. Half Blocks
8. Half Minimal
9. Inset Arc
10. Dark Radial
11. Dark Needle
12. Dark Compact
13. Mini Double Ring
14. Mini Ticks
15. Mini Open Arc
16. Mini Display
17. Mini Dense

## Personalization

Open **Theme & Gauge**.

The gauge-style gallery shows a live thumbnail for all 17 models.

Targets:

- All gauges
- CPU
- RAM
- Battery

Use **Apply to selection** for a per-gauge override or **Apply to all** to set one global default.

Gauge style state is stored separately from the theme-color JSON under the current user's LocalAppData BDFR LogonUI settings folder.
