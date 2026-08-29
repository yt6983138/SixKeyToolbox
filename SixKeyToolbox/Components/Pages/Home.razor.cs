using Microsoft.AspNetCore.Components;
using SixKeyToolbox.Models;
using SixKeyToolbox.Services;
using System.Collections.Concurrent;

namespace SixKeyToolbox.Components.Pages;

public partial class Home : ComponentBase
{
	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;
	[Inject]
	internal ILogger<Home> Logger { get; set; } = null!;

	private ToolSettings _settings = ToolSettings.Default;
	public int TotalPlays { get; set; }
	public double TopRating { get; set; }
	public string TopRatingText => this.TopRating > 0 ? this.TopRating.ToString(this._settings.AccuracyFormat) : "—";
	public string? StatsError { get; set; }
	public bool IsUpdating { get; private set; }

	protected override async Task OnInitializedAsync()
	{
		this._settings = await this.LocalService.GetSettingsAsync();

		try
		{
			if (this.LocalService.RatingCache.IsEmpty)
			{
				this.IsUpdating = true;
				this.StateHasChanged();
				_ = Task.Run(UpdateCore);
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

		async Task UpdateCore()
		{
			try
			{
				ConcurrentBag<RatingPlay> result = await this.LocalService.TryUpdatingPlaysOnceAsync();
				this.TotalPlays = result.Count;
				this.TopRating = !result.IsEmpty ? result.Max(p => p.Rating) : 0;
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Failed to refresh rating");
				this.StatsError = $"Could not load stats: {ex.Message}";
			}
			finally
			{
				this.IsUpdating = false;
				await this.InvokeAsync(this.StateHasChanged);
			}
		}
	}
}
