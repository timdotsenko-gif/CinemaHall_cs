using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CinemaHall;

class ShadowMap
{
    readonly int program; //шейдерная рпограмма для рендера глубины
    readonly int locVp; //лацкация uniform-матрицы
    public int Texture; //Индефекатор текстуры
    public bool Ok; //успешное сохранение теней

    public ShadowMap() //компиляций шейдероов глубины
    {
        program = Shaders.Build(Shaders.DepthVs, Shaders.DepthFs);
        locVp = GL.GetUniformLocation(program, "uVp");
    }

    public void Bake(LightSystem lights, List<Part> parts, Vector2i framebufferSize) //сохранение карты теней для всех источников света
    {
        int n = lights.Count;
        if (n == 0) return; //если источников света нет - ничего не делаем
        int size = Config.ShadowSize;

        /*Создание текстуры кубического массива*/
        Texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMapArray, Texture);

        //Выделениме памяти под все грани всех истчоников света
        GL.TexImage3D(TextureTarget.TextureCubeMapArray, 0, PixelInternalFormat.DepthComponent32f,
            size, size, 6 * n, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

        //Настройка параметров фильтрации и обертки
        GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

        /*Временный буфер для рендера в текстуры*/
        int fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        //Отключение цветовые буферы
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        //Активация шейдера глубины
        GL.UseProgram(program);
        GL.Viewport(0, 0, size, size);
        GL.Enable(EnableCap.DepthTest);

        //Напралвение взгляда для 6 граней куба
        Vector3[] dirs =
        {
            new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1)
        };
        Vector3[] ups =
        {
            new(0, -1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1), new(0, -1, 0), new(0, -1, 0)
        };

        bool ok = true;

        /*для каждого источника с вета рендерим 6 граней куба*/
        for (int i = 0; i < n && ok; i++)
        {
            var light = lights.Lights[i];
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1f, Config.ShadowNear, light.Far);
            for (int f = 0; f < 6; f++)
            {
                //привязываем нужный слой текстуры как цель глубины
                GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                    Texture, 0, i * 6 + f);
                //проверка коректности буфера
                if (i == 0 && f == 0)
                {
                    var st = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                    if (st != FramebufferErrorCode.FramebufferComplete)
                    {
                        ok = false;
                        break;
                    }
                }

                //очищаем буфер глубины
                GL.Clear(ClearBufferMask.DepthBufferBit);

                //постролйка матрицы
                var vp = Matrix4.LookAt(light.Pos, light.Pos + dirs[f], ups[f]) * proj;
                GL.UniformMatrix4(locVp, false, ref vp); //передача матрицы в шейдер
                
                /*Отрисовка всех частей модели*/
                foreach (var p in parts)
                {
                    GL.BindVertexArray(p.Vao);
                    GL.DrawElements(PrimitiveType.Triangles, p.IndexCount, DrawElementsType.UnsignedInt, 0);
                }
            }
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); //возвращение к оснвному буферу
        GL.DeleteFramebuffer(fbo); //удаление временого буфера
        GL.Viewport(0, 0, framebufferSize.X, framebufferSize.Y); //всотаановление viewport под размер окна
        Ok = ok; //сохранение результатов
    }
}
