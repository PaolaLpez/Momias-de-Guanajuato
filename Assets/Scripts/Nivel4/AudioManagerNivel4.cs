using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Gestor centralizado de audio para el Nivel 4: "El Panteón de Santa Paula y la Campana del Alba".
/// Administra el arrastre de cadenas pesadas, campanas de iglesia, aullidos de la momia y viento nocturno.
/// Cuenta con síntesis procedural nativa para garantizar que todos los sonidos suenen siempre sin depender de descargas externas.
/// </summary>
public class AudioManagerNivel4 : MonoBehaviour
{
    public static AudioManagerNivel4 Instance { get; private set; }

    public const string CLIP_CADENAS = "arrastre_cadenas";
    public const string CLIP_CAMPANA = "campana_alba";
    public const string CLIP_VIENTO = "viento_panteon";
    public const string CLIP_ALARIDO = "alarido_bruja";
    public const string CLIP_PERDER = "perder_panteon";
    public const string CLIP_VICTORIA = "victoria_alba";
    public const string CLIP_RELIQUIA = "reliquia_consagrada";
    public const string CLIP_PASO_SIGILO = "paso_sigilo_grava";
    public const string CLIP_PASO_CARRERA = "paso_carrera_adoquin";
    public const string CLIP_HOJAS_SECAS = "hojas_secas_crujido";
    public const string CLIP_PUERTA = "puerta_capilla";
    public const string CLIP_INTERACCION = "interaccion_cirio";
    public const string CLIP_FONDO_4 = "fondo_4";
    public const string CLIP_CAMINATA_MOMIA = "caminata_momia";
    public const string CLIP_DETECCION_MOMIA4 = "deteccion";

    // Clips oficiales compartidos del Nivel 2 (Resources/Audio/Nivel2 y AudioNivel2)
    public const string CLIP_CAMINATA_N2 = "caminata";
    public const string CLIP_HERIDA_N2 = "herida_personaje";
    public const string CLIP_PERDER_N2 = "perder";
    public const string CLIP_SONIDO_MOMIA_N2 = "sonido_momia";
    public const string CLIP_ATAQUE_MOMIA_N2 = "ataque";

