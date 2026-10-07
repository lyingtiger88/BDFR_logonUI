# Gauge Design Reference

The user-provided semicircular gauge is the canonical visual reference for gauges in BDFR LogonUI.

## Visual language

All system-status gauges should follow this family unless a theme explicitly overrides it.

### Geometry

- Large semicircular / horseshoe gauge.
- Open bottom section; do not use a full circular dial.
- Wide outer progress band with a thinner inner accent ring.
- Rounded, clean geometry with generous whitespace.
- Central value is the dominant visual element.
- Tick labels are placed outside the arc.
- Default range: 0–100 with labels at 10-unit intervals.
- Avoid dense tick marks or dashboard-style clutter.

### Value presentation

- Large numeric value centered inside the gauge.
- Use a dark navy / near-black foreground for strong readability.
- Units may appear smaller below or beside the main value when required.
- No unnecessary captions inside the dial.

### Arc behavior

Reference appearance:

- Neutral/unfilled arc: very light cool gray.
- Normal/healthy region: soft mint/green.
- Warning region: warm orange.
- Critical region: orange-to-red.
- Current value marker: narrow dark navy marker crossing the progress band.

Color thresholds are semantic and configurable per metric; the UI component must not hard-code CPU-specific thresholds.

### Motion

- Animate value transitions smoothly.
- Progress arc follows the numeric value with easing.
- Marker movement must remain continuous; no abrupt jumps.
- On first appearance, use a short restrained reveal rather than a dramatic sweep.
- Respect reduced-motion accessibility settings.

### Layout behavior

The component must scale cleanly for:

- CPU
- RAM
- storage
- battery/charge
- temperature
- network quality
- security/health scores
- any normalized 0–100 provider metric

The same gauge component should support compact and large variants while preserving the reference proportions.

## Lock-screen use

For BDFR LogonUI:

- Prefer 1–3 important gauges rather than many small dials.
- Detailed metrics belong in an expanded system-status panel.
- Gauges shown while locked must consume only sanitized broker data.
- Secret/private information must never be encoded into gauge labels.
- The gauge must remain readable over seasonal/background imagery by using a controlled backing surface when necessary.

## Theme integration

The geometry and information hierarchy remain consistent across themes.

Themes may change:

- arc palette;
- typography;
- backing-surface opacity;
- glow/shadow;
- animation intensity.

Themes should not radically change:

- semicircular structure;
- central large value;
- external numeric scale;
- thin current-value marker;
- clean, sparse composition.

## Implementation direction

Create a reusable `BDFRGauge` control rather than implementing individual gauges per widget.

Suggested API surface:

```text
Minimum
Maximum
Value
Unit
NormalRange
WarningRange
CriticalRange
ShowScale
ShowMarker
AnimationDuration
PrivacyState
```

The control should expose semantic state to accessibility APIs and provide a non-animated fallback.
