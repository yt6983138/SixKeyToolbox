namespace SixKeyToolbox.Models;

public class ToolSettings
{
	public static ToolSettings Default => new();

	public string OsuBaseFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
	public string AccuracyFormat { get; set; } = "F2";
	public string ChartConstantFormat { get; set; } = "F2";
}
