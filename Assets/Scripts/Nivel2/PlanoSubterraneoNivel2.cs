using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sistema de Cartel y Visor del Plano Real del Nivel 2.
/// - Estética 100% inmersiva de lámina de hierro oxidado y cartografía minera de Guanajuato.
/// - Tableros físicos de chapa remachada en la pared con foco minero ámbar cálido.
/// - Al presionar [E], abre la gran placa de lámina de hierro con el plano arquitectónico detallado.
/// - Dibuja muros macizos de piedra, umbrales de rejas [||||], iconos de bombillas industriales,
///   marcador de inicio, generador, compuerta y baliza táctica "TÚ ESTÁS AQUÍ".
/// - Oculta todo el HUD del juego mientras el plano está en pantalla para evitar cualquier solapamiento.
/// - Cierre receptivo con [E] o [ESC].
/// NO TOCA NINGÚN ARCHIVO DEL NIVEL 1.
/// </summary>
public class PlanoSubterraneoNivel2 : MonoBehaviour
{
    public static PlanoSubterraneoNivel2 Instance { get; private set; }

    public static bool VisorPlanoAbierto { get; private set; } = false;

    private Transform playerTransform;
    private PlayerController playerScript;
    private Rigidbody2D playerRb;
    private float tiempoApertura = 0f;
    private Rect ultimoBtnCerrarRect;
    private Rect ultimoMarcoTotalRect;

    // Interruptores de fase y generador
    private InterruptorFusibleNivel2[] interruptores;
    private GeneradorPrincipalNivel2 generador;

    // Texturas procedurales estilo lámina de hierro envejecido y cartografía minera de Guanajuato
    private Texture2D texLaminaHierroDesgastada;
    private Texture2D texChapaMetalGrabada;
    private Texture2D texMuroPiedraCantera;
    private Texture2D texOxidoHerrumbre;
    private Texture2D texRemacheHierro;
    private Texture2D texTornilloHierro;
    private Texture2D texRejaForjaUmbral;
    private Texture2D texSueloCalderas;
    private Texture2D texSueloCriptas;
    private Texture2D texSueloSubestacion;
    private Texture2D texSueloPasillo;
    private Texture2D texPlacaBronce;

