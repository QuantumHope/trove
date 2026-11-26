# Trove

An ingestion hub for media I want to consume or track: what to read, watch, play and listen to, what I own, what I'm in
the middle of, what I've finished, and how the year's goals are going. Private, on my tailnet, with no social layer.

Part of the [HopeLab](https://github.com/QuantumHope/HopeLab) bench.

## Status

The layout, navigation and the 24 shared HopeLab themes, plus the first feature: **full playthroughs**
(Library → Full playthroughs, `/library/youtube`). Trove reads the YouTube channels in `YouTube:Channels`, keeps only
full-game videos (not "Part 3" uploads), and shows them on a board: New, Watch next, In progress, Done and Ignored.
Videos in progress or done take a 0.25 to 5 star rating; moving one to Done asks for the rating and records the date.

## YouTube API key

Trove reads channels with the YouTube Data API v3, which needs a free API key. The key stays on the machine that runs
Trove; it is never committed.

1. In the [Google Cloud console](https://console.cloud.google.com/), create a project (or pick one).
2. Under **APIs & Services → Library**, enable **YouTube Data API v3**.
3. Under **APIs & Services → Credentials**, choose **Create credentials → API key**. Restrict it to the YouTube Data
   API v3.
4. Paste it into **Settings → YouTube** in Trove. Trove tries it against YouTube before saving it, keeps it encrypted
   (ASP.NET Core Data Protection) and starts loading the channels straight away. Or set it outside the app, in user
   secrets or the environment variable `YouTube__ApiKey`:

   ```bash
   dotnet user-secrets set "YouTube:ApiKey" "your-key" --project src/Trove.Web
   ```

   A key saved in Settings takes the place of one in configuration.
5. Trove syncs at start and every 6 hours (`YouTube:SyncInterval`); **Sync now** on the board runs one
   on demand.

The channel list is `YouTube:Channels` in `src/Trove.Web/appsettings.json` (handles such as `@MKIceAndFire`).

## Storage

With `ConnectionStrings:Trove` set, Trove uses that Postgres database (schema `trove`) and applies its migrations at
start. Without it, Trove keeps a local SQLite file at `App_Data/trove.db` (or `Trove:SqlitePath`).

## Run it

```bash
dotnet run --project src/Trove.Web   # http://localhost:5317
dotnet test Trove.slnx
```

Needs the .NET 10 SDK. See `CLAUDE.md` for the layout and the decisions behind it.

![Home](docs/screenshots/home.png)
