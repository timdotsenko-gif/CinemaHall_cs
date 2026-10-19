using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using CinemaHall;

HideConsole.ApplyHideConsole(); //скрытие консоли

var settings = new NativeWindowSettings
{
    ClientSize = new Vector2i(1600, 800), //установка размера окна
    Title = "Cinema Hall", // название окна
    APIVersion = new Version(4, 0), // версия OpenGL
    Profile = ContextProfile.Core, //Профиль OpenGL - Сore - современыый
};

using var win = new CinemaWindow(GameWindowSettings.Default, settings); //создание главного цикла окна
win.Run(); // запуск главного цикла
