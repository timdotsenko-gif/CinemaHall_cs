using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace CinemaHall;

static class Textures //утилиты ддля работы с текстурами
{
    /*Декодирование изображение из фала в формате RGBA*/
    public static byte[] Decode(string path, out int width, out int height)
    {
        using var fs = File.OpenRead(path); //открытие файла
        var img = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha); //декодировка изображдения

        /*Запись размеров*/
        width = img.Width;
        height = img.Height;

        return img.Data; //возвращаем данные декодирвоания
    }

    //Изменение размеров изображенияч
    public static byte[] Halve(byte[] s, int w, int h)
    {
        int nw = w / 2, nh = h / 2;
        var d = new byte[nw * nh * 4];
        for (int y = 0; y < nh; y++)
        {
            for (int x = 0; x < nw; x++)
            {
                int i00 = ((2 * y) * w + 2 * x) * 4;
                int i01 = i00 + 4;
                int i10 = i00 + w * 4;
                int i11 = i10 + 4;
                int o = (y * nw + x) * 4;
                for (int c = 0; c < 4; c++)
                    d[o + c] = (byte)((s[i00 + c] + s[i01 + c] + s[i10 + c] + s[i11 + c] + 2) >> 2);
            }
        }
        return d;
    }

    /*Замена содержимого существующей текстуры (для анимации, размер не меняется)*/
    public static void Update(int tex, byte[] data, int w, int h)
    {
        GL.BindTexture(TextureTarget.Texture2D, tex);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, w, h,
            PixelFormat.Rgba, PixelType.UnsignedByte, data);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    /*Создание OpenGL текстуры из массива RGBA и возращение её индефекеатора*/
    public static int Create(byte[] data, int w, int h)
    {
        int t = GL.GenTexture(); //генерация индефикатора текстуры
        GL.BindTexture(TextureTarget.Texture2D, t); //привязка как 2d текстуры

        /*Загрузка данных в видеопамять*/
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, data);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D); //генерация мип мапов

        /*Настрйока фильотрации*/
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        //Обертка
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        //Максимальный уровень сглаживапния
        GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)0x84FE, 8f);
        return t;
    }
}