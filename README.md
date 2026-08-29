# SixKeyToolbox

A toolbox for osu!mania 6K players. This application uses your browser as frontend, so it may look like a website at first glance, but it is a fully local desktop application.

> [!Note]
> Only supports osu stable yet.
>
> **This project is written with LLM's assistance.**

## Getting Started

### Prerequisites
- Windows 10 or later
- .NET 10 Runtime
- osu! stable installation

### Configuration
1. Launch SixKeyToolbox
2. Navigate to **Config** page
3. Set your osu! installation path (default: `%LocalAppData%/osu!`)
4. Customize number formats if desired (default: 2 decimal places)

## Pages

### Home
Landing page with quick stats: total 6K plays and your top rating. Links to all tools for easy navigation.

### Difficulty Estimator
Upload beatmaps (supports multiple files, up to 50) to calculate:
- **Chart constant**: the automatically calculated difficulty of a 6K chart. Uses sunnyxxy's mod-aware star rating algorithm
- **Rating**: estimated player performance based on accuracy or judgement counts
- Supports mod selection (NM/HT/DT/NC) and ScoreV2 calculations

### Rating
View your best plays sorted by rating. Reads from your local osu! database to show:
- Chart constant and rating for each play
- Beatmap title, artist, and difficulty
- Accuracy and mods used
- Configurable top-N display (default: 50)

### Dan Splitter
Split a dan chart into 4 individual song charts:
- Interactive density graph with visual hit object distribution
- 3 draggable split points to define song boundaries
- Audio playback with synced playhead line
- Multi-difficulty selection when folder contains multiple .osu files
- Preset support for quick splitting
- Generates 4 separate .osu files (one per song)

### Dan Calculator
Calculate per-song real accuracies from dan plays:
- Upload replay (.osr) or pick from recent plays
- Automatic density graph when beatmap is found
- Manual .osu upload fallback when beatmap not in database
- Preset dropdown for quick note count setup
- ScoreV2 toggle for correct LN counting
- Back-solves individual song accuracies from cumulative transition points
- Fully auto accuracy filling if replay is present (not implemented yet)

### Converters
Batch beatmap conversion tools:
- **Inverse LN Convert**: convert notes to long notes with customizable gap sizes (1/16, 1/8, 1/4, 1/2) and OD settings
    - Adds `@Inverse` suffix
    - Adds `inverse` tag for easier searching
- **7K to 6K**: deletes spacebar
    - Adds `@7to6DelSpace` suffix
    - Adds `7to6` and `delspace` tag for easier searching

### Configurator
Global settings management, stores everything under `%LocalAppData%/SixKeyToolbox/`:
- osu! installation path configuration
- Accuracy and chart constant decimal places
- Manage saved dan definitions, you can extend default dan presets here

# Tech Details
- .NET 10 with C# 15 preview features
- Blazor SSR + interactive server
- `10CSS` Windows 10 style UI framework (yes I wrote this, I like windows 10 very much)
- `Coosu` for beatmap and database processing, `OsuParsers` for replay
- `StarRatingRebirth` for star rating calculations (sunnyxxy's mod-aware SR)
- Rating/Chart constant calculation ported from [mania-rating-gui](https://github.com/Siflorite/mania-rating-gui/)

## License
MIT License
