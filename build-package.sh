#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_REL="Jellyfin.Plugin.CuratedHome/Jellyfin.Plugin.CuratedHome.csproj"
DIST_DIR="$ROOT_DIR/dist"
PUBLISH_DIR="$DIST_DIR/publish"
ZIP_PATH="$DIST_DIR/Jellyfin.Plugin.CuratedHome.zip"
DOTNET_IMAGE="mcr.microsoft.com/dotnet/sdk:9.0"

mkdir -p "$PUBLISH_DIR"
rm -rf "$PUBLISH_DIR"/* "$ZIP_PATH"

build_with_dotnet() {
    dotnet publish "$ROOT_DIR/$PROJECT_REL" -c Release -o "$PUBLISH_DIR"
}

build_with_container() {
    local runtime="$1"
    "$runtime" run --rm \
        -v "$ROOT_DIR:/work" \
        -w /work \
        "$DOTNET_IMAGE" \
        dotnet publish "$PROJECT_REL" -c Release -o /work/dist/publish
}

if command -v dotnet >/dev/null 2>&1; then
    build_with_dotnet
elif command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
    build_with_container docker
elif command -v podman >/dev/null 2>&1; then
    build_with_container podman
else
    echo "No usable dotnet, docker, or podman runtime found." >&2
    exit 1
fi

python3 - <<'PY' "$PUBLISH_DIR" "$ZIP_PATH"
import pathlib
import sys
import zipfile

publish_dir = pathlib.Path(sys.argv[1])
zip_path = pathlib.Path(sys.argv[2])

with zipfile.ZipFile(zip_path, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(publish_dir.rglob('*')):
        if path.is_file():
            archive.write(path, path.relative_to(publish_dir))
PY

echo "Built publish output at $PUBLISH_DIR"
echo "Packed plugin zip at $ZIP_PATH"
