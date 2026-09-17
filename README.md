# jellyfin-plugin-kinopoisk

Jellyfin metadata provider for [КиноПоиск](https://www.kinopoisk.ru/).

This fork is maintained for Jellyfin 12.x and currently publishes builds for:

- Jellyfin 12.0
- Jellyfin 12.1
- .NET 10

## Install

In Jellyfin go to **Dashboard → Plugins → Repositories** and add:

```text
https://raw.githubusercontent.com/maestronix/jellyfin-plugin-kinopoisk/master/manifest.json
```

Then install **КиноПоиск** from the Metadata category.

## Configuration

Plugin settings are available under **Dashboard → Plugins → My Plugins → КиноПоиск → Settings**.

The plugin can use an API token from `kinopoiskapiunofficial.tech`. A personal token is recommended for larger libraries because the shared token is rate-limited.

## Metadata

The plugin supports movies and series and can fetch:

- rating
- descriptions
- posters and backdrops
- actors and staff
- trailers available through YouTube
- Kinopoиск IDs from `kp-12345` / `kp12345` in filenames or folder names

## Builds

GitHub Actions builds and tests the plugin against both Jellyfin 12.0 and 12.1. Each build is published as a GitHub Release and added to `manifest.json`, so Jellyfin can select the matching ABI automatically.

The committed API client is used for normal builds. OpenAPI client regeneration is opt-in with `GenerateKinopoiskApiClient=true`.
