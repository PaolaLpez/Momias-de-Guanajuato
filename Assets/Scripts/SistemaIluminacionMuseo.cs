using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class SistemaIluminacionMuseo : MonoBehaviour
{
    public static SistemaIluminacionMuseo Instance { get; private set; }

    [Header("Estado de Luces")]
    public bool lucesEncendidas = true;

    [Header("Ajustes de Intensidad")]
    [SerializeField] private float intensidadGlobalEncendida = 1.15f;
    [SerializeField] private float intensidadGlobalApagada = 0.08f;

    [Header("Colores")]
    [SerializeField] private Color colorLuzMuseo = new Color(1f, 0.96f, 0.90f, 1f);
    [SerializeField] private Color fondoCamaraEncendida = new Color(0.18f, 0.17f, 0.22f, 1f);
    [SerializeField] private Color fondoCamaraApagada = new Color(0.02f, 0.02f, 0.03f, 1f);

    private Camera cam;
    private Light2D luzGlobal;

    private void Awake()
    {
        if (Application.isPlaying && Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        cam = Camera.main;
        ConfigurarLuzGlobal();
        LimpiarLinternasEnNivel1();
        AplicarEstadoLuces();
    }

    private void OnEnable()
    {
        cam = Camera.main;
        ConfigurarLuzGlobal();
        LimpiarLinternasEnNivel1();
        AplicarEstadoLuces();
    }

    private void Start()
    {
        LimpiarLinternasEnNivel1();
        AplicarEstadoLuces();
    }

    private void Update()
    {
        // Tecla L para alternar las luces del museo entre Encendidas y Apagadas
        if (Input.GetKeyDown(KeyCode.L))
        {
            AlternarLuces();
        }

        // Mantener a la vigilante sin linterna en Nivel 1
        if (Application.isPlaying && Time.frameCount % 60 == 0)
        {
            LimpiarLinternasEnNivel1();
        }
    }

    public void AlternarLuces()
    {
        lucesEncendidas = !lucesEncendidas;
        AplicarEstadoLuces();
        string estado = lucesEncendidas ? "<color=#FFD700>ENCENDIDAS (Museo Iluminado)</color>" : "<color=#FF6666>APAGADAS (Modo Penumbra y Tenebroso)</color>";
        Debug.Log($"[SistemaIluminacion] Luces del museo: {estado}");
    }

    public void EncenderLuces()
    {
        lucesEncendidas = true;
        AplicarEstadoLuces();
    }

    public void ApagarLuces()
    {
        lucesEncendidas = false;
        AplicarEstadoLuces();
    }

    private void ConfigurarLuzGlobal()
    {
        GameObject lgObj = GameObject.Find("LuzGlobalMuseo");
        if (lgObj == null)
        {
            lgObj = new GameObject("LuzGlobalMuseo");
            luzGlobal = lgObj.AddComponent<Light2D>();
        }
        else
        {
            luzGlobal = lgObj.GetComponent<Light2D>();
            if (luzGlobal == null) luzGlobal = lgObj.AddComponent<Light2D>();
        }

        luzGlobal.lightType = Light2D.LightType.Global;
        luzGlobal.color = colorLuzMuseo;
        luzGlobal.intensity = lucesEncendidas ? intensidadGlobalEncendida : intensidadGlobalApagada;
    }

    public static void LimpiarLinternasEnNivel1()
    {
        // En el Nivel 1 el jugador NO lleva linterna en la mano ni cono de luz.
        // La linterna se introduce hasta el Nivel 2.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Transform lint1 = player.transform.Find("LinternaVigilante");
            if (lint1 != null)
            {
                if (Application.isPlaying) Destroy(lint1.gameObject);
                else DestroyImmediate(lint1.gameObject);
            }
            Transform lint2 = player.transform.Find("LinternaSubterraneo");
            if (lint2 != null)
            {
                if (Application.isPlaying) Destroy(lint2.gameObject);
                else DestroyImmediate(lint2.gameObject);
            }
            Transform lint3 = player.transform.Find("VisualLinternaMano");
            if (lint3 != null)
            {
                if (Application.isPlaying) Destroy(lint3.gameObject);
                else DestroyImmediate(lint3.gameObject);
            }
        }
    }

    private void AplicarEstadoLuces()
    {
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = lucesEncendidas ? fondoCamaraEncendida : fondoCamaraApagada;
        }

        if (luzGlobal == null)
        {
            ConfigurarLuzGlobal();
        }

        if (luzGlobal != null)
        {
            luzGlobal.intensity = lucesEncendidas ? intensidadGlobalEncendida : intensidadGlobalApagada;
        }
    }

    private void OnGUI()
    {
        if (Level1Manager.Instance != null && (Level1Manager.Instance.IsNivelCompletado || Level1Manager.Instance.IsJuegoTerminado)) return;

        // Factor de escala responsivo sincronizado con Level1Manager
        float s = Mathf.Clamp(Screen.height / 540f, 1.0f, 2.2f);

        float btnW = 180f * s;
        float btnH = 26f * s;
        float pad = 10f * s;
        float boxH = 64f * s;
        float rectY = pad + boxH + 6f * s;
        Rect rect = new Rect(Screen.width - btnW - pad, rectY, btnW, btnH);

        string label = lucesEncendidas ? "[L] Luces: <color=#FFD700>ENCENDIDAS</color>" : "[L] Luces: <color=#FFAAAA>APAGADAS</color>";

        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = Mathf.RoundToInt(11 * s);
        style.fontStyle = FontStyle.Bold;
        style.richText = true;

        if (GUI.Button(rect, label, style))
        {
            AlternarLuces();
        }
    }
}
