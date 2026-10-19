using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CinemaHall;

class ScreenDisplay
{
    readonly ModelData model;  //ссылка на модель
    readonly LightSystem lights; //ссылка на систему освещения

    public int Texture; //индефикатор текстуры
    public Vector2 Scale = Vector2.One; //сохранение пропорции изображения

    //кэш файла изображения
    static string SavedImageFile => Path.Combine(AppContext.BaseDirectory, "screen_image.txt");

    //конструктолр для сохранения ссылки на модель и системы света
    public ScreenDisplay(ModelData model, LightSystem lights)
    {
        this.model = model;
        this.lights = lights;
    }

    /*Загрузка изображение при старте париложения*/
    public void LoadInitial()
    {
        if (!model.HasScreen) return;

        if (File.Exists(SavedImageFile))
        {
            string saved = File.ReadAllText(SavedImageFile).Trim();
            if (File.Exists(saved) && LoadFile(saved, false)) return;
        }
        foreach (var name in new[] { "screen.png", "screen.jpg", "screen.jpeg", "screen.bmp" })
        {
            string p = Path.Combine(AppContext.BaseDirectory, "model", name);
            if (File.Exists(p) && LoadFile(p, false)) return;
        }
    }

    /*Загрузка изображения из файла*/
    public bool LoadFile(string path, bool remember)
    {
        if (!model.HasScreen) return false;
        try
        {
            byte[] d = Textures.Decode(path, out int w, out int h);

            for (int i = 0; i < d.Length; i += 4)
            {
                int a = d[i + 3];
                if (a != 255)
                {
                    d[i] = (byte)(d[i] * a / 255);
                    d[i + 1] = (byte)(d[i + 1] * a / 255);
                    d[i + 2] = (byte)(d[i + 2] * a / 255);
                    d[i + 3] = 255;
                }
            }
            while ((w > 4096 || h > 4096) && w >= 2 && h >= 2)
            {
                d = Textures.Halve(d, w, h);
                w /= 2;
                h /= 2;
            }

            SetImage(d, w, h);
            if (remember) File.WriteAllText(SavedImageFile, Path.GetFullPath(path));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Не удалось загрузить картинку \"" + path + "\": " + ex.Message);
            return false;
        }
    }

    void SetImage(byte[] rgba, int w, int h) //устьановка изображения на экран
    {
        if (Texture != 0) GL.DeleteTexture(Texture);
        Texture = Textures.Create(rgba, w, h);

        Scale = Vector2.One;
        if (Config.ScreenKeepAspect)
        {
            float imgAspect = w / (float)h;
            float scrAspect = (model.ScreenMax.Z - model.ScreenMin.Z) / (model.ScreenMax.Y - model.ScreenMin.Y);
            Scale = imgAspect > scrAspect
                ? new Vector2(1f, imgAspect / scrAspect)
                : new Vector2(scrAspect / imgAspect, 1f);
        }

        lights.SetScreenColors(rgba, w, h); //обновление цвет источника света
    }


}
