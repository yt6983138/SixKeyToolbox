using StarRatingRebirth;
using System.Buffers;
using System.Text;

namespace SixKeyToolbox.OsuHelpers;

public static class OsuExtensions
{
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
	}
}
