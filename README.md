# Universal Link Peeker

Universal Link Peeker is a powerful utility for Windows that allows you to preview any link instantly without opening a new browser tab. Simply hover over a link and hold your configured trigger key to see a live preview.

## Features

- **Instant Preview**: Hover over any link (in any application) and hold the activation key to see a popup preview.
- **Configurable Trigger**: Choose between `Shift` (default), `Ctrl`, or `Alt` keys via the system tray menu.
- **Security First**: 
  - Automatically blocks file downloads (`.exe`, `.zip`, etc.) to prevent malware.
  - Prevents new windows/popups from opening within the preview.
- **Scroll Support**: Use your mouse wheel to scroll up and down within the preview window without activating it.
- **Copy Link**: Press `C` while previewing to copy the URL to your clipboard.
- **Smart Positioning**: The preview window automatically positions itself near your mouse but stays within screen bounds.

## Installation

1. Download the latest release (`UniversalLinkPeeker-v1.0.0.zip`).
2. Extract the ZIP file to a folder of your choice.
3. Run `UniversalLinkPeeker.exe`.

## Usage

1. The application runs in the background. You will see an icon in the system tray.
2. **To Preview**: Hover your mouse over a link in any application (browser, document, chat app, etc.).
3. **Hold Shift** (or your configured key). The preview window will appear.
4. **Scroll**: Use the mouse wheel to scroll the page.
5. **Copy**: Press `C` to copy the link.
6. **Release Key**: Release the key to close the preview.

## Configuration

Right-click the system tray icon to:
- Change the **Trigger Key** (Shift, Ctrl, Alt).
- Exit the application.

## Requirements

- Windows 10 or Windows 11.
- WebView2 Runtime (usually pre-installed on modern Windows).

## License

MIT License.
