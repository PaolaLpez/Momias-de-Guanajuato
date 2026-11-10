using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detector acústico en tiempo real del micrófono físico del jugador para el Nivel 2.
/// Optimizado con muestreo desacoplado a 25 Hz y caché de objetos del escenario para
/// garantizar 0 tirones (stutter) y movimiento 100% fluido del personaje principal.
/// </summary>
public class DetectorMicrofonoNivel2 : MonoBehaviour
{
    public static DetectorMicrofonoNivel2 Instance { get; private set; }

    [Header("Configuración del Micrófono (Solo desde Menú Configuración)")]
    public bool microfonoActivo = true;
    [Range(0.15f, 0.95f)]
    public float sensibilidad = 0.72f;
    public float enfriamientoEmision = 1.35f;
    public float radioMinimoSonido = 13f;
    public float radioMaximoSonido = 34f;

    // Estado de lectura en tiempo real para la barra del HUD y la IA
    public float NivelVolumenActual { get; private set; }
    public float NivelPicoReciente { get; private set; }
    public float DecibeliosAprox { get; private set; } = -60f;
    public bool DispositivoDisponible { get; private set; }
    public string NombreDispositivo { get; private set; } = "Detectando...";
    public bool SuperandoUmbral { get; private set; }
    public bool JugadorEnEsconditeSeguro { get; private set; }
    public bool SonidoEnmascaradoPorVapor { get; private set; }
    public float TiempoUltimaDeteccionMic { get; private set; } = -99f;

    public static bool VozSimuladaActiva { get; set; } = false;
    private AudioSource audioSourceVoz;
    private static AudioClip clipVozDetectada;

    // Umbral dinámico según sensibilidad configurada (calibrado para captar la voz humana con naturalidad)
    public float UmbralDisparo => Mathf.Lerp(0.48f, 0.18f, sensibilidad);

    private class CanalMicrofono
    {
        public string nombre;
        public AudioClip clip;
    }

    private readonly List<CanalMicrofono> canalesActivos = new List<CanalMicrofono>();
    private const int VentanaMuestras = 512;
    private const int FrecuenciaMuestreo = 44100;
    private const float IntervaloMuestreoMic = 0.04f; // 25 Hz: respuesta instantánea sin bloquear el hilo principal
    private readonly float[] bufferMuestras = new float[VentanaMuestras];

    private int indiceDispositivoForzado = -1; // -1 = Automático
    private float temporizadorCooldown;
    private float temporizadorReintentoMic;
    private float temporizadorMuestreoMic;
    private float ultimoNivelAmplificado;
    private float pisoRuidoAdaptativo = 0.015f;
    private float tiempoSostenidoSobreUmbral;

    // Caché estática de objetos del escenario para evitar FindObjectsByType cada frame
    private static EsconditeNivel2[] cacheEscondites;
    private static ValvulaVaporNivel2[] cacheValvulas;
    private static float tiempoUltimaActualizacionCache = -99f;

    private const float PisoRuidoElectrico = 0.015f;
    private const string PrefKeySensibilidad = "Nivel2_MicSensibilidad_V2";
    private const string PrefKeyActivo = "Nivel2_MicActivo_V2";

