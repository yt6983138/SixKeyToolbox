using Microsoft.AspNetCore.Components;
using SixKeyToolbox.Models;
using SixKeyToolbox.Services;

namespace SixKeyToolbox.Components.Pages;

public partial class Config : ComponentBase
{
	[Inject]
	internal OsuLocalService LocalService { get; set; } = null!;

	public ToolSettings Settings { get; set; } = ToolSettings.Default;
	public List<DanDefinition> DanPresets { get; set; } = [];

	public string? SettingsMessage { get; set; }
	public string SettingsClass { get; set; } = "";
	public string? PresetsMessage { get; set; }
	public string PresetsClass { get; set; } = "";

	protected override async Task OnInitializedAsync()
	{
		this.Settings = await this.LocalService.GetSettingsAsync();
		this.DanPresets = await this.LocalService.GetDanDefinitionsAsync();
	}

	public async Task SaveSettings()
	{
		this.SettingsMessage = null;
		try
		{
			await this.LocalService.SaveSettingsAsync(this.Settings);
			this.SettingsMessage = "Saved.";
			this.SettingsClass = "ok";
		}
		catch (Exception ex)
		{
			this.SettingsMessage = $"Error: {ex.Message}";
			this.SettingsClass = "err";
		}
	}

	public void AddPreset()
	{
		this.DanPresets.Add(new DanDefinition
		{
			Name = $"Dan {this.DanPresets.Count + 1}",
			Sections =
			[
				new() { Name = "Jack", NoteCount = 0, LNCount = 0 },
				new() { Name = "Tech", NoteCount = 0, LNCount = 0 },
				new() { Name = "Stamina", NoteCount = 0, LNCount = 0 },
				new() { Name = "Speed", NoteCount = 0, LNCount = 0 },
			],
		});
	}

	public void RemovePreset(int idx)
	{
		if (idx >= 0 && idx < this.DanPresets.Count)
			this.DanPresets.RemoveAt(idx);
	}

	public async Task SavePresets()
	{
		this.PresetsMessage = null;
		try
		{
			await this.LocalService.SaveDanDefinitionsAsync(this.DanPresets);
			this.PresetsMessage = "Saved.";
			this.PresetsClass = "ok";
		}
		catch (Exception ex)
		{
			this.PresetsMessage = $"Error: {ex.Message}";
			this.PresetsClass = "err";
		}
	}
}
