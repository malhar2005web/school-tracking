---
name: Claymorphic Safety & Transit
colors:
  surface: '#fbf8ff'
  surface-dim: '#d8d7f7'
  surface-bright: '#fbf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f5f2ff'
  surface-container: '#eeecff'
  surface-container-high: '#e7e6ff'
  surface-container-highest: '#e1e0ff'
  on-surface: '#181930'
  on-surface-variant: '#484552'
  inverse-surface: '#2d2e47'
  inverse-on-surface: '#f1efff'
  outline: '#797584'
  outline-variant: '#cac4d4'
  surface-tint: '#644bba'
  primary: '#644bba'
  on-primary: '#ffffff'
  primary-container: '#9b83f6'
  on-primary-container: '#310b87'
  inverse-primary: '#ccbeff'
  secondary: '#0c6b55'
  on-secondary: '#ffffff'
  secondary-container: '#a1f3d7'
  on-secondary-container: '#18715b'
  tertiary: '#765b00'
  on-tertiary: '#ffffff'
  tertiary-container: '#cca741'
  on-tertiary-container: '#503d00'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#e7deff'
  primary-fixed-dim: '#ccbeff'
  on-primary-fixed: '#1e0060'
  on-primary-fixed-variant: '#4b30a1'
  secondary-fixed: '#a1f3d7'
  secondary-fixed-dim: '#85d6bb'
  on-secondary-fixed: '#002118'
  on-secondary-fixed-variant: '#00513f'
  tertiary-fixed: '#ffdf93'
  tertiary-fixed-dim: '#e9c259'
  on-tertiary-fixed: '#241a00'
  on-tertiary-fixed-variant: '#594400'
  background: '#fbf8ff'
  on-background: '#181930'
  surface-variant: '#e1e0ff'
typography:
  headline-xl:
    fontFamily: Plus Jakarta Sans
    fontSize: 34px
    fontWeight: '800'
    lineHeight: 42px
    letterSpacing: -0.02em
  headline-xl-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 28px
    fontWeight: '800'
    lineHeight: 36px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 26px
    fontWeight: '700'
    lineHeight: 34px
    letterSpacing: -0.015em
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 22px
    fontWeight: '700'
    lineHeight: 28px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 18px
    fontWeight: '700'
    lineHeight: 24px
    letterSpacing: 0em
  body-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 16px
    fontWeight: '500'
    lineHeight: 24px
    letterSpacing: 0em
  body-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
    letterSpacing: 0em
  body-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.01em
  label-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 15px
    fontWeight: '700'
    lineHeight: 20px
    letterSpacing: 0.01em
  label-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 13px
    fontWeight: '700'
    lineHeight: 18px
    letterSpacing: 0.02em
  label-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 11px
    fontWeight: '700'
    lineHeight: 14px
    letterSpacing: 0.03em
rounded:
  sm: 0.5rem
  DEFAULT: 1rem
  md: 1.5rem
  lg: 2rem
  xl: 3rem
  full: 9999px
spacing:
  gutter: 1rem
  gutter-mobile: 0.75rem
  margin: 1.25rem
  margin-mobile: 1rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2rem
---

## Brand & Style

This design system establishes a warm, delightful, and reassuring atmosphere for student transit and daily safety tracking. By merging the tactile charm of claymorphism with high-trust legibility, the interface reduces parental anxiety through friendly visual language, tangible interactive affordances, and clear physical metaphors.

### Visual Style: Tactile Claymorphism
- **Tactile Volume:** UI elements feel like soft, matte modeling clay sculpted into functional controls. Surfaces feature dual-layered shadows (a diffuse ambient drop shadow underneath and a crisp, light-catching inner bevel on top).
- **Physical Feedback:** Micro-interactions mimic compression. Buttons visibly depress inward upon press, reducing their offset shadow and shifting specular highlights.
- **Tone & Mood:** Cheerful, protective, and human. Balances playfulness for students with the absolute reliability and swift scanning required by busy parents and school administrators.
- **Illustration & Imagery:** Smooth, volumetric 3D clay-rendered assets depicting buses, route waypoints, badges, and mascot reactions, avoiding sharp edges or glossy hyper-reflections.

## Colors

