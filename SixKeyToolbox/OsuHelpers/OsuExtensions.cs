using Coosu.Beatmap.Sections.HitObject;
using Coosu.Database.DataTypes;
using StarRatingRebirth;
using System.Buffers;
using System.Collections.Immutable;
using System.Text;

namespace SixKeyToolbox.OsuHelpers;

public static class OsuExtensions
{
	public static readonly ImmutableArray<string> RegularDanSections = ["Jack", "Tech", "Stream", "Speed"];
	public static readonly ImmutableArray<string> LNDanSections = ["Hybrid", "Speed", "Inverse", "Release"];

	extension(Mods self)
	{
		public bool? ToRatingFlag()
		{
			if (self.HasFlag(Mods.DoubleTime) || self.HasFlag(Mods.Nightcore)) return true;
			if (self.HasFlag(Mods.HalfTime)) return false;
			return null;
		}
		public bool HasScoreV2 => self.HasFlag(Mods.ScoreV2);
	}
	extension(OsuParsers.Enums.Mods self)
	{
		public bool? ToRatingFlag() => ((Mods)(int)self).ToRatingFlag();
		public bool HasScoreV2 => self.HasFlag(OsuParsers.Enums.Mods.ScoreV2);
	}
	extension(Score self)
	{
		public double Calculate6KRating(double chartConstant)
		{
			return RatingCalculator.Calculate6KRating(chartConstant, self.RatingAccuracy);
		}
		public double Accuracy =>
			RatingCalculator.CalculateAccuracy(
				self.CountGeki, self.Count300, self.CountKatu,
				self.Count100, self.Count50, self.CountMiss, self.Mods.HasFlag(Mods.ScoreV2));

		public double RatingAccuracy =>
			RatingCalculator.CalculateRatingAccuracy(
				self.CountGeki, self.Count300, self.CountKatu,
				self.Count100, self.Count50, self.CountMiss);

	}
	extension(Beatmap self)
	{
		public string? TryGetPath(string osuBaseFolder)
		{
			if (osuBaseFolder is null) return null;
			string path = Path.Combine(osuBaseFolder, "Songs", self.FolderName, self.FileName);
			return File.Exists(path) ? path : null;
		}
	}
	extension(RawHitObject self)
	{
		public int GetColumn(int totalColumns)
		{
			return Math.Clamp((int)Math.Floor(self.X * totalColumns / 512.0), 0, totalColumns - 1);
		}
	}
	extension(IEnumerable<RawHitObject> self)
	{
		public int GetNoteCount(double startMs, double endMs)
		{
			return self.Count(h => !h.RawType.HasFlag(RawObjectType.Hold) && h.Offset >= startMs && h.Offset <= endMs);
		}
		public int GetLNCount(double startMs, double endMs)
		{
			return self.Count(h => h.RawType.HasFlag(RawObjectType.Hold) && h.Offset >= startMs && h.Offset <= endMs);
		}
	}
	extension(ManiaData self)
	{
		public static async Task<ManiaData> FromStreamAsync(Stream stream, int length, Encoding? encoding = null)
		{
			encoding ??= Encoding.UTF8;
			byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
			try
			{
				await stream.ReadExactlyAsync(buffer, 0, length);

				string[] lines = encoding.GetString(buffer, 0, length).Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
				return ManiaData.FromLines(lines);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="modFlag">null = nm, false = ht, true = dt/nc</param>
		/// <returns></returns>
		public double Calculate6KChartConstant(bool? modFlag = null)
		{
			if (modFlag == false)
			{
				self = self.HT();
			}
			else if (modFlag == true)
			{
				self = self.DT();
			}

			// https://github.com/Siflorite/mania-rating-gui/blob/b5c8de4b3e83d82f6f83fe8220cb687eb4214280/src/db/ratings.rs#L279
			return (SRCalculator.Calculate(self) * 200.0 / 81.0) + (7.0 / 6.0);
		}
		public double TryCalculate6KChartConstant(bool? modFlag = null, double defaultValue = 0)
		{
			try
			{
				return self.Calculate6KChartConstant(modFlag);
			}
			catch
			{
				return defaultValue;
			}
		}
	}
}
