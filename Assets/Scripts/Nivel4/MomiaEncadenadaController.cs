using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controlador de IA de Carmen "La Bruja" / La Momia Encadenada (Panteón de Santa Paula).
/// - Escala más alta e imponente (1.58x) respetando 100% su diseño original.
/// - Aparición natural emergiendo gradualmente desde la tierra y la bruma con sombra en los pies,
///   polvo, chispas de cadenas y encendido progresivo de sus ojos cataratosos.
/// - Animación de caminata corregida por orientación real de cada sprite (sin giros invertidos ni inclinación artificial).
/// - Más rápida y agresiva, con arrastre metálico de cadenas y ataque animado lanzando las cadenas (13 frames).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MomiaEncadenadaController : MonoBehaviour
{
    public static MomiaEncadenadaController Instance { get; private set; }

    public enum EstadoMomia4
    {
        EsperandoAparicion,
        Emergiendo,
        Patrullando,
        AcechandoBajo,
        GritoParalizante,
        InvestigandoRuido,
        PersecucionFrenetica,
        LanzandoCadenas,
        InspeccionandoEscondite,
        AtacandoJumpscare,
        Petrificada
    }

    private enum FacingNativo
    {
        Izquierda,
        Derecha,
        Simetrico
    }

    [Header("Control General y Escala")]
    public bool momiaDetenida = false;
    public float escalaBase = 1.58f; // Más alta e imponente sobre la vigilante sin deformar el diseño

    [Header("Estado y Velocidades (Más Rápida y Agresiva)")]
    public EstadoMomia4 estadoActual = EstadoMomia4.EsperandoAparicion;
    public float velocidadPatrulla = 2.60f;
    public float velocidadAcecho = 3.40f;
    public float velocidadPersecucion = 5.10f; // Rápida y agresiva (Vigilante corre a 6.2f)

    [Header("Sentidos y Alcance de Cadenas")]
    public float radioVision = 9.8f;
    public float radioOidoSigilo = 3.2f;
    public float radioOidoNormal = 8.4f;
    public float radioOidoCorriendo = 17.5f;
    public float rangoInicioLatigazo = 4.35f; // Ajustado a la altura 1.58x
    public float alcanceGolpeCadena = 4.75f;  // Alcance real de la cadena lanzada

    [Header("Aparición Natural")]
    public float distanciaActivacionAparicion = 14.5f;
    public float tiempoMaximoEsperaAparicion = 14.0f;

    // Audio
    private AudioSource audioCadenas;
    private AudioSource audioCaminataMomia;
    private AudioSource audioVoz;
    private AudioClip clipCadenas;
    private AudioClip clipCaminataMomia;
    private AudioClip clipDeteccion;
    private AudioClip clipLatigazoCadena;

    // Componentes y referencias
    private Rigidbody2D rb;
    private BoxCollider2D colFisico;
    private SpriteRenderer sr;
    private Transform playerTransform;

    // Ojos fosforescentes y sombra en el suelo
    private GameObject objOjosFosforescentes;
    private Light2D luzOjos;
    private SpriteRenderer srSombraPies;

    // Sprites cargados de Resources/Momia4
    private Sprite spIdle0, spIdle1, spSide, spBack;
    private Sprite spWalk0, spWalk1, spWalk2, spWalk3, spWalkDrag, spWalkFrontal;
    private Sprite spCrawl0, spCrawl1, spCrawl2;
    private Sprite spScream, spStoop;
    private Sprite[] spWhipFrames;

    // Puntos de patrulla
    private Vector2[] puntosPatrulla;
    private int indicePatrulla = 0;
    private Vector2 destinoActual;
    private Vector2 posicionUltimoRuido;

    private float timerEstado = 0f;
    private float timerAnimacion = 0f;
    private int frameAnim = 0;
    private float timerGritoCooldown = 14f;
    private float timerEsperaAparicion = 0f;
    private float progresoEmergencia = 0f;

    // Orientación visual actual
    private bool mirandoIzquierda = false;
    private Vector2 ultimaVel = Vector2.right;

    // Control de lanzamiento de cadenas
    private int frameWhip = 0;
    private float timerWhipFrame = 0f;
    private bool whipImpactoEjecutado = false;
    private Vector2 dirWhip = Vector2.right;

    // Chispas y polvo en el suelo
    private Sprite spChispa;
    private float timerChispa = 0f;

    private Vector2 lastPosCheck;
    private float timerAtascada = 0f;
    private float timerCheckStuck = 0f;
    private float timerCooldownAtaque = 0f;

    // Control de repulsión sagrada (Cirio Bendito) para alejarse y seguir deambulando sin trabarse
    private float timerRepelidaFuego = 0f;
    private Vector2 centroFuegoRepulsor = Vector2.zero;
    private float radioFuegoRepulsor = 5.8f;
    private CirioBenditoNivel4[] ciriosEscena;
    public bool EstaRepelidaPorFuego => timerRepelidaFuego > 0f;

    // Control de inspección breve cuando la jugadora entra a una lápida/cripta o habitación (mira y se aleja)
    private bool observandoEscondite = false;
    private float timerAcercamientoEscondite = 0f;
    private float timerIgnorarEscondite = 0f;
    private float timerPermanenciaHabitacion = 0f;
    private Vector2 posEsconditeInspeccion = Vector2.zero;

    // Navegación suave y deslizamiento por paredes (sin perder la armonía de persecución ni atascarse)
    private float timerDesvioObstaculo = 0f;
    private Vector2 dirDesvioObstaculo = Vector2.zero;
    private int ladoDesvioPreferido = 0;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        colFisico = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();

        transform.localScale = new Vector3(escalaBase, escalaBase, 1f);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Colisionador compacto y redondeado solo en los pies para deslizarse entre lápidas y muros sin engancharse
        colFisico.size = new Vector2(0.34f, 0.28f);
        colFisico.offset = new Vector2(0f, -0.52f);
        colFisico.edgeRadius = 0.06f;
        colFisico.isTrigger = false;
        colFisico.enabled = false; // Oculta bajo tierra hasta emerger de forma natural

        ConfigurarAudios();
        CargarSprites();
        InicializarPuntosPatrulla();
        ConfigurarOjosFosforescentes();
        CrearSpriteChispa();
        ConfigurarSombraPies();

        // Inicia oculta en la tierra/bruma antes de emerger naturalmente
        estadoActual = EstadoMomia4.EsperandoAparicion;
        if (sr != null)
        {
            sr.color = new Color(0.95f, 0.94f, 0.90f, 0f);
        }
        if (luzOjos != null)
        {
            luzOjos.enabled = false;
            luzOjos.intensity = 0f;
        }
    }

    private void Start()
    {
        if (VigilanteNivel4.Instance != null)
        {
            playerTransform = VigilanteNivel4.Instance.transform;
        }
        ciriosEscena = FindObjectsByType<CirioBenditoNivel4>(FindObjectsSortMode.None);
    }

    private void ConfigurarAudios()
    {
        audioCadenas = gameObject.AddComponent<AudioSource>();
        audioCadenas.loop = true;
        audioCadenas.spatialBlend = 0f;
        audioCadenas.playOnAwake = false;

        audioCaminataMomia = gameObject.AddComponent<AudioSource>();
        audioCaminataMomia.loop = true;
        audioCaminataMomia.spatialBlend = 0f;
        audioCaminataMomia.playOnAwake = false;
        audioCaminataMomia.volume = 0f;

        audioVoz = gameObject.AddComponent<AudioSource>();
        audioVoz.loop = false;
        audioVoz.spatialBlend = 0f;

        clipCadenas = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CADENAS);
        clipCaminataMomia = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMINATA_MOMIA);
        clipDeteccion = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_DETECCION_MOMIA4);
        if (clipDeteccion == null && AudioManagerNivel4.Instance != null)
        {
            AudioManagerNivel4.Instance.CargarClipExternoSiFalta(
                AudioManagerNivel4.CLIP_DETECCION_MOMIA4,
                @"C:\Users\User\Downloads\deteccion.mp3",
                c => { if (c != null) clipDeteccion = c; });
        }
        clipLatigazoCadena = SintetizarLatigazoCadena();

        if (clipCadenas != null)
        {
            audioCadenas.clip = clipCadenas;
            audioCadenas.volume = 0f;
            audioCadenas.Play();
        }
        if (clipCaminataMomia != null)
        {
            audioCaminataMomia.clip = clipCaminataMomia;
        }
    }

    /// <summary>
    /// Síntesis procedural de latigazo y azote metálico de cadenas pesadas al lanzarse.
    /// </summary>
    private static AudioClip SintetizarLatigazoCadena()
    {
        int sampleRate = 22050;
        float duracion = 0.65f;
        int total = Mathf.CeilToInt(sampleRate * duracion);
        float[] samples = new float[total];
        System.Random rng = new System.Random(1865);

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float envWhoosh = Mathf.Exp(-t * 6.5f) * Mathf.Clamp01(t / 0.04f);
            float white = (float)rng.NextDouble() * 2f - 1f;

            float whoosh = white * envWhoosh * 0.45f;
            float dtCrack = Mathf.Max(0f, t - 0.14f);
            float envCrack = (t >= 0.14f) ? Mathf.Exp(-dtCrack * 16f) : 0f;

            float metal1 = Mathf.Sin(2f * Mathf.PI * 1950f * dtCrack) * 0.45f;
            float metal2 = Mathf.Sin(2f * Mathf.PI * 2840f * dtCrack) * 0.35f;
            float metal3 = Mathf.Sin(2f * Mathf.PI * 820f * dtCrack) * 0.40f;
            float impacto = (metal1 + metal2 + metal3 + white * 0.35f) * envCrack;

            samples[i] = Mathf.Clamp((whoosh + impacto) * 0.9f, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("LatigazoCadena_Procedural", total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void CargarSprites()
    {
        spIdle0 = Resources.Load<Sprite>("Momia4/momia4_idle_0");
        spIdle1 = Resources.Load<Sprite>("Momia4/momia4_idle_1");
        spSide = Resources.Load<Sprite>("Momia4/momia4_idle_side");
        spBack = Resources.Load<Sprite>("Momia4/momia4_idle_back");

        spWalk0 = Resources.Load<Sprite>("Momia4/momia4_walk_0");
        spWalk1 = Resources.Load<Sprite>("Momia4/momia4_walk_1");
        spWalk2 = Resources.Load<Sprite>("Momia4/momia4_walk_2");
        spWalk3 = Resources.Load<Sprite>("Momia4/momia4_walk_3");
        spWalkDrag = Resources.Load<Sprite>("Momia4/momia4_walk_drag");
        spWalkFrontal = Resources.Load<Sprite>("Momia4/momia4_walk_frontal");

        spCrawl0 = Resources.Load<Sprite>("Momia4/momia4_crawl_0");
        spCrawl1 = Resources.Load<Sprite>("Momia4/momia4_crawl_1");
        spCrawl2 = Resources.Load<Sprite>("Momia4/momia4_crawl_2");

        spScream = Resources.Load<Sprite>("Momia4/momia4_scream");
        spStoop = Resources.Load<Sprite>("Momia4/momia4_stoop");

        spWhipFrames = new Sprite[13];
        for (int i = 0; i < 13; i++)
        {
            spWhipFrames[i] = Resources.Load<Sprite>($"Momia4/momia4_whip_{i}");
        }

        if (sr != null && spIdle0 != null)
        {
            sr.sprite = spIdle0;
            sr.sortingOrder = 12; // Mismo plano que la Vigilante: nunca tapada por bruma plana del piso
            sr.color = new Color(0.95f, 0.94f, 0.90f, 0f);
        }
    }

    private void ConfigurarSombraPies()
    {
        int w = 48, h = 20;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - w * 0.5f) / (w * 0.5f);
                float ny = (y - h * 0.5f) / (h * 0.5f);
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * 0.68f;
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        }
        tex.Apply();

        Sprite spSombra = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        GameObject objSombra = new GameObject("SombraPiesMomia");
        objSombra.transform.SetParent(transform, false);
        objSombra.transform.localPosition = new Vector3(0f, -1.04f, 0f);
        objSombra.transform.localScale = new Vector3(1.1f, 0.9f, 1f);

        srSombraPies = objSombra.AddComponent<SpriteRenderer>();
        srSombraPies.sprite = spSombra;
        srSombraPies.sortingOrder = 4; // Sombra sobre suelo (-15) y debajo de la momia (12)
        srSombraPies.color = new Color(1f, 1f, 1f, 0f);
    }

    private void ConfigurarOjosFosforescentes()
    {
        objOjosFosforescentes = new GameObject("OjosFosforescentes");
        objOjosFosforescentes.transform.SetParent(transform, false);
        objOjosFosforescentes.transform.localPosition = new Vector3(0.08f, 0.65f, 0f);

        luzOjos = objOjosFosforescentes.AddComponent<Light2D>();
        luzOjos.lightType = Light2D.LightType.Point;
        luzOjos.color = new Color(0.88f, 0.96f, 0.38f, 1f);
        luzOjos.intensity = 0f;
        luzOjos.pointLightInnerRadius = 0.12f;
        luzOjos.pointLightOuterRadius = 3.0f;
    }

    private void CrearSpriteChispa()
    {
        Texture2D tex = new Texture2D(2, 2);
        Color[] px = new Color[] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(px);
        tex.Apply();
        spChispa = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);
    }

    private void EmitirParticulaSuelo(Vector3 pos, bool esChispa, bool fuerte)
    {
        if (spChispa == null) return;
        GameObject ch = new GameObject(esChispa ? "ChispaCadena" : "PolvoTierraMomia");
        ch.transform.position = pos + new Vector3(Random.Range(-0.45f, 0.45f), -1.35f + Random.Range(-0.18f, 0.22f), 0f);
        float esc = fuerte ? Random.Range(0.50f, 0.95f) : Random.Range(0.28f, 0.55f);
        ch.transform.localScale = new Vector3(esc, esc, 1f);

        SpriteRenderer srC = ch.AddComponent<SpriteRenderer>();
        srC.sprite = spChispa;
        srC.sortingOrder = 11;

        if (esChispa)
        {
            srC.color = fuerte
                ? new Color(1f, Random.Range(0.65f, 0.9f), 0.25f, 0.95f)
                : new Color(0.95f, 0.72f, 0.35f, 0.75f);
        }
        else
        {
            srC.color = new Color(0.28f, 0.25f, 0.22f, Random.Range(0.45f, 0.75f));
        }

        Destroy(ch, fuerte ? 0.32f : 0.22f);
    }

    private void InicializarPuntosPatrulla()
    {
        // Puntos de patrulla verificados en avenidas amplias libres de tumbas, faroles y muros de mausoleos
        puntosPatrulla = new Vector2[]
        {
            new Vector2(0f, -15f),     // Calzada Central Sur
            new Vector2(-12f, -15f),   // Cruce Avenida Sur-Oeste
            new Vector2(-18f, -11f),   // Corredor Oeste Sur (entre tumbas y faroles)
            new Vector2(-18f, -2f),    // Corredor frente al acceso del Mausoleo Leroy
            new Vector2(-12f, 6f),     // Explanada abierta Oeste-Centro
            new Vector2(-14f, 16f),    // Conector Norte-Oeste hacia Capilla
            new Vector2(0f, 16f),      // Explanada frontal de la Capilla
            new Vector2(14f, 16f),     // Conector Norte-Este hacia Capilla
            new Vector2(12f, 6f),      // Explanada abierta Este-Centro
            new Vector2(18f, -2f),     // Corredor frente al acceso del Gran Osario
            new Vector2(18f, -11f),    // Corredor Este Sur
            new Vector2(12f, -15f),    // Cruce Avenida Sur-Este
            new Vector2(0f, -6f)       // Cruce Central
        };

        destinoActual = puntosPatrulla[0];
    }

    private void Update()
    {
        if (timerCooldownAtaque > 0f) timerCooldownAtaque -= Time.deltaTime;
        if (timerRepelidaFuego > 0f) timerRepelidaFuego -= Time.deltaTime;
        if (timerIgnorarEscondite > 0f) timerIgnorarEscondite -= Time.deltaTime;
        if (timerDesvioObstaculo > 0f) timerDesvioObstaculo -= Time.deltaTime;

        if (momiaDetenida)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (audioCadenas != null && audioCadenas.isPlaying) audioCadenas.Stop();
            if (audioCaminataMomia != null && audioCaminataMomia.isPlaying) audioCaminataMomia.Stop();
            return;
        }

        if (Time.timeScale == 0f || estadoActual == EstadoMomia4.Petrificada || (Nivel4Manager.Instance != null && Nivel4Manager.Instance.enInteriorCapilla))
        {
            if (audioCadenas != null && audioCadenas.isPlaying) audioCadenas.Stop();
            if (audioCaminataMomia != null && audioCaminataMomia.isPlaying) audioCaminataMomia.Stop();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (audioCadenas != null && !audioCadenas.isPlaying && clipCadenas != null)
        {
            audioCadenas.Play();
        }

        if (playerTransform == null && VigilanteNivel4.Instance != null)
        {
            playerTransform = VigilanteNivel4.Instance.transform;
        }

        timerGritoCooldown -= Time.deltaTime;

        // Mantener escala alta uniforme intacta
        if (Mathf.Abs(transform.localScale.x - escalaBase) > 0.01f)
        {
            transform.localScale = new Vector3(escalaBase, escalaBase, 1f);
        }

        if (estadoActual == EstadoMomia4.EsperandoAparicion)
        {
            ActualizarEsperaAparicion();
            return;
        }

        if (estadoActual == EstadoMomia4.Emergiendo)
        {
            ActualizarSecuenciaEmergencia();
            return;
        }

        ActualizarVolumenCadenas();
        ActualizarOjosFosforescentes();
        ActualizarChispasArrastre();
        EvaluarPercepcionJugador();
        ActualizarMaquinaDeEstados();
        ActualizarAnimacionVisualYPostura();
    }

    private void ActualizarEsperaAparicion()
    {
        timerEsperaAparicion += Time.deltaTime;

        float dist = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 99f;

        // Silencio antes de emerger para no ensuciar la música ambiental (fondo_4.mp3)
        if (audioCadenas != null)
        {
            audioCadenas.volume = 0f;
        }

        bool reliquiaRecogida = Nivel4Manager.Instance != null && Nivel4Manager.Instance.ReliquiasRecolectadas >= 1;
        if (dist <= distanciaActivacionAparicion || timerEsperaAparicion >= tiempoMaximoEsperaAparicion || reliquiaRecogida)
        {
            IniciarEmergenciaNatural();
        }
    }

    private void ReproducirSonidoDeteccion(float volumen)
    {
        if (audioVoz == null) return;
        if (clipDeteccion == null)
        {
            clipDeteccion = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_DETECCION_MOMIA4);
        }
        if (clipDeteccion != null)
        {
            audioVoz.PlayOneShot(clipDeteccion, volumen);
        }
    }

    public void IniciarEmergenciaNatural()
    {
        if (estadoActual != EstadoMomia4.EsperandoAparicion) return;

        estadoActual = EstadoMomia4.Emergiendo;
        progresoEmergencia = 0f;
        colFisico.enabled = true;

        if (playerTransform != null)
        {
            mirandoIzquierda = playerTransform.position.x < transform.position.x;
        }

        ReproducirSonidoDeteccion(0.75f);

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.MostrarAviso("¡LAS CADENAS RESUENAN! LA MOMIA ENCADENADA EMERGE DE ENTRE LAS SOMBRAS", 4.5f);
            Nivel4Manager.Instance.SacudirPantalla(0.85f, 14f);
        }
    }

    private void ActualizarSecuenciaEmergencia()
    {
        rb.linearVelocity = Vector2.zero;
        progresoEmergencia += Time.deltaTime / 1.85f;
        float p = Mathf.Clamp01(progresoEmergencia);

        // Polvo de tierra y chispas al salir
        timerChispa -= Time.deltaTime;
        if (timerChispa <= 0f)
        {
            timerChispa = 0.09f;
            EmitirParticulaSuelo(transform.position, Random.value < 0.4f, true);
        }

        // Aparece suavemente de agazapada -> encorvada -> erguida con sombra progresiva
        float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p * 1.4f));
        if (sr != null)
        {
            sr.color = new Color(0.95f, 0.94f, 0.90f, alpha);
            if (p < 0.35f)
            {
                AplicarSprite(spCrawl2 ?? spStoop, FacingNativo.Izquierda);
            }
            else if (p < 0.72f)
            {
                AplicarSprite(spStoop ?? spWalk0, FacingNativo.Izquierda);
            }
            else
            {
                AplicarSprite(spScream ?? spIdle0, FacingNativo.Simetrico);
            }
        }

        if (srSombraPies != null)
        {
            srSombraPies.color = new Color(1f, 1f, 1f, alpha);
        }

        if (luzOjos != null)
        {
            luzOjos.enabled = true;
            luzOjos.intensity = Mathf.Lerp(0f, 1.15f, p);
        }

        if (audioCadenas != null)
        {
            audioCadenas.volume = Mathf.Lerp(0.2f, 0.85f, p);
            audioCadenas.pitch = Mathf.Lerp(0.75f, 1.05f, p);
        }

        if (p >= 1f)
        {
            if (sr != null) sr.color = new Color(0.95f, 0.94f, 0.90f, 1f);
            if (srSombraPies != null) srSombraPies.color = Color.white;
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= radioVision)
            {
                IniciarPersecucion(playerTransform.position);
            }
            else
            {
                estadoActual = EstadoMomia4.Patrullando;
            }
        }
    }

    private void ActualizarVolumenCadenas()
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        float vol = Mathf.Clamp01(1f - (dist / 26f));
        vol = Mathf.Pow(vol, 1.35f);

        if (audioCadenas != null)
        {
            bool cadenasActivas = estadoActual != EstadoMomia4.EsperandoAparicion &&
                                  estadoActual != EstadoMomia4.Petrificada &&
                                  estadoActual != EstadoMomia4.AtacandoJumpscare &&
                                  rb != null && rb.linearVelocity.sqrMagnitude > 0.12f &&
                                  dist <= 16f;
            float volCadenasObj = cadenasActivas ? Mathf.Clamp01(1f - (dist / 16f)) * 0.14f : 0f;
            audioCadenas.volume = Mathf.Lerp(audioCadenas.volume, volCadenasObj, Time.deltaTime * 7f);
            bool enCarrera = (estadoActual == EstadoMomia4.PersecucionFrenetica || estadoActual == EstadoMomia4.LanzandoCadenas);
            float pitchObjetivo = enCarrera ? 1.15f : (estadoActual == EstadoMomia4.AcechandoBajo ? 1.0f : 0.88f);
            audioCadenas.pitch = Mathf.Lerp(audioCadenas.pitch, pitchObjetivo, Time.deltaTime * 6f);
        }

        if (audioCaminataMomia != null)
        {
            if (clipCaminataMomia == null) clipCaminataMomia = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMINATA_MOMIA);
            bool caminandoVisible = estadoActual != EstadoMomia4.EsperandoAparicion &&
                                    estadoActual != EstadoMomia4.Emergiendo &&
                                    estadoActual != EstadoMomia4.GritoParalizante &&
                                    estadoActual != EstadoMomia4.AtacandoJumpscare &&
                                    estadoActual != EstadoMomia4.Petrificada &&
                                    rb != null && rb.linearVelocity.sqrMagnitude > 0.12f &&
                                    dist <= 22f;

            if (caminandoVisible && clipCaminataMomia != null)
            {
                if (audioCaminataMomia.clip != clipCaminataMomia) audioCaminataMomia.clip = clipCaminataMomia;
                float proxCaminata = Mathf.Pow(Mathf.Clamp01(1f - (dist / 22f)), 1.25f);
                audioCaminataMomia.volume = Mathf.Lerp(audioCaminataMomia.volume, proxCaminata * 0.85f, Time.deltaTime * 8f);
                audioCaminataMomia.pitch = (estadoActual == EstadoMomia4.PersecucionFrenetica) ? 1.24f : 0.96f;
                if (!audioCaminataMomia.isPlaying) audioCaminataMomia.Play();
            }
            else
            {
                audioCaminataMomia.volume = Mathf.Lerp(audioCaminataMomia.volume, 0f, Time.deltaTime * 10f);
                if (audioCaminataMomia.volume < 0.01f && audioCaminataMomia.isPlaying) audioCaminataMomia.Stop();
            }
        }
    }

    private void ActualizarChispasArrastre()
    {
        if (rb == null || rb.linearVelocity.sqrMagnitude < 0.5f) return;

        timerChispa -= Time.deltaTime;
        float intervalo = (estadoActual == EstadoMomia4.PersecucionFrenetica) ? 0.08f : 0.22f;
        if (timerChispa <= 0f)
        {
            timerChispa = intervalo;
            EmitirParticulaSuelo(transform.position, true, estadoActual == EstadoMomia4.PersecucionFrenetica);
        }
    }

    private void ActualizarOjosFosforescentes()
    {
        if (luzOjos == null || playerTransform == null) return;

        Vector2 dirHaciaJugador = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 dirMiradaMomia = mirandoIzquierda ? Vector2.left : Vector2.right;

        float dot = Vector2.Dot(dirMiradaMomia, dirHaciaJugador);
        bool encarandoJugador = (dot > 0.10f ||
                                 estadoActual == EstadoMomia4.PersecucionFrenetica ||
                                 estadoActual == EstadoMomia4.LanzandoCadenas ||
                                 estadoActual == EstadoMomia4.InspeccionandoEscondite);

        luzOjos.enabled = encarandoJugador && (estadoActual != EstadoMomia4.Petrificada);
        if (luzOjos.enabled)
        {
            luzOjos.intensity = 0.85f + 0.40f * Mathf.Sin(Time.time * 8f);
        }
    }

    private bool PuntoEnZonaCirioActivo(Vector2 punto, out Vector2 centroCirio, out float radioCirio)
    {
        if (ciriosEscena == null || ciriosEscena.Length == 0)
        {
            ciriosEscena = FindObjectsByType<CirioBenditoNivel4>(FindObjectsSortMode.None);
        }

        if (ciriosEscena != null)
        {
            for (int i = 0; i < ciriosEscena.Length; i++)
            {
                CirioBenditoNivel4 c = ciriosEscena[i];
                if (c != null && c.EstaEncendido)
                {
                    float d = Vector2.Distance(punto, c.transform.position);
                    if (d <= c.radioProteccion + 0.75f)
                    {
                        centroCirio = c.transform.position;
                        radioCirio = c.radioProteccion;
                        return true;
                    }
                }
            }
        }

        centroCirio = Vector2.zero;
        radioCirio = 0f;
        return false;
    }

    private void EvaluarPercepcionJugador()
    {
        if (timerRepelidaFuego > 0f) return;

        if (playerTransform == null ||
            estadoActual == EstadoMomia4.AtacandoJumpscare ||
            estadoActual == EstadoMomia4.GritoParalizante ||
            estadoActual == EstadoMomia4.LanzandoCadenas)
        {
            return;
        }

        // Si la jugadora está protegida por un Cirio Bendito encendido, Carmen evita esa zona y sigue deambulando
        if (PuntoEnZonaCirioActivo(playerTransform.position, out Vector2 centroCirio, out float radioCirio))
        {
            if (estadoActual == EstadoMomia4.PersecucionFrenetica || estadoActual == EstadoMomia4.InvestigandoRuido)
            {
                RepelerPorFuegoBendito(centroCirio, radioCirio);
            }
            return;
        }

        // Si Carmen permanece dentro de una habitación cerrada (Mausoleo Leroy u Osario) sin estar atacando, salir lejos
        bool enMausoleo = transform.position.x >= -35.5f && transform.position.x <= -20.5f && transform.position.y >= 0.2f && transform.position.y <= 12f;
        bool enOsario = transform.position.x >= 20.5f && transform.position.x <= 35.5f && transform.position.y >= 0.2f && transform.position.y <= 12f;
        if ((enMausoleo || enOsario) && estadoActual != EstadoMomia4.LanzandoCadenas)
        {
            timerPermanenciaHabitacion += Time.deltaTime;
            if (timerPermanenciaHabitacion >= 2.4f)
            {
                timerPermanenciaHabitacion = 0f;
                timerIgnorarEscondite = 10.5f;
                Vector2 centroHab = enMausoleo ? new Vector2(-28f, 6f) : new Vector2(28f, 6f);
                destinoActual = ElegirPuntoPatrullaLejosDe(centroHab, 16.0f);
                estadoActual = EstadoMomia4.Patrullando;
                timerEstado = 10.5f;
            }
        }
        else
        {
            timerPermanenciaHabitacion = 0f;
        }

        bool jugadoraOculta = VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta;
        if (jugadoraOculta) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (dist <= rangoInicioLatigazo && timerCooldownAtaque <= 0f)
        {
            IniciarAtaqueCadenas();
            return;
        }

        bool enSigilo = VigilanteNivel4.ModoSigiloActivo;
        bool corriendo = VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaCorriendo;

        float radioAudible = enSigilo ? radioOidoSigilo : (corriendo ? radioOidoCorriendo : radioOidoNormal);
        if (dist <= radioAudible)
        {
            DetectarYDecidirAtaque(playerTransform.position, dist);
            return;
        }

        if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.linternaEncendida && dist <= radioVision)
        {
            DetectarYDecidirAtaque(playerTransform.position, dist);
            return;
        }

        if (dist <= radioVision * 1.2f && VigilanteNivel4.Instance != null)
        {
            Vector2 dirHaciaJugador = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            Vector2 dirMiradaJugador = VigilanteNivel4.Instance.DireccionMirada;

            float dotEspalda = Vector2.Dot(dirMiradaJugador, dirHaciaJugador);
            if (dotEspalda > 0.45f && !enSigilo)
            {
                DetectarYDecidirAtaque(playerTransform.position, dist);
                if (Nivel4Manager.Instance != null)
                {
                    Nivel4Manager.Instance.MostrarAviso("¡LA BRUJA DEL PANTEÓN TE ACECHA! SUS CADENAS SE ACERCAN", 2.2f);
                }
                return;
            }
        }
    }

    private void DetectarYDecidirAtaque(Vector2 pos, float dist)
    {
        if (dist <= rangoInicioLatigazo && timerCooldownAtaque <= 0f)
        {
            IniciarAtaqueCadenas();
        }
        else if (timerGritoCooldown <= 0f && dist > 4.6f && dist < 10.5f)
        {
            DesatarGritoParalizante(pos);
        }
        else
        {
            IniciarPersecucion(pos);
        }
    }

    public void NotificarRuido(Vector2 posRuido, float radio, float intensidad)
    {
        if (momiaDetenida) return;
        if (estadoActual == EstadoMomia4.EsperandoAparicion)
        {
            if (Vector2.Distance(transform.position, posRuido) <= radio)
            {
                IniciarEmergenciaNatural();
            }
            return;
        }
        if (estadoActual == EstadoMomia4.Emergiendo ||
            estadoActual == EstadoMomia4.AtacandoJumpscare ||
            estadoActual == EstadoMomia4.Petrificada ||
            estadoActual == EstadoMomia4.LanzandoCadenas ||
            estadoActual == EstadoMomia4.InspeccionandoEscondite)
            return;

        if (timerRepelidaFuego > 0f || timerIgnorarEscondite > 0f || PuntoEnZonaCirioActivo(posRuido, out _, out _))
            return;

        if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta)
            return;

        float d = Vector2.Distance(transform.position, posRuido);
        if (d <= radio)
        {
            posicionUltimoRuido = posRuido;
            if (estadoActual != EstadoMomia4.PersecucionFrenetica && estadoActual != EstadoMomia4.GritoParalizante)
            {
                estadoActual = EstadoMomia4.InvestigandoRuido;
                destinoActual = posRuido;
                timerEstado = 6.5f;
            }
        }
    }

    private void DesatarGritoParalizante(Vector2 pos)
    {
        estadoActual = EstadoMomia4.GritoParalizante;
        destinoActual = pos;
        timerEstado = 1.25f;
        timerGritoCooldown = 16f;

        rb.linearVelocity = Vector2.zero;

        ReproducirSonidoDeteccion(0.95f);

        if (VigilanteNivel4.Instance != null)
        {
            VigilanteNivel4.Instance.Aturdir(1.6f);
        }

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.SacudirPantalla(1.25f, 24f);
            Nivel4Manager.Instance.MostrarAviso("¡EL ALARIDO DE LA BRUJA PARALIZA TUS SENTIDOS!", 2.5f);
        }
    }

    private void IniciarPersecucion(Vector2 pos)
    {
        if (estadoActual != EstadoMomia4.PersecucionFrenetica)
        {
            estadoActual = EstadoMomia4.PersecucionFrenetica;
            if (audioVoz != null && !audioVoz.isPlaying)
            {
                ReproducirSonidoDeteccion(0.90f);
            }
        }
        destinoActual = pos;
        timerEstado = 9.0f;
    }

    private void IniciarAtaqueCadenas()
    {
        if (estadoActual == EstadoMomia4.LanzandoCadenas ||
            estadoActual == EstadoMomia4.Petrificada ||
            estadoActual == EstadoMomia4.AtacandoJumpscare ||
            estadoActual == EstadoMomia4.EsperandoAparicion ||
            estadoActual == EstadoMomia4.Emergiendo)
            return;

        estadoActual = EstadoMomia4.LanzandoCadenas;
        frameWhip = 0;
        timerWhipFrame = 0f;
        whipImpactoEjecutado = false;
        timerCooldownAtaque = 2.0f;

        if (playerTransform != null)
        {
            dirWhip = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            if (dirWhip.sqrMagnitude < 0.01f) dirWhip = Vector2.right;

            if (Mathf.Abs(dirWhip.x) > 0.05f)
            {
                mirandoIzquierda = dirWhip.x < 0f;
            }
        }

        if (audioVoz != null && clipLatigazoCadena != null)
        {
            audioVoz.PlayOneShot(clipLatigazoCadena, 1.0f);
        }
    }

    public void NotificarJugadorOculto(Vector2 posEscondite)
    {
        if (momiaDetenida || timerRepelidaFuego > 0f) return;
        if (estadoActual == EstadoMomia4.EsperandoAparicion ||
            estadoActual == EstadoMomia4.Emergiendo ||
            estadoActual == EstadoMomia4.Petrificada ||
            estadoActual == EstadoMomia4.AtacandoJumpscare)
            return;

        float dist = Vector2.Distance(transform.position, posEscondite);
        bool veniaSiguiendo = (estadoActual == EstadoMomia4.PersecucionFrenetica ||
                               estadoActual == EstadoMomia4.InvestigandoRuido ||
                               estadoActual == EstadoMomia4.GritoParalizante ||
                               estadoActual == EstadoMomia4.LanzandoCadenas);

        if (veniaSiguiendo || dist <= 13.5f)
        {
            IniciarInspeccionEscondite(posEscondite);
        }
    }

    private void IniciarInspeccionEscondite(Vector2 posEscondite)
    {
        estadoActual = EstadoMomia4.InspeccionandoEscondite;
        posEsconditeInspeccion = posEscondite;
        destinoActual = posEscondite;
        frameWhip = 0;
        whipImpactoEjecutado = false;

        float dist = Vector2.Distance(transform.position, posEscondite);
        if (dist <= 3.6f)
        {
            observandoEscondite = true;
            timerEstado = 2.0f;
            rb.linearVelocity = Vector2.zero;
            mirandoIzquierda = posEscondite.x < transform.position.x;
        }
        else
        {
            observandoEscondite = false;
            timerAcercamientoEscondite = 0f;
            timerAtascada = 0f;
        }
    }

    private void ActualizarMaquinaDeEstados()
    {
        timerEstado -= Time.deltaTime;

        switch (estadoActual)
        {
            case EstadoMomia4.Patrullando:
                MoverHaciaDestino(velocidadPatrulla);
                if (Vector2.Distance(transform.position, destinoActual) <= 1.3f)
                {
                    indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
                    destinoActual = puntosPatrulla[indicePatrulla];

                    if (Random.value < 0.35f)
                    {
                        estadoActual = EstadoMomia4.AcechandoBajo;
                        timerEstado = Random.Range(4.5f, 8.0f);
                    }
                }
                break;

            case EstadoMomia4.AcechandoBajo:
                MoverHaciaDestino(velocidadAcecho);
                if (timerEstado <= 0f || Vector2.Distance(transform.position, destinoActual) <= 1.3f)
                {
                    estadoActual = EstadoMomia4.Patrullando;
                    indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
                    destinoActual = puntosPatrulla[indicePatrulla];
                }
                break;

            case EstadoMomia4.GritoParalizante:
                rb.linearVelocity = Vector2.zero;
                if (timerEstado <= 0f)
                {
                    if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta)
                    {
                        Vector2 posEsc = VigilanteNivel4.Instance.EsconditeActual != null
                            ? (Vector2)VigilanteNivel4.Instance.EsconditeActual.transform.position
                            : destinoActual;
                        IniciarInspeccionEscondite(posEsc);
                    }
                    else
                    {
                        IniciarPersecucion(destinoActual);
                    }
                }
                break;

            case EstadoMomia4.InvestigandoRuido:
                if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta)
                {
                    Vector2 posEsc = VigilanteNivel4.Instance.EsconditeActual != null
                        ? (Vector2)VigilanteNivel4.Instance.EsconditeActual.transform.position
                        : destinoActual;
                    if (Vector2.Distance(transform.position, posEsc) <= 3.6f || timerEstado <= 4.5f)
                    {
                        IniciarInspeccionEscondite(posEsc);
                        break;
                    }
                }
                MoverHaciaDestino(velocidadAcecho);
                if (Vector2.Distance(transform.position, destinoActual) <= 1.5f || timerEstado <= 0f)
                {
                    estadoActual = EstadoMomia4.Patrullando;
                }
                break;

            case EstadoMomia4.PersecucionFrenetica:
                if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta)
                {
                    Vector2 posEsc = VigilanteNivel4.Instance.EsconditeActual != null
                        ? (Vector2)VigilanteNivel4.Instance.EsconditeActual.transform.position
                        : destinoActual;
                    IniciarInspeccionEscondite(posEsc);
                    return;
                }
                if (playerTransform != null)
                {
                    destinoActual = playerTransform.position;
                    float dist = Vector2.Distance(transform.position, playerTransform.position);
                    if (dist <= rangoInicioLatigazo && timerCooldownAtaque <= 0f)
                    {
                        IniciarAtaqueCadenas();
                        return;
                    }
                }
                MoverHaciaDestino(velocidadPersecucion);

                if (timerEstado <= 0f)
                {
                    estadoActual = EstadoMomia4.Patrullando;
                }
                break;

            case EstadoMomia4.InspeccionandoEscondite:
                if (!observandoEscondite)
                {
                    timerAcercamientoEscondite += Time.deltaTime;
                    float distEsc = Vector2.Distance(transform.position, posEsconditeInspeccion);
                    if (distEsc <= 3.0f || timerAcercamientoEscondite >= 1.2f)
                    {
                        observandoEscondite = true;
                        timerEstado = 1.15f;
                        rb.linearVelocity = Vector2.zero;
                        mirandoIzquierda = posEsconditeInspeccion.x < transform.position.x;
                    }
                    else
                    {
                        MoverHaciaDestino(velocidadAcecho);
                        if (rb.linearVelocity.sqrMagnitude < 0.05f || timerAtascada >= 0.25f)
                        {
                            observandoEscondite = true;
                            timerEstado = 1.15f;
                            rb.linearVelocity = Vector2.zero;
                            mirandoIzquierda = posEsconditeInspeccion.x < transform.position.x;
                        }
                    }
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                    mirandoIzquierda = posEsconditeInspeccion.x < transform.position.x;
                    if (timerEstado <= 0f)
                    {
                        observandoEscondite = false;
                        timerIgnorarEscondite = 11.5f;
                        timerPermanenciaHabitacion = 0f;
                        destinoActual = ElegirPuntoPatrullaLejosDe(posEsconditeInspeccion, 16.0f);
                        estadoActual = EstadoMomia4.Patrullando;
                        timerEstado = 11.5f;
                    }
                }
                break;

            case EstadoMomia4.LanzandoCadenas:
                ActualizarLanzamientoCadenas();
                break;

            case EstadoMomia4.Petrificada:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void ActualizarLanzamientoCadenas()
    {
        if (frameWhip >= 5 && frameWhip <= 8)
        {
            rb.linearVelocity = dirWhip * 2.4f;
        }
        else
        {
            rb.linearVelocity = dirWhip * 0.45f;
        }

        timerWhipFrame += Time.deltaTime;
        if (timerWhipFrame >= 0.055f)
        {
            timerWhipFrame = 0f;
            frameWhip++;

            if (frameWhip == 7 || frameWhip == 8)
            {
                Vector3 posPunta = transform.position + (Vector3)(dirWhip * 2.2f);
                EmitirParticulaSuelo(posPunta, true, true);
                EmitirParticulaSuelo(posPunta + new Vector3(0.25f, 0.25f, 0f), true, true);
            }

            if (frameWhip >= 7 && !whipImpactoEjecutado)
            {
                whipImpactoEjecutado = true;
                EvaluarImpactoLatigazoCadena();
            }

            if (frameWhip >= 13)
            {
                if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta)
                {
                    Vector2 posEsc = VigilanteNivel4.Instance.EsconditeActual != null
                        ? (Vector2)VigilanteNivel4.Instance.EsconditeActual.transform.position
                        : (Vector2)transform.position;
                    IniciarInspeccionEscondite(posEsc);
                }
                else if (playerTransform != null)
                {
                    IniciarPersecucion(playerTransform.position);
                }
                else
                {
                    estadoActual = EstadoMomia4.Patrullando;
                }
            }
        }
    }

    private void EvaluarImpactoLatigazoCadena()
    {
        if (playerTransform == null) return;
        if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta) return;

        Vector2 haciaJugador = (Vector2)playerTransform.position - (Vector2)transform.position;
        float dist = haciaJugador.magnitude;

        float dot = dist > 0.01f ? Vector2.Dot(dirWhip.normalized, haciaJugador.normalized) : 1f;
        if (dist <= alcanceGolpeCadena && (dot >= 0.15f || dist <= 2.1f))
        {
            AplicarDanioCadenaJugador();
        }
    }

    private float ProbarDistanciaLibre(Vector2 origen, Vector2 dirPrueba, float maxDist, out Vector2 normalChoque)
    {
        normalChoque = Vector2.zero;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origen, 0.28f, dirPrueba, maxDist, LayerMask.GetMask("Default"));
        float menorDist = maxDist;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject && !hit.collider.CompareTag("Player"))
            {
                // Ignorar muros que rozamos de lado o de los que ya nos estamos alejando
                if (hit.distance <= 0.02f && Vector2.Dot(dirPrueba, hit.normal) >= -0.05f) continue;
                if (hit.distance < menorDist)
                {
                    menorDist = hit.distance;
                    normalChoque = hit.normal;
                }
            }
        }
        return menorDist;
    }

    private bool TieneCaminoDespejado(Vector2 desde, Vector2 hasta)
    {
        Vector2 delta = hasta - desde;
        float dist = delta.magnitude;
        if (dist < 0.2f) return true;
        Vector2 dir = delta / dist;
        float probeDist = Mathf.Min(dist, 22f);
        return ProbarDistanciaLibre(desde, dir, probeDist, out _) >= probeDist - 0.3f;
    }

    private Vector2 ElegirMejorDireccionDesvio(Vector2 origen, Vector2 dirDeseada, Vector2 normalChoque)
    {
        Vector2 perpA = normalChoque != Vector2.zero ? Vector2.Perpendicular(normalChoque).normalized : Vector2.Perpendicular(dirDeseada).normalized;
        Vector2 perpB = -perpA;

        float dotA = Vector2.Dot(perpA, dirDeseada);
        float dotB = Vector2.Dot(perpB, dirDeseada);
        float libreA = ProbarDistanciaLibre(origen, perpA, 1.45f, out _);
        float libreB = ProbarDistanciaLibre(origen, perpB, 1.45f, out _);

        // Si ambos costados están despejados, mantener consistencia de lado para no zigzaguear frente a un muro plano
        float biasA = (ladoDesvioPreferido == 1) ? 0.35f : 0f;
        float biasB = (ladoDesvioPreferido == -1) ? 0.35f : 0f;

        float scoreA = libreA * 1.15f + Mathf.Max(0f, dotA) * 1.10f + biasA;
        float scoreB = libreB * 1.15f + Mathf.Max(0f, dotB) * 1.10f + biasB;

        Vector2 elegido;
        if (scoreA >= scoreB)
        {
            elegido = perpA;
            ladoDesvioPreferido = 1;
        }
        else
        {
            elegido = perpB;
            ladoDesvioPreferido = -1;
        }

        // Solo si ambas tangentes están completamente cerradas (esquina ciega), probar escape en diagonal abierta
        if (libreA < 0.32f && libreB < 0.32f)
        {
            Vector2 esc1 = (Quaternion.Euler(0f, 0f, 115f) * dirDeseada).normalized;
            Vector2 esc2 = (Quaternion.Euler(0f, 0f, -115f) * dirDeseada).normalized;
            elegido = (ProbarDistanciaLibre(origen, esc1, 1.5f, out _) >= ProbarDistanciaLibre(origen, esc2, 1.5f, out _)) ? esc1 : esc2;
        }

        if (normalChoque != Vector2.zero)
        {
            elegido = (elegido + normalChoque * 0.25f).normalized;
        }
        return elegido;
    }

    private void MoverHaciaDestino(float vel)
    {
        Vector2 posActual = transform.position;
        Vector2 origenCast = (colFisico != null && colFisico.enabled) ? (Vector2)colFisico.bounds.center : posActual;
        Vector2 dirDeseada = (destinoActual - posActual).normalized;
        if (dirDeseada.sqrMagnitude < 0.001f) dirDeseada = Vector2.right;

        // Si acaba de ser repelida por un Cirio Bendito y sigue dentro del radio sagrado,
        // priorizar alejarse limpiamente del pebetero sin quedar trabada en el altar ni contra muros
        float velEfectiva = vel;
        Vector2 haciaFuego = posActual - centroFuegoRepulsor;
        float distFuego = haciaFuego.magnitude;
        if (timerRepelidaFuego > 0f && distFuego < radioFuegoRepulsor + 1.2f)
        {
            Vector2 dirFuera = distFuego > 0.05f ? (haciaFuego / distFuego) : Vector2.down;
            dirDeseada = (dirDeseada * 0.55f + dirFuera * 0.95f).normalized;
            velEfectiva = Mathf.Max(vel, velocidadAcecho);
        }

        // Movimiento armonioso directo al objetivo, con deslizamiento natural solo si un muro bloquea el frente
        Vector2 dir = dirDeseada;
        float distFrente = ProbarDistanciaLibre(origenCast, dirDeseada, 0.85f, out Vector2 normalChoque);
        if (distFrente < 0.82f)
        {
            if (timerDesvioObstaculo <= 0f)
            {
                dirDesvioObstaculo = ElegirMejorDireccionDesvio(origenCast, dirDeseada, normalChoque);
                timerDesvioObstaculo = 0.42f;
            }
            dir = Vector2.Lerp(dirDeseada, dirDesvioObstaculo, 0.88f).normalized;
        }
        else if (timerDesvioObstaculo > 0f)
        {
            dir = Vector2.Lerp(dirDesvioObstaculo, dirDeseada, 0.55f).normalized;
        }
        else
        {
            ladoDesvioPreferido = 0;
        }

        // Control suave de atasco real (nunca cancela la persecución frenética ni cambia al azar en caza)
        timerCheckStuck += Time.deltaTime;
        if (timerCheckStuck >= 0.25f)
        {
            float distAvanzada = Vector2.Distance(posActual, lastPosCheck);
            if (distAvanzada < 0.08f)
            {
                timerAtascada += timerCheckStuck;
                if (timerAtascada >= 0.55f)
                {
                    ladoDesvioPreferido = (ladoDesvioPreferido == 0) ? 1 : -ladoDesvioPreferido;
                    dirDesvioObstaculo = ElegirMejorDireccionDesvio(origenCast, dirDeseada, Vector2.zero);
                    timerDesvioObstaculo = 0.65f;
                    dir = dirDesvioObstaculo;

                    if (estadoActual == EstadoMomia4.Patrullando || estadoActual == EstadoMomia4.AcechandoBajo || timerRepelidaFuego > 0f)
                    {
                        if (timerRepelidaFuego > 0f)
                        {
                            destinoActual = ElegirPuntoPatrullaLejosDe(centroFuegoRepulsor, radioFuegoRepulsor);
                        }
                        else
                        {
                            indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
                            destinoActual = puntosPatrulla[indicePatrulla];
                        }
                    }
                    timerAtascada = 0f;
                }
            }
            else
            {
                timerAtascada = 0f;
            }
            lastPosCheck = posActual;
            timerCheckStuck = 0f;
        }

        rb.linearVelocity = dir * velEfectiva;
        ultimaVel = rb.linearVelocity;

        if (Mathf.Abs(dir.x) > 0.08f)
        {
            mirandoIzquierda = dir.x < 0f;
            if (objOjosFosforescentes != null)
            {
                objOjosFosforescentes.transform.localPosition = new Vector3(mirandoIzquierda ? -0.15f : 0.15f, 0.68f, 0f);
            }
        }
    }

    private void AplicarSprite(Sprite sp, FacingNativo dirNativa)
    {
        if (sr == null || sp == null) return;
        sr.sprite = sp;

        switch (dirNativa)
        {
            case FacingNativo.Izquierda:
                sr.flipX = !mirandoIzquierda;
                break;
            case FacingNativo.Derecha:
                sr.flipX = mirandoIzquierda;
                break;
            default:
                sr.flipX = false;
                break;
        }
    }

    private void ActualizarAnimacionVisualYPostura()
    {
        if (sr == null || estadoActual == EstadoMomia4.Petrificada) return;

        timerAnimacion += Time.deltaTime;
        float velocidadAnim = (estadoActual == EstadoMomia4.PersecucionFrenetica) ? 0.10f : 0.15f;

        if (timerAnimacion >= velocidadAnim)
        {
            timerAnimacion = 0f;
            frameAnim++;
        }

        transform.rotation = Quaternion.identity; // Sin inclinación artificial flotante

        switch (estadoActual)
        {
            case EstadoMomia4.LanzandoCadenas:
                if (spWhipFrames != null && spWhipFrames.Length == 13)
                {
                    int idx = Mathf.Clamp(frameWhip, 0, 12);
                    AplicarSprite(spWhipFrames[idx] ?? spWalk0, FacingNativo.Derecha);
                }
                else
                {
                    AplicarSprite(spScream ?? spIdle0, FacingNativo.Simetrico);
                }
                break;

            case EstadoMomia4.GritoParalizante:
                AplicarSprite(spScream ?? spIdle0, FacingNativo.Simetrico);
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 18f) * 2.2f);
                break;

            case EstadoMomia4.AcechandoBajo:
                Sprite[] crawSeq = new Sprite[] { spCrawl0, spCrawl1, spCrawl2, spCrawl1 };
                AplicarSprite(crawSeq[frameAnim % crawSeq.Length] ?? spCrawl0, FacingNativo.Izquierda);
                break;

            case EstadoMomia4.PersecucionFrenetica:
                if (ultimaVel.y > 0.8f && Mathf.Abs(ultimaVel.x) < 0.45f * ultimaVel.y && spBack != null)
                {
                    AplicarSprite(spBack, FacingNativo.Simetrico);
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 14f) * 2.5f);
                }
                else
                {
                    // Alternancia de carrera agresiva respetando la orientación nativa de cada sprite
                    int step = frameAnim % 4;
                    if (step == 0) AplicarSprite(spStoop ?? spWalk0, FacingNativo.Izquierda);
                    else if (step == 1) AplicarSprite(spWalk0, FacingNativo.Izquierda);
                    else if (step == 2) AplicarSprite(spWalkDrag ?? spWalk2, spWalkDrag != null ? FacingNativo.Derecha : FacingNativo.Izquierda);
                    else AplicarSprite(spWalk3 ?? spWalk2, FacingNativo.Izquierda);
                }
                break;

            case EstadoMomia4.AtacandoJumpscare:
                AplicarSprite(spScream ?? spIdle0, FacingNativo.Simetrico);
                break;

            case EstadoMomia4.InspeccionandoEscondite:
                if (observandoEscondite || rb.linearVelocity.sqrMagnitude <= 0.1f)
                {
                    // Se detiene mirando con desconfianza hacia la cripta/lápida antes de marcharse
                    AplicarSprite(spSide ?? spIdle0, spSide != null ? FacingNativo.Derecha : FacingNativo.Simetrico);
                }
                else
                {
                    int step = frameAnim % 4;
                    if (step == 0) AplicarSprite(spWalk0, FacingNativo.Izquierda);
                    else if (step == 1) AplicarSprite(spWalk2, FacingNativo.Izquierda);
                    else if (step == 2) AplicarSprite(spWalkDrag ?? spWalk1, spWalkDrag != null ? FacingNativo.Derecha : FacingNativo.Izquierda);
                    else AplicarSprite(spWalk3, FacingNativo.Izquierda);
                }
                break;

            default:
                if (rb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    // Caminata natural erguida según la dirección cardinal real
                    if (ultimaVel.y > 0.65f && Mathf.Abs(ultimaVel.x) < 0.45f * ultimaVel.y && spBack != null)
                    {
                        AplicarSprite(spBack, FacingNativo.Simetrico);
                        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 9f) * 2.0f);
                    }
                    else if (ultimaVel.y < -0.65f && Mathf.Abs(ultimaVel.x) < 0.45f * -ultimaVel.y && spWalkFrontal != null)
                    {
                        if (frameAnim % 2 == 0) AplicarSprite(spWalkFrontal, FacingNativo.Derecha);
                        else AplicarSprite(spWalk1 ?? spWalkFrontal, FacingNativo.Izquierda);
                    }
                    else
                    {
                        int step = frameAnim % 4;
                        if (step == 0) AplicarSprite(spWalk0, FacingNativo.Izquierda);
                        else if (step == 1) AplicarSprite(spWalk2, FacingNativo.Izquierda);
                        else if (step == 2) AplicarSprite(spWalkDrag ?? spWalk1, spWalkDrag != null ? FacingNativo.Derecha : FacingNativo.Izquierda);
                        else AplicarSprite(spWalk3, FacingNativo.Izquierda);
                    }
                }
                else
                {
                    AplicarSprite((frameAnim % 2 == 0) ? spIdle0 : spIdle1, FacingNativo.Simetrico);
                }
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (momiaDetenida || timerRepelidaFuego > 0f) return;
        if (estadoActual == EstadoMomia4.EsperandoAparicion ||
            estadoActual == EstadoMomia4.Emergiendo ||
            estadoActual == EstadoMomia4.Petrificada ||
            estadoActual == EstadoMomia4.AtacandoJumpscare) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<VigilanteNivel4>() != null)
        {
            if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta) return;
            if (PuntoEnZonaCirioActivo(collision.transform.position, out _, out _)) return;

            if (timerCooldownAtaque <= 0f)
            {
                IniciarAtaqueCadenas();
                AplicarDanioCadenaJugador();
                whipImpactoEjecutado = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (momiaDetenida || timerRepelidaFuego > 0f) return;
        if (estadoActual == EstadoMomia4.EsperandoAparicion ||
            estadoActual == EstadoMomia4.Emergiendo ||
            estadoActual == EstadoMomia4.Petrificada ||
            estadoActual == EstadoMomia4.AtacandoJumpscare) return;

        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            if (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta) return;
            if (PuntoEnZonaCirioActivo(other.transform.position, out _, out _)) return;

            if (timerCooldownAtaque <= 0f)
            {
                IniciarAtaqueCadenas();
                AplicarDanioCadenaJugador();
                whipImpactoEjecutado = true;
            }
        }
    }

    private void AplicarDanioCadenaJugador()
    {
        if (VigilanteNivel4.Instance == null) return;

        float danio = 20f;
        VigilanteNivel4.Instance.RecibirDanio(danio, "Carmen");

        if (VigilanteNivel4.Instance.vidaActual <= 0f)
        {
            estadoActual = EstadoMomia4.AtacandoJumpscare;
            rb.linearVelocity = Vector2.zero;
            if (Nivel4Manager.Instance != null)
            {
                Nivel4Manager.Instance.ActivarJumpscareGameOver("Carmen");
            }
        }
        else
        {
            if (Nivel4Manager.Instance != null)
            {
                Nivel4Manager.Instance.MostrarAviso($"¡LA BRUJA TE AZOTÓ CON SUS CADENAS! (❤ {Mathf.CeilToInt(VigilanteNivel4.Instance.vidaActual)})", 2.4f);
                Nivel4Manager.Instance.SacudirPantalla(0.55f, 18f);
            }
        }
    }

    private Vector2 ElegirPuntoPatrullaLejosDe(Vector2 centroEvitar, float radioEvitar)
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            Vector2 away = ((Vector2)transform.position - centroEvitar).normalized;
            if (away.sqrMagnitude < 0.01f) away = Vector2.down;
            return (Vector2)transform.position + away * 14f;
        }

        Vector2 origen = (colFisico != null && colFisico.enabled) ? (Vector2)colFisico.bounds.center : (Vector2)transform.position;
        List<int> candidatosDespejados = new List<int>();
        List<int> candidatosGenerales = new List<int>();
        float minDist = Mathf.Max(radioEvitar + 6.0f, 10.5f);

        for (int i = 0; i < puntosPatrulla.Length; i++)
        {
            if (i == indicePatrulla) continue;
            if (Vector2.Distance(puntosPatrulla[i], centroEvitar) >= minDist)
            {
                candidatosGenerales.Add(i);
                if (TieneCaminoDespejado(origen, puntosPatrulla[i]))
                {
                    candidatosDespejados.Add(i);
                }
            }
        }

        if (candidatosDespejados.Count > 0)
        {
            int elegido = candidatosDespejados[Random.Range(0, candidatosDespejados.Count)];
            indicePatrulla = elegido;
            return puntosPatrulla[elegido];
        }

        if (candidatosGenerales.Count > 0)
        {
            int elegido = candidatosGenerales[Random.Range(0, candidatosGenerales.Count)];
            indicePatrulla = elegido;
            return puntosPatrulla[elegido];
        }

        // Respaldo: elegir el punto más lejano disponible en el panteón para no rondar cerca
        int masLejano = 0;
        float maxD = -1f;
        for (int i = 0; i < puntosPatrulla.Length; i++)
        {
            float d = Vector2.Distance(puntosPatrulla[i], centroEvitar);
            if (d > maxD)
            {
                maxD = d;
                masLejano = i;
            }
        }
        indicePatrulla = masLejano;
        return puntosPatrulla[masLejano];
    }

    public void RepelerPorFuegoBendito(Vector3 centroFuego, float radio)
    {
        if (estadoActual == EstadoMomia4.EsperandoAparicion ||
            estadoActual == EstadoMomia4.Emergiendo ||
            estadoActual == EstadoMomia4.Petrificada ||
            estadoActual == EstadoMomia4.AtacandoJumpscare)
            return;

        centroFuegoRepulsor = centroFuego;
        radioFuegoRepulsor = radio;

        bool necesitaNuevoDestino = (timerRepelidaFuego <= 0f) ||
                                    (estadoActual != EstadoMomia4.Patrullando) ||
                                    (Vector2.Distance(destinoActual, centroFuego) < radio + 4.0f);

        if (necesitaNuevoDestino)
        {
            destinoActual = ElegirPuntoPatrullaLejosDe(centroFuego, radio);
            estadoActual = EstadoMomia4.Patrullando;
            timerRepelidaFuego = 4.5f;
            timerEstado = 8.0f;
            frameWhip = 0;
            whipImpactoEjecutado = false;
        }
    }

    public void PetrificarMomia()
    {
        estadoActual = EstadoMomia4.Petrificada;
        rb.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.identity;

        if (audioCadenas != null) audioCadenas.Stop();
        if (audioCaminataMomia != null) audioCaminataMomia.Stop();
        if (luzOjos != null) luzOjos.enabled = false;

        if (sr != null)
        {
            AplicarSprite(spScream ?? spIdle0, FacingNativo.Simetrico);
            sr.color = new Color(0.48f, 0.46f, 0.44f, 1f);
        }
    }
}
