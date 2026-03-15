# Velvet Rows

![Velvet Rows logo](./assets/velvet-rows-logo.svg)

Velvet Rows is a Jellyfin companion plugin that adds curated homepage rails for the collections you actually care about.

It is built to pair with [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections) and [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages), then publish focused rows such as:

- Malayalam Movies - Newly Added
- Malayalam Movies - Newly Released
- English Movies - Newly Added
- English Movies - Newly Released
- Malayalam TV Shows - Newly Added
- Malayalam TV Shows - Newly Released

## Why it exists

Plex-style discovery rails feel great, but Jellyfin home screens often need more curation when you split media across language-specific libraries. Velvet Rows gives you language-aware shelves without forking the whole Jellyfin home experience.

## Release logic

- `Newly Added`: sorted by the latest library additions.
- `Newly Released` for movies: sorted by premiere date.
- `Newly Released` for Malayalam TV: sorted by series premiere date.

## Configuration

Velvet Rows exposes a polished management page inside Jellyfin where you can configure comma-separated library IDs for:

- English movie libraries
- Malayalam movie libraries
- Malayalam TV libraries

If you do not have a dedicated Malayalam TV library yet, leave that field empty until you add one.

## Dependencies

Install these first:

- [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections)
- [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages)

## Build

```bash
./build-package.sh
```

The script prefers a local `dotnet` SDK. If that is not available, it falls back to a working `docker` or `podman` runtime.

## Install

1. Build the plugin.
2. Copy the published plugin files into a Jellyfin plugin directory.
3. Restart Jellyfin.
4. Open `Dashboard -> Plugins -> Velvet Rows` and set your library IDs.
5. Open Home Screen Sections to position the new rails where you want them.

## Repository layout

- `Jellyfin.Plugin.CuratedHome/`: server plugin source
- `assets/`: brand assets
- `build-package.sh`: local or containerized build helper

## Notes

The assembly namespace remains `Jellyfin.Plugin.CuratedHome` for compatibility, but the product name presented to Jellyfin users is `Velvet Rows`.
