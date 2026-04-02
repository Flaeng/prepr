# Design System Document: The Forensic Architect

## 1. Overview & Creative North Star
The North Star for this design system is **"The Forensic Architect."** In the world of high-fidelity code analysis, we move away from "software-as-a-utility" and toward "software-as-a-lens." The goal is to provide a surgical view of complex data structures while maintaining an atmosphere of calm, professional focus.

This design system rejects the "standard dashboard" template. Instead of rigid, boxed-in grids, we utilize **Asymmetric Information Density**. This means core metrics are presented with bold, expansive breathing room, while raw code data and logs utilize high-density, condensed layouts. We break the template by using overlapping glass layers and tonal depth to guide the eye, ensuring the "prepr" experience feels like a custom-built IDE rather than a generic web app.

---

## 2. Colors: Tonal Depth & The "No-Line" Rule
Our palette is rooted in a deep, nocturnal foundation. The objective is to eliminate visual noise by removing harsh separators and replacing them with environmental transitions.

*   **Primary (`#39b8fd`):** The "Active Pulse." Used sparingly for focus states and primary actions.
*   **Secondary (`#69f6b8`):** The "Success Signal." Used for passing builds and healthy code metrics.
*   **Tertiary (`#ff6f7e`):** The "Critical Alert." Used for security vulnerabilities and breaking errors.

### The "No-Line" Rule
Explicitly prohibit 1px solid borders for sectioning. Boundaries must be defined solely through background color shifts or subtle tonal transitions.
*   **Implementation:** A `surface-container-low` section sitting on a `surface` background provides all the separation a professional eye needs. Do not "box" the user in; let the content define the space.

### Surface Hierarchy & Nesting
Treat the UI as a series of physical layers—like stacked sheets of frosted glass.
*   **Base Layer:** `background` (#060e20).
*   **Secondary Context:** `surface-container-low` for sidebars and navigation backgrounds.
*   **Active Workspace:** `surface-container` or `surface-container-high` for main dashboard cards.
*   **Floating Elements:** `surface-bright` for modals and tooltips.

### The "Glass & Gradient" Rule
To move beyond a flat UI, main CTAs and "Hero" data points must use a subtle gradient transition from `primary` to `primary_container`. For floating panels, use a `backdrop-blur` of 12px–20px combined with a semi-transparent `surface_variant` (alpha 40%) to create a "frosted" effect that feels integrated into the environment.

---

## 3. Typography: Editorial Precision
The typography system pairs the technical precision of Inter with the structured geometry of Space Grotesk.

*   **The Power Couple:** 
    *   **Space Grotesk (Display/Headline):** Used for top-level metrics and page titles. Its idiosyncratic letterforms provide the "signature" brand feel.
    *   **Inter (Body/Label/Title):** Used for all functional data. Its high x-height ensures readability at the high information densities required for code analysis.
*   **Visual Hierarchy:**
    *   **Display-LG (3.5rem):** Reserved for "Hero" metrics (e.g., a Global Security Score).
    *   **Label-SM (0.6875rem):** All-caps with 5% letter-spacing for metadata and "micro-labels" (e.g., "COMMIT HASH" or "TIMESTAMP").

---

## 4. Elevation & Depth: Tonal Layering
In "The Forensic Architect," shadows are rarely black; they are "environmental."

*   **The Layering Principle:** Depth is achieved by "stacking." Place a `surface-container-lowest` card on a `surface-container-low` section. This creates a natural "recessed" or "elevated" feel without a single border.
*   **Ambient Shadows:** For floating modals, use a shadow with a 32px blur and 6% opacity. The shadow color should be derived from the `on-surface` token (a tinted navy) rather than pure black.
*   **The "Ghost Border" Fallback:** If a border is required for accessibility, use the `outline-variant` token at **15% opacity**. This creates a "glint" on the edge of the glass rather than a hard line.
*   **Glassmorphism:** Apply to navigation bars and floating filters. Use `surface_container_high` at 60% opacity with a heavy background blur to allow code-highlights to bleed through the UI as the user scrolls.

---

## 5. Components: Custom Primitives

### Buttons
*   **Primary:** Gradient fill (`primary` to `primary_container`), `md` roundedness, no border.
*   **Secondary:** Ghost style. No background, `outline` token at 20% opacity. Text in `primary`.
*   **State:** On hover, primary buttons should "glow" using a soft 12px shadow of the `primary` color at 30% opacity.

### Cards & Lists
*   **Forbid Divider Lines:** Use vertical white space (Spacing `4` or `6`) or a `surface-container-lowest` background to separate list items.
*   **Data Density:** Use `body-sm` for list descriptions to maximize information per square inch.

### Input Fields
*   **Styling:** Use `surface_container_lowest` for the fill. The active state is indicated by a 1px "Ghost Border" of the `primary` color and a subtle `surface_tint` inner glow.

### Signature Component: The "Analysis Monolith"
A specialized dashboard card for "prepr."
*   **Background:** `surface-container-highest` with a `primary` top-border accent (2px).
*   **Content:** Combines a `Space Grotesk` Headline-SM with a monospaced code snippet block (Inter) nested in a `surface-container-lowest` sub-well.

---

## 6. Do's and Don'ts

### Do:
*   **Do** use intentional asymmetry. A wide left column for code and a narrow right column for metadata creates an editorial feel.
*   **Do** use `secondary` (emerald) and `tertiary` (rose) for status indicators, but keep them small (dots or thin 2px bars).
*   **Do** use the Spacing Scale strictly. Align everything to the `0.4rem` (2) or `0.9rem` (4) increments to maintain mathematical harmony.

### Don't:
*   **Don't** use 100% opaque, high-contrast borders. It shatters the "glass" illusion.
*   **Don't** use standard "Drop Shadows" from a UI kit. Shadows must be large, soft, and tinted.
*   **Don't** overcrowd the `display` type. Give the `Space Grotesk` headlines room to breathe so they act as anchors for the eye.
*   **Don't** use pure white (#FFFFFF) for text. Always use `on_surface` or `on_surface_variant` to reduce eye strain in Developer Dark Mode.