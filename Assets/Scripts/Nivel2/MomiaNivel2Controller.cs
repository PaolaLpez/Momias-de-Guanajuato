using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Enemigo principal del Nivel 2: "La Que Escucha" (Momia 2).
/// - Acecha en silencio absoluto (CERO ruidos constantes que contaminen el ambiente).
/// - OPCIÓN 2 (Rejas impenetrables + Alcantarillas):
///   * Las rejas mecánicas cerradas (PuertaRejaMecanicaNivel2) son 100% impenetrables para la Momia (jamás las abre).
///   * Si hay otra puerta abierta hacia la sala objetivo, rodea lógicamente por la puerta abierta.
///   * Si la Guardia la encierra con todas las rejas de una sala cerradas, golpea/rasguña los barrotes impotente
///     durante 6.5s (en alerta) o patrulla la sala durante 11s, y luego se dirige a la Rejilla de Alcantarilla
///     de esa sala para sumergirse por el drenaje y emerger en otra alcantarilla del mapa.
/// - Mayor dificultad de escape y detección de pasos sin Modo Sigilo cerca.
/// - Animación dedicada de rasguñar (framesRasgunar + zarpazos sangrientos + audio de desgarro) al atacar a la jugadora.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class MomiaNivel2Controller : MonoBehaviour
{
    public static MomiaNivel2Controller Instance { get; private set; }

    public enum EstadoMomia2
    {
        AcechandoEnOscuridad,
        AlertaAcusticaGrito,
        InvestigandoSonido,
        CazandoContactoLetal
    }

    private enum FaseAlcantarilla
    {
        Ninguna,
        CaminandoHaciaAlcantarilla,
        SumergiendoseEnDrenaje,
        EmergiendoDeDrenaje
    }

    [Header("Estado Actual (Solo Lectura)")]
    public EstadoMomia2 estadoActual = EstadoMomia2.AcechandoEnOscuridad;
    public string ultimoSonidoEscuchado = "Silencio subterráneo";
    public Vector2 ultimaPosicionSonido;
    public float distanciaAlJugador = 99f;
    public bool jugadorDetectadoAlEsconderse;

    [Header("Velocidades y Percepción Acústica (Equilibradas y Jugables)")]
    public float velocidadPatrulla = 3.05f;
    public float velocidadInvestigacion = 5.55f;
    public float velocidadFrenesiGrito = 7.15f;
    public float multiplicadorAudicion = 1.05f;
    public float radioDeteccionContacto = 1.15f;
    public float rangoAtaqueLetal = 1.45f;
    public float danioPorSegundo = 48f;

    public const float PPU_MOMIA2 = 112f;

    // Estados de ataque de rasguño, encierro tras reja y viaje por alcantarillas
    public bool EstaEjecutandoAtaqueRasguno { get; private set; }
    public float TiempoRasgunandoActual { get; private set; }
    public bool EstaGolpeandoRejaEncerrada { get; private set; }
    public bool EstaUsandoAlcantarilla => faseAlcantarilla != FaseAlcantarilla.Ninguna;

    // Sprites cargados desde Resources/Momia2 + secuencia dedicada de rasguñar
    private Sprite poseScream;
    private Sprite poseStalk;
    private Sprite poseBack;
    private Sprite poseSide;
    private Sprite[] framesWalk;
    private Sprite[] framesBack;
    private Sprite[] framesScream;
    private Sprite[] framesRasgunar;
    private Sprite[] spritesEfectoZarpazo;

    // Componentes
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    private Transform sombraTransform;
    private SpriteRenderer sombraSr;
    private Light2D luzAuraTerror;

    // Efecto visual de zarpazos sobre la jugadora al rasguñarla
    private GameObject objEfectoZarpazo;
    private SpriteRenderer srEfectoZarpazo;
    private float timerEfectoZarpazo;
    private int frameZarpazoActual;

    // Referencias al jugador
    private Transform playerTransform;
    private PlayerController playerScript;
    private Rigidbody2D playerRb;
    private EsconditeNivel2 esconditeComprometido;

    // Control de animación por frames
    private Vector2 direccionVisual = Vector2.right;
    private Vector2 direccionMovimientoSuavizada = Vector2.up;
    private float animTimer;
    private int currentFrame;

    // Temporizadores de IA y sistema anti-atasco
    private float timerAlertaGrito;
    private float timerInvestigacionRestante;
    private float timerBusquedaLocal;
    private Vector2 puntoBusquedaLocal;
    private int indicePatrullaActual;
    private float timerCambioPatrulla;
    private float timerIgnorarObstaculos;
    private float velocidadObjetivoActual;

    // Control de encierro por rejas cerradas y transición por alcantarillas (Opción 2)
    private float timerEncerradaSinRuta;
    private PuertaRejaMecanicaNivel2 rejaQueLaEncierra;
    private float timerSonidoGolpeReja;
    private FaseAlcantarilla faseAlcantarilla = FaseAlcantarilla.Ninguna;
    private float timerFaseAlcantarilla;
    private int nodoAlcantarillaOrigen = -1;
    private int nodoAlcantarillaDestino = -1;

    // Nodos del grafo donde están ubicadas las 4 Rejillas Estratégicas de Alcantarilla por sector:
    // 6: Pasillo Central Drenaje (0, -8), 11: Sala de Calderas (-17, -2),
    // 16: Criptas y Osarios (17, -2), 20: Subestación Norte (0, 10.4)
    private static readonly int[] NodosAlcantarillasSalas = new int[] { 6, 11, 16, 20 };

    // Escalado progresivo por Fases Eléctricas (0/3, 1/3, 2/3 y 3/3 Asedio Final de Salida)
    public int FasesElectricasActivas { get; private set; }
    private int ultimaFaseProcesada = 0;
    private int indiceAsedioSalida = 0;
    private static readonly int[] NodosAsedioSalida = new int[] { 20, 19, 22, 23, 24, 21, 20, 8 };

    // Control de desatasco automático respetando muros físicos
    private Vector2 posicionChequeoAtasco;
    private float timerChequeoAtasco;
    private int contadorDesatascosConsecutivos;

    // Audio puntual
    private AudioSource fuenteAudioPuntual;
    private AudioSource fuenteAudioCercania;
    private AudioClip clipRugidoDeteccion;
    private AudioClip clipAtaqueCercano;
    private AudioClip clipZarpazoRasguno;
    private AudioClip clipGolpeRejaEncierro;
    private AudioClip clipAlcantarillaDrenaje;
    private AudioClip clipCercaniaMomia;
    private float timerSonidoMomiaDeVezEnCuando = 4.0f;
    private float tiempoUltimoRugidoAlerta = -20f;
    private float tiempoUltimoSonidoAtaque = -20f;
    private float tiempoUltimoSonidoRasguno = -20f;

    // Ondas visuales de escucha
    private readonly List<OndaVisualTerror> ondasActivas = new List<OndaVisualTerror>();
    private static Sprite spriteAnilloOnda;

    private class OndaVisualTerror
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public float tiempo;
        public float duracion;
        public float escalaFinal;
        public Color colorBase;
    }

    // =========================================================================
    // GRAFO DE NAVEGACIÓN DE 25 NODOS INTERCONECTADOS DEL SUBTERRÁNEO (NIVEL 2)
    // =========================================================================
    private static readonly Vector2[] NodosGrafo = new Vector2[]
    {
        // 0..5: Vestíbulo Sur y pasos laterales hacia las Rejas Sur (Y = -13.2 evita los muros divisorios)
        new Vector2(0f, -16.0f),      // 0: Vestíbulo Sur Centro (Alcantarilla Vestíbulo)
        new Vector2(0f, -13.2f),      // 1: Cruce Sur Pasillo Central
        new Vector2(-5.0f, -13.2f),   // 2: Paso Sur-Oeste (antes de muro divisorio)
        new Vector2(-10.5f, -13.2f),  // 3: Entrada Sur Reja 1 (Calderas)
        new Vector2(5.0f, -13.2f),    // 4: Paso Sur-Este (antes de muro divisorio)
        new Vector2(10.5f, -13.2f),   // 5: Entrada Sur Reja 2 (Criptas)

        // 6..8: Pasillo Central Troncal (X = 0)
        new Vector2(0f, -8.0f),       // 6: Pasillo Central Sur (Alcantarilla Pasillo Sur)
        new Vector2(0f, -1.5f),       // 7: Pasillo Central Medio
        new Vector2(0f, 5.2f),        // 8: Pasillo Central Norte (frente a Reja 5 / Alcantarilla Norte)

        // 9..13: Sala de Calderas (Oeste)
        new Vector2(-10.5f, -8.2f),   // 9: Interior Sur Calderas (tras Reja 1)
        new Vector2(-17.0f, -8.2f),   // 10: Calderas Sur-Oeste
        new Vector2(-17.0f, -2.0f),   // 11: Calderas Centro (Alcantarilla Calderas)
        new Vector2(-17.0f, 4.8f),    // 12: Calderas Norte-Oeste
        new Vector2(-10.5f, 5.2f),    // 13: Salida Norte Calderas (antes de Reja 3)

        // 14..18: Criptas y Osarios (Este)
        new Vector2(10.5f, -8.2f),    // 14: Interior Sur Criptas (tras Reja 2)
        new Vector2(17.0f, -8.2f),    // 15: Criptas Sur-Este
        new Vector2(17.0f, -2.0f),    // 16: Criptas Centro (Alcantarilla Criptas)
        new Vector2(17.0f, 4.8f),     // 17: Criptas Norte-Este
        new Vector2(10.5f, 5.2f),     // 18: Salida Norte Criptas (antes de Reja 4)

        // 19..24: Subestación Eléctrica Norte (rodeando el Generador Central en 0, 14)
        new Vector2(-10.5f, 10.6f),   // 19: Subestación Suroeste (tras Reja 3)
        new Vector2(0f, 10.4f),       // 20: Subestación Sur-Centro (tras Reja 5 / Alcantarilla Subestación)
        new Vector2(10.5f, 10.6f),    // 21: Subestación Sureste (tras Reja 4)
        new Vector2(-10.5f, 17.5f),   // 22: Subestación Noroeste (junto a Fusible 3)
        new Vector2(0f, 18.2f),       // 23: Subestación Norte-Centro (frente a Compuerta de Salida)
        new Vector2(10.5f, 17.5f)     // 24: Subestación Noreste
    };

    private static readonly int[][] VecinosGrafo = new int[][]
    {
        new int[] { 1 },                   // 0
        new int[] { 0, 2, 4, 6 },          // 1
        new int[] { 1, 3 },                // 2
        new int[] { 2, 9 },                // 3 (cruza Reja 1 en -10.5, -11)
        new int[] { 1, 5 },                // 4
        new int[] { 4, 14 },               // 5 (cruza Reja 2 en 10.5, -11)
        new int[] { 1, 7 },                // 6
        new int[] { 6, 8 },                // 7
        new int[] { 7, 20 },               // 8 (cruza Reja 5 en 0, 8)
        new int[] { 3, 10, 11, 13 },       // 9
        new int[] { 9, 11 },               // 10
        new int[] { 9, 10, 12, 13 },       // 11
        new int[] { 11, 13 },              // 12
        new int[] { 9, 11, 12, 19 },       // 13 (cruza Reja 3 en -10.5, 8)
        new int[] { 5, 15, 16, 18 },       // 14
        new int[] { 14, 16 },              // 15
        new int[] { 14, 15, 17, 18 },      // 16
        new int[] { 16, 18 },              // 17
        new int[] { 14, 16, 17, 21 },      // 18 (cruza Reja 4 en 10.5, 8)
        new int[] { 13, 20, 22 },          // 19
        new int[] { 8, 19, 21 },           // 20
        new int[] { 18, 20, 24 },          // 21
        new int[] { 19, 23 },              // 22
        new int[] { 22, 24 },              // 23
        new int[] { 21, 23 }               // 24
    };

    private static readonly int[] RutaNodosPatrulla = new int[]
    {
        8, 7, 6, 1, 2, 3, 9, 11, 12, 13, 19, 22, 23, 24, 21, 18, 17, 16, 14, 5, 4, 1, 6, 7
    };

    private void Awake()
    {
        Instance = this;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col.direction = CapsuleDirection2D.Vertical;
        col.size = new Vector2(0.48f, 0.48f);
        col.offset = new Vector2(0f, 0.28f);

        velocidadPatrulla = 3.05f;
        velocidadInvestigacion = 5.55f;
        velocidadFrenesiGrito = 7.15f;
        multiplicadorAudicion = 1.05f;
        radioDeteccionContacto = 1.15f;
        rangoAtaqueLetal = 1.45f;
        danioPorSegundo = 48f;

        CargarSpritesMomia2();
        ConfigurarSombraYAura();
        if (Application.isPlaying)
        {
            InicializarAudioPuntual();
            InicializarEfectoZarpazo();
        }
    }

    private void OnEnable()
    {
        Instance = this;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        CargarSpritesMomia2();
        if (Application.isPlaying)
        {
            TrampaAcusticaNivel2.OnRuidoGenerado -= EscucharRuidoDelEntornoOMicrofono;
            TrampaAcusticaNivel2.OnRuidoGenerado += EscucharRuidoDelEntornoOMicrofono;
        }
    }

    private void OnDisable()
    {
        TrampaAcusticaNivel2.OnRuidoGenerado -= EscucharRuidoDelEntornoOMicrofono;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (objEfectoZarpazo != null)
        {
            if (Application.isPlaying) Destroy(objEfectoZarpazo);
            else DestroyImmediate(objEfectoZarpazo);
        }
        for (int i = 0; i < ondasActivas.Count; i++)
        {
            if (ondasActivas[i].obj != null)
            {
                if (Application.isPlaying) Destroy(ondasActivas[i].obj);
                else DestroyImmediate(ondasActivas[i].obj);
            }
        }
        ondasActivas.Clear();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        LocalizarJugador();
        IgnorarColisionesSoloConRatas();
        indicePatrullaActual = 0;
        posicionChequeoAtasco = transform.position;
    }

    public void CargarSpritesMomia2()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        poseScream = CargarSpriteDirectoDesdeTextura("Momia2/momia2_pose0_scream");
        poseStalk = CargarSpriteDirectoDesdeTextura("Momia2/momia2_pose1_stalk");
        poseBack = CargarSpriteDirectoDesdeTextura("Momia2/momia2_pose2_back");
        poseSide = CargarSpriteDirectoDesdeTextura("Momia2/momia2_pose3_side");

        framesWalk = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            framesWalk[i] = CargarSpriteDirectoDesdeTextura("Momia2/momia2_walk_" + i) ?? poseSide;
        }

        framesBack = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            framesBack[i] = CargarSpriteDirectoDesdeTextura("Momia2/momia2_back_" + i) ?? poseBack;
        }

        framesScream = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            framesScream[i] = CargarSpriteDirectoDesdeTextura("Momia2/momia2_scream_" + i) ?? poseScream;
        }

        if (framesRasgunar == null || framesRasgunar.Length == 0 || framesRasgunar[0] == null)
        {
            GenerarSpritesAnimacionRasgunar();
        }

        if (spritesEfectoZarpazo == null || spritesEfectoZarpazo.Length == 0 || spritesEfectoZarpazo[0] == null)
        {
            GenerarSpritesEfectoZarpazoSobreJugadora();
        }

        if (sr != null && poseStalk != null && !Application.isPlaying)
        {
            sr.sprite = poseStalk;
        }
    }

    public static Sprite CargarSpriteDirectoDesdeTextura(string rutaResources)
    {
        Texture2D tex = Resources.Load<Texture2D>(rutaResources);
        if (tex == null)
        {
            Sprite spRes = Resources.Load<Sprite>(rutaResources);
            if (spRes != null) tex = spRes.texture;
        }

        if (tex != null)
        {
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.04f), PPU_MOMIA2);
        }
        return null;
    }

    public static Sprite CargarSpriteSeguro(string rutaResources)
    {
        return CargarSpriteDirectoDesdeTextura(rutaResources);
    }

    private void GenerarSpritesAnimacionRasgunar()
    {
        framesRasgunar = new Sprite[6];
        string[] rutasBase = new string[]
        {
            "Momia2/momia2_scream_0",
            "Momia2/momia2_scream_1",
            "Momia2/momia2_walk_2",
            "Momia2/momia2_scream_2",
            "Momia2/momia2_scream_3",
            "Momia2/momia2_walk_5"
        };

        for (int f = 0; f < 6; f++)
        {
            Texture2D texOrigen = Resources.Load<Texture2D>(rutasBase[f]);
            if (texOrigen == null && framesScream != null && framesScream.Length > 0 && framesScream[0] != null)
            {
                texOrigen = framesScream[f % framesScream.Length].texture;
            }

            if (texOrigen == null)
            {
                framesRasgunar[f] = poseScream ?? poseStalk;
                continue;
            }

            Texture2D texCopia = CopiarTexturaLegible(texOrigen);
            int w = texCopia.width;
            int h = texCopia.height;
            Color[] pix = texCopia.GetPixels();

            PintarZarpazoEnTexturaMomia(pix, w, h, f);

            texCopia.SetPixels(pix);
            texCopia.Apply();
            texCopia.filterMode = FilterMode.Bilinear;
            framesRasgunar[f] = Sprite.Create(texCopia, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.04f), PPU_MOMIA2);
        }
    }

    private static Texture2D CopiarTexturaLegible(Texture2D fuente)
    {
        RenderTexture rt = RenderTexture.GetTemporary(fuente.width, fuente.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(fuente, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D copia = new Texture2D(fuente.width, fuente.height, TextureFormat.RGBA32, false);
        copia.ReadPixels(new Rect(0, 0, fuente.width, fuente.height), 0, 0);
        copia.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copia;
    }

    private static void PintarZarpazoEnTexturaMomia(Color[] pix, int w, int h, int frameIdx)
    {
        bool diagonalIzqADer = (frameIdx == 1 || frameIdx == 2 || frameIdx == 5);
        bool diagonalDerAIzq = (frameIdx == 3 || frameIdx == 4 || frameIdx == 5);

        float intensidad = (frameIdx == 1 || frameIdx == 4 || frameIdx == 5) ? 1.0f : 0.65f;

        for (int garra = -1; garra <= 1; garra++)
        {
            float offsetGarra = garra * (w * 0.065f);

            if (diagonalIzqADer)
            {
                Vector2 ini = new Vector2(w * 0.16f + offsetGarra, h * 0.72f);
                Vector2 fin = new Vector2(w * 0.84f + offsetGarra, h * 0.24f);
                DibujarTrazoGarraEnBuffer(pix, w, h, ini, fin, w * 0.018f, intensidad);
            }

            if (diagonalDerAIzq)
            {
                Vector2 ini = new Vector2(w * 0.84f + offsetGarra, h * 0.74f);
                Vector2 fin = new Vector2(w * 0.16f + offsetGarra, h * 0.22f);
                DibujarTrazoGarraEnBuffer(pix, w, h, ini, fin, w * 0.018f, intensidad);
            }
        }
    }

    private static void DibujarTrazoGarraEnBuffer(Color[] pix, int w, int h, Vector2 a, Vector2 b, float radioMax, float alphaMax)
    {
        int pasos = Mathf.CeilToInt(Vector2.Distance(a, b) * 1.5f);
        for (int i = 0; i <= pasos; i++)
        {
            float t = i / (float)Mathf.Max(1, pasos);
            Vector2 p = Vector2.Lerp(a, b, t);
            p.y += Mathf.Sin(t * Mathf.PI) * (h * 0.045f);

            float perfil = Mathf.Sin(t * Mathf.PI);
            float rActual = Mathf.Max(1.2f, radioMax * perfil);
            int rInt = Mathf.CeilToInt(rActual);

            int cx = Mathf.RoundToInt(p.x);
            int cy = Mathf.RoundToInt(p.y);

            for (int dy = -rInt; dy <= rInt; dy++)
            {
                int py = cy + dy;
                if (py < 0 || py >= h) continue;
                for (int dx = -rInt; dx <= rInt; dx++)
                {
                    int px = cx + dx;
                    if (px < 0 || px >= w) continue;

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= rActual)
                    {
                        float nucleo = 1f - (dist / rActual);
                        Color colGarra = Color.Lerp(new Color(0.82f, 0.05f, 0.05f, 0.90f), new Color(1f, 0.88f, 0.82f, 1f), nucleo * nucleo);
                        float aBlend = colGarra.a * alphaMax * perfil;
                        Color fondo = pix[py * w + px];
                        Color res = Color.Lerp(fondo, colGarra, aBlend);
                        res.a = Mathf.Max(fondo.a, aBlend);
                        pix[py * w + px] = res;
                    }
                }
            }
        }
    }

    private void GenerarSpritesEfectoZarpazoSobreJugadora()
    {
        spritesEfectoZarpazo = new Sprite[4];
        const int tam = 96;

        for (int idx = 0; idx < 4; idx++)
        {
            Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            Color[] pix = new Color[tam * tam];

            bool invertido = (idx % 2 == 1);
            bool cruzado = (idx == 3);

            for (int g = -1; g <= 1; g++)
            {
                float off = g * 16f;
                Vector2 p1 = invertido ? new Vector2(82f + off * 0.4f, 84f - off * 0.3f) : new Vector2(14f + off * 0.4f, 84f + off * 0.3f);
                Vector2 p2 = invertido ? new Vector2(14f + off * 0.4f, 12f - off * 0.3f) : new Vector2(82f + off * 0.4f, 12f + off * 0.3f);
                DibujarTrazoGarraEnBuffer(pix, tam, tam, p1, p2, 3.6f, 0.95f);

                if (cruzado)
                {
                    Vector2 c1 = new Vector2(82f + off * 0.4f, 84f - off * 0.3f);
                    Vector2 c2 = new Vector2(14f + off * 0.4f, 12f - off * 0.3f);
                    DibujarTrazoGarraEnBuffer(pix, tam, tam, c1, c2, 3.2f, 0.90f);
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            spritesEfectoZarpazo[idx] = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 64f);
        }
    }

    private void InicializarEfectoZarpazo()
    {
        if (objEfectoZarpazo != null) return;
        objEfectoZarpazo = new GameObject("EfectoZarpazoRasguno_Momia2");
        srEfectoZarpazo = objEfectoZarpazo.AddComponent<SpriteRenderer>();
        srEfectoZarpazo.sortingOrder = 1600;
        srEfectoZarpazo.enabled = false;
    }

    private void ConfigurarSombraYAura()
    {
        Transform sombraExistente = transform.Find("Sombra_Momia2");
        if (sombraExistente == null)
        {
            GameObject objSombra = new GameObject("Sombra_Momia2");
            objSombra.transform.SetParent(transform, false);
            objSombra.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            objSombra.transform.localScale = new Vector3(0.65f, 0.35f, 1f);
            sombraSr = objSombra.AddComponent<SpriteRenderer>();
            Sprite spSombra = Resources.Load<Sprite>("Momia/momia_shadow");
            if (spSombra != null)
            {
                sombraSr.sprite = spSombra;
            }
            sombraSr.color = new Color(0f, 0f, 0f, 0.55f);
            sombraSr.sortingOrder = 12;
            sombraTransform = objSombra.transform;
        }
        else
        {
            sombraTransform = sombraExistente;
            sombraTransform.localScale = new Vector3(0.65f, 0.35f, 1f);
            sombraSr = sombraExistente.GetComponent<SpriteRenderer>();
        }

        Transform luzExistente = transform.Find("LuzAura_Momia2");
        if (luzExistente == null)
        {
            GameObject objLuz = new GameObject("LuzAura_Momia2");
            objLuz.transform.SetParent(transform, false);
            objLuz.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            luzAuraTerror = objLuz.AddComponent<Light2D>();
            luzAuraTerror.lightType = Light2D.LightType.Point;
            luzAuraTerror.pointLightInnerRadius = 0.2f;
            luzAuraTerror.pointLightOuterRadius = 2.6f;
            luzAuraTerror.intensity = 0.32f;
            luzAuraTerror.color = new Color(0.55f, 0.32f, 0.22f);
        }
        else
        {
            luzAuraTerror = luzExistente.GetComponent<Light2D>();
        }
    }

    private void LocalizarJugador()
    {
        if (playerTransform != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponent<PlayerController>();
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }
    }

    /// <summary>
    /// Ignora colisiones únicamente con las ratas pequeñas, pero mantiene colisión física 100% sólida
    /// con los muros y con todas las rejas mecánicas cerradas (PuertaRejaMecanicaNivel2).
    /// </summary>
    public void IgnorarColisionesSoloConRatas()
    {
        if (col == null) return;

        PuertaRejaMecanicaNivel2[] rejas = FindObjectsByType<PuertaRejaMecanicaNivel2>(FindObjectsSortMode.None);
        for (int i = 0; i < rejas.Length; i++)
        {
            if (rejas[i] == null || rejas[i].estaAbierta) continue;
            Collider2D[] colsReja = rejas[i].GetComponentsInChildren<Collider2D>(true);
            for (int j = 0; j < colsReja.Length; j++)
            {
                if (colsReja[j] != null && !colsReja[j].isTrigger)
                {
                    Physics2D.IgnoreCollision(col, colsReja[j], false);
                }
            }
        }

        RataSubterraneaNivel2[] ratas = FindObjectsByType<RataSubterraneaNivel2>(FindObjectsSortMode.None);
        for (int i = 0; i < ratas.Length; i++)
        {
            if (ratas[i] == null) continue;
            Collider2D colRata = ratas[i].GetComponent<Collider2D>();
            if (colRata != null)
            {
                Physics2D.IgnoreCollision(col, colRata, true);
            }
        }
    }

    private void EscucharRuidoDelEntornoOMicrofono(Vector3 posicionRuido, float radioBase, string etiqueta)
    {
        if (!Application.isPlaying || Time.timeScale == 0f) return;

        // Periodo de gracia inicial de 3.5 segundos al cargar el nivel para evitar alertas automáticas al arrancar
        if (Time.timeSinceLevelLoad < 3.5f) return;

        if (DetectorMicrofonoNivel2.VerificarVaporActivoCercano(posicionRuido, 7.5f) ||
            DetectorMicrofonoNivel2.VerificarVaporActivoCercano(transform.position, 6.2f))
        {
            return;
        }

        bool esSonidoFuerteOMic = radioBase >= 11f ||
                                  (etiqueta != null && (etiqueta.Contains("MICRÓFONO") || etiqueta.Contains("Corriendo") || etiqueta.Contains("FUSIBLE")));

        float distanciaAlRuido = Vector2.Distance(transform.position, posicionRuido);
        // El eco acústico se propaga por las tuberías de alcantarillado subterráneo entre todas las salas
        float radioEfectivo = Mathf.Max(radioBase * multiplicadorAudicion, esSonidoFuerteOMic ? 54f : 32f);

        if (distanciaAlRuido > radioEfectivo)
        {
            return;
        }

        ultimaPosicionSonido = posicionRuido;
        ultimoSonidoEscuchado = string.IsNullOrEmpty(etiqueta) ? "Ruido detectado" : etiqueta;

        // Si el sonido proviene de entrar/salir de un escondite, detectarlo y fijarlo como objetivo
        bool esRuidoEscondite = etiqueta != null && (etiqueta.Contains("ESCONDITE") || etiqueta.Contains("TAQUILLA") || etiqueta.Contains("CRIPTA") || etiqueta.Contains("LOSA") || etiqueta.Contains("APERTURA"));
        if (esRuidoEscondite)
        {
            EsconditeNivel2 escCercano = null;
            float menorDist = 4.5f;
            EsconditeNivel2[] todosEscondites = FindObjectsByType<EsconditeNivel2>(FindObjectsSortMode.None);
            for (int i = 0; i < todosEscondites.Length; i++)
            {
                if (todosEscondites[i] == null) continue;
                float d = Vector2.Distance(todosEscondites[i].transform.position, posicionRuido);
                if (d < menorDist)
                {
                    menorDist = d;
                    escCercano = todosEscondites[i];
                }
            }

            if (escCercano != null && escCercano.EstaOcupado)
            {
                esconditeComprometido = escCercano;
                jugadorDetectadoAlEsconderse = true;
                ultimaPosicionSonido = escCercano.transform.position;
                ultimoSonidoEscuchado = "¡LA MOMIA ESCUCHÓ TU ESCONDITE!";
            }
        }

        timerInvestigacionRestante = esSonidoFuerteOMic ? 14.5f : 10.5f;
        timerBusquedaLocal = 0f;
        puntoBusquedaLocal = Vector2.zero;

        Vector2 dirAlRuido = ((Vector2)posicionRuido - (Vector2)transform.position);
        if (dirAlRuido.sqrMagnitude > 0.01f)
        {
            direccionVisual = dirAlRuido.normalized;
        }

        GenerarOndaAcusticaMomia(transform.position + new Vector3(0f, 1.0f, 0f), 3.6f, new Color(0.95f, 0.18f, 0.14f, 0.78f));

        if (estadoActual == EstadoMomia2.AcechandoEnOscuridad)
        {
            estadoActual = EstadoMomia2.AlertaAcusticaGrito;
            timerAlertaGrito = 0.22f;
            tiempoUltimoRugidoAlerta = Time.time;

            if (fuenteAudioPuntual != null && clipRugidoDeteccion != null && !fuenteAudioPuntual.isPlaying)
            {
                float aten = Mathf.Clamp01(1f - (distanciaAlJugador / 34f));
                fuenteAudioPuntual.pitch = Random.Range(0.92f, 1.04f);
                fuenteAudioPuntual.PlayOneShot(clipRugidoDeteccion, Mathf.Max(0.38f, aten * 0.88f));
            }
        }
        else if (estadoActual != EstadoMomia2.CazandoContactoLetal)
        {
            // Si ya está investigando, NO frena a 0: continúa persiguiendo el nuevo punto de sonido
            estadoActual = EstadoMomia2.InvestigandoSonido;
        }

        // Si la Momia está en otra sala separada por rejas cerradas del lugar donde hiciste ruido,
        // viaja inmediatamente por las alcantarillas hacia la sala del sonido en vez de dar vueltas
        bool hayRutaAbiertaAlSonido = TieneRutaAbiertaSinRejasCerradas(transform.position, posicionRuido);
        if (!hayRutaAbiertaAlSonido)
        {
            bool recienEncerradaEnTuCara = EstaGolpeandoRejaEncerrada && distanciaAlJugador <= 6.0f && timerEncerradaSinRuta < 2.2f;
            if (!recienEncerradaEnTuCara)
            {
                if (faseAlcantarilla == FaseAlcantarilla.Ninguna)
                {
                    IniciarViajePorAlcantarilla(posicionRuido, true);
                }
                else if (faseAlcantarilla == FaseAlcantarilla.CaminandoHaciaAlcantarilla ||
                         faseAlcantarilla == FaseAlcantarilla.SumergiendoseEnDrenaje)
                {
                    nodoAlcantarillaDestino = EscogerAlcantarillaDestino(nodoAlcantarillaOrigen, posicionRuido, true);
                }
            }
        }
    }

    private bool TieneRutaAbiertaSinRejasCerradas(Vector2 posOrigen, Vector2 posDestino)
    {
        if (!HayMuroORejaCerradaEntrePuntos(posOrigen, posDestino, 0.30f)) return true;
        int nodoInicio = ObtenerMejorNodoAccesible(posOrigen);
        int nodoMeta = ObtenerMejorNodoAccesible(posDestino);
        if (nodoInicio == nodoMeta) return true;
        bool[] alcanzables = CalcularNodosAlcanzablesSinCruzarRejasCerradas(nodoInicio);
        return alcanzables[nodoMeta];
    }

    private void Update()
    {
        if (!EstaUsandoAlcantarilla)
        {
            transform.localScale = Vector3.one;
        }
        transform.localRotation = Quaternion.identity;

        if (!Application.isPlaying)
        {
            if (sr != null && sr.sprite == null) CargarSpritesMomia2();
            return;
        }

        if (Time.timeScale == 0f)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (srEfectoZarpazo != null) srEfectoZarpazo.enabled = false;
            return;
        }

        LocalizarJugador();
        ActualizarEscaladoPorFasesElectricas();

        timerIgnorarObstaculos += Time.deltaTime;
        if (timerIgnorarObstaculos >= 1.5f)
        {
            timerIgnorarObstaculos = 0f;
            IgnorarColisionesSoloConRatas();
        }

        if (EstaUsandoAlcantarilla)
        {
            ActualizarViajePorAlcantarilla();
            ActualizarAnimacionPorFrames();
            ActualizarEfectoVisualZarpazo();
            ActualizarOndasVisuales();
            return;
        }

        ActualizarRelacionConJugadorYEscondites();
        ActualizarAudioCercaniaMomia();
        ActualizarMaquinaEstadosIA();
        ActualizarAnimacionPorFrames();
        ActualizarEfectoVisualZarpazo();
        ActualizarOndasVisuales();
    }

    private void ActualizarAudioCercaniaMomia()
    {
        if (clipCercaniaMomia == null)
        {
            clipCercaniaMomia = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_SONIDO_MOMIA);
            if (clipCercaniaMomia == null) clipCercaniaMomia = Resources.Load<AudioClip>("Audio/Nivel2/sonido_momia");
            if (clipCercaniaMomia == null) clipCercaniaMomia = Resources.Load<AudioClip>("AudioNivel2/sonido_momia");
            if (clipCercaniaMomia == null) clipCercaniaMomia = Resources.Load<AudioClip>("sonido_momia");
        }

        if (clipZarpazoRasguno == null || clipZarpazoRasguno.name != AudioManagerNivel2.CLIP_ATAQUE)
        {
            AudioClip cAtaque = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_ATAQUE);
            if (cAtaque != null)
            {
                clipZarpazoRasguno = cAtaque;
                clipAtaqueCercano = cAtaque;
            }
        }

        // Si la momia está usando la alcantarilla o el juego está en pausa, no emitir sonido
        if (EstaUsandoAlcantarilla || Time.timeScale == 0f) return;

        // La momia emite su sonido característico ("sonido momia.mp3") periódicamente ("de vez en cuando")
        // haciéndose más frecuente e intenso conforme se aproxima a la jugadora y cubriendo todo el subsuelo
        timerSonidoMomiaDeVezEnCuando -= Time.deltaTime;
        if (timerSonidoMomiaDeVezEnCuando <= 0f)
        {
            float dist = distanciaAlJugador;
            // Intervalo "de vez en cuando": más frecuente cerca (2.5s a 4.2s) y más espaciado lejos (3.8s a 6.5s)
            float tMin = dist < 12f ? 2.5f : 3.8f;
            float tMax = dist < 12f ? 4.2f : 6.5f;
            timerSonidoMomiaDeVezEnCuando = UnityEngine.Random.Range(tMin, tMax);

            if (clipCercaniaMomia != null)
            {
                // Curva de volumen 2D que se escucha en TODO el subsuelo (eco siniestro a la distancia y rugido al acercarse)
                float vol;
                if (dist < 6f)
                {
                    vol = UnityEngine.Random.Range(0.85f, 1.0f); // Muy cerca: aterrador
                }
                else if (dist < 15f)
                {
                    vol = Mathf.Lerp(0.85f, 0.50f, (dist - 6f) / 9f);
                }
                else if (dist < 30f)
                {
                    vol = Mathf.Lerp(0.50f, 0.32f, (dist - 15f) / 15f);
                }
                else
                {
                    vol = Mathf.Lerp(0.32f, 0.20f, Mathf.Clamp01((dist - 30f) / 25f)); // Lejos: eco siniestro
                }

                AudioSource fuente = fuenteAudioPuntual != null ? fuenteAudioPuntual : GetComponent<AudioSource>();
                if (fuente == null)
                {
                    fuente = gameObject.AddComponent<AudioSource>();
                    fuente.playOnAwake = false;
                    fuenteAudioPuntual = fuente;
                }

                if (fuente != null)
                {
                    fuente.spatialBlend = 0f; // Asegurar estéreo directo sin problemas de Z
                    fuente.pitch = UnityEngine.Random.Range(0.92f, 1.06f);
                    fuente.PlayOneShot(clipCercaniaMomia, Mathf.Clamp01(vol));
                }
            }
        }
    }

    /// <summary>
    /// Aumenta progresivamente la velocidad de la Momia cada vez que se enciende una Fase Eléctrica (1/3, 2/3, 3/3).
    /// Al alcanzar las 3 fases (3/3), activa el Modo Asedio de Salida para intentar impedir el escape en la Subestación Norte,
    /// manteniéndolo 100% lograble si la Guardia la esquiva rodeando el Generador Central o la distrae con ruido.
    /// </summary>
    private void ActualizarEscaladoPorFasesElectricas()
    {
        int fases = GeneradorPrincipalNivel2.Instance != null ? GeneradorPrincipalNivel2.Instance.fasesRestauradas : 0;
        FasesElectricasActivas = fases;

        switch (fases)
        {
            case 0:
                velocidadPatrulla = 3.05f;
                velocidadInvestigacion = 5.55f;
                velocidadFrenesiGrito = 7.15f;
                multiplicadorAudicion = 1.05f;
                break;
            case 1:
                velocidadPatrulla = 3.85f;
                velocidadInvestigacion = 6.35f;
                velocidadFrenesiGrito = 7.95f;
                multiplicadorAudicion = 1.18f;
                break;
            case 2:
                velocidadPatrulla = 4.70f;
                velocidadInvestigacion = 7.15f;
                velocidadFrenesiGrito = 8.75f;
                multiplicadorAudicion = 1.32f;
                break;
            default: // 3 fases activas: ¡Impedir el escape a toda costa (pero superable por el jugador)!
                velocidadPatrulla = 6.25f;
                velocidadInvestigacion = 8.30f;
                velocidadFrenesiGrito = 9.60f;
                multiplicadorAudicion = 1.50f;
                break;
        }

        if (fases > ultimaFaseProcesada)
        {
            ultimaFaseProcesada = fases;

            if (fuenteAudioPuntual != null && clipRugidoDeteccion != null)
            {
                fuenteAudioPuntual.pitch = 0.90f + fases * 0.06f;
                fuenteAudioPuntual.PlayOneShot(clipRugidoDeteccion, 0.92f);
            }
            GenerarOndaAcusticaMomia(transform.position + new Vector3(0f, 1.0f, 0f), 4.2f, new Color(1.0f, 0.25f, 0.12f, 0.85f));

            if (fases >= 3)
            {
                ultimoSonidoEscuchado = "¡3/3 FASES! ¡Intentando bloquear tu escape en la Subestación Norte!";
                // Si está en otra sala cerrada lejos de la Subestación Norte, viaja por la alcantarilla para custodiar la salida
                Vector2 posSubestacion = NodosGrafo[20]; // (0, 10.4) Subestación Norte
                if (!TieneRutaAbiertaSinRejasCerradas(transform.position, posSubestacion) ||
                    transform.position.y < 3.0f)
                {
                    ultimaPosicionSonido = NodosGrafo[23]; // Frente a la Compuerta Norte (0, 18.2)
                    timerInvestigacionRestante = 11.0f;
                    estadoActual = EstadoMomia2.InvestigandoSonido;
                    if (faseAlcantarilla == FaseAlcantarilla.Ninguna)
                    {
                        IniciarViajePorAlcantarilla(posSubestacion, true);
                    }
                }
            }
            else
            {
                ultimoSonidoEscuchado = $"¡Fase Eléctrica {fases}/3 encendida! Velocidad aumentada";
            }
        }
    }

    private void LateUpdate()
    {
        if (sr != null)
        {
            float footY = transform.position.y - 0.85f;
            sr.sortingOrder = 500 + Mathf.RoundToInt(-footY * 10f);
            if (sombraSr != null)
            {
                sombraSr.sortingOrder = sr.sortingOrder - 2;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying || Time.timeScale == 0f)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        EjecutarMovimientoSubterraneo();
    }

    private void ActualizarRelacionConJugadorYEscondites()
    {
        if (playerTransform == null)
        {
            distanciaAlJugador = 99f;
            return;
        }

        distanciaAlJugador = Vector2.Distance(transform.position, playerTransform.position);

        EsconditeNivel2 esconditeActual = ObtenerEsconditeOcupado();

        if (esconditeActual != null)
        {
            EstaEjecutandoAtaqueRasguno = false;
            TiempoRasgunandoActual = 0f;

            if (esconditeComprometido == null &&
                (estadoActual == EstadoMomia2.AlertaAcusticaGrito ||
                 estadoActual == EstadoMomia2.InvestigandoSonido ||
                 estadoActual == EstadoMomia2.CazandoContactoLetal) &&
                distanciaAlJugador <= 6.2f &&
                timerInvestigacionRestante > 6.5f &&
                !HayMuroORejaCerradaEntrePuntos(transform.position, esconditeActual.transform.position, 0.22f))
            {
                esconditeComprometido = esconditeActual;
                jugadorDetectadoAlEsconderse = true;
                ultimaPosicionSonido = esconditeActual.transform.position;
                ultimoSonidoEscuchado = "¡TE ESCUCHÓ ENTRAR AL ESCONDITE!";
            }

            if (esconditeComprometido != null &&
                Vector2.Distance(transform.position, esconditeComprometido.transform.position) <= 2.2f &&
                !HayMuroORejaCerradaEntrePuntos(transform.position, esconditeComprometido.transform.position, 0.20f))
            {
                esconditeComprometido.SalirEscondite();
                esconditeComprometido = null;
                jugadorDetectadoAlEsconderse = false;
                estadoActual = EstadoMomia2.CazandoContactoLetal;
                ultimoSonidoEscuchado = "¡LA MOMIA TE SACÓ DEL ESCONDITE!";
            }
            return;
        }
        else
        {
            esconditeComprometido = null;
            jugadorDetectadoAlEsconderse = false;
        }

        // La Momia es 100% ciega ("La Que Escucha"): escucha los pasos de quien camine o corra de pie.
        // Para pasar a su lado sin ser detectado y asesinado, la vigilante DEBE agacharse en Modo Sigilo.
        bool guardiaEnSigilo = SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo;
        bool separadasPorMuroOReja = HayMuroORejaCerradaEntrePuntos(transform.position, playerTransform.position, 0.20f);

        if (!separadasPorMuroOReja)
        {
            // Si la Guardia pasa a un lado de la Momia (distancia <= 4.8m) y NO está agachada en sigilo,
            // la Momia escucha sus pisadas de inmediato y la ataca letalmente.
            Rigidbody2D pRb = playerTransform != null ? playerTransform.GetComponent<Rigidbody2D>() : null;
            bool seMueveDePie = !guardiaEnSigilo && pRb != null && pRb.linearVelocity.sqrMagnitude > 0.05f;

            if (seMueveDePie && distanciaAlJugador <= 4.8f)
            {
                ultimaPosicionSonido = playerTransform.position;
                timerInvestigacionRestante = Mathf.Max(timerInvestigacionRestante, 8.0f);
                if (estadoActual != EstadoMomia2.CazandoContactoLetal)
                {
                    estadoActual = EstadoMomia2.CazandoContactoLetal;
                    ultimoSonidoEscuchado = "¡PASOS DETECTADOS AL LADO DE LA MOMIA!";
                }
            }
            else
            {
                // Contacto físico directo cuerpo a cuerpo (en sigilo solo si chocas a < 0.65m)
                float radioContactoEfectivo = guardiaEnSigilo ? 0.65f : radioDeteccionContacto;
                if (distanciaAlJugador <= radioContactoEfectivo)
                {
                    ultimaPosicionSonido = playerTransform.position;
                    timerInvestigacionRestante = Mathf.Max(timerInvestigacionRestante, 6.0f);
                    if (estadoActual != EstadoMomia2.CazandoContactoLetal)
                    {
                        estadoActual = EstadoMomia2.CazandoContactoLetal;
                        ultimoSonidoEscuchado = "¡CONTACTO FÍSICO CON LA MOMIA!";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gestiona el viaje de la Momia por las Rejillas de Alcantarilla cuando ha quedado encerrada
    /// tras rejas mecánicas cerradas o cuando escucha un ruido en otra sala cerrada.
    /// </summary>
    private void IniciarViajePorAlcantarilla(Vector2 destinoDeseado, bool buscandoSonido = false)
    {
        EstaGolpeandoRejaEncerrada = false;
        rejaQueLaEncierra = null;
        timerEncerradaSinRuta = 0f;

        bool enModoSonido = buscandoSonido || estadoActual != EstadoMomia2.AcechandoEnOscuridad;
        nodoAlcantarillaOrigen = ObtenerAlcantarillaAccesibleEnSalaActual(transform.position);
        nodoAlcantarillaDestino = EscogerAlcantarillaDestino(nodoAlcantarillaOrigen, destinoDeseado, enModoSonido);

        faseAlcantarilla = FaseAlcantarilla.CaminandoHaciaAlcantarilla;
        timerFaseAlcantarilla = 0f;
        ultimoSonidoEscuchado = enModoSonido
            ? "¡Rastreando sonido por la red de alcantarillas!"
            : "Cambiando de sector por la alcantarilla de drenaje...";
    }

    private void ActualizarViajePorAlcantarilla()
    {
        EstaEjecutandoAtaqueRasguno = false;
        TiempoRasgunandoActual = 0f;
        EstaGolpeandoRejaEncerrada = false;

        bool buscandoSonido = estadoActual != EstadoMomia2.AcechandoEnOscuridad;

        switch (faseAlcantarilla)
        {
            case FaseAlcantarilla.CaminandoHaciaAlcantarilla:
                velocidadObjetivoActual = buscandoSonido ? velocidadFrenesiGrito : velocidadInvestigacion;
                Vector2 posGrateOrigen = NodosGrafo[nodoAlcantarillaOrigen];
                timerFaseAlcantarilla += Time.deltaTime;

                if (Vector2.Distance(transform.position, posGrateOrigen) <= 0.95f || timerFaseAlcantarilla > 5.5f)
                {
                    rb.position = posGrateOrigen;
                    rb.linearVelocity = Vector2.zero;
                    faseAlcantarilla = FaseAlcantarilla.SumergiendoseEnDrenaje;
                    timerFaseAlcantarilla = 0f;
                    ultimoSonidoEscuchado = buscandoSonido
                        ? "¡Viajando por la alcantarilla hacia el origen del sonido!"
                        : "¡La Momia se sumerge por la alcantarilla!";

                    if (fuenteAudioPuntual != null && clipAlcantarillaDrenaje != null)
                    {
                        fuenteAudioPuntual.pitch = 0.92f;
                        fuenteAudioPuntual.PlayOneShot(clipAlcantarillaDrenaje, 0.85f);
                    }
                    GenerarOndaAcusticaMomia(posGrateOrigen, 3.8f, new Color(0.35f, 0.78f, 0.65f, 0.75f));
                }
                break;

            case FaseAlcantarilla.SumergiendoseEnDrenaje:
                velocidadObjetivoActual = 0f;
                rb.linearVelocity = Vector2.zero;
                timerFaseAlcantarilla += Time.deltaTime;
                float duracionSub = buscandoSonido ? 1.05f : 1.30f;
                float tSub = Mathf.Clamp01(timerFaseAlcantarilla / duracionSub);
                float escalaSub = Mathf.Lerp(1.0f, 0.12f, tSub);
                transform.localScale = new Vector3(escalaSub, escalaSub, 1f);

                if (timerFaseAlcantarilla >= duracionSub)
                {
                    // Revalidar que la alcantarilla destino sigue siendo la óptima hacia el último sonido
                    if (buscandoSonido)
                    {
                        nodoAlcantarillaDestino = EscogerAlcantarillaDestino(nodoAlcantarillaOrigen, ultimaPosicionSonido, true);
                    }

                    Vector2 posGrateDestino = NodosGrafo[nodoAlcantarillaDestino];
                    rb.position = posGrateDestino;
                    transform.position = new Vector3(posGrateDestino.x, posGrateDestino.y, 0f);
                    posicionChequeoAtasco = posGrateDestino;

                    faseAlcantarilla = FaseAlcantarilla.EmergiendoDeDrenaje;
                    timerFaseAlcantarilla = 0f;
                    ultimoSonidoEscuchado = buscandoSonido
                        ? "¡La Momia emergió de la alcantarilla buscando tu sonido!"
                        : "¡La Momia emergió de una alcantarilla!";

                    if (fuenteAudioPuntual != null && clipAlcantarillaDrenaje != null)
                    {
                        fuenteAudioPuntual.pitch = 1.06f;
                        fuenteAudioPuntual.PlayOneShot(clipAlcantarillaDrenaje, 0.90f);
                    }
                    GenerarOndaAcusticaMomia(posGrateDestino, 4.2f, new Color(0.95f, 0.45f, 0.18f, 0.80f));
                }
                break;

            case FaseAlcantarilla.EmergiendoDeDrenaje:
                velocidadObjetivoActual = 0f;
                rb.linearVelocity = Vector2.zero;
                timerFaseAlcantarilla += Time.deltaTime;
                float duracionEm = buscandoSonido ? 0.95f : 1.10f;
                float tEm = Mathf.Clamp01(timerFaseAlcantarilla / duracionEm);
                float escalaEm = Mathf.Lerp(0.12f, 1.0f, tEm);
                transform.localScale = new Vector3(escalaEm, escalaEm, 1f);

                if (timerFaseAlcantarilla >= duracionEm)
                {
                    transform.localScale = Vector3.one;
                    faseAlcantarilla = FaseAlcantarilla.Ninguna;
                    timerEncerradaSinRuta = 0f;

                    if (buscandoSonido)
                    {
                        estadoActual = EstadoMomia2.InvestigandoSonido;
                        timerInvestigacionRestante = Mathf.Max(timerInvestigacionRestante, 10.5f);
                        timerBusquedaLocal = 0f;
                        puntoBusquedaLocal = Vector2.zero;
                    }

                    // Ajustar índice de patrulla al nuevo sector donde emergió
                    for (int i = 0; i < RutaNodosPatrulla.Length; i++)
                    {
                        if (RutaNodosPatrulla[i] == nodoAlcantarillaDestino)
                        {
                            indicePatrullaActual = (i + 1) % RutaNodosPatrulla.Length;
                            break;
                        }
                    }
                }
                break;
        }
    }

    private int ObtenerAlcantarillaAccesibleEnSalaActual(Vector2 posActual)
    {
        int nodoInicio = ObtenerMejorNodoAccesible(posActual);
        bool[] alcanzables = CalcularNodosAlcanzablesSinCruzarRejasCerradas(nodoInicio);

        int mejorGrate = NodosAlcantarillasSalas[0];
        float mejorDist = float.MaxValue;

        for (int i = 0; i < NodosAlcantarillasSalas.Length; i++)
        {
            int nGrate = NodosAlcantarillasSalas[i];
            if (alcanzables[nGrate])
            {
                float d = Vector2.Distance(posActual, NodosGrafo[nGrate]);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    mejorGrate = nGrate;
                }
            }
        }

        if (mejorDist < float.MaxValue) return mejorGrate;

        // Si por alguna razón ninguna estaba marcada, devolver la más cercana en distancia
        for (int i = 0; i < NodosAlcantarillasSalas.Length; i++)
        {
            int nGrate = NodosAlcantarillasSalas[i];
            float d = Vector2.Distance(posActual, NodosGrafo[nGrate]);
            if (d < mejorDist)
            {
                mejorDist = d;
                mejorGrate = nGrate;
            }
        }
        return mejorGrate;
    }

    private int EscogerAlcantarillaDestino(int grateOrigen, Vector2 destinoObjetivo, bool buscandoSonido = false)
    {
        bool enBusquedaSonido = buscandoSonido || estadoActual != EstadoMomia2.AcechandoEnOscuridad;

        if (enBusquedaSonido)
        {
            // MODO RASTREO DE SONIDO:
            // Priorizar la alcantarilla que esté DENTRO de la misma sala/sector accesible donde se produjo el sonido
            int nodoSonido = ObtenerMejorNodoAccesible(destinoObjetivo);
            bool[] mismoSectorSonido = CalcularNodosAlcanzablesSinCruzarRejasCerradas(nodoSonido);

            int mejorGrateSector = -1;
            float mejorDistSector = float.MaxValue;

            for (int i = 0; i < NodosAlcantarillasSalas.Length; i++)
            {
                int candidato = NodosAlcantarillasSalas[i];
                if (candidato == grateOrigen) continue;
                if (!mismoSectorSonido[candidato]) continue;

                float dObj = Vector2.Distance(NodosGrafo[candidato], destinoObjetivo);
                if (dObj < mejorDistSector)
                {
                    mejorDistSector = dObj;
                    mejorGrateSector = candidato;
                }
            }

            if (mejorGrateSector != -1)
            {
                return mejorGrateSector;
            }

            // Si por alguna razón no había otra alcantarilla en el sector, elegir la alcantarilla más cercana al sonido
            int mejorGrateCercano = NodosAlcantarillasSalas[0];
            float menorDistAlSonido = float.MaxValue;
            for (int i = 0; i < NodosAlcantarillasSalas.Length; i++)
            {
                int candidato = NodosAlcantarillasSalas[i];
                if (candidato == grateOrigen) continue;
                float dObj = Vector2.Distance(NodosGrafo[candidato], destinoObjetivo);
                if (dObj < menorDistAlSonido)
                {
                    menorDistAlSonido = dObj;
                    mejorGrateCercano = candidato;
                }
            }
            return mejorGrateCercano;
        }

        // MODO PATRULLA SILENCIOSA (cuando NO hiciste ruido):
        // Rotar de sector sin emerger encima de la Guardia
        int mejorGrate = NodosAlcantarillasSalas[0];
        float mejorPuntaje = float.MaxValue;

        for (int i = 0; i < NodosAlcantarillasSalas.Length; i++)
        {
            int candidato = NodosAlcantarillasSalas[i];
            if (candidato == grateOrigen) continue;

            Vector2 posCand = NodosGrafo[candidato];
            if (playerTransform != null && Vector2.Distance(posCand, playerTransform.position) < 8.5f)
            {
                continue;
            }

            float dObjetivo = Vector2.Distance(posCand, destinoObjetivo);
            if (dObjetivo < mejorPuntaje)
            {
                mejorPuntaje = dObjetivo;
                mejorGrate = candidato;
            }
        }

        if (mejorGrate == grateOrigen || mejorPuntaje == float.MaxValue)
        {
            float maxDistJugador = -1f;
            for (int i = 0; i < NodosAlcantarillasSalas.Length; i++)
            {
                int cand = NodosAlcantarillasSalas[i];
                if (cand == grateOrigen) continue;
                float dJ = playerTransform != null ? Vector2.Distance(NodosGrafo[cand], playerTransform.position) : 20f;
                if (dJ > maxDistJugador)
                {
                    maxDistJugador = dJ;
                    mejorGrate = cand;
                }
            }
        }
        return mejorGrate;
    }

    private void ActualizarMaquinaEstadosIA()
    {
        switch (estadoActual)
        {
            case EstadoMomia2.AcechandoEnOscuridad:
                EstaEjecutandoAtaqueRasguno = false;
                TiempoRasgunandoActual = 0f;
                EstaGolpeandoRejaEncerrada = false;
                velocidadObjetivoActual = velocidadPatrulla;
                timerCambioPatrulla += Time.deltaTime;

                if (FasesElectricasActivas >= 3)
                {
                    // MODO ASEDIO DE SALIDA (3/3 fases):
                    // Patrulla a alta velocidad custodiando los accesos de la Subestación Norte y la Compuerta de Salida (23),
                    // pero moviéndose continuamente en circuito alrededor del Generador Central para que la Guardia pueda esquivarla.
                    ultimoSonidoEscuchado = "¡3/3 FASES! Custodiando accesos a la Compuerta Norte...";
                    int nodoAsedioIdx = NodosAsedioSalida[indiceAsedioSalida % NodosAsedioSalida.Length];
                    Vector2 destinoAsedio = NodosGrafo[nodoAsedioIdx];
                    if (Vector2.Distance(transform.position, destinoAsedio) <= 1.20f || timerCambioPatrulla > 4.5f)
                    {
                        timerCambioPatrulla = 0f;
                        indiceAsedioSalida = (indiceAsedioSalida + 1) % NodosAsedioSalida.Length;
                    }
                }
                else
                {
                    int nodoPatrullaIdx = RutaNodosPatrulla[indicePatrullaActual % RutaNodosPatrulla.Length];
                    Vector2 destinoPatrulla = NodosGrafo[nodoPatrullaIdx];
                    if (Vector2.Distance(transform.position, destinoPatrulla) <= 1.15f || timerCambioPatrulla > 10f)
                    {
                        timerCambioPatrulla = 0f;
                        indicePatrullaActual = (indicePatrullaActual + 1) % RutaNodosPatrulla.Length;
                    }
                }
                break;

            case EstadoMomia2.AlertaAcusticaGrito:
                EstaEjecutandoAtaqueRasguno = false;
                TiempoRasgunandoActual = 0f;
                velocidadObjetivoActual = 0f;
                timerAlertaGrito -= Time.deltaTime;
                if (timerAlertaGrito <= 0f)
                {
                    estadoActual = EstadoMomia2.InvestigandoSonido;
                }
                break;

            case EstadoMomia2.InvestigandoSonido:
                EstaEjecutandoAtaqueRasguno = false;
                TiempoRasgunandoActual = 0f;

                if (DetectorMicrofonoNivel2.VerificarVaporActivoCercano(transform.position, 5.8f))
                {
                    timerInvestigacionRestante -= Time.deltaTime * 2.5f;
                    ultimoSonidoEscuchado = "Rastro acústico confundido por el vapor";
                }

                timerInvestigacionRestante -= Time.deltaTime;
                float distAlOrigenRuido = Vector2.Distance(transform.position, ultimaPosicionSonido);

                if (puntoBusquedaLocal == Vector2.zero && distAlOrigenRuido > 1.40f)
                {
                    velocidadObjetivoActual = timerInvestigacionRestante > 7.5f ? velocidadFrenesiGrito : velocidadInvestigacion;
                }
                else
                {
                    // Ya llegó al punto exacto donde sonó el ruido: ahora inspecciona los rincones de esa misma sala
                    velocidadObjetivoActual = velocidadInvestigacion * 0.82f;
                    timerBusquedaLocal -= Time.deltaTime;
                    bool llegoAPuntoLocal = puntoBusquedaLocal != Vector2.zero && Vector2.Distance(transform.position, puntoBusquedaLocal) <= 1.05f;
                    if (puntoBusquedaLocal == Vector2.zero || llegoAPuntoLocal || timerBusquedaLocal <= 0f)
                    {
                        timerBusquedaLocal = 2.6f;
                        int nodoCentroRuido = ObtenerMejorNodoAccesible(ultimaPosicionSonido);
                        int[] vecinosSala = VecinosGrafo[nodoCentroRuido];
                        int nodoElegido = nodoCentroRuido;
                        for (int k = 0; k < vecinosSala.Length; k++)
                        {
                            int idxVec = vecinosSala[( k + Random.Range(0, vecinosSala.Length) ) % vecinosSala.Length];
                            if (ObtenerRejaCerradaEnArista(nodoCentroRuido, idxVec) == null)
                            {
                                nodoElegido = idxVec;
                                break;
                            }
                        }
                        puntoBusquedaLocal = NodosGrafo[nodoElegido] + Random.insideUnitCircle * 1.15f;
                        ultimoSonidoEscuchado = "Buscando el origen del sonido en la sala...";
                    }
                }

                if (timerInvestigacionRestante <= 0f)
                {
                    EstaGolpeandoRejaEncerrada = false;
                    puntoBusquedaLocal = Vector2.zero;
                    estadoActual = EstadoMomia2.AcechandoEnOscuridad;
                    ultimoSonidoEscuchado = "Rastro perdido... acechando en silencio";
                    int nodoActual = ObtenerNodoMasCercano(transform.position);
                    for (int i = 0; i < RutaNodosPatrulla.Length; i++)
                    {
                        if (RutaNodosPatrulla[i] == nodoActual)
                        {
                            indicePatrullaActual = (i + 1) % RutaNodosPatrulla.Length;
                            break;
                        }
                    }
                }
                break;

            case EstadoMomia2.CazandoContactoLetal:
                bool jugadorOculto = DetectorMicrofonoNivel2.VerificarSiJugadorEstaEscondido();
                if (jugadorOculto && esconditeComprometido == null)
                {
                    EstaEjecutandoAtaqueRasguno = false;
                    TiempoRasgunandoActual = 0f;
                    estadoActual = EstadoMomia2.InvestigandoSonido;
                    timerInvestigacionRestante = 6.0f;
                    break;
                }

                // Solo actualiza la posición de la Guardia mientras la está tocando/rasguñando de cerca (<= 1.85m);
                // si te alejas en silencio, no tiene radar GPS y pierde tu posición exacta.
                if (playerTransform != null && distanciaAlJugador <= 1.85f)
                {
                    ultimaPosicionSonido = playerTransform.position;
                    Vector2 haciaJugadora = (Vector2)playerTransform.position - (Vector2)transform.position;
                    if (haciaJugadora.sqrMagnitude > 0.005f)
                    {
                        direccionVisual = haciaJugadora.normalized;
                    }
                }

                velocidadObjetivoActual = velocidadFrenesiGrito;

                // Solo puede rasguñar a la Guardia si NO hay una reja cerrada o muro interponiéndose
                bool sinRejaDePorMedio = playerTransform != null &&
                                         !HayMuroORejaCerradaEntrePuntos(transform.position, playerTransform.position, 0.20f);

                if (playerScript != null && distanciaAlJugador <= rangoAtaqueLetal && !jugadorOculto && sinRejaDePorMedio)
                {
                    EstaEjecutandoAtaqueRasguno = true;
                    TiempoRasgunandoActual += Time.deltaTime;

                    if (distanciaAlJugador <= 0.92f)
                    {
                        velocidadObjetivoActual = 1.8f;
                    }

                    if (playerRb != null)
                    {
                        playerRb.linearVelocity *= 0.82f;
                    }

                    if (!playerScript.EstaMuerto)
                    {
                        playerScript.RecibirDanio(danioPorSegundo * Time.deltaTime, "momia2");
                    }

                    if (Time.time - tiempoUltimoSonidoRasguno > 0.30f && fuenteAudioPuntual != null && clipZarpazoRasguno != null)
                    {
                        tiempoUltimoSonidoRasguno = Time.time;
                        fuenteAudioPuntual.pitch = Random.Range(0.92f, 1.12f);
                        fuenteAudioPuntual.PlayOneShot(clipZarpazoRasguno, 0.92f);
                    }

                    if (Time.time - tiempoUltimoSonidoAtaque > 1.6f && fuenteAudioPuntual != null && clipAtaqueCercano != null)
                    {
                        tiempoUltimoSonidoAtaque = Time.time;
                        fuenteAudioPuntual.pitch = Random.Range(0.95f, 1.05f);
                        fuenteAudioPuntual.PlayOneShot(clipAtaqueCercano, 0.85f);
                    }
                }
                else
                {
                    EstaEjecutandoAtaqueRasguno = false;
                    TiempoRasgunandoActual = 0f;
                }

                if (distanciaAlJugador > 2.25f || !sinRejaDePorMedio)
                {
                    EstaEjecutandoAtaqueRasguno = false;
                    TiempoRasgunandoActual = 0f;
                    if (distanciaAlJugador > 2.25f)
                    {
                        estadoActual = EstadoMomia2.InvestigandoSonido;
                        timerInvestigacionRestante = 5.0f;
                    }
                }
                break;
        }
    }

    private void EjecutarMovimientoSubterraneo()
    {
        if (faseAlcantarilla == FaseAlcantarilla.SumergiendoseEnDrenaje ||
            faseAlcantarilla == FaseAlcantarilla.EmergiendoDeDrenaje ||
            velocidadObjetivoActual <= 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            timerChequeoAtasco = 0f;
            posicionChequeoAtasco = rb.position;
            return;
        }

        Vector2 destinoFinal;
        if (faseAlcantarilla == FaseAlcantarilla.CaminandoHaciaAlcantarilla && nodoAlcantarillaOrigen >= 0)
        {
            destinoFinal = NodosGrafo[nodoAlcantarillaOrigen];
        }
        else if (estadoActual == EstadoMomia2.AcechandoEnOscuridad)
        {
            if (FasesElectricasActivas >= 3)
            {
                int nodoAsedioIdx = NodosAsedioSalida[indiceAsedioSalida % NodosAsedioSalida.Length];
                destinoFinal = NodosGrafo[nodoAsedioIdx];
            }
            else
            {
                int nodoPatrullaIdx = RutaNodosPatrulla[indicePatrullaActual % RutaNodosPatrulla.Length];
                destinoFinal = NodosGrafo[nodoPatrullaIdx];
            }
        }
        else if (esconditeComprometido != null)
        {
            destinoFinal = esconditeComprometido.transform.position;
        }
        else if (estadoActual == EstadoMomia2.InvestigandoSonido && puntoBusquedaLocal != Vector2.zero)
        {
            destinoFinal = puntoBusquedaLocal;
        }
        else
        {
            destinoFinal = ultimaPosicionSonido;
        }

        Vector2 puntoNavegacion = CalcularSiguienteWaypointGrafo(rb.position, destinoFinal);
        Vector2 haciaPunto = puntoNavegacion - rb.position;

        if (haciaPunto.sqrMagnitude <= 0.03f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dirDeseada = CalcularDireccionDeslizamientoLibre(haciaPunto.normalized);
        direccionMovimientoSuavizada = Vector2.Lerp(direccionMovimientoSuavizada, dirDeseada, Time.fixedDeltaTime * 15f).normalized;
        if (direccionMovimientoSuavizada.sqrMagnitude < 0.01f)
        {
            direccionMovimientoSuavizada = dirDeseada;
        }

        float multiplicadorZarpazo = 1f;
        if (EstaEjecutandoAtaqueRasguno)
        {
            multiplicadorZarpazo = 0.75f + Mathf.Sin(Time.time * 24f) * 0.45f;
        }

        rb.linearVelocity = direccionMovimientoSuavizada * (velocidadObjetivoActual * multiplicadorZarpazo);

        if (rb.linearVelocity.sqrMagnitude > 0.05f && !EstaEjecutandoAtaqueRasguno && !EstaGolpeandoRejaEncerrada)
        {
            direccionVisual = rb.linearVelocity.normalized;
        }

        VerificarYResolverAtasco();
    }

    private void VerificarYResolverAtasco()
    {
        if (EstaUsandoAlcantarilla || EstaEjecutandoAtaqueRasguno || EstaGolpeandoRejaEncerrada)
        {
            timerChequeoAtasco = 0f;
            posicionChequeoAtasco = rb.position;
            return;
        }

        timerChequeoAtasco += Time.fixedDeltaTime;
        if (timerChequeoAtasco < 0.45f) return;

        float avance = Vector2.Distance(rb.position, posicionChequeoAtasco);
        posicionChequeoAtasco = rb.position;
        timerChequeoAtasco = 0f;

        if (avance < 0.16f && velocidadObjetivoActual > 0.5f)
        {
            contadorDesatascosConsecutivos++;
            int nodoLibre = ObtenerMejorNodoAccesible(rb.position);
            Vector2 posNodo = NodosGrafo[nodoLibre];
            Vector2 haciaNodo = posNodo - rb.position;

            if (haciaNodo.sqrMagnitude > 0.02f && !HayMuroORejaCerradaEntrePuntos(rb.position, rb.position + haciaNodo.normalized * 0.4f, 0.20f))
            {
                rb.position += haciaNodo.normalized * 0.32f;
                direccionMovimientoSuavizada = haciaNodo.normalized;
            }

            if (estadoActual == EstadoMomia2.AcechandoEnOscuridad || contadorDesatascosConsecutivos >= 2)
            {
                indicePatrullaActual = (indicePatrullaActual + 1) % RutaNodosPatrulla.Length;
                contadorDesatascosConsecutivos = 0;
            }
        }
        else
        {
            contadorDesatascosConsecutivos = 0;
        }
    }

    private static bool EsAristaDePuertaEntreSalas(int u, int v)
    {
        int a = Mathf.Min(u, v);
        int b = Mathf.Max(u, v);
        return (a == 3 && b == 9) ||   // Reja 1 (Calderas Sur)
               (a == 5 && b == 14) ||  // Reja 2 (Criptas Sur)
               (a == 13 && b == 19) || // Reja 3 (Subestación Oeste)
               (a == 18 && b == 21) || // Reja 4 (Subestación Este)
               (a == 8 && b == 20) ||  // Reja 5 (Subestación Central)
               (a == 1 && b == 6) ||   // Umbral Central Sur
               (a == 1 && b == 2) ||
               (a == 2 && b == 3) ||
               (a == 1 && b == 4) ||
               (a == 4 && b == 5);
    }

    /// <summary>
    /// Devuelve la reja cerrada que bloquea físicamente la arista (u, v), o null si el paso está abierto.
    /// </summary>
    private static PuertaRejaMecanicaNivel2 ObtenerRejaCerradaEnArista(int u, int v)
    {
        if (!EsAristaDePuertaEntreSalas(u, v)) return null;

        Vector2 a = NodosGrafo[u];
        Vector2 b = NodosGrafo[v];
        RaycastHit2D[] hits = Physics2D.LinecastAll(a, b);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null) continue;
            PuertaRejaMecanicaNivel2 reja = c.GetComponent<PuertaRejaMecanicaNivel2>() ?? c.GetComponentInParent<PuertaRejaMecanicaNivel2>();
            if (reja != null && !reja.estaAbierta)
            {
                return reja;
            }
        }
        return null;
    }

    private bool[] CalcularNodosAlcanzablesSinCruzarRejasCerradas(int nodoOrigen)
    {
        const int n = 25;
        bool[] visitado = new bool[n];
        Queue<int> cola = new Queue<int>();
        visitado[nodoOrigen] = true;
        cola.Enqueue(nodoOrigen);

        while (cola.Count > 0)
        {
            int u = cola.Dequeue();
            int[] vecinos = VecinosGrafo[u];
            for (int k = 0; k < vecinos.Length; k++)
            {
                int v = vecinos[k];
                if (visitado[v]) continue;
                if (ObtenerRejaCerradaEnArista(u, v) != null) continue; // Rejas cerradas son 100% impenetrables
                visitado[v] = true;
                cola.Enqueue(v);
            }
        }
        return visitado;
    }

    /// <summary>
    /// Calcula el siguiente punto de paso hacia 'destino' usando Dijkstra sobre el grafo de 25 nodos.
    /// OPCIÓN 2: Las rejas cerradas bloquean por completo el paso. Si la Momia queda encerrada sin ninguna
    /// ruta abierta hacia la sala destino, golpea los barrotes (en persecución) o patrulla su sala y luego
    /// utiliza la Rejilla de Alcantarilla de su sala para emerger en otro sector.
    /// </summary>
    private Vector2 CalcularSiguienteWaypointGrafo(Vector2 origen, Vector2 destino)
    {
        if (!HayMuroORejaCerradaEntrePuntos(origen, destino, 0.34f))
        {
            EstaGolpeandoRejaEncerrada = false;
            return destino;
        }

        int nodoInicio = ObtenerMejorNodoAccesible(origen);
        int nodoMeta = ObtenerMejorNodoAccesible(destino);

        if (nodoInicio == nodoMeta)
        {
            EstaGolpeandoRejaEncerrada = false;
            if (Vector2.Distance(origen, NodosGrafo[nodoInicio]) > 0.55f)
            {
                return NodosGrafo[nodoInicio];
            }
            return destino;
        }

        // Dijkstra tratando cualquier arista con reja cerrada como IMPENETRABLE
        const int n = 25;
        float[] dist = new float[n];
        int[] prev = new int[n];
        bool[] visitado = new bool[n];
        PuertaRejaMecanicaNivel2 rejaFronteraMasCercana = null;
        int nodoFronteraReja = -1;
        float mejorDistFronteraADestino = float.MaxValue;

        for (int i = 0; i < n; i++)
        {
            dist[i] = float.MaxValue;
            prev[i] = -1;
        }
        dist[nodoInicio] = 0f;

        for (int iter = 0; iter < n; iter++)
        {
            int u = -1;
            float mejorD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (!visitado[i] && dist[i] < mejorD)
                {
                    mejorD = dist[i];
                    u = i;
                }
            }

            if (u == -1 || u == nodoMeta) break;
            visitado[u] = true;

            int[] vecinos = VecinosGrafo[u];
            for (int k = 0; k < vecinos.Length; k++)
            {
                int v = vecinos[k];
                if (visitado[v]) continue;

                PuertaRejaMecanicaNivel2 rejaCerrada = ObtenerRejaCerradaEnArista(u, v);
                if (rejaCerrada != null)
                {
                    // Guardar cuál es la reja cerrada que le impide salir hacia el destino
                    float dAlDestino = Vector2.Distance(NodosGrafo[v], destino);
                    if (dAlDestino < mejorDistFronteraADestino)
                    {
                        mejorDistFronteraADestino = dAlDestino;
                        rejaFronteraMasCercana = rejaCerrada;
                        nodoFronteraReja = u;
                    }
                    continue; // ¡No puede atravesar rejas cerradas!
                }

                float peso = Vector2.Distance(NodosGrafo[u], NodosGrafo[v]);
                if (dist[u] + peso < dist[v])
                {
                    dist[v] = dist[u] + peso;
                    prev[v] = u;
                }
            }
        }

        // ¿Quedó ENCERRADA sin ninguna ruta abierta hacia nodoMeta?
        if (dist[nodoMeta] == float.MaxValue)
        {
            if (faseAlcantarilla != FaseAlcantarilla.Ninguna)
            {
                return NodosGrafo[nodoInicio];
            }

            timerEncerradaSinRuta += Time.fixedDeltaTime;

            // Si está en patrulla normal, elegir otro nodo que sí sea alcanzable dentro de su sala actual
            if (estadoActual == EstadoMomia2.AcechandoEnOscuridad)
            {
                EstaGolpeandoRejaEncerrada = false;
                float limiteEsperaPatrulla = (FasesElectricasActivas >= 3) ? 2.2f : 6.5f;
                if (timerEncerradaSinRuta >= limiteEsperaPatrulla)
                {
                    IniciarViajePorAlcantarilla(destino, FasesElectricasActivas >= 3);
                    return NodosGrafo[nodoAlcantarillaOrigen];
                }

                // Avanzar al siguiente nodo de patrulla que sí esté dentro de las salas abiertas actuales
                for (int paso = 1; paso <= RutaNodosPatrulla.Length; paso++)
                {
                    int idxPrueba = (indicePatrullaActual + paso) % RutaNodosPatrulla.Length;
                    int nodoPrueba = RutaNodosPatrulla[idxPrueba];
                    if (dist[nodoPrueba] < float.MaxValue && Vector2.Distance(origen, NodosGrafo[nodoPrueba]) > 1.5f)
                    {
                        indicePatrullaActual = idxPrueba;
                        nodoMeta = nodoPrueba;
                        break;
                    }
                }

                if (dist[nodoMeta] == float.MaxValue)
                {
                    return NodosGrafo[nodoInicio];
                }
            }
            else
            {
                // En alerta o investigando un sonido desde otra sala cerrada:
                // Solo sacude los barrotes durante 2.4s si le acabas de bajar la reja justo en la cara (< 3.6m de la reja y < 6.5m de ti).
                // En cualquier otro caso (cuando haces ruido desde otra sala), viaja INMEDIATAMENTE por las alcantarillas hacia tu sala.
                bool rejaRecienCerradaEnSuCara = rejaFronteraMasCercana != null &&
                                                 nodoFronteraReja != -1 &&
                                                 Vector2.Distance(origen, rejaFronteraMasCercana.transform.position) <= 3.6f &&
                                                 distanciaAlJugador <= 6.5f;

                if (!rejaRecienCerradaEnSuCara || timerEncerradaSinRuta >= 2.4f)
                {
                    IniciarViajePorAlcantarilla(ultimaPosicionSonido, true);
                    return NodosGrafo[nodoAlcantarillaOrigen];
                }

                Vector2 posFrenteAReja = NodosGrafo[nodoFronteraReja];
                float distAFrenteReja = Vector2.Distance(origen, posFrenteAReja);
                if (distAFrenteReja <= 1.35f)
                {
                    EstaGolpeandoRejaEncerrada = true;
                    rejaQueLaEncierra = rejaFronteraMasCercana;
                    ultimoSonidoEscuchado = "¡MOMIA ENCERRADA! Sacudiendo la reja antes de ir a la alcantarilla...";
                    Vector2 haciaReja = (Vector2)rejaFronteraMasCercana.transform.position - origen;
                    if (haciaReja.sqrMagnitude > 0.01f) direccionVisual = haciaReja.normalized;

                    timerSonidoGolpeReja -= Time.fixedDeltaTime;
                    if (timerSonidoGolpeReja <= 0f)
                    {
                        timerSonidoGolpeReja = 1.15f;
                        if (fuenteAudioPuntual != null && clipGolpeRejaEncierro != null)
                        {
                            fuenteAudioPuntual.pitch = Random.Range(0.90f, 1.08f);
                            fuenteAudioPuntual.PlayOneShot(clipGolpeRejaEncierro, 0.78f);
                        }
                        GenerarOndaAcusticaMomia(rejaFronteraMasCercana.transform.position, 2.8f, new Color(0.95f, 0.55f, 0.18f, 0.65f));
                    }
                    return origen;
                }
                else
                {
                    EstaGolpeandoRejaEncerrada = false;
                    nodoMeta = nodoFronteraReja;
                }
            }
        }
        else
        {
            EstaGolpeandoRejaEncerrada = false;
            rejaQueLaEncierra = null;
            timerEncerradaSinRuta = Mathf.Max(0f, timerEncerradaSinRuta - Time.fixedDeltaTime * 0.5f);
        }

        // Reconstruir el primer paso hacia nodoMeta (que ahora es 100% alcanzable por puertas abiertas)
        int pasoActual = nodoMeta;
        int pasoSiguiente = nodoMeta;
        int guardia = 0;
        while (prev[pasoActual] != -1 && prev[pasoActual] != nodoInicio && guardia < n)
        {
            pasoSiguiente = pasoActual;
            pasoActual = prev[pasoActual];
            guardia++;
        }

        bool cruzaPuerta = EsAristaDePuertaEntreSalas(nodoInicio, pasoActual);

        if (cruzaPuerta)
        {
            if (Vector2.Distance(origen, NodosGrafo[nodoInicio]) > 0.65f)
            {
                return NodosGrafo[nodoInicio];
            }
            return NodosGrafo[pasoActual];
        }

        if (Vector2.Distance(origen, NodosGrafo[nodoInicio]) < 0.90f)
        {
            return NodosGrafo[pasoActual];
        }

        if (!HayMuroORejaCerradaEntrePuntos(origen, NodosGrafo[pasoActual], 0.32f))
        {
            if (!EsAristaDePuertaEntreSalas(pasoActual, pasoSiguiente) &&
                pasoSiguiente != pasoActual &&
                !HayMuroORejaCerradaEntrePuntos(origen, NodosGrafo[pasoSiguiente], 0.32f))
            {
                return NodosGrafo[pasoSiguiente];
            }
            return NodosGrafo[pasoActual];
        }

        return NodosGrafo[nodoInicio];
    }

    private int ObtenerMejorNodoAccesible(Vector2 pos)
    {
        int mejorVisible = -1;
        float distMejorVisible = float.MaxValue;
        int mejorAbsoluto = 0;
        float distMejorAbsoluto = float.MaxValue;

        for (int i = 0; i < NodosGrafo.Length; i++)
        {
            float d = Vector2.Distance(pos, NodosGrafo[i]);
            if (d < distMejorAbsoluto)
            {
                distMejorAbsoluto = d;
                mejorAbsoluto = i;
            }
            if (d < distMejorVisible && !HayMuroORejaCerradaEntrePuntos(pos, NodosGrafo[i], 0.26f))
            {
                distMejorVisible = d;
                mejorVisible = i;
            }
        }

        return mejorVisible != -1 ? mejorVisible : mejorAbsoluto;
    }

    private static int ObtenerNodoMasCercano(Vector2 pos)
    {
        int mejor = 0;
        float mejorD = float.MaxValue;
        for (int i = 0; i < NodosGrafo.Length; i++)
        {
            float d = (pos - NodosGrafo[i]).sqrMagnitude;
            if (d < mejorD)
            {
                mejorD = d;
                mejor = i;
            }
        }
        return mejor;
    }

    private bool HayMuroORejaCerradaEntrePuntos(Vector2 a, Vector2 b, float radio)
    {
        Vector2 dir = b - a;
        float dist = dir.magnitude;
        if (dist <= 0.12f) return false;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(a + new Vector2(0f, 0.28f), radio, dir / dist, dist);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (EsObstaculoFisicoSolido(c)) return true;
        }
        return false;
    }

    private Vector2 CalcularDireccionDeslizamientoLibre(Vector2 dirObjetivo)
    {
        if (!HayObstaculoSolidoEnDireccion(dirObjetivo, 0.72f))
        {
            return dirObjetivo;
        }

        float[] angulosPrueba = { 22f, -22f, 45f, -45f, 70f, -70f, 95f, -95f, 125f, -125f, 155f, -155f };
        Vector2 mejorDir = dirObjetivo;
        float mejorPuntaje = -999f;

        for (int i = 0; i < angulosPrueba.Length; i++)
        {
            Vector2 dirRotada = ((Vector2)(Quaternion.Euler(0f, 0f, angulosPrueba[i]) * dirObjetivo)).normalized;
            if (!HayObstaculoSolidoEnDireccion(dirRotada, 0.68f))
            {
                float alineacionMeta = Vector2.Dot(dirRotada, dirObjetivo);
                float alineacionInercia = Vector2.Dot(dirRotada, direccionMovimientoSuavizada);
                float puntaje = alineacionMeta * 1.0f + alineacionInercia * 0.45f;
                if (puntaje > mejorPuntaje)
                {
                    mejorPuntaje = puntaje;
                    mejorDir = dirRotada;
                }
            }
        }

        return mejorDir;
    }

    private bool HayObstaculoSolidoEnDireccion(Vector2 dir, float distancia)
    {
        Vector2 origen = rb != null ? rb.position + new Vector2(0f, 0.28f) : (Vector2)transform.position + new Vector2(0f, 0.28f);
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origen, 0.23f, dir, distancia);
        for (int i = 0; i < hits.Length; i++)
        {
            if (EsObstaculoFisicoSolido(hits[i].collider))
            {
                return true;
            }
        }
        return false;
    }

    private bool EsObstaculoFisicoSolido(Collider2D c)
    {
        if (c == null || c == col || c.isTrigger) return false;
        if (c.CompareTag("Player")) return false;
        if (c.GetComponent<RataSubterraneaNivel2>() != null) return false;

        PuertaRejaMecanicaNivel2 reja = c.GetComponent<PuertaRejaMecanicaNivel2>() ?? c.GetComponentInParent<PuertaRejaMecanicaNivel2>();
        if (reja != null)
        {
            return !reja.estaAbierta;
        }

        return true;
    }

    private void ActualizarAnimacionPorFrames()
    {
        if (sr == null) return;

        float velocidadReal = rb.linearVelocity.magnitude;
        bool enMovimiento = velocidadReal > 0.12f;

        if (direccionVisual.x > 0.12f)
        {
            sr.flipX = false;
        }
        else if (direccionVisual.x < -0.12f)
        {
            sr.flipX = true;
        }

        // 1. Animación de estar rasguñando a la jugadora (o rasguñando los barrotes cuando la encierran)
        if ((EstaEjecutandoAtaqueRasguno || EstaGolpeandoRejaEncerrada) && framesRasgunar != null && framesRasgunar.Length > 0)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 0.065f)
            {
                animTimer = 0f;
                currentFrame = (currentFrame + 1) % framesRasgunar.Length;
            }
            sr.sprite = framesRasgunar[currentFrame % framesRasgunar.Length];
            sr.color = (currentFrame % 2 == 1) ? new Color(1f, 0.85f, 0.85f, 1f) : Color.white;
        }
        // 2. Modo Grito / Persecución Letal
        else if ((estadoActual == EstadoMomia2.AlertaAcusticaGrito || estadoActual == EstadoMomia2.CazandoContactoLetal) &&
                 framesScream != null && framesScream.Length > 0)
        {
            sr.color = Color.white;
            animTimer += Time.deltaTime;
            if (animTimer >= 0.075f)
            {
                animTimer = 0f;
                currentFrame = (currentFrame + 1) % framesScream.Length;
            }
            sr.sprite = framesScream[currentFrame % framesScream.Length];
        }
        // 3. Caminata / Investigación
        else if (enMovimiento)
        {
            sr.color = Color.white;
            bool caminandoDeEspaldas = direccionVisual.y > 0.55f && Mathf.Abs(direccionVisual.y) > Mathf.Abs(direccionVisual.x);
            float intervaloFrame = estadoActual == EstadoMomia2.AcechandoEnOscuridad ? 0.095f : 0.065f;

            animTimer += Time.deltaTime;
            if (animTimer >= intervaloFrame)
            {
                animTimer = 0f;
                currentFrame++;
            }

            if (caminandoDeEspaldas && framesBack != null && framesBack.Length > 0)
            {
                sr.sprite = framesBack[currentFrame % framesBack.Length];
            }
            else if (framesWalk != null && framesWalk.Length > 0)
            {
                sr.sprite = framesWalk[currentFrame % framesWalk.Length];
            }
        }
        else
        {
            sr.color = Color.white;
            currentFrame = 0;
            animTimer = 0f;
            if (poseStalk != null)
            {
                sr.sprite = poseStalk;
            }
        }

        if (luzAuraTerror != null)
        {
            if (EstaEjecutandoAtaqueRasguno || estadoActual == EstadoMomia2.AlertaAcusticaGrito ||
                estadoActual == EstadoMomia2.CazandoContactoLetal || estadoActual == EstadoMomia2.InvestigandoSonido)
            {
                luzAuraTerror.color = Color.Lerp(luzAuraTerror.color, new Color(0.92f, 0.14f, 0.10f), Time.deltaTime * 6f);
                luzAuraTerror.intensity = EstaEjecutandoAtaqueRasguno ? 0.72f : 0.54f;
                luzAuraTerror.pointLightOuterRadius = EstaEjecutandoAtaqueRasguno ? 3.6f : 3.2f;
            }
            else
            {
                luzAuraTerror.color = Color.Lerp(luzAuraTerror.color, new Color(0.52f, 0.35f, 0.22f), Time.deltaTime * 4f);
                luzAuraTerror.intensity = 0.30f;
                luzAuraTerror.pointLightOuterRadius = 2.4f;
            }
        }
    }

    private void ActualizarEfectoVisualZarpazo()
    {
        if (objEfectoZarpazo == null || srEfectoZarpazo == null)
        {
            InicializarEfectoZarpazo();
        }
        if (srEfectoZarpazo == null) return;

        if (!EstaEjecutandoAtaqueRasguno || playerTransform == null || spritesEfectoZarpazo == null || spritesEfectoZarpazo.Length == 0)
        {
            if (srEfectoZarpazo.enabled) srEfectoZarpazo.enabled = false;
            return;
        }

        srEfectoZarpazo.enabled = true;
        timerEfectoZarpazo += Time.deltaTime;
        if (timerEfectoZarpazo >= 0.07f)
        {
            timerEfectoZarpazo = 0f;
            frameZarpazoActual = (frameZarpazoActual + 1) % spritesEfectoZarpazo.Length;
        }

        srEfectoZarpazo.sprite = spritesEfectoZarpazo[frameZarpazoActual];
        float jitterX = Mathf.Sin(Time.time * 48f) * 0.14f;
        float jitterY = Mathf.Cos(Time.time * 54f) * 0.12f;
        objEfectoZarpazo.transform.position = playerTransform.position + new Vector3(jitterX, 0.52f + jitterY, 0f);
        float escalaPulso = 1.15f + Mathf.Sin(Time.time * 36f) * 0.22f;
        objEfectoZarpazo.transform.localScale = new Vector3(escalaPulso, escalaPulso, 1f);
        srEfectoZarpazo.color = new Color(1f, 0.92f, 0.92f, 0.92f);
    }

    public void GenerarOndaAcusticaMomia(Vector3 posicion, float escalaMaxima, Color color)
    {
        if (spriteAnilloOnda == null)
        {
            spriteAnilloOnda = CrearSpriteAnilloOnda();
        }

        GameObject objOnda = new GameObject("OndaEcolocalizacionMomia2");
        objOnda.transform.position = posicion;
        SpriteRenderer srOnda = objOnda.AddComponent<SpriteRenderer>();
        srOnda.sprite = spriteAnilloOnda;
        srOnda.color = color;
        srOnda.sortingOrder = 600;

        ondasActivas.Add(new OndaVisualTerror
        {
            obj = objOnda,
            sr = srOnda,
            tiempo = 0f,
            duracion = 0.65f,
            escalaFinal = escalaMaxima,
            colorBase = color
        });
    }

    private void ActualizarOndasVisuales()
    {
        for (int i = ondasActivas.Count - 1; i >= 0; i--)
        {
            OndaVisualTerror onda = ondasActivas[i];
            if (onda.obj == null)
            {
                ondasActivas.RemoveAt(i);
                continue;
            }

            onda.tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(onda.tiempo / onda.duracion);
            float escala = Mathf.Lerp(0.35f, onda.escalaFinal, 1f - Mathf.Pow(1f - t, 2f));
            onda.obj.transform.localScale = new Vector3(escala, escala, 1f);

            Color c = onda.colorBase;
            c.a = onda.colorBase.a * (1f - t);
            onda.sr.color = c;

            if (t >= 1f)
            {
                Destroy(onda.obj);
                ondasActivas.RemoveAt(i);
            }
        }
    }

    private static Sprite CrearSpriteAnilloOnda()
    {
        const int tam = 96;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 centro = new Vector2(tam * 0.5f, tam * 0.5f);
        float radioMax = tam * 0.46f;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), centro) / radioMax;
                float anilloExterior = Mathf.Exp(-Mathf.Pow((d - 0.88f) * 14f, 2f));
                float anilloInterior = Mathf.Exp(-Mathf.Pow((d - 0.55f) * 18f, 2f)) * 0.45f;
                float a = Mathf.Clamp01(anilloExterior + anilloInterior);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 48f);
    }

    private EsconditeNivel2 ObtenerEsconditeOcupado()
    {
        return DetectorMicrofonoNivel2.ObtenerEsconditeOcupadoCacheado();
    }

    private void InicializarAudioPuntual()
    {
        fuenteAudioPuntual = GetComponent<AudioSource>();
        if (fuenteAudioPuntual == null) fuenteAudioPuntual = gameObject.AddComponent<AudioSource>();
        fuenteAudioPuntual.playOnAwake = false;
        fuenteAudioPuntual.loop = false;
        fuenteAudioPuntual.spatialBlend = 0f;

        fuenteAudioCercania = gameObject.AddComponent<AudioSource>();
        fuenteAudioCercania.playOnAwake = false;
        fuenteAudioCercania.loop = true;
        fuenteAudioCercania.spatialBlend = 1f; // Audio 3D espacializado
        fuenteAudioCercania.minDistance = 2.0f;
        fuenteAudioCercania.maxDistance = 11.5f;
        fuenteAudioCercania.rolloffMode = AudioRolloffMode.Linear;
        fuenteAudioCercania.volume = 0f;

        clipRugidoDeteccion = SintetizarGritoMomia2(0.85f, false);
        clipAtaqueCercano = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_ATAQUE) ?? SintetizarGritoMomia2(0.60f, true);
        clipZarpazoRasguno = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_ATAQUE) ?? SintetizarSonidoRasgunoGarras(0.28f);
        clipGolpeRejaEncierro = SintetizarSonidoGolpeReja(0.55f);
        clipAlcantarillaDrenaje = SintetizarSonidoAlcantarilla(0.75f);
        clipCercaniaMomia = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_SONIDO_MOMIA);
    }

    public static AudioClip SintetizarGritoMomia2(float duracion, bool esAgresivo)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.FloorToInt(sampleRate * duracion);
        float[] samples = new float[sampleCount];

        float baseFreq = esAgresivo ? 82f : 65f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duracion;

            float fm = Mathf.Sin(2f * Mathf.PI * 16f * t) * (esAgresivo ? 14f : 9f);
            float freq = baseFreq + fm;

            float baseWave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.55f +
                             Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.28f;
            float harmonic = Mathf.Sin(2f * Mathf.PI * (esAgresivo ? 420f : 310f) * t) *
                             Mathf.Exp(-Mathf.Pow(prog - 0.35f, 2f) * 14f) * 0.18f;

            float env = Mathf.Sin(prog * Mathf.PI) * (1f - prog * 0.25f);
            samples[i] = Mathf.Clamp((baseWave + harmonic) * env * 0.85f, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create(esAgresivo ? "Momia2_Ataque" : "Momia2_Alerta", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip SintetizarSonidoRasgunoGarras(float duracion)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.FloorToInt(sampleRate * duracion);
        float[] samples = new float[sampleCount];
        System.Random rng = new System.Random(91823);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duracion;

            float sweepFreq = Mathf.Lerp(1450f, 380f, prog);
            float slashTone = Mathf.Sin(2f * Mathf.PI * sweepFreq * t) * 0.38f;
            float ruidoDesgarro = ((float)rng.NextDouble() * 2f - 1f) * 0.62f;
            float env = Mathf.Sin(prog * Mathf.PI) * Mathf.Exp(-prog * 2.2f);

            samples[i] = Mathf.Clamp((slashTone + ruidoDesgarro) * env * 0.9f, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("Momia2_RasgunoGarras", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip SintetizarSonidoGolpeReja(float duracion)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.FloorToInt(sampleRate * duracion);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float clank1 = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-t * 14f) * 0.65f;
            float clank2 = t > 0.22f ? Mathf.Sin(2f * Mathf.PI * 290f * (t - 0.22f)) * Mathf.Exp(-(t - 0.22f) * 12f) * 0.75f : 0f;
            float resonancia = Mathf.Sin(2f * Mathf.PI * 490f * t) * Mathf.Exp(-t * 6f) * 0.25f;
            samples[i] = Mathf.Clamp(clank1 + clank2 + resonancia, -0.92f, 0.92f);
        }

        AudioClip clip = AudioClip.Create("Momia2_GolpeReja", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip SintetizarSonidoAlcantarilla(float duracion)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.FloorToInt(sampleRate * duracion);
        float[] samples = new float[sampleCount];
        System.Random rng = new System.Random(44119);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duracion;
            float rejillaMetal = Mathf.Sin(2f * Mathf.PI * (165f + Mathf.Sin(t * 35f) * 40f) * t) * Mathf.Exp(-t * 5.5f) * 0.55f;
            float chapoteo = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Sin(prog * Mathf.PI) * 0.38f;
            samples[i] = Mathf.Clamp(rejillaMetal + chapoteo, -0.90f, 0.90f);
        }

        AudioClip clip = AudioClip.Create("Momia2_Alcantarilla", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
