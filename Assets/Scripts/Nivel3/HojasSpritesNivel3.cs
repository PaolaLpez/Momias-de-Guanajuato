using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Carga, recorta y procesa con transparencia las hojas de sprites del Nivel 3:
/// - Momias Gemelas (2 filas x 4 columnas: frente, 3/4 derecha, perfil derecha, espalda).
/// - Escorpiones (3 filas x 4 columnas con transparencia de fondo blanco).
/// Cuenta con generación procedural de respaldo garantizada para que los personajes
/// NUNCA queden invisibles bajo ninguna circunstancia.
/// </summary>
public static class HojasSpritesNivel3
{
    private const string Uploads = @"C:\Users\ADMIN\.gemini\antigravity\brain\872a3237-d38b-4734-9e83-abeca50664a8\.user_uploaded\";
    public static readonly string[] RutasMomias = { 
        Uploads + "media_1793846201701_4b88749a.jpg",
        Application.dataPath + "/Resources/Momia/media_1793846201701_4b88749a.jpg",
        Application.dataPath + "/Resources/Momia/media_1793805854090_18e915b9.jpg",
        Application.dataPath + "/Resources/Momia/momia_gemelas_sheet.jpg",
        Uploads + "media_1793805854090_18e915b9.jpg" 
    };
    public static readonly string[] RutasEscorpiones = { 
        Application.dataPath + "/Resources/Momia/media_1793806041880_b454b234.png",
        Application.dataPath + "/Resources/Momia/escorpiones_sheet.png",
        Uploads + "media_1793806041880_b454b234.png", 
        Uploads + "media_1793740666972_727799a3.png" 
    };

    private static List<Sprite> momias;
    private static List<Sprite> escorpiones;
    private static Sprite momiaAcostada1;
    private static Sprite momiaAcostada2;

    public static int OrdenY(float y) => 300 + Mathf.RoundToInt((20f - y) * 5f);

    public static List<Sprite> Momias
    {
        get
        {
            if (momias != null && momias.Count > 0 && momias[0] != null) return momias;
            momias = ProcesarMomias();
            return momias;
        }
    }

    public static List<Sprite> Escorpiones
    {
        get
        {
            if (escorpiones != null && escorpiones.Count > 0 && escorpiones[0] != null) return escorpiones;
            escorpiones = ProcesarEscorpiones();
            return escorpiones;
        }
    }

    /// <summary>dir: 0 frente, 1 derecha, 2 izquierda, 3 espalda. frame: 0 o 1.</summary>
    public static Sprite MomiaFrame(int dir, int frame)
    {
        List<Sprite> l = Momias;
        if (l == null || l.Count == 0) return null;
        int f = Mathf.Abs(frame) % 2;
        int d = Mathf.Clamp(dir, 0, 3);
        if (l.Count >= 8) return l[f * 4 + d];
        return l[Mathf.Min(d, l.Count - 1)];
    }

    public static Sprite MomiaAtaque()
    {
        List<Sprite> l = Momias;
        if (l == null || l.Count == 0) return null;
        return l.Count >= 8 ? l[4] : l[0];
    }

    public static Sprite EscorpionCaminar(int frame)
    {
        List<Sprite> l = Escorpiones;
        if (l == null || l.Count == 0) return null;
        int f = Mathf.Abs(frame) % 2;
        return (f == 0) ? l[0] : (l.Count > 1 ? l[1] : l[0]);
    }

    public static Sprite EscorpionAtaque()
    {
        List<Sprite> l = Escorpiones;
        if (l == null || l.Count == 0) return null;
        return (l.Count > 2) ? l[2] : l[0];
    }

    public static Sprite ObtenerMomiaAcostada(int variante)
    {
        // Retornar null para que GeneradorEscenarioNivel3 use siempre sus sprites de camilla
        // quirúrgica procedurales (spriteMomiaAcostada1 y spriteMomiaAcostada2), los cuales
        // representan fielmente un cadáver tendido bajo sábanas con proporciones exactas de camilla.
        return null;
    }

    public static void LimpiarCache()
    {
        momias = null;
        escorpiones = null;
    }

    private static readonly string[] RutasResourcesMomias = {
        "Momia/media_1793846201701_4b88749a",
        "Momia/media_1793805854090_18e915b9"
    };
    private static readonly string[] RutasResourcesEscorpiones = {
        "Momia/media_1793806041880_b454b234"
    };

