using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using SixKeyToolbox.Components;
using SixKeyToolbox.Services;
using System.Diagnostics;

namespace SixKeyToolbox;

public class Program
{
	private static void StartUrl(string url)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true
		});
	}

	public static async Task Main(string[] args)
	{
		Process currentProcess = Process.GetCurrentProcess();
		if (Process.GetProcesses().Any(x => x.ProcessName == currentProcess.ProcessName && x.Id != currentProcess.Id))
		{
			StartUrl(new Uri(Path.GetFullPath("./wwwroot/AlreadyRunning.html")).AbsoluteUri);
			throw new InvalidOperationException("Another instance of toolbox is already running!");
		}

		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		// Add services to the container.
		builder.Services.AddRazorComponents()
			.AddInteractiveServerComponents(o => o.DetailedErrors = true);
		builder.Services.AddSingleton<OsuLocalService>();

		WebApplication app = builder.Build();

		app.UseStaticFiles();
		app.UseAntiforgery();

		app.MapRazorComponents<App>()
			.AddInteractiveServerRenderMode();

		await app.StartAsync();

		IServer server = app.Services.GetRequiredService<IServer>();
		IServerAddressesFeature? addressFeature = server.Features.Get<IServerAddressesFeature>();
		if (addressFeature is not null && builder.Configuration.GetValue<bool>("StartBrowserImmediately"))
		{
			// prefer http since this is a local app and https may have certificate issues
			string url = addressFeature.Addresses.FirstOrDefault(x => x.StartsWith("http://")) ?? addressFeature.Addresses.First();
			StartUrl(url);
		}

		await app.WaitForShutdownAsync();
	}
}
