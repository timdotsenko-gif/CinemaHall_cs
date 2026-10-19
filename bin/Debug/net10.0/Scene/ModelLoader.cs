using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using AI = Assimp;

namespace CinemaHall;

class ModelLoader //загрузка модели
{
    const int Stride = 8; //размер одной врешины в массиве float

    readonly ModelData data = new(); // объект, в который будет собираться модель
    readonly Dictionary<string, int> texCache = new(); //кеш уже загруженных текстур
    readonly int fallbackTex; //запасная текстура
    string dir; //папка, в которой лежит .obj-фалй

    ModelLoader(int fallbackTex) //приватный констурктор
    {
        this.fallbackTex = fallbackTex;
    }

    /*точка входа загурзки модели*/
    public static ModelData Load(string rel, int fallbackTex)
    {
        var loader = new ModelLoader(fallbackTex);
        loader.Run(rel);
        return loader.data;
    }

    /*Основнй метод загрузки*/
    void Run(string rel)
    {
        string full = Path.Combine(AppContext.BaseDirectory, rel); //строим путь относительно рабочей директории
        if (!File.Exists(full)) //обработка иисключения
            throw new FileNotFoundException("Модель не найдена: " + full);
        dir = Path.GetDirectoryName(full); //запоминание папки где лежит модель

        Console.WriteLine("Загрузка модели..."); //вывод сообщения
        using var ctx = new AI.AssimpContext(); 
        //моздания контекста Assimp
        //указание пост процессов
        //все полигоны конвертируется в треугольники
        //объеденение одинаковых вершинн
        //сгенерирование 
        var scene = ctx.ImportFile(full,
            AI.PostProcessSteps.Triangulate |
            AI.PostProcessSteps.JoinIdenticalVertices |
            AI.PostProcessSteps.GenerateNormals |
            AI.PostProcessSteps.FlipUVs);

        VisitNode(scene, scene.RootNode, AI.Matrix4x4.Identity); //рекурсивнывй обход всех узлов сцен
        data.TextureCount = texCache.Count; //запись текстур в их список по кол-ву
        Console.WriteLine($"Загружено объектов: {data.Parts.Count}, текстур: {texCache.Count}"); //вывод сообщение о загрузке
    }

    /*Рекурсивный обход сцен*/
    void VisitNode(AI.Scene scene, AI.Node node, AI.Matrix4x4 parent)
    {
        var m = parent * node.Transform; //вычисление матрицы трансформации узла
        foreach (int mi in node.MeshIndices) 
            AddMesh(scene, scene.Meshes[mi], m); //обработка всех мешей привязанных к узлу
        foreach (var c in node.Children)
            VisitNode(scene, c, m); //рекурсивный обход дочрених узлов
    }

    static float Min3(float a, float b, float c) => MathF.Min(a, MathF.Min(b, c)); //функция для поитска минимума трех чисел
    static float Max3(float a, float b, float c) => MathF.Max(a, MathF.Max(b, c)); //функция для поиска максиммума трех чисел

    /*проверка, начинается ли строка с префикса*/
    static bool StartsWithAny(string s, string[] prefixes)
    {
        foreach (var p in prefixes)
            if (s.StartsWith(p, StringComparison.Ordinal)) return true;
        return false;
    }

    /*Обработка отдельного меша*/
    void AddMesh(AI.Scene scene, AI.Mesh mesh, AI.Matrix4x4 m)
    {
        /*Проверка есть ли у меша UV-координаты и нормали*/
        bool hasUv = mesh.HasTextureCoords(0); 
        bool hasNrm = mesh.HasNormals;

        var verts = new float[mesh.VertexCount * Stride]; //создания формата для шейдера
        
        /*Заполнение массива вершин*/
        for (int i = 0; i < mesh.VertexCount; i++)
        {
            var p = mesh.Vertices[i];
            int o = i * Stride;
            verts[o + 0] = m.A1 * p.X + m.A2 * p.Y + m.A3 * p.Z + m.A4;
            verts[o + 1] = m.B1 * p.X + m.B2 * p.Y + m.B3 * p.Z + m.B4;
            verts[o + 2] = m.C1 * p.X + m.C2 * p.Y + m.C3 * p.Z + m.C4;
            if (hasUv) //если есть UV координаты  - копируем иих
            {
                var t = mesh.TextureCoordinateChannels[0][i];
                verts[o + 3] = t.X;
                verts[o + 4] = t.Y;
            }
            /*Трансформация нормали, без учета смещений*/
            float vnx = 0f, vny = 1f, vnz = 0f;
            if (hasNrm)
            {
                var nn = mesh.Normals[i];
                vnx = m.A1 * nn.X + m.A2 * nn.Y + m.A3 * nn.Z;
                vny = m.B1 * nn.X + m.B2 * nn.Y + m.B3 * nn.Z;
                vnz = m.C1 * nn.X + m.C2 * nn.Y + m.C3 * nn.Z;
            }
            verts[o + 5] = vnx;
            verts[o + 6] = vny;
            verts[o + 7] = vnz;
        }

        var idx = mesh.GetUnsignedIndices(); //получние индекса треугольника

        /*Получение материала и имени*/
        var mat = scene.Materials[mesh.MaterialIndex];
        string matName = mat.Name ?? "";
        string meshName = mesh.Name ?? "";

        /*определение типа материала по имени или мешу*/
        bool isScreen = StartsWithAny(matName, Config.ScreenMaterials) || Config.ScreenNames.Contains(meshName);
        bool isWalk = !isScreen && (StartsWithAny(matName, Config.WalkableMaterials) || StartsWithAny(meshName, Config.WalkableNames));
        bool isBlock = !isScreen && (StartsWithAny(matName, Config.BlockerMaterials) || StartsWithAny(meshName, Config.BlockerNames));
        bool isLamp = !isScreen && (StartsWithAny(matName, Config.LampMaterials) || StartsWithAny(meshName, Config.LampNames));

        if (isScreen) MapScreenUv(verts, mesh.VertexCount); //правльное наложение на плоскость экрана

        if (isWalk || isBlock || isLamp) //если меш пол, препятсиве или лампа - создаем колизию
            CollectTriangles(verts, idx, isWalk, isBlock);

        var part = new Part { IndexCount = idx.Length, IsScreen = isScreen }; //созздание объекта Part
        part.Vao = GL.GenVertexArray();
        part.Vbo = GL.GenBuffer();
        part.Ebo = GL.GenBuffer();
        GL.BindVertexArray(part.Vao);

        /*Загрузка вершин VBO*/
        GL.BindBuffer(BufferTarget.ArrayBuffer, part.Vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);

        /*Загрузка индексов Ebo*/
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, part.Ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, idx.Length * sizeof(uint), idx, BufferUsageHint.StaticDraw);

