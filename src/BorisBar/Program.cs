using System.Threading;

namespace BorisBar;

internal static class Program
{
    private const string MutexName = @"Local\BorisBar.ercoppa";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out bool created);
        if (!created)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new BorisApplicationContext());
    }
}
