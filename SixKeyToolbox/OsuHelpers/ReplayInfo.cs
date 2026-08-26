// Licensed under the MIT License
// Original work Copyright (c) 2021 mrflashstudio.
// https://github.com/mrflashstudio/OsuParsers/blob/master/OsuParsers/Decoders/ReplayDecoder.cs
// https://github.com/mrflashstudio/OsuParsers/blob/master/OsuParsers/Replays/Replay.cs

// i didn't use the original library directly, because it seems unmaintained and we already have Coosu, adding this would create a mess

using Coosu.Beatmap.Sections.GamePlay;
using Coosu.Database.DataTypes;

namespace SixKeyToolbox.OsuHelpers;

public class ReplayInfo
{
	public required GameMode Ruleset { get; set; }
	public required int OsuVersion { get; set; }
	public required string BeatmapMD5Hash { get; set; }
	public required string PlayerName { get; set; }
	public required string ReplayMD5Hash { get; set; }
	public required ushort Count300 { get; set; }
	public required ushort Count100 { get; set; }
	public required ushort Count50 { get; set; }
	public required ushort CountGeki { get; set; }
	public required ushort CountKatu { get; set; }
	public required ushort CountMiss { get; set; }
	public required int ReplayScore { get; set; }
	public required ushort Combo { get; set; }
	public required bool PerfectCombo { get; set; }
	public required Mods Mods { get; set; }
	public required DateTime ReplayTimestamp { get; set; }
	public required int ReplayLength { get; set; }
	// public List<ReplayFrame> ReplayFrames { get; set; } = new List<ReplayFrame>();
	// public List<LifeFrame> LifeFrames { get; set; } = new List<LifeFrame>();
	// public int Seed { get; set; }

	/// <summary>
	/// Currently not used
	/// </summary>
	public required long OnlineId { get; set; }

	public int TotalHits => this.Count300 + this.Count100 + this.Count50 + this.CountGeki + this.CountKatu + this.CountMiss;
	/// <summary>
	/// 0 ~ 100, NaN if not mania, https://github.com/Siflorite/mania-rating-gui/blob/b5c8de4b3e83d82f6f83fe8220cb687eb4214280/src/db/ratings.rs#L192
	/// </summary>
	public double Accuracy
	{
		get
		{
			if (this.Ruleset != GameMode.Mania)
				return double.NaN;

			return RatingCalculator.CalculateAccuracy(
				this.CountGeki,
				this.Count300,
				this.CountKatu,
				this.Count100,
				this.Count50,
				this.CountMiss,
				this.Mods.HasFlag(Mods.ScoreV2));
		}
	}
	/// <inheritdoc cref="Accuracy"/>
	public double RatingAccuracy
	{
		get
		{
			if (this.Ruleset != GameMode.Mania)
				return double.NaN;

			return RatingCalculator.CalculateRatingAccuracy(
				this.CountGeki,
				this.Count300,
				this.CountKatu,
				this.Count100,
				this.Count50,
				this.CountMiss);
		}
	}

	public double Calculate6KRating(double chartConstant)
	{
		return RatingCalculator.Calculate6KRating(chartConstant, this.RatingAccuracy);
	}

	public static ReplayInfo FromStream(Stream stream)
	{
		using BinaryReader reader = new(stream);

		return new()
		{
			Ruleset = (GameMode)reader.ReadByte(),
			OsuVersion = reader.ReadInt32(),
			BeatmapMD5Hash = ReadOsuString(reader).EnsureNotNull(),
			PlayerName = ReadOsuString(reader).EnsureNotNull(),
			ReplayMD5Hash = ReadOsuString(reader).EnsureNotNull(),
			Count300 = reader.ReadUInt16(),
			Count100 = reader.ReadUInt16(),
			Count50 = reader.ReadUInt16(),
			CountGeki = reader.ReadUInt16(),
			CountKatu = reader.ReadUInt16(),
			CountMiss = reader.ReadUInt16(),
			ReplayScore = reader.ReadInt32(),
			Combo = reader.ReadUInt16(),
			PerfectCombo = reader.ReadBoolean(),
			Mods = (Mods)reader.ReadInt32(),
			ReplayTimestamp = DateTime.FromBinary(reader.ReadInt64()),
			ReplayLength = reader.ReadInt32(),

			OnlineId = 0 // TODO: check if we really need this
		};

		static string? ReadOsuString(BinaryReader reader)
		{
			byte isPresent = reader.ReadByte();
			if (isPresent == 0x00)
				return null;
			return reader.ReadString();
		}
	}
}
