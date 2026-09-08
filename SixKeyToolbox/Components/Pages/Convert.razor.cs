using Coosu.Beatmap;
using Coosu.Beatmap.Sections.HitObject;
using Microsoft.AspNetCore.Components;
using SixKeyToolbox.OsuHelpers;
using SixKeyToolbox.Services;

namespace SixKeyToolbox.Components.Pages;

public enum ConversionMode
{
	InverseLN,
	SevenKToSixK,
	ColumnMapping
}

public partial class Convert : ComponentBase
{
	public static IReadOnlyList<string> GapPresets { get; } = ["1/16", "1/8", "1/4", "1/2", "1/3", "1/6"];

	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;

	public ConversionMode Mode { get; set; } = ConversionMode.InverseLN;
	public string GapPreset { get; set; } = "1/8";
	public double OverallDifficulty { get; set; } = 6;

	public int TargetKeyCount { get; set; } = 6;
	public List<int> ColumnMappings { get; set; } = [0, 0, 1, 2, 3, 4];
	public int SourceKeyCount { get; set; }

	public string? SelectedFolder { get; set; }
	public List<BeatmapFile> AvailableFiles { get; set; } = [];

	public string? ResultMessage { get; set; }
	public string ResultClass { get; set; } = "";
	public List<string> ConvertedFiles { get; set; } = [];

	public class BeatmapFile
	{
		public required string Path { get; set; }
		public required string FileName { get; set; }
		public bool IsSelected { get; set; } = true;
	}

	public void UpdateMappingArray()
	{
		if (this.ColumnMappings.Count == this.TargetKeyCount) return;

		List<int> newMappings = [];
		for (int i = 0; i < this.TargetKeyCount; i++)
		{
			if (i < this.ColumnMappings.Count)
				newMappings.Add(this.ColumnMappings[i]);
			else
				newMappings.Add(0);
		}
		this.ColumnMappings = newMappings;
	}

	public async Task PickFolder()
	{
		this.ResultMessage = null;
		this.ConvertedFiles = [];
		this.AvailableFiles = [];
		try
		{
			NativeFileDialogSharp.DialogResult pickResult = await this.LocalService.PickFolderAsync();
			if (pickResult.IsCancelled) return;
			if (pickResult.IsError)
			{
				this.SetResult(false, pickResult.ErrorMessage ?? "Unknown error.");
				return;
			}

			string folder = pickResult.Path;
			if (!Directory.Exists(folder))
			{
				this.SetResult(false, "No folder selected.");
				return;
			}

			this.SelectedFolder = folder;
			string[] osuFiles = Directory.GetFiles(folder, "*.osu", SearchOption.TopDirectoryOnly);
			if (osuFiles.Length == 0)
			{
				this.SetResult(false, "No .osu files found in the folder.");
				return;
			}

			foreach (string path in osuFiles)
			{
				this.AvailableFiles.Add(new BeatmapFile
				{
					Path = path,
					FileName = Path.GetFileName(path),
					IsSelected = true
				});
			}

			this.SetResult(true, $"Loaded {this.AvailableFiles.Count} beatmap(s). Select files to convert and click Convert.");
		}
		catch (Exception ex)
		{
			this.SetResult(false, $"Error: {ex.Message}");
		}
	}

	public async Task ConvertSelected()
	{
		this.ResultMessage = null;
		this.ConvertedFiles = [];

		List<BeatmapFile> selectedFiles = this.AvailableFiles.Where(f => f.IsSelected).ToList();
		if (selectedFiles.Count == 0)
		{
			this.SetResult(false, "No files selected. Please select at least one file to convert.");
			return;
		}

		try
		{
			int ok = 0;
			foreach (BeatmapFile file in selectedFiles)
			{
				string path = file.Path;
				string outPath = this.Mode switch
				{
					ConversionMode.InverseLN => this.GetInversePath(path),
					ConversionMode.SevenKToSixK => this.Get7to6Path(path),
					ConversionMode.ColumnMapping => this.GetColumnMappingPath(path),
					_ => path
				};
				string name = Path.GetFileName(outPath);

				if (this.Mode == ConversionMode.InverseLN)
				{
					await Task.Run(() => this.ConvertOneInverse(path, outPath));
				}
				else if (this.Mode == ConversionMode.SevenKToSixK)
				{
					OsuFile osu = OsuFile.ReadFromFile(path);
					if (osu.Difficulty!.CircleSize != 7) continue;
					await Task.Run(() => this.Convert7KTo6K(osu, outPath));
				}
				else if (this.Mode == ConversionMode.ColumnMapping)
				{
					await Task.Run(() => this.ConvertColumnMapping(path, outPath));
				}

				this.ConvertedFiles.Add(name);
				ok++;
			}

			string resultMsg = this.Mode switch
			{
				ConversionMode.InverseLN => $"Converted {ok} beatmap(s) to inverse.",
				ConversionMode.SevenKToSixK => $"Converted {ok} beatmap(s) from 7K to 6K.",
				ConversionMode.ColumnMapping => $"Converted {ok} beatmap(s) with column mapping to {this.TargetKeyCount}K.",
				_ => $"Converted {ok} beatmap(s)."
			};
			this.SetResult(true, resultMsg);
		}
		catch (Exception ex)
		{
			this.SetResult(false, $"Error: {ex.Message}");
		}
	}

