using Coosu.Beatmap;
using Coosu.Beatmap.Sections.HitObject;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SixKeyToolbox.Models;
using SixKeyToolbox.OsuHelpers;

namespace SixKeyToolbox.Components;

public class SplitSection
{
	public int StartMs { get; set; }
	public int NoteCount { get; set; }
	public int LNCount { get; set; }
	public int GetTotalCount(bool isScoreV2) => this.NoteCount + (this.LNCount * (isScoreV2 ? 2 : 1));
	public DanSection ToDanSection(string name) => new()
	{
		Name = name,
		StartMilliseconds = this.StartMs,
		NoteCount = this.NoteCount,
		LNCount = this.LNCount,
	};
}
public partial class DensitySplitView : ComponentBase, IAsyncDisposable
{
	private const int BucketCount = 200;
	private OsuFile? _lastBeatmap;
	private string? _lastAudioSrc;
	private List<SplitSection> _sections = [new(), new(), new(), new()];

	[Inject]
	internal IJSRuntime JS { get; set; } = null!;

	[Parameter]
	public OsuFile? Beatmap { get; set; }

	[Parameter]
	public EventCallback<DensitySplitView> SplitPointChanged { get; set; }

	[Parameter]
	public string? AudioSrc { get; set; }

	public string AudioId { get; } = "dsv-audio";
	public double AudioPos { get; set; }
	public double AudioDuration { get; set; }
	public bool IsPlaying { get; set; }

	public bool HasData { get; set; }
	public List<int> Density { get; set; } = [];
	public List<(double X, double Y, double W, double H)> DensityBars { get; set; } = [];
	public double MinMs { get; private set; }
	public double MaxMs { get; private set; } = 1;
	public IReadOnlyList<SplitSection> Sections => this._sections;

	public async Task ApplyPreset(DanDefinition preset)
	{
		if (preset.Sections.Count != this._sections.Count) return;
		for (int i = 0; i < this._sections.Count; i++)
		{
			this._sections[i].StartMs = preset.Sections[i].StartMilliseconds;
		}

		List<RawHitObject> objects = this.Beatmap?.HitObjects?.HitObjectList ?? [];
		for (int i = 0; i < this._sections.Count; i++)
		{
			SplitSection updateSection = this._sections[i];
			double endMs = i == this._sections.Count - 1 ? this.MaxMs : this._sections[i + 1].StartMs;
			updateSection.NoteCount = objects.GetNoteCount(updateSection.StartMs, endMs);
			updateSection.LNCount = objects.GetLNCount(updateSection.StartMs, endMs);
		}

		this.RecomputeBars();
		await this.SplitPointChanged.InvokeAsync(this);
		this.StateHasChanged();
	}

	protected override async Task OnParametersSetAsync()
	{
		if (this.AudioSrc != this._lastAudioSrc)
		{
			this._lastAudioSrc = this.AudioSrc;
			this.AudioPos = 0;
			this.AudioDuration = 0;
			if (this.IsPlaying)
			{
				this.IsPlaying = false;
				try
				{
					await this.JS.InvokeVoidAsync("pauseAudio", this.AudioId);
				}
				catch { }
			}
		}

		if (!ReferenceEquals(this.Beatmap, this._lastBeatmap))
		{
			this._lastBeatmap = this.Beatmap;
			if (this.Beatmap is null)
			{
				this.HasData = false;
				this.Density = [];
				this.DensityBars = [];
			}
			else
			{
				this.LoadDensity(this.Beatmap);
				this.HasData = this.Density.Count > 0;
			}
		}
	}

	private void LoadDensity(OsuFile osu)
	{
		List<RawHitObject> hits = osu.HitObjects?.HitObjectList ?? [];
		if (hits.Count == 0)
		{
			this.Density = [];
			this.DensityBars = [];
			return;
		}

		this.MinMs = hits[0].Offset;
		this.MaxMs = hits[^1].Offset;
		double span = Math.Max(1.0, this.MaxMs - this.MinMs);
		double step = span / BucketCount;

		int[] buckets = new int[BucketCount];
		foreach (RawHitObject h in hits)
		{
			int b = (int)Math.Clamp((h.Offset - this.MinMs) / step, 0, BucketCount - 1);
			buckets[b]++;
		}

		this.Density = [.. buckets];
		for (int i = 1; i < this._sections.Count; i++)
		{
			this._sections[i].StartMs = (int)(this.MinMs + (i * span / this._sections.Count));
		}
		this.RecomputeBars();
	}

	private void RecomputeBars()
	{
		int max = this.Density.Count == 0 ? 1 : Math.Max(1, this.Density.Max());
		double barW = 1000.0 / BucketCount;
		this.DensityBars = this.Density
			.Select((v, i) => (X: i * barW, Y: 120.0 - (120.0 * v / max), W: barW, H: 120.0 * v / max))
			.ToList();
	}

	private double MsToX(double ms)
	{
		if (this.MaxMs <= this.MinMs) return 0;
		return 1000.0 * (ms - this.MinMs) / (this.MaxMs - this.MinMs);
	}

	private async Task OnSliderInput(SplitSection section, ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out double v))
		{
			section.StartMs = (int)v;
			this._sections.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));

			List<RawHitObject> objects = this.Beatmap?.HitObjects?.HitObjectList ?? [];
			for (int i = 0; i < this._sections.Count; i++)
			{
				SplitSection updateSection = this._sections[i];
				double endMs = i == this._sections.Count - 1 ? this.MaxMs : this._sections[i + 1].StartMs;
				updateSection.NoteCount = objects.GetNoteCount(updateSection.StartMs, endMs);
				updateSection.LNCount = objects.GetLNCount(updateSection.StartMs, endMs);
			}

			this.RecomputeBars();
			await this.SplitPointChanged.InvokeAsync(this);
		}
	}

	public async Task TogglePlay()
	{
		if (this.AudioDuration <= 0 && this.AudioSrc is null) return;
		this.IsPlaying = !this.IsPlaying;
		if (this.IsPlaying)
		{
			await this.JS.InvokeVoidAsync("playAudio", this.AudioId);
			_ = this.PollPositionAsync();
		}
		else
		{
			await this.JS.InvokeVoidAsync("pauseAudio", this.AudioId);
		}
	}

	public async Task OnAudioMeta()
	{
		this.AudioDuration = await this.JS.InvokeAsync<double>("getAudioDuration", this.AudioId);
	}

	public async Task OnAudioScrub(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out double pos))
		{
			this.AudioPos = pos;
			await this.JS.InvokeVoidAsync("setAudioCurrentTime", this.AudioId, pos);
		}
	}

	private async Task PollPositionAsync()
	{
		while (this.IsPlaying)
		{
			this.AudioPos = await this.JS.InvokeAsync<double>("getAudioCurrentTime", this.AudioId);
			if (this.AudioPos >= this.AudioDuration && this.AudioDuration > 0)
			{
				this.IsPlaying = false;
				this.AudioPos = 0;
				await this.JS.InvokeVoidAsync("pauseAudio", this.AudioId);
				await this.JS.InvokeVoidAsync("setAudioCurrentTime", this.AudioId, 0);
				break;
			}
			this.StateHasChanged();
			await Task.Delay(200);
		}
		this.StateHasChanged();
	}

	public async ValueTask DisposeAsync()
	{
		if (this.IsPlaying)
		{
			this.IsPlaying = false;
			try
			{
				await this.JS.InvokeVoidAsync("pauseAudio", this.AudioId);
			}
			catch { }
		}
	}
}