        /*Настройка атрибутьов вершимн*/
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Stride * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Stride * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, Stride * sizeof(float), 5 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        //Загружаенм тектсуру если это не экран или есчть дифусная текстура
        part.Texture = (isScreen || !mat.HasTextureDiffuse) ? fallbackTex : LoadTexture(mat.TextureDiffuse.FilePath);
        
        data.Parts.Add(part); //добавляем обработанный элемент в список
    }

    void MapScreenUv(float[] verts, int vertexCount) //растяжение изображения для всей плоскости экрана
    {
        var mn = new Vector3(float.MaxValue);
        var mx = new Vector3(float.MinValue);
        for (int i = 0; i < vertexCount; i++)
        {
            var v = new Vector3(verts[i * Stride], verts[i * Stride + 1], verts[i * Stride + 2]);
            mn = Vector3.ComponentMin(mn, v);
            mx = Vector3.ComponentMax(mx, v);
        }
        float sw = MathF.Max(mx.Z - mn.Z, 1e-4f), sh = MathF.Max(mx.Y - mn.Y, 1e-4f);
        for (int i = 0; i < vertexCount; i++)
        {
            verts[i * Stride + 3] = (verts[i * Stride + 2] - mn.Z) / sw;
            verts[i * Stride + 4] = (mx.Y - verts[i * Stride + 1]) / sh;
        }
        data.ScreenMin = mn;
        data.ScreenMax = mx;
        data.HasScreen = true;
    }

    /*Сбор и групировка треугольников из меша*/
    void CollectTriangles(float[] verts, uint[] idx, bool isWalk, bool isBlock)
    {
        for (int t = 0; t + 2 < idx.Length; t += 3)
        {
            int a = (int)idx[t] * Stride, b = (int)idx[t + 1] * Stride, c = (int)idx[t + 2] * Stride;
            var A = new Vector3(verts[a], verts[a + 1], verts[a + 2]);
            var B = new Vector3(verts[b], verts[b + 1], verts[b + 2]);
            var C = new Vector3(verts[c], verts[c + 1], verts[c + 2]);
            if (isWalk) //проверка - пол ли это
            {
                float triNy = Vector3.Cross(B - A, C - A).Y; //берем нормаль, котороая смотри вверх
                if (triNy > 1e-6f) data.Walk.Add(new Tri(A, B, C));
            }
            else if (isBlock) //в ином случае если это препятсиве
            {
                //создаем пространтсво из 4 плоскостей для треугольника
                data.Blockers.Add(new Box(Min3(A.X, B.X, C.X), Max3(A.X, B.X, C.X),
                                          Min3(A.Z, B.Z, C.Z), Max3(A.Z, B.Z, C.Z),
                                          Min3(A.Y, B.Y, C.Y), Max3(A.Y, B.Y, C.Y)));
            }
            else //если лампа
            {
                data.LampPoints.Add((A + B + C) / 3f); //сохраняем центр треугольника
            }
        }
    }

    int LoadTexture(string file) //поиск и загрузка файла
    {
        file = file.Replace('\\', '/');
        if (texCache.TryGetValue(file, out int cached)) return cached; //если уже загружена, просто возвращаем id тексстуры

        string p = Path.Combine(dir, file); //если не загружен, ищем файл в папках проекта
        if (!File.Exists(p))
        {
            var found = Directory.GetFiles(dir, Path.GetFileName(file), SearchOption.AllDirectories);
            if (found.Length == 0) //вывод ошибки поиска файла
            {
                Console.WriteLine("НЕТ ТЕКСТУРЫ: " + file);
                texCache[file] = fallbackTex;
                return fallbackTex;
            }
            p = found[0];
        }

        Console.WriteLine($"Текстура {texCache.Count + 1}: {Path.GetFileName(p)}"); //вывод в консоль загрузки текстуры

        byte[] pixels = Textures.Decode(p, out int w, out int h); //декдиинг изображения
        for (int k = 0; k < Config.TextureHalvings && w >= 2 && h >= 2; k++) //изменение размера текстуры
        {
            pixels = Textures.Halve(pixels, w, h);
            w /= 2;
            h /= 2;
        }

        /*Создание OpenGL текстуры*/
        int tex = Textures.Create(pixels, w, h);
        texCache[file] = tex;
        return tex;
    }
}
