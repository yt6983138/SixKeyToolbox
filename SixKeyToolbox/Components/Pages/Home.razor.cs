using Microsoft.AspNetCore.Components;
using SixKeyToolbox.Services;

namespace SixKeyToolbox.Components.Pages;

public partial class Home : ComponentBase
{
	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;

	public int TotalPlays { get; set; }
	public double TopRating { get; set; }
	public string TopRatingText => this.TopRating > 0 ? $"{this.TopRating:F2}" : "—";
	public string? StatsError { get; set; }
	public bool IsUpdating { get; private set; }

	protected override async Task OnInitializedAsync()
	{
		try
		{
			if (this.LocalService.RatingCache.IsEmpty)
			{
				this.IsUpdating = true;
				this.StateHasChanged();
				_ = Task.Run(this.LocalService.UpdateRatingPlaysAsync)
					.ContinueWith(async x =>
					{
						this.IsUpdating = false;
						await this.InvokeAsync(this.StateHasChanged);
						this.TotalPlays = this.LocalService.RatingCache.Count;
						this.TopRating = this.LocalService.RatingCache.Count != 0 ? this.LocalService.RatingCache.Max(p => p.Rating) : 0;
					});
			}

			this.TotalPlays = this.LocalService.RatingCache.Count;
			this.TopRating = this.LocalService.RatingCache.Count != 0 ? this.LocalService.RatingCache.Max(p => p.Rating) : 0;
		}
		catch (Exception ex)
		{
			this.StatsError = $"Could not load stats: {ex.Message}";
			this.IsUpdating = false;
		}

		await base.OnInitializedAsync();
	}
}
