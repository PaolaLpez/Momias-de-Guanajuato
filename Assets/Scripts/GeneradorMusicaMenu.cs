using UnityEngine;

/// <summary>
/// Generador procedural de audio ambiental tenebroso para el menú de "Momias de Guanajuato: La Noche del Museo".
/// Genera un drone espectral con campanas de catedral colonial, acordes lúgubres en Re menor y viento de medianoche.
/// </summary>
public static class GeneradorMusicaMenu
{
    public static AudioClip GenerarMusicaTerror(string clipName = "MusicaMenuTerror", float duracion = 30f)
    {
        int sampleRate = 44100;
        int totalSamples = Mathf.CeilToInt(duracion * sampleRate);
        int channels = 2;
        float[] data = new float[totalSamples * channels];

        // Frecuencias base para el drone sombrío en Re menor (D minor):
        // D1 = 36.71 Hz (sub-bajo), D2 = 73.42 Hz, D2 detuned = 73.95 Hz (batimiento de 0.53 Hz)
        float freqDrone1 = 36.71f;
        float freqDrone2 = 73.42f;
        float freqDroneDetuned = 73.95f;

        // Campanas coloniales (La3 / 220Hz) que tocan cada 10 segundos
        float[] campanaTiempos = new float[] { 0.5f, 10.0f, 20.0f };
        float campanaFreq = 220f;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;

            // 1. Drone profundo con modulación lenta de volumen (respiración oscura)
            float droneLFO = 0.72f + 0.28f * Mathf.Sin(t * 0.35f);
            float drone = (Mathf.Sin(2f * Mathf.PI * freqDrone1 * t) * 0.35f +
                           Mathf.Sin(2f * Mathf.PI * freqDrone2 * t) * 0.25f +
                           Mathf.Sin(2f * Mathf.PI * freqDroneDetuned * t) * 0.25f) * droneLFO;

            // 2. Pad armónico tenebroso en progresión Re menor
            float f1 = 146.83f, f2 = 174.61f, f3 = 220.00f; // Dm (Re menor)
            if (t >= 8f && t < 16f) { f1 = 116.54f; f2 = 146.83f; f3 = 174.61f; } // Sib mayor (Bb)
            else if (t >= 16f && t < 24f) { f1 = 98.00f; f2 = 116.54f; f3 = 146.83f; } // Sol menor (Gm)
            else if (t >= 24f) { f1 = 110.00f; f2 = 138.59f; f3 = 164.81f; } // La disminuido / dominante

            // Trémolo lento de cuerdas fantasmales
            float tremolo = 0.85f + 0.15f * Mathf.Sin(t * 2.8f);
            float pad = (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.10f +
                         Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.08f +
                         Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.07f) * tremolo;

            // 3. Campana de catedral colonial de Guanajuato (armónicos de campana con decaimiento natural)
            float campana = 0f;
            foreach (float ct in campanaTiempos)
            {
                if (t >= ct)
                {
                    float dt = t - ct;
                    float env = Mathf.Exp(-dt * 0.65f); // Resonancia larga
                    if (env > 0.0005f)
                    {
                        campana += (Mathf.Sin(2f * Mathf.PI * campanaFreq * dt) * 0.32f +
                                    Mathf.Sin(2f * Mathf.PI * (campanaFreq * 2.00f) * dt) * 0.18f +
                                    Mathf.Sin(2f * Mathf.PI * (campanaFreq * 2.76f) * dt) * 0.14f +
                                    Mathf.Sin(2f * Mathf.PI * (campanaFreq * 5.40f) * dt) * 0.08f) * env;
                    }
                }
            }

            // 4. Viento nocturno de callejón colonial
            float windMod = Mathf.Sin(t * 0.3f) * Mathf.Cos(t * 0.5f);
            float wind = Mathf.Sin(2f * Mathf.PI * (210f + 80f * windMod) * t) * 0.035f * Mathf.Abs(windMod);

            // Mezcla estéreo espacializada
            float mixL = (drone * 0.52f + pad * 0.60f + campana * 0.50f + wind * 0.45f);
            float mixR = (drone * 0.50f + pad * 0.58f + campana * 0.46f - wind * 0.45f);

            // Ventana de cross-fade en extremos para bucle sin saltos (seamless loop)
            float fadeTime = 2.5f;
            float loopFade = 1f;
            if (t < fadeTime)
            {
                loopFade = Mathf.SmoothStep(0f, 1f, t / fadeTime);
            }
            else if (t > (duracion - fadeTime))
            {
                loopFade = Mathf.SmoothStep(0f, 1f, (duracion - t) / fadeTime);
            }

            data[i * 2] = Mathf.Clamp(mixL * loopFade, -0.92f, 0.92f);
            data[i * 2 + 1] = Mathf.Clamp(mixR * loopFade, -0.92f, 0.92f);
        }

        AudioClip clip = AudioClip.Create(clipName, totalSamples, channels, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
