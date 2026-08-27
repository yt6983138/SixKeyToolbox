namespace SixKeyToolbox.Models;

public class DanSection
{
	public required string Name { get; set; }
	public int StartMilliseconds { get; set; } = 0;
	public required int NoteCount { get; set; }
	public required int LNCount { get; set; }
	public int GetTotalCount(bool isScoreV2) => this.NoteCount + (this.LNCount * (isScoreV2 ? 2 : 1));
}
public class DanDefinition
{
	public static List<DanDefinition> Defaults =>
	[

	];

	public required string Name { get; set; }
	public required List<DanSection> Sections { get; set; }
	public int GetTotalCount(bool isScoreV2) => this.Sections.Sum(s => s.GetTotalCount(isScoreV2));
}
