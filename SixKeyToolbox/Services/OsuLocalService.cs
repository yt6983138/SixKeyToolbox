using Coosu.Database.DataTypes;
using Coosu.Database.Serialization;
using MemoryPack;
using NativeFileDialogSharp;
using SixKeyToolbox.Models;
using SixKeyToolbox.OsuHelpers;
using StarRatingRebirth;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SixKeyToolbox.Services;

public partial class OsuLocalService
{
	[MemoryPackable]
	private partial record struct ChartConstantKey(string MD5, bool? Flag);

	private static readonly string _dataPathBase = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SixKeyToolbox));

	private static readonly string _danDefinitionsPath = Path.Combine(_dataPathBase, "dan_definitions.json");
	private static readonly string _config = Path.Combine(_dataPathBase, "config.json");
	private static readonly string _beatmapConstantCachePath = Path.Combine(_dataPathBase, "beatmap_constant_cache.bin");

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{

	};

	private readonly ILogger<OsuLocalService> _logger;
	private readonly SemaphoreSlim _updateOnceLock = new(1, 1);

	private ToolSettings? _settings;

	private ScoresDb? _scoresDb;
	private OsuDb? _osuDb;
	private Task<ConcurrentBag<RatingPlay>>? _onGoingRatingUpdateTask;

	private ConcurrentDictionary<ChartConstantKey, double> _beatmapChartConstantCache = new();

	public ConcurrentBag<RatingPlay> RatingCache { get; private set; } = [];

	public OsuLocalService(ILogger<OsuLocalService> logger)
	{
		this._logger = logger;
		Directory.CreateDirectory(_dataPathBase);
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

		if (File.Exists(_beatmapConstantCachePath))
		{
			// god, wish the .net 12 new type inference comes soon
			this._beatmapChartConstantCache = MemoryPackSerializer.Deserialize<ConcurrentDictionary<ChartConstantKey, double>>(
				await File.ReadAllBytesAsync(_beatmapConstantCachePath)) ?? new();
		}
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
	public Task<ConcurrentBag<RatingPlay>> TryUpdatingPlaysOnceAsync()
	{
		this._updateOnceLock.Wait();
		Task<ConcurrentBag<RatingPlay>>? task = Volatile.Read(ref this._onGoingRatingUpdateTask);
		if (task is not null)
		{
			this._logger.LogInformation("Rating update already in progress, skipping");
			this._updateOnceLock.Release();
			return task;
		}
		Task<ConcurrentBag<RatingPlay>> newTask = this.UpdateRatingPlaysAsync();
		this._updateOnceLock.Release();
		return newTask;
	}
	public Task<ConcurrentBag<RatingPlay>> UpdateRatingPlaysAsync()
	{
		Task<ConcurrentBag<RatingPlay>> task = this.UpdateRatingPlaysAsyncCore();

		Volatile.Write(ref this._onGoingRatingUpdateTask, task);

		return Core();

		async Task<ConcurrentBag<RatingPlay>> Core()
		{
			try
			{
				return await task;
			}
			finally
			{
				// only replaces it if the current task is the same as the one we started with,
				// if its not the same, it means another update has started and we should not null it out
				_ = Interlocked.CompareExchange(ref this._onGoingRatingUpdateTask, null, task);
			}
		}
	}
	private async Task<ConcurrentBag<RatingPlay>> UpdateRatingPlaysAsyncCore()
	{
		await Task.Yield();
		ToolSettings settings = await this.GetSettingsAsync();
		ConcurrentBag<RatingPlay> ratingPlays = [];
		OsuDb osuDb = await this.GetOsuDbAsync();
		List<ScoreBeatmap> beatmaps = (await this.GetScoresDbAsync()).Beatmaps;
		// i know this is a bruteforce, but lets optimize later
		Parallel.ForEach(beatmaps, new() { MaxDegreeOfParallelism = 4 }, (item) =>
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

			Score? htMax = null;
			Score? dtMax = null;
			Score? nmMax = null;
			foreach (Score play in item.Scores)
			{
				if (play.GameMode != DbGameMode.Mania) continue;
				if (play.Mods.ToRatingFlag() == false && play.RatingAccuracy > (htMax?.RatingAccuracy ?? 0))
					htMax = play;
				else if (play.Mods.ToRatingFlag() == true && play.RatingAccuracy > (dtMax?.RatingAccuracy ?? 0))
					dtMax = play;
				else if (play.Mods.ToRatingFlag() == null && play.RatingAccuracy > (nmMax?.RatingAccuracy ?? 0))
					nmMax = play;
			}

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

		await File.WriteAllBytesAsync(_beatmapConstantCachePath, MemoryPackSerializer.Serialize(this._beatmapChartConstantCache));

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
