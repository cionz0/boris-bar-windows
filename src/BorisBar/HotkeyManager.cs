using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BorisBar;

internal sealed class HotkeyManager : Form
{
    public const int ClipCount = 9;

    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyUp = 0x0105;
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int Vk1 = 0x31;
    private const int LlkhfUp = 0x80;

    private readonly LowLevelKeyboardProc _hookProc;
    private IntPtr _hook = IntPtr.Zero;
    private int? _heldDigit;

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
        _hookProc = HookCallback;
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

    public bool InstallWinAltNumberHook()
    {
        _ = Handle;
        if (_hook != IntPtr.Zero)
        {
            return true;
        }

        using var process = Process.GetCurrentProcess();
        var moduleName = process.MainModule?.ModuleName;
        IntPtr module = string.IsNullOrEmpty(moduleName)
            ? IntPtr.Zero
            : GetModuleHandle(moduleName);

        _hook = SetWindowsHookEx(WhKeyboardLl, _hookProc, module, 0);
        return _hook != IntPtr.Zero;
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

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            int vk = info.VkCode;
            bool isDigit = vk is >= Vk1 and <= (Vk1 + 8);
            bool keyDown = msg is WmKeyDown or WmSysKeyDown;
            bool keyUp = msg is WmKeyUp or WmSysKeyUp || (info.Flags & LlkhfUp) != 0;

            if (isDigit && keyUp)
            {
                if (_heldDigit == vk)
                {
                    _heldDigit = null;
                }
            }
            else if (isDigit && keyDown && IsWinAltOnly())
            {
                if (_heldDigit == vk)
                {
                    return (IntPtr)1;
                }

                _heldDigit = vk;
                int index = vk - Vk1;
                PostToUi(() => ClipRequested?.Invoke(index));
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private static bool IsWinAltOnly()
    {
        bool win = IsDown(VkLWin) || IsDown(VkRWin);
        bool alt = IsDown(VkMenu);
        bool ctrl = IsDown(VkControl);
        bool shift = IsDown(VkShift);
        return win && alt && !ctrl && !shift;
    }

    private static bool IsDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    protected override void Dispose(bool disposing)
    {
        if (disposing && _hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        base.Dispose(disposing);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int VkCode;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
