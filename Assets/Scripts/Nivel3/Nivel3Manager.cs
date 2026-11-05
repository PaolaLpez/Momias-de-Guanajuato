using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor General del Nivel 3: "Las Hermanas" — Taller de Restauración (2:00 AM a 3:00 AM).
/// Administra el reloj en vivo, las 3 cifras de la morgue, los audios ambientales,
/// el estado de veneno de los escorpiones y las condiciones de victoria y derrota.
/// </summary>
public class Nivel3Manager : MonoBehaviour
{
    public static Nivel3Manager Instance { get; private set; }

    [Header("Estado del Nivel")]
    public bool JuegoPausadoOTerminado = false;
    public bool NivelCompletado = false;
    public bool TieneLlaveMaestra = false;
    public bool PuertaEscapeAbierta = false;

    [Header("Cifras de la Caja Fuerte [ 7 ] [ 3 ] [ 9 ]")]
    public int? cifra1 = null;
    public int? cifra2 = null;
    public int? cifra3 = null;

    [Header("Reloj 2:00 AM - 3:00 AM")]
    private float tiempoNivel = 0f;
    private const float DURACION_NIVEL_SEGUNDOS = 210f; // 3.5 minutos reales
    private string textoReloj = "02:00 AM";

    [Header("Audios de Nivel 3")]
    private AudioSource audioFondo;
    private AudioSource audioReloj;
    private AudioSource audioSFX;
    private AudioSource audioJumpscare;
    private AudioClip clipGameOver;

    [Header("Jumpscare de Momias y Game Over")]
    private bool jumpscareActivado = false;
    private float tiempoJumpscare = 0f;
    private const float DURACION_JUMPSCARE = 1.95f;
    private Texture2D texMomiaFrente1;
    private Texture2D texMomiaFrente2;

    [Header("Victoria con Momia Riéndose (Como en Nivel 1)")]
    private Texture2D[] texMomiaSonriendo;
    private Texture2D texAuraRadialMomia;
    private AudioSource audioVictoria;
    private AudioClip clipRisaVictoria;
    private bool risaVictoriaReproducida = false;

    [Header("Efecto de Veneno por Alacrán")]
    private float intensidadVeneno = 0f;
    private Texture2D texVinetaTerror;

    [Header("Avisos y Modales")]
    private string avisoRuidoTexto = "";
    private float timerAvisoRuido = 0f;

    private string textoModalNota = "";
    private float timerModalNota = 0f;

    private string promptInteraccionTexto = "";
    private float progresoAccion = 0f;
    private string etiquetaProgreso = "";

    private string motivoGameOver = "";

    [Header("Letrero Tenebroso de Despertar")]
    private bool mostrarBannerDespertar = false;
    private float timerBannerDespertar = 0f;
    private const float DURACION_BANNER_DESPERTAR = 8.5f;

    // Gemelas
    private MomiaAcechadoraNivel3 gemelaAcechadora;
    private MomiaCiegaNivel3 gemelaCiega;

    [Header("Menú de Configuración y Monitoreo de Audio")]
    public bool menuConfiguracionAbierto = false;
    private float volumenGeneral = 1f;
    private float volumenMusica = 0.60f;
    private float volumenSFX = 0.85f;
    private readonly float[] muestrasAudio = new float[128];
    private float nivelVolumenRMS = 0f;
    private float decibeliosMonitor = -60f;

    private void Awake()
    {
        Time.timeScale = 1f;
        JuegoPausadoOTerminado = false;
        NivelCompletado = false;
        TieneLlaveMaestra = false;
        PuertaEscapeAbierta = false;
        jumpscareActivado = false;
        tiempoJumpscare = 0f;
        menuConfiguracionAbierto = false;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Configurar Audios
        ConfigurarAudios();
    }

