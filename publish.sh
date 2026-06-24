#!/bin/bash
# Publish script for Another Color Picker
# Creates a self-contained single-file Linux executable

set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$PROJECT_DIR/publish"

echo "🎨 Building Another Color Picker..."
echo "=================================="

# Clean previous builds
rm -rf "$OUTPUT_DIR"

# Publish self-contained single-file executable
dotnet publish "$PROJECT_DIR/AnotherColorPicker/AnotherColorPicker.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o "$OUTPUT_DIR"

echo ""
echo "✅ Build complete!"
echo "📦 Output: $OUTPUT_DIR/AnotherColorPicker"
echo ""

# Make executable
chmod +x "$OUTPUT_DIR/AnotherColorPicker"

# Show file size
ls -lh "$OUTPUT_DIR/AnotherColorPicker"

echo ""
echo "Run with: $OUTPUT_DIR/AnotherColorPicker"