    private static Texture2D CargarTexturaFuente(string[] resourcePaths, string[] diskPaths)
    {
        if (resourcePaths != null)
        {
            foreach (string resPath in resourcePaths)
            {
                Texture2D resTex = Resources.Load<Texture2D>(resPath);
                if (resTex != null && resTex.isReadable)
                {
                    return resTex;
                }
            }
        }

#if UNITY_EDITOR
        byte[] bytes = LeerBytes(diskPaths);
        if (bytes != null)
        {
            Texture2D src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (src.LoadImage(bytes))
            {
                return src;
            }
        }
#endif
        return null;
    }

#if UNITY_EDITOR
    private static byte[] LeerBytes(string[] rutas)
    {
        try
        {
            foreach (string r in rutas)
            {
                if (File.Exists(r))
                {
                    byte[] data = File.ReadAllBytes(r);
                    if (data != null && data.Length > 0)
                    {
                        return data;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[HojasSpritesNivel3] Error leyendo archivo: " + ex.Message);
        }
        return null;
    }
#endif

    private static List<Sprite> ProcesarMomias()
    {
        Texture2D src = CargarTexturaFuente(RutasResourcesMomias, RutasMomias);
        if (src != null)
        {
            {
                int W = src.width;
                int H = src.height;
                int cols = 4;
                int rows = 2;
                int cellW = W / cols;
                int cellH = H / rows;

                List<Sprite> sprites = new List<Sprite>();

                float ppu = Mathf.Max(32f, (float)cellH / 2.2f); // Altura perfecta de ~2.2m en espacio de juego

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int startX = c * cellW;
                        int startY = (rows - 1 - r) * cellH; // Unity Y va de abajo hacia arriba

                        Color[] celda = src.GetPixels(startX, startY, cellW, cellH);

                        // El fondo del sprite sheet es negro puro (#000000).
                        // Solo eliminamos los píxeles de fondo negro sin desvanecer sombras ni vendas de la momia.
                        for (int i = 0; i < celda.Length; i++)
                        {
                            Color p = celda[i];
                            float maxC = Mathf.Max(p.r, Mathf.Max(p.g, p.b));

                            if (maxC < 0.035f)
                            {
                                celda[i] = Color.clear;
                            }
                            else if (maxC < 0.075f)
                            {
                                float a = (maxC - 0.035f) / 0.040f;
                                celda[i] = new Color(p.r, p.g, p.b, a);
                            }
                            else
                            {
                                celda[i] = new Color(p.r, p.g, p.b, 1f);
                            }
                        }

                        Texture2D texCelda = new Texture2D(cellW, cellH, TextureFormat.RGBA32, false);
                        texCelda.filterMode = FilterMode.Bilinear;
                        texCelda.wrapMode = TextureWrapMode.Clamp;
                        texCelda.SetPixels(celda);
                        texCelda.Apply();

                        // Pivot en la base de los pies (0.5, 0.05) para contacto firme con el suelo
                        Sprite sp = Sprite.Create(texCelda, new Rect(0, 0, cellW, cellH), new Vector2(0.5f, 0.05f), ppu);
                        sp.name = "Momia_R" + r + "_C" + c;
                        sprites.Add(sp);
                    }
                }

                if (sprites.Count >= 8) return sprites;
            }
        }

        // Respaldo procedural de alta definición para que NUNCA sea nulo
        return GenerarMomiasProcedurales();
    }

    private static List<Sprite> ProcesarEscorpiones()
    {
        Texture2D src = CargarTexturaFuente(RutasResourcesEscorpiones, RutasEscorpiones);
        if (src != null)
        {
            {
                int W = src.width;
                int H = src.height;

                // Extraer estrictamente el espécimen cenital central (Row 1, Col 1)
                // Y inicia en 0.380f (muy por encima de Row 2 que termina en 0.320f)
                int rx = Mathf.Clamp((int)(W * 0.265f), 0, W - 1);
                int ry = Mathf.Clamp((int)(H * 0.380f), 0, H - 1);
                int rw = Mathf.Clamp((int)(W * 0.145f), 1, W - rx);
                int rh = Mathf.Clamp((int)(H * 0.235f), 1, H - ry);

                Color[] celda = src.GetPixels(rx, ry, rw, rh);

                // 1. Quitar fondo blanco y compresión
                bool[] esCuerpo = new bool[celda.Length];
                for (int i = 0; i < celda.Length; i++)
                {
                    Color p = celda[i];
                    float brillo = (p.r + p.g + p.b) / 3f;
                    if (brillo > 0.60f || (p.r > 0.58f && p.g > 0.58f && p.b > 0.58f))
                    {
                        celda[i] = Color.clear;
                        esCuerpo[i] = false;
                    }
                    else
                    {
                        esCuerpo[i] = true;
                    }
                }

                // 2. Anular borde perimetral de 2 píxeles para cortar cualquier residuo exterior
                for (int y = 0; y < rh; y++)
                {
                    for (int x = 0; x < rw; x++)
                    {
                        if (x < 2 || x >= rw - 2 || y < 2 || y >= rh - 2)
                        {
                            int idx = y * rw + x;
                            celda[idx] = Color.clear;
                            esCuerpo[idx] = false;
                        }
                    }
                }

                // 3. Filtro 4-conectado desde el centro del cuerpo (SIN DIAGONALES)
                int cx = rw / 2;
                int cy = rh / 2;
                if (!esCuerpo[cy * rw + cx])
                {
                    float mejorDist = 9999f;
                    int bestX = cx, bestY = cy;
                    for (int dy = -25; dy <= 25; dy++)
                    {
                        for (int dx = -25; dx <= 25; dx++)
                        {
                            int nx = cx + dx;
                            int ny = cy + dy;
                            if (nx >= 2 && nx < rw - 2 && ny >= 2 && ny < rh - 2 && esCuerpo[ny * rw + nx])
                            {
                                float d = dx * dx + dy * dy;
                                if (d < mejorDist)
                                {
                                    mejorDist = d;
                                    bestX = nx;
                                    bestY = ny;
                                }
                            }
                        }
                    }
                    cx = bestX;
                    cy = bestY;
                }

                bool[] visitado = new bool[celda.Length];
                Queue<int> cola = new Queue<int>();
                if (esCuerpo[cy * rw + cx])
                {
                    int startIdx = cy * rw + cx;
                    visitado[startIdx] = true;
                    cola.Enqueue(startIdx);

                    int[] dx4 = { 1, -1, 0, 0 };
                    int[] dy4 = { 0, 0, 1, -1 };

                    while (cola.Count > 0)
                    {
                        int curr = cola.Dequeue();
                        int px = curr % rw;
                        int py = curr / rw;

                        for (int k = 0; k < 4; k++)
                        {
                            int nx = px + dx4[k];
                            int ny = py + dy4[k];
                            if (nx >= 0 && nx < rw && ny >= 0 && ny < rh)
                            {
                                int nIdx = ny * rw + nx;
                                if (!visitado[nIdx] && esCuerpo[nIdx])
                                {
                                    visitado[nIdx] = true;
                                    cola.Enqueue(nIdx);
                                }
                            }
                        }
                    }
                }

                // Borrar absolutamente todo lo que no esté conectado al cuerpo central
                int minX = rw, maxX = 0, minY = rh, maxY = 0;
                bool hayPixeles = false;
                for (int y = 0; y < rh; y++)
                {
                    for (int x = 0; x < rw; x++)
                    {
                        int idx = y * rw + x;
                        if (!visitado[idx])
                        {
                            celda[idx] = Color.clear;
                        }
                        else
                        {
                            hayPixeles = true;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (hayPixeles && maxX > minX && maxY > minY)
                {
                    int cropW = maxX - minX + 1;
                    int cropH = maxY - minY + 1;
                    Color[] cropPix = new Color[cropW * cropH];
                    for (int y = 0; y < cropH; y++)
                    {
                        for (int x = 0; x < cropW; x++)
                        {
                            cropPix[y * cropW + x] = celda[(minY + y) * rw + (minX + x)];
                        }
                    }

                    Texture2D texCrop = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
                    texCrop.filterMode = FilterMode.Bilinear;
                    texCrop.wrapMode = TextureWrapMode.Clamp;
                    texCrop.SetPixels(cropPix);
                    texCrop.Apply();

                    Sprite spLimpio = Sprite.Create(texCrop, new Rect(0, 0, cropW, cropH), new Vector2(0.5f, 0.5f), 160f);
                    spLimpio.name = "Escorpion_TopDown_Limpio";

                    List<Sprite> sprites = new List<Sprite>();
                    for (int i = 0; i < 4; i++) sprites.Add(spLimpio);
                    return sprites;
                }
            }
        }

        return GenerarEscorpionesProcedurales();
    }

    private static List<Sprite> GenerarMomiasProcedurales()
    {
        List<Sprite> list = new List<Sprite>();
        int w = 64, h = 96;
        for (int i = 0; i < 8; i++)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int j = 0; j < cols.Length; j++) cols[j] = Color.clear;

            Color cVendas = new Color(0.78f, 0.72f, 0.60f, 1f);
            Color cOjos = new Color(0.95f, 0.2f, 0.2f, 1f);
            Vector2 centro = new Vector2(32, 48);

            for (int y = 8; y < 88; y++)
            {
                for (int x = 16; x < 48; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), centro);
                    if (dist < 32f)
                    {
                        bool pliegue = (y % 5 == 0);
                        cols[y * w + x] = pliegue ? new Color(0.45f, 0.40f, 0.32f, 1f) : cVendas;

                        // Ojos rojos brillantes en la cabeza
                        if (y >= 70 && y <= 74 && (x == 26 || x == 38))
                        {
                            cols[y * w + x] = cOjos;
                        }
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 32f);
            sp.name = "Momia_Proc_" + i;
            list.Add(sp);
        }
        return list;
    }

    private static List<Sprite> GenerarEscorpionesProcedurales()
    {
        List<Sprite> list = new List<Sprite>();
        int w = 48, h = 48;
        for (int i = 0; i < 8; i++)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int j = 0; j < cols.Length; j++) cols[j] = Color.clear;

            Color cCaparazon = new Color(0.18f, 0.08f, 0.08f, 1f);
            Color cOjo = Color.red;

            for (int y = 10; y < 38; y++)
            {
                for (int x = 12; x < 36; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(24, 24));
                    if (dist < 14f)
                    {
                        cols[y * w + x] = cCaparazon;
                        if (y == 28 && (x == 21 || x == 27)) cols[y * w + x] = cOjo;
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            sp.name = "Escorpion_Proc_" + i;
            list.Add(sp);
        }
        return list;
    }
}
