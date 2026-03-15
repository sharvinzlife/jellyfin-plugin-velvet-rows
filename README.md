# Velvet Rows

![Release](https://img.shields.io/github/v/release/sharvinzlife/jellyfin-plugin-velvet-rows?display_name=tag&label=release)
![Build](https://img.shields.io/github/actions/workflow/status/sharvinzlife/jellyfin-plugin-velvet-rows/build.yml?branch=main&label=build)

![Velvet Rows logo](./assets/velvet-rows-logo.svg)

Velvet Rows is a Jellyfin companion plugin that publishes curated homepage rails for the collections you actually care about.

It is designed to pair with [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections) and [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages), then surface shelves such as:

- Malayalam Movies - Newly Added
- Malayalam Movies - Newly Released
- Malayalam TV Shows - Newly Added
- Malayalam TV Shows - Newly Released
- English Movies - Newly Added
- English Movies - Newly Released
- English TV Shows - Newly Added
- English TV Shows - Newly Released

## Why it exists

 Plex-style discovery rails feel great, but Jellyfin home screens often need more curation when media is split across language-specific libraries or mixed libraries. Velvet Rows gives you focused shelves without forking the whole home screen experience.

## Highlights

- Dedicated movie shelves for English and Malayalam libraries
- English TV shelves for your main TV library
- Metadata-aware Malayalam TV shelves that can filter a shared TV library or use a dedicated Malayalam TV library
- Smarter newly released sorting with fallback to production year and creation time when release metadata is sparse
- Polished plugin settings UI inside Jellyfin
- GitHub Actions for CI builds and tagged zip releases
- Custom Jellyfin plugin repository manifest for simpler future installs and updates

## Release logic

- `Newly Added`: sorted by recent library additions
- `Newly Released` for movies and shows: sorted by premiere date first, then production year, then creation time as a fallback

## Configuration

Velvet Rows exposes a management page inside Jellyfin where you can configure:

- row size limit
- English movie library IDs
- English TV library IDs
- Malayalam movie library IDs
- Malayalam TV source library IDs
- Malayalam TV metadata match terms

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
