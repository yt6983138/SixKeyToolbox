using Coosu.Database.DataTypes;

namespace SixKeyToolbox.Models;

public class RecentReplay
{
	public required DateTime Timestamp { get; set; }
	public required string Player { get; set; }
	public required Mods Mods { get; set; }
	public required int CountGeki { get; set; }
	public required int Count300 { get; set; }
	public required int CountKatu { get; set; }
	public required int Count100 { get; set; }
	public required int Count50 { get; set; }
	public required int CountMiss { get; set; }
	public required string BeatmapHash { get; set; }
	public required string BeatmapTitle { get; set; }
	public required string BeatmapDifficulty { get; set; }

	public string Label => $"{this.BeatmapTitle} — {this.Player} ({this.Timestamp:yyyy-MM-dd HH:mm})";
}
