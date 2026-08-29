using Coosu.Beatmap;
using Coosu.Beatmap.Sections.HitObject;
using Coosu.Beatmap.Sections.Timing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SixKeyToolbox.Models;
using SixKeyToolbox.OsuHelpers;
using SixKeyToolbox.Services;

namespace SixKeyToolbox.Components.Pages;

public class OsuFileOption
{
	public string Path { get; set; } = "";
	public string DisplayName { get; set; } = "";
}

public partial class DanSplit : ComponentBase, IAsyncDisposable
{
	private List<DanDefinition> _danDefinitions = [];
	private DensitySplitView? densitySplitView;
	private List<OsuFileOption> _osuFileOptions = [];
	private string? _selectedOsuPath;

	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;
	[Inject]
	internal IJSRuntime JS { get; set; } = null!;

	public string? AudioSrc { get; set; }
	public OsuFile? Beatmap { get; set; }
	public string? BeatmapTitle { get; set; }
	public string? LoadError { get; set; }
	public bool HasChart { get; set; }
	public bool HasMultipleDifficulties => this._osuFileOptions.Count > 1;

	public IReadOnlyList<SplitSection> SplitSections { get; set; } = [];
	public List<string> SongNames { get; set; } = OsuExtensions.RegularDanSections.ToList();

	public string? SaveMessage { get; set; }
	public string SaveClass { get; set; } = "";
	public string? GenMessage { get; set; }
	public string GenClass { get; set; } = "";

	private string? _sourcePath;

	protected override async Task OnInitializedAsync()
	{
		this._danDefinitions = await this.LocalService.GetDanDefinitionsAsync();
		await base.OnInitializedAsync();
	}

	private void OnSplitMsChanged(DensitySplitView view)
	{
		this.SplitSections = view.Sections;
	}

	private void UseLNNames()
	{
		this.SongNames = OsuExtensions.LNDanSections.ToList();
	}
	private void UseRegularNames()
	{
		this.SongNames = OsuExtensions.RegularDanSections.ToList();
	}

	private async Task OnPresetSelected(ChangeEventArgs e)
	{
		string? presetName = e.Value?.ToString();
		if (string.IsNullOrEmpty(presetName)) return;

		DanDefinition? preset = this._danDefinitions.FirstOrDefault(d => d.Name == presetName);
		if (preset is null || this.densitySplitView is null) return;

		await this.densitySplitView.ApplyPreset(preset);
		this.SplitSections = this.densitySplitView.Sections;

		for (int i = 0; i < Math.Min(preset.Sections.Count, this.SongNames.Count); i++)
		{
			this.SongNames[i] = preset.Sections[i].Name;
		}
	}

	public async Task PickFolder()
	{
		this.LoadError = null;
		this._osuFileOptions.Clear();
		try
		{
			NativeFileDialogSharp.DialogResult pickResult = await this.LocalService.PickFolderAsync();
			if (pickResult.IsCancelled) return;
			if (pickResult.IsError)
			{
				this.LoadError = $"Error: {pickResult.ErrorMessage}";
				return;
			}

			string folder = pickResult.Path;
			if (!Directory.Exists(folder))
			{
				this.LoadError = "No folder selected.";
				return;
			}

			string[] osuFiles = Directory.GetFiles(folder, "*.osu", SearchOption.TopDirectoryOnly);
			if (osuFiles.Length == 0)
			{
				this.LoadError = "No .osu in folder.";
				return;
			}

			foreach (string path in osuFiles)
			{
				try
				{
					OsuFile osu = OsuFile.ReadFromFile(path);
					string diffName = osu.Metadata?.Version ?? "Unknown";
					this._osuFileOptions.Add(new OsuFileOption
					{
						Path = path,
						DisplayName = diffName
					});
				}
				catch
				{
					continue;
				}
			}

			if (this._osuFileOptions.Count == 0)
			{
				this.LoadError = "No valid .osu files found.";
				return;
			}

			this._selectedOsuPath = this._osuFileOptions[0].Path;
			await this.LoadSelectedDifficulty();
		}
		catch (Exception ex)
		{
			this.LoadError = $"Error: {ex.Message}";
		}
	}

	private async Task OnDifficultySelected(ChangeEventArgs e)
	{
		string? selected = e.Value?.ToString();
		if (string.IsNullOrEmpty(selected)) return;

		this._selectedOsuPath = selected;
		await this.LoadSelectedDifficulty();
	}

	private async Task LoadSelectedDifficulty()
	{
		if (this._selectedOsuPath is null || !File.Exists(this._selectedOsuPath))
		{
			this.LoadError = "Selected file not found.";
			return;
		}

		try
		{
			OsuFile osu = OsuFile.ReadFromFile(this._selectedOsuPath);
			this.Beatmap = osu;
			this._sourcePath = this._selectedOsuPath;
			this.BeatmapTitle = $"{osu.Metadata?.Artist} - {osu.Metadata?.Title} [{osu.Metadata?.Version}]";

			string folder = Path.GetDirectoryName(this._selectedOsuPath)!;
			await this.LoadAudio(folder, osu);
			this.HasChart = true;
		}
		catch (Exception ex)
		{
			this.LoadError = $"Error loading difficulty: {ex.Message}";
		}
	}

