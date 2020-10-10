using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public static class Hook
{
    private static KeyboardHook hook;

    public static void Start(bool isAll = false)
    {
        hook = new KeyboardHook(isAll);
        hook.StartHook();
    }

    public static void Stop()
    {
        hook?.StopHook();
        hook?.Dispose();
    }
}

internal class BlockingQueue<T>
{
    readonly Queue<T> que = new Queue<T>();
    readonly Semaphore sem = new Semaphore(0, int.MaxValue);

    public T Dequeue()
    {
        sem.WaitOne();
        lock (que)
        {
            return que.Dequeue();
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

public abstract class GlobalHook<T>
{
    protected int hookHandle = 0;
    private readonly BlockingQueue<T> MessageQueue = new BlockingQueue<T>();
    private readonly Thread MessageThread = null;
    private readonly object lockObject = new object();
    private readonly Win32.HookProc HookCallBack = null;
    private bool IsValid = true;
    protected bool isAll = false;

    protected abstract int HookId { get; }

    public GlobalHook(bool isAll)
    {
        this.isAll = isAll;
        HookCallBack = new Win32.HookProc(CallBack);
        MessageThread = new Thread(MessageThreadStart) { Name = "GlobalHook", IsBackground = true };
        MessageThread.Start();
    }

    protected virtual int CallBack(int nCode, IntPtr wParam, Win32.KBDLLHOOKSTRUCT lParam)
    {
        var blnEat = false;

        switch ((int)wParam)
        {
            case 256: // WM_KEYDOWN
            case 257: // WM_KEYUP
            case 260: // WM_SYSKEYDOWN
            case 261: // WM_SYSKEYUP
                if (!isAll)
                {
                    blnEat =
                    ((lParam.vkCode == 0x09) && (lParam.flags == 0x20)) || // Alt+Tab
                    ((lParam.vkCode == 0x1B) && (lParam.flags == 0x20)) || // Alt+Esc
                    ((lParam.vkCode == 0x1B) && (lParam.flags == 0x00)) || // Ctrl+Esc
                    ((lParam.vkCode == 0x5B) && (lParam.flags == 0x01)) || // Left Windows Key
                    ((lParam.vkCode == 0x5C) && (lParam.flags == 0x01)) || // Right Windows Key
                    ((lParam.vkCode == 0x73) && (lParam.flags == 0x20)); // Alt+F4
                }
                else
                {
                    blnEat = true;
                }
                break;
        }
        if (blnEat)
        {
            return 1;
        }
        else
        {
            return Win32.CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }
    }

    protected abstract void OnMessageHooked(T input);

    protected abstract T ParseLowMessage(IntPtr wParam, IntPtr lParam);

    public bool StartHook()
    {
        lock (lockObject)
        {
            if (hookHandle != 0) { return false; }
            hookHandle = Win32.SetWindowsHookEx(HookId, HookCallBack, GetCurrentModuleHandle(), 0);
            return hookHandle != 0;
        }
    }

    private static IntPtr GetCurrentModuleHandle()
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return Win32.GetModuleHandle(curModule.ModuleName);
    }

    public void StopHook()
    {
        lock (lockObject)
        {
            if (hookHandle == 0) { return; }
            Win32.UnhookWindowsHookEx(hookHandle);
            hookHandle = 0;
        }
    }

    private void MessageThreadStart()
    {
        while (IsValid)
        {
            var msg = MessageQueue.Dequeue();
            OnMessageHooked(msg);
        }
    }

    public void Dispose()
    {
        IsValid = false;
        StopHook();
    }
}

public class KeyboardHook : GlobalHook<KeyboardHookEventArgs>
{
    public event EventHandler<KeyboardHookEventArgs> MessageHooked;

    public KeyboardHook(bool isAll) : base(isAll)
    {
        base.isAll = isAll;
    }

    protected override int HookId
    {
        get { return Win32.WH_KEYBOARD_LL; }
    }

    protected override void OnMessageHooked(KeyboardHookEventArgs input)
    {
        MessageHooked?.Invoke(null, input);
    }

    protected override KeyboardHookEventArgs ParseLowMessage(IntPtr wParam, IntPtr lParam)
    {
        return new KeyboardHookEventArgs(wParam, lParam);
    }
}

public class KeyboardHookEventArgs : EventArgs
{
    [Serializable]
    public enum Messages { WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105 };
    public Messages Message { get; set; }
    public Win32.KBDLLHOOKSTRUCT Data { get; set; }


    internal KeyboardHookEventArgs(IntPtr wParam, IntPtr lParam)
    {
        Win32.KBDLLHOOKSTRUCT kbHook = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
        Message = (Messages)wParam;
        Data = kbHook;
    }
}

public sealed class Win32
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    internal static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    internal static extern bool UnhookWindowsHookEx(int idHook);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    internal static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, KBDLLHOOKSTRUCT lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);

    internal delegate int HookProc(int nCode, IntPtr wParam, KBDLLHOOKSTRUCT lParam);

    [StructLayout(LayoutKind.Sequential), Serializable()]
    public class KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    internal const int WH_KEYBOARD_LL = 13;
}