using OpenTK.Mathematics;

namespace CinemaHall; //пространтсов имен проекта

static class Config
{
    public const string ModelFile = "model/model.obj"; //путь к модели зала
    public const int TextureHalvings = 1; //уменьшение текстуры

    public const float EyeHeight = 2.5f; //рост игрока
    public const float WalkSpeed = 2.5f; //скорость игрока
    public const float FlySpeed = 6f; //скорость полета
    public const float PlayerRadius = 0.25f; //объем "тела" игрока
    public const float StepUp = 0.6f; //макс-высота на котрую может подняться игрок
    public const float WallMargin = 0.4f; //предотвращение выхода за модель
    public const float MouseSens = 0.05f; //чувсвтуительность мыши

    /*Координаты поялвения игрока*/
    public const float SpawnX = -9.8f;
    public const float SpawnZ = 0f;
    public const float SpawnYaw = 0f;

    /*Границы тела игрока*/
    public const float CellSize = 0.1f;
    public const float BodyLow = 0.25f;
    public const float BodyHigh = 1.6f;

    /*Матеиралы*/
    public static readonly string[] WalkableMaterials = { "5c9b7a10", "24349e59", "b3118c89" }; //материалы по которыфм можно ходить
    public static readonly string[] BlockerMaterials = { "63012c75", "f86c7163", "dfbf8999" }; //материалы препятсивя
    public static readonly string[] WalkableNames = { "cinema_floor1", "cinema_stair1", "cinema_stair2" }; //проходимые меши
    public static readonly string[] BlockerNames = { "cinema_seats", "cinema_column" };//меши  препятсвия

    /*Коэфиценты света*/
    public const float BakedGain = 0.23f; //свет текстур
    public const float LampGain = 3.1f; //свет от ламп
    public const float ScreenGlow = 1.0f; //свет от экрана

    /*Лампы и тени*/
    public static readonly string[] LampMaterials = { "603b676f" }; //материалы-лампы
    public static readonly string[] LampNames = { "cinema_stairlight" }; //меши-лампы
    public const int MaxLights = 16; //макс кол-во источников света
    public const int ShadowSize = 512; //размер гграни кубичсеской карть теини в пикселях
    public const float ShadowNear = 0.05f; //плоскость отсечения для карты теней
    public const float LampShadowFar = 8f; //дальняя плоскость для теней от ламп
    public const float LampRadius = 5.5f; //радиус действия лампы
    public const float LampFalloffK = 1.5f; //кофицент затухания лампы
    public static readonly Vector3 LampOffset = new(0.12f, -0.08f, 0f); // смещение позиции лампы относительно центра меша (чтобы свет не торчал из стены/потолка)
    public static readonly Vector3 LampColor = new(1.0f, 0.82f, 0.6f); //свет от лампы

    /*Экран*/
    public static readonly string[] ScreenMaterials = { "0f754cde" }; //текстура материала экрана
    public static readonly string[] ScreenNames = { "cinema_screen_0" }; //имена мешей экрана
    public const float ScreenBrightness = 1f; //яркость экрана
    public const bool ScreenKeepAspect = false; //пропорции изображения на экране

    public const int ScreenLightCols = 3; //динамическое освещение от экрана - кол-во источников
    public const int ScreenLightRows = 2; //такжен кол-во источников но уже по высоте
    public const float ScreenLightInset = 0.5f; //глубина в которой находяться источники счвета от экрана
    public const float ScreenLightRadius = 30f; //радиус действия света от экрана
    public const float ScreenLightShadowFar = 32f; //дальняя плоскость для теней от источников света экрана
    public const float ScreenLightFalloffDist = 6f; // расстояние, на котором затухание света экрана становится заметным

    /*Анимация экрана*/
    public const string ScreenFramesFolder = "screen_frames"; //папка с кадрами внутри model (кадры идут по порядку имён)
    public const float ScreenFrameRate = 24f; //частота кадров для папки с кадрами (у gif своя задержка)
    public const int ScreenAnimMaxSize = 1280; //макс. размер кадра в пикселях, большие уменьшаются вдвое
    public const int ScreenAnimMemoryMB = 400; //лимит памяти под кадры анимации

    /*Анимация ламп*/
    public const float LampPulseAmount = 0.3f; //глубина мерцания ламп (0 - выключено)
    public const float LampPulseSpeed = 2f; //скорость мерцания
    public const float LampPulseWave = 0.6f; //сдвиг фазы между соседними лампами (бегущая волна)

    public static bool HideConsole = true; //скрытие консоли
}