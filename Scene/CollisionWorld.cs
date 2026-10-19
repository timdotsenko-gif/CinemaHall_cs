using OpenTK.Mathematics;

namespace CinemaHall;

class CollisionWorld
{
    readonly Tri[] walk; //массив треугольников по которым м ожно хордить
    readonly bool[] blocked = Array.Empty<bool>(); //сетка заблокированых ячеек
    readonly int gridNx, gridNz; //размеры сетки колизий
    readonly float gridOx, gridOz; //смещение начала сетки
    readonly Vector2 hallMin, hallMax; //прямоугльники внутри которых можно ходить
    readonly Vector2[] ring = new Vector2[8]; //проврека, что игрок не задевает колизии своим телом

    public bool HasFloor => walk.Length > 0; //свйоства для показа наличия пола

    /*Констурктор, принимающий список колизий и свободного пространнства*/
    public CollisionWorld(List<Tri> walkTris, List<Box> blockers)
    {
        walk = walkTris.ToArray(); //сохранения треугольников пола в массив

        /*Заполнения кольца из 8 точек и масштабировангие их на объем тела игрока*/
        for (int i = 0; i < ring.Length; i++)
        {
            float a = i * MathF.PI * 2f / ring.Length;
            ring[i] = new Vector2(MathF.Cos(a), MathF.Sin(a)) * Config.PlayerRadius;
        }

        //если пола нет - строить сетку не нужно
        if (walk.Length == 0) return;

        /*Границы треугольников пола*/
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var t in walk)
        {
            foreach (var v in new[] { t.A, t.B, t.C })
            {
                minX = MathF.Min(minX, v.X); maxX = MathF.Max(maxX, v.X);
                minZ = MathF.Min(minZ, v.Z); maxZ = MathF.Max(maxZ, v.Z);
            }
        }

        /*Область по которой можно ходить (не включаая модель персонажа)*/
        hallMin = new Vector2(minX + Config.WallMargin, minZ + Config.WallMargin);
        hallMax = new Vector2(maxX - Config.WallMargin, maxZ - Config.WallMargin);

        float cs = Config.CellSize; //параметры сетки

        //смещаем немонго назад, что ася модель вммещалась
        gridOx = minX - 0.5f;
        gridOz = minZ - 0.5f;
        //вычисление кол-ва ячеек
        gridNx = (int)MathF.Ceiling((maxX - minX + 1f) / cs);
        gridNz = (int)MathF.Ceiling((maxZ - minZ + 1f) / cs);
        blocked = new bool[gridNx * gridNz]; //массив заблокирвоаных ячеек

        var groundCache = new float[gridNx * gridNz]; //кэш высоты пола для каждой ячейки
        Array.Fill(groundCache, float.NaN);

        /*Обработка всех препятсивй*/
        foreach (var b in blockers)
        {
            int ix0 = Math.Max(0, (int)MathF.Floor((b.X0 - gridOx) / cs));
            int ix1 = Math.Min(gridNx - 1, (int)MathF.Floor((b.X1 - gridOx) / cs));
            int iz0 = Math.Max(0, (int)MathF.Floor((b.Z0 - gridOz) / cs));
            int iz1 = Math.Min(gridNz - 1, (int)MathF.Floor((b.Z1 - gridOz) / cs));
            for (int ix = ix0; ix <= ix1; ix++)
            {
                for (int iz = iz0; iz <= iz1; iz++)
                {
                    int id = ix * gridNz + iz;
                    if (blocked[id]) continue;
                    float g = groundCache[id];
                    if (float.IsNaN(g))
                    {
                        float cx = gridOx + (ix + 0.5f) * cs;
                        float cz = gridOz + (iz + 0.5f) * cs;
                        g = GroundAt(cx, cz, float.PositiveInfinity, out float gy) ? gy : float.NegativeInfinity;
                        groundCache[id] = g;
                    }
                    if (float.IsNegativeInfinity(g)) { blocked[id] = true; continue; }
                    if (b.Y1 > g + Config.BodyLow && b.Y0 < g + Config.BodyHigh)
                        blocked[id] = true;
                }
            }
        }
    }

    //высота пола под точкой
    public bool GroundAt(float x, float z, float maxY, out float best)
    {
        best = float.NegativeInfinity;
        foreach (var t in walk)
        {
            float det = (t.B.Z - t.C.Z) * (t.A.X - t.C.X) + (t.C.X - t.B.X) * (t.A.Z - t.C.Z);
            if (MathF.Abs(det) < 1e-8f) continue;
            float l0 = ((t.B.Z - t.C.Z) * (x - t.C.X) + (t.C.X - t.B.X) * (z - t.C.Z)) / det;
            float l1 = ((t.C.Z - t.A.Z) * (x - t.C.X) + (t.A.X - t.C.X) * (z - t.C.Z)) / det;
            float l2 = 1f - l0 - l1;
            const float e = -1e-4f;
            if (l0 < e || l1 < e || l2 < e) continue;
            float y = l0 * t.A.Y + l1 * t.B.Y + l2 * t.C.Y;
            if (y <= maxY && y > best) best = y;
        }
        return !float.IsNegativeInfinity(best);
    }

    bool CellBlocked(float x, float z) //проверка блокировки ячейки
    {
        int ix = (int)MathF.Floor((x - gridOx) / Config.CellSize);
        int iz = (int)MathF.Floor((z - gridOz) / Config.CellSize);
        if (ix < 0 || iz < 0 || ix >= gridNx || iz >= gridNz) return true;
        return blocked[ix * gridNz + iz];
    }

    public bool CanStand(float x, float z, float maxY, out float ground) //главный метод - проверка может ли игрок стоять на точке
    {
        ground = 0f;
        if (x < hallMin.X || x > hallMax.X || z < hallMin.Y || z > hallMax.Y) return false;
        if (CellBlocked(x, z)) return false;
        foreach (var o in ring)
            if (CellBlocked(x + o.X, z + o.Y)) return false;
        return GroundAt(x, z, maxY, out ground);
    }
}
