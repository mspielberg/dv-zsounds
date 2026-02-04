#!/bin/bash

# Packaging script for Linux/Unix systems
# Equivalent to package.ps1

set -e  # Exit on error

# Parse arguments
NO_ARCHIVE=false
OUTPUT_DIRECTORY="$(cd "$(dirname "$0")" && pwd)"

while [[ $# -gt 0 ]]; do
    case $1 in
        --no-archive)
            NO_ARCHIVE=true
            shift
            ;;
        --output-directory)
            OUTPUT_DIRECTORY="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

cd "$OUTPUT_DIRECTORY"

# Files to include in the package
FILES_TO_INCLUDE=(
    "info.json"
    "build"
    "LICENSE"
    "resources/Sounds"
)

# Read mod info from info.json using grep and sed (no jq required)
MOD_ID=$(grep -oP '"Id"\s*:\s*"\K[^"]+' info.json)
MOD_VERSION=$(grep -oP '"Version"\s*:\s*"\K[^"]+' info.json)

DIST_DIR="$OUTPUT_DIRECTORY/dist"

if [ "$NO_ARCHIVE" = true ]; then
    ZIP_WORK_DIR="$OUTPUT_DIRECTORY"
else
    ZIP_WORK_DIR="$DIST_DIR/tmp"
fi

ZIP_OUT_DIR="$ZIP_WORK_DIR/$MOD_ID"

# Create output directory
mkdir -p "$ZIP_OUT_DIR"

# Copy info.json and LICENSE directly
cp -f "info.json" "$ZIP_OUT_DIR/"
cp -f "LICENSE" "$ZIP_OUT_DIR/"

# Copy DLL from build folder to root (not the build folder itself)
if [ -f "build/ZSounds.dll" ]; then
    cp -f "build/ZSounds.dll" "$ZIP_OUT_DIR/"
else
    echo "Warning: build/ZSounds.dll not found"
fi

# Copy Sounds folder from resources
if [ -d "resources/Sounds" ]; then
    cp -rf "resources/Sounds" "$ZIP_OUT_DIR/"
else
    echo "Warning: resources/Sounds not found"
fi

# Create archive if not disabled
if [ "$NO_ARCHIVE" = false ]; then
    mkdir -p "$DIST_DIR"
    FILE_NAME="$DIST_DIR/${MOD_ID}_${MOD_VERSION}.zip"
    
    # Remove old archive if exists
    rm -f "$FILE_NAME"
    
    # Create new archive with maximum compatibility
    # -r = recurse into directories
    # -9 = maximum compression
    # -X = exclude extra file attributes (for better Windows compatibility)
    # Using . instead of ./* to avoid issues with some unzip tools
    cd "$ZIP_OUT_DIR"
    zip -r -9 -X "$FILE_NAME" .
    
    echo "Package created: $FILE_NAME"
    
    # Clean up temporary directory
    cd "$OUTPUT_DIRECTORY"
    rm -rf "$ZIP_WORK_DIR"
fi