    // Coordenadas reales del mundo en Nivel 2
    private const float MIN_WORLD_X = -26f;
    private const float MAX_WORLD_X = 26f;
    private const float MIN_WORLD_Y = -20f;
    private const float MAX_WORLD_Y = 23f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInicializar()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Nivel 2") || sceneName.Contains("Nivel2") || sceneName.Contains("Level2") || sceneName.Contains("Subterraneo"))
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("PlanoSubterraneoNivel2");
                go.AddComponent<PlanoSubterraneoNivel2>();
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GenerarTexturasHierroGuanajuato();
    }

    private void Start()
    {
        BuscarJugador();
        CrearEstacionesDePlanoEnEscenario();

        if (Camera.main != null)
        {
            CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
            if (cf != null)
            {
                cf.permitirTeclaM = false;
            }
        }
    }

    private void BuscarJugador()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) p = GameObject.Find("Vigilante");
            if (p == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) p = pc.gameObject;
            }
            if (p != null)
            {
                playerTransform = p.transform;
                playerScript = p.GetComponent<PlayerController>();
                playerRb = p.GetComponent<Rigidbody2D>();
            }
        }

        if (interruptores == null || interruptores.Length == 0)
        {
            interruptores = FindObjectsByType<InterruptorFusibleNivel2>(FindObjectsSortMode.None);
        }
        if (generador == null)
        {
            generador = GeneradorPrincipalNivel2.Instance;
        }
    }

    private void Update()
    {
        BuscarJugador();

        // Cierre receptivo con ESC, E, botón touch o pulsación directa en pantalla
        if (VisorPlanoAbierto)
        {
            if (Time.unscaledTime - tiempoApertura > 0.15f)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E) || TouchControlsManager.IsInteractTriggered())
                {
                    CerrarVisorPlano();
                    return;
                }

                // Cierre por toque en pantalla (botón de cerrar o toque en viñeta oscura fuera del plano)
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        Touch t = Input.GetTouch(i);
                        if (t.phase == TouchPhase.Began)
                        {
                            Vector2 guiTouch = new Vector2(t.position.x, Screen.height - t.position.y);
                            if (ultimoBtnCerrarRect.Contains(guiTouch) || !ultimoMarcoTotalRect.Contains(guiTouch))
                            {
                                CerrarVisorPlano();
                                break;
                            }
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    if (ultimoBtnCerrarRect.Contains(guiMouse) || !ultimoMarcoTotalRect.Contains(guiMouse))
                    {
                        CerrarVisorPlano();
                    }
                }
            }
        }
    }

    public void AlternarVisorPlano()
    {
        if (VisorPlanoAbierto)
        {
            CerrarVisorPlano();
        }
        else
        {
            // Solo se permite abrir el plano si la jugadora está físicamente en el lugar correspondiente (frente a un mural)
            if (!EstacionPlanoInteraccion.EsJugadorFrenteAEstacion()) return;
            AbrirVisorPlano();
        }
    }

    public void AbrirVisorPlano()
    {
        if (!EstacionPlanoInteraccion.EsJugadorFrenteAEstacion()) return;
        VisorPlanoAbierto = true;
        tiempoApertura = Time.unscaledTime;
    }

    public void CerrarVisorPlano()
    {
        VisorPlanoAbierto = false;
    }

    private void CrearEstacionesDePlanoEnEscenario()
    {
        CrearTableroFisico("TableroPlano_Inicio", new Vector2(3.6f, -16.0f), "Vestíbulo de Entrada");
        CrearTableroFisico("TableroPlano_Calderas", new Vector2(-21.0f, 0.6f), "Sala de Calderas");
        CrearTableroFisico("TableroPlano_Criptas", new Vector2(21.0f, 0.6f), "Criptas y Osarios");
        CrearTableroFisico("TableroPlano_Subestacion", new Vector2(-12.5f, 16.0f), "Subestación Norte");
    }

    private void CrearTableroFisico(string nombre, Vector2 posicion, string salaEtiqueta)
    {
        if (GameObject.Find(nombre) != null) return;

        GameObject go = new GameObject(nombre);
        go.transform.position = new Vector3(posicion.x, posicion.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GenerarSpriteCartelFisicoPared();
        sr.sortingOrder = 2;

        GameObject luzGo = new GameObject("LuzMineraAmbar");
        luzGo.transform.SetParent(go.transform);
        luzGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light2D l2d = luzGo.AddComponent<Light2D>();
        l2d.lightType = Light2D.LightType.Point;
        l2d.color = new Color(1.0f, 0.75f, 0.40f, 1f); // Foco minero de filamento cálido
        l2d.intensity = 0.95f;
        l2d.pointLightOuterRadius = 2.8f;
        l2d.pointLightInnerRadius = 0.6f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3.0f, 3.0f);

        EstacionPlanoInteraccion est = go.AddComponent<EstacionPlanoInteraccion>();
        est.nombreSector = salaEtiqueta;
    }

    private Sprite GenerarSpriteCartelFisicoPared()
    {
        int w = 64;
        int h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color marcoHierro = new Color(0.18f, 0.16f, 0.14f, 1f);
        Color chapaMetal = new Color(0.13f, 0.12f, 0.11f, 1f);
        Color oxido = new Color(0.35f, 0.22f, 0.14f, 1f);
        Color remache = new Color(0.52f, 0.46f, 0.36f, 1f);
        Color tizaGrabada = new Color(0.70f, 0.65f, 0.55f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool esRemache = ((x == 3 || x == 4) && (y == 3 || y == 4)) ||
                                 ((x == w - 5 || x == w - 4) && (y == 3 || y == 4)) ||
                                 ((x == 3 || x == 4) && (y == h - 5 || y == h - 4)) ||
                                 ((x == w - 5 || x == w - 4) && (y == h - 5 || y == h - 4));

                if (esRemache)
                {
                    tex.SetPixel(x, y, remache);
                    continue;
                }

                if (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3)
                {
                    tex.SetPixel(x, y, marcoHierro);
                }
                else
                {
                    Color baseCol = chapaMetal;
                    int ruido = (x * 7 + y * 13) % 17;
                    if (ruido < 5 || (x > 18 && x < 28 && y < 22))
                    {
                        baseCol = Color.Lerp(chapaMetal, oxido, 0.70f);
                    }

                    // Esquema arquitectónico grabado
                    bool esLinea = (x == 18 && y >= 10 && y <= 38) ||
                                  (x == 46 && y >= 10 && y <= 38) ||
                                  (y == 24 && x >= 18 && x <= 46) ||
                                  (y == 38 && x >= 24 && x <= 40) ||
                                  (x == 32 && y >= 8 && y <= 38);

                    if (esLinea) baseCol = tizaGrabada;

                    tex.SetPixel(x, y, baseCol);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    private void GenerarTexturasHierroGuanajuato()
    {
        // 1. Placa base de hierro forjado envejecido con corrosión y raspaduras
        texLaminaHierroDesgastada = CrearTexturaLaminaHierro(128, 128);
        // 2. Chapa interior del plano con tono de zinc/hojalata oxidada
        texChapaMetalGrabada = CrearTexturaChapaGrabada(128, 128);
        // 3. Muros gruesos de mampostería y cantera minera
        texMuroPiedraCantera = CrearTexturaMuroPiedra(64, 64);
        // 4. Herrumbre y óxido profundo
        texOxidoHerrumbre = CrearTexturaConRuido(32, 32, new Color(0.42f, 0.22f, 0.12f), new Color(0.28f, 0.14f, 0.08f));
        // 5. Remache de forja industrial con relieve 3D
        texRemacheHierro = CrearTexturaRemache(32, 32);
        // 6. Tornillo ranurado de cabeza plana
        texTornilloHierro = CrearTexturaTornillo(32, 32);
        // 7. Placa de bronce antiguo y latón envejecido
        texPlacaBronce = CrearTexturaConRuido(32, 32, new Color(0.48f, 0.38f, 0.22f), new Color(0.34f, 0.26f, 0.14f));
        // 8. Reja de hierro forjado
        texRejaForjaUmbral = CrearTexturaRejaForja(32, 32);

        // Suelos temáticos subterráneos envejecidos
        texSueloCalderas = CrearTexturaConRuido(32, 32, new Color(0.18f, 0.14f, 0.11f), new Color(0.13f, 0.10f, 0.08f));
        texSueloCriptas = CrearTexturaConRuido(32, 32, new Color(0.12f, 0.13f, 0.15f), new Color(0.09f, 0.10f, 0.12f));
        texSueloSubestacion = CrearTexturaConRuido(32, 32, new Color(0.13f, 0.15f, 0.14f), new Color(0.10f, 0.12f, 0.11f));
        texSueloPasillo = CrearTexturaConRuido(32, 32, new Color(0.11f, 0.11f, 0.11f), new Color(0.08f, 0.08f, 0.08f));
    }

    private Texture2D CrearTexturaLaminaHierro(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        Color hierroOscuro = new Color(0.13f, 0.12f, 0.11f, 1f);
        Color hierroClaro = new Color(0.19f, 0.17f, 0.15f, 1f);
        Color oxidoRojo = new Color(0.38f, 0.22f, 0.13f, 1f);
        Color oxidoAmarillo = new Color(0.48f, 0.32f, 0.15f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nBase = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                float nDetalle = Mathf.PerlinNoise(x * 0.25f + 12.3f, y * 0.25f + 45.6f);
                float nOxido = Mathf.PerlinNoise(x * 0.05f + 88f, y * 0.05f + 19f);

                Color col = Color.Lerp(hierroOscuro, hierroClaro, nBase);

                // Manchas de herrumbre natural en los bordes y zonas desgastadas
                if (nOxido > 0.58f)
                {
                    float factorOxido = (nOxido - 0.58f) / 0.42f;
                    Color tonoOxido = Color.Lerp(oxidoRojo, oxidoAmarillo, nDetalle);
                    col = Color.Lerp(col, tonoOxido, factorOxido * 0.85f);
                }

                // Picaduras y arañazos metálicos
                if (nDetalle > 0.82f)
                {
                    col = Color.Lerp(col, new Color(0.06f, 0.05f, 0.05f, 1f), 0.5f);
                }

                t.SetPixel(x, y, col);
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D CrearTexturaChapaGrabada(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        Color metalBase = new Color(0.09f, 0.085f, 0.08f, 1f);
        Color metalVar = new Color(0.13f, 0.125f, 0.12f, 1f);
        Color oxidoLeve = new Color(0.24f, 0.16f, 0.11f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.12f, y * 0.12f);
                float ox = Mathf.PerlinNoise(x * 0.04f + 33f, y * 0.04f + 77f);
                Color c = Color.Lerp(metalBase, metalVar, n);
                if (ox > 0.65f)
                {
                    c = Color.Lerp(c, oxidoLeve, (ox - 0.65f) * 1.5f);
                }
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D CrearTexturaMuroPiedra(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        Color cantera1 = new Color(0.32f, 0.28f, 0.22f, 1f);
        Color cantera2 = new Color(0.24f, 0.20f, 0.16f, 1f);
        Color juntaMortero = new Color(0.12f, 0.10f, 0.08f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool esJunta = (y % 8 == 0) || ((x % 16 == 0) && (y / 8 % 2 == 0)) || ((x % 16 == 8) && (y / 8 % 2 != 0));
                if (esJunta)
                {
                    t.SetPixel(x, y, juntaMortero);
                }
                else
                {
                    float n = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    t.SetPixel(x, y, Color.Lerp(cantera1, cantera2, n));
                }
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D CrearTexturaRemache(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        float centro = w * 0.5f;
        float radio = w * 0.42f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > radio)
                {
                    t.SetPixel(x, y, Color.clear);
                }
                else
                {
                    // Relieve esférico con luz proveniente de arriba-izquierda
                    float nx = dx / radio;
                    float ny = dy / radio;
                    float luz = Mathf.Clamp01((-nx * 0.5f + ny * 0.7f) * 0.5f + 0.5f);

                    Color baseRemache = Color.Lerp(new Color(0.18f, 0.15f, 0.12f, 1f), new Color(0.68f, 0.58f, 0.44f, 1f), luz);
                    if (dist > radio - 1.5f)
                    {
                        baseRemache = new Color(0.08f, 0.07f, 0.06f, 1f); // Sombra profunda de contacto
                    }
                    t.SetPixel(x, y, baseRemache);
                }
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D CrearTexturaTornillo(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        float centro = w * 0.5f;
        float radio = w * 0.42f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > radio)
                {
                    t.SetPixel(x, y, Color.clear);
                }
                else
                {
                    bool ranura = Mathf.Abs(dy) <= 1.5f && Mathf.Abs(dx) <= radio * 0.75f;
                    if (ranura)
                    {
                        t.SetPixel(x, y, new Color(0.05f, 0.04f, 0.04f, 1f)); // Hendidura oscura de destornillador
                    }
                    else
                    {
                        float luz = Mathf.Clamp01((-dx / radio * 0.5f + dy / radio * 0.6f) * 0.5f + 0.5f);
                        Color c = Color.Lerp(new Color(0.22f, 0.18f, 0.14f, 1f), new Color(0.55f, 0.46f, 0.35f, 1f), luz);
                        t.SetPixel(x, y, c);
                    }
                }
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D CrearTexturaRejaForja(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        Color barraMetal = new Color(0.52f, 0.45f, 0.35f, 1f);
        Color barraBrillo = new Color(0.72f, 0.64f, 0.50f, 1f);
        Color huecoOscuro = new Color(0.08f, 0.07f, 0.06f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int modX = x % 4;
                if (modX == 0) t.SetPixel(x, y, barraBrillo);
                else if (modX == 1) t.SetPixel(x, y, barraMetal);
                else t.SetPixel(x, y, huecoOscuro);
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D CrearTexturaConRuido(int w, int h, Color baseCol, Color varCol)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.25f, y * 0.25f);
                t.SetPixel(x, y, Color.Lerp(baseCol, varCol, n));
            }
        }
        t.Apply();
        return t;
    }

    private void OnGUI()
    {
        if (!VisorPlanoAbierto) return;

        // Máxima prioridad de render: se dibuja por encima de todo
        GUI.depth = -500;

        float scale = ResponsiveGUIHelper.BeginScaledGUI(out float vw, out float vh, out Matrix4x4 prevMatrix, 890f, 640f);
        try
        {
            float ancho = Mathf.Min(860f, vw * 0.94f);
            float alto = Mathf.Min(610f, vh * 0.94f);
            float posX = (vw - ancho) / 2f;
            float posY = (vh - alto) / 2f;
            Rect marcoTotal = new Rect(posX, posY, ancho, alto);
            ultimoMarcoTotalRect = ResponsiveGUIHelper.VirtualRectToScreen(marcoTotal, scale);

            // 1. Oscurecimiento cinematográfico de fondo (elimina cualquier distracción)
            GUI.color = new Color(0.04f, 0.04f, 0.05f, 0.95f);
            GUI.DrawTexture(new Rect(0, 0, vw, vh), Texture2D.whiteTexture);

            // Viñeta periférica oscura
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, vw, posY), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, posY + alto, vw, vh - (posY + alto)), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, posY, posX, alto), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(posX + ancho, posY, vw - (posX + ancho), alto), Texture2D.whiteTexture);

        // 2. Gran placa base de lámina de hierro forjado desgastada
        GUI.color = Color.white;
        GUI.DrawTexture(marcoTotal, texLaminaHierroDesgastada);

        // Rebordes exteriores de forja con relieve, biselado y óxido
        DibujarBorde(marcoTotal, 6f, new Color(0.28f, 0.22f, 0.16f, 1f));
        DibujarBorde(new Rect(posX + 6, posY + 6, ancho - 12, alto - 12), 2.5f, new Color(0.42f, 0.28f, 0.16f, 0.85f));
        DibujarBorde(new Rect(posX + 8, posY + 8, ancho - 16, alto - 16), 1.5f, new Color(0.12f, 0.10f, 0.09f, 1f));

        // Escuadras de refuerzo en las 4 esquinas de la chapa de hierro
        DibujarEscuadraEsquina(posX + 8, posY + 8, 36f, 36f, true, true);
        DibujarEscuadraEsquina(posX + ancho - 44, posY + 8, 36f, 36f, false, true);
        DibujarEscuadraEsquina(posX + 8, posY + alto - 44, 36f, 36f, true, false);
        DibujarEscuadraEsquina(posX + ancho - 44, posY + alto - 44, 36f, 36f, false, false);

        // Tornillos industriales y remaches en todo el perímetro del cartel
        DibujarTornillo(posX + 16, posY + 16);
        DibujarTornillo(posX + ancho - 32, posY + 16);
        DibujarTornillo(posX + 16, posY + alto - 32);
        DibujarTornillo(posX + ancho - 32, posY + alto - 32);

        for (int i = 1; i <= 4; i++)
        {
            float tRemX = Mathf.Lerp(posX + 50, posX + ancho - 50, i / 5f);
            DibujarRemache(tRemX - 6f, posY + 8f, 12f);
            DibujarRemache(tRemX - 6f, posY + alto - 20f, 12f);
        }
        for (int i = 1; i <= 3; i++)
        {
            float tRemY = Mathf.Lerp(posY + 50, posY + alto - 50, i / 4f);
            DibujarRemache(posX + 8f, tRemY - 6f, 12f);
            DibujarRemache(posX + ancho - 20f, tRemY - 6f, 12f);
        }

        // 3. Encabezado: Placa de bronce atornillada a la lámina de hierro
        Rect headerRect = new Rect(posX + 48, posY + 14, ancho - 96, 52f);
        GUI.color = Color.white;
        GUI.DrawTexture(headerRect, texPlacaBronce);
        DibujarBorde(headerRect, 2.5f, new Color(0.24f, 0.18f, 0.10f, 1f));
        DibujarBorde(new Rect(headerRect.x + 3, headerRect.y + 3, headerRect.width - 6, headerRect.height - 6), 1f, new Color(0.65f, 0.52f, 0.32f, 0.65f));

        // Tornillos de sujeción de la placa de bronce
        DibujarTornillo(headerRect.x + 4, headerRect.y + 4);
        DibujarTornillo(headerRect.xMax - 20, headerRect.y + 4);
        DibujarTornillo(headerRect.x + 4, headerRect.yMax - 20);
        DibujarTornillo(headerRect.xMax - 20, headerRect.yMax - 20);

        GUIStyle subtituloEstilo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        subtituloEstilo.normal.textColor = new Color(0.72f, 0.58f, 0.38f, 1f);
        GUI.Label(new Rect(headerRect.x, headerRect.y + 6, headerRect.width, 16), "MUSEO DE LAS MOMIAS DE GUANAJUATO", subtituloEstilo);

        GUIStyle tituloEstilo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        tituloEstilo.normal.textColor = new Color(0.96f, 0.90f, 0.78f, 1f); // Tono marfil grabado
        GUI.Label(new Rect(headerRect.x, headerRect.y + 22, headerRect.width, 24), "⚜ PLANO DE RED ELÉCTRICA Y GALERÍAS SUBTERRÁNEAS (1894) ⚜", tituloEstilo);

            // Botón [✕ CERRAR] en forma de chapa de hierro forjado
            Rect btnCerrarRect = new Rect(posX + ancho - 145, posY + 20, 115, 36);
            ultimoBtnCerrarRect = ResponsiveGUIHelper.VirtualRectToScreen(btnCerrarRect, scale);
            GUI.color = new Color(0.18f, 0.15f, 0.13f, 1f);
            GUI.DrawTexture(btnCerrarRect, Texture2D.whiteTexture);
            DibujarBorde(btnCerrarRect, 2f, new Color(0.55f, 0.42f, 0.28f, 1f));
            GUI.color = Color.white;

            GUIStyle btnEstilo = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnEstilo.normal.textColor = new Color(0.95f, 0.88f, 0.75f, 1f);
            if (GUI.Button(btnCerrarRect, "✕ CERRAR [ESC]", btnEstilo))
            {
                CerrarVisorPlano();
            }

            // 4. Área central del plano arquitectónico
            Rect areaMapa = new Rect(posX + 26, posY + 74, ancho - 52, alto - 144);
            DibujarPlanoArquitectonico(areaMapa);

            // 5. Barra inferior de estado (chapa metálica remachada)
            Rect footerRect = new Rect(posX + 26, posY + alto - 62, ancho - 52, 48);
            GUI.color = new Color(0.12f, 0.11f, 0.10f, 1f);
            GUI.DrawTexture(footerRect, Texture2D.whiteTexture);
            DibujarBorde(footerRect, 2f, new Color(0.38f, 0.28f, 0.18f, 1f));
            GUI.color = Color.white;

            DibujarTornillo(footerRect.x + 4, footerRect.y + 4);
            DibujarTornillo(footerRect.xMax - 20, footerRect.y + 4);
            DibujarTornillo(footerRect.x + 4, footerRect.yMax - 20);
            DibujarTornillo(footerRect.xMax - 20, footerRect.yMax - 20);

            string sectorActual = ObtenerNombreSectorJugador();
            int fasesCompletadas = ObtenerFasesCompletadas();

            GUIStyle infoEstilo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            infoEstilo.normal.textColor = new Color(0.85f, 0.80f, 0.72f, 1f);

            string colorFases = (fasesCompletadas >= 3) ? "#66FF66" : (fasesCompletadas > 0 ? "#FFCC44" : "#FF5544");
            string textoInfo = $"<b>SECTOR ACTUAL:</b> <color=#F0D090>{sectorActual.ToUpper()}</color>    |    " +
                               $"<b>RED ELÉCTRICA:</b> <color={colorFases}>{fasesCompletadas} / 3 FASES CON CORRIENTE</color>    |    " +
                               $"Presiona <b>[ESC]</b> para cerrar el plano";

            GUI.Label(new Rect(footerRect.x + 20, footerRect.y + 12, footerRect.width - 40, 26), textoInfo, infoEstilo);
        }
        finally
        {
            ResponsiveGUIHelper.EndScaledGUI(prevMatrix);
        }
    }

    private void DibujarPlanoArquitectonico(Rect r)
    {
        // Fondo chapa interior grabada con pátina
        GUI.color = Color.white;
        GUI.DrawTexture(r, texChapaMetalGrabada);

        // Marco macizo de cantera y hierro forjado alrededor de la cartografía
        DibujarBorde(r, 3.5f, new Color(0.44f, 0.34f, 0.22f, 1f));
        DibujarBorde(new Rect(r.x + 3.5f, r.y + 3.5f, r.width - 7f, r.height - 7f), 1.5f, new Color(0.16f, 0.14f, 0.12f, 1f));

        // =========================================================================
        // DIBUJO DE LAS 5 CÁMARAS ARQUITECTÓNICAS CON MUROS DE CANTERA Y SUELO
        // =========================================================================
        // 1. Vestíbulo Sur (Inicio) X: -7.5 a 7.5, Y: -20 a -11
        DibujarSalaArquitectonica(r, -7f, -20f, 14f, 9f, "I. VESTÍBULO DE ACCESO\n[Entrada Principal Sur]", texSueloPasillo, new Color(0.85f, 0.78f, 0.65f));

        // 2. Pasillo Central Troncal X: -7.5 a 7.5, Y: -11 a 8
        DibujarSalaArquitectonica(r, -7.5f, -11f, 15f, 19f, "II. PASILLO DE LAS CATACUMBAS\n[Galería Troncal Central]", texSueloPasillo, new Color(0.80f, 0.75f, 0.68f));

        // 3. Sala de Calderas (Oeste) X: -25 a -7.5, Y: -11 a 8
        DibujarSalaArquitectonica(r, -25f, -11f, 17.5f, 19f, "III. SALA DE CALDERAS\n[Presión de Vapor y Válvulas]", texSueloCalderas, new Color(0.90f, 0.70f, 0.55f));

        // 4. Criptas y Osarios (Este) X: 7.5 a 25, Y: -11 a 8
        DibujarSalaArquitectonica(r, 7.5f, -11f, 17.5f, 19f, "IV. CRIPTAS Y OSARIOS\n[Nichos Funerarios y Catacumbas]", texSueloCriptas, new Color(0.70f, 0.78f, 0.90f));

        // 5. Subestación Eléctrica Norte X: -25 a 25, Y: 8 a 22
        DibujarSalaArquitectonica(r, -25f, 8f, 50f, 14f, "V. SUBESTACIÓN ELÉCTRICA NORTE\n[Dínamos y Transformadores de Alta Tensión]", texSueloSubestacion, new Color(0.75f, 0.88f, 0.80f));

        // Umbrales de paso y Puertas de Reja metálicas
        DibujarUmbralReja(r, -10.5f, -11f, 3.8f, 1.2f, "REJA SUR-OESTE");
        DibujarUmbralReja(r, 10.5f, -11f, 3.8f, 1.2f, "REJA SUR-ESTE");
        DibujarUmbralReja(r, -10.5f, 8f, 3.8f, 1.2f, "REJA NORTE-OESTE");
        DibujarUmbralReja(r, 10.5f, 8f, 3.8f, 1.2f, "REJA NORTE-ESTE");

        // =========================================================================
        // ELEMENTOS TÁCTICOS E INTERACTIVOS DEL PLANO
        // =========================================================================
        // 1. INICIO DEL JUGADOR (0, -16)
        Vector2 posInicio = MundoAPantalla(new Vector2(0f, -16f), r);
        DibujarPlacaSello(posInicio, new Color(0.95f, 0.85f, 0.55f, 1f), "⚓ ACCESO SUR\n[PUNTO DE PARTIDA]");

        // 2. FASE 1: CALDERAS (-21, -2)
        bool f1 = EsFaseActiva(1);
        Vector2 posF1 = MundoAPantalla(new Vector2(-21f, -2f), r);
        DibujarIndicadorFusible(posF1, f1, "FASE 1: CALDERAS");

        // 3. FASE 2: CRIPTAS (21, -2)
        bool f2 = EsFaseActiva(2);
        Vector2 posF2 = MundoAPantalla(new Vector2(21f, -2f), r);
        DibujarIndicadorFusible(posF2, f2, "FASE 2: CRIPTAS");

        // 4. FASE 3: SUBESTACIÓN (-16, 16)
        bool f3 = EsFaseActiva(3);
        Vector2 posF3 = MundoAPantalla(new Vector2(-16f, 16f), r);
        DibujarIndicadorFusible(posF3, f3, "FASE 3: SUBESTACIÓN");

        // 5. GENERADOR CENTRAL (0, 14)
        bool genListo = (f1 && f2 && f3);
        Vector2 posGen = MundoAPantalla(new Vector2(0f, 14f), r);
        DibujarPlacaMaquinaria(posGen, genListo, "⚙ GENERADOR DINAMO ⚡\n(Requiere 3 Fases)");

        // 6. GRAN COMPUERTA DE SALIDA (0, 21)
        Vector2 posCompuerta = MundoAPantalla(new Vector2(0f, 21f), r);
        DibujarPlacaCompuerta(posCompuerta, genListo, "⛩ COMPUERTA BLINDADA\n[SALIDA AL EXTERIOR]");

        // =========================================================================
        // BALIZA DE ORIENTACIÓN: "TÚ ESTÁS AQUÍ"
        // =========================================================================
        if (playerTransform != null)
        {
            Vector2 playerPosMundo = new Vector2(playerTransform.position.x, playerTransform.position.y);
            Vector2 posP = MundoAPantalla(playerPosMundo, r);

            float pulso = Mathf.PingPong(Time.unscaledTime * 3.2f, 1f);
            Color colBaliza = Color.Lerp(new Color(0.92f, 0.35f, 0.12f, 1f), new Color(1f, 0.88f, 0.35f, 1f), pulso);

            // Aura de filamento incandescente
            float rad = 14f + pulso * 6f;
            GUI.color = new Color(colBaliza.r, colBaliza.g, colBaliza.b, 0.28f);
            GUI.DrawTexture(new Rect(posP.x - rad, posP.y - rad, rad * 2f, rad * 2f), Texture2D.whiteTexture);

            // Puntero de orientación en latón envejecido
            GUI.color = new Color(0.16f, 0.12f, 0.08f, 1f);
            GUI.DrawTexture(new Rect(posP.x - 7f, posP.y - 7f, 14f, 14f), Texture2D.whiteTexture);
            GUI.color = colBaliza;
            GUI.DrawTexture(new Rect(posP.x - 5f, posP.y - 5f, 10f, 10f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Cartela grabada "TÚ ESTÁS AQUÍ"
            GUIStyle estiloTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            estiloTag.normal.textColor = new Color(1f, 0.95f, 0.80f, 1f);

            Rect rTag = new Rect(posP.x + 12f, posP.y - 12f, 130f, 24f);
            GUI.color = new Color(0.12f, 0.10f, 0.09f, 0.96f);
            GUI.DrawTexture(rTag, Texture2D.whiteTexture);
            DibujarBorde(rTag, 1.5f, new Color(0.85f, 0.65f, 0.32f, 1f));
            GUI.color = Color.white;
            GUI.Label(rTag, "⌖ TÚ ESTÁS AQUÍ", estiloTag);
        }
    }

    private void DibujarSalaArquitectonica(Rect mapaRect, float wx, float wy, float ww, float wh, string nombre, Texture2D texSuelo, Color colTexto)
    {
        Vector2 p1 = MundoAPantalla(new Vector2(wx, wy + wh), mapaRect);
        Vector2 p2 = MundoAPantalla(new Vector2(wx + ww, wy), mapaRect);

        Rect secRect = new Rect(p1.x, p1.y, Mathf.Abs(p2.x - p1.x), Mathf.Abs(p2.y - p1.y));

        // 1. Suelo de la sala
        GUI.color = Color.white;
        GUI.DrawTexture(secRect, texSuelo);

        // 2. Muros macizos de cantera minera alrededor de la cámara
        DibujarBorde(secRect, 4f, new Color(0.24f, 0.20f, 0.16f, 1f));
        DibujarBorde(new Rect(secRect.x + 3f, secRect.y + 3f, secRect.width - 6f, secRect.height - 6f), 1.2f, new Color(0.48f, 0.40f, 0.30f, 0.90f));

        // 3. Rótulo grabado al estilo estarcido de mina
        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        st.normal.textColor = colTexto;
        GUI.Label(secRect, nombre, st);
    }

    private void DibujarUmbralReja(Rect mapaRect, float wx, float wy, float ww, float wh, string tooltip)
    {
        Vector2 p1 = MundoAPantalla(new Vector2(wx - ww * 0.5f, wy + wh * 0.5f), mapaRect);
        Vector2 p2 = MundoAPantalla(new Vector2(wx + ww * 0.5f, wy - wh * 0.5f), mapaRect);

        Rect rUmbral = new Rect(p1.x, p1.y, Mathf.Max(14f, Mathf.Abs(p2.x - p1.x)), Mathf.Max(8f, Mathf.Abs(p2.y - p1.y)));

        GUI.color = Color.white;
        GUI.DrawTexture(rUmbral, texRejaForjaUmbral);
        DibujarBorde(rUmbral, 1.2f, new Color(0.42f, 0.35f, 0.26f, 1f));
    }

    private void DibujarIndicadorFusible(Vector2 posP, bool activo, string texto)
    {
        // Caja metálica de interruptor
        Rect cajaRect = new Rect(posP.x - 9f, posP.y - 9f, 18f, 18f);
        GUI.color = new Color(0.14f, 0.12f, 0.10f, 1f);
        GUI.DrawTexture(cajaRect, Texture2D.whiteTexture);
        DibujarBorde(cajaRect, 1.5f, new Color(0.48f, 0.40f, 0.28f, 1f));

        // Foco de filamento incandescente
        Color colBombilla = activo ? new Color(0.35f, 0.95f, 0.45f, 1f) : new Color(0.92f, 0.22f, 0.16f, 1f);
        GUI.color = colBombilla;
        GUI.DrawTexture(new Rect(posP.x - 5f, posP.y - 5f, 10f, 10f), Texture2D.whiteTexture);

        // Halo de luz tenue
        GUI.color = new Color(colBombilla.r, colBombilla.g, colBombilla.b, 0.35f);
        GUI.DrawTexture(new Rect(posP.x - 8f, posP.y - 8f, 16f, 16f), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            richText = true
        };
        st.normal.textColor = activo ? new Color(0.70f, 0.98f, 0.65f, 1f) : new Color(0.92f, 0.50f, 0.42f, 1f);
        string estadoStr = activo ? "\n<color=#66FF66>[ENERGIZADO]</color>" : "\n<color=#FF5544>[SIN CORRIENTE]</color>";
        GUI.Label(new Rect(posP.x - 65f, posP.y + 11f, 130f, 28f), texto + estadoStr, st);
    }

    private void DibujarPlacaSello(Vector2 posP, Color col, string texto)
    {
        Rect r = new Rect(posP.x - 8f, posP.y - 8f, 16f, 16f);
        GUI.color = col;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        DibujarBorde(r, 1.5f, new Color(0.16f, 0.12f, 0.08f, 1f));

        GUI.color = Color.white;
        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        st.normal.textColor = col;
        GUI.Label(new Rect(posP.x - 60f, posP.y + 10f, 120f, 28f), texto, st);
    }

    private void DibujarPlacaMaquinaria(Vector2 posP, bool activa, string texto)
    {
        Rect r = new Rect(posP.x - 10f, posP.y - 10f, 20f, 20f);
        Color c = activa ? new Color(0.98f, 0.92f, 0.42f, 1f) : new Color(0.68f, 0.55f, 0.38f, 1f);

        GUI.color = new Color(0.16f, 0.14f, 0.12f, 1f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = c;
        GUI.DrawTexture(new Rect(posP.x - 7f, posP.y - 7f, 14f, 14f), Texture2D.whiteTexture);
        DibujarBorde(r, 1.5f, new Color(0.48f, 0.38f, 0.25f, 1f));

        GUI.color = Color.white;
        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            richText = true
        };
        st.normal.textColor = c;
        GUI.Label(new Rect(posP.x - 75f, posP.y + 12f, 150f, 28f), texto, st);
    }

    private void DibujarPlacaCompuerta(Vector2 posP, bool abierta, string texto)
    {
        Rect r = new Rect(posP.x - 10f, posP.y - 10f, 20f, 20f);
        Color c = abierta ? new Color(0.40f, 0.95f, 0.45f, 1f) : new Color(0.62f, 0.52f, 0.45f, 1f);

        GUI.color = new Color(0.14f, 0.12f, 0.10f, 1f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = c;
        GUI.DrawTexture(new Rect(posP.x - 7f, posP.y - 7f, 14f, 14f), Texture2D.whiteTexture);
        DibujarBorde(r, 1.5f, new Color(0.48f, 0.38f, 0.28f, 1f));

        GUI.color = Color.white;
        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            richText = true
        };
        st.normal.textColor = c;
        string sub = abierta ? "\n<color=#66FF66>[DESBLOQUEADA]</color>" : "\n<color=#AAAAAA>[BLOQUEO CERRADO]</color>";
        GUI.Label(new Rect(posP.x - 75f, posP.y + 12f, 150f, 28f), texto + sub, st);
    }

    private void DibujarEscuadraEsquina(float x, float y, float w, float h, bool esIzquierda, bool esArriba)
    {
        Rect r = new Rect(x, y, w, h);
        GUI.color = new Color(0.22f, 0.18f, 0.15f, 1f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        DibujarBorde(r, 1.5f, new Color(0.42f, 0.32f, 0.22f, 1f));
        GUI.color = Color.white;

        float remX = esIzquierda ? (x + 8f) : (x + w - 18f);
        float remY = esArriba ? (y + 8f) : (y + h - 18f);
        DibujarRemache(remX, remY, 10f);
    }

    private void DibujarRemache(float x, float y, float tam)
    {
        Rect r = new Rect(x, y, tam, tam);
        GUI.color = Color.white;
        GUI.DrawTexture(r, texRemacheHierro != null ? texRemacheHierro : Texture2D.whiteTexture);
    }

    private void DibujarTornillo(float x, float y)
    {
        Rect r = new Rect(x, y, 16f, 16f);
        GUI.color = Color.white;
        GUI.DrawTexture(r, texTornilloHierro != null ? texTornilloHierro : Texture2D.whiteTexture);
    }

    private Vector2 MundoAPantalla(Vector2 posMundo, Rect r)
    {
        float normX = Mathf.InverseLerp(MIN_WORLD_X, MAX_WORLD_X, posMundo.x);
        float normY = 1.0f - Mathf.InverseLerp(MIN_WORLD_Y, MAX_WORLD_Y, posMundo.y);

        float px = r.x + normX * r.width;
        float py = r.y + normY * r.height;

        return new Vector2(px, py);
    }

    private void DibujarBorde(Rect rect, float grosor, Color col)
    {
        GUI.color = col;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, grosor), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - grosor, rect.width, grosor), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, grosor, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - grosor, rect.y, grosor, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private bool EsFaseActiva(int idFase)
    {
        if (interruptores == null) return false;
        for (int i = 0; i < interruptores.Length; i++)
        {
            if (interruptores[i] != null && interruptores[i].idFase == idFase)
            {
                return interruptores[i].estaActivado;
            }
        }
        return false;
    }

    private int ObtenerFasesCompletadas()
    {
        if (interruptores == null) return 0;
        int c = 0;
        for (int i = 0; i < interruptores.Length; i++)
        {
            if (interruptores[i] != null && interruptores[i].estaActivado) c++;
        }
        return c;
    }

    private string ObtenerNombreSectorJugador()
    {
        if (playerTransform == null) return "Desconocido";

        Vector2 p = playerTransform.position;

        if (p.y > 8.0f) return "Subestación Eléctrica Norte";
        if (p.y < -11.0f) return "Vestíbulo Sur (Entrada)";
        if (p.x < -7.5f) return "Sala de Calderas";
        if (p.x > 7.5f) return "Criptas y Osarios";
        return "Pasillo Central Troncal";
    }
}

/// <summary>
/// Componente interactivo colocado en cada cartel de chapa en la pared.
/// Muestra un cartel de aviso en tono pergamino/bronce y abre/cierra con [E].
/// </summary>
public class EstacionPlanoInteraccion : MonoBehaviour
{
    private static readonly List<EstacionPlanoInteraccion> todasLasEstaciones = new List<EstacionPlanoInteraccion>();

    public static bool EsJugadorFrenteAEstacion()
    {
        for (int i = 0; i < todasLasEstaciones.Count; i++)
        {
            if (todasLasEstaciones[i] != null && todasLasEstaciones[i].jugadorCerca) return true;
        }
        return false;
    }

    public string nombreSector = "Subsuelo";
    private bool jugadorCerca = false;
    private Transform playerTransform;

    private void OnEnable()
    {
        if (!todasLasEstaciones.Contains(this)) todasLasEstaciones.Add(this);
    }

    private void OnDisable()
    {
        todasLasEstaciones.Remove(this);
        jugadorCerca = false;
    }

    private void Start()
    {
        if (!todasLasEstaciones.Contains(this)) todasLasEstaciones.Add(this);
        BuscarJugador();
    }

    private void BuscarJugador()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) p = GameObject.Find("Vigilante");
            if (p != null) playerTransform = p.transform;
        }
    }

    private bool ComprobarToqueDirecto()
    {
        if (Camera.main == null) return false;

        float zDist = Mathf.Abs(Camera.main.transform.position.z);
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    Vector3 sp = new Vector3(t.position.x, t.position.y, zDist);
                    Vector2 worldTap = Camera.main.ScreenToWorldPoint(sp);
                    if (col.OverlapPoint(worldTap)) return true;
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Vector3 sp = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDist);
            Vector2 worldTap = Camera.main.ScreenToWorldPoint(sp);
            if (col.OverlapPoint(worldTap)) return true;
        }
        return false;
    }

    private void Update()
    {
        BuscarJugador();

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            jugadorCerca = (dist <= 2.5f);
        }
        else
        {
            jugadorCerca = false;
        }

        bool clicDirectoEnCartel = ComprobarToqueDirecto();

        // El plano SOLO se abre si la jugadora está en el lugar correspondiente (frente al cartel mural)
        // Y presiona el botón correspondiente: [E], botón táctil [E] ACCIÓN, o clic directo sobre el cartel
        if (jugadorCerca && (Input.GetKeyDown(KeyCode.E) || TouchControlsManager.IsInteractTriggered() || clicDirectoEnCartel))
        {
            if (PlanoSubterraneoNivel2.Instance != null && Time.timeScale > 0f)
            {
                PlanoSubterraneoNivel2.Instance.AlternarVisorPlano();
            }
        }
    }

    private void OnGUI()
    {
        if (!jugadorCerca || PlanoSubterraneoNivel2.VisorPlanoAbierto) return;

        Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.6f) : Vector3.zero;
        if (screenPos.z <= 0) return;

        float s = ResponsiveGUIHelper.BeginScaledGUI(out float vw, out float vh, out Matrix4x4 prevMatrix);
        try
        {
            Vector2 guiPos = ResponsiveGUIHelper.WorldScreenToVirtualGUI(screenPos, s);
            float ancho = 240f;
            float alto = 34f;
            float x = Mathf.Clamp(guiPos.x - ancho / 2f, 8f, vw - ancho - 8f);
            float y = Mathf.Clamp(guiPos.y - alto, 8f, vh - alto - 8f);

            Rect r = new Rect(x, y, ancho, alto);

            // Estilo chapa metálica envejecida
            GUI.color = new Color(0.12f, 0.10f, 0.08f, 0.92f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.55f, 0.44f, 0.30f, 1f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(0.92f, 0.84f, 0.68f, 1f);
            GUI.Label(r, " [E] Consultar Plano del Subsuelo", style);
        }
        finally
        {
            ResponsiveGUIHelper.EndScaledGUI(prevMatrix);
        }
    }
}