    private void Awake()
    {
        string escena = SceneManager.GetActiveScene().name;
        if (escena == "Nivel1" || escena == "MenuPrincipal" || !escena.Contains("2"))
        {
            enabled = false;
            return;
        }

        Instance = this;
        sensibilidad = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKeySensibilidad, 0.72f), 0.15f, 0.95f);
        microfonoActivo = PlayerPrefs.GetInt(PrefKeyActivo, 1) == 1;
    }

    private void Start()
    {
        RefrescarCacheEscenario(true);
        if (microfonoActivo)
        {
            IniciarCapturaMicrofono();
        }
    }

    private void OnEnable()
    {
        Instance = this;
        sensibilidad = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKeySensibilidad, 0.72f), 0.15f, 0.95f);
        microfonoActivo = PlayerPrefs.GetInt(PrefKeyActivo, 1) == 1;
        if (Application.isPlaying && microfonoActivo && canalesActivos.Count == 0)
        {
            IniciarCapturaMicrofono();
        }
    }

    private void OnDisable()
    {
        DetenerCapturaMicrofono();
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Solo invocable desde el Menú de Configuración del juego.
    /// </summary>
    public void CambiarSensibilidadDesdeConfiguracion(float nuevaSensibilidad)
    {
        sensibilidad = Mathf.Clamp(nuevaSensibilidad, 0.15f, 0.95f);
        PlayerPrefs.SetFloat(PrefKeySensibilidad, sensibilidad);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Solo invocable desde el Menú de Configuración del juego.
    /// </summary>
    public void CambiarDispositivoDesdeConfiguracion()
    {
        if (canalesActivos.Count <= 1) return;
        indiceDispositivoForzado++;
        if (indiceDispositivoForzado >= canalesActivos.Count)
        {
            indiceDispositivoForzado = -1;
        }
    }

    public void IniciarCapturaMicrofono()
    {
#if !UNITY_WEBGL
        DetenerCapturaMicrofono();
        canalesActivos.Clear();

        try
        {
            string[] dispositivos = Microphone.devices;
            if (dispositivos != null && dispositivos.Length > 0)
            {
                int maxDispositivos = Mathf.Min(dispositivos.Length, 2);
                for (int i = 0; i < maxDispositivos; i++)
                {
                    string dev = dispositivos[i];
                    if (string.IsNullOrEmpty(dev)) continue;

                    try
                    {
                        if (Microphone.IsRecording(dev))
                        {
                            Microphone.End(dev);
                        }

                        int minF, maxF;
                        Microphone.GetDeviceCaps(dev, out minF, out maxF);
                        int freq = 44100;
                        if (minF > 0 && freq < minF) freq = minF;
                        if (maxF > 0 && freq > maxF) freq = maxF;

                        AudioClip clip = Microphone.Start(dev, true, 3, freq);
                        if (clip != null)
                        {
                            canalesActivos.Add(new CanalMicrofono { nombre = dev, clip = clip });
                        }
                    }
                    catch
                    {
                    }
                }
            }

            if (canalesActivos.Count == 0)
            {
                int minF, maxF;
                Microphone.GetDeviceCaps(null, out minF, out maxF);
                int freq = 44100;
                if (minF > 0 && freq < minF) freq = minF;
                if (maxF > 0 && freq > maxF) freq = maxF;

                AudioClip clipDefault = Microphone.Start(null, true, 3, freq);
                if (clipDefault != null)
                {
                    canalesActivos.Add(new CanalMicrofono { nombre = "Predeterminado", clip = clipDefault });
                }
            }

            if (canalesActivos.Count > 0)
            {
                DispositivoDisponible = true;
                NombreDispositivo = canalesActivos[0].nombre;
            }
            else
            {
                DispositivoDisponible = false;
                NombreDispositivo = "Sin micrófono";
            }
        }
        catch (Exception ex)
        {
            DispositivoDisponible = false;
            NombreDispositivo = "Micrófono no disponible";
            Debug.LogWarning("[DetectorMicrofonoNivel2] No se pudo abrir el micrófono: " + ex.Message);
        }
#else
        DispositivoDisponible = false;
        NombreDispositivo = "WebGL Audio";
#endif
    }

    private void DetenerCapturaMicrofono()
    {
#if !UNITY_WEBGL
        try
        {
            for (int i = 0; i < canalesActivos.Count; i++)
            {
                string dev = canalesActivos[i].nombre;
                if (dev == "Predeterminado") dev = null;
                if (Microphone.IsRecording(dev))
                {
                    Microphone.End(dev);
                }
            }
        }
        catch
        {
        }
        canalesActivos.Clear();
#endif
    }

    private void Update()
    {
        if (!Application.isPlaying || Time.timeScale == 0f)
        {
            NivelVolumenActual = 0f;
            SuperandoUmbral = false;
            return;
        }

        JugadorEnEsconditeSeguro = VerificarSiJugadorEstaEscondido();
        SonidoEnmascaradoPorVapor = VerificarVaporActivoCercano(transform.position, 7.8f);

        if (microfonoActivo && (!DispositivoDisponible || canalesActivos.Count == 0))
        {
            temporizadorReintentoMic += Time.deltaTime;
            if (temporizadorReintentoMic >= 3.5f)
            {
                temporizadorReintentoMic = 0f;
                IniciarCapturaMicrofono();
            }
        }

        // Muestreo desacoplado a 25 Hz para cero impacto en el framerate del movimiento
        temporizadorMuestreoMic += Time.deltaTime;
        if (temporizadorMuestreoMic >= IntervaloMuestreoMic)
        {
            temporizadorMuestreoMic = 0f;
            float nivelEntradaBruto = 0f;
            if (microfonoActivo && DispositivoDisponible)
            {
                nivelEntradaBruto = LeerNivelAudioMicrofono();
            }

            // Filtro adaptativo de ruido de fondo: aprende el nivel de estática del micrófono para no activarse en silencio
            if (nivelEntradaBruto < pisoRuidoAdaptativo + 0.012f)
            {
                pisoRuidoAdaptativo = Mathf.Lerp(pisoRuidoAdaptativo, Mathf.Max(PisoRuidoElectrico, nivelEntradaBruto * 1.25f), 0.12f);
            }
            else
            {
                pisoRuidoAdaptativo = Mathf.Lerp(pisoRuidoAdaptativo, Mathf.Max(PisoRuidoElectrico, nivelEntradaBruto * 0.65f), 0.015f);
            }

            float pisoEfectivo = Mathf.Max(PisoRuidoElectrico, pisoRuidoAdaptativo);
            float senalLimpia = Mathf.Max(0f, nivelEntradaBruto - pisoEfectivo);
            float gananciaSegunSensibilidad = Mathf.Lerp(4.5f, 16.0f, sensibilidad);
            ultimoNivelAmplificado = Mathf.Clamp01(senalLimpia * gananciaSegunSensibilidad);
        }

        float velocidadSuavizado = ultimoNivelAmplificado > NivelVolumenActual ? 18f : 8.5f;
        NivelVolumenActual = Mathf.Lerp(NivelVolumenActual, ultimoNivelAmplificado, Time.deltaTime * velocidadSuavizado);
        if (NivelVolumenActual < 0.012f) NivelVolumenActual = 0f;

        if (NivelVolumenActual > NivelPicoReciente)
        {
            NivelPicoReciente = NivelVolumenActual;
        }
        else
        {
            NivelPicoReciente = Mathf.MoveTowards(NivelPicoReciente, NivelVolumenActual, Time.deltaTime * 0.45f);
        }

        DecibeliosAprox = Mathf.Lerp(-60f, 0f, NivelVolumenActual);

        float umbral = UmbralDisparo;

        // Soporte de voz simulada (Tecla V, botón táctil en pantalla o móvil)
        bool inputVozSimulada = Input.GetKey(KeyCode.V) || VozSimuladaActiva || TouchControlsManager.IsVoiceSimulated;
        if (inputVozSimulada)
        {
            ultimoNivelAmplificado = 0.95f;
            NivelVolumenActual = Mathf.Max(NivelVolumenActual, 0.95f);
            tiempoSostenidoSobreUmbral = 0.35f;
        }

        bool sobreUmbralInstantaneo = microfonoActivo && NivelVolumenActual >= umbral;
        if (sobreUmbralInstantaneo)
        {
            tiempoSostenidoSobreUmbral += Time.deltaTime;
        }
        else
        {
            tiempoSostenidoSobreUmbral = Mathf.Max(0f, tiempoSostenidoSobreUmbral - Time.deltaTime * 3f);
        }

        // Exigir al menos 0.14s de voz real continua sobre el umbral para ignorar chasquidos o estática (instantáneo si es voz simulada)
        SuperandoUmbral = sobreUmbralInstantaneo && (tiempoSostenidoSobreUmbral >= 0.14f || inputVozSimulada);

        if (temporizadorCooldown > 0f)
        {
            temporizadorCooldown -= Time.deltaTime;
        }

        if (SuperandoUmbral && temporizadorCooldown <= 0f)
        {
            temporizadorCooldown = enfriamientoEmision;
            tiempoSostenidoSobreUmbral = 0f;

            if (JugadorEnEsconditeSeguro || SonidoEnmascaradoPorVapor)
            {
                return;
            }

            // Reproducir sonido audible en el juego de la voz/aliento de la vigilante
            if (audioSourceVoz == null)
            {
                audioSourceVoz = GetComponent<AudioSource>();
                if (audioSourceVoz == null) audioSourceVoz = gameObject.AddComponent<AudioSource>();
                audioSourceVoz.spatialBlend = 0f;
            }
            if (audioSourceVoz != null)
            {
                audioSourceVoz.PlayOneShot(GenerarAudioAlertaVoz(), 0.92f);
            }

            float exceso = Mathf.InverseLerp(umbral, 1f, NivelVolumenActual);
            float radioEmitido = Mathf.Lerp(radioMinimoSonido, radioMaximoSonido, exceso);
            TiempoUltimaDeteccionMic = Time.time;

            string etiqueta = exceso > 0.45f
                ? "¡VOZ FUERTE EN MICRÓFONO DETECTADA!"
                : "¡SONIDO EN TU MICRÓFONO DETECTADO!";

            TrampaAcusticaNivel2.EmitirRuido(transform.position, radioEmitido, etiqueta);
        }
    }

    private AudioClip GenerarAudioAlertaVoz()
    {
        if (clipVozDetectada != null) return clipVozDetectada;
        int sampleRate = 22050;
        float duracion = 0.50f;
        int totalMuestras = Mathf.CeilToInt(sampleRate * duracion);
        float[] muestras = new float[totalMuestras];
        System.Random rng = new System.Random(54321);

        for (int i = 0; i < totalMuestras; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duracion;

            // Aliento / suspiro / quejido sonoro que suena claramente en altavoces/auriculares
            float formante1 = Mathf.Sin(2f * Mathf.PI * 360f * t) * 0.38f;
            float formante2 = Mathf.Sin(2f * Mathf.PI * 680f * t) * 0.28f;
            float soplido = ((float)rng.NextDouble() * 2f - 1f) * 0.42f;
            float env = Mathf.Sin(prog * Mathf.PI) * Mathf.Exp(-prog * 1.8f);

            muestras[i] = Mathf.Clamp((formante1 + formante2 + soplido) * env * 0.90f, -0.95f, 0.95f);
        }

        clipVozDetectada = AudioClip.Create("AlertaVozJugador_Cue", totalMuestras, 1, sampleRate, false);
        clipVozDetectada.SetData(muestras, 0);
        return clipVozDetectada;
    }

    private float LeerNivelAudioMicrofono()
    {
#if !UNITY_WEBGL
        if (canalesActivos.Count == 0) return 0f;

        if (indiceDispositivoForzado >= 0 && indiceDispositivoForzado < canalesActivos.Count)
        {
            CanalMicrofono canal = canalesActivos[indiceDispositivoForzado];
            NombreDispositivo = canal.nombre;
            return MedirCanal(canal);
        }

        float mejorNivel = 0f;
        string mejorNombre = canalesActivos[0].nombre;

        for (int i = 0; i < canalesActivos.Count; i++)
        {
            float nivel = MedirCanal(canalesActivos[i]);
            if (nivel > mejorNivel)
            {
                mejorNivel = nivel;
                mejorNombre = canalesActivos[i].nombre;
            }
        }

        if (mejorNivel > 0.004f)
        {
            NombreDispositivo = mejorNombre;
        }

        return mejorNivel;
#else
        return 0f;
#endif
    }

    private float MedirCanal(CanalMicrofono canal)
    {
#if !UNITY_WEBGL
        if (canal == null || canal.clip == null) return 0f;

        string devQuery = canal.nombre == "Predeterminado" ? null : canal.nombre;
        int posEscritura = Microphone.GetPosition(devQuery);
        if (posEscritura <= 0 || canal.clip.samples <= VentanaMuestras) return 0f;

        int posInicio = posEscritura - VentanaMuestras;
        if (posInicio < 0) posInicio = 0;
        if (posInicio + VentanaMuestras > canal.clip.samples)
        {
            posInicio = Mathf.Max(0, canal.clip.samples - VentanaMuestras);
        }

        if (!canal.clip.GetData(bufferMuestras, posInicio))
        {
            return 0f;
        }

        float sumaCuadrados = 0f;
        float picoMax = 0f;
        for (int i = 0; i < VentanaMuestras; i++)
        {
            float muestra = Mathf.Abs(bufferMuestras[i]);
            sumaCuadrados += muestra * muestra;
            if (muestra > picoMax) picoMax = muestra;
        }

        float rms = Mathf.Sqrt(sumaCuadrados / VentanaMuestras);
        return Mathf.Max(rms * 2.6f, picoMax * 0.80f);
#else
        return 0f;
#endif
    }

    private static void RefrescarCacheEscenario(bool forzar = false)
    {
        if (!forzar && cacheEscondites != null && cacheValvulas != null && (Time.unscaledTime - tiempoUltimaActualizacionCache) < 2.0f)
        {
            return;
        }
        tiempoUltimaActualizacionCache = Time.unscaledTime;
        cacheEscondites = FindObjectsByType<EsconditeNivel2>(FindObjectsSortMode.None);
        cacheValvulas = FindObjectsByType<ValvulaVaporNivel2>(FindObjectsSortMode.None);
    }

    public static EsconditeNivel2 ObtenerEsconditeOcupadoCacheado()
    {
        RefrescarCacheEscenario();
        if (cacheEscondites == null) return null;
        for (int i = 0; i < cacheEscondites.Length; i++)
        {
            if (cacheEscondites[i] != null && cacheEscondites[i].EstaOcupado)
            {
                return cacheEscondites[i];
            }
        }
        return null;
    }

    public static bool VerificarSiJugadorEstaEscondido()
    {
        return ObtenerEsconditeOcupadoCacheado() != null;
    }

    public static bool VerificarVaporActivoCercano(Vector3 posicion, float distanciaMax)
    {
        RefrescarCacheEscenario();
        if (cacheValvulas == null) return false;
        float distCuadradaMax = distanciaMax * distanciaMax;
        for (int i = 0; i < cacheValvulas.Length; i++)
        {
            if (cacheValvulas[i] != null && cacheValvulas[i].estaActiva)
            {
                float d2 = (cacheValvulas[i].transform.position - posicion).sqrMagnitude;
                if (d2 <= distCuadradaMax)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
