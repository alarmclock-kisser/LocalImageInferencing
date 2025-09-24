using Microsoft.Extensions.DependencyInjection;
using Radzen;
using LocalImageInferencing.Client;
using LocalImageInferencing.WebApp.Components;

namespace LocalImageInferencing.WebApp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Api Base URL aus Konfiguration
			var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
			if (string.IsNullOrWhiteSpace(apiBaseUrl))
			{
				throw new InvalidOperationException("ApiBaseUrl ist nicht konfiguriert. Füge sie zu appsettings.json oder Environment Variables hinzu.");
			}

			var ollamaUrl = builder.Configuration["OllamaUrl"] ?? "http://localhost:11434/";
			var defaultModel = builder.Configuration["DefaultModel"] ?? string.Empty;
			var defaultDark = builder.Configuration.GetValue<bool?>("DefaultDarkMode") ?? false;

			// Blazor + Radzen
			builder.Services.AddRazorPages();
			builder.Services.AddServerSideBlazor();
			builder.Services.AddRadzenComponents();

			// Konfig Service
			builder.Services.AddSingleton(new ApiUrlConfig(apiBaseUrl));
			builder.Services.AddSingleton(new WebAppConfig(defaultModel, defaultDark));

			// Typed HttpClient für ApiClient (DI-fähiger Konstruktor)
			builder.Services.AddHttpClient<ApiClient>((sp, client) =>
			{
				var cfg = sp.GetRequiredService<ApiUrlConfig>();
				client.BaseAddress = new Uri(cfg.BaseUrl);
			});


            builder.Services.AddSignalR(o =>
            {
                // z.B. 128 MB
                o.MaximumReceiveMessageSize = 1024 * 1024 * 128;
            });


            var app = builder.Build();

			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();
			app.UseAntiforgery();

			app.MapBlazorHub();
			app.MapFallbackToPage("/_Host");
			app.MapRazorPages();

			app.Run();
		}
	}

	public class ApiUrlConfig
	{
		public string BaseUrl { get; set; }

		public ApiUrlConfig(string baseUrl)
		{
			this.BaseUrl = baseUrl;
		}
	}

	public class WebAppConfig
	{
		public string DefaultModel { get; }
		public bool DefaultDarkMode { get; }
		public WebAppConfig(string defaultModel, bool defaultDarkMode)
		{
			this.DefaultModel = defaultModel;
			this.DefaultDarkMode = defaultDarkMode;
		}
	}
}
