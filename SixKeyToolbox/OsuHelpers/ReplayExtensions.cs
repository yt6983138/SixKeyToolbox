using OsuParsers.Decoders;
using OsuParsers.Enums;
using OsuParsers.Replays;

namespace SixKeyToolbox.OsuHelpers;

public static class ReplayExtensions
{
	extension(Replay self)
	{
		public int TotalHits => self.Count300 + self.Count100 + self.Count50 + self.CountGeki + self.CountKatu + self.CountMiss;
		/// <summary>
		/// 0 ~ 100, NaN if not mania, https://github.com/Siflorite/mania-rating-gui/blob/b5c8de4b3e83d82f6f83fe8220cb687eb4214280/src/db/ratings.rs#L192
		/// </summary>
		public double Accuracy
		{
			get
			{
				if (self.Ruleset != Ruleset.Mania)
					return double.NaN;

				return RatingCalculator.CalculateAccuracy(
					self.CountGeki,
					self.Count300,
					self.CountKatu,
					self.Count100,
					self.Count50,
					self.CountMiss,
					self.Mods.HasFlag(Mods.ScoreV2));
			}
		}
		/// <inheritdoc cref="Accuracy"/>
		public double RatingAccuracy
		{
			get
			{
				if (self.Ruleset != Ruleset.Mania)
					return double.NaN;

				return RatingCalculator.CalculateRatingAccuracy(
					self.CountGeki,
					self.Count300,
					self.CountKatu,
					self.Count100,
					self.Count50,
					self.CountMiss);
			}
		}

		public double Calculate6KRating(double chartConstant)
		{
			return RatingCalculator.Calculate6KRating(chartConstant, self.RatingAccuracy);
		}
	}
	extension(ReplayDecoder)
	{
		public static async Task<Replay> DecodeAsync(Stream stream)
		{
			using MemoryStream memoryStream = new();
			await stream.CopyToAsync(memoryStream);
			memoryStream.Seek(0, SeekOrigin.Begin);
			return ReplayDecoder.Decode(memoryStream);
		}
	}
}
