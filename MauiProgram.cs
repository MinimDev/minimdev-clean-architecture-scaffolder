using ArchStudio.Services;
using Microsoft.Extensions.Logging;

namespace ArchStudio;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        // ── App Services ──────────────────────────
        builder.Services.AddSingleton<WindowsFolderPicker>();
        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<FileManager>();
        builder.Services.AddSingleton<TemplateEngine>();
        builder.Services.AddSingleton<EntityParserService>();
        builder.Services.AddSingleton<CodeGenerator>(sp =>
            new CodeGenerator(
                sp.GetRequiredService<TemplateEngine>(),
                sp.GetRequiredService<FileManager>()));

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
