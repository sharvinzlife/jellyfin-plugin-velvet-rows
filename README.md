# Velvet Rows

![Velvet Rows logo](./assets/velvet-rows-logo.svg)

Velvet Rows is a Jellyfin companion plugin that publishes curated homepage rails for the collections you actually care about.

It is designed to pair with [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections) and [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages), then surface shelves such as:

- Malayalam Movies - Newly Added
- Malayalam Movies - Newly Released
- Malayalam TV Shows - Newly Added
- Malayalam TV Shows - Newly Released
- English Movies - Newly Added
- English Movies - Newly Released
- Tamil Movies - Newly Added
- Tamil Movies - Newly Released

## Why it exists

Plex-style discovery rails feel great, but Jellyfin home screens often need more curation when media is split across language-specific libraries or mixed libraries. Velvet Rows gives you focused shelves without forking the whole home screen experience.

## Highlights

- Dedicated movie shelves for English, Malayalam, and Tamil libraries
- Metadata-aware Malayalam TV shelves that can filter a shared TV library using terms like `malayalam` and `മലയാളം`
- Polished plugin settings UI inside Jellyfin
- GitHub Actions for CI builds and tagged zip releases
- Custom Jellyfin plugin repository manifest for simpler future installs and updates

## Release logic

- `Newly Added`: sorted by recent library additions
- `Newly Released` for movies: sorted by premiere date
- `Newly Released` for Malayalam TV: sorted by series premiere date

## Configuration

Velvet Rows exposes a management page inside Jellyfin where you can configure:

- row size limit
- English movie library IDs
- Malayalam movie library IDs
- Tamil movie library IDs
- Malayalam TV source library IDs
- Malayalam TV metadata match terms

If you do not have a dedicated Malayalam TV library yet, point the TV field at your general TV library or leave it blank to search all TV libraries, then keep the default match terms.

## Dependencies

Install these first:

- [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections)
- [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages)

## Jellyfin repository manifest

Add this URL to Jellyfin plugin repositories:

```text
https://raw.githubusercontent.com/sharvinzlife/jellyfin-plugin-velvet-rows/main/manifest.json
```

## Build

```bash
./build-package.sh
```

The script prefers a local `dotnet` SDK. If that is not available, it falls back to a working `docker` or `podman` runtime.

## Install

1. Build the plugin.
2. Copy the published plugin files into a Jellyfin plugin directory.
3. Restart Jellyfin.
4. Open `Dashboard -> Plugins -> Velvet Rows` and set your library IDs and TV match terms.
5. Open Home Screen Sections to position the curated rails.

## Repository layout

- `Jellyfin.Plugin.CuratedHome/`: server plugin source
- `assets/`: brand assets
- `.github/workflows/`: build and release automation
- `manifest.json`: Jellyfin plugin repository manifest
- `build-package.sh`: local or containerized build helper

## License

MIT
