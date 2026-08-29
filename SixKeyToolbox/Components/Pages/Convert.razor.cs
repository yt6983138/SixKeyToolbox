using Coosu.Beatmap;
using Coosu.Beatmap.Sections.HitObject;
using Microsoft.AspNetCore.Components;
using SixKeyToolbox.OsuHelpers;
using SixKeyToolbox.Services;

namespace SixKeyToolbox.Components.Pages;

public enum ConversionMode
{
	InverseLN,
	SevenKToSixK
}

public partial class Convert : ComponentBase
{
	public static IReadOnlyList<string> GapPresets { get; } = ["1/16", "1/8", "1/4", "1/2", "1/3", "1/6"];

	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;

	public ConversionMode Mode { get; set; } = ConversionMode.InverseLN;
	public string GapPreset { get; set; } = "1/8";
	public double OverallDifficulty { get; set; } = 6;

	public string? ResultMessage { get; set; }
	public string ResultClass { get; set; } = "";
	public List<string> ConvertedFiles { get; set; } = [];

	public async Task PickAndConvert()
	{
		this.ResultMessage = null;
		this.ConvertedFiles = [];
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

			string[] osuFiles = Directory.GetFiles(folder, "*.osu", SearchOption.TopDirectoryOnly);
			if (osuFiles.Length == 0)
			{
				this.SetResult(false, "No .osu files found in the folder.");
				return;
			}

			int ok = 0;
			foreach (string path in osuFiles)
			{
				string outPath = this.Mode == ConversionMode.InverseLN
					? this.GetInversePath(path)
					: this.Get7to6Path(path);
				string name = Path.GetFileName(outPath);

				if (this.Mode == ConversionMode.InverseLN)
				{
					await Task.Run(() => this.ConvertOneInverse(path, outPath));
				}
				else
				{
					OsuFile osu = OsuFile.ReadFromFile(path);
					if (osu.Difficulty!.CircleSize != 7) continue;
					await Task.Run(() => this.Convert7KTo6K(osu, outPath));
				}

				this.ConvertedFiles.Add(name);
				ok++;
			}

			string resultMsg = this.Mode == ConversionMode.InverseLN
				? $"Converted {ok} beatmap(s) to inverse."
				: $"Converted {ok} beatmap(s) from 7K to 6K.";
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

	private void SetResult(bool ok, string msg)
	{
		this.ResultMessage = msg;
		this.ResultClass = ok ? "ok" : "err";
	}
}
