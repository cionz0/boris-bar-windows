using System.Runtime.InteropServices;

namespace BorisBar;

internal sealed class HotkeyManager : Form
{
    public const int ClipCount = 9;

    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private const uint Vk1 = 0x31;

    private readonly bool[] _registered = new bool[ClipCount];

    public event Action<int>? ClipRequested;

    public HotkeyManager()
    {
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Size = new Size(1, 1);
        Opacity = 0;
        Text = "BorisBarHotkeys";
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-32000, -32000);
    }

    protected override bool ShowWithoutActivation => true;

    protected override void SetVisibleCore(bool value)
    {
        if (!IsHandleCreated)
        {
            CreateHandle();
        }

        base.SetVisibleCore(false);
    }

    public IReadOnlyList<int> RegisterWinAltNumberDefaults()
    {
        _ = Handle;
        var failed = new List<int>();
        uint modifiers = ModAlt | ModWin | ModNoRepeat;

        for (int i = 0; i < ClipCount; i++)
        {
            int id = i + 1;
            uint vk = Vk1 + (uint)i;
            if (RegisterHotKey(Handle, id, modifiers, vk))
            {
                _registered[i] = true;
            }
            else
            {
                failed.Add(id);
            }
        }

        return failed;
    }

    public void PostToUi(Action action)
    {
        if (IsHandleCreated && InvokeRequired)
        {
            BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            int id = m.WParam.ToInt32();
            if (id is >= 1 and <= ClipCount)
            {
                ClipRequested?.Invoke(id - 1);
            }
        }

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            for (int i = 0; i < ClipCount; i++)
            {
                if (_registered[i] && IsHandleCreated)
                {
                    UnregisterHotKey(Handle, i + 1);
                    _registered[i] = false;
                }
            }
        }

        base.Dispose(disposing);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
