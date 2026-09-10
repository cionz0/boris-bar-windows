using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BorisBar;

internal sealed class BorisApplicationContext : ApplicationContext
{
    private readonly AudioPlayer _player = new();
    private readonly HotkeyManager _hotkeys = new();
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;
    private readonly Icon _icon;

    public BorisApplicationContext()
    {
        AppPaths.EnsureDataDirs();

        _icon = IconLoader.LoadFish();
        _menu = new ContextMenuStrip();
        _menu.Opening += (_, _) => RebuildMenu();

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "Boris Bar",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _tray.MouseUp += OnTrayMouseUp;

        _player.Failed += message =>
            _hotkeys.PostToUi(() =>
                _tray.ShowBalloonTip(5000, "Boris Bar", message, ToolTipIcon.Error));

        _hotkeys.ClipRequested += PlayBuiltInIndex;
        var failed = _hotkeys.RegisterWinAltNumberDefaults();
        if (failed.Count > 0)
        {
            _tray.ShowBalloonTip(
                5000,
                "Boris Bar",
                "Alcuni shortcut Win+Alt sono già in uso: " + string.Join(", ", failed.Select(i => "Win+Alt+" + i)),
                ToolTipIcon.Warning);
        }

        RebuildMenu();
    }

    private void OnTrayMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        SetForegroundWindow(_hotkeys.Handle);
        _menu.Show(Cursor.Position);
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        for (int i = 0; i < ClipCatalog.BuiltIns.Length; i++)
        {
            int index = i;
            var clip = ClipCatalog.BuiltIns[i];
            var item = new ToolStripMenuItem(clip.Label)
            {
                ShortcutKeyDisplayString = "Win+Alt+" + (i + 1)
            };
            item.Click += (_, _) => PlayBuiltInIndex(index);
            _menu.Items.Add(item);
        }

        var customClips = ClipLocator.LoadCustomClips();
        if (customClips.Count > 0)
        {
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(new ToolStripMenuItem("Suoni personalizzati") { Enabled = false });
            foreach (var path in customClips)
            {
                var filePath = path;
                var item = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(path));
                item.Click += (_, _) => _player.Play(filePath);
                _menu.Items.Add(item);
            }
        }

        _menu.Items.Add(new ToolStripSeparator());

        var addCustom = new ToolStripMenuItem("Aggiungi suono personalizzato…");
        addCustom.Click += (_, _) => AddCustomSound();
        _menu.Items.Add(addCustom);

        var openDir = new ToolStripMenuItem("Apri cartella suoni");
        openDir.Click += (_, _) => OpenFolder(AppPaths.Custom);
        _menu.Items.Add(openDir);

        var startup = new ToolStripMenuItem("Avvia con Windows")
        {
            Checked = StartupEntry.IsEnabled
        };
        startup.Click += (_, _) =>
        {
            StartupEntry.SetEnabled(!StartupEntry.IsEnabled);
            startup.Checked = StartupEntry.IsEnabled;
        };
        _menu.Items.Add(startup);

        _menu.Items.Add(new ToolStripSeparator());

        var about = new ToolStripMenuItem("Informazioni e disclaimer...");
        about.Click += (_, _) => OpenDisclaimer();
        _menu.Items.Add(about);

        var exit = new ToolStripMenuItem("Esci");
        exit.Click += (_, _) => ExitThread();
        _menu.Items.Add(exit);
    }

    private void PlayBuiltInIndex(int index)
    {
        if (index < 0 || index >= ClipCatalog.BuiltIns.Length)
        {
            return;
        }

        var path = ClipLocator.FindBuiltIn(ClipCatalog.BuiltIns[index].BaseName);
        if (path is null)
        {
            _tray.ShowBalloonTip(
                5000,
                "Boris Bar",
                "Clip non trovato. Importa i file audio built-in (vedi README).",
                ToolTipIcon.Warning);
            return;
        }

        try
        {
            _player.Play(path);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(5000, "Boris Bar", "Riproduzione fallita: " + ex.Message, ToolTipIcon.Error);
        }
    }

    private void AddCustomSound()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Scegli un file audio",
            Filter = "Audio|*.mp3;*.mp4;*.m4a;*.wav;*.aiff;*.aif;*.caf;*.ogg|Tutti i file|*.*"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            AppPaths.EnsureDataDirs();
            var destination = Path.Combine(AppPaths.Custom, Path.GetFileName(dialog.FileName));
            File.Copy(dialog.FileName, destination, overwrite: true);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(5000, "Boris Bar", "Copia fallita: " + ex.Message, ToolTipIcon.Error);
        }
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private static void OpenDisclaimer()
    {
        var path = AppPaths.DisclaimerFile;
        if (!File.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
        _hotkeys.Dispose();
        _player.Dispose();
        _icon.Dispose();
        base.ExitThreadCore();
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}

internal static class IconLoader
{
    public static Icon LoadFish()
    {
        var ico = Path.Combine(AppContext.BaseDirectory, "assets", "fish.ico");
        if (File.Exists(ico))
        {
            return new Icon(ico, 32, 32);
        }

        var png = Path.Combine(AppContext.BaseDirectory, "assets", "fish.png");
        if (File.Exists(png))
        {
            using var bitmap = new Bitmap(png);
            IntPtr handle = bitmap.GetHicon();
            try
            {
                return (Icon)Icon.FromHandle(handle).Clone();
            }
            finally
            {
                DestroyIcon(handle);
            }
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
