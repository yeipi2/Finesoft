# Interface System (Project-Only)

## Direction and Feel
- Compact, decision-first action bars for detail screens.
- Primary action visible; secondary actions grouped in a right-aligned `Acciones` dropdown.
- Keep visual weight low to avoid oversized button rows.

## Depth Strategy
- Borders-first plus subtle panel elevation (`shadow-sm` equivalent) in detail views.
- Use stronger border contrast and soft layered shadows for cards that need separation.
- Keep action area compact; avoid heavy floating controls.

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

## Invoice Details Visual Pattern
- Use a light neutral surface system with subtle borders (`--invd-*` tokens) and avoid saturated full-width headers.
- Top area uses a compact summary panel with breadcrumb, status badge, quick chips (fecha/total/saldo/tipo), and compact actions.
- Main content uses soft "panel" sections (`.invd-panel`) with thin borders, 12-14px radius, and restrained icon badges.
- Tables should be `table-sm`, uppercase muted headers, and low-contrast row separators.
- Sidebar follows the same panel language: key-value list, metric micro-cards, and payment-state callout card.

## Cross-Details Styling
- All `*Details` pages (`Invoice`, `Quote`, `Ticket`, `Project`) use page wrappers with soft radial background, rounded container, stronger border contrast, and subtle layered shadows.
- Header sections use compact "hero" containers instead of floating title + buttons.
- Existing Bootstrap semantic headers (`bg-primary`, `bg-success`, etc.) are softened to tinted backgrounds with dark text for readability and reduced visual noise.
- Quote and Ticket detail child components follow panel primitives (`qd-*` / `td-*`) with the same hierarchy style used in invoice details.

## Modal Forms (Dark Mode)
- Dark modal form wrappers (`.rz-dialog-wrapper`) use a blue-slate palette for all form surfaces to avoid mixed hue perception.
- Keep modal/form tokens aligned: `--fsd-surface-0`, `--fsd-surface-1`, `--fsd-surface-2`, `--fsd-border`, `--fsd-text`, `--fsd-muted`, `--fsd-hover`.
- Form section header/body/footer, autocomplete panel, and light badges inside modals must consume the same token family.
- Focus states in modal form controls use blue ring accents only; avoid purple-tinted dark surfaces.