	private async Task LoadAudio(string folder, OsuFile osu)
	{
		string audioName = osu.General?.AudioFilename ?? "";
		string audioPath = Path.Combine(folder, audioName);
		if (string.IsNullOrEmpty(audioName) || !File.Exists(audioPath)) return;

		byte[] bytes = await File.ReadAllBytesAsync(audioPath);
		this.AudioSrc = await this.JS.InvokeAsync<string>("createAudioBlobUrl", bytes);
	}

	public async Task SaveDan()
	{
		this.SaveMessage = null;
		try
		{
			List<DanSection> sections = [];
			for (int i = 0; i < this.SongNames.Count; i++)
			{
				List<RawHitObject> hitObjects = this.Beatmap?.HitObjects?.HitObjectList ?? [];

				sections.Add(this.SplitSections[i].ToDanSection(this.SongNames[i]));
			}
			this._danDefinitions.Add(new()
			{
				Name = this.BeatmapTitle ?? "Untitled Dan",
				Sections = sections
			});
			await this.LocalService.SaveDanDefinitionsAsync(this._danDefinitions);
			this.SaveMessage = "Saved.";
			this.SaveClass = "ok";
		}
		catch (Exception ex)
		{
			this.SaveMessage = $"Error: {ex.Message}";
			this.SaveClass = "err";
		}
	}

	public async Task GenerateCharts()
	{
		this.GenMessage = null;
		if (this._sourcePath is null || !File.Exists(this._sourcePath))
		{
			this.GenMessage = "No source beatmap loaded.";
			this.GenClass = "err";
			return;
		}

		string folder = Path.GetDirectoryName(this._sourcePath)!;
		string baseName = Path.GetFileNameWithoutExtension(this._sourcePath);
		double maxMs = this.Beatmap?.HitObjects?.HitObjectList[^1].Offset ?? 0;

		try
		{
			int written = 0;
			for (int i = 0; i < this.SongNames.Count; i++)
			{
				double segStart = this.GetSegmentStart(i);
				double segEnd = this.GetSegmentEnd(i, maxMs);
				string name = string.IsNullOrWhiteSpace(this.SongNames[i]) ? $"Song{i + 1}" : this.SongNames[i];
				string outPath = Path.Combine(folder, $"{baseName} [{name}].osu");

				await Task.Run(() => this.WriteSlice(outPath, segStart, segEnd, name));
				written++;
			}

			this.GenMessage = $"Generated {written} charts in the same folder.";
			this.GenClass = "ok";
		}
		catch (Exception ex)
		{
			this.GenMessage = $"Error: {ex.Message}";
			this.GenClass = "err";
		}
	}

	private void WriteSlice(string outPath, double segStart, double segEnd, string name)
	{
		OsuFile slice = OsuFile.ReadFromFile(this._sourcePath!);

		List<RawHitObject> hits = slice.HitObjects?.HitObjectList ?? [];
		List<RawHitObject> kept = hits
			.Where(h => h.Offset >= segStart && h.Offset <= segEnd)
			.ToList();

		slice.HitObjects?.HitObjectList = kept;
		slice.Metadata?.Version = name;
		TimingPoint[]? redlines = slice.TimingPoints?.TimingList.Where(x => !x.IsInherit).ToArray();

		// i still dont know why they call those timing points redline greenline bruh
		if (redlines is null || redlines.Length < 2)
			goto Save;

		if (segStart > redlines[^1].Offset)
		{
			slice.TimingPoints?.TimingList = [redlines[^1]];
			goto Save;
		}


		TimingPoint lastPoint = redlines[0];
		for (int i = 1; i < redlines.Length; i++)
		{
			TimingPoint current = redlines[i];
			if (current.Offset < segStart) continue;

			double currentGapToStart = current.Offset - segStart;
			double lastGapToStart = segStart - lastPoint.Offset;

			slice.TimingPoints?.TimingList = [currentGapToStart < lastGapToStart ? current : lastPoint];
			break;
		}

	Save:
		slice.Save(outPath);
	}

	private double GetSegmentStart(int i)
	{
		if (i == 0) return 0;
		IReadOnlyList<SplitSection> secs = this.SplitSections;
		return secs.Count > 0 ? secs[i].StartMs : 0;
	}

	private double GetSegmentEnd(int i, double maxMs)
	{
		IReadOnlyList<SplitSection> secs = this.SplitSections;
		if (i < secs.Count - 1) return secs[i + 1].StartMs;
		return maxMs;
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			if (this.AudioSrc is not null)
				await this.JS.InvokeVoidAsync("revokeObjectUrl", this.AudioSrc);
		}
		catch { }
	}
}
