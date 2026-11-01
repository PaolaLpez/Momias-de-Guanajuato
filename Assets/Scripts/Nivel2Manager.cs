using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor principal del Nivel 2 ("La que escucha" — 1:00 a. m. a 2:00 a. m.).
/// Supervisa el HUD de supervivencia, estado de la electricidad (fusibles),
/// notificaciones de trampas de ruido y condiciones de victoria/derrota.
/// TOTALMENTE INDEPENDIENTE DEL NIVEL 1.
/// </summary>
public class Nivel2Manager : MonoBehaviour
{
    public static Nivel2Manager Instance { get; private set; }

    [Header("Estado del Nivel")]
    private bool nivelCompletado = false;
    private bool juegoTerminado = false;

    // Alertas de ruido temporal en pantalla
    private string alertaRuidoTexto = "";
    private float tiempoAlertaRuido = 0f;

    // Estilos de Interfaz
    private GUIStyle headerStyle;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;
    private Texture2D texPanel;
    private Texture2D texBarraFondo;
    private Texture2D texBarraVerde;
    private Texture2D texBarraAmarilla;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Asegurar que el Setup del Nivel 2 esté activo
        if (GetComponent<Nivel2Setup>() == null)
        {
            gameObject.AddComponent<Nivel2Setup>();
        }

