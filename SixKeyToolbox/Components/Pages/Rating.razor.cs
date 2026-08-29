using Microsoft.AspNetCore.Components;
using SixKeyToolbox.Models;
using SixKeyToolbox.Services;
using System.Collections.Concurrent;

namespace SixKeyToolbox.Components.Pages;

public partial class Rating : ComponentBase
{
	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;
	[Inject]
	internal ILogger<Rating> Logger { get; set; } = null!;

	private ToolSettings _settings = ToolSettings.Default;
	private List<string> _players = [];
	private string? _selectedPlayer = null;

	public List<RatingPlay> Plays { get; set; } = [];
	public int TopN { get; set; } = 50;
	public string? LoadError { get; set; }
	public bool IsUpdating { get; private set; }

	public IReadOnlyList<RatingPlay> TopPlays =>
		this.Plays
			.Where(p => this._selectedPlayer == null || p.PlayerName == this._selectedPlayer)
			.OrderByDescending(p => p.Rating)
			.Take(this.TopN > 0 ? this.TopN : 50)
			.ToList();

	protected override async Task OnInitializedAsync()
	{
		this._settings = await this.LocalService.GetSettingsAsync();
		await this.Reload(false);
		await base.OnInitializedAsync();
	}

	public async Task Reload(bool isManual)
	{
		this.LoadError = null;
		try
		{
			await this.LocalService.ReloadDatabasesAsync();
			this._players = await this.LocalService.GetPlayersAsync();

			if (isManual || this.LocalService.RatingCache.IsEmpty)
			{
				this.IsUpdating = true;
				this.StateHasChanged();
				_ = Task.Run(UpdateCore);
			}
			else
			{
				this.Plays = this.LocalService.RatingCache.OrderDescending().ToList();
			}
		}
		catch (Exception ex)
		{
			this.LoadError = $"Could not load plays: {ex.Message}";
			this.Plays = [];
			this.IsUpdating = false;
		}

		async Task UpdateCore()
		{
			try
			{
				ConcurrentBag<RatingPlay> result = await this.LocalService.TryUpdatingPlaysOnceAsync();
				this.Plays = result.OrderDescending().ToList();
				// same reason in home
				GC.Collect();
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Failed to refresh rating");
				this.LoadError = $"Could not load plays: {ex.Message}";
				this.Plays = [];
			}
			finally
			{
				this.IsUpdating = false;
				await this.InvokeAsync(this.StateHasChanged);
			}
		}
	}

	public void OnPlayerChanged(ChangeEventArgs e)
	{
		string? value = e.Value?.ToString();
		this._selectedPlayer = string.IsNullOrEmpty(value) ? null : value;
	}
}
