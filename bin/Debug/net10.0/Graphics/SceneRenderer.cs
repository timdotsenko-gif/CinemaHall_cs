using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CinemaHall;

class SceneRenderer
{
    readonly int program;
    readonly int locMvp, locTex, locShadow, locEmissive, locScreenScale, locScreenBright;
    readonly int locBaked, locLampGain, locScreenGain, locLightCount, locLampCount;
    readonly int locLightPos, locLightCol, locLightFar, locLightRad, locLightK;
    readonly int locEye, locNear, locShadowsOn;

    public SceneRenderer() //отрисовка всей сцены
    {
        program = Shaders.Build(Shaders.SceneVs, Shaders.SceneFs);

        /*Получение локаций*/

        locMvp = GL.GetUniformLocation(program, "uMvp"); //матрица
        locTex = GL.GetUniformLocation(program, "uTex"); // основная текстура
        locShadow = GL.GetUniformLocation(program, "uShadow"); //карта теней
        locEmissive = GL.GetUniformLocation(program, "uEmissive"); //экран
        locScreenScale = GL.GetUniformLocation(program, "uScreenScale"); //размер экрана - пропорции
        locScreenBright = GL.GetUniformLocation(program, "uScreenBright"); //яркость экрана
        locBaked = GL.GetUniformLocation(program, "uBaked"); //сохраноное освещение
        locLampGain = GL.GetUniformLocation(program, "uLampGain"); //свет от ламп
        locScreenGain = GL.GetUniformLocation(program, "uScreenGain"); //свет от внутриигрового экрана
        locLightCount = GL.GetUniformLocation(program, "uLightCount"); //общее  кол-во источников света
        locLampCount = GL.GetUniformLocation(program, "uLampCount"); //кол-во ламп
        locLightPos = GL.GetUniformLocation(program, "uLightPos"); //массив позиций источника света
        locLightCol = GL.GetUniformLocation(program, "uLightCol"); //массив цвета источников света
        locLightFar = GL.GetUniformLocation(program, "uLightFar"); //массив дальних плоскастей теней
        locLightRad = GL.GetUniformLocation(program, "uLightRad"); //массив радиусов источников света
        locLightK = GL.GetUniformLocation(program, "uLightK"); //массив коэфицента затуханния
        locEye = GL.GetUniformLocation(program, "uEye"); //позиция камеры
        locNear = GL.GetUniformLocation(program, "uNear"); //ближняя плоскость теней
        locShadowsOn = GL.GetUniformLocation(program, "uShadowsOn"); //включение теней
    }

    /*Вызов покадровой отрисовки*/
    public void Draw(Matrix4 mvp, Vector3 eye, float screenGlow,
        List<Part> parts, LightSystem lights, ShadowMap shadows, ScreenDisplay screen)
    {
        GL.UseProgram(program); //активация шейдерной программы
        GL.UniformMatrix4(locMvp, false, ref mvp); //передача матрицы MVP
        GL.Uniform3(locEye, eye); //передача позиции камеры
        GL.Uniform1(locTex, 0); //указание основной текстуры
        GL.Uniform1(locShadow, 1); //указание на карту теней
       
        /*Коефиценты освещения*/
        GL.Uniform1(locBaked, Config.BakedGain);
        GL.Uniform1(locLampGain, Config.LampGain);
        GL.Uniform1(locScreenGain, screenGlow);

        GL.Uniform1(locNear, Config.ShadowNear); //ближняя плосксть теней
        GL.Uniform1(locShadowsOn, shadows.Ok ? 1 : 0);//флагн проверки сохранения теней
        GL.Uniform1(locScreenBright, Config.ScreenBrightness); //передача яркости экрана
        GL.Uniform2(locScreenScale, screen.Scale); //передача размера экрана

        /*Передача кол-ва источников света*/
        int count = lights.Count;
        GL.Uniform1(locLightCount, count);
        GL.Uniform1(locLampCount, lights.LampCount);

        //если есть источники света - передаем их в массивы шейдеров
        if (count > 0)
        {
            GL.Uniform3(locLightPos, count, lights.Pos);
            GL.Uniform3(locLightCol, count, lights.Col);
            GL.Uniform1(locLightFar, count, lights.Far);
            GL.Uniform1(locLightRad, count, lights.Rad);
            GL.Uniform1(locLightK, count, lights.K);
        }

        //если есть текстуры - привязываем их к текстурам
        if (shadows.Texture != 0)
        {
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.TextureCubeMapArray, shadows.Texture);
        }
        GL.ActiveTexture(TextureUnit.Texture0);

        foreach (var p in parts) //отрисовка всех частей модели
        {
            GL.Uniform1(locEmissive, p.IsScreen ? 1 : 0);
            GL.BindTexture(TextureTarget.Texture2D, p.IsScreen && screen.Texture != 0 ? screen.Texture : p.Texture);
            GL.BindVertexArray(p.Vao);
            GL.DrawElements(PrimitiveType.Triangles, p.IndexCount, DrawElementsType.UnsignedInt, 0);
        }
    }
}
