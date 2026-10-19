using OpenTK.Mathematics;

namespace CinemaHall;

readonly struct Tri //хранения структуры треугольников пола
{
    public readonly Vector3 A, B, C; //три вершины треугольника
    public Tri(Vector3 a, Vector3 b, Vector3 c) { A = a; B = b; C = c; } //контсукттор сохранения трех точек
}

readonly struct Box //стурктура для хранения препятсивий
{
    public readonly float X0, X1, Z0, Z1, Y0, Y1; //границы по трем осям
    public Box(float x0, float x1, float z0, float z1, float y0, float y1) //конструктор принимающий все границы
    {
        X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; Y0 = y0; Y1 = y1;
    }
}

class Part //описание одной части модели
{
    /*Индефикатор OpenGL объектов*/
    public int Vao, Vbo, Ebo; //Vao - буфер и атрибуты, Vbo - хранение вершин, Ebo - хранение индексов треугольник

    public int IndexCount; // кол-во индексов треугольников

    public int Texture; //индефикатор текстуры, которуб нужно использовать в данной части

    public bool IsScreen; //проврека, является ли данная часть экраном
}

class ModelData //контейнер всех данных полученных при загрузке модели
{
    public readonly List<Part> Parts = new(); //список всех частей модели для рендера
    public readonly List<Tri> Walk = new(); //список треугольников по которым можно ходить
    public readonly List<Box> Blockers = new(); //список препятсивй
    public readonly List<Vector3> LampPoints = new(); //списки точек где находятся лампы
    public int TextureCount; //кол-во текстур загруженных из модели
    public bool HasScreen; //флаг есть ли в модели экран
    public Vector3 ScreenMin, ScreenMax; //мин и макс координаты экрана
}
