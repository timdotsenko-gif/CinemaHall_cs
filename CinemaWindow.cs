using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace CinemaHall;

class CinemaWindow : GameWindow //наследование от GameWindow
{
    SceneRenderer renderer; //поле генерации сцены
    ShadowMap shadows; //система теней
    ModelData model; //загружаемая модель
    CollisionWorld world; //колизии
    LightSystem lights; //система источников цвета
    ScreenDisplay screen; //дисплей экроана зала
    Player player; // управление и камера игрока
    float time; //время работы приложения (для анимации)

    public CinemaWindow(GameWindowSettings g, NativeWindowSettings n) : base(g, n) { } //контсруктор настройки предает в базоый цикл

    protected override void OnLoad() //метод инцилизаций
    {
        base.OnLoad(); //вызов базовой реализации
        GL.ClearColor(0.02f, 0.02f, 0.03f, 1f); //метод очистки буфера
        GL.Enable(EnableCap.DepthTest); //включение тесата глубины
        CursorState = CursorState.Grabbed; //метод для скрытия курсора мыши

        renderer = new SceneRenderer(); //создания рендера сцены
        shadows = new ShadowMap(); //создания системы карт теней

        int fallbackTex = Textures.Create(new byte[] { 40, 40, 40, 255 }, 1, 1);  //создания запасной текстуры
        model = ModelLoader.Load(Config.ModelFile, fallbackTex); // загрузка модели из файла, вместе с запасной
        world = new CollisionWorld(model.Walk, model.Blockers); //создание колизий

        lights = new LightSystem(); //создания системы освещения
        lights.Build(model); //источники светана ооснове ламп и границ экрана
        shadows.Bake(lights, model.Parts, FramebufferSize); //карты тейней для всех источников света

        screen = new ScreenDisplay(model, lights); //создания дисплея экрана
        screen.LoadInitial(); //загрузка начального изображения на экран

        player = new Player(world); //создания игрока им передача ему мира колизий
        player.Spawn(); //размещение игрока в начальной позиции
    }

    /*Обработка движений мыши*/
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (!IsFocused) return;
        player.Look(e.Delta);
    }

    /*Обработка нажатия клавиш*/
    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.IsRepeat) return;
        if (e.Key == Keys.Escape) Close(); //закрытие окна на Esc
        if (e.Key == Keys.LeftAlt) player.ToggleFly(); //включение ноуклипа
    }

    /*Обновление логики*/
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args); //вызов базовой реализации
        if (!IsFocused) return; //если окно не в фокусе - игнор

        float dt = MathF.Min((float)args.Time, 0.05f); //время прешедшее с прошлого кадра
        player.Update(KeyboardState, dt); //обновление состояние игрока
    }

    protected override void OnRenderFrame(FrameEventArgs args) //вызов каждого кадра перед отрисовкой
    {
        base.OnRenderFrame(args); //вызов базовой реализации

        float dt = MathF.Min((float)args.Time, 0.25f); //время кадра
        time += dt;
        lights.AnimateLamps(time); //мерцание ламп
        screen.Update(dt); //смена кадров на экране

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); //очистка буфера цвета и глубины

        /*перспектива окна*/
        var proj = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(70f), Size.X / (float)Size.Y, 0.05f, 200f);
        var mvp = player.View * proj; //итоговая матрица

        renderer.Draw(mvp, player.Eye, Config.ScreenGlow, model.Parts, lights, shadows, screen); //отрисовка всей сцены
        SwapBuffers(); //показ нарисованого кадра
    }

    /*Одновление размера окна*/
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height); //обновление области вывода OpenGL под новый размер окна
    }
}