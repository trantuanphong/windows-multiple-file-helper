#!/bin/bash
# Cài đặt FilePilot Quick Actions cho macOS Finder

INSTALL_DIR="$HOME/.local/bin"
SERVICES_DIR="$HOME/Library/Services"

mkdir -p "$INSTALL_DIR"
cp filepilot-cli "$INSTALL_DIR/filepilot"
chmod +x "$INSTALL_DIR/filepilot"

echo "Đã cài đặt binary FilePilot tại $INSTALL_DIR/filepilot"
echo "Để tạo Quick Action trong macOS Automator:"
echo "1. Mở Automator -> Tạo mới 'Quick Action'."
echo "2. Workflow receives current: 'files or folders' in 'Finder'."
echo "3. Kéo 'Run Shell Script' vào với nội dung:"
echo "   $INSTALL_DIR/filepilot --action=numbering \"\$@\""
