using Microsoft.Extensions.Logging;
using UraniumUI;
using InstaLoaderMaui.Platforms.Android;
using Plugin.MauiMTAdmob;




#if ANDROID
using Firebase;
using Microsoft.Maui.LifecycleEvents;
#endif

namespace InstaLoaderMaui;

public static class MauiProgram
{
    private static readonly string Tag = nameof(MauiProgram);

    public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseMauiMTAdmob()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFontAwesomeIconFonts();
                fonts.AddMaterialIconFonts();
            });

        // dependency injection
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<IServiceDownload, DownloadService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
