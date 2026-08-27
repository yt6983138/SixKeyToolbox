using Coosu.Beatmap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OsuParsers.Decoders;
using OsuParsers.Replays;
using SixKeyToolbox.Models;
using SixKeyToolbox.OsuHelpers;
using SixKeyToolbox.Services;

namespace SixKeyToolbox.Components.Pages;

public enum DanCalcState
{
	Nothing,
	ReplayNoBeatmap,
	ReplayBeatmap,
}

public partial class DanCalc : ComponentBase
{
	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;

	public Replay? Replay { get; set; }
	public bool HasReplay => this.Replay is not null;
	public string? ReplayError { get; set; }
	public string ScoreV2Text => this.HasReplay && this.Replay!.Mods.HasFlag(OsuParsers.Enums.Mods.ScoreV2) ? " (scoreV2)" : "";

	public OsuFile? Beatmap { get; set; }
	public bool HasBeatmap => this.Beatmap is not null;
	public string? BeatmapTitle { get; set; }
	public string? BeatmapError { get; set; }

	public List<int> NoteCounts { get; set; } = [0, 0, 0, 0];
	public List<string> SongLabels { get; set; } = ["Jack", "Tech", "Stamina", "Speed"];
	public List<double> CumulativeAcc { get; set; } = [0, 0, 0, 0];
	public List<double> Results { get; set; } = [];

	public IReadOnlyList<DanDefinition> DanPresets { get; set; } = [];
	public IReadOnlyList<RecentReplay> RecentReplays { get; set; } = [];

	public DanCalcState State
	{
		get
		{
			if (this.HasReplay && this.HasBeatmap) return DanCalcState.ReplayBeatmap;
			if (this.HasReplay && !this.HasBeatmap) return DanCalcState.ReplayNoBeatmap;
			return DanCalcState.Nothing;
		}
	}

	public bool ShowGraph => this.State == DanCalcState.ReplayBeatmap;
	public int ManualTransitionCount => this.State == DanCalcState.Nothing ? 4 : 3;

	public string TransitionHelpText => this.State switch
	{
		DanCalcState.ReplayBeatmap => "Transition accuracies auto-derive from the replay when implemented; otherwise type them. The total is from the replay.",
		DanCalcState.ReplayNoBeatmap => "Type the cumulative accuracy you see at the end of songs 1, 2, and 3. The total is from the replay.",
		_ => "Type the cumulative accuracy at the end of each song, including the total (song 4).",
	};

	public double ReplayTotalRatingAcc =>
		this.Replay is not null ? this.Replay.RatingAccuracy : 0;

	protected override async Task OnInitializedAsync()
	{
		this.DanPresets = await this.LocalService.GetDanDefinitionsAsync();
		this.RecentReplays = await this.LocalService.GetRecentReplaysAsync();
	}

	public IReadOnlyList<RecentReplay> TopRecentReplays =>
		this.RecentReplays.Count <= 15
			? this.RecentReplays
			: this.RecentReplays.Take(15).ToList();

	public async Task OnRecentReplayPicked(ChangeEventArgs e)
	{
		int idx = int.TryParse(e.Value?.ToString(), out int i) ? i : -1;
		if (idx < 0 || idx >= this.TopRecentReplays.Count) return;
		RecentReplay recent = this.TopRecentReplays[idx];

		this.ReplayError = null;
		try
		{
			this.Replay = new Replay
			{
				PlayerName = recent.Player,
				BeatmapMD5Hash = recent.BeatmapHash,
				Ruleset = OsuParsers.Enums.Ruleset.Mania,
				Mods = (OsuParsers.Enums.Mods)(int)recent.Mods,
				Count300 = (ushort)recent.Count300,
				Count100 = (ushort)recent.Count100,
				Count50 = (ushort)recent.Count50,
				CountGeki = (ushort)recent.CountGeki,
				CountKatu = (ushort)recent.CountKatu,
				CountMiss = (ushort)recent.CountMiss,
			};

			this.CumulativeAcc[3] = this.ReplayTotalRatingAcc;
			await this.TryLoadBeatmapFromReplay();
			this.TryDeriveTransitionAccuraciesFromReplay();
		}
		catch (Exception ex)
		{
			this.ReplayError = $"Could not use replay: {ex.Message}";
			this.Replay = null;
		}
	}

