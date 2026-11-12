#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generador de Arte y Texturas 2D Exclusivas del Nivel 4 (Panteón de Santa Paula).
/// Crea 19 sprites dedicados de alta calidad inspirados fielmente en las referencias:
/// - Verjas y portón monumental de hierro forjado gótico (filigrana y lanzas).
/// - Pilares de piedra con hiedra trepadora.
/// - Árboles góticos centenarios retorcidos y desnudos con ramas enredadas.
/// - Cipreses fúnebres esbeltos.
/// - Variedad realista de lápidas (cruz celta, obeliscos, arcos labrados, lápidas quebradas).
/// - Sarcófagos de piedra y fosas abiertas con tierra removida.
/// - Suelo natural de tierra húmeda de sepultura, ceniza y fango (sin baldosas).
/// - Calzada de adoquín de cantera envejecida y muros de gavetas de Guanajuato.
/// - Fachada de capilla monumental gótica, altar de cirios y faroles victorianos.
/// </summary>
public static class GeneradorAssetsCementerio
{
    private const string CARPETA_RESOURCES = "Assets/Resources/Cementerio";

    [MenuItem("Juego/Generar Assets del Cementerio", false, 10)]
    public static void GenerarTodosLosAssets()
    {
        if (!Directory.Exists(CARPETA_RESOURCES))
        {
            Directory.CreateDirectory(CARPETA_RESOURCES);
        }

        // 1. PISOS Y MUROS CONTINUOS (Wrap Repeat)
        GuardarTextura(CrearTexturaSueloTierra(), "suelo_tierra_panteon", true);
        GuardarTextura(CrearTexturaCaminoPiedra(), "camino_piedra_antigua", true);
        GuardarTextura(CrearTexturaMuroGavetas(), "muro_cantera_gavetas", true);

        // 2. ARQUITECTURA DE ENTRADA Y VERJAS
        GuardarTextura(CrearSpritePortonHierro(), "porton_hierro_ornado", false);
        GuardarTextura(CrearSpriteVerjaHierro(), "verja_hierro_gotica", false);
        GuardarTextura(CrearSpritePilarPiedra(), "pilar_piedra_hiedra", false);

        // 3. VEGETACIÓN GÓTICA NOCTURNA
        GuardarTextura(CrearSpriteArbolMuerto1(), "arbol_muerto_gotico_1", false);
        GuardarTextura(CrearSpriteArbolMuerto2(), "arbol_muerto_gotico_2", false);
        GuardarTextura(CrearSpriteCipresGotico(), "cipres_gotico", false);

        // 4. LÁPIDAS, SEPULTURAS Y FOSAS
        GuardarTextura(CrearSpriteLapidaArco(), "lapida_arco_1", false);
        GuardarTextura(CrearSpriteLapidaCruzCelta(), "lapida_cruz_celta", false);
        GuardarTextura(CrearSpriteLapidaObelisco(), "lapida_obelisco", false);
        GuardarTextura(CrearSpriteLapidaQuebrada(), "lapida_quebrada", false);
        GuardarTextura(CrearSpriteSarcofago(), "sarcofago_piedra_tumba", false);
        GuardarTextura(CrearSpriteFosaAbierta(), "fosa_abierta_tierra", false);

        // 5. EDIFICACIONES Y ELEMENTOS SACROS
        GuardarTextura(CrearSpriteFarolGas(), "farol_gas_gotico", false);
        GuardarTextura(CrearSpriteCapillaFachada(), "capilla_fachada_gotica", false);
        GuardarTextura(CrearSpriteAltarCirios(), "altar_gotico_cirios", false);
        GuardarTextura(CrearSpriteBrumaNiebla(), "capa_bruma_niebla", false);

        AssetDatabase.Refresh();
        Debug.Log("<color=green>[GeneradorAssetsCementerio] ¡19 assets 2D dedicados del Panteón generados e importados exitosamente!</color>");
    }

