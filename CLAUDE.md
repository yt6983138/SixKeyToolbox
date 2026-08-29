# Common rules
Please only write or modify code that is related to frontend. 
For backend jobs, please let the user do it instead. This includes: Extensions, classes, services, data processing, basically anything outside Components/ folder.
One exception: You are allowed to create a hollow service/backend related class and let the user fill it in later.

Ask the user questions if you are unsure about what to do, do not assume anything. You can use deepwiki mcp to ask API questions, like Coosu/Coosu for the coosu api.

You are free to modify or take notes inside this file. DO NOT leave comments in the codebase.

## Tech stacks
This project uses following things:
1. Blazor ssr with interactivity enabled
2. .NET 10 with preview lang features (C# 15)
3. 10CSS for styling. Basically, it is a windows 10 style CSS framework, uses dialog (WinDialog.razor) to seperate things into a "workspace" or things like that.
4. Use Coosu for beatmap and database processing.
5. Use the replay class under OsuHelpers folder to process replays.

## Some specialized things
This is a toolbox for osu!mania 6k only. It is not a general purpose osu!mania toolbox.

- chart constant: The chart's inherent difficulty, a linear rescaling of the sunnyxxy SR Reborn star rating (sr_mod × 200/81 + 7/6) onto the ~1–26 community scale — shown as [{diff}] (e.g. [16]) — independent of the player's accuracy. Applies to 6k only.
- rating: The player's performance on that play, produced by feeding chart constant and rating accuracy through a piecewise curve that scales up sharply as accuracy passes 93%→96%→98%→99.5%. Applies to 6k only.

# Requests

This project need following pages:
1. Home page: A landing page that has an overview and links to other tool pages.
2. Difficulty Estimator page: A page that allows users to input a beatmap and get an estimated chart constant. Also make few fields for users to optionally input accuracy, or judegement counts (300/200/100/50/miss etc), and an "Is score v2" checkbox, to get an estimated rating.
3. Rating page: A page that reads the user's existing plays (from database or replays), and transform them into a list of plays with chart constant and rating. Sort them by rating, high to low.
4. Dan splitting page: A page that allows users to input a beatmap, and get a ui for splitting the chart into dan levels. The user can adjust the split points with both density preview and music preview. Like a bar graph with 3 slidebar on it (3 because a dan has 4 songs). It also need to have 4 fields for users to input the dan level names. Defaults to: "Jack", "Tech", "Stamina", and "Speed".
5. Dan calculating page: A page that allows users to input a dan level, and accuracies they see during different transitions. Then, calculate the real accuracies of each song back. This should also accept a replay. Leave datas empty.
6. LN converting page: A page that allows users to input beatmaps, and convert them to inverse, with different gap sizes (1/8, 1/4, etc). Also OD that defaults to 6.

# Decisions

This section records the design decisions agreed during planning. Rules of engagement: code is only written inside `Components/`. For DB/OS/persistence/file-streaming jobs, create hollow classes/methods and let the user fill them in. Local file I/O from a page is acceptable (the app is purely local; everything runs in-process). Leave no comments in the codebase.

## Stack & rules
- Blazor SSR + interactive server, .NET 10 / C# 15 (preview).
- 10CSS styling, loaded from CDN in `App.razor`. `WinDialog.razor` is the "workspace" container; a page may hold several dialogs.
- Coosu for beatmap + database processing; StarRatingRebirth for SR; `ReplayInfo` under `OsuHelpers/` for `.osr` replays.
- 6k only. chart constant = `SRCalculator.Calculate(maniaData) * 200/81 + 7/6` (mod-aware via `OsuExtensions.Calculate6KChartConstant(modFlag)`, where null=NM, false=HT, true=DT/NC). rating = `RatingCalculator.Calculate6KRating(chartConstant, ratingAccuracy)`.

## App shell
- Routed `@page` per tool; each page wraps its content in one or more `WinDialog` as the layout needs.
- Persistent top navbar in `MainLayout` (matching the `PersonalWebsite/Pages/Shared/_Layout.cshtml` reference): an `<a>` per tool + Home, active page flagged `is-active`.
- Dark-only theme: `App.razor` keeps loading `10css.dark.css` unconditionally. No dark/light toggle, no `common.js`.

## Hollow service interface (`OsuLocalService` — I scaffold, user fills in)
A single service registered with DI, exposing:
1. `GetSettings()` / `SetDbPath(path)` — auto-detect, overridable in-app.
2. `GetRatingPlays()` — returns plays WITH chart constant already computed (loads each `.osu`, runs `SRCalculator`). Each play carries: beatmap title/artist, chart constant, mods, scoreV2 flag, judgement counts (300/200/100/50/miss/Geki/Katu). Frontend computes the rating from CC + `RatingAccuracy`.
3. `GetRecentReplays()` — recent replay records (timestamp, player, mods, scoreV2, judgements, beatmap hash/title). Backs Dan calculating's "recent replays" picker.
4. `ResolveReplayToPlay(stream)` — parse uploaded `.osr` via `ReplayInfo.FromStream` and resolve its beatmap hash to a chart constant (loads the `.osu`). Returns the play shape from (2).
5. `PickFolder()` — returns a real absolute path via an OS folder dialog. The single shared mechanism wherever a real disk path is needed (Dan splitting, LN converting).
6. `GetDanDefinitions()` / `SaveDanDefinition(...)` — read/write saved dan definitions for Dan calculating's dropdown.

Also a hollow audio streaming endpoint (I scaffold, user fills file streaming), e.g. `GET /audio?path=...`, used only by Dan splitting. (Fallback if preferred: Blob-URL via JS interop from Blazor Server's local disk access — default to the hollow endpoint unless noted otherwise.)

## Page specs

### 1. Home
- Overview + link cards to all 6 tools, PLUS a live stats strip (total 6k plays, top rating) pulled from `GetRatingPlays()`.

### 2. Difficulty Estimator
- `.osu` via `InputFile` (parse bytes → `ManiaData`, as `Home.razor.cs` prototypes). Optional browser "pick set" to choose among a set's difficulties (browser directory picker, content-only; no real path needed).
- Mod selector (NM/HT/DT/NC) → `Calculate6KChartConstant(modFlag)`.
- Two input modes for rating: (A) type accuracy → rating; (B) type judgement counts (300/200/100/50/miss/Geki/Katu) + scoreV2 checkbox → page computes accuracy via `RatingCalculator.CalculateAccuracy` → rating via `Calculate6KRating`. Mode toggle, not both at once.

### 3. Rating
- Reads local database via `GetRatingPlays()`; best score per beatmap, top-N (default 50).
- Card list: title/artist, CC [{diff}], big rating number, acc, mods. Default sort: rating desc.

### 4. Dan splitting
- Input via hollow `PickFolder()` (real path → find the `.osu` + sibling audio file inside the folder).
- Density curve computed in `Components/` (bucket hit objects into time windows → notes-per-window).
- 3 sliders = 3 split-point timestamps (ms) along the timeline → 4 contiguous segments = the dan's 4 songs.
- Music preview = audio playback (hollow streaming endpoint) + synced density-timeline scrubber.
- 4 name fields; defaults: Jack, Tech, Stamina, Speed.
- Saves a dan definition via the hollow dan-def service.

### 5. Dan calculating
- Uploaded `.osr` (→ judgements + mods + scoreV2 via `ReplayInfo`).
- Transition split points picked by the user.
- Per-song note counts via a dropdown of presets: developer-seeded defaults + saved dan definitions from Dan splitting (which pre-fill both splits and counts; editable afterwards).
- "Leave datas empty" = note-count fields start at preset defaults; the page back-solves per-song real accuracy by differencing cumulative judgements at the 3 boundaries.
- "Recent replays" picker via `GetRecentReplays()`.
- In theory only the replay is needed (preset + replay cover everything).

### 6. LN converting
- Input via hollow `PickFolder()` (batch all `.osu` in the folder).
- The page writes the converted `.osu` directly (`File.WriteAllText`) back to the SAME folder, with an `@Inverse` suffix on the filename.
- Transform (per column, notes sorted by time): every note except the last becomes an LN with `end = next.start − gap`; the last note per column stays a normal note; existing LNs are re-processed identically (their old ends discarded).
- Gap: dropdown of beat-fraction presets (1/16, 1/8, 1/4, 1/2) + free-text override, snapped to timing via the beatmap's `TimingPoints` beat length.
- OD: defaults to 6.
- Edge case: when `next.start − gap ≤ current.start` (notes closer than the gap), leave the note as a normal note (no LN).

## Persistence
- DB-path override → local JSON (e.g. `%AppData%/SixKeyToolbox/settings.json`), page writes directly.
- Dan definitions → local JSON (e.g. `%AppData%/SixKeyToolbox/dans.json`) via the hollow dan-def service.

## Open minor assumptions (confirm before implementing)
1. Difficulty Estimator's optional "pick set" uses the browser directory picker (content-only, no real path). Hollow `PickFolder()` reserved for real-path needs (Dan splitting, LN converting).
2. Hollow audio streaming endpoint stays as the chosen approach (vs. Blob-URL via JS interop).
3. Service/method names are proposals; user renames freely when filling in.
4. `@Inverse` filename collision handled by overwriting silently, unless skip/prompt preferred.

## Resolved (planning Q&A)

### Rating accuracy source — RESOLVED
- Rating ALWAYS uses `RatingCalculator.CalculateRatingAccuracy` (310-based, scoreV2-agnostic), never the scoreV2-aware `CalculateAccuracy`.
- Difficulty Estimator: in judgement-count mode, the "Is score v2" checkbox affects only the *displayed* accuracy (via `CalculateAccuracy`); rating is computed from `RatingAccuracy` regardless of the checkbox.
- This matches `ReplayInfo.Calculate6KRating` which already uses `RatingAccuracy`. Keep pages consistent with that.

### Dan calculating transition input — RESOLVED
- At each of the 3 split points the user enters a SINGLE cumulative accuracy (option (a)), not judgement counts.
- (Implementation note: single cumulative accuracies at 3 boundaries + per-song note counts may not uniquely constrain per-judgement breakdowns; page will back-solve per-song real accuracy from the cumulative accuracy deltas. See open question below re: whether this is well-defined.)

### Scope / service / DI — RESOLVED
- May create hollow `OsuLocalService` OUTSIDE `Components/` (e.g. `Services/OsuLocalService.cs`).
- May add the `AddSingleton<OsuLocalService>()` DI line to `Program.cs` directly.

### 10CSS dialog display — RESOLVED
- 10CSS shows `<dialog>` via the `is-open` class using CSS only (no JS helper). Leave `common.js` script in `App.razor` as-is.
- May add JS interop where needed (e.g. audio Blob URL, browser directory picker via `webkitdirectory`).

### Navbar reference — RESOLVED
- Reference: `C:\Users\tgg\source\repos\PSLBotMiscPlugins\PersonalWebsite\Pages\Shared\_Layout.cshtml`.
- Pattern: `<header><nav>` with one `<a>` per section, active link gets class `is-active`. No dark-mode toggle (dark-only per Decisions).

### `@Inverse` collision — RESOLVED
- Overwrite silently.

### Dan splitting audio — RESOLVED
- Use JS interop Blob URL (page reads audio bytes → JS creates `URL.createObjectURL(blob)` → `<audio src>`). NO hollow HTTP `/audio` endpoint.

## API notes (Coosu / StarRatingRebirth — verified against DLL during impl)

### Read a .osu
- `OsuFile.ReadFromFileAsync(path)` → `Task<LocalOsuFile>` (`LocalOsuFile` : `OsuFile`). Sync `OsuFile.ReadFromFile(path)` also exists.
- `OsuFile.ReadFromStream(Stream)` exists — reads an .osu from an in-memory Stream (used by Dan Calc's manual .osu upload). Returns `OsuFile`. `ReadFromFile` wraps it.
- Hit objects: `osuFile.HitObjects.HitObjectList` → `List<RawHitObject>`.
- Per `RawHitObject`: `Offset` (**double** in this version, not int — confirmed at compile time), `X` (**double/floating**, not int — confirmed), `Y`, `HoldEnd` (int), `RawType` (`RawObjectType`).
- **Mania column from X:** `column = floor(X * keyCount / 512)`. Key count from `osuFile.Difficulty.CircleSize` (float). No built-in helper.

### Hit object types (correction to earlier notes)
- `HitObjectType` is a read-only getter that interprets the `RawType` **flags** enum (`RawObjectType`, `[Flags]`). Standard osu! values: `Circle=1, Slider=2, NewCombo=4, Spinner=8, Hold=128`.
- A mania tap note is stored as `Circle`; an LN as `Hold`. `ObjectType` prioritizes `Hold` over `Circle`.
- **To turn a note into an LN while preserving NewCombo:** do NOT overwrite `ObjectType` (it's read-only). Use bit ops on `RawType`: `rawType &= ~RawObjectType.Circle; rawType |= RawObjectType.Hold;`. To revert a note: `rawType |= Circle; rawType &= ~Hold; HoldEnd = 0;`.

### Write a .osu
- `osuFile.Save("path/to/new.osu")` or `osuFile.SaveToDirectory(dir)`.
- `AppendSerializedString()` writes `HoldEnd` only when ObjectType is Hold/Spinner (Hold → colon, Spinner → comma).
- OD read/set via `osuFile.Difficulty.OverallDifficulty` (float).

### Timing points
- `osuFile.TimingPoints` → `TimingSection`; its `TimingList` → `List<TimingPoint>`.
- `osuFile.TimingPoints.GetRedLine(double offset)` → active uninherited `TimingPoint` at/before offset. Lives in namespace `Coosu.Beatmap` (the `Extensions/` folder uses `// ReSharper disable once CheckNamespace`, so it's NOT `Coosu.Beatmap.Extensions` — that namespace does NOT exist). Signature is `double offset`.
- `timingPoint.Factor` = ms-per-beat for uninherited (red) lines. `Bpm` is computed. `IsInherit` distinguishes green (inherited) lines.

### General / metadata
- `osuFile.General.AudioFilename` (string) — sibling audio filename for Dan splitting's Blob-URL.
- `osuFile.Metadata.Artist` / `osuFile.Metadata.Title`.

### Reading scores from local DB
- `ScoresDb.ReadFromFile(path)` → `ScoresDb`; `.Beatmaps` → `List<ScoreBeatmap>`; each `.Scores` → `List<Score>`.
- Per `Score`: `BeatmapHash`, `Count300/100/50/Geki/Katu/Miss`, `Mods`, `ScoreVersion` (==2 ⇒ ScoreV2), `Player`.
- Beatmap title/artist NOT in scores.db — must join `BeatmapHash` against `osu!.db` via `OsuDb.ReadFromFile(path)` and search by `Md5Hash`.

### Coosu namespaces (verified, used in `_Imports.razor`)
- `OsuFile`, `LocalOsuFile`, `DifficultySection`, `MetadataSection`, `HitObjectSection`, `TimingSection` → `Coosu.Beatmap`.
- `RawHitObject`, `HitObjectType`, `RawObjectType` → `Coosu.Beatmap.Sections.HitObject`.
- `TimingPoint` → `Coosu.Beatmap.Sections.Timing`.
- `GameMode`, `Mods` → `Coosu.Beatmap.Sections.GamePlay` / `Coosu.Database.DataTypes`.
- `GetRedLine` extension → `Coosu.Beatmap` (via `TimingExtensions`).
- `ManiaData`, `SRCalculator` → `StarRatingRebirth`.

## Dan calculating — 4-condition state machine (RESOLVED)

`DanCalcState` enum drives the UI. The page back-solves per-song real accuracy from **4 cumulative points** (3 transitions + total) → 4 per-song accuracies, via: `weightedPoints_i = (cumulativeAcc_i/100 * 3.1 * cumulativeNotes_i) − prevCumulativePoints`, then `acc_i = weightedPoints_i / (3.1 * n_i) * 100`.

| Condition | State | What's shown |
|---|---|---|
| replay + beatmap found | `ReplayBeatmap` | density graph + split sliders (`DensitySplitView`), preset/note-counts, **3** cumulative-acc inputs (total auto from replay), calculate |
| replay, no beatmap | `ReplayNoBeatmap` | "Beatmap not found" + optional manual .osu upload (to enable the graph), preset/note-counts, **3** cumulative-acc inputs, calculate |
| beatmap, no replay | (disallowed) | beatmap upload only appears in the C2 section — i.e. only when a replay is present |
| nothing | `Nothing` | preset/note-counts, **4** cumulative-acc inputs (after songs 1/2/3/total), calculate |

- **Beatmap resolution:** on replay load, `OsuLocalService.ResolveBeatmapPathByHash(replay.BeatmapMD5Hash)` (hollow) → loads that `.osu` (C1). On `NotImplementedException`/null → falls back to manual `.osu` upload in C2.
- **Per-transition accuracy from replay frames = HOLLOW.** `DanCalc.DeriveTransitionAccuraciesFromReplay()` is a `protected virtual` returning `null` (`ReplayInfo` has no frame parsing yet). Manual inputs cover it. Override/fill once frames are parseable.
- **Note counts** always manual/preset (`DanDefinition.NoteCounts`); the graph does NOT auto-count them (consistent with "manual/preset like today").
- Without the beatmap (C2), per-song note counts must come entirely from preset/manual entry.

## Shared component — DensitySplitView (RESOLVED)
- `Components/DensitySplitView.razor` (+`.razor.cs`): owns density bucketing (hit objects → notes-per-window, 200 buckets), SVG bars, 3 split sliders. Takes `OsuFile` as `[Parameter]`, reports `SplitMs` via `SplitMsChanged` `EventCallback`.
- Used by both Dan Splitting and Dan Calculating (when `ReplayBeatmap`). Dan Splitting adds the page-specific audio scrubber on top; Dan Calc uses it bare.
- Lifecycle note: only recomputes density when the beatmap **instance** changes (`ReferenceEquals`), so dragging a slider doesn't reset splits on re-render.

## Hollow service surface — OsuLocalService (current)
`GetSettings`, `SetDbPath`, `GetRatingPlays`, `GetRecentReplays`, `ResolveReplayToPlay`, `ResolveBeatmapPathByHash` (new — DB lookup by beatmap hash for Dan Calc), `PickFolder`, `GetDanDefinitions`, `SaveDanDefinition`. All throw `NotImplementedException`; every page catches it and degrades gracefully.

## User refactor round (service filled, replay lib swapped) — UPDATE
The hollow `OsuLocalService` was fully implemented by the user. Key changes since the above:
- **Replay library:** swapped to **OsuParsers** (`OsuParsers.Replays.Replay` + `ReplayDecoder.DecodeAsync`) for replay decoding. Use it ONLY for replay-related functionality; Coosu remains the beatmap/DB library everywhere else. `ReplayInfo`/`ReplayExtensions` (under `OsuHelpers/`) wrap OsuParsers `Replay` with `RatingAccuracy`/`Accuracy`/`Calculate6KRating`. OsuParsers `Replay` count props are `ushort` (cast from int when constructing from `RecentReplay`).
- **DensitySplitView** now exposes `IReadOnlyList<SplitSection> Sections` and auto-computes per-section note/LN counts via `OsuExtensions` `GetNoteCount`/`GetLNCount`. It reports changes via `SplitPointChanged` `EventCallback<DensitySplitView>` (NOT `SplitMsChanged`).
- **DanDefinition** reshaped: now `Name` + `List<DanSection> Sections`, each `DanSection` = `Name` / `StartMilliseconds` / `NoteCount` / `LNCount` / `GetTotalCount(isScoreV2)`. `SplitSection.ToDanSection(name)` bridges the two.
- **Service API** is now async + cached: `GetSettingsAsync`/`SaveSettingsAsync`, `ReloadDatabasesAsync`, `UpdateRatingPlaysAsync` (Parallel.ForEach over scores.db, best score per mod flag per beatmap, `ConcurrentDictionary` CC cache keyed by `(MD5, flag)`), `GetRecentReplaysAsync`, `ResolveBeatmapPathByHashAsync`, `PickFolderAsync` (returns `NativeFileDialogSharp.DialogResult`), `GetDanDefinitionsAsync`/`SaveDanDefinitionsAsync`. Persistence: `%LocalAppData%/config.json` + `dan_definitions.json`.
- **Config:** `ToolSettings` = `OsuBaseFolder` (default `%LocalAppData%/osu!`). `Beatmap.TryGetPath(osuBaseFolder)` resolves a beatmap file from osu!.db.
- Exception handling trimmed; pages catch only what's needed.

## Five fixes/features (implemented)

### 1. Audio playback fix (Dan Splitting)
Root cause: `createAudioBlobUrl` received a .NET `byte[]` which JS interop serializes as a **base64 string**, so `new Uint8Array(bytes)` built a wrong/empty blob; and `TogglePlay` only set `currentTime` without calling `play()`/`pause()`, and `AudioPos` never updated (showed `0s/0s`).
- `wwwroot/js/common.js`: `createAudioBlobUrl` now `atob`-decodes the base64 into a real `Uint8Array` → correct Blob. Added `playAudio`/`pauseAudio`/`getAudioDuration`.
- `DanSplit.razor.cs`: `TogglePlay` calls real `playAudio`/`pauseAudio`; `OnAudioMeta` uses `getAudioDuration`; `PollPositionAsync` updates `AudioPos` every 200ms while playing and stops at end.

### 2. Dan Splitting — generates 4 split charts (clarified intent)
Dan Split is NOT a preset-config page; it **splits one dan chart into 4 charts** (one song each), removing other songs' notes/speed changes.
- `GenerateCharts` re-reads a fresh `OsuFile` from the stored source path per segment (clean clone — Coosu has no public `Clone()`), filters `HitObjects.HitObjectList` to objects in `[segStart, segEnd]`, sets `Metadata.Version` = song name, `Save`s to the **same folder** as `{baseName} [{SongName}].osu` (overwrites). `WriteSlice` does the work; `GetSegmentStart/GetSegmentEnd` derive bounds from `SplitSections`.

### 3. Rating updating message
Home + Rating had `// TODO: make this non-blocking, ui says "updating"`. Added `IsUpdating` flag, set true before `UpdateRatingPlaysAsync`, `StateHasChanged()` to render the "Calculating ratings… this may take a moment." message immediately, cleared after. Rating's Reload button disabled while updating.

### 4. Recent replays in Dan Calculating
Spec had `GetRecentReplays` backing a picker; now wired. Dropdown of **top 15** recent replays (by timestamp), labeled `title — player (date)`. Picking one builds an OsuParsers `Replay` from the `RecentReplay`'s counts + hash (no `.osr` decode needed — `Replay` has a parameterless ctor, settable `ushort` counts; `Mods` cast `Coosu`→`OsuParsers` via `(Mods)(int)recent.Mods`), then resolves beatmap by hash + fills total acc — same path as an uploaded replay. The `.osr` upload remains.

### 5. Global config page (`/config`)
New routed page + navbar link. Edits `OsuBaseFolder` (save via `SaveSettingsAsync`). Dan presets CRUD: list `DanDefinition`s, rename (edit `Name`), add empty preset (4 default sections Jack/Tech/Stamina/Speed), remove, save via `SaveDanDefinitionsAsync`. Ratings rebuild intentionally NOT here — stays on Rating page's Reload button.

## Coosu beatmap mutation (verified via deepwiki, for chart generation)
- No public `Clone()`/`Copy()` on `OsuFile`; deep-clone by re-reading from the source path (`OsuFile.ReadFromFile(path)`) or save-then-reread.
- `HitObjectSection.HitObjectList` is a mutable `List<RawHitObject>` — can reassign (`slice.HitObjects.HitObjectList = filtered`) or clear/add.
- New `RawHitObject`: parameterless init with `X`/`Y`/`Offset`/`RawType`/`HoldEnd`.
- Difficulty name = `Metadata.Version` (string).
- `slice.Save(path)` writes it.

## Navbar (current)
Home, Difficulty, Rating, Dan Split, Dan Calc, LN Convert, **Config**. Active link flagged `is-active` via `IsActive(href)` matching `HttpContext.Request.Path`.

## Features and Refactoring Round (August 2026)

### 1. Unified Audio Playback & Density Scrubber
- The audio player controls and the `<audio>` element are grouped entirely inside the `DensitySplitView` component rather than living in the parent `DanSplit` page.
- `DensitySplitView` accepts an optional `[Parameter] public string? AudioSrc { get; set; }`. If present, it renders Play/Pause, scrubber range input, and duration text.
- Scrubbing updates the audio playback time using the JS interop function `setAudioCurrentTime`.
- A vertical playhead line `dsv-playline` is drawn on the SVG density graph corresponding to `MsToX(AudioPos * 1000.0)` for visual feedback.
- `MsToX` converts milliseconds to X coordinate units (0 to 1000).

### 2. Multi-osu Selection for Dan Splitting
- When a folder is picked in `DanSplit`, all `.osu` files are scanned and parsed to retrieve their `Version` (Difficulty Name) and path.
- A select dropdown menu is displayed to let the user select which difficulty to split when multiple exist. Changing it updates the loaded `Beatmap` and recomputes the splits/audio.

### 3. Convert Page (LN Conversion & 7K to 6K)
- The page `/ln-convert` is renamed to `/convert` (navigating to `/convert`), renamed to `Convert.razor` and `Convert.razor.cs`.
- Implements two conversion options: "Inverse LN Convert" and "7K to 6K spacebar removal".
- **7K to 6K Spacebar Removal Logic:**
  - Middle column (column 3 in 0-based 7K system) is deleted. Any hit objects in column 3 are removed.
  - Columns 0, 1, 2 stay the same (mapped to new columns 0, 1, 2).
  - Columns 4, 5, 6 shift left by 1 column (mapped to new columns 3, 4, 5).
  - Remapped coordinates: `h.X = (int)Math.Round((newCol * 512.0 + 256.0) / 6.0)`.
  - Saves file to same folder with `@7to6DelSpace` suffix.

### 4. Preset Editing in Config Page
- The `/config` page displays full input fields for all 4 sections of each `DanDefinition` preset:
  - Name, StartMilliseconds, NoteCount, LNCount.
  - Edits bind directly and save to `dan_definitions.json` via `SaveDanDefinitionsAsync`.

### 5. Preset split loader in DanSplit
- `DanSplit` features a preset select menu. Picking a preset calls `densitySplitView.ApplyPreset(preset)` to instantly align split points with the preset's segment start times.

### 6. ScoreV2 Toggle for Dan Calc
- A ScoreV2 toggle checkbox on the `DanCalc` page lets users select if ScoreV2 is active (affects note count mapping: `GetTotalCount(IsScoreV2)` counts each LN as 2 judgements). It is pre-filled from the replay mods if a replay is present, but remains manually toggleable.

### 7. Multiple File Support in Difficulty Estimator
- The Difficulty Estimator page now accepts multiple beatmap files (up to 50) via the `multiple` attribute on `InputFile`.
- Created `BeatmapResult` class with `Name`, `Data`, `ChartConstant`, `Rating` properties.
- Changed from single `ManiaData` to `List<BeatmapResult> Results`.
- `OnFilesSelected` uses `e.GetMultipleFiles(50)` to process multiple files in parallel.
- Added `RecalculateAll()` method that recalculates all results when mod or accuracy changes.
- UI displays a results table with grid layout showing beatmap name, chart constant, and rating for each file.
- Injected `OsuLocalService` to load format settings for display.

### 8. Configurable Number Formatting
- Added `AccuracyFormat` and `ChartConstantFormat` properties to `ToolSettings` (default "F2").
- Config page displays format inputs with placeholders and explanation: "Format strings control decimal places (e.g., 'F2' = 2 decimals, 'F1' = 1 decimal)".
- All pages updated to use format settings via `value.ToString(this._settings.FormatString)`:
  - **DifficultyEstimator**: loads settings in `OnInitializedAsync`, formats chart constant and rating displays.
  - **Rating**: loads settings in `OnInitializedAsync`, formats accuracy/chart constant/rating in card display (lines 33, 34, 41).
  - **Home**: loads settings in `OnInitializedAsync`, formats `TopRatingText` display.
  - **DanCalc**: loads settings in `OnInitializedAsync`, formats accuracies at 3 locations (replay info line 35, transition input line 107, results line 129).
- Persisted in `config.json` alongside `OsuBaseFolder`.

### 9. Config Page Direct Binding Refactor
- Refactored `Config.razor.cs` to bind directly to the `ToolSettings` instance, eliminating intermediate property copying.
- **Removed**: individual properties (`OsuBaseFolder`, `AccuracyFormat`, `ChartConstantFormat`).
- **Added**: `public ToolSettings Settings { get; set; } = ToolSettings.Default;`
- `OnInitializedAsync` now loads the instance directly: `this.Settings = await this.LocalService.GetSettingsAsync();`
- `SaveSettings` passes the instance: `await this.LocalService.SaveSettingsAsync(this.Settings);`
- `Config.razor` bindings updated to `@bind="this.Settings.PropertyName"` for all settings fields (OsuBaseFolder, AccuracyFormat, ChartConstantFormat).
- Cleaner architecture, no duplication between properties and settings object.

## Current Project State (as of August 2026)

### Completed Features
All original page requirements implemented and working:
1. **Home** — landing page with stats and tool links, displays total plays and top rating.
2. **Difficulty Estimator** — supports multiple files (up to 50), mod selection (NM/HT/DT/NC), two input modes (accuracy or judgement counts), chart constant and rating calculation, configurable number formats.
3. **Rating** — reads local database, displays sorted plays with chart constant and rating, configurable top-N and formats, reload button with loading state.
4. **Dan Split** — folder picker with multi-difficulty selection, density graph with 3 split sliders and audio playback with synced playhead line, preset dropdown, generates 4 split charts.
5. **Dan Calc** — replay upload (.osr) or recent replay picker, optional manual .osu upload when beatmap not found, density graph and split sliders when beatmap available, preset dropdown, ScoreV2 toggle, calculates per-song real accuracies.
6. **Convert** — folder picker, batch conversion of all .osu files, two modes: inverse LN conversion (with gap size and OD settings) and 7K to 6K spacebar removal.
7. **Config** — global settings (osu! base folder, format strings), dan presets CRUD with full section editing (Name, StartMilliseconds, NoteCount, LNCount).

### Key Patterns and Conventions
- **Chart constant calculation**: `SRCalculator.Calculate(maniaData) * 200/81 + 7/6` (accessed via `OsuExtensions.Calculate6KChartConstant(modFlag)`).
- **Rating calculation**: `RatingCalculator.Calculate6KRating(chartConstant, ratingAccuracy)` where `ratingAccuracy` is 310-based via `RatingCalculator.CalculateRatingAccuracy`.
- **Column calculation for mania**: `column = floor(X * keyCount / 512)` where `X` and `keyCount` are from `RawHitObject.X` and `osuFile.Difficulty.CircleSize`.
- **ScoreV2 note counting**: `GetTotalCount(isScoreV2)` counts LNs as 2 judgements when true, 1 when false.
- **7K to 6K column remapping**: columns 0-2 unchanged, column 3 deleted, columns 4-6 shift left by 1, X recalculated as `Math.Round((newCol * 512.0 + 256.0) / 6.0)`.
- **Service layer**: all persistence/OS interactions through `OsuLocalService` (DI singleton). Frontend only modifies `Components/` except for hollow service method stubs.
- **Number formatting**: use `value.ToString(this._settings.AccuracyFormat)` or `this._settings.ChartConstantFormat` for all display.

### Technical Details
- **Frameworks**: Blazor SSR + interactive server, .NET 10, C# 15 preview, 10CSS for styling.
- **Libraries**: Coosu (beatmaps/DB), StarRatingRebirth (SR calculation), OsuParsers (replay decoding).
- **Audio**: JS interop with Blob URLs (`createAudioBlobUrl`, `playAudio`, `pauseAudio`, `setAudioCurrentTime`, `getAudioDuration`).
- **Persistence**: `%LocalAppData%/SixKeyToolbox/config.json` (ToolSettings) and `dan_definitions.json` (DanDefinition list).
- **Shared component**: `DensitySplitView` — density graph + split sliders + optional audio controls, used by both Dan Split and Dan Calc.

### Open Work
- Tasks.md can be deleted (all handoff tasks completed).
- No known bugs or incomplete features.
- Dan Calc's `DeriveTransitionAccuraciesFromReplay()` remains hollow (replay frame parsing not implemented — manual cumulative accuracy input covers it).