	private void ConvertOneInverse(string inPath, string outPath)
	{
		OsuFile osu = OsuFile.ReadFromFile(inPath);

		osu.Difficulty!.OverallDifficulty = (float)this.OverallDifficulty;
		osu.Metadata!.Title += "@Inverse";
		osu.Metadata!.TitleUnicode += "@Inverse";
		osu.Metadata?.TagList?.Add("inverse");

		List<RawHitObject> hits = osu.HitObjects?.HitObjectList ?? [];
		int keyCount = (int?)osu.Difficulty?.CircleSize ?? -1;
		if (keyCount < 0)
		{
			this.SetResult(false, $"Invalid beatmap in {inPath}.");
			return;
		}

		Dictionary<int, List<RawHitObject>> byColumn = [];
		foreach (RawHitObject h in hits)
		{
			int col = h.GetColumn(keyCount);
			if (!byColumn.TryGetValue(col, out List<RawHitObject>? list))
			{
				list = [];
				byColumn[col] = list;
			}
			list.Add(h);
		}
		foreach (List<RawHitObject> list in byColumn.Values)
			list.Sort((a, b) => a.Offset.CompareTo(b.Offset));

		if (osu.TimingPoints is null)
		{
			this.SetResult(false, $"Beatmap has no timing points in {inPath}.");
			return;
		}

		double gapRatio = this.GetGapRatio();

		foreach (KeyValuePair<int, List<RawHitObject>> pair in byColumn)
		{
			List<RawHitObject> notes = pair.Value;
			for (int i = 0; i < notes.Count; i++)
			{
				RawHitObject cur = notes[i];
				if (i == notes.Count - 1)
				{
					this.MakeNote(cur);
					continue;
				}

				RawHitObject next = notes[i + 1];
				double beatLen = osu.TimingPoints.GetRedLine((double)cur.Offset).Factor;
				double gap = gapRatio * beatLen;
				double end = next.Offset - gap;

				if (end - cur.Offset < gap)
				{
					this.MakeNote(cur);
					continue;
				}

				this.MakeLn(cur, (int)Math.Round(end));
			}
		}

		osu.Save(outPath);
	}

	private void Convert7KTo6K(OsuFile osu, string outPath)
	{
		osu.Difficulty!.CircleSize = 6;
		osu.Metadata!.Title += "@7to6DelSpace";
		osu.Metadata!.TitleUnicode += "@7to6DelSpace";
		osu.Metadata?.TagList?.Add("7to6");
		osu.Metadata?.TagList?.Add("delspace");

		List<RawHitObject> hits = osu.HitObjects?.HitObjectList ?? [];
		List<RawHitObject> kept = [];

		foreach (RawHitObject h in hits)
		{
			int col = h.GetColumn(7);
			if (col == 3) continue;

			int newCol = col < 3 ? col : col - 1;
			h.X = (float)Math.Round(((newCol * 512.0) + 256.0) / 6.0);
			kept.Add(h);
		}

		osu.HitObjects!.HitObjectList = kept;
		osu.Save(outPath);
	}