    private static void GuardarTextura(Texture2D tex, string nombre, bool esTileable)
    {
        string ruta = Path.Combine(CARPETA_RESOURCES, nombre + ".png");
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(ruta, bytes);

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.alphaIsTransparency = true;
            ti.wrapMode = esTileable ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.SaveAndReimport();
        }
    }

    // =========================================================================
    // 1. SUELO DE TIERRA DE SEPULTURA OSCURA (Seamless 256x256)
    // =========================================================================
    private static Texture2D CrearTexturaSueloTierra()
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                float n2 = Mathf.PerlinNoise((x + 120) * 0.12f, (y + 120) * 0.12f);
                float n3 = Mathf.PerlinNoise((x + 240) * 0.25f, (y + 240) * 0.25f);

                // Base tierra oscura húmeda de camposanto
                float baseL = 0.08f + n1 * 0.05f + n2 * 0.03f;
                Color c = new Color(baseL * 1.05f, baseL * 1.15f, baseL * 1.05f, 1f);

                // Manchas de humus / tierra negra profunda
                if (n2 > 0.65f)
                {
                    c = Color.Lerp(c, new Color(0.04f, 0.05f, 0.04f, 1f), (n2 - 0.65f) * 2f);
                }

                // Moho y musgo frío de panteón
                if (n1 > 0.60f && n3 > 0.50f)
                {
                    c = Color.Lerp(c, new Color(0.09f, 0.14f, 0.09f, 1f), 0.45f);
                }

                // Hojas secas descompuestas dispersas
                if (n3 > 0.82f)
                {
                    c = Color.Lerp(c, new Color(0.18f, 0.12f, 0.07f, 1f), 0.65f);
                }

                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 2. CALZADA DE ADOQUÍN DE CANTERA GRIS DESGASTADA (Seamless 256x256)
    // =========================================================================
    private static Texture2D CrearTexturaCaminoPiedra()
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        int tamBloque = 32;

        for (int y = 0; y < h; y++)
        {
            int fila = y / tamBloque;
            int localY = y % tamBloque;
            int offsetFila = (fila % 2 == 0) ? 0 : tamBloque / 2;

            for (int x = 0; x < w; x++)
            {
                int posX = (x + offsetFila) % w;
                int colId = posX / tamBloque;
                int localX = posX % tamBloque;

                // Juntas de mortero / lodo oscuro entre adoquines
                bool esJunta = (localX <= 2 || localX >= tamBloque - 2 || localY <= 2 || localY >= tamBloque - 2);

                if (esJunta)
                {
                    float ruidoJunta = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.04f;
                    cols[y * w + x] = new Color(0.05f + ruidoJunta, 0.06f + ruidoJunta, 0.06f + ruidoJunta, 1f);
                }
                else
                {
                    // Relieve del adoquín con bisel e irregularidad de cantera
                    float distCentroX = Mathf.Abs((localX - tamBloque / 2f) / (tamBloque / 2f));
                    float distCentroY = Mathf.Abs((localY - tamBloque / 2f) / (tamBloque / 2f));
                    float factorBisel = 1f - Mathf.Max(distCentroX, distCentroY) * 0.35f;

                    float ruidoPiedra = Mathf.PerlinNoise((x + colId * 17) * 0.15f, (y + fila * 23) * 0.15f) * 0.08f;
                    float brillo = (0.18f + ruidoPiedra) * factorBisel;

                    // Cantera gris azulada fría de Guanajuato nocturna
                    cols[y * w + x] = new Color(brillo * 0.95f, brillo * 1.05f, brillo * 1.15f, 1f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 3. MURO DE GAVETAS DE CANTERA / NICHOS APILADOS (Seamless 256x256)
    // =========================================================================
    private static Texture2D CrearTexturaMuroGavetas()
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        // 4 niveles de gavetas apiladas verticalmente
        int altoNivel = 64;
        int anchoNicho = 85;

        for (int y = 0; y < h; y++)
        {
            int nivel = y / altoNivel;
            int localY = y % altoNivel;

            for (int x = 0; x < w; x++)
            {
                int nichoId = x / anchoNicho;
                int localX = x % anchoNicho;

                // Marco de cantera de la gaveta
                bool esMarco = (localX <= 4 || localX >= anchoNicho - 4 || localY <= 4 || localY >= altoNivel - 4);

                if (esMarco)
                {
                    float n = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.06f;
                    cols[y * w + x] = new Color(0.24f + n, 0.22f + n, 0.20f + n, 1f); // Cantera rosa oscura
                }
                else
                {
                    // Placa sepulcral interior de mármol/cantera sellada
                    float centroX = anchoNicho / 2f;
                    float centroY = altoNivel / 2f;
                    float dX = Mathf.Abs(localX - centroX);
                    float dY = Mathf.Abs(localY - centroY);

                    // Cruz grabada en relieve en el centro de la gaveta
                    bool esCruz = (dX <= 2f && dY <= 14f) || (dY <= 2f && dX <= 8f && localY > centroY - 2f);

                    float n = Mathf.PerlinNoise((x + 70) * 0.08f, (y + 70) * 0.08f) * 0.05f;
                    if (esCruz)
                    {
                        cols[y * w + x] = new Color(0.10f + n, 0.10f + n, 0.11f + n, 1f);
                    }
                    else
                    {
                        // Placa mortuoria
                        cols[y * w + x] = new Color(0.18f + n, 0.18f + n, 0.19f + n, 1f);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 4. VERJA DE HIERRO FORJADO GÓTICA (128x256 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteVerjaHierro()
    {
        int w = 128, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color hierro = new Color(0.14f, 0.14f, 0.15f, 1f);
        Color hierroBrillo = new Color(0.30f, 0.32f, 0.35f, 1f);

        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        // Barrotes verticales con puntas de lanza góticas
        int numBarrotes = 6;
        int paso = w / numBarrotes;

        for (int b = 0; b < numBarrotes; b++)
        {
            int cx = b * paso + paso / 2;
            for (int y = 20; y < 220; y++)
            {
                for (int x = cx - 2; x <= cx + 2; x++)
                {
                    if (x >= 0 && x < w)
                    {
                        cols[y * w + x] = (x == cx - 2) ? hierroBrillo : hierro;
                    }
                }
            }

            // Punta de lanza gótica arriba (y: 220 a 248)
            for (int y = 220; y < 248; y++)
            {
                float progreso = (y - 220f) / 28f;
                int ancho = Mathf.RoundToInt((1f - progreso) * 6f);
                for (int x = cx - ancho; x <= cx + ancho; x++)
                {
                    if (x >= 0 && x < w)
                    {
                        cols[y * w + x] = (x <= cx) ? hierroBrillo : hierro;
                    }
                }
            }
        }

        // Rieles horizontales de hierro (superior, intermedio y base)
        int[] rielesY = new int[] { 35, 120, 210 };
        foreach (int ry in rielesY)
        {
            for (int y = ry - 4; y <= ry + 4; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    cols[y * w + x] = (y == ry + 4) ? hierroBrillo : hierro;
                }
            }
        }

        // Adornos de filigrana gótica circular entre barrotes
        for (int b = 0; b < numBarrotes - 1; b++)
        {
            int centroX = b * paso + paso;
            int centroY = 165;
            int radio = 10;

            for (int y = centroY - radio; y <= centroY + radio; y++)
            {
                for (int x = centroX - radio; x <= centroX + radio; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centroX, centroY));
                    if (dist >= radio - 2f && dist <= radio)
                    {
                        if (x >= 0 && x < w && y >= 0 && y < h)
                        {
                            cols[y * w + x] = hierro;
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 5. GRAN PORTÓN MONUMENTAL DE HIERRO ORNADO (256x256 Transparent)
    // =========================================================================
    private static Texture2D CrearSpritePortonHierro()
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color hierro = new Color(0.12f, 0.12f, 0.14f, 1f);
        Color oxido = new Color(0.35f, 0.20f, 0.14f, 1f);
        Color brillo = new Color(0.28f, 0.30f, 0.34f, 1f);

        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        // Gran arco gótico superior del portón
        Vector2 centroArco = new Vector2(128f, 160f);
        float radioArco = 110f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centroArco);

                // Marco exterior de arco
                if (y >= 160 && dist <= radioArco + 6f && dist >= radioArco - 6f)
                {
                    cols[y * w + x] = hierro;
                }

                // Marco perimetral rectangular inferior
                if (y <= 160 && (x <= 14 || x >= w - 15 || y <= 10))
                {
                    cols[y * w + x] = hierro;
                }
            }
        }

        // Barrotes con puntas de lanza
        int paso = 16;
        for (int x = 20; x < w - 20; x += paso)
        {
            for (int y = 14; y < 220; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centroArco);
                if (y <= 160 || dist < radioArco - 8f)
                {
                    for (int bx = x - 2; bx <= x + 2; bx++)
                    {
                        cols[y * w + bx] = (bx == x - 2) ? brillo : hierro;
                    }
                }
            }

            // Puntas de lanza
            for (int ly = 0; ly < 16; ly++)
            {
                int py = 160 + ly;
                int ancho = (16 - ly) / 3;
                for (int bx = x - ancho; bx <= x + ancho; bx++)
                {
                    if (py < h && bx >= 0 && bx < w) cols[py * w + bx] = hierro;
                }
            }
        }

        // Filigrana victoriana en espiral (arcos ornamentales)
        for (int f = 40; f < w - 40; f += 40)
        {
            Vector2 cFil = new Vector2(f, 175f);
            for (int y = 150; y < 200; y++)
            {
                for (int x = f - 20; x < f + 20; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), cFil);
                    if (d >= 14f && d <= 17f)
                    {
                        cols[y * w + x] = oxido;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 6. PILAR DE PIEDRA DE ENTRADA CON HIEDRA (128x256 Transparent)
    // =========================================================================
    private static Texture2D CrearSpritePilarPiedra()
    {
        int w = 128, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        for (int y = 0; y < h; y++)
        {
            // Plinto base, fuste central y capitel superior
            int anchoPilar = (y < 40 || y > 220) ? 90 : 72;
            int xMin = (w - anchoPilar) / 2;
            int xMax = xMin + anchoPilar;

            for (int x = xMin; x <= xMax; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.08f;
                float distBorde = Mathf.Min(x - xMin, xMax - x) / (anchoPilar / 2f);
                float luz = 0.20f + distBorde * 0.12f + n;

                Color cPiedra = new Color(luz * 0.95f, luz * 0.98f, luz * 1.05f, 1f);

                // Grietas en la piedra
                if (Mathf.Abs(Mathf.Sin(y * 0.1f + x * 0.2f)) < 0.06f && y > 50 && y < 200)
                {
                    cPiedra = Color.Lerp(cPiedra, new Color(0.08f, 0.08f, 0.08f, 1f), 0.7f);
                }

                cols[y * w + x] = cPiedra;
            }
        }

        // Hiedra trepadora verde oscura por el pilar
        for (int y = 20; y < 210; y += 3)
        {
            int hiedraX = 64 + Mathf.RoundToInt(Mathf.Sin(y * 0.08f) * 22f);
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -5; dx <= 5; dx++)
                {
                    int px = hiedraX + dx;
                    int py = y + dy;
                    if (px >= 0 && px < w && py >= 0 && py < h && Vector2.Distance(new Vector2(dx, dy), Vector2.zero) <= 4.5f)
                    {
                        cols[py * w + px] = new Color(0.08f, 0.18f, 0.09f, 0.95f);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 7. ÁRBOL MUERTO GÓTICO 1 - RETORCIDO Y DESNUDO (256x384 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteArbolMuerto1()
    {
        int w = 256, h = 384;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color madera = new Color(0.12f, 0.11f, 0.10f, 1f);
        Color corteza = new Color(0.22f, 0.20f, 0.18f, 1f);

        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        // Tronco gnarled principal
        for (int y = 0; y < 220; y++)
        {
            float prog = y / 220f;
            float curvaTronco = Mathf.Sin(prog * 3.5f) * 16f;
            int centroX = 128 + Mathf.RoundToInt(curvaTronco);
            int radio = Mathf.RoundToInt(Mathf.Lerp(32f, 12f, prog));

            for (int x = centroX - radio; x <= centroX + radio; x++)
            {
                if (x >= 0 && x < w)
                {
                    float d = Mathf.Abs(x - centroX) / (float)radio;
                    cols[y * w + x] = (d < 0.6f) ? corteza : madera;
                }
            }
        }

        // Ramas primarias retorcidas
        DibujarRama(cols, w, h, new Vector2(135, 180), new Vector2(50, 320), 12, 3, madera);
        DibujarRama(cols, w, h, new Vector2(50, 320), new Vector2(15, 370), 5, 1, madera);
        DibujarRama(cols, w, h, new Vector2(130, 210), new Vector2(120, 370), 10, 2, madera);
        DibujarRama(cols, w, h, new Vector2(140, 190), new Vector2(215, 320), 12, 3, madera);
        DibujarRama(cols, w, h, new Vector2(215, 320), new Vector2(250, 365), 5, 1, madera);

        // Raíces retorcidas en la base
        DibujarRama(cols, w, h, new Vector2(120, 25), new Vector2(70, 0), 16, 4, madera);
        DibujarRama(cols, w, h, new Vector2(136, 25), new Vector2(190, 0), 16, 4, madera);

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 8. ÁRBOL GÓTICO 2 - DOSEL HORIZONTAL (384x256 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteArbolMuerto2()
    {
        int w = 384, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color madera = new Color(0.11f, 0.10f, 0.09f, 1f);

        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        // Tronco inclinado que nace en la izquierda y cruza horizontalmente
        DibujarRama(cols, w, h, new Vector2(40, 0), new Vector2(90, 150), 28, 14, madera);
        DibujarRama(cols, w, h, new Vector2(90, 150), new Vector2(240, 210), 18, 8, madera);
        DibujarRama(cols, w, h, new Vector2(240, 210), new Vector2(370, 240), 9, 2, madera);
        DibujarRama(cols, w, h, new Vector2(180, 180), new Vector2(290, 140), 10, 2, madera);
        DibujarRama(cols, w, h, new Vector2(130, 160), new Vector2(60, 240), 11, 2, madera);

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 9. CIPRÉS CENTENARIO FÚNEBRE (128x384 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteCipresGotico()
    {
        int w = 128, h = 384;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        for (int y = 10; y < 370; y++)
        {
            float prog = (y - 10f) / 360f;
            // Forma esbelta de llama fúnebre
            float anchoMax = Mathf.Sin(prog * Mathf.PI * 0.85f) * 26f;
            float ruido = Mathf.PerlinNoise(y * 0.12f, 10f) * 5f;
            int radio = Mathf.RoundToInt(anchoMax + ruido);

            for (int x = 64 - radio; x <= 64 + radio; x++)
            {
                if (x >= 0 && x < w)
                {
                    float d = Mathf.Abs(x - 64) / (float)Mathf.Max(1, radio);
                    float n = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.08f;
                    float luz = (0.07f + n) * (1f - d * 0.4f);
                    cols[y * w + x] = new Color(luz * 0.7f, luz * 1.4f, luz * 0.8f, 1f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 10. LÁPIDA GÓTICA CON ARCO LABRADO (96x128 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteLapidaArco()
    {
        int w = 96, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        Vector2 cArco = new Vector2(48f, 75f);
        float radioArco = 36f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool dentro = false;
                if (y <= 75 && x >= 12 && x <= 84) dentro = true;
                if (y > 75 && Vector2.Distance(new Vector2(x, y), cArco) <= radioArco) dentro = true;

                if (dentro)
                {
                    float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.1f;
                    float bX = (x <= 16 || x >= 80) ? 0.7f : 1f;
                    float bY = (y >= 105) ? 1.2f : 1f;
                    float c = (0.35f + n) * bX * bY;

                    // Cruz tallada en relieve en el centro
                    float dX = Mathf.Abs(x - 48);
                    float dY = Mathf.Abs(y - 65);
                    bool cruz = (dX <= 3 && dY <= 20) || (dY <= 3 && dX <= 12 && y > 60);

                    if (cruz)
                    {
                        cols[y * w + x] = new Color(0.12f, 0.13f, 0.14f, 1f);
                    }
                    else
                    {
                        cols[y * w + x] = new Color(c * 0.95f, c * 0.98f, c * 1.05f, 1f);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 11. LÁPIDA CON CRUZ CELTA HISTÓRICA (96x160 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteLapidaCruzCelta()
    {
        int w = 96, h = 160;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        Vector2 centroCruz = new Vector2(48f, 115f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Pedestal escalonado base
                bool basePiedra = (y < 20 && x >= 14 && x <= 82) || (y >= 20 && y < 45 && x >= 24 && x <= 72);

                // Fuste vertical y travesaño de la cruz
                float dx = Mathf.Abs(x - 48f);
                float dy = Mathf.Abs(y - 115f);
                bool fuste = (dx <= 6f && y >= 45 && y <= 150);
                bool travesano = (dy <= 6f && x >= 18 && x <= 78);

                // Halo celta circular
                float distCentro = Vector2.Distance(new Vector2(x, y), centroCruz);
                bool halo = (distCentro >= 18f && distCentro <= 25f);

                if (basePiedra || fuste || travesano || halo)
                {
                    float n = Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.08f;
                    float c = 0.32f + n;
                    cols[y * w + x] = new Color(c * 0.95f, c * 1.0f, c * 1.08f, 1f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 12. OBELISCO GÓTICO MONUMENTAL (80x192 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteLapidaObelisco()
    {
        int w = 80, h = 192;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        for (int y = 0; y < h; y++)
        {
            int ancho;
            if (y < 35) ancho = 32; // Plinto base
            else if (y < 165) ancho = Mathf.RoundToInt(Mathf.Lerp(22f, 10f, (y - 35f) / 130f)); // Columna cónica
            else ancho = Mathf.RoundToInt(Mathf.Lerp(10f, 0f, (y - 165f) / 27f)); // Piramidión punta

            for (int x = 40 - ancho; x <= 40 + ancho; x++)
            {
                if (x >= 0 && x < w)
                {
                    float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.08f;
                    float sombreo = (x < 40) ? 1.15f : 0.85f;
                    float c = (0.34f + n) * sombreo;
                    cols[y * w + x] = new Color(c * 0.95f, c * 0.98f, c * 1.05f, 1f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 13. LÁPIDA QUEBRADA Y HUNDIDA EN FANGO (96x96 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteLapidaQuebrada()
    {
        int w = 96, h = 96;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Lápida ladeada 15 grados con esquina superior derecha rota
                float xRot = (x - 48f) * 0.96f - (y - 48f) * 0.26f + 48f;
                float yRot = (x - 48f) * 0.26f + (y - 48f) * 0.96f + 48f;

                bool dentro = (xRot >= 22 && xRot <= 74 && yRot >= 10 && yRot <= 80);
                // Esquina rota
                if (xRot > 55 && yRot > 60 && (xRot + yRot > 125)) dentro = false;

                if (dentro)
                {
                    float n = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.1f;
                    float c = 0.30f + n;

                    // Grieta profunda
                    if (Mathf.Abs(y - (x * 0.8f + 15)) < 1.8f && y > 25 && y < 65)
                    {
                        cols[y * w + x] = new Color(0.06f, 0.06f, 0.06f, 1f);
                    }
                    else
                    {
                        cols[y * w + x] = new Color(c * 0.95f, c * 0.98f, c * 1.05f, 1f);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 14. SARCOFAGO DE PIEDRA / TUMBA MONUMENTAL (192x128 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteSarcofago()
    {
        int w = 192, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        for (int y = 10; y < 118; y++)
        {
            int ancho = (y > 75) ? 82 : 74; // Tapa más ancha que la caja
            for (int x = 96 - ancho; x <= 96 + ancho; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.08f;
                float luz = (y > 75) ? 1.25f : 0.9f;
                float c = (0.28f + n) * luz;

                // Relieve de cruz sobre la tapa
                if (y > 85 && y < 105 && Mathf.Abs(x - 96) < 30)
                {
                    bool cruz = (Mathf.Abs(y - 95) < 3) || (Mathf.Abs(x - 96) < 4);
                    if (cruz) c *= 0.6f;
                }

                cols[y * w + x] = new Color(c * 0.95f, c * 0.98f, c * 1.05f, 1f);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 15. FOSA ABIERTA CON TIERRA REMOVIDA (192x128 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteFosaAbierta()
    {
        int w = 192, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        for (int y = 10; y < 118; y++)
        {
            for (int x = 20; x < w - 20; x++)
            {
                // Hoyo central negro profundo de la fosa
                bool hoyo = (x >= 45 && x <= 147 && y >= 30 && y <= 98);

                if (hoyo)
                {
                    float prof = (y - 30f) / 68f;
                    cols[y * w + x] = new Color(0.02f * prof, 0.02f * prof, 0.02f * prof, 1f);
                }
                else
                {
                    // Montículos irregulares de tierra fresca alrededor
                    float dX = Mathf.Min(Mathf.Abs(x - 45), Mathf.Abs(x - 147));
                    float dY = Mathf.Min(Mathf.Abs(y - 30), Mathf.Abs(y - 98));
                    float dist = Mathf.Min(dX, dY);

                    if (dist < 22f)
                    {
                        float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.1f;
                        cols[y * w + x] = new Color(0.18f + n, 0.12f + n, 0.07f + n, 0.95f);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 16. FAROL DE GAS VICTORIANO / NOCTURNO (64x144 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteFarolGas()
    {
        int w = 64, h = 144;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        Color hierro = new Color(0.12f, 0.12f, 0.14f, 1f);

        // Poste central
        for (int y = 0; y < 90; y++)
        {
            int anchoPoste = (y < 20) ? 8 : 4;
            for (int x = 32 - anchoPoste; x <= 32 + anchoPoste; x++)
            {
                cols[y * w + x] = hierro;
            }
        }

        // Linterna superior con llama
        for (int y = 90; y < 140; y++)
        {
            float prog = (y - 90f) / 50f;
            int anchoLinterna = Mathf.RoundToInt(Mathf.Lerp(10f, 18f, prog));
            if (y > 125) anchoLinterna = Mathf.RoundToInt(Mathf.Lerp(18f, 2f, (y - 125f) / 15f));

            for (int x = 32 - anchoLinterna; x <= 32 + anchoLinterna; x++)
            {
                bool borde = (x == 32 - anchoLinterna || x == 32 + anchoLinterna || y == 90 || y == 125);
                if (borde)
                {
                    cols[y * w + x] = hierro;
                }
                else if (y < 125)
                {
                    // Llama de gas cálida en el centro
                    float dLlama = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 107f));
                    if (dLlama < 8f)
                    {
                        cols[y * w + x] = new Color(1f, 0.88f, 0.45f, 1f);
                    }
                    else
                    {
                        cols[y * w + x] = new Color(0.85f, 0.70f, 0.35f, 0.45f);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 17. FACHADA MONUMENTAL DE CAPILLA GÓTICA (384x384 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteCapillaFachada()
    {
        int w = 384, h = 384;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        Color cantera = new Color(0.32f, 0.28f, 0.27f, 1f);
        Color canteraSombra = new Color(0.18f, 0.16f, 0.15f, 1f);

        for (int y = 0; y < h; y++)
        {
            // Frontón triangular gótico superior
            int anchoFachada = (y < 220) ? 170 : Mathf.RoundToInt(Mathf.Lerp(170f, 0f, (y - 220f) / 160f));

            for (int x = 192 - anchoFachada; x <= 192 + anchoFachada; x++)
            {
                // Portal monumental central abierto (arco apuntado)
                bool enPortal = false;
                if (y < 140 && Mathf.Abs(x - 192) < 45) enPortal = true;
                if (y >= 140 && y < 200)
                {
                    float hProg = (y - 140f) / 60f;
                    if (Mathf.Abs(x - 192) < (1f - hProg) * 45f) enPortal = true;
                }

                if (!enPortal)
                {
                    float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.08f;
                    cols[y * w + x] = new Color(cantera.r + n, cantera.g + n, cantera.b + n, 1f);
                }
                else
                {
                    // Hueco interior oscuro de la puerta
                    cols[y * w + x] = canteraSombra;
                }
            }
        }

        // Rosetón gótico circular sobre el portal
        Vector2 cRoseton = new Vector2(192f, 250f);
        for (int y = 210; y < 290; y++)
        {
            for (int x = 152; x < 232; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), cRoseton);
                if (d < 32f)
                {
                    bool trazas = (Mathf.Abs(d - 30f) < 2f) || (Mathf.Abs(x - 192) < 2f) || (Mathf.Abs(y - 250) < 2f);
                    cols[y * w + x] = trazas ? cantera : new Color(0.08f, 0.12f, 0.16f, 0.85f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 18. ALTAR MAYOR CON CIRIOS DE CERA ENCENDIDOS (160x160 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteAltarCirios()
    {
        int w = 160, h = 160;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color trasp = new Color(0, 0, 0, 0);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = trasp;

        Color cantera = new Color(0.40f, 0.38f, 0.36f, 1f);
        Color mantel = new Color(0.24f, 0.06f, 0.08f, 1f); // Carmesí fúnebre
        Color oro = new Color(0.85f, 0.70f, 0.25f, 1f);

        // Mesa del altar
        for (int y = 10; y < 80; y++)
        {
            int ancho = (y > 65) ? 72 : 64;
            for (int x = 80 - ancho; x <= 80 + ancho; x++)
            {
                cols[y * w + x] = (y < 65) ? cantera : mantel;
            }
        }

        // Cruz de latón central
        for (int y = 80; y < 145; y++)
        {
            for (int x = 77; x <= 83; x++) cols[y * w + x] = oro;
            if (y >= 115 && y <= 123)
            {
                for (int x = 62; x <= 98; x++) cols[y * w + x] = oro;
            }
        }

        // 4 Cirios de cera encendidos
        int[] posX = new int[] { 30, 50, 110, 130 };
        foreach (int cx in posX)
        {
            // Cirio blanco
            for (int cy = 80; cy < 115; cy++)
            {
                for (int x = cx - 3; x <= cx + 3; x++)
                {
                    cols[cy * w + x] = new Color(0.92f, 0.90f, 0.82f, 1f);
                }
            }

            // Llama dorada
            for (int fy = 115; fy < 132; fy++)
            {
                float prog = (fy - 115f) / 17f;
                int anchoLlama = Mathf.RoundToInt((1f - prog) * 4f);
                for (int x = cx - anchoLlama; x <= cx + anchoLlama; x++)
                {
                    cols[fy * w + x] = new Color(1f, 0.85f, 0.40f, 1f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 19. BRUMA / NIEBLA ESPECTRAL REALISTA (256x256 Transparent)
    // =========================================================================
    private static Texture2D CrearSpriteBrumaNiebla()
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        Vector2 centro = new Vector2(128f, 128f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centro);
                float caida = Mathf.Clamp01(1f - (dist / 128f));

                float n1 = Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                float n2 = Mathf.PerlinNoise((x + 100) * 0.08f, (y + 100) * 0.08f);
                float alpha = caida * (n1 * 0.6f + n2 * 0.4f) * 0.35f;

                cols[y * w + x] = new Color(0.75f, 0.82f, 0.90f, alpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // UTILIDAD: DIBUJAR RAMA RETORCIDA CON BRESENHAM
    // =========================================================================
    private static void DibujarRama(Color[] cols, int w, int h, Vector2 p0, Vector2 p1, float r0, float r1, Color col)
    {
        int pasos = Mathf.RoundToInt(Vector2.Distance(p0, p1) * 1.5f);
        for (int i = 0; i <= pasos; i++)
        {
            float t = i / (float)pasos;
            Vector2 pos = Vector2.Lerp(p0, p1, t);
            // Curvatura orgánica
            pos.x += Mathf.Sin(t * Mathf.PI) * 12f;

            float radio = Mathf.Lerp(r0, r1, t);
            int rInt = Mathf.CeilToInt(radio);

            for (int dy = -rInt; dy <= rInt; dy++)
            {
                for (int dx = -rInt; dx <= rInt; dx++)
                {
                    if (dx * dx + dy * dy <= radio * radio)
                    {
                        int px = Mathf.RoundToInt(pos.x + dx);
                        int py = Mathf.RoundToInt(pos.y + dy);
                        if (px >= 0 && px < w && py >= 0 && py < h)
                        {
                            cols[py * w + px] = col;
                        }
                    }
                }
            }
        }
    }
}
#endif