The palette balances soothing pastel volumes with high-contrast text to fulfill strict accessibility and readability standards on mobile viewports.

### Palette Architecture
- **Primary (`#9B83F6` — Soft Lavender):** Used for primary CTAs, active route states, bottom navigation emphasis, and core clay structures.
- **Secondary (`#8BDCC1` — Mint Green):** Represents safety, completed journeys, safe boarding confirmations, and positive state markers.
- **Tertiary (`#FFD66B` — Warm Cheerful Yellow):** Highlights en-route transit alerts, vehicle proximity warnings, rewards, and time-sensitive status flags.
- **Alert / Accent (`#FF927F` — Coral Pink):** Reserved for immediate warnings, delays, missed stops, emergency contact actions, and unscheduled route deviations.
- **Neutral / Text (`#292A42` — Dark Navy):** Replaces harsh pure black for all structural typography, deep borders, and primary icon glyphs to maintain a soft yet high-contrast presence.
- **Canvas (`#F8F7FF` — Crisp Tinted Chalk):** An ultra-soft off-white canvas with a subtle cool undertone, allowing pure white (`#FFFFFF`) clay cards to visually lift from the backdrop.

### Clay Shading Tokens
- **Clay Inner Light Highlight:** `inset 2px 2px 4px rgba(255, 255, 255, 0.75)`
- **Clay Inner Depth Shadow:** `inset -3px -3px 6px rgba(41, 42, 66, 0.08)`
- **Primary Clay Underside:** `inset 0px -4px 0px rgba(118, 93, 219, 0.35)`

## Typography

Plus Jakarta Sans powers all textual layers. Its modern geometric curves and rounded terminals mirror the clay physical language without losing clarity during high-speed, on-the-go scanning.

- **Weight Hierarchy:** Bold weights (`700`, `800`) are emphasized for names, transit statuses, and buttons to evoke toy-like solidity. Body copy sits at medium (`500`) to guarantee legibility against lightly tinted card backgrounds.
- **Compact Vertical Rhythms:** Headings are locked to tight line heights to keep mobile information dense and visually grouped above live transit maps.

## Layout & Spacing

The system is mobile-first, prioritizing thumb-zone interaction patterns with fluid adaptive scaling for tablets and monitoring consoles.

### Mobile Screen Structure (Primary Form Factor)
- **Top Safe Bar:** Houses student quick-switch avatars and school zone status indicators.
- **Interactive Middle Zone:** Map rendering viewport or real-time stop progress timeline.
- **Sticky Bottom Sheet / Bar:** Elevated clay bottom drawer displaying transit ETA, driver information, and contact triggers, situated directly above the floating bottom navigation bar.

### Grid Rhythm & Adapters
- **Mobile (up to 599px):** 4-column fluid layout with `margin-mobile` (16px) margins and `gutter-mobile` (12px) gutters.
- **Tablet (600px - 1023px):** 8-column layout with 20px margins, introducing persistent split-screen tracking (live map on left, student status feed on right).
- **Desktop / Tablet Landscape (1024px+):** Max fixed container of 1140px, centered with 12 columns for institutional administration views.

## Elevation & Depth

Visual depth is achieved through tactile clay modeling rather than flat material planes or glossy glass reflections. Every floating card behaves like physical extruded clay resting over a soft surface.

### Layered Depth Formula
1. **Base Floor (Level 0):** Canvas `#F8F7FF`. Neutral, non-elevated ground.
2. **Clay Cards & Tiles (Level 1):** Solid `#FFFFFF` background.
   - External Shadow: `0 8px 20px -4px rgba(41, 42, 66, 0.08), 0 4px 8px -2px rgba(41, 42, 66, 0.04)`
   - Internal Surface Light: `inset 2px 2px 4px rgba(255, 255, 255, 0.9)`
   - Internal Surface Depth: `inset -2px -3px 4px rgba(41, 42, 66, 0.04)`
3. **Interactive Floating Pill Buttons & Floating Panels (Level 2):**
   - External Shadow: `0 12px 24px -6px rgba(155, 131, 246, 0.35)`
   - Physical Bottom Lip: `inset 0 -4px 0 rgba(0, 0, 0, 0.12)`
   - Physical Top Light: `inset 0 3px 2px rgba(255, 255, 255, 0.45)`
