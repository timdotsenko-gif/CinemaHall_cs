using OpenTK.Graphics.OpenGL4;

namespace CinemaHall;

static class Shaders
{
    /*Передача атрибутов дальше для отрисовки пространства*/
    public const string SceneVs = @"#version 400 core
layout(location=0) in vec3 aPos;
layout(location=1) in vec2 aUv;
layout(location=2) in vec3 aNrm;
uniform mat4 uMvp;
out vec2 vUv;
out vec3 vPos;
out vec3 vNrm;
void main(){
    vUv = aUv;
    vPos = aPos;
    vNrm = aNrm;
    gl_Position = uMvp * vec4(aPos, 1.0);
}";

    /*Шейдер, который рисует либо экран, либо обычную геометрию
     с освещением от ламп + света от экрана + сохраненого освещения + тенями*/
    public const string SceneFs = @"#version 400 core
in vec2 vUv;
in vec3 vPos;
in vec3 vNrm;
uniform sampler2D uTex;
uniform samplerCubeArray uShadow;
uniform int uEmissive;
uniform vec2 uScreenScale;
uniform float uScreenBright;
uniform float uBaked;
uniform float uLampGain;
uniform float uScreenGain;
uniform int uLightCount;
uniform int uLampCount;
uniform vec3 uLightPos[16];
uniform vec3 uLightCol[16];
uniform float uLightFar[16];
uniform float uLightRad[16];
uniform float uLightK[16];
uniform vec3 uEye;
uniform float uNear;
uniform int uShadowsOn;
out vec4 FragColor;

float shadowAt(int li, vec3 d, float nl)
{
    float farD = uLightFar[li];
    float m = max(abs(d.x), max(abs(d.y), abs(d.z)));
    float bias = (0.03 + 0.05 * (1.0 - nl)) * (1.0 + 0.1 * m);
    const vec3 o[4] = vec3[4](vec3(1.0, 1.0, 1.0), vec3(-1.0, -1.0, 1.0), vec3(-1.0, 1.0, -1.0), vec3(1.0, -1.0, -1.0));
    float r = 0.012 * m;
    float lit = 0.0;
    for (int i = 0; i < 4; i++)
    {
        float stored = textureLod(uShadow, vec4(d + o[i] * r, float(li)), 0.0).r;
        float ndc = stored * 2.0 - 1.0;
        float ms = 2.0 * farD * uNear / ((farD + uNear) - ndc * (farD - uNear));
        lit += (m - bias > ms) ? 0.0 : 1.0;
    }
    return lit * 0.25;
}

void main(){
    if (uEmissive != 0)
    {
        vec2 uv = (vUv - 0.5) * uScreenScale + 0.5;
        vec3 img = texture(uTex, clamp(uv, 0.0, 1.0)).rgb;
        bool inside = all(greaterThanEqual(uv, vec2(0.0))) && all(lessThanEqual(uv, vec2(1.0)));
        FragColor = vec4(inside ? img * uScreenBright : vec3(0.0), 1.0);
        return;
    }

    vec3 t = texture(uTex, vUv).rgb;
    vec3 bakedCol = pow(t, vec3(1.0 / 2.2));

    vec3 n = normalize(vNrm);
    vec3 v = normalize(uEye - vPos);
    if (dot(n, v) < 0.0) n = -n;

    float mx = max(t.r, max(t.g, t.b));
    vec3 albedo = mix(vec3(0.5), 0.5 * t / (mx + 0.05), 0.7);

    vec3 sumLamp = vec3(0.0);
    vec3 sumScreen = vec3(0.0);
    for (int i = 0; i < uLightCount; i++)
    {
        vec3 d = vPos - uLightPos[i];
        float dist = length(d);
        float rad = uLightRad[i];
        if (dist > rad) continue;
        vec3 l = -d / dist;
        float nl = dot(n, l);
        if (nl <= 0.0) continue;
        float w = 1.0 - pow(dist / rad, 4.0);
        w = w * w;
        float att = w / (1.0 + uLightK[i] * dist * dist);
        float sh = (uShadowsOn != 0) ? shadowAt(i, d, nl) : 1.0;
        vec3 c = uLightCol[i] * (att * nl * sh);
        if (i < uLampCount) sumLamp += c; else sumScreen += c;
    }

    vec3 col = bakedCol * uBaked + albedo * (sumLamp * uLampGain + sumScreen * uScreenGain);
    FragColor = vec4(col, 1.0);
}";

    //трансформация позиций
    public const string DepthVs = @"#version 400 core
layout(location=0) in vec3 aPos;
uniform mat4 uVp;
void main(){ gl_Position = uVp * vec4(aPos, 1.0); }";

    //глубина
    public const string DepthFs = @"#version 400 core
void main(){ }";

    //Метод сборки шейдерной программы
    public static int Build(string vs, string fs)
    {
        int Compile(ShaderType type, string src) //компиляция одного шейдера
        {
            foreach (char ch in src)
                if (ch > 127)
                    throw new Exception($"В шейдере {type} неккоректный символ."); //проврека символов
            int s = GL.CreateShader(type);
            GL.ShaderSource(s, src);
            GL.CompileShader(s);

            /*Проверка статуса компляции*/
            GL.GetShader(s, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0) throw new Exception(type + ": " + GL.GetShaderInfoLog(s));
            return s;
        }

        //Компиляция шейдеров
        int v = Compile(ShaderType.VertexShader, vs);
        int f = Compile(ShaderType.FragmentShader, fs);

        //Создание и линковка программы
        int p = GL.CreateProgram();
        GL.AttachShader(p, v);
        GL.AttachShader(p, f);
        GL.LinkProgram(p);

        //Проврека статуса линковки
        GL.GetProgram(p, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0) throw new Exception(GL.GetProgramInfoLog(p));

        //Удаление уже вшитых шейдеров
        GL.DeleteShader(v);
        GL.DeleteShader(f);


        return p;
    }
}
