using Microsoft.AspNetCore.Components;
using SixKeyToolbox.Models;
using SixKeyToolbox.Services;

namespace SixKeyToolbox.Components.Pages;

public partial class Rating : ComponentBase
{
	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;

	public List<RatingPlay> Plays { get; set; } = [];
	public int TopN { get; set; } = 50;
	public string? LoadError { get; set; }
	public bool IsUpdating { get; private set; }

	public IReadOnlyList<RatingPlay> TopPlays =>
		this.Plays
			.OrderByDescending(p => p.Rating)
			.Take(this.TopN > 0 ? this.TopN : 50)
			.ToList();

	protected override async Task OnInitializedAsync()
	{
		await base.OnInitializedAsync();
		_ = this.Reload(false);
	}

	public async Task Reload(bool isManual)
	{
		this.LoadError = null;
		try
		{
			await this.LocalService.ReloadDatabasesAsync();

			if (isManual || this.LocalService.RatingCache.IsEmpty)
			{
				this.IsUpdating = true;
				this.StateHasChanged();
				_ = Task.Run(this.LocalService.UpdateRatingPlaysAsync)
					.ContinueWith(async x =>
					{
						this.IsUpdating = false;
						await this.InvokeAsync(this.StateHasChanged);
						this.Plays = this.LocalService.RatingCache.OrderDescending().ToList();
					});
			}
			this.Plays = this.LocalService.RatingCache.OrderDescending().ToList();
		}
		catch (Exception ex)
		{
			this.LoadError = $"Could not load plays: {ex.Message}";
			this.Plays = [];
			this.IsUpdating = false;
		}
	}
}
