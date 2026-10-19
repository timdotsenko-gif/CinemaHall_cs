using OpenTK.Mathematics;

namespace CinemaHall;

struct Light //описание одного источника света
{
    public Vector3 Pos; //позиция источника света в координатах мира внутри  окна
    public float Far; //дальняя карта для источников света
    public float Radius; //радиусм действия света
    public float K; //коэфицент затухания
}

class LightSystem //управление всеми источниками света
{
    public readonly List<Light> Lights = new();
    public int LampCount;

    public float[] Pos = Array.Empty<float>();
    public float[] Col = Array.Empty<float>();
    public float[] Far = Array.Empty<float>();
    public float[] Rad = Array.Empty<float>();
    public float[] K = Array.Empty<float>();

    readonly Vector3[] colors = new Vector3[Config.MaxLights];

    public int Count => Lights.Count;

    public void Build(ModelData model)
    {
        BuildLamps(model.LampPoints);
        BuildScreenLights(model);
        Pack();
    }

    void BuildLamps(List<Vector3> lampPoints) //создание источников света из ламп найденных в модели
    {
        if (lampPoints.Count == 0) return;

        var pts = lampPoints.OrderBy(p => p.X).ToList(); //вычисление центра
        var cluster = new List<Vector3>();
        float startX = pts[0].X;

        void Flush() //создание источника света
        {
            if (cluster.Count == 0) return;
            Vector3 sum = Vector3.Zero;
            foreach (var c in cluster) sum += c;
            Lights.Add(new Light
            {
                Pos = sum / cluster.Count + Config.LampOffset,
                Far = Config.LampShadowFar,
                Radius = Config.LampRadius,
                K = Config.LampFalloffK,
            });
            cluster.Clear();
        }

        foreach (var p in pts)
        {
            if (p.X - startX > 0.3f)
            {
                Flush();
                startX = p.X;
            }
            cluster.Add(p);
        }
        Flush();

        //Ограничение кол---ва ламп для света от экрана
        int maxLamps = Config.MaxLights - Config.ScreenLightCols * Config.ScreenLightRows;
        if (Lights.Count > maxLamps)
            Lights.RemoveRange(maxLamps, Lights.Count - maxLamps);

        LampCount = Lights.Count;
    }

    //Создания источнгика света от экрана
    void BuildScreenLights(ModelData model)
    {
        if (!model.HasScreen) return;
        int cols = Config.ScreenLightCols, rows = Config.ScreenLightRows;
        float w = model.ScreenMax.Z - model.ScreenMin.Z, h = model.ScreenMax.Y - model.ScreenMin.Y;
        float dref = Config.ScreenLightFalloffDist;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Lights.Add(new Light
                {
                    Pos = new Vector3(
                        model.ScreenMin.X - Config.ScreenLightInset,
                        model.ScreenMax.Y - (r + 0.5f) / rows * h,
                        model.ScreenMin.Z + (c + 0.5f) / cols * w),
                    Far = Config.ScreenLightShadowFar,
                    Radius = Config.ScreenLightRadius,
                    K = 1f / (dref * dref),
                });
            }
        }
    }

    /*Упаковка источников света в полскость для шейдера*/
    void Pack()
    {
        int n = Lights.Count;
        Pos = new float[n * 3];
        Col = new float[n * 3];
        Far = new float[n];
        Rad = new float[n];
        K = new float[n];
        for (int i = 0; i < n; i++)
        {
            var l = Lights[i];
            Pos[i * 3 + 0] = l.Pos.X;
            Pos[i * 3 + 1] = l.Pos.Y;
            Pos[i * 3 + 2] = l.Pos.Z;
            Far[i] = l.Far;
            Rad[i] = l.Radius;
            K[i] = l.K;
            colors[i] = i < LampCount ? Config.LampColor : Vector3.Zero;
        }
        FillColors();
    }

    //Копирования цвета
    void FillColors()
    {
        for (int i = 0; i < Lights.Count; i++)
        {
            Col[i * 3 + 0] = colors[i].X;
            Col[i * 3 + 1] = colors[i].Y;
            Col[i * 3 + 2] = colors[i].Z;
        }
    }

    //Обновление света в зависимости от цвета материала
    public void SetScreenColors(byte[] d, int w, int h)
    {
        int cols = Config.ScreenLightCols, rows = Config.ScreenLightRows;
        if (Lights.Count < LampCount + cols * rows) return;

        int stepX = Math.Max(1, w / 256), stepY = Math.Max(1, h / 144);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int x0 = c * w / cols, x1 = (c + 1) * w / cols;
                int y0 = r * h / rows, y1 = (r + 1) * h / rows;
                double sr = 0, sg = 0, sb = 0;
                int n = 0;
                for (int y = y0; y < y1; y += stepY) //подсчет среднего света 
                {
                    for (int x = x0; x < x1; x += stepX)
                    {
                        int i = (y * w + x) * 4;
                        sr += d[i];
                        sg += d[i + 1];
                        sb += d[i + 2];
                        n++;
                    }
                }
                if (n == 0) n = 1;

                /*Запись среднего цвета*/
                colors[LampCount + r * cols + c] =
                    new Vector3((float)(sr / n), (float)(sg / n), (float)(sb / n)) / 255f;
            }
        }
        FillColors(); //обновление массива для шейдера
    }
}
