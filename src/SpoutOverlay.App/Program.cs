using System.Windows.Forms;
using SpoutOverlay.App.App;

namespace SpoutOverlay.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var context = new OverlayApplicationContext();
        Application.Run(context);
    }
}