4. **Pressed/Engaged State (Tactile Compression):**
   - External Shadow: `0 3px 8px -2px rgba(155, 131, 246, 0.25)`
   - Bottom Lip: `inset 0 -1px 0 rgba(0, 0, 0, 0.12)`
   - Transform: `translateY(3px)`
5. **Modals & Safety Sheets (Level 3):**
   - External Shadow: `0 24px 48px -12px rgba(41, 42, 66, 0.18)`
   - Blurred under-tint overlay: `rgba(41, 42, 66, 0.35)` backdrop blur (6px).

## Shapes

The shape system relies on pronounced pill-like geometry and ultra-soft contours. Sharp vertices are strictly forbidden across all touch interfaces to sustain a friendly, child-safe product feel.

- **Component Standard:** Cards, modals, and container panels use corner radii between 22px and 28px (`rounded-xl` to `rounded-2xl`).
- **Interactive Controls:** All buttons, status chips, avatar borders, and notification tags are fully pill-shaped (`9999px` / `rounded-full`).
- **Input Fields & Inset Wells:** Input track areas use a continuous 24px curvature with concave inner shadows to look like stamped impressions in modeling clay.

## Components

### Buttons
- **Primary Pill Button:** `#9B83F6` solid clay surface with white bold text (`label-lg`), `inset 0 3px 2px rgba(255, 255, 255, 0.45)` top highlight, and `inset 0 -4px 0 rgba(118, 93, 219, 0.5)` base lip. When active, translates 3px down and reduces bottom lip to 1px.
- **Secondary Action Button:** Mint `#8BDCC1` surface with `#292A42` label, styled with matching top highlight and an organic mint underside lip (`inset 0 -4px 0 rgba(103, 184, 157, 0.45)`).
- **Icon Utility Button:** Rounded circular clay pucks (48x48px) with centered glyphs, designed for quick SOS calls, map re-centering, or horn alerts.

### Cards & Surface Tiles
- **Student Status Card:** Pure white clay surface with 24px rounded corners. Contains a prominent student circular avatar with an active status ring, trip progress indicator bar, and rounded status pill.
- **Live ETA Tile:** Tertiary yellow `#FFD66B` clay container with a dark navy bus icon, displaying remaining minutes in `headline-lg` with an inner embossed badge.

### Chips & Badges
- **Status Pills:** Compact fully rounded indicators. "On Bus" in secondary mint (`#8BDCC1`), "Approaching Stop" in tertiary yellow (`#FFD66B`), and "Delayed" in coral alert (`#FF927F`).
- All chips carry a gentle `inset 1px 1px 2px rgba(255,255,255,0.6)` specular rim.

### Text Inputs & Form Fields
- Inset clay styling: `#F8F7FF` background set into cards with `inset 2px 2px 4px rgba(41, 42, 66, 0.08)` and `inset -2px -2px 4px rgba(255, 255, 255, 0.9)`.
- 52px height minimum with 24px border radius. Placeholder in navy at 40% opacity; focused state outlines field with a 2px `#9B83F6` halo.

### Checkboxes & Toggle Switches
- **Toggle Switches:** Recessed track resembling a sculpted clay slot. The thumb is a crisp white spherical puck with 3D drop shadow that rolls smoothly from left to right on toggle.
- **Checkboxes:** 24x24px rounded squircle (8px radius) with extruded border, turning into mint `#8BDCC1` with a chunky white check icon when checked.

### Bottom Navigation Bar
- A floating clay bar positioned 16px above the device home indicator. Features 28px rounded corners, pure white clay surface, dual diffuse drop shadows, and 4 icon slots.
- The active item displays a soft lavender `#9B83F6` clay pill background behind the icon with micro-bounce transitions.

### Safety Micro-Components (Specialized)
- **SOS Panic Trigger:** A rounded coral-red tactile puck with tactile ribbed outer ridges and a countdown hold confirmation ring.
- **Waypoint Timeline:** A vertical clay string connecting pill-shaped bus stop nodes, with completed stops depicted as satisfied check tokens and upcoming stops as softly pulsing pastel bubbles.