using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Procesa automáticamente los recursos subidos por el usuario (audio del escorpión y sprites recortados con transparencia).
/// </summary>
[ExecuteAlways]
public class PreparadorAssetsNivel3 : MonoBehaviour
{
    private static bool yaProcesado = false;

    private void Awake()
    {
        ProcesarAssets();
    }

    [ContextMenu("Forzar Procesamiento de Assets")]
    public static void ProcesarAssets()
    {
        if (yaProcesado && Application.isPlaying) return;
        yaProcesado = true;

#if UNITY_EDITOR
        ProcesarAudioEscorpion();
        ProcesarSpritesEscorpion();
#endif
    }

    private static void ProcesarAudioEscorpion()
    {
        try
        {
            string descargas = @"C:\Users\ADMIN\Downloads";
            string destDir = Application.dataPath + "/Resources/AudioNivel3";
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            if (Directory.Exists(descargas))
            {
                string[] archivos = Directory.GetFiles(descargas, "*yodguard*");
                if (archivos.Length == 0) archivos = Directory.GetFiles(descargas, "*scorpion*");

                foreach (string f in archivos)
                {
                    string ext = Path.GetExtension(f).ToLower();
                    if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                    {
                        string destPath = destDir + "/ataque_escorpion" + ext;
                        File.Copy(f, destPath, true);
                        Debug.Log("<color=green>[PreparadorAssetsNivel3] Audio copiado: " + destPath + "</color>");
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[PreparadorAssetsNivel3] Aviso en audio: " + ex.Message);
        }
    }

    private static void ProcesarSpritesEscorpion()
    {
        try
        {
            string imgSubida = @"C:\Users\ADMIN\.gemini\antigravity\brain\872a3237-d38b-4734-9e83-abeca50664a8\.user_uploaded\media_1793740666972_727799a3.png";
            if (!File.Exists(imgSubida)) return;

            string destFolder = Application.dataPath + "/Resources/Escorpiones";
            if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

            byte[] bytes = File.ReadAllBytes(imgSubida);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return;

            int w = tex.width;
            int h = tex.height;

            // Hacer transparente el fondo blanco
            Color[] cols = tex.GetPixels();
            for (int i = 0; i < cols.Length; i++)
            {
                Color c = cols[i];
                if (c.r > 0.90f && c.g > 0.90f && c.b > 0.90f)
                {
                    cols[i] = Color.clear;
                }
                else if (c.r > 0.82f && c.g > 0.82f && c.b > 0.82f)
                {
                    float a = 1.0f - ((c.r + c.g + c.b) / 3f - 0.82f) / 0.18f;
                    cols[i] = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
                }
            }
            tex.SetPixels(cols);
            tex.Apply();

            // La hoja de sprites tiene 3 filas y 4 columnas
            int cellW = w / 4;
            int cellH = h / 3;

            // Fila 2 (superior en Unity Y): Columna 1 -> idle, Columna 0 -> attack
            // Fila 1 (centro en Unity Y): Columna 0 -> walk 0, Columna 1 -> walk 1
            GuardarCelda(tex, 1, 2, cellW, cellH, destFolder + "/escorpion_idle.png");
            GuardarCelda(tex, 0, 1, cellW, cellH, destFolder + "/escorpion_walk_0.png");
            GuardarCelda(tex, 1, 1, cellW, cellH, destFolder + "/escorpion_walk_1.png");
            GuardarCelda(tex, 0, 2, cellW, cellH, destFolder + "/escorpion_attack.png");

            Debug.Log("<color=green>[PreparadorAssetsNivel3] ¡Sprites del escorpión procesados con éxito!</color>");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[PreparadorAssetsNivel3] Aviso en sprites: " + ex.Message);
        }
    }

    private static void GuardarCelda(Texture2D fuente, int col, int fila, int cellW, int cellH, string rutaDestino)
    {
        int startX = col * cellW;
        int startY = fila * cellH;

        Color[] celda = fuente.GetPixels(startX, startY, cellW, cellH);

        // Encontrar bounding box con píxeles visibles
        int minX = cellW, maxX = 0, minY = cellH, maxY = 0;
        for (int y = 0; y < cellH; y++)
        {
            for (int x = 0; x < cellW; x++)
            {
                if (celda[y * cellW + x].a > 0.1f)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (minX > maxX || minY > maxY) return;

        int cropW = maxX - minX + 1;
        int cropH = maxY - minY + 1;
        int maxDim = Mathf.Max(cropW, cropH) + 8;

        Texture2D resultado = new Texture2D(maxDim, maxDim, TextureFormat.RGBA32, false);
        Color[] resCols = new Color[maxDim * maxDim];
        for (int i = 0; i < resCols.Length; i++) resCols[i] = Color.clear;

        int offsetX = (maxDim - cropW) / 2;
        int offsetY = (maxDim - cropH) / 2;

        for (int y = 0; y < cropH; y++)
        {
            for (int x = 0; x < cropW; x++)
            {
                Color c = celda[(minY + y) * cellW + (minX + x)];
                resCols[(offsetY + y) * maxDim + (offsetX + x)] = c;
            }
        }

        resultado.SetPixels(resCols);
        resultado.Apply();

        byte[] pngData = resultado.EncodeToPNG();
        File.WriteAllBytes(rutaDestino, pngData);
    }
}
