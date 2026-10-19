using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace CinemaHall;

class Player
{
    readonly CollisionWorld world; //ссылка на мир колизий

    float px, pz, foot; //горизонтальные координаты ног игрока
    float yaw, pitch; //углы поворота камеры

    public Vector3 Eye; //позиция глаз камеры
    public bool Fly; //флаг ноулипа

    public Player(CollisionWorld world) //конструктьор мира колизий
    {
        this.world = world;
    }

    public Vector3 Front //вектор направвления взгляда
    {
        get
        {
            float y = MathHelper.DegreesToRadians(yaw), p = MathHelper.DegreesToRadians(pitch);
            return Vector3.Normalize(new Vector3(MathF.Cos(p) * MathF.Cos(y), MathF.Sin(p), MathF.Cos(p) * MathF.Sin(y)));
        }
    }

    /*Матрица вида*/
    public Matrix4 View => Matrix4.LookAt(Eye, Eye + Front, Vector3.UnitY);

    /*Обновление углов поворота на основе движения мыши*/
    public void Look(Vector2 delta)
    {
        yaw += delta.X * Config.MouseSens;
        pitch -= delta.Y * Config.MouseSens;
        pitch = MathHelper.Clamp(pitch, -89f, 89f);
    }

    public void Spawn() //стартовая позиция игрока
    {
        yaw = Config.SpawnYaw;
        pitch = 0f;
        px = Config.SpawnX;
        pz = Config.SpawnZ;
        if (world.HasFloor && world.GroundAt(px, pz, float.PositiveInfinity, out float g))
        {
            Fly = false;
            foot = g;
        }
        else
        {
            Fly = true;
            foot = -3f;
        }
        Eye = new Vector3(px, foot + Config.EyeHeight, pz);
    }

    public void ToggleFly() //режим ноуклипа
    {
        Fly = !Fly;
        if (Fly) return;
        if (world.HasFloor && world.CanStand(Eye.X, Eye.Z, Eye.Y, out float g))
        {
            px = Eye.X;
            pz = Eye.Z;
            foot = g;
        }
        else
        {
            Spawn(); //возращение на спавн - если при выключении ноулипа нельяза встать на ноги
        }
    }

    public void Update(KeyboardState k, float dt) //основной метод
    {
        Vector3 front = Front; //вектор взгляда
        Vector3 flat = new Vector3(front.X, 0f, front.Z); 
        flat.Normalize();
        Vector3 right = Vector3.Normalize(Vector3.Cross(flat, Vector3.UnitY));
        Vector3 fwd = Fly ? front : flat;

        /*движения при  нажатии на клваиатуру*/
        Vector3 dir = Vector3.Zero;
        if (k.IsKeyDown(Keys.W)) dir += fwd;
        if (k.IsKeyDown(Keys.S)) dir -= fwd;
        if (k.IsKeyDown(Keys.D)) dir += right;
        if (k.IsKeyDown(Keys.A)) dir -= right;

        if (Fly)
        {
            if (k.IsKeyDown(Keys.Space)) dir += Vector3.UnitY; //движение вверх в полете
            if (k.IsKeyDown(Keys.LeftControl)) dir -= Vector3.UnitY; //движение вниз в полете
            if (dir.LengthSquared > 0f) dir.Normalize();
            Eye += dir * Config.FlySpeed * dt;
        }
        else
        {
            if (dir.LengthSquared > 0f) dir.Normalize();
            MoveHoriz(new Vector2(dir.X, dir.Z) * Config.WalkSpeed * dt);

            if (world.GroundAt(px, pz, foot + Config.StepUp, out float g))
                foot = MoveToward(foot, g, (g > foot ? 5f : 10f) * dt);

            Eye = new Vector3(px, foot + Config.EyeHeight, pz);
        }
    }

    /*Двжиение игрока и с проверкой колизий*/
    void MoveHoriz(Vector2 d)
    {
        float len = d.Length;
        if (len < 1e-6f) return;
        int n = (int)MathF.Ceiling(len / 0.1f);
        Vector2 s = d / n;
        for (int i = 0; i < n; i++)
        {
            if (s.X != 0f && world.CanStand(px + s.X, pz, foot + Config.StepUp, out _)) px += s.X;
            if (s.Y != 0f && world.CanStand(px, pz + s.Y, foot + Config.StepUp, out _)) pz += s.Y;
        }
    }

    /*Плавный подъем или спуск*/
    static float MoveToward(float cur, float target, float maxDelta)
    {
        if (MathF.Abs(target - cur) <= maxDelta) return target;
        return cur + MathF.Sign(target - cur) * maxDelta;
    }
}
