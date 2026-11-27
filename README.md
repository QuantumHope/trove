# Trove

An ingestion hub for media I want to consume or track: what to read, watch, play and listen to, what I own, what I'm in
the middle of, what I've finished, and how the year's goals are going. Private, on my tailnet, with no social layer.

Part of the [HopeLab](https://github.com/QuantumHope/HopeLab) bench.

## How it fits with CORA

Trove is only the app. Its data, the YouTube sync and the API live in CORA's Trove module (CORA ADR-126), and CORA
serves this app at `/trove/`. So Trove works on any device that can reach CORA, signs in with CORA's sign-in, and
needs no database of its own. Its settings (CORA's address, the YouTube key and the channel list) are kept by the module
and changed in CORA under **Admin → Modules → Trove**. If the module doesn't know CORA's address yet, Trove asks for it
when it starts, filled in with the site's own address. CORA's assistant can search the board too.

## Status

The layout, navigation and the 24 shared HopeLab themes, plus the first feature: **full playthroughs**
(Library → Full playthroughs, `/trove/library/youtube`). CORA reads the YouTube channels on its Trove module's list,
keeps the videos each channel's rule allows (full games only, not "Part 3" uploads, or any video of 20 minutes or more), and Trove shows them on a board: New, Watch next, In progress, Done and
Ignored. Videos in progress or done take a 0.25 to 5 star rating; moving one to Done asks for the rating and records
the date. Each CORA account has its own board.

## YouTube API key

CORA's Trove module reads channels with the YouTube Data API v3, which needs a free API key. It stays on the CORA
server and is never committed.

1. In the [Google Cloud console](https://console.cloud.google.com/), create a project (or pick one).
2. Under **APIs & Services → Library**, enable **YouTube Data API v3**.
3. Under **APIs & Services → Credentials**, choose **Create credentials → API key**. Restrict it to the YouTube Data
   API v3.
4. In CORA, open **Admin → Modules**, enable Trove and paste the key into its YouTube card. CORA tries it against
   YouTube before saving it and starts loading the channels. Or set `YouTube:ApiKey` in the `CORA.Trove` project's user
   secrets; a key saved in Admin takes its place.
5. Add channels in the same card by @handle, channel link or a link to one of their videos.

## Build it

```bash
dotnet build Trove.slnx
dotnet test Trove.slnx
dotnet publish src/Trove.App -c Release -o artifacts/trove
```

Point CORA at the published app by setting `Trove:StaticFilesPath` on `cora-api` to `artifacts/trove/wwwroot` (the
full path), then open `/trove/` on CORA. Needs the .NET 10 SDK. See `CLAUDE.md` for the layout and the decisions
behind it.

![Home](docs/screenshots/home.png)
