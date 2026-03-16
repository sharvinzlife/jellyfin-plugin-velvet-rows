# Velvet Rows

![Release](https://img.shields.io/github/v/release/sharvinzlife/jellyfin-plugin-velvet-rows?display_name=tag&label=release)
![Build](https://img.shields.io/github/actions/workflow/status/sharvinzlife/jellyfin-plugin-velvet-rows/build.yml?label=build)

Velvet Rows is a Jellyfin server plugin that adds elegant, configurable home rails on top of [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections) and rich browse destinations through [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages).

This community branch turns the personalized Malayalam pack into a reusable focused-language pack. You choose one primary language for movies and TV, then pair it with English movie and TV shelves.

## What this branch adds

- `My Media` pinned to the top for every user
- Focused-language shelves for:
  - `Newly Added`
  - `Newly Released`
  - `Romance and Love`
  - `Thriller and Suspense`
  - `Action and Adventure`
  - `Comedy`
  - `Crime and Mystery`
  - `Family`
  - `Mystery`
- English movie and TV shelves with the same curated logic
- Dedicated Explore pages for focused-language movies, focused-language TV, English movies, and English TV
- Cleaner filtering that hides filename-style junk from curated rows

## Good fits

Use this branch when your Jellyfin server centers around a language pack such as:

- Malayalam
- Tamil
- Hindi
- Japanese
- Spanish
- Korean

## Configuration model

Velvet Rows on this branch is driven by:

- `FocusedLanguageDisplayName`
- `FocusedMovieLibraryIds`
- `FocusedTvLibraryIds`
- `FocusedTvMatchTerms`
- `EnglishMovieLibraryIds`
- `EnglishTvLibraryIds`

`FocusedTvMatchTerms` is especially useful when a shared TV library contains mixed languages.

## Requirements

- Jellyfin `10.11.x`
- [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections)
- [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages)

## Build

From the repo root:

```bash
./build-package.sh
```

The packaged plugin zip is written to:

- `dist/Jellyfin.Plugin.CuratedHome.zip`

## Branch layout

- `main`: Sharvin's production-focused branch with live OCI deployment history
- `codex/community-language-packs`: public/community variant with one configurable focused-language pack plus English

## Repository

- GitHub: [sharvinzlife/jellyfin-plugin-velvet-rows](https://github.com/sharvinzlife/jellyfin-plugin-velvet-rows)
