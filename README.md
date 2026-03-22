# Velvet Rows

![Release](https://img.shields.io/github/v/release/sharvinzlife/jellyfin-plugin-velvet-rows?display_name=tag&label=release)
![Build](https://img.shields.io/github/actions/workflow/status/sharvinzlife/jellyfin-plugin-velvet-rows/build.yml?branch=main&label=build)

![Velvet Rows logo](./assets/velvet-rows-logo.svg)

Velvet Rows is a Jellyfin companion plugin that publishes curated homepage rails for the collections you actually care about.

It is designed to pair with [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections) and [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages), then surface shelves such as:

- Malayalam Movies - Spotlight Mix
- Malayalam Movies - Wildcard Rotation
- Malayalam Movies - Recently Added
- Malayalam Movies - Latest
- Malayalam Movies - Romance and Love
- Malayalam Movies - Thriller and Suspense
- Malayalam Movies - Action and Adventure
- Malayalam Movies - Comedy
- Malayalam Movies - Crime and Mystery
- Malayalam Movies - Family
- Malayalam Movies - Mystery
- Malayalam TV Shows - Spotlight Mix
- Malayalam TV Shows - Wildcard Rotation
- Malayalam TV Shows - Recently Added
- Malayalam TV Shows - Latest
- English Movies - Spotlight Mix
- English Movies - Wildcard Rotation
- English Movies - Recently Added
- English Movies - Latest
- English Movies - Romance and Love
- English Movies - Thriller and Suspense
- English Movies - Action and Adventure
- English Movies - Comedy
- English Movies - Crime and Mystery
- English Movies - Family
- English Movies - Mystery
- English TV Shows - Spotlight Mix
- English TV Shows - Wildcard Rotation
- English TV Shows - Recently Added
- English TV Shows - Latest

## Why it exists

 Plex-style discovery rails feel great, but Jellyfin home screens often need more curation when media is split across language-specific libraries or mixed libraries. Velvet Rows gives you focused shelves without forking the whole home screen experience, then adds dedicated Explore pages for each library group.

## Highlights

- Dedicated movie shelves for English and Malayalam libraries
- Core shelves rotate mixed picks on every reload instead of repeating a fixed stable order
- Explicit `Recently Added` and `Latest` shelves stay available alongside the rotating mixes
- Genre-driven shelves for romance, thriller, action, comedy, crime, family, and mystery discovery
- Genre shelves always pull a rotating mixed set from the full matched library instead of ranking by newest titles
- English TV shelves for your main TV library
- Metadata-aware Malayalam TV shelves that can filter a shared TV library or use a dedicated Malayalam TV library
- Library-group Explore pages delivered through Plugin Pages
- Cleaner curated shelves that hide low-confidence filename-style titles
- Smarter newly released sorting that only trusts premiere dates and production year
- Stronger Home Screen Sections default syncing so existing and future users inherit the curated layout more reliably
- Polished plugin settings UI inside Jellyfin
- GitHub Actions for CI builds and tagged zip releases
- Custom Jellyfin plugin repository manifest for simpler future installs and updates

## Shelf logic

- `Spotlight Mix`: rotates through fresh additions, well-rated titles, library staples, and deeper catalog pulls
- `Wildcard Rotation`: swings wider across the full matched library so reloads surface more surprising combinations
- `Recently Added`: stays strict and library-addition-first for a familiar fresh-arrivals rail
- `Latest`: stays strict and release-date-first for people who want the newest releases fast
- Genre shelves: require clean metadata and matching Jellyfin genres, then use the same rotating mix engine inside that mood lane
- Low-confidence filename titles can be hidden from all curated shelves

## Configuration

Velvet Rows exposes a management page inside Jellyfin where you can configure:

- row size limit
- whether `My Media` is pinned to the top
- whether genre shelves are enabled
- whether low-confidence filename-like titles are hidden
- English movie library IDs
- English TV library IDs
- Malayalam movie library IDs
- Malayalam TV source library IDs
- Malayalam TV metadata match terms

## Explore pages

Velvet Rows also registers dedicated user-facing pages through Plugin Pages:

- Malayalam Movies Explore
- English Movies Explore
- Malayalam TV Explore
- English TV Explore

Each page bundles Spotlight Mix, Wildcard Rotation, explicit Recently Added and Latest shelves, and genre-led shelves for that library group.

## Client note

Velvet Rows works best anywhere Jellyfin is rendering the hosted web interface. That includes the official web client and LG webOS, whose official app is a wrapper around Jellyfin Web:

- [Jellyfin for webOS](https://github.com/jellyfin/jellyfin-webos)

## Jellyfin repository manifest

Add this URL to Jellyfin plugin repositories:

```text
https://raw.githubusercontent.com/sharvinzlife/jellyfin-plugin-velvet-rows/main/manifest.json
```

## Quick install

1. Add the repository manifest URL above to Jellyfin.
2. Open `Dashboard -> Plugins -> Catalog`.
3. Install `Velvet Rows`.
4. Keep `Home Screen Sections` and `Plugin Pages` enabled.
5. Open `Dashboard -> Plugins -> Velvet Rows` and set your library routing.
6. Reopen the web client or LG webOS app so the updated shelves and Explore pages are picked up.
7. Restart Jellyfin after changing My Media pinning or genre shelf publishing so the home layout re-registers cleanly.

## Build

```bash
./build-package.sh
```

The script prefers a local `dotnet` SDK. If that is not available, it falls back to a working `docker` or `podman` runtime.

## Repository layout

- `Jellyfin.Plugin.CuratedHome/`: server plugin source
- `assets/`: brand assets
- `.github/workflows/`: build and release automation
- `manifest.json`: Jellyfin plugin repository manifest
- `build-package.sh`: local or containerized build helper

## License

MIT