        CrearTexturas();
    }

    private void OnEnable()
    {
        TrampaAcusticaNivel2.OnRuidoGenerado += MostrarAlertaRuido;
    }

    private void OnDisable()
    {
        TrampaAcusticaNivel2.OnRuidoGenerado -= MostrarAlertaRuido;
    }

    private void MostrarAlertaRuido(Vector3 pos, float radio, string etiqueta)
    {
        alertaRuidoTexto = etiqueta;
        tiempoAlertaRuido = 2.2f;
    }

    private void Update()
    {
        if (tiempoAlertaRuido > 0f)
        {
            tiempoAlertaRuido -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            VolverAlMenu();
        }
    }

    public void ActivarVictoria()
    {
        if (nivelCompletado || juegoTerminado) return;
        nivelCompletado = true;
        Time.timeScale = 0f;
        Debug.Log("<color=green>[Nivel2Manager] ¡Nivel 2 Superado con Éxito!</color>");
    }

    public void ActivarGameOver()
    {
        if (nivelCompletado || juegoTerminado) return;
        juegoTerminado = true;
        Time.timeScale = 0f;
        Debug.Log("<color=red>[Nivel2Manager] ¡La Momia que escucha te ha atrapado!</color>");
    }

    public void ReintentarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Nivel 2");
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    private void CrearTexturas()
    {
        texPanel = MakeTex(2, 2, new Color(0.06f, 0.07f, 0.09f, 0.92f));
        texBarraFondo = MakeTex(2, 2, new Color(0.12f, 0.12f, 0.16f, 0.9f));
        texBarraVerde = MakeTex(2, 2, new Color(0.2f, 0.85f, 0.35f, 1f));
        texBarraAmarilla = MakeTex(2, 2, new Color(0.95f, 0.75f, 0.15f, 1f));
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        // Si SistemaMomiaYMicrofonoNivel2 está activo, delega todo el HUD, modales y avisos a dicho sistema
        // para evitar textos duplicados o encimados en pantalla.
        if (SistemaMomiaYMicrofonoNivel2.Instance != null)
        {
            return;
        }

        InitStyles();

        // 1. HUD SUPERIOR: OBJETIVO Y CHECKLIST DE FASES
        float panelW = 340f;
        float panelH = 88f;
        GUI.Box(new Rect(12, 12, panelW, panelH), "", boxStyle);

        GUI.Label(new Rect(20, 14, panelW - 16, 18), "NIVEL 2: 1:00 AM - 2:00 AM", headerStyle);

        GeneradorPrincipalNivel2 gen = GeneradorPrincipalNivel2.Instance;
        int fuses = gen != null ? gen.fasesRestauradas : 0;
        int maxFuses = gen != null ? gen.fasesRequeridas : 3;

        bool f1 = gen != null && gen.EstaFaseRestaurada(1);
        bool f2 = gen != null && gen.EstaFaseRestaurada(2);
        bool f3 = gen != null && gen.EstaFaseRestaurada(3);

        string f1Str = f1 ? "<color=#00FFAA>[✓] 1.Calderas</color>" : "<color=#888888>[ ] 1.Calderas</color>";
        string f2Str = f2 ? "<color=#00FFAA>[✓] 2.Criptas</color>" : "<color=#888888>[ ] 2.Criptas</color>";
        string f3Str = f3 ? "<color=#00FFAA>[✓] 3.Subestación</color>" : "<color=#888888>[ ] 3.Subestación</color>";

        string faseStr = fuses >= maxFuses ? "<color=#00FFAA>¡ENERGÍA 100%! PUERTA ABIERTA</color>" : $"Energía: {fuses}/{maxFuses} Fases activadas";
        GUI.Label(new Rect(20, 32, panelW - 16, 18), faseStr, textStyle);
        GUI.Label(new Rect(20, 48, panelW - 16, 18), $"{f1Str}  {f2Str}  {f3Str}", textStyle);

        // Barra de energía
        float pct = Mathf.Clamp01((float)fuses / maxFuses);
        GUI.DrawTexture(new Rect(20, 68, panelW - 40, 8), texBarraFondo);
        GUI.DrawTexture(new Rect(20, 68, (panelW - 40) * pct, 8), fuses >= maxFuses ? texBarraVerde : texBarraAmarilla);

        // Indicador de navegación directo hacia la Compuerta de Salida cuando la energía está al 100%
        if (fuses >= maxFuses && !nivelCompletado && !juegoTerminado)
        {
            float exitW = 500f;
            float exitH = 36f;
            Rect exitRect = new Rect((Screen.width - exitW) / 2f, 14f, exitW, exitH);
            GUI.Box(exitRect, "", boxStyle);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Vigilante");
            float distSalida = player != null ? Vector2.Distance(player.transform.position, new Vector2(0f, 21.6f)) : 0f;

            GUIStyle exitStyle = new GUIStyle(textStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(exitRect, $"<color=#00FFAA>▲ ¡ENERGÍA RESTAURADA! VE A LA GRAN COMPUERTA NORTE ({distSalida:F0}m) ▲</color>", exitStyle);
        }

        // Banner de Alerta de Ruido en el centro de la pantalla
        if (tiempoAlertaRuido > 0f)
        {
            float alertW = 340f;
            float alertH = 34f;
            Rect alertRect = new Rect((Screen.width - alertW) / 2f, 55f, alertW, alertH);
            GUI.Box(alertRect, "", boxStyle);

            GUIStyle alertStyle = new GUIStyle(textStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(alertRect, $"<color=#FF3333>⚠ {alertaRuidoTexto} ⚠</color>", alertStyle);
        }

        // Mini guía de controles en el pie de pantalla
        GUI.Label(new Rect(12, Screen.height - 24, 600, 20),
            "<color=#888888>WASD/Flechas: Mover | Shift: Correr (¡Peligro de ruido!) | E: Interactuar/Esconderse | ESC: Menú</color>", textStyle);

        // 2. MODAL DE VICTORIA
        if (nivelCompletado)
        {
            float w = 460f;
            float h = 230f;
            Rect center = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.Box(center, "", boxStyle);
            GUI.Label(new Rect(center.x + 20, center.y + 20, w - 40, 35), "¡NIVEL 2 SUPERADO!", titleStyle);
            GUI.Label(new Rect(center.x + 20, center.y + 65, w - 40, 50),
                "Has restaurado la energía eléctrica y escapado del subsuelo subterráneo con éxito.", textStyle);

            if (GUI.Button(new Rect(center.x + (w - 220) / 2f, center.y + 150, 220, 42), "INICIAR NIVEL 3 ▶", buttonStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("Nivel 3");
            }
        }

        // 3. MODAL DE GAME OVER
        if (juegoTerminado)
        {
            float w = 440f;
            float h = 220f;
            Rect center = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.Box(center, "", boxStyle);
            GUI.Label(new Rect(center.x + 20, center.y + 20, w - 40, 35), "GAME OVER", titleStyle);
            GUI.Label(new Rect(center.x + 20, center.y + 65, w - 40, 50),
                "La momia que escucha detectó tus pasos en la oscuridad.\nRecuerda: camina en silencio y evita los charcos y cristales rotos.", textStyle);

            if (GUI.Button(new Rect(center.x + (w - 180) / 2f, center.y + 145, 180, 40), "REINTENTAR", buttonStyle))
            {
                ReintentarNivel();
            }
        }
    }

    private void InitStyles()
    {
        if (boxStyle != null) return;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = texPanel;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        headerStyle.normal.textColor = new Color(0.95f, 0.85f, 0.4f);

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(0.95f, 0.8f, 0.25f);

        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            richText = true
        };
        textStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        buttonStyle.normal.textColor = Color.white;
    }
}