    private void ConfigurarAudios()
    {
        volumenGeneral = PlayerPrefs.GetFloat("VolumenGeneral", PlayerPrefs.GetFloat("VolumenMusica", 1f));
        volumenMusica = PlayerPrefs.GetFloat("VolumenAmbienteNivel3", 0.60f);
        volumenSFX = PlayerPrefs.GetFloat("VolumenSFXNivel3", 0.85f);
        AudioListener.volume = volumenGeneral;

        audioFondo = gameObject.AddComponent<AudioSource>();
        audioFondo.loop = true;
        audioFondo.playOnAwake = false;
        audioFondo.spatialBlend = 0f;
        audioFondo.volume = volumenMusica * 0.55f;

        AudioClip clipFondo = Resources.Load<AudioClip>("AudioNivel3/fondo_nivel3");
        if (clipFondo == null) clipFondo = Resources.Load<AudioClip>("fondo_nivel3");
        if (clipFondo != null)
        {
            audioFondo.clip = clipFondo;
            audioFondo.Play();
        }

        audioReloj = gameObject.AddComponent<AudioSource>();
        audioReloj.loop = true;
        audioReloj.playOnAwake = false;
        audioReloj.spatialBlend = 0f;
        audioReloj.volume = volumenMusica * 0.65f;

        AudioClip clipReloj = Resources.Load<AudioClip>("AudioNivel3/reloj_nivel3");
        if (clipReloj == null) clipReloj = Resources.Load<AudioClip>("reloj_nivel3");
        if (clipReloj != null)
        {
            audioReloj.clip = clipReloj;
            audioReloj.Play();
        }

        audioSFX = gameObject.AddComponent<AudioSource>();
        audioSFX.spatialBlend = 0f;
        audioSFX.playOnAwake = false;
        audioSFX.volume = volumenSFX;

        audioJumpscare = gameObject.AddComponent<AudioSource>();
        audioJumpscare.spatialBlend = 0f;
        audioJumpscare.playOnAwake = false;
        audioJumpscare.loop = false;
        audioJumpscare.ignoreListenerPause = true;
        audioJumpscare.volume = volumenSFX;

        audioVictoria = gameObject.AddComponent<AudioSource>();
        audioVictoria.spatialBlend = 0f;
        audioVictoria.playOnAwake = false;
        audioVictoria.loop = false;
        audioVictoria.ignoreListenerPause = true;
        audioVictoria.volume = volumenSFX;

        clipGameOver = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_PERDER);
        if (clipGameOver == null) clipGameOver = Resources.Load<AudioClip>("AudioNivel2/perder");
        if (clipGameOver == null) clipGameOver = Resources.Load<AudioClip>("perder");
        if (clipGameOver == null) clipGameOver = GenerarAudioJumpscareTerror();
    }

    private AudioClip GenerarRisaMomiaProcedural()
    {
        int sampleRate = 44100;
        float duracion = 3.2f;
        int totalMuestras = (int)(sampleRate * duracion);
        float[] muestras = new float[totalMuestras];
        System.Random rng = new System.Random(494314);

        float[] tiemposBursts = { 0.05f, 0.35f, 0.65f, 0.98f, 1.34f, 1.74f, 2.18f, 2.62f };
        float[] frecsBase = { 350f, 370f, 385f, 365f, 340f, 310f, 280f, 245f };

        for (int i = 0; i < totalMuestras; i++)
        {
            float t = (float)i / sampleRate;
            float mixTotal = 0f;

            for (int b = 0; b < tiemposBursts.Length; b++)
            {
                float tInicio = tiemposBursts[b];
                float durBurst = (b >= 6) ? 0.65f : 0.35f;
                if (t >= tInicio && t < tInicio + durBurst)
                {
                    float tb = t - tInicio;
                    float progB = tb / durBurst;
                    float env = Mathf.Sin(progB * Mathf.PI) * Mathf.Exp(-tb * 3.2f);

                    float fBase = frecsBase[b];
                    float vibrato = Mathf.Sin(2f * Mathf.PI * 8f * tb) * 16f;
                    float f = fBase + vibrato;

                    float fundamental = Mathf.Sin(2f * Mathf.PI * f * t);
                    float armonico2 = Mathf.Sin(2f * Mathf.PI * (f * 2.02f) * t) * 0.55f;
                    float armonico3 = Mathf.Sin(2f * Mathf.PI * (f * 3.01f) * t) * 0.35f;
                    float formanteGarganta = Mathf.Sin(2f * Mathf.PI * 760f * t) * 0.45f;
                    float respiracionSeca = ((float)rng.NextDouble() * 2f - 1f) * 0.3f;

                    float burst = (fundamental + armonico2 + armonico3 + formanteGarganta + respiracionSeca) * env;
                    mixTotal += burst;
                }
            }

            float reverb = (t > 0.16f) ? Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.15f * Mathf.Exp(-(t - 0.16f) * 1.1f) : 0f;
            mixTotal += reverb;

            muestras[i] = Mathf.Clamp((float)System.Math.Tanh(mixTotal * 1.6f), -0.98f, 0.98f);
        }

        AudioClip clip = AudioClip.Create("Risa_Momia_Victoria_Procedural", totalMuestras, 1, sampleRate, false);
        clip.SetData(muestras, 0);
        return clip;
    }

    private AudioClip GenerarAudioJumpscareTerror()
    {
        int sampleRate = 44100;
        float duracion = 1.95f;
        int totalMuestras = (int)(sampleRate * duracion);
        float[] muestras = new float[totalMuestras];
        System.Random rng = new System.Random(1337);

        for (int i = 0; i < totalMuestras; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duracion;

            float slam = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(115f, 32f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 4.5f) * 0.95f;
            float vibrato = Mathf.Sin(2f * Mathf.PI * 26f * t) * 35f;
            float gritoAgudo = Mathf.Sin(2f * Mathf.PI * (690f + vibrato - prog * 140f) * t) * 0.48f;
            float gritoDisonante = Mathf.Sin(2f * Mathf.PI * (985f - vibrato * 1.3f) * t) * 0.38f;
            float gutural = Mathf.Sin(2f * Mathf.PI * 148f * t) * 0.45f;
            float ruido = ((float)rng.NextDouble() * 2f - 1f) * 0.52f;

            float env = 1f;
            if (prog < 0.03f) env = prog / 0.03f;
            else if (prog > 0.72f) env = Mathf.Clamp01(1f - ((prog - 0.72f) / 0.28f));

            float mix = (slam + (gritoAgudo + gritoDisonante + gutural + ruido) * env);
            muestras[i] = Mathf.Clamp((float)System.Math.Tanh(mix * 1.65f), -0.98f, 0.98f);
        }

        AudioClip clip = AudioClip.Create("Jumpscare_Momia3_Terror", totalMuestras, 1, sampleRate, false);
        clip.SetData(muestras, 0);
        return clip;
    }

    private void CargarTexturasJumpscare()
    {
        if (texMomiaFrente1 != null && texMomiaFrente2 != null) return;

        // 1. Obtener las dos momias procesadas con transparencia limpia y de frente (dir = 0)
        Sprite sp1 = HojasSpritesNivel3.MomiaFrame(0, 0); // Gemela 1 de frente
        Sprite sp2 = HojasSpritesNivel3.MomiaFrame(0, 1); // Gemela 2 de frente (pose de ataque)

        if (sp1 != null) texMomiaFrente1 = sp1.texture;
        if (sp2 != null) texMomiaFrente2 = sp2.texture;

        if (texMomiaFrente2 == null || texMomiaFrente2 == texMomiaFrente1)
        {
            Sprite spAtk = HojasSpritesNivel3.MomiaAtaque();
            if (spAtk != null && spAtk.texture != null) texMomiaFrente2 = spAtk.texture;
        }

        // Respaldo de seguridad garantizado
        if (texMomiaFrente1 == null)
        {
            texMomiaFrente1 = Resources.Load<Texture2D>("Momia/momia_scream_0") ?? Resources.Load<Texture2D>("Momia2/momia2_pose0_scream");
        }
        if (texMomiaFrente2 == null)
        {
            texMomiaFrente2 = Resources.Load<Texture2D>("Momia/momia_scream_1") ?? texMomiaFrente1;
        }
    }

    private void CargarRecursosVictoria()
    {
        if (texMomiaSonriendo == null || texMomiaSonriendo.Length != 6)
        {
            texMomiaSonriendo = new Texture2D[6];
            for (int i = 0; i < 6; i++)
            {
                Texture2D t = Resources.Load<Texture2D>($"Momia/momia_smile_{i}");
                if (t == null)
                {
                    Sprite s = Resources.Load<Sprite>($"Momia/momia_smile_{i}");
                    if (s != null) t = s.texture;
                }
                texMomiaSonriendo[i] = t;
            }
        }

        if (texAuraRadialMomia == null)
        {
            texAuraRadialMomia = Resources.Load<Texture2D>("Momia/tex_aura_radial");
            if (texAuraRadialMomia == null)
            {
                texAuraRadialMomia = Resources.Load<Texture2D>("tex_aura_radial");
            }
        }

        if (clipRisaVictoria == null)
        {
            string[] nombresClips = {
                "Audio_Victoria",
                "dragon-studio-evil-laughter-494314",
                "Audio/Audio_Victoria",
                "Audio/dragon-studio-evil-laughter-494314",
                "Audio/risa",
                "AudioNivel3/risa",
                "risa",
                "Momia/risa"
            };
            foreach (string nom in nombresClips)
            {
                clipRisaVictoria = Resources.Load<AudioClip>(nom);
                if (clipRisaVictoria != null) break;
            }
            if (clipRisaVictoria == null)
            {
                clipRisaVictoria = GenerarRisaMomiaProcedural();
            }
        }
    }

    public void ReproducirRisaVictoria()
    {
        if (risaVictoriaReproducida) return;
        risaVictoriaReproducida = true;

        if (clipRisaVictoria == null)
        {
            CargarRecursosVictoria();
        }

        if (audioVictoria == null)
        {
            audioVictoria = gameObject.AddComponent<AudioSource>();
            audioVictoria.spatialBlend = 0f;
            audioVictoria.playOnAwake = false;
            audioVictoria.loop = false;
            audioVictoria.ignoreListenerPause = true;
            audioVictoria.volume = 1.0f;
        }

        if (clipRisaVictoria != null && audioVictoria != null)
        {
            audioVictoria.clip = clipRisaVictoria;
            audioVictoria.volume = 1.0f;
            audioVictoria.Play();
        }
    }

    private void Start()
    {
        gemelaAcechadora = FindFirstObjectByType<MomiaAcechadoraNivel3>(FindObjectsInactive.Include);
        gemelaCiega = FindFirstObjectByType<MomiaCiegaNivel3>(FindObjectsInactive.Include);
    }

    public void TerminarCinematicaInicio()
    {
        // Activar la cacería de las dos momias
        if (gemelaAcechadora != null) gemelaAcechadora.activa = true;
        if (gemelaCiega != null) gemelaCiega.activa = true;

        MostrarAvisoRuido("¡Las camillas están vacías! Las gemelas andan sueltas en el taller");
    }

    public float ObtenerTiempoNivel()
    {
        return tiempoNivel;
    }

    public int ObtenerMinutoActual()
    {
        float fraccionHora = Mathf.Clamp01(tiempoNivel / DURACION_NIVEL_SEGUNDOS);
        return Mathf.FloorToInt(fraccionHora * 60f);
    }

    private void Update()
    {
        if (jumpscareActivado)
        {
            tiempoJumpscare += Time.unscaledDeltaTime;
            if (tiempoJumpscare >= DURACION_JUMPSCARE && Input.GetKeyDown(KeyCode.R))
            {
                Reintentar();
            }
            return;
        }

        if (NivelCompletado)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                VolverAlMenu();
            }
            return;
        }

        // Monitoreo continuo del nivel de audio en tiempo real
        ActualizarMonitoreoAudio();

        // Tecla ESC: Alternar Menú de Configuración y Monitoreo de Audio (como en Nivel 2)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CCTVMonitorNivel3.Instance != null && CCTVMonitorNivel3.Instance.monitorAbierto)
            {
                CCTVMonitorNivel3.Instance.CerrarMonitor();
            }
            else
            {
                AlternarMenuConfiguracion();
            }
            return;
        }

        // Si el menú de configuración está abierto, detener la simulación del juego y permitir atajos
        if (menuConfiguracionAbierto)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                AlternarMenuConfiguracion();
                Reintentar();
            }
            return;
        }

        if (JuegoPausadoOTerminado) return;

        // Progresión del reloj 2:00 AM -> 3:00 AM
        tiempoNivel += Time.deltaTime;
        float fraccionHora = Mathf.Clamp01(tiempoNivel / DURACION_NIVEL_SEGUNDOS);
        int minutos = Mathf.FloorToInt(fraccionHora * 60f);
        textoReloj = string.Format("02:{0:00} AM", minutos);

        if (tiempoNivel >= DURACION_NIVEL_SEGUNDOS)
        {
            // Tiempo límite alcanzado
            ActivarGameOver("¡Son las 3:00 AM! La noche se cerró y las momias te atraparon en su hora de mayor poder.");
            return;
        }

        // Desvanecimiento de veneno
        if (intensidadVeneno > 0f)
        {
            intensidadVeneno = Mathf.Max(0f, intensidadVeneno - Time.deltaTime * 0.25f);
        }

        // Temporizadores de avisos
        if (timerAvisoRuido > 0f) timerAvisoRuido -= Time.deltaTime;
        if (timerModalNota > 0f) timerModalNota -= Time.deltaTime;
        if (timerBannerDespertar > 0f)
        {
            timerBannerDespertar -= Time.deltaTime;
            if (timerBannerDespertar <= 0f) mostrarBannerDespertar = false;
        }

        // Reset de progreso por frame (los scripts lo actualizan si están interactuando)
        progresoAccion = 0f;
        etiquetaProgreso = "";
        ActualizarPromptsCercanos();

        // Escape automático si la puerta ya está abierta y el jugador la cruza
        if (PuertaEscapeAbierta && !NivelCompletado)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            GameObject puerta = GameObject.Find("PuertaEscapeNivel3");
            if (jugador != null && puerta != null)
            {
                if (Vector2.Distance(jugador.transform.position, puerta.transform.position) < 2.0f || jugador.transform.position.y >= 10.8f)
                {
                    EscaparNivel3();
                    return;
                }
            }
        }
    }

    private void ActualizarPromptsCercanos()
    {
        promptInteraccionTexto = "";

        // Revisar si está cerca de la caja fuerte
        if (KeypadCajaFuerteNivel3.Instance != null && KeypadCajaFuerteNivel3.Instance.EstaCerca())
        {
            promptInteraccionTexto = "[F] Introducir combinación en la Caja Fuerte Acorazada";
            return;
        }

        // Revisar si está cerca de la puerta de escape trasera
        GameObject puerta = GameObject.Find("PuertaEscapeNivel3");
        if (puerta != null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null && Vector2.Distance(puerta.transform.position, jugador.transform.position) < 3.0f)
            {
                if (PuertaEscapeAbierta || TieneLlaveMaestra)
                {
                    promptInteraccionTexto = "¡COMPUERTA ABIERTA! Cruza o pulsa [F] para escapar";
                    if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
                    {
                        EscaparNivel3();
                    }
                }
                else
                {
                    promptInteraccionTexto = "Compuerta de emergencia bloqueada (Introduce la clave en la Caja Fuerte)";
                }
                return;
            }
        }

        // Revisar si está cerca de la consola de CCTV y escritorio
        if (CCTVMonitorNivel3.Instance != null && CCTVMonitorNivel3.Instance.EstaCerca())
        {
            if (!CCTVMonitorNivel3.Instance.monitorAbierto)
            {
                EsconditeNivel3 esc = CCTVMonitorNivel3.Instance.GetComponent<EsconditeNivel3>();
                if (esc != null && esc.EstaOcupado())
                {
                    promptInteraccionTexto = "[E] Salir de abajo del escritorio";
                }
                else
                {
                    promptInteraccionTexto = "[TAB] Monitor CCTV  |  [E] Ocultarse bajo el escritorio";
                }
            }
            return;
        }

        // Revisar pistas primero (prioridad para investigar cifras de la caja fuerte)
        PistasTallerNivel3[] pistas = FindObjectsByType<PistasTallerNivel3>(FindObjectsSortMode.None);
        foreach (var p in pistas)
        {
            if (p != null && p.EstaCerca())
            {
                promptInteraccionTexto = p.ObtenerTextoPrompt();
                return;
            }
        }

        // Revisar si está cerca de un escondite
        EsconditeNivel3[] escondites = FindObjectsByType<EsconditeNivel3>(FindObjectsSortMode.None);
        foreach (var esc in escondites)
        {
            if (esc != null && esc.EstaCerca())
            {
                promptInteraccionTexto = esc.EstaOcupado() ? "[E] Salir del escondite" : "[E] Ocultarse en " + esc.nombreEscondite;
                return;
            }
        }
    }

    public void RegistrarCifra(int indice, int valor, string descripcion)
    {
        if (indice == 1) cifra1 = valor;
        else if (indice == 2) cifra2 = valor;
        else if (indice == 3) cifra3 = valor;

        textoModalNota = descripcion;
        timerModalNota = 4.5f;

        if (cifra1.HasValue && cifra2.HasValue && cifra3.HasValue)
        {
            MostrarAvisoRuido("¡TIENES LAS 3 CIFRAS [ 7 - 3 - 9 ]! Regresa a la caseta a abrir la caja");
        }
    }

    public void AplicarVenenoEscorpion()
    {
        intensidadVeneno = 0.85f;
    }

    public void MostrarAvisoRuido(string texto)
    {
        avisoRuidoTexto = texto;
        timerAvisoRuido = 3.0f;
    }

    public void MostrarLetreroTenebrosoDespertar()
    {
        mostrarBannerDespertar = true;
        timerBannerDespertar = DURACION_BANNER_DESPERTAR;
        if (audioSFX != null)
        {
            AudioClip clipSusto = Resources.Load<AudioClip>("AudioNivel3/come_closer");
            if (clipSusto != null) audioSFX.PlayOneShot(clipSusto, 1.0f);
        }
    }

    public void MostrarProgresoInteraccion(string etiqueta, float porcentaje)
    {
        etiquetaProgreso = etiqueta;
        progresoAccion = porcentaje;
    }

    public void AbrirCajaFuerteExito()
    {
        TieneLlaveMaestra = true;
        PuertaEscapeAbierta = true;
        MostrarAvisoRuido("¡CÓDIGO CORRECTO! COMPUERTA DE ESCAPE ABIERTA — ¡CORRE AFUERA!");

        GameObject puerta = GameObject.Find("PuertaEscapeNivel3");
        if (puerta != null)
        {
            SpriteRenderer psr = puerta.GetComponent<SpriteRenderer>();
            if (psr != null)
            {
                Sprite openSp = Resources.Load<Sprite>("Props/puerta_escape_opened");
                if (openSp == null) openSp = Resources.Load<Sprite>("puerta_escape_opened");
                if (openSp != null) psr.sprite = openSp;
                else psr.color = new Color(0.2f, 1f, 0.45f, 0.95f);
            }

            // Cambiar luz de alarma a verde esmeralda brillante
            Transform tLuz = puerta.transform.Find("LuzAlarmaPuerta");
            if (tLuz != null)
            {
                UnityEngine.Rendering.Universal.Light2D l = tLuz.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
                if (l != null)
                {
                    l.color = new Color(0.15f, 1f, 0.35f);
                    l.intensity = 2.4f;
                    l.pointLightOuterRadius = 6.0f;
                }
            }

            // Desbloquear paso físico
            BoxCollider2D bcol = puerta.GetComponent<BoxCollider2D>();
            if (bcol != null)
            {
                bcol.isTrigger = true;
            }
        }

        if (audioSFX != null)
        {
            AudioClip clipPuerta = Resources.Load<AudioClip>("Props/puerta_abrir");
            if (clipPuerta == null) clipPuerta = Resources.Load<AudioClip>("puerta_abrir");
            if (clipPuerta != null) audioSFX.PlayOneShot(clipPuerta, 1f);
        }

        // Alerta a la Gemela Ciega hacia la caseta
        if (gemelaCiega != null) gemelaCiega.AlertarRuido(new Vector2(0f, 4.5f), "¡Alarma de escape! ¡Las momias corren!");
    }

    public void EscaparNivel3()
    {
        if (NivelCompletado) return;
        NivelCompletado = true;
        JuegoPausadoOTerminado = true;
        Time.timeScale = 0f;

        // Silenciar audios ambientales
        if (audioFondo != null) audioFondo.Stop();
        if (audioReloj != null) audioReloj.Stop();

        CargarRecursosVictoria();
        ReproducirRisaVictoria();
    }

    public void ActivarGameOver(string motivo)
    {
        if (JuegoPausadoOTerminado) return;
        motivoGameOver = motivo;
        JuegoPausadoOTerminado = true;
        jumpscareActivado = true;
        tiempoJumpscare = 0f;

        // Silenciar audios ambientales
        if (audioFondo != null) audioFondo.Stop();
        if (audioReloj != null) audioReloj.Stop();

        CargarTexturasJumpscare();

        // Reproducir sonido de Game Over idéntico a Nivel 2
        if (audioJumpscare != null)
        {
            if (clipGameOver == null)
            {
                clipGameOver = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_PERDER);
                if (clipGameOver == null) clipGameOver = GenerarAudioJumpscareTerror();
            }
            audioJumpscare.PlayOneShot(clipGameOver, 1.0f);
        }

        Time.timeScale = 0f;
    }

    public void Reintentar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Nivel 3");
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    private void OnGUI()
    {
        // 0. Prioridad absoluta: Jumpscare aterrador y Game Over
        if (jumpscareActivado)
        {
            DibujarSecuenciaJumpscareYGameOver();
            return;
        }

        // Prioridad: Modal de Victoria épico y espeluznante
        if (NivelCompletado)
        {
            DibujarModalVictoria();
            return;
        }

        // Prioridad: Modal de Configuración y Monitoreo de Volumen (ESC)
        if (menuConfiguracionAbierto)
        {
            DibujarModalConfiguracionJuego();
            return;
        }

        // Viñeta cinematográfica de terror en los bordes de la pantalla
        DibujarVinetaTerrorPantalla();

        // 1. Efecto visual de veneno (pantalla verdosa parpadeante)
        if (intensidadVeneno > 0.05f)
        {
            Color colVeneno = new Color(0.1f, 0.6f, 0.15f, intensidadVeneno * 0.35f);
            GUI.color = colVeneno;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // 2. HUD Superior: Libreta de Cifras + Reloj + Barra de Vida (Oculto si el monitor CCTV está abierto)
        bool cctvAbierto = CCTVMonitorNivel3.Instance != null && CCTVMonitorNivel3.Instance.monitorAbierto;
        if (!cctvAbierto)
        {
            DibujarHUDSuperior();
        }

        // 3. Banner de Avisos de Ruido
        if (timerAvisoRuido > 0f && !mostrarBannerDespertar)
        {
            float w = 520f;
            float h = 34f;
            Rect r = new Rect((Screen.width - w) * 0.5f, 75f, w, h);
            GUI.Box(r, "");
            GUIStyle alStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.3f, 0.3f) }
            };
            GUI.Label(r, "⚠ " + avisoRuidoTexto + " ⚠", alStyle);
        }

        // 3.1 Banner Tenebroso: Ambas momias despiertas + Escorpiones
        if (mostrarBannerDespertar && timerBannerDespertar > 0f)
        {
            DibujarLetreroTenebrosoDespertar();
        }

        // 4. Modal de Ficha Forense / Nota encontrada
        if (timerModalNota > 0f)
        {
            float nw = 460f;
            float nh = 130f;
            Rect nr = new Rect((Screen.width - nw) * 0.5f, Screen.height - 180f, nw, nh);
            GUI.Box(nr, "");
            GUIStyle notaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 0.8f) }
            };
            GUI.Label(new Rect(nr.x + 15, nr.y + 10, nw - 30, nh - 20), textoModalNota, notaStyle);
        }

        // 5. Barra de progreso al forzar gaveta
        if (progresoAccion > 0f)
        {
            float bw = 320f;
            float bh = 42f;
            Rect br = new Rect((Screen.width - bw) * 0.5f, Screen.height * 0.55f, bw, bh);
            GUI.Box(br, "");
            GUI.Label(new Rect(br.x, br.y + 4, bw, 18), etiquetaProgreso, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

            float barraW = (bw - 20) * progresoAccion;
            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(br.x + 10, br.y + 24, barraW, 10), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // 6. Prompt de interacción en el centro inferior
        if (!string.IsNullOrEmpty(promptInteraccionTexto) && progresoAccion <= 0f)
        {
            GUIStyle pStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
            float pw = Mathf.Min(540f, Screen.width - 30f);
            GUI.Box(new Rect((Screen.width - pw) * 0.5f, Screen.height - 44, pw, 34), promptInteraccionTexto, pStyle);
        }

        // 7. Pie de controles discreto con atajo de configuración (como en Nivel 2)
        GUIStyle controlStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        controlStyle.normal.textColor = new Color(0.70f, 0.74f, 0.80f, 0.85f);
        GUI.Label(new Rect(14f, Screen.height - 18f, Screen.width - 28f, 16f),
            "<color=#888888>WASD: Mover | Shift: Correr | Espacio: Saltar | [TAB] Monitor CCTV | [ESC] Configuración y Audio</color>",
            controlStyle);
    }

    private void DibujarVinetaTerrorPantalla()
    {
        if (texVinetaTerror == null)
        {
            int s = 128;
            texVinetaTerror = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] cols = new Color[s * s];
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), c) / (s * 0.5f);
                    float a = Mathf.Clamp01((dist - 0.42f) / 0.58f);
                    a = a * a;
                    cols[y * s + x] = new Color(0.01f, 0.01f, 0.02f, a * 0.85f);
                }
            }
            texVinetaTerror.SetPixels(cols);
            texVinetaTerror.Apply();
        }

        // Pulso rojizo si alguna momia acecha o está cazando cerca del jugador
        float pulso = 0f;
        Transform pTransform = null;
        if (PlayerController.Instance != null) pTransform = PlayerController.Instance.transform;
        else
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) pTransform = pObj.transform;
        }

        if (pTransform != null)
        {
            float dMin = 999f;
            if (gemelaAcechadora != null && gemelaAcechadora.gameObject.activeInHierarchy)
            {
                dMin = Mathf.Min(dMin, Vector2.Distance(pTransform.position, gemelaAcechadora.transform.position));
            }
            if (gemelaCiega != null && gemelaCiega.gameObject.activeInHierarchy)
            {
                dMin = Mathf.Min(dMin, Vector2.Distance(pTransform.position, gemelaCiega.transform.position));
            }
            if (dMin < 9.0f)
            {
                float cercania = 1.0f - (dMin / 9.0f);
                pulso = Mathf.Sin(Time.time * 5f) * 0.35f * cercania;
            }
        }

        Color prevColor = GUI.color;
        GUI.color = (pulso > 0.05f) ? new Color(1f, 0.15f, 0.15f, 0.90f + pulso) : Color.white;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texVinetaTerror);
        GUI.color = prevColor;
    }

    private void DibujarHUDSuperior()
    {
        float totalW = Screen.width;
        float margen = 14f;
        float gap = 10f;
        float h = 66f;
        float cuadranteW = (totalW - (margen * 2f) - (gap * 2f)) / 3f;

        // Estilo de tarjeta oscura elegante
        Color fondoPanel = new Color(0.04f, 0.06f, 0.08f, 0.88f);
        Color bordePanel = new Color(0.35f, 0.40f, 0.45f, 0.50f);

        // 1. Cuadrante Izquierdo: LIBRETA DE SEGURIDAD (CÓDIGO) Y BARRA DE VIDA
        Rect rIzq = new Rect(margen, 10f, cuadranteW, h);
        GUI.color = bordePanel;
        GUI.DrawTexture(new Rect(rIzq.x - 1, rIzq.y - 1, rIzq.width + 2, rIzq.height + 2), Texture2D.whiteTexture);
        GUI.color = fondoPanel;
        GUI.DrawTexture(rIzq, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.75f, 0.85f, 0.95f) }
        };
        GUI.Label(new Rect(rIzq.x + 10f, rIzq.y + 4f, rIzq.width - 20f, 16f), "LIBRETA DE SEGURIDAD (CAJA)", titStyle);

        string s1 = cifra1.HasValue ? $"<color=#00FFAA>[ {cifra1.Value} ]</color>" : "<color=#888888>[ _ ]</color>";
        string s2 = cifra2.HasValue ? $"<color=#00FFAA>[ {cifra2.Value} ]</color>" : "<color=#888888>[ _ ]</color>";
        string s3 = cifra3.HasValue ? $"<color=#00FFAA>[ {cifra3.Value} ]</color>" : "<color=#888888>[ _ ]</color>";

        GUIStyle codeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.95f, 0.85f, 0.3f) }
        };
        string extraLlave = TieneLlaveMaestra ? " <color=#00FFAA>★ LLAVE ★</color>" : "";
        GUI.Label(new Rect(rIzq.x + 10f, rIzq.y + 22f, rIzq.width - 20f, 20f), $"CÓDIGO: {s1} {s2} {s3}{extraLlave}", codeStyle);

        // Barra de Vida
        float vida = PlayerController.Instance != null ? PlayerController.Instance.vidaActual : 150f;
        float maxVida = PlayerController.Instance != null ? PlayerController.Instance.vidaMaxima : 150f;
        float pctVida = Mathf.Clamp01(vida / maxVida);

        GUIStyle hpTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
        };
        GUI.Label(new Rect(rIzq.x + 10f, rIzq.y + 44f, 85f, 16f), $"❤️ {Mathf.CeilToInt(vida)}/{Mathf.CeilToInt(maxVida)} HP", hpTextStyle);

        float barX = rIzq.x + 95f;
        float barW = Mathf.Max(30f, rIzq.width - 105f);
        GUI.color = new Color(0.12f, 0.12f, 0.14f, 0.9f);
        GUI.DrawTexture(new Rect(barX, rIzq.y + 47f, barW, 10f), Texture2D.whiteTexture);

        Color colSalud = pctVida > 0.5f ? new Color(0.2f, 0.9f, 0.3f) : (pctVida > 0.25f ? new Color(0.95f, 0.75f, 0.1f) : new Color(0.95f, 0.2f, 0.2f));
        GUI.color = colSalud;
        GUI.DrawTexture(new Rect(barX, rIzq.y + 47f, barW * pctVida, 10f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. Cuadrante Central: RELOJ Y MONITOR PORTÁTIL
        Rect rCen = new Rect(margen + cuadranteW + gap, 10f, cuadranteW, h);
        GUI.color = bordePanel;
        GUI.DrawTexture(new Rect(rCen.x - 1, rCen.y - 1, rCen.width + 2, rCen.height + 2), Texture2D.whiteTexture);
        GUI.color = fondoPanel;
        GUI.DrawTexture(rCen, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle clockStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.35f, 0.35f) }
        };
        GUI.Label(new Rect(rCen.x, rCen.y + 4f, rCen.width, 24f), "⏰ " + textoReloj, clockStyle);

        GUIStyle subClock = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.35f, 0.90f, 0.75f) }
        };
        GUI.Label(new Rect(rCen.x, rCen.y + 32f, rCen.width, 18f), "CÁMARAS EN VIVO: [TAB]", subClock);

        // 3. Cuadrante Derecho: RADAR DE AMENAZAS
        Rect rDer = new Rect(margen + (cuadranteW * 2f) + (gap * 2f), 10f, cuadranteW, h);
        GUI.color = bordePanel;
        GUI.DrawTexture(new Rect(rDer.x - 1, rDer.y - 1, rDer.width + 2, rDer.height + 2), Texture2D.whiteTexture);
        GUI.color = fondoPanel;
        GUI.DrawTexture(rDer, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(rDer.x + 10f, rDer.y + 4f, rDer.width - 20f, 16f), "RADAR DE LAS HERMANAS", titStyle);

        bool g1Despierta = ControladorDespertarMomiasNivel3.Instance != null ? ControladorDespertarMomiasNivel3.Instance.gemela1Despierta : (gemelaAcechadora != null && gemelaAcechadora.activa);
        bool g2Despierta = ControladorDespertarMomiasNivel3.Instance != null ? ControladorDespertarMomiasNivel3.Instance.gemela2Despierta : (gemelaCiega != null && gemelaCiega.activa);

        string estadoG1 = !g1Despierta 
            ? "<color=#888888>G1 (Acecho): Acostada en camilla</color>"
            : (gemelaAcechadora != null && gemelaAcechadora.estaCongelada) 
                ? "<color=#55DDFF>G1 (Acecho): ¡CONGELADA!</color>" 
                : "<color=#FFAA33>G1 (Acecho): Acechando en sombras</color>";
            
        string estadoG2 = !g2Despierta 
            ? "<color=#888888>G2 (Ciega): Acostada en camilla</color>"
            : (gemelaCiega != null && gemelaCiega.estadoActual == MomiaCiegaNivel3.EstadoCiega.Cazando) 
                ? "<color=#FF2222><b>G2 (Ciega): ¡CAZANDO TUS PASOS!</b></color>" 
                : "<color=#88CC88>G2 (Ciega): Patrulla en silencio</color>";

        GUIStyle amStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        GUI.Label(new Rect(rDer.x + 10f, rDer.y + 22f, rDer.width - 20f, 16f), estadoG1, amStyle);
        GUI.Label(new Rect(rDer.x + 10f, rDer.y + 38f, rDer.width - 20f, 16f), estadoG2, amStyle);
    }

    private void DibujarLetreroTenebrosoDespertar()
    {
        float w = Mathf.Min(860f, Screen.width - 40f);
        float h = 105f;
        float x = (Screen.width - w) * 0.5f;
        float y = 85f;

        float pulso = Mathf.PingPong(Time.time * 4f, 1f);
        float alpha = Mathf.Clamp01(timerBannerDespertar / 0.8f);

        // Fondo oscuro con tinte de sangre
        Color colorFondo = new Color(0.12f + pulso * 0.15f, 0.02f, 0.02f, 0.92f * alpha);
        GUI.color = colorFondo;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // Marco ensangrentado parpadeante
        Color colorBorde = new Color(1f, 0.15f + pulso * 0.25f, 0.15f, 0.95f * alpha);
        GUI.color = colorBorde;
        GUI.DrawTexture(new Rect(x, y, w, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - 3f, w, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 3f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - 3f, y, 3f, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle styleTitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.25f + pulso * 0.2f, 0.2f) }
        };
        GUI.Label(new Rect(x + 10f, y + 8f, w - 20f, 28f), "☠ ¡LAS MOMIAS GEMELAS ACECHAN JUNTO CON LOS ESCORPIONES! ☠", styleTitulo);

        GUIStyle styleSub = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.9f, 0.25f) }
        };
        GUI.Label(new Rect(x + 10f, y + 40f, w - 20f, 26f), "¡DATE PRISA Y ENCUENTRA LA CLAVE DE LA CAJA FUERTE PARA ESCAPAR!", styleSub);

        GUIStyle stylePista = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.88f, 0.88f, 0.88f) }
        };
        GUI.Label(new Rect(x + 10f, y + 72f, w - 20f, 20f), "Camilla Central: Cifra 1 | Lab de Reactivos: Cifra 2 | Gavetas de Morgue: Cifra 3", stylePista);
    }

    private void DibujarSecuenciaJumpscareYGameOver()
    {
        // FASE 1 (0 a 1.95s): Jumpscare violento con AMBAS momias de frente abalanzándose hacia la cámara
        if (tiempoJumpscare < DURACION_JUMPSCARE)
        {
            float t = tiempoJumpscare / DURACION_JUMPSCARE;
            bool flashSangre = Mathf.Sin(tiempoJumpscare * 60f) > 0.25f;

            Color colorFondo = flashSangre
                ? new Color(0.42f, 0.02f, 0.02f, 0.98f)
                : new Color(0.03f, 0.01f, 0.01f, 0.98f);

            Color prevColor = GUI.color;
            GUI.color = colorFondo;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            if (texMomiaFrente1 == null || texMomiaFrente2 == null)
            {
                CargarTexturasJumpscare();
            }

            // Progreso de embestida con curva de aceleración agresiva
            float progresoEmbestida = Mathf.Clamp01(t * 1.55f);
            float curvaImpacto = 1f - Mathf.Pow(1f - progresoEmbestida, 3.5f);

            // Escala frontal impactante: de 0.85x a 2.45x
            float multiplicadorAlto = Mathf.Lerp(0.85f, 2.45f, curvaImpacto);
            float altoImg = Screen.height * multiplicadorAlto;

            float intensidadShake = Mathf.Lerp(45f, 15f, t);
            float shakeX = Mathf.Sin(tiempoJumpscare * 95f) * intensidadShake + Random.Range(-12f, 12f);
            float shakeY = Mathf.Cos(tiempoJumpscare * 110f) * intensidadShake + Random.Range(-12f, 12f);
            float focoRostroY = Mathf.Lerp((Screen.height - altoImg) * 0.5f, -altoImg * 0.08f, curvaImpacto);

            if (flashSangre && Random.value > 0.45f)
            {
                GUI.color = new Color(1f, 0.75f, 0.70f, 1f);
            }

            // Separación de las dos momias: convergen desde los lados hacia el centro
            float separacionX = Mathf.Lerp(Screen.width * 0.24f, Screen.width * 0.16f, curvaImpacto);

            // 1. GEMELA 1 (DE FRENTE - IZQUIERDA)
            if (texMomiaFrente1 != null)
            {
                float aspect1 = (float)texMomiaFrente1.width / Mathf.Max(1f, texMomiaFrente1.height);
                float ancho1 = altoImg * aspect1;
                Rect rectG1 = new Rect(
                    (Screen.width * 0.5f) - separacionX - (ancho1 * 0.5f) + shakeX,
                    focoRostroY + shakeY + Mathf.Sin(tiempoJumpscare * 45f) * 8f,
                    ancho1,
                    altoImg
                );
                GUI.DrawTexture(rectG1, texMomiaFrente1, ScaleMode.ScaleToFit, true);
            }

            // 2. GEMELA 2 (DE FRENTE - DERECHA - ATACANDO CON GARRAS)
            Texture2D texG2 = texMomiaFrente2 ?? texMomiaFrente1;
            if (texG2 != null)
            {
                float aspect2 = (float)texG2.width / Mathf.Max(1f, texG2.height);
                float ancho2 = altoImg * aspect2;
                Rect rectG2 = new Rect(
                    (Screen.width * 0.5f) + separacionX - (ancho2 * 0.5f) - shakeX * 0.7f,
                    focoRostroY + shakeY - Mathf.Cos(tiempoJumpscare * 45f) * 8f,
                    ancho2,
                    altoImg
                );
                GUI.DrawTexture(rectG2, texG2, ScaleMode.ScaleToFit, true);
            }

            GUI.color = prevColor;

            // Bordes ensangrentados pulsantes
            Color bordeSangre = new Color(0.85f, 0.05f, 0.05f, 0.65f);
            GUI.color = bordeSangre;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, 36f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - 36f, Screen.width, 36f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, 36f, Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - 36f, 0, 36f, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;
            return;
        }

        // FASE 2: Pantalla Game Over estilizada y aterradora
        DibujarModalGameOver();
    }

    private void DibujarModalVictoria()
    {
        CargarRecursosVictoria();
        ReproducirRisaVictoria();

        // Fondo de terror abisal profundo
        GUI.color = new Color(0.01f, 0.02f, 0.03f, 0.97f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        float w = Mathf.Min(500f, Screen.width - 24f);
        float h = Mathf.Min(400f, Screen.height - 24f);
        Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        float pulso = Mathf.PingPong(Time.unscaledTime * 3f, 1f);

        // Borde dorado ancestral maldito
        Color bordeVictoria = Color.Lerp(new Color(0.85f, 0.72f, 0.20f), new Color(0.95f, 0.35f, 0.15f), pulso);
        GUI.color = bordeVictoria;
        GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Texture2D.whiteTexture);

        // Fondo del panel oscuro
        GUI.color = new Color(0.05f, 0.06f, 0.08f, 0.99f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Título Glorioso y Espeluznante
        GUIStyle vTit = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = new Color(1f, 0.88f, 0.30f) }
        };
        GUI.Label(new Rect(r.x + 10, r.y + 12, w - 20, 24), "★ ¡FELICIDADES! HAS AVANZADO AL ÚLTIMO NIVEL ★", vTit);

        GUIStyle vSub = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = new Color(0.35f, 0.95f, 0.65f) }
        };
        GUI.Label(new Rect(r.x + 10, r.y + 36, w - 20, 18), "⏰ 03:00 AM — ESCAPASTE CON VIDA DEL TALLER FORENSE", vSub);

        // Separador decorativo superior
        GUI.color = new Color(0.75f, 0.65f, 0.25f, 0.55f);
        GUI.DrawTexture(new Rect(r.x + 25, r.y + 56, w - 50, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Mensaje breve centrado
        GUIStyle vBody = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            wordWrap = true,
            normal = { textColor = new Color(0.92f, 0.92f, 0.92f) }
        };
        string txtBreve = "¡Burlaste a Las Hermanas Gemelas y sobreviviste!\n" +
                          "<color=#FF2222><b>EL NIVEL 4 SERÁ EL MÁS DIFÍCIL: CRIPTA ANCESTRAL</b></color>";
        GUI.Label(new Rect(r.x + 15, r.y + 62, w - 30, 36), txtBreve, vBody);

        // =========================================================================
        // MOMIA RIÉNDOSE EN EL CENTRO
        // =========================================================================
        float mummyY = r.y + 104f;
        float mw = 120f;
        float mh = 140f;

        if (texMomiaSonriendo != null && texMomiaSonriendo.Length > 0)
        {
            int[] smileSeq = new int[] { 0, 1, 2, 1, 0, 4, 0, 3, 0, 1 };
            int frameIdx = smileSeq[(int)(Time.unscaledTime * 5f) % smileSeq.Length];
            Texture2D texActualRisa = texMomiaSonriendo[Mathf.Clamp(frameIdx, 0, texMomiaSonriendo.Length - 1)];

            if (texActualRisa != null)
            {
                float swayX = Mathf.Sin(Time.unscaledTime * 3.5f) * 3f;
                float swayY = Mathf.Cos(Time.unscaledTime * 2.8f) * 2f;
                float pulseMummy = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 5f);

                mw *= pulseMummy;
                mh *= pulseMummy;
                Rect mummyRect = new Rect(r.x + (w - mw) * 0.5f + swayX, mummyY + swayY, mw, mh);

                // Aura resplandeciente siniestra de la momia riéndose
                Texture2D aura = texAuraRadialMomia ?? Texture2D.whiteTexture;
                float auraAlpha = 0.45f + 0.25f * Mathf.Sin(Time.unscaledTime * 4.5f);
                GUI.color = new Color(1f, 0.75f, 0.2f, auraAlpha);
                float auraPad = 32f;
                GUI.DrawTexture(new Rect(mummyRect.x - auraPad * 0.5f, mummyRect.y - auraPad * 0.5f, mw + auraPad, mh + auraPad), aura);
                GUI.color = Color.white;

                // Dibujar momia riéndose centrada
                GUI.DrawTexture(mummyRect, texActualRisa, ScaleMode.ScaleToFit);
            }
        }

        // Frases tenebrosas debajo de la momia riéndose
        GUIStyle quoteStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = new Color(1f, 0.84f, 0.35f) }
        };
        GUI.Label(new Rect(r.x + 10, mummyY + mh + 6f, w - 20, 18f), "« ¿Podrás sobrevivir a la última pesadilla? »", quoteStyle);

        GUIStyle subQuoteStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = new Color(1f, 0.45f, 0.40f) }
        };
        GUI.Label(new Rect(r.x + 10, mummyY + mh + 26f, w - 20, 18f), "Sin cámaras ni caseta... El Patriarca Ancestral te aguarda.", subQuoteStyle);

        // Separador decorativo inferior
        GUI.color = new Color(0.75f, 0.65f, 0.25f, 0.55f);
        GUI.DrawTexture(new Rect(r.x + 25, r.y + h - 56f, w - 50, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Botones inferiores centrados
        float btnW = 205f;
        float btnH = 36f;
        float btnY = r.y + h - 46f;
        float totalBtnsW = btnW * 2f + 14f;
        float startBtnX = r.x + (w - totalBtnsW) * 0.5f;

        if (GUI.Button(new Rect(startBtnX, btnY, btnW, btnH), "ENFRENTAR NIVEL 4 (FINAL) ▶"))
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded("Nivel 4"))
            {
                SceneManager.LoadScene("Nivel 4");
            }
            else
            {
                SceneManager.LoadScene("MenuPrincipal");
            }
        }

        if (GUI.Button(new Rect(startBtnX + btnW + 14f, btnY, btnW, btnH), "MENÚ PRINCIPAL"))
        {
            VolverAlMenu();
        }
    }

    private void DibujarModalGameOver()
    {
        // Fondo oscuro con viñeta de sangre
        GUI.color = new Color(0.04f, 0.01f, 0.01f, 0.96f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        float w = 560f;
        float h = 290f;
        Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        // Marco ensangrentado
        GUI.color = new Color(0.85f, 0.15f, 0.15f, 0.95f);
        GUI.DrawTexture(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Texture2D.whiteTexture);
        GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.98f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle goTit = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.18f, 0.18f) }
        };
        GUI.Label(new Rect(r.x + 20, r.y + 16, w - 40, 38), "☠ GAME OVER ☠", goTit);

        GUIStyle goBody = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.90f, 0.90f) }
        };
        GUI.Label(new Rect(r.x + 25, r.y + 60, w - 50, 65), motivoGameOver, goBody);

        GUIStyle goPistas = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0.45f) }
        };
        GUI.Label(new Rect(r.x + 25, r.y + 130, w - 50, 60),
            "💡 Pistas de Supervivencia:\n• La Gemela Acechadora se congela si la miras de frente.\n• La Gemela Ciega no ve pero oye tus pasos si corres.\n• Ten cuidado de no pisar a los escorpiones venenosos.", goPistas);

        float btnW = 195f;
        float btnH = 44f;
        if (GUI.Button(new Rect(r.x + 45, r.y + 215, btnW, btnH), "REINTENTAR [ R ]"))
        {
            Reintentar();
        }
        if (GUI.Button(new Rect(r.x + w - 45 - btnW, r.y + 215, btnW, btnH), "MENÚ PRINCIPAL"))
        {
            VolverAlMenu();
        }
    }

    /// <summary>
    /// Alterna el menú de configuración y pausa/reanuda la partida con [ESC].
    /// </summary>
    public void AlternarMenuConfiguracion()
    {
        menuConfiguracionAbierto = !menuConfiguracionAbierto;
        Time.timeScale = menuConfiguracionAbierto ? 0f : 1f;
    }

    /// <summary>
    /// Monitorea continuamente la salida de audio del juego para calcular el volumen RMS y dB.
    /// </summary>
    private void ActualizarMonitoreoAudio()
    {
        try
        {
            AudioListener.GetOutputData(muestrasAudio, 0);
            float sum = 0f;
            for (int i = 0; i < muestrasAudio.Length; i++)
            {
                sum += muestrasAudio[i] * muestrasAudio[i];
            }
            float rms = Mathf.Sqrt(sum / muestrasAudio.Length);
            nivelVolumenRMS = Mathf.Clamp01(rms * 4.5f);
            decibeliosMonitor = rms > 0.0001f ? Mathf.Max(-60f, 20f * Mathf.Log10(rms)) : -60f;
        }
        catch
        {
            nivelVolumenRMS = 0f;
            decibeliosMonitor = -60f;
        }
    }

    /// <summary>
    /// Menú de Configuración y Monitoreo de Audio (accesible con ESC, idéntico al Nivel 2).
    /// Permite calibrar el volumen del juego, ambiente y SFX, además de monitorear el nivel acústico.
    /// </summary>
    private void DibujarModalConfiguracionJuego()
    {
        // Fondo oscuro semitransparente que cubre la pantalla
        DibujarRectanguloColor(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.82f));

        float w = 480f;
        float h = 330f;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        // Caja principal con marco dorado
        DibujarCajaConBorde(new Rect(x, y, w, h),
            new Color(0.06f, 0.07f, 0.10f, 0.98f),
            new Color(0.85f, 0.72f, 0.30f, 0.95f));

        GUIStyle estiloTitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        GUI.Label(new Rect(x + 20f, y + 14f, w - 40f, 30f),
            "<color=#FFD700>⚙ CONFIGURACIÓN Y AUDIO</color>", estiloTitulo);

        GUIStyle estiloLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        estiloLabel.normal.textColor = new Color(0.90f, 0.92f, 0.95f);

        GUIStyle estiloBoton = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        // 1. Slider: Volumen General del Juego
        float volActual = AudioListener.volume;
        GUI.Label(new Rect(x + 28f, y + 54f, 200f, 20f),
            "Volumen General: <b>" + Mathf.RoundToInt(volActual * 100f) + "%</b>", estiloLabel);
        float nuevoVol = GUI.HorizontalSlider(new Rect(x + 225f, y + 58f, 220f, 18f), volActual, 0f, 1f);
        if (Mathf.Abs(nuevoVol - volActual) > 0.001f)
        {
            volumenGeneral = nuevoVol;
            AudioListener.volume = nuevoVol;
            PlayerPrefs.SetFloat("VolumenGeneral", nuevoVol);
            PlayerPrefs.SetFloat("VolumenMusica", nuevoVol);
            PlayerPrefs.Save();
        }

        // 2. Slider: Volumen de Música y Ambiente
        GUI.Label(new Rect(x + 28f, y + 84f, 200f, 20f),
            "Música y Reloj: <b>" + Mathf.RoundToInt(volumenMusica * 100f) + "%</b>", estiloLabel);
        float nuevoMus = GUI.HorizontalSlider(new Rect(x + 225f, y + 88f, 220f, 18f), volumenMusica, 0f, 1f);
        if (Mathf.Abs(nuevoMus - volumenMusica) > 0.001f)
        {
            volumenMusica = nuevoMus;
            if (audioFondo != null) audioFondo.volume = volumenMusica * 0.55f;
            if (audioReloj != null) audioReloj.volume = volumenMusica * 0.65f;
            PlayerPrefs.SetFloat("VolumenAmbienteNivel3", volumenMusica);
            PlayerPrefs.Save();
        }

        // 3. Slider: Efectos de Sonido (SFX y Sustos)
        GUI.Label(new Rect(x + 28f, y + 114f, 200f, 20f),
            "Efectos SFX / Sustos: <b>" + Mathf.RoundToInt(volumenSFX * 100f) + "%</b>", estiloLabel);
        float nuevoSfx = GUI.HorizontalSlider(new Rect(x + 225f, y + 118f, 220f, 18f), volumenSFX, 0f, 1f);
        if (Mathf.Abs(nuevoSfx - volumenSFX) > 0.001f)
        {
            volumenSFX = nuevoSfx;
            if (audioSFX != null) audioSFX.volume = volumenSFX;
            if (audioJumpscare != null) audioJumpscare.volume = volumenSFX;
            if (audioVictoria != null) audioVictoria.volume = volumenSFX;
            PlayerPrefs.SetFloat("VolumenSFXNivel3", volumenSFX);
            PlayerPrefs.Save();
        }

        // 4. Barra de Monitoreo de Volumen en Tiempo Real (VU Meter estilo Nivel 2)
        GUI.Label(new Rect(x + 28f, y + 148f, w - 56f, 18f),
            string.Format("Monitor Acústico en Vivo: <color=#00FFAA>{0:0}%</color>  <color=#AAAAAA>({1:0} dB)</color>",
            nivelVolumenRMS * 100f, decibeliosMonitor), estiloLabel);

        float barraX = x + 28f;
        float barraY = y + 170f;
        float barraW = w - 56f;
        float barraH = 18f;

        // Fondo de la barra
        DibujarRectanguloColor(new Rect(barraX, barraY, barraW, barraH), new Color(0.12f, 0.14f, 0.18f, 0.95f));

        // Nivel activo con gradiente de color según intensidad
        Color colorVU;
        if (nivelVolumenRMS >= 0.75f)
            colorVU = new Color(0.95f, 0.20f, 0.18f, 0.95f); // Rojo peligro
        else if (nivelVolumenRMS >= 0.40f)
            colorVU = new Color(0.95f, 0.75f, 0.20f, 0.95f); // Amarillo moderado
        else
            colorVU = new Color(0.22f, 0.88f, 0.45f, 0.95f); // Verde óptimo

        float llenadoW = Mathf.Clamp(barraW * nivelVolumenRMS, 0f, barraW);
        DibujarRectanguloColor(new Rect(barraX + 2f, barraY + 2f, llenadoW, barraH - 4f), colorVU);

        // Si existe detector de micrófono en el proyecto, mostrar su estado
        if (DetectorMicrofonoNivel2.Instance != null)
        {
            var mic = DetectorMicrofonoNivel2.Instance;
            GUI.Label(new Rect(x + 28f, y + 196f, w - 56f, 18f),
                "Micrófono Acústico: <color=#00FFAA>" + mic.NombreDispositivo + "</color> (Sensibilidad: " + Mathf.RoundToInt(mic.sensibilidad * 100f) + "%)", estiloLabel);
        }
        else
        {
            GUI.Label(new Rect(x + 28f, y + 196f, w - 56f, 18f),
                "Audio del Taller: <color=#88CCFF>Monitoreo Máster Activo</color> (Sonido ambiental y pasos)", estiloLabel);
        }

        // Botones de acción inferiores
        if (GUI.Button(new Rect(x + 28f, y + 260f, 130f, 42f), "CONTINUAR [ESC]", estiloBoton))
        {
            AlternarMenuConfiguracion();
        }

        if (GUI.Button(new Rect(x + 168f, y + 260f, 140f, 42f), "REINTENTAR [R]", estiloBoton))
        {
            AlternarMenuConfiguracion();
            Reintentar();
        }

        if (GUI.Button(new Rect(x + 318f, y + 260f, 134f, 42f), "MENÚ PRINCIPAL", estiloBoton))
        {
            Time.timeScale = 1f;
            VolverAlMenu();
        }
    }

    private void DibujarCajaConBorde(Rect rect, Color colorFondo, Color colorBorde)
    {
        DibujarRectanguloColor(new Rect(rect.x - 1.5f, rect.y - 1.5f, rect.width + 3f, rect.height + 3f), colorBorde);
        DibujarRectanguloColor(rect, colorFondo);
    }

    private void DibujarRectanguloColor(Rect rect, Color color)
    {
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
