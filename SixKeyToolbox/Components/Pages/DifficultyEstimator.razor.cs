using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SixKeyToolbox.OsuHelpers;
using StarRatingRebirth;

namespace SixKeyToolbox.Components.Pages;

public enum SelectableMods
{
	NM,
	HT,
	DT_NC
}
public partial class DifficultyEstimator : ComponentBase
{
	private static readonly Dictionary<SelectableMods, bool?> ModToFlag = new()
	{
		{ SelectableMods.NM, null },
		{ SelectableMods.HT, false },
		{ SelectableMods.DT_NC, true },
	};

	private ManiaData? _maniaData;

	public double? ChartConstant { get; set; }
	public string? FileError { get; set; }

	public SelectableMods SelectedMod { get; set; } = SelectableMods.NM;

	public string RatingMode { get; set; } = "accuracy";
	public double? AccuracyInput { get; set; }
	public int Judgement300G { get; set; }
	public int Judgement300 { get; set; }
	public int Judgement200 { get; set; }
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

	public double EstimatedRating =>
		this.ChartConstant is { } cc && this.RatingAccuracy is { } acc
			? RatingCalculator.Calculate6KRating(cc, acc)
			: 0;

	private int TotalJudgements =>
		this.Judgement300G + this.Judgement300 + this.Judgement200 + this.Judgement100 + this.Judgement50 + this.JudgementMiss;

	private bool? CurrentModFlag => ModToFlag[this.SelectedMod];

	private void OnModChanged(string? key)
	{
		this.SelectedMod = key is null ? SelectableMods.NM : Enum.Parse<SelectableMods>(key);
		if (this._maniaData is not null)
			this.ChartConstant = this._maniaData.Calculate6KChartConstant(this.CurrentModFlag);
	}

	private async Task OnFileSelected(InputFileChangeEventArgs e)
	{
		this.FileError = null;
		try
		{
			IBrowserFile file = e.File;
			int len = (int)Math.Min(file.Size, 10 * 1024 * 1024);
			using Stream stream = file.OpenReadStream(len);
			this._maniaData = await ManiaData.FromStreamAsync(stream, len);
			this.ChartConstant = this._maniaData.Calculate6KChartConstant(this.CurrentModFlag);
		}
		catch (Exception ex)
		{
			this.FileError = $"Could not read .osu: {ex.Message}";
			this.ChartConstant = null;
		}

		this.StateHasChanged();
	}

	protected override void OnParametersSet()
	{
		if (this._maniaData is not null)
		{
			this.ChartConstant = this._maniaData.Calculate6KChartConstant(this.CurrentModFlag);
		}
	}
}
