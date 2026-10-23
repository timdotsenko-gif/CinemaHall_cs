using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CinemaHall;

static class HideConsole
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    static extern bool AllocConsole(); //создание нового окна консоли

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 1;

    //скрытие консоли (если она уже есть)
    public static void Hide()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_HIDE);
        }
    }

    //показ консоли: если приложение собрано как WinExe и консоли нет - создаём её
    public static void Show()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_SHOW);
            return;
        }

        if (AllocConsole())
        {
            //подключаем вывод к новой консоли, чтобы Console.WriteLine и русский текст работали
            Console.OutputEncoding = Encoding.UTF8;
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
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