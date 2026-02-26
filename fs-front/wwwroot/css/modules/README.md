# CSS Modules Guide

`app.css` is now a manifest that imports these files in cascade order.

## Editing rules

1. Keep imports ordered in `app.css`.
2. Add new styles to the closest module by responsibility.
3. Avoid `!important` unless overriding third-party CSS is unavoidable.
4. Prefer `.razor.css` for component-specific styles.

## Modules

- `00-core.css`: fonts, tokens, base Blazor styles, loading/error base.
- `01-shell.css`: app shell, sidebar, topbar, user dropdown, cards.
- `02-forms.css`: shared form wrappers and footer layout patterns.
- `03-public-quote.css`: public quote response page styles.
- `04-responsive.css`: responsive rules tied to shell/public layout.
- `05-dark-mode.css`: dark-theme global overrides.
- `06-form-sections-dialogs.css`: form sections, autocomplete, dialogs, dialog dark overrides.
- `07-profile.css`: profile page + profile dark-mode styles.
