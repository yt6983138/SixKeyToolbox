using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SixKeyToolbox.Models;
using SixKeyToolbox.OsuHelpers;
using SixKeyToolbox.Services;
using StarRatingRebirth;
using System.Text.Json;

namespace SixKeyToolbox.Components.Pages;

public enum SelectableMods
{
	NM,
	HT,
	DT_NC
}

public class BeatmapResult
{
	public string Name { get; set; } = "";
	public ManiaData Data { get; set; } = null!;
	public double ChartConstant { get; set; }
	public double? Rating { get; set; }
}

public partial class DifficultyEstimator : ComponentBase
{
	private static readonly Dictionary<SelectableMods, bool?> ModToFlag = new()
	{
		{ SelectableMods.NM, null },
		{ SelectableMods.HT, false },
		{ SelectableMods.DT_NC, true },
	};

	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;
	[Inject]
	internal ILogger<DifficultyEstimator> Logger { get; set; } = null!;

	private ToolSettings _settings = ToolSettings.Default;
	public List<BeatmapResult> Results { get; set; } = [];
	public string? FileError { get; set; }

	public SelectableMods SelectedMod { get; set; } = SelectableMods.NM;

	public string RatingMode { get; set; } = "accuracy";
	public double? AccuracyInput { get; set; }
	public int Judgement300G { get; set; }
	public int Judgement200 { get; set; }
	public int Judgement300 { get; set; }
	public int Judgement100 { get; set; }
	public int Judgement50 { get; set; }
	public int JudgementMiss { get; set; }
	public bool IsScoreV2 { get; set; }

	public double? RatingAccuracy =>
		this.RatingMode == "accuracy"
			? this.AccuracyInput
			: (this.TotalJudgements == 0
				? null
				: RatingCalculator.CalculateRatingAccuracy(
					this.Judgement300G, this.Judgement300, this.Judgement200, this.Judgement100, this.Judgement50, this.JudgementMiss));

	public double DisplayedAccuracy =>
		this.TotalJudgements == 0
			? 0
			: RatingCalculator.CalculateAccuracy(
				this.Judgement300G, this.Judgement300, this.Judgement200, this.Judgement100, this.Judgement50, this.JudgementMiss, this.IsScoreV2);

	private int TotalJudgements =>
		this.Judgement300G + this.Judgement300 + this.Judgement200 + this.Judgement100 + this.Judgement50 + this.JudgementMiss;

	private bool? CurrentModFlag => ModToFlag[this.SelectedMod];

	protected override async Task OnInitializedAsync()
	{
		this._settings = await this.LocalService.GetSettingsAsync();
	}

	private void OnModChanged(string? key)
	{
		this.SelectedMod = key is null ? SelectableMods.NM : Enum.Parse<SelectableMods>(key);
		this.RecalculateAll();
	}

	private void RecalculateAll()
	{
		foreach (BeatmapResult result in this.Results)
		{
			// -1 was to test if its a render issue or it really returned nan, some users had issues with the chart constant being nan
			result.ChartConstant = result.Data.TryCalculate6KChartConstant(this.CurrentModFlag, -1);
			result.Rating = this.RatingAccuracy is { } acc
				? RatingCalculator.Calculate6KRating(result.ChartConstant, acc)
				: null;
		}

		this.StateHasChanged();
	}

	private async Task OnFilesSelected(InputFileChangeEventArgs e)
	{
		this.FileError = null;
		this.Results.Clear();

		try
		{
			foreach (IBrowserFile file in e.GetMultipleFiles(50))
			{
				try
				{
					int len = (int)Math.Min(file.Size, 10 * 1024 * 1024);
					using Stream stream = file.OpenReadStream(len);
					ManiaData maniaData = await ManiaData.FromStreamAsync(stream, len);
					this.Logger.LogInformation("Loaded {f} with {c} notes, od {od}, cs {cs}, checking for irregularities", file.Name, maniaData.Notes.Count, maniaData.OD, maniaData.CS);
					for (int i = 0; i < maniaData.Notes.Count; i++)
					{
						// maybe we need to do more validation here,
						// a user is reporting that some maps just shows nan, but it does work
						// on my dev machine, so maybe we need to check if the notes are valid, or if the maniaData is valid
						Note note = maniaData.Notes[i];
						if (note.Key < 0 || note.Key >= maniaData.CS
							|| note.Head < 0
							|| (note.IsLong && note.Tail < note.Head))
						{
							this.Logger.LogWarning("Note {i} is invalid: {note}", i, JsonSerializer.Serialize(note));
						}
					}
					this.Logger.LogInformation("Checking {f} done", file.Name);

					this.Results.Add(new BeatmapResult
					{
						Name = file.Name,
						Data = maniaData
					});
				}
				catch (Exception ex)
				{
					this.Results.Add(new BeatmapResult
					{
						Name = $"{file.Name} (error: {ex.Message})",
						Data = null!,
						ChartConstant = 0,
						Rating = null
					});
				}
			}

			if (this.Results.Count == 0)
			{
				this.FileError = "No files loaded.";
			}
		}
		catch (Exception ex)
		{
			this.FileError = $"Error loading files: {ex.Message}";
		}

		this.RecalculateAll();
	}

	protected override void OnParametersSet()
	{
		this.RecalculateAll();
	}
}
