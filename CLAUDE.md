# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Trove is a private, single-person media queue: what to read, watch, play and listen to (books in any format, YouTube
from chosen creators, movies and TV, games, board games, and articles from CORA's reading list), with status, ownership
and yearly goals. No social features. It is part of the HopeLab bench (`QuantumHope/HopeLab`), which launches it from
`hopelab-app.json`.

Trove is a client only. Its data model, the YouTube sync and the API are CORA's Trove module (`backend/CORA.Trove` in
`QuantumHope/CORA`, ADR-126), and CORA serves this app at `/trove/`. Trove has no database and no sign-in: CORA's
cookie covers it. Its settings (CORA's address, the YouTube key, the channel list) belong to the module and are changed
in CORA's Admin → Modules (ADR-131). The one Trove itself can set is CORA's address: `SetupPrompt` asks for it at start
when `api/setup` says it's missing, and Settings shows what is set.

## Commands

```bash
dotnet build Trove.slnx
dotnet test Trove.slnx
dotnet publish src/Trove.App -c Release -o artifacts/trove   # set Trove:StaticFilesPath on cora-api to artifacts/trove/wwwroot
```

## Layout

- `src/Trove.App` is the standalone WebAssembly app. `wwwroot/index.html` has `<base href="/trove/">`, and the PWA
  pieces (`manifest.webmanifest`, `service-worker.js`, `icon.svg`) sit beside it. `CoraApiHandler` puts
  `Accept: application/json` and `X-Requested-With: XMLHttpRequest` on every call (CORA refuses writes without the
  second, and only answers 401 instead of redirecting when the first is there), and sends a lapsed sign-in to CORA's
  `/signin?returnUrl=...`. It turns CORA's 409 (module off) and 502 (module unreachable) into readable messages.
- `src/Trove.UI` is a Razor class library with every screen, the layout and the theming, so a MAUI Blazor Hybrid shell
  can reuse it later. Put new screens and components here. `HttpPlaythroughLibrary` calls `api/playthroughs`, which is
  CORA's `/trove/api/playthroughs`. Links into CORA (such as `CoraLinks.Modules`) are root-relative, since CORA and
  Trove share an address.
- `src/Trove.Core` holds the model and the rules that need no UI (watch states, star ratings, the full-game title
  filter, what a move does to the rating and watched date). CORA's module keeps a copy of the wire types and rules; keep
  the two in step.
- `tests/Trove.Tests` covers the theme catalog against the shared package, the layout and the board with bUnit, and the
  rules.

## Decisions already made

These were settled with the user on Nov 25-26, 2026 (design page in the HopeLab channel, scope on HopeLab issue 16):

- Its own repo for the app. On Nov 26 the server moved into a CORA module (their words: "moving the server into a CORA
  module so you administer it from CORA and CORA can search its data but all front ends are in the client app").
- Blazor PWA now, MAUI Blazor Hybrid later.
- Trove is the source of truth; services are imports (Goodreads CSV, Audible CSV, Kindle page, ePub folder, Steam,
  YouTube creators plus rules, CORA's reading list). Metadata from Open Library, TMDB, IGDB and the YouTube Data API.
- Sign-in is CORA's; the module sees CORA's forwarded subject, so each account has its own board.
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
