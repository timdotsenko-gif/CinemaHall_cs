using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace CinemaHall;

class ScreenDisplay
{
    readonly ModelData model;  //ссылка на модель
    readonly LightSystem lights; //ссылка на систему освещения

    public int Texture; //индефикатор текстуры
    public Vector2 Scale = Vector2.One; //сохранение пропорции изображения

    /*Состояние анимации*/
    readonly List<byte[]> frames = new(); //кадры анимации (пусто, если на экране статичная картинка)
    readonly List<float> delays = new(); //сколько секунд показывается каждый кадр
    int frameW, frameH; //размер кадров
    int current; //номер текущего кадра
    float timer; //сколько времени показывается текущий кадр

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

        string modelDir = Path.Combine(AppContext.BaseDirectory, "model");

        //1) папка с кадрами model/screen_frames
        string framesDir = Path.Combine(modelDir, Config.ScreenFramesFolder);
        if (Directory.Exists(framesDir) && LoadFrames(framesDir)) return;

        //2) анимированный gif model/screen.gif
        string gif = Path.Combine(modelDir, "screen.gif");
        if (File.Exists(gif) && LoadFile(gif, false)) return;

        //3) обычная картинка
        if (File.Exists(SavedImageFile))
        {
            string saved = File.ReadAllText(SavedImageFile).Trim();
            if (File.Exists(saved) && LoadFile(saved, false)) return;
        }
        foreach (var name in new[] { "screen.png", "screen.jpg", "screen.jpeg", "screen.bmp" })
        {
            string p = Path.Combine(modelDir, name);
            if (File.Exists(p) && LoadFile(p, false)) return;
        }
    }

    /*Загрузка изображения из файла*/
    public bool LoadFile(string path, bool remember)
    {
        if (!model.HasScreen) return false;

        //gif может быть анимированным
        if (Path.GetExtension(path).Equals(".gif", StringComparison.OrdinalIgnoreCase) && LoadGif(path))
        {
            if (remember) File.WriteAllText(SavedImageFile, Path.GetFullPath(path));
            return true;
        }

        try
        {
            byte[] d = Textures.Decode(path, out int w, out int h);
            Prepare(ref d, ref w, ref h, 4096);

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

    /*Загрузка анимированного gif: все кадры и задержки между ними*/
    bool LoadGif(string path)
    {
        try
        {
            var fr = new List<byte[]>();
            var dl = new List<float>();
            int w = 0, h = 0;
            long bytes = 0;

            using var fs = File.OpenRead(path);
            foreach (var f in ImageResult.AnimatedGifFramesFromStream(fs, ColorComponents.RedGreenBlueAlpha))
            {
                byte[] d = (byte[])f.Data.Clone(); //копия: библиотека может переиспользовать один буфер для всех кадров
                int fw = f.Width, fh = f.Height;
                Prepare(ref d, ref fw, ref fh, Config.ScreenAnimMaxSize);

                if (fr.Count == 0) { w = fw; h = fh; }
                else if (fw != w || fh != h) continue;

                bytes += d.Length;
                if (bytes > Config.ScreenAnimMemoryMB * 1024L * 1024L) break; //лимит памяти

                fr.Add(d);
                float ms = f.DelayInMs; //задержка кадра в миллисекундах
                dl.Add(ms >= 20f ? ms / 1000f : 0.1f); //слишком маленькую задержку заменяем на 0.1 с
            }

            if (fr.Count == 0) return false;
            StartAnimation(fr, dl, w, h);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Не удалось загрузить gif \"" + path + "\": " + ex.Message);
            return false;
        }
    }

    /*Загрузка анимации из папки с кадрами (png, jpg, bmp), кадры идут по порядку имён файлов*/
    bool LoadFrames(string dir)
    {
        var files = Directory.GetFiles(dir)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0) return false;

        var fr = new List<byte[]>();
        var dl = new List<float>();
        int w = 0, h = 0;
        long bytes = 0;

        foreach (var file in files)
        {
            try
            {
                byte[] d = Textures.Decode(file, out int fw, out int fh);
                Prepare(ref d, ref fw, ref fh, Config.ScreenAnimMaxSize);

                if (fr.Count == 0) { w = fw; h = fh; }
                else if (fw != w || fh != h) continue; //кадры должны быть одного размера

                bytes += d.Length;
                if (bytes > Config.ScreenAnimMemoryMB * 1024L * 1024L)
                {
                    Console.WriteLine("Кадры анимации ограничены по памяти: загружено " + fr.Count);
                    break;
                }

                fr.Add(d);
                dl.Add(1f / Config.ScreenFrameRate);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось загрузить кадр \"" + file + "\": " + ex.Message);
            }
        }

        if (fr.Count == 0) return false;
        StartAnimation(fr, dl, w, h);
        return true;
    }

    /*Подготовка кадра: прозрачность заменяется чёрным фоном, большие кадры уменьшаются*/
    static void Prepare(ref byte[] d, ref int w, ref int h, int maxSize)
    {
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
        while ((w > maxSize || h > maxSize) && w >= 2 && h >= 2)
        {
            d = Textures.Halve(d, w, h);
            w /= 2;
            h /= 2;
        }
    }

    /*Запуск анимации: первый кадр показываем сразу, остальные меняются в Update*/
    void StartAnimation(List<byte[]> fr, List<float> dl, int w, int h)
    {
        SetImage(fr[0], w, h);
        Console.WriteLine($"Изображение загружено");
        if (fr.Count < 2) return; //один кадр - обычная картинка

        frames.AddRange(fr);
        delays.AddRange(dl);
        frameW = w;
        frameH = h;
        current = 0;
        timer = 0f;
    }

    /*Смена кадров по времени, вызывается каждый кадр*/
    public void Update(float dt)
    {
        if (frames.Count < 2) return;

        timer += dt;
        bool changed = false;
        while (timer >= delays[current])
        {
            timer -= delays[current];
            current = (current + 1) % frames.Count;
            changed = true;
        }
        if (!changed) return;

        Textures.Update(Texture, frames[current], frameW, frameH); //новый кадр в ту же текстуру
        lights.SetScreenColors(frames[current], frameW, frameH); //свет от экрана подстраивается под кадр
    }

    void SetImage(byte[] rgba, int w, int h) //устьановка изображения на экран
    {
        frames.Clear(); //статичная картинка - анимации нет
        delays.Clear();

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