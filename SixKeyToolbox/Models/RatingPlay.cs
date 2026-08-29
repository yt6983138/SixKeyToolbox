using Coosu.Database.DataTypes;
using OsuParsers.Replays;
using SixKeyToolbox.OsuHelpers;

namespace SixKeyToolbox.Models;

public class RatingPlay : IComparable<RatingPlay>
{
	public required string Title { get; set; }
	public required string DifficultyName { get; set; }
	public required string Artist { get; set; }
	public required double ChartConstant { get; set; }
	public required string PlayerName { get; set; }
	public required Mods Mods { get; set; }
	public required bool IsScoreV2 { get; set; }
	public required int CountGeki { get; set; }
	public required int Count300 { get; set; }
	public required int CountKatu { get; set; }
	public required int Count100 { get; set; }
	public required int Count50 { get; set; }
	public required int CountMiss { get; set; }

	public double RatingAccuracy =>
		RatingCalculator.CalculateRatingAccuracy(
			this.CountGeki, this.Count300, this.CountKatu,
			this.Count100, this.Count50, this.CountMiss);

	public double Accuracy =>
		RatingCalculator.CalculateAccuracy(
			this.CountGeki, this.Count300, this.CountKatu,
			this.Count100, this.Count50, this.CountMiss, this.IsScoreV2);

	public double Rating =>
		RatingCalculator.Calculate6KRating(this.ChartConstant, this.RatingAccuracy);

	public string ChartConstantText => $"[{this.ChartConstant:F1}]";
	public string RatingText => $"{this.Rating:F2}";
	public string AccuracyText => $"{this.Accuracy:F2}%";
	public string ModsText => this.Mods.ToString();

	public static RatingPlay FromReplay(Replay replay, string title, string difficultyName, string artist, double chartConstant)
	{
		return new RatingPlay
		{
			Title = title,
			DifficultyName = difficultyName,
			Artist = artist,
			ChartConstant = chartConstant,
			PlayerName = replay.PlayerName,
			Mods = (Mods)(int)replay.Mods,
			IsScoreV2 = replay.Mods.HasFlag(OsuParsers.Enums.Mods.ScoreV2),
			CountGeki = replay.CountGeki,
			Count300 = replay.Count300,
			CountKatu = replay.CountKatu,
			Count100 = replay.Count100,
			Count50 = replay.Count50,
			CountMiss = replay.CountMiss
		};
	}
	public static RatingPlay FromScore(Score score, string title, string difficultyName, string artist, double chartConstant)
	{
		return new RatingPlay
		{
			Title = title,
			DifficultyName = difficultyName,
			Artist = artist,
			ChartConstant = chartConstant,
			PlayerName = score.Player,
			Mods = score.Mods,
			IsScoreV2 = score.Mods.HasFlag(Mods.ScoreV2),
			CountGeki = score.CountGeki,
			Count300 = score.Count300,
			CountKatu = score.CountKatu,
			Count100 = score.Count100,
			Count50 = score.Count50,
			CountMiss = score.CountMiss
		};
	}

	public int CompareTo(RatingPlay? other)
	{
		if (other is null) return 1;
		return this.Rating.CompareTo(other.Rating);
	}
}