    private static readonly Dictionary<string, AudioClip> cacheClips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PrecargarClips();
    }

    private void Update()
    {
        // Desactivado el tañido automático periódico para que solo suene el ambiente limpio (fondo_4.mp3)
    }

    public static AudioClip ObtenerClip(string nombreClave)
    {
        if (cacheClips.TryGetValue(nombreClave, out AudioClip clip) && clip != null)
        {
            return clip;
        }

        // 1. Intentar cargar desde Resources (Nivel 4 y recursos oficiales compartidos del Nivel 2)
        AudioClip rClip = Resources.Load<AudioClip>("AudioNivel4/" + nombreClave);
        if (rClip == null) rClip = Resources.Load<AudioClip>("Audio/Nivel4/" + nombreClave);
        if (rClip == null) rClip = Resources.Load<AudioClip>("Audio/Nivel2/" + nombreClave);
        if (rClip == null) rClip = Resources.Load<AudioClip>("AudioNivel2/" + nombreClave);
        if (rClip == null) rClip = Resources.Load<AudioClip>(nombreClave);

        // 2. Si no existe en disco, generar proceduralmente
        if (rClip == null)
        {
            rClip = GenerarAudioProcedural(nombreClave);
        }

        if (rClip != null)
        {
            cacheClips[nombreClave] = rClip;
        }

        return rClip;
    }

    private void PrecargarClips()
    {
        ObtenerClip(CLIP_CADENAS);
        ObtenerClip(CLIP_CAMPANA);
        ObtenerClip(CLIP_ALARIDO);
        ObtenerClip(CLIP_PERDER);
        ObtenerClip(CLIP_VICTORIA);
        ObtenerClip(CLIP_RELIQUIA);
        ObtenerClip(CLIP_HOJAS_SECAS);
        ObtenerClip(CLIP_CAMINATA_N2);
        ObtenerClip(CLIP_HERIDA_N2);
        ObtenerClip(CLIP_PERDER_N2);
        ObtenerClip(CLIP_SONIDO_MOMIA_N2);
        ObtenerClip(CLIP_ATAQUE_MOMIA_N2);
        ObtenerClip(CLIP_FONDO_4);
        ObtenerClip(CLIP_CAMINATA_MOMIA);
        ObtenerClip(CLIP_DETECCION_MOMIA4);

        CargarClipExternoSiFalta(CLIP_FONDO_4, @"C:\Users\User\Downloads\fondo 4.mp3");
        CargarClipExternoSiFalta(CLIP_CAMINATA_MOMIA, @"C:\Users\User\Downloads\caminata momia.mp3");
        CargarClipExternoSiFalta(CLIP_DETECCION_MOMIA4, @"C:\Users\User\Downloads\deteccion.mp3");
    }

    public void CargarClipExternoSiFalta(string clave, string rutaDownloads, Action<AudioClip> callback = null)
    {
        AudioClip existente = ObtenerClip(clave);
        if (existente != null)
        {
            callback?.Invoke(existente);
            return;
        }

        string rutaProyecto = Path.Combine(Application.dataPath, "Resources", "Audio", "Nivel4", clave + ".mp3");
        string rutaElegida = File.Exists(rutaProyecto) ? rutaProyecto : rutaDownloads;
        if (!string.IsNullOrEmpty(rutaElegida) && File.Exists(rutaElegida))
        {
            StartCoroutine(CargarAudioWebCoroutine(clave, rutaElegida, callback));
        }
    }

    private IEnumerator CargarAudioWebCoroutine(string clave, string rutaArchivo, Action<AudioClip> callback)
    {
        string uri = "file:///" + rutaArchivo.Replace("\\", "/");
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    clip.name = clave;
                    clip.LoadAudioData();
                    cacheClips[clave] = clip;
                    callback?.Invoke(clip);
                }
            }
        }
    }

    private static AudioClip GenerarAudioProcedural(string tipo)
    {
        switch (tipo.ToLowerInvariant())
        {
            case CLIP_CADENAS:
                return SintetizarArrastreCadenas();
            case CLIP_CAMPANA:
                return SintetizarCampanaIglesia();
            case CLIP_VIENTO:
            case CLIP_FONDO_4:
            case CLIP_CAMINATA_MOMIA:
            case CLIP_DETECCION_MOMIA4:
            case CLIP_ALARIDO:
                // Sin alaridos sintéticos ni ruido blanco brusco; se usan los MP3 dedicados del Nivel 4
                return null;
            case CLIP_PERDER:
            case CLIP_PERDER_N2:
                return SintetizarJumpscareFinal();
            case CLIP_VICTORIA:
                return SintetizarVictoriaAlba();
            case CLIP_RELIQUIA:
                return SintetizarChimeReliquia();
            case CLIP_HOJAS_SECAS:
                return SintetizarHojasSecas();
            case CLIP_PASO_SIGILO:
            case CLIP_CAMINATA_N2:
                return SintetizarPasoPiedra(false);
            case CLIP_PASO_CARRERA:
                return SintetizarPasoPiedra(true);
            case CLIP_HERIDA_N2:
                return SintetizarDolorVigilante();
            case CLIP_SONIDO_MOMIA_N2:
            case CLIP_ATAQUE_MOMIA_N2:
                return SintetizarAlaridoBruja();
            case CLIP_PUERTA:
            case CLIP_INTERACCION:
                return SintetizarChimeReliquia();
            default:
                return null;
        }
    }

    /// <summary>
    /// Síntesis de eslabones pesados de hierro forjado golpeando y arrastrándose sobre adoquines.
    /// </summary>
    private static AudioClip SintetizarArrastreCadenas()
    {
        int sampleRate = 22050;
        float duracion = 2.4f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;

            // Eslabones pesados y sordos (sin estática ni pitidos agudos que ensucien fondo_4)
            float ciclo = t % 0.75f;
            float golpe = Mathf.Sin(2f * Mathf.PI * 240f * ciclo) * Mathf.Exp(-ciclo * 24f) * 0.28f
                        + Mathf.Sin(2f * Mathf.PI * 410f * ciclo) * Mathf.Exp(-ciclo * 30f) * 0.16f;

            samples[i] = Mathf.Clamp(golpe * 0.75f, -0.8f, 0.8f);
        }

        AudioClip clip = AudioClip.Create("ArrastreCadenas_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Síntesis del solemne toque de campana de bronce de la Capilla del Panteón (Campana del Alba).
    /// </summary>
    private static AudioClip SintetizarCampanaIglesia()
    {
        int sampleRate = 22050;
        float duracion = 4.2f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float strike = Mathf.Exp(-t * 22f) * 0.5f;

            // Armónicos típicos de campana litúrgica de bronce (fundamental en 220Hz La3)
            float h1 = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-t * 0.85f) * 0.65f;
            float h2 = Mathf.Sin(2f * Mathf.PI * 440f * t) * Mathf.Exp(-t * 1.35f) * 0.45f;
            float h3 = Mathf.Sin(2f * Mathf.PI * 660f * t) * Mathf.Exp(-t * 2.1f) * 0.35f;
            float h4 = Mathf.Sin(2f * Mathf.PI * 1180f * t) * Mathf.Exp(-t * 3.4f) * 0.25f;
            float hum = Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 0.45f) * 0.40f;

            float mix = strike + h1 + h2 + h3 + h4 + hum;
            samples[i] = Mathf.Clamp(mix * 0.9f, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("CampanaAlba_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Viento soplador gélido nocturno entre las tumbas y mausoleos.
    /// </summary>
    private static AudioClip SintetizarVientoPanteon()
    {
        int sampleRate = 22050;
        float duracion = 5.0f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];
        System.Random rng = new System.Random(404);

        float lastR = 0f;
        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float white = (float)rng.NextDouble() * 2f - 1f;

            // Filtro paso bajo simple para simular viento
            lastR = Mathf.Lerp(lastR, white, 0.045f);

            // Modulación de ráfaga
            float rafaga = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.25f * t + Mathf.Sin(t * 0.8f));
            float silbido = Mathf.Sin(2f * Mathf.PI * (280f + 45f * Mathf.Sin(t * 1.5f)) * t) * 0.08f;

            float val = (lastR * 0.7f + silbido) * rafaga * 0.4f;
            samples[i] = Mathf.Clamp(val, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create("VientoPanteon_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Alarido desgarrador de agonía de la Momia Encadenada.
    /// </summary>
    private static AudioClip SintetizarAlaridoBruja()
    {
        int sampleRate = 22050;
        float duracion = 2.1f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];
        System.Random rng = new System.Random(666);

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float p = t / duracion;

            float mod = Mathf.Sin(2f * Mathf.PI * 32f * t) * 45f;
            float fBase = Mathf.Lerp(750f, 420f, p) + mod;

            float a1 = Mathf.Sin(2f * Mathf.PI * fBase * t) * 0.45f;
            float a2 = Mathf.Sin(2f * Mathf.PI * (fBase * 1.52f) * t) * 0.35f;
            float a3 = Mathf.Sin(2f * Mathf.PI * (fBase * 0.48f) * t) * 0.30f;
            float raspe = ((float)rng.NextDouble() * 2f - 1f) * 0.35f;

            float env = Mathf.Sin(p * Mathf.PI);
            float mix = (a1 + a2 + a3 + raspe) * env;
            samples[i] = Mathf.Clamp(mix * 0.9f, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("AlaridoBruja_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Jumpscare orquestal violento para el momento de ser atrapado por la Bruja.
    /// </summary>
    private static AudioClip SintetizarJumpscareFinal()
    {
        int sampleRate = 22050;
        float duracion = 1.6f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];
        System.Random rng = new System.Random(1313);

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float slam = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Exp(-t * 6f) * 0.85f;
            float rasp = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 2.8f) * 0.55f;
            float screamt = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 1.8f) * 0.4f;

            samples[i] = Mathf.Clamp(slam + rasp + screamt, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("JumpscareFinal_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Fanfarria de campanas, esperanza y triunfo al romper el maleficio al amanecer.
    /// </summary>
    private static AudioClip SintetizarVictoriaAlba()
    {
        int sampleRate = 22050;
        float duracion = 3.6f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];

        float[] notas = new float[] { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f }; // Do mayor arpegiado brillante

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float sum = 0f;

            for (int n = 0; n < notas.Length; n++)
            {
                float tNota = t - (n * 0.28f);
                if (tNota > 0f)
                {
                    float env = Mathf.Exp(-tNota * 1.6f);
                    sum += Mathf.Sin(2f * Mathf.PI * notas[n] * tNota) * env * 0.25f;
                }
            }

            samples[i] = Mathf.Clamp(sum, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create("VictoriaAlba_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Chime místico al recolectar o consagrar una reliquia bendita.
    /// </summary>
    private static AudioClip SintetizarChimeReliquia()
    {
        int sampleRate = 22050;
        float duracion = 1.4f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float s1 = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * Mathf.Exp(-t * 3.5f) * 0.45f;
            float s2 = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * Mathf.Exp(-t * 4.2f) * 0.35f;
            float s3 = Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * Mathf.Exp(-t * 5.0f) * 0.25f;

            samples[i] = Mathf.Clamp(s1 + s2 + s3, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("ChimeReliquia_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Crujido seco y quebradizo de hojas de otoño al ser pisadas.
    /// </summary>
    private static AudioClip SintetizarHojasSecas()
    {
        int sampleRate = 22050;
        float duracion = 0.28f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];
        System.Random rng = new System.Random(717);
        float prev = 0f;

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float burst = Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.PI * t * 18f)), 1.6f) * Mathf.Exp(-t * 7f);
            float white = (float)rng.NextDouble() * 2f - 1f;
            // Filtro pasa-altos simple para crujido quebradizo
            float crisp = white - prev * 0.72f;
            prev = white;
            float grain = (rng.NextDouble() > 0.84) ? crisp * 1.4f : crisp * 0.55f;
            samples[i] = Mathf.Clamp(grain * burst * 0.85f, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("HojasSecasCrujido_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Pisada seca sobre grava o adoquín del panteón (caminar o correr).
    /// </summary>
    private static AudioClip SintetizarPasoPiedra(bool carrera)
    {
        int sampleRate = 22050;
        float duracion = carrera ? 0.14f : 0.11f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];
        System.Random rng = new System.Random(carrera ? 902 : 901);

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * (carrera ? 22f : 28f));
            float thud = Mathf.Sin(2f * Mathf.PI * (carrera ? 130f : 95f) * t) * 0.45f;
            float grit = ((float)rng.NextDouble() * 2f - 1f) * (carrera ? 0.45f : 0.25f);
            samples[i] = Mathf.Clamp((thud + grit) * env, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create(carrera ? "PasoCarrera_Procedural" : "PasoSigilo_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip SintetizarDolorVigilante()
    {
        int sampleRate = 22050;
        float duracion = 0.28f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];
        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Sin((t / duracion) * Mathf.PI) * Mathf.Exp(-t * 4f);
            float f = Mathf.Lerp(320f, 185f, t / duracion);
            samples[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.7f, -0.9f, 0.9f);
        }
        AudioClip clip = AudioClip.Create("HeridaVigilante_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
