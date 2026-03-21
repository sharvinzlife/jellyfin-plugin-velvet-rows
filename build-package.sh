#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_REL="Jellyfin.Plugin.CuratedHome/Jellyfin.Plugin.CuratedHome.csproj"
DIST_DIR="$ROOT_DIR/dist"
DEFAULT_PUBLISH_DIR="$DIST_DIR/publish"
FALLBACK_PUBLISH_DIR="$DIST_DIR/publish-staging"
PUBLISH_DIR="$DEFAULT_PUBLISH_DIR"
ZIP_PATH="$DIST_DIR/Jellyfin.Plugin.CuratedHome.zip"
DOTNET_IMAGE="mcr.microsoft.com/dotnet/sdk:9.0"

mkdir -p "$DIST_DIR"

clean_path() {
    local path="$1"
    local relative_path="${path#"$ROOT_DIR"/}"
    rm -rf "$path" 2>/dev/null || true
    if [ -e "$path" ] && command -v podman >/dev/null 2>&1; then
        podman unshare rm -rf "$path" 2>/dev/null || true
        if [ -e "$path" ]; then
            podman run --rm \
                -v "$ROOT_DIR:/work" \
                "$DOTNET_IMAGE" \
                sh -lc "rm -rf /work/$relative_path" 2>/dev/null || true
        fi
    fi
    rm -rf "$path" 2>/dev/null || true
    [ ! -e "$path" ]
}

clean_dist() {
    clean_path "$PUBLISH_DIR" || return 1
    clean_path "$ZIP_PATH" || return 1
    mkdir -p "$PUBLISH_DIR"
}

prepare_output_paths() {
    if clean_dist; then
        return 0
    fi

    PUBLISH_DIR="$FALLBACK_PUBLISH_DIR"
    if ! clean_path "$PUBLISH_DIR"; then
        echo "Unable to prepare $DEFAULT_PUBLISH_DIR or fallback $FALLBACK_PUBLISH_DIR" >&2
        exit 1
    fi

    mkdir -p "$PUBLISH_DIR"
    echo "Falling back to $PUBLISH_DIR because $DEFAULT_PUBLISH_DIR could not be cleaned." >&2
}

strip_publish_runtimes() {
    rm -rf "$PUBLISH_DIR/runtimes"
}

copy_source_tree() {
    local destination="$1"
    mkdir -p "$destination"
    tar -C "$ROOT_DIR" \
        --exclude=.git \
        --exclude=dist \
        --exclude='Jellyfin.Plugin.CuratedHome/bin' \
        --exclude='Jellyfin.Plugin.CuratedHome/obj' \
        --exclude='._*' \
        -cf - . | tar -C "$destination" -xf -
}

prepare_output_paths

build_with_dotnet() {
    dotnet publish "$ROOT_DIR/$PROJECT_REL" -c Release -o "$PUBLISH_DIR"
}

build_with_container() {
    local runtime="$1"
    local temp_build_root
    temp_build_root="$(mktemp -d "${TMPDIR:-/tmp}/velvet-rows-build.XXXXXX")"
    copy_source_tree "$temp_build_root"

    "$runtime" run --rm \
        -e HOME=/tmp/dotnet-home \
        -v "$temp_build_root:/work" \
        -w /work \
        "$DOTNET_IMAGE" \
        dotnet publish "$PROJECT_REL" -c Release -o /work/dist/publish

    cp -r "$temp_build_root/dist/publish/." "$PUBLISH_DIR"/
    cp "$temp_build_root/assets/velvet-rows-logo.png" "$PUBLISH_DIR/logo.png"
    rm -rf "$PUBLISH_DIR/runtimes"
    rm -rf "$temp_build_root" 2>/dev/null || true
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

strip_publish_runtimes

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
