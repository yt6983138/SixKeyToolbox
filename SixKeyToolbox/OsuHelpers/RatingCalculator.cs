namespace SixKeyToolbox.OsuHelpers;

public static class RatingCalculator
{
	public static double CalculateAccuracy(
		int countGeki,
		int count300,
		int countKatu,
		int count100,
		int count50,
		int countMiss,
		bool isScoreV2)
	{
		int totalHits = countGeki + count300 + countKatu + count100 + count50 + countMiss;
		if (isScoreV2)
		{
			return ((305 * countGeki)
				+ (300 * count300)
				+ (200 * countKatu)
				+ (100 * count100)
				+ (50 * count50))
				/ (3.05 * totalHits);
		}
		else
		{
			return ((300 * (countGeki + count300))
				+ (200 * countKatu)
				+ (100 * count100)
				+ (50 * count50))
				/ (3.0 * totalHits);
		}
	}

	public static double CalculateRatingAccuracy(
		int countGeki,
		int count300,
		int countKatu,
		int count100,
		int count50,
		int countMiss)
	{
		int totalHits = countGeki + count300 + countKatu + count100 + count50 + countMiss;
		return ((310 * countGeki)
			+ (300 * count300)
			+ (200 * countKatu)
			+ (100 * count100)
			+ (50 * count50))
			/ (3.1 * totalHits);
	}
	/// <summary>
	/// https://github.com/Siflorite/mania-rating-gui/blob/b5c8de4b3e83d82f6f83fe8220cb687eb4214280/src/db/ratings.rs#L246
	/// </summary>
	/// <param name="chartConstant"></param>
	/// <param name="acc">0 ~ 100</param>
	/// <returns></returns>
	public static double Calculate6KRating(double chartConstant, double acc)
	{
		if (acc < 0.0 || acc > 100.0)
			return 0;

		double ccLower = Math.Max(chartConstant - 3.0, 0.0);

		if (acc <= 80.0)
		{
			return 0;
		}
		else if (acc <= 93.0)
		{
			return ccLower * (acc - 80.0) / 13.0;
		}
		else if (acc <= 96.0)
		{
			return ((chartConstant - ccLower) * (acc - 93.0) / 3.0) + ccLower;
		}
		else if (acc <= 98.0)
		{
			double accExtra = acc - 96.0;
			return (1.5 * accExtra / (3.0 - (accExtra / 2.0))) + chartConstant;
		}
		else if (acc <= 99.5)
		{
			double accExtra = (acc - 98.0) / 1.5;
			return (2.0 * accExtra * 2.0 / (3.0 - accExtra)) + chartConstant + 1.5;
		}
		else
		{
			double accExtra = (acc - 99.5) * 2.0;
			return (accExtra * 2.0 / (3.0 - accExtra)) + chartConstant + 3.5;
		}
	}
}
