using Coosu.Database.DataTypes;
using Coosu.Database.Serialization;
using NativeFileDialogSharp;
using SixKeyToolbox.Models;
using SixKeyToolbox.OsuHelpers;
using StarRatingRebirth;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SixKeyToolbox.Services;

public class OsuLocalService
{
	private record struct ChartConstantKey(string MD5, bool? Flag);

	private static readonly string _danDefinitionsPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "dan_definitions.json");
	private static readonly string _config = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "config.json");

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{

	};

	private readonly ILogger<OsuLocalService> _logger;
	private readonly ConcurrentDictionary<ChartConstantKey, double> _beatmapChartConstantCache = new();

	private ToolSettings? _settings;

	private ScoresDb? _scoresDb;
	private OsuDb? _osuDb;

	public ConcurrentBag<RatingPlay> RatingCache { get; private set; } = [];

	public OsuLocalService(ILogger<OsuLocalService> logger)
	{
		this._logger = logger;
	}

	private async ValueTask<ScoresDb> GetScoresDbAsync()
	{
		if (this._scoresDb is null) await this.ReloadDatabasesAsync();
		return this._scoresDb!;
	}
	private async ValueTask<OsuDb> GetOsuDbAsync()
	{
		if (this._osuDb is null) await this.ReloadDatabasesAsync();
		return this._osuDb!;
	}

	public async Task ReloadDatabasesAsync()
	{
		ToolSettings settings = await this.GetSettingsAsync();
		// bruh no async
		this._osuDb = OsuDb.ReadFromFile(Path.Combine(settings.OsuBaseFolder, "osu!.db"));
		this._scoresDb = ScoresDb.ReadFromFile(Path.Combine(settings.OsuBaseFolder, "scores.db"));
	}

	public async ValueTask<ToolSettings> GetSettingsAsync()
	{
		if (this._settings is not null)
			return this._settings;

		if (!File.Exists(_config))
		{
			this._settings = ToolSettings.Default;
		}
		else
		{
			this._settings = JsonSerializer.Deserialize<ToolSettings>(await File.ReadAllTextAsync(_config), _jsonOptions) ?? ToolSettings.Default;
		}

		return this._settings;
	}
	public async Task SaveSettingsAsync(ToolSettings settings)
	{
		await File.WriteAllTextAsync(_config, JsonSerializer.Serialize(settings, _jsonOptions));
	}
	public async Task<ConcurrentBag<RatingPlay>> UpdateRatingPlaysAsync()
	{
		ToolSettings settings = await this.GetSettingsAsync();
		ConcurrentBag<RatingPlay> ratingPlays = [];
		OsuDb osuDb = await this.GetOsuDbAsync();
		List<ScoreBeatmap> beatmaps = (await this.GetScoresDbAsync()).Beatmaps;
		// i know this is a bruteforce, but lets optimize later
		Parallel.ForEach(beatmaps, new() { MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 3, 1) }, (item) =>
		{
			Beatmap? beatmap = osuDb.Beatmaps.FirstOrDefault(b => b.Md5Hash == item.Hash);
			if (beatmap is null)
			{
				this._logger.LogWarning("Failed to find beatmap {hash}", item.Hash);
				return;
			}
			if (beatmap.GameMode != DbGameMode.Mania) return;

			string? path = beatmap.TryGetPath(settings.OsuBaseFolder);
			if (path is null)
			{
				this._logger.LogWarning("Failed to find beatmap path from hash {hash}", item.Hash);
				return;
			}

			ManiaData maniaData;
			try
			{
				maniaData = ManiaData.FromFile(path);
			}
			catch (Exception ex)
			{
				this._logger.LogDebug(ex, "Skipped bad beatmap {hash}", item.Hash);
				return;
			}

			Score[] maniaPlays = item.Scores.Where(s => s.GameMode == DbGameMode.Mania).ToArray();
			Score? htMax = maniaPlays.Where(s => s.Mods.ToRatingFlag() == false).MaxBy(s => s.RatingAccuracy);
			Score? dtMax = maniaPlays.Where(s => s.Mods.ToRatingFlag() == true).MaxBy(s => s.RatingAccuracy);
			Score? nmMax = maniaPlays.Where(s => s.Mods.ToRatingFlag() == null).MaxBy(s => s.RatingAccuracy);

			if (htMax is not null)
			{
				double cc = this._beatmapChartConstantCache.GetOrAdd(new(item.Hash, false), x => maniaData.Calculate6KChartConstant(false));
				ratingPlays.Add(RatingPlay.FromScore(htMax, beatmap.TitleUnicode, beatmap.ArtistUnicode, cc));
			}
			if (dtMax is not null)
			{
				double cc = this._beatmapChartConstantCache.GetOrAdd(new(item.Hash, true), x => maniaData.Calculate6KChartConstant(true));
				ratingPlays.Add(RatingPlay.FromScore(dtMax, beatmap.TitleUnicode, beatmap.ArtistUnicode, cc));
			}
			if (nmMax is not null)
			{
				double cc = this._beatmapChartConstantCache.GetOrAdd(new(item.Hash, null), x => maniaData.Calculate6KChartConstant(null));
				ratingPlays.Add(RatingPlay.FromScore(nmMax, beatmap.TitleUnicode, beatmap.ArtistUnicode, cc));
			}
		});
		this.RatingCache = ratingPlays;
		return ratingPlays;
	}
	public async Task<IReadOnlyList<RecentReplay>> GetRecentReplaysAsync()
	{
		ScoresDb scoresDb = await this.GetScoresDbAsync();
		OsuDb osuDb = await this.GetOsuDbAsync();
		List<RecentReplay> replays = [];
		foreach (ScoreBeatmap item in scoresDb.Beatmaps)
		{
			foreach (Score score in item.Scores)
			{
				string title = osuDb.Beatmaps.FirstOrDefault(b => b.Md5Hash == item.Hash)?.TitleUnicode ?? $"<unknown {item.Hash}>";
				replays.Add(new RecentReplay
				{
					Timestamp = score.Timestamp,
					Player = score.Player,
					Mods = score.Mods,
					CountGeki = score.CountGeki,
					Count300 = score.Count300,
					CountKatu = score.CountKatu,
					Count100 = score.Count100,
					Count50 = score.Count50,
					CountMiss = score.CountMiss,
					BeatmapHash = item.Hash,
					BeatmapTitle = title
				});
			}
		}
		replays.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
		return replays;
	}
	public async Task<string?> ResolveBeatmapPathByHashAsync(string beatmapHash)
	{
		ToolSettings settings = await this.GetSettingsAsync();
		foreach (Beatmap item in (await this.GetOsuDbAsync()).Beatmaps)
		{
			if (item.Md5Hash == beatmapHash)
			{
				return item.TryGetPath(settings.OsuBaseFolder);
			}
		}

		return null;
	}
	public Task<DialogResult> PickFolderAsync(string? defaultPath = null)
	{
		return Task.Run(() =>
		{
			return Dialog.FolderPicker(defaultPath);
		});
	}
	public async Task<List<DanDefinition>> GetDanDefinitionsAsync()
	{
		if (!File.Exists(_danDefinitionsPath))
		{
			return DanDefinition.Defaults;
		}

		return JsonSerializer.Deserialize<List<DanDefinition>>(await File.ReadAllTextAsync(_danDefinitionsPath), _jsonOptions)
			?? DanDefinition.Defaults;
	}
	public async Task SaveDanDefinitionsAsync(List<DanDefinition> defs)
	{
		await File.WriteAllTextAsync(_danDefinitionsPath, JsonSerializer.Serialize(defs, _jsonOptions));
	}
}
