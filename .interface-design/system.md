# Interface System (Project-Only)

## Direction and Feel
- Compact, decision-first action bars for detail screens.
- Primary action visible; secondary actions grouped in a right-aligned `Acciones` dropdown.
- Keep visual weight low to avoid oversized button rows.

## Depth Strategy
- Borders-first with subtle Bootstrap shadows only on dropdown menus.
- No heavy cards or large floating controls in the action area.

## Spacing Base Unit
- 8px rhythm.
- Action bars use `gap-2` (8px) and `btn-sm` sizing by default.

## Key Component Patterns
- Detail topbar pattern:
  - Optional contextual primary button (e.g., `Registrar pago`, `Aceptar`, `Facturar`, `Editar`).
  - `Acciones` dropdown for all secondary or destructive actions.
  - `Volver al listado` is inside the dropdown, not as a separate large row button.
- Destructive actions use `text-danger` in dropdown items.
- Action rows must wrap on mobile and remain right-aligned.
- Use concise labels and icon + text format for scanability.
