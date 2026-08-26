using Microsoft.AspNetCore.Components.Forms;
using StarRatingRebirth;
using System.Text;

namespace SixKeyToolbox.Components.Pages;

public partial class Home
{
	public double StarRating { get; set; } = 0.0;

	private async Task HandleFileSelected(InputFileChangeEventArgs e)
	{
		byte[] buffer = new byte[e.File.Size];
		using Stream stream = e.File.OpenReadStream();
		await stream.ReadExactlyAsync(buffer);

		string[] lines = Encoding.UTF8.GetString(buffer).Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
		ManiaData data = ManiaData.FromLines(lines);

		this.StarRating = (SRCalculator.Calculate(data) * 200.0 / 81.0) + (7.0 / 6.0);
	}
}