	private void ConvertColumnMapping(string inPath, string outPath)
	{
		OsuFile osu = OsuFile.ReadFromFile(inPath);

		int sourceKeyCount = (int?)osu.Difficulty?.CircleSize ?? -1;
		if (sourceKeyCount < 0)
		{
			this.SetResult(false, $"Invalid beatmap in {inPath}.");
			return;
		}

		this.SourceKeyCount = sourceKeyCount;

		List<RawHitObject> sourceHits = osu.HitObjects?.HitObjectList ?? [];
		Dictionary<int, List<RawHitObject>> bySourceColumn = [];

		foreach (RawHitObject h in sourceHits)
		{
			int col = h.GetColumn(sourceKeyCount);
			if (!bySourceColumn.TryGetValue(col, out List<RawHitObject>? list))
			{
				list = [];
				bySourceColumn[col] = list;
			}
			list.Add(h);
		}

		List<RawHitObject> newHits = [];
		HashSet<int> warnedColumns = [];

		for (int targetCol = 0; targetCol < this.TargetKeyCount; targetCol++)
		{
			if (targetCol >= this.ColumnMappings.Count) continue;

			int mapping = this.ColumnMappings[targetCol];
			if (mapping == 0) continue;

			int sourceCol = mapping - 1;
			if (sourceCol < 0 || sourceCol >= sourceKeyCount)
			{
				warnedColumns.Add(mapping);
				continue;
			}

			if (!bySourceColumn.TryGetValue(sourceCol, out List<RawHitObject>? sourceNotes))
				continue;

			foreach (RawHitObject sourceNote in sourceNotes)
			{
				RawHitObject newNote = new()
				{
					Offset = sourceNote.Offset,
					Y = sourceNote.Y,
					RawType = sourceNote.RawType,
					HoldEnd = sourceNote.HoldEnd,
					X = (float)Math.Round(((targetCol * 512.0) + 256.0) / this.TargetKeyCount)
				};
				newHits.Add(newNote);
			}
		}

		if (warnedColumns.Count > 0)
		{
			string warned = string.Join(", ", warnedColumns.OrderBy(x => x));
			this.SetResult(false, $"Warning: Invalid source column(s) {warned} for {sourceKeyCount}K map. These columns were skipped.");
		}

		newHits.Sort((a, b) => a.Offset.CompareTo(b.Offset));

		string suffix = $"@{osu.Difficulty!.CircleSize}ColMap{string.Join(',', this.ColumnMappings)}";
		osu.Metadata!.Title += suffix;
		osu.Difficulty!.CircleSize = this.TargetKeyCount;
		osu.Metadata!.TitleUnicode += suffix;
		osu.Metadata?.TagList?.Add(suffix[1..].ToLowerInvariant());
		osu.HitObjects!.HitObjectList = newHits;

		osu.Save(outPath);
	}

	private double GetGapRatio()
	{
		return this.GapPreset switch
		{
			"1/16" => 1.0 / 8.0,
			"1/8" => 1.0 / 4.0,
			"1/4" => 1.0 / 2.0,
			"1/2" => 1,
			"1/3" => 2 / 3.0,
			"1/6" => 1 / 3.0,
			_ => 1,
		};
	}

	private void MakeNote(RawHitObject h)
	{
		h.RawType &= ~RawObjectType.Hold;
		h.RawType |= RawObjectType.Circle;
		h.HoldEnd = 0;
	}

	private void MakeLn(RawHitObject h, int endMs)
	{
		h.RawType &= ~RawObjectType.Circle;
		h.RawType |= RawObjectType.Hold;
		h.HoldEnd = endMs;
	}

	private string GetInversePath(string path)
	{
		string dir = Path.GetDirectoryName(path)!;
		string fn = Path.GetFileNameWithoutExtension(path);
		if (!fn.EndsWith("@Inverse", StringComparison.Ordinal))
			fn += "@Inverse";
		return Path.Combine(dir, fn + ".osu");
	}

	private string Get7to6Path(string path)
	{
		string dir = Path.GetDirectoryName(path)!;
		string fn = Path.GetFileNameWithoutExtension(path);
		if (!fn.EndsWith("@7to6DelSpace", StringComparison.Ordinal))
			fn += "@7to6DelSpace";
		return Path.Combine(dir, fn + ".osu");
	}

	private string GetColumnMappingPath(string path)
	{
		string dir = Path.GetDirectoryName(path)!;
		string fn = Path.GetFileNameWithoutExtension(path);
		if (!fn.EndsWith("@ColMap", StringComparison.Ordinal))
			fn += "@ColMap";
		return Path.Combine(dir, fn + ".osu");
	}

	private void SetResult(bool ok, string msg)
	{
		this.ResultMessage = msg;
		this.ResultClass = ok ? "ok" : "err";
	}
}