	public async Task OnReplaySelected(InputFileChangeEventArgs e)
	{
		this.ReplayError = null;
		try
		{
			IBrowserFile file = e.File;
			int len = (int)Math.Min(file.Size, 5 * 1024 * 1024);
			using Stream stream = file.OpenReadStream(len);
			this.Replay = await ReplayDecoder.DecodeAsync(stream);

			this.CumulativeAcc[3] = this.ReplayTotalRatingAcc;
			await this.TryLoadBeatmapFromReplay();
			this.TryDeriveTransitionAccuraciesFromReplay();
		}
		catch (Exception ex)
		{
			this.ReplayError = $"Could not read .osr: {ex.Message}";
			this.Replay = null;
		}
	}

	public async Task OnBeatmapSelected(InputFileChangeEventArgs e)
	{
		this.BeatmapError = null;
		try
		{
			IBrowserFile file = e.File;
			int len = (int)Math.Min(file.Size, 10 * 1024 * 1024);
			using Stream stream = file.OpenReadStream(len);
			byte[] buffer = new byte[len];
			await stream.ReadExactlyAsync(buffer, 0, len);
			await using MemoryStream ms = new(buffer, writable: false);
			this.Beatmap = OsuFile.ReadFromStream(ms);
			this.BeatmapTitle = $"{this.Beatmap.Metadata?.Artist} - {this.Beatmap.Metadata?.Title}";
		}
		catch (Exception ex)
		{
			this.BeatmapError = $"Could not read .osu: {ex.Message}";
		}
	}

	private async Task TryLoadBeatmapFromReplay()
	{
		if (this.Replay is null) return;
		try
		{
			string? path = await this.LocalService.ResolveBeatmapPathByHashAsync(this.Replay.BeatmapMD5Hash);
			if (path is null || !File.Exists(path)) return;
			this.Beatmap = await Task.Run(() => OsuFile.ReadFromFile(path));
			this.BeatmapTitle = $"{this.Beatmap.Metadata?.Artist} - {this.Beatmap.Metadata?.Title}";
		}
		catch (Exception) { }
	}

	private void TryDeriveTransitionAccuraciesFromReplay()
	{
		if (this.Replay is null) return;
		List<double>? derived = null; // TODO: derive the accuracies from the replay when implemented
		if (derived is null || derived.Count < 3) return;
		for (int i = 0; i < 3; i++)
			this.CumulativeAcc[i] = derived[i];
	}
	public void OnSplitPointChanged(DensitySplitView view)
	{
		this.NoteCounts = view.Sections.Select(x => x.GetTotalCount(false)).ToList();
	}

	public void OnPresetChanged(ChangeEventArgs e)
	{
		string? name = e.Value?.ToString();
		if (string.IsNullOrEmpty(name)) return;
		DanDefinition? def = this.DanPresets.FirstOrDefault(d => d.Name == name);
		if (def is null) return;

		this.SongLabels = def.Sections.Select(s => s.Name).ToList();
		this.NoteCounts = def.Sections.Select(s => s.GetTotalCount(false)).ToList();
	}

	public void Calculate()
	{
		this.Results = [];
		if (this.NoteCounts.Any(n => n <= 0)) return;

		List<int> n = this.NoteCounts;
		List<double> c = this.CumulativeAcc;
		if (c.Count < 4 || c.Any(x => x <= 0)) return;

		double[] cumulativeNotes = new double[4];
		int running = 0;
		for (int i = 0; i < 4; i++)
		{
			running += n[i];
			cumulativeNotes[i] = running;
		}

		double[] weightedPoints = new double[4];
		double prevCumulativePoints = 0;
		for (int i = 0; i < 4; i++)
		{
			double cumulativePoints = c[i] / 100.0 * 3.1 * cumulativeNotes[i];
			weightedPoints[i] = cumulativePoints - prevCumulativePoints;
			prevCumulativePoints = cumulativePoints;
		}

		this.Results = [];
		for (int i = 0; i < 4; i++)
		{
			double acc = 3.1 * n[i] > 0
				? weightedPoints[i] / (3.1 * n[i]) * 100.0
				: 0;
			this.Results.Add(acc);
		}
	}
}
