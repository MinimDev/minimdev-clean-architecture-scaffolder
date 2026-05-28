using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ArchStudio.Services;

/// <summary>
/// Native Windows folder picker using WinRT StoragePicker.
/// Falls back gracefully if picker is unavailable.
/// </summary>
public class WindowsFolderPicker
{
    public async Task<string?> PickFolderAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            // Get the HWND of the current MAUI window
            var hwnd = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler!.PlatformView!).WindowHandle;
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FolderPicker error: {ex.Message}");
            return null;
        }
    }
}
