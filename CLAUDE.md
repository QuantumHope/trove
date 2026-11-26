# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Trove is a private, single-person media queue: what to read, watch, play and listen to (books in any format, YouTube
from chosen creators, movies and TV, games, board games, and articles from CORA's reading list), with status, ownership
and yearly goals. No social features. It is part of the HopeLab bench (`QuantumHope/HopeLab`), which launches it from
`hopelab-app.json`.

Today it is a shell: layout, navigation, placeholder screens and the shared themes. No database, sign-in or external
APIs yet.

## Commands

```bash
dotnet build Trove.slnx
dotnet test Trove.slnx
dotnet run --project src/Trove.Web     # http://localhost:5317
```

## Layout

- `src/Trove.Web` is the ASP.NET Core server. It serves the Blazor web app and will host the API. It listens on
  **localhost only** (`Urls` in `appsettings.json`); `tailscale serve` is the one way in and provides HTTPS, so there is
  no HTTPS redirect. The PWA pieces (`manifest.webmanifest`, `service-worker.js`, `icon.svg`) are in its `wwwroot`.
- `src/Trove.Web.Client` is the WebAssembly entry point. The whole app is interactive WebAssembly, prerendered on the
  server.
- `src/Trove.UI` is a Razor class library with every screen, the layout and the theming, so a MAUI Blazor Hybrid shell
  can reuse it later. Put new screens and components here, not in the two host projects. The one exception is
  `src/Trove.Web/Components/Pages/Error.razor`: the production error page reads the request, so it stays on the server and
  opts out of interactive routing.
- `src/Trove.Core` holds the model and the rules that need no UI or database (watch states, star ratings, the
  full-game title filter, what a move does to the rating and watched date). Both hosts and the UI reference it.
- `src/Trove.Web/Data` is EF Core: Postgres (schema `trove`, migrations in `Data/Migrations`) when
  `ConnectionStrings:Trove` is set, otherwise SQLite with `EnsureCreated`. Add a migration with
  `dotnet ef migrations add <Name> --project src/Trove.Web --output-dir Data/Migrations`.
- `src/Trove.Web/YouTube` reads the YouTube Data API (key in user secrets as `YouTube:ApiKey`, sent in the
  `X-Goog-Api-Key` header so it never lands in a logged URL) and syncs channels incrementally on a timer.
  `src/Trove.Web/Playthroughs` is the `/api/playthroughs` API the WebAssembly client calls.
- `tests/Trove.Tests` covers the theme catalog against the shared package, the layout with bUnit, and the server with
  `WebApplicationFactory`.

## Decisions already made

These were settled with the user on Nov 25-26, 2026 (design page in the HopeLab channel, scope on HopeLab issue 16):

- Its own repo, with its own schema in CORA's Postgres (one role and schema per service, as CORA's ADR-036 does).
- Blazor PWA now, MAUI Blazor Hybrid later.
- Trove is the source of truth; services are imports (Goodreads CSV, Audible CSV, Kindle page, ePub folder, Steam,
  YouTube creators plus rules, CORA's reading list). Metadata from Open Library, TMDB, IGDB and the YouTube Data API.
- Sign-in from the `Tailscale-User-Login` header that `tailscale serve` adds; refuse requests without it.
- Status (Wishlist, Up next, In progress, Done, Dropped) is separate from ownership (copies). A book counts once in any
  format.

## Themes

Trove wears the shared HopeLab themes from `MattEland.HopeLab.Themes` (0.1.*, like the other apps). That package is an
Avalonia library, so it is restored by `build/HopeLabThemes`, a project of its own that nothing references, and
`build/HopeLabThemes.targets` copies its `css/` folder into `src/Trove.UI/wwwroot/themes/` on every build (git-ignored).

- Change a theme in `MattEland.HopeLab.Shared`, never in the copied CSS.
- `src/Trove.UI/Theming/ThemeCatalog.cs` mirrors the package's catalog (which a web app can't load).
  `ThemeCatalogTests` fails when the package gains, renames or drops a theme; update the list to match.
- Style with the tokens (`--surface-*`, `--text-*`, `--accent-*`, `--border-*`, `--font-*`, `--type-*`, `--space-*`) in
  `src/Trove.UI/wwwroot/trove.css`. Don't hard-code colours, or one of the 24 themes will break.
- The shell and picker follow CORA's: the main areas as a tab strip in the header (a bottom bar below 768px), and a
  theme button that opens the theme list (System, then light themes, then dark, each with a thumbnail drawn in its own
  `data-theme`). `ThemePreference.cs` holds the rules; `ThemeToggle`, `ThemeList` and `ThemeThumbnail` draw them.
- The choice is kept in `localStorage` (`trove.theme`; nothing stored means System, which follows the device between
  Light and Dark) and applied by `theme.js` in `<head>` before first paint.
- The themes name faces (Inter, JetBrains Mono, VT323 and others) that the package carries only inside its Avalonia
  library, so the browser falls back to whatever the device has.

## Screenshots

Bring screenshots of every screen a change touches. `docs/screenshots/` holds the current set (desktop, a light theme,
a styled dark theme, and phone width).
