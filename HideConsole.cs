using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CinemaHall;

static class HideConsole
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 1;

    //скрытие консоли
    public static void Hide()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_HIDE);
        }
    }

    //показ конеосли
    public static void Show()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero )
        {
            ShowWindow(handle, SW_SHOW);
        }
    }

    public static void ApplyHideConsole() //примененние настроек из Config
    {
        if (Config.HideConsole)
        {
            Hide();
        }
        else
        {
            Show();
        }    
    }
}