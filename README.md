# GlanceX

AI-powered real-time desktop screen translator.

GlanceX captures any region of your screen, recognizes text via Windows OCR, and translates it in real-time using OpenAI-compatible APIs.

## Features

- **Screen Region Capture** — Select any area of your desktop to translate
- **Windows OCR** — Built-in text recognition, no external dependencies
- **AI Translation** — Powered by OpenAI-compatible APIs (OpenAI, DeepSeek, Ollama, etc.)
- **Streaming Output** — See translation results appear in real-time
- **Floating Overlay** — Draggable, resizable, always-on-top translation window
- **Global Hotkeys** — `Ctrl+Alt+S` to select region, `Ctrl+Alt+T` to start/stop
- **System Tray** — Minimize to tray, quick access via right-click menu
- **Multi-language** — Supports 10+ source/target languages

## Tech Stack

- .NET 8 + WPF
- Windows.Media.Ocr (WinRT)
- OpenAI Chat Completions API (streaming SSE)
- CommunityToolkit.Mvvm
- Inno Setup (installer packaging)

## Quick Start

1. Download the latest release from [Releases](https://github.com/karsonto/GlanceX/releases)
2. Run `GlanceX_Setup_v*.exe` to install, or extract the portable zip
3. Open **Settings** (gear icon) and configure your API Key and Base URL
4. Click **Select Region** (or `Ctrl+Alt+S`) to pick a screen area
5. Click **Start Translation** (or `Ctrl+Alt+T`) to begin real-time translation

## Build from Source

```bash
# Clone
git clone https://github.com/karsonto/GlanceX.git
cd GlanceX

# Build
dotnet build DesktopTranslator/DesktopTranslator.csproj

# Run
dotnet run --project DesktopTranslator/DesktopTranslator.csproj
```

## Local Packaging

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php):

```bash
build.bat
```

The installer will be generated in the `installer/` folder.

## System Requirements

- Windows 10 (version 2004+) / Windows 11 (64-bit)
- ~200MB disk space
- OCR language packs (install via Windows Settings → Language)

## License

MIT
