using System.Collections;
using UnityEngine;

/// <summary>
/// Monitor de CCTV en vivo para el Nivel 3.
/// Permite al vigilante ver el taller/museo EN TIEMPO REAL a través de las 3 cámaras de seguridad:
/// - CAM 01: Sala Quirúrgica (camillas, lámpara de cirugía, momias acostadas/despertando en vivo).
/// - CAM 02: Laboratorio Químico (mesas de reactivos, frascos con órganos y pizarra).
/// - CAM 03: Cámaras Frías de Morgue (batería de gavetas congeladas y niebla de hielo).
/// Al salir con [ESC], la cámara vuelve a enfocar al jugador fluidamente.
/// </summary>
public class CCTVMonitorNivel3 : MonoBehaviour
{
    public static CCTVMonitorNivel3 Instance { get; private set; }

    [Header("Configuración")]
    public float distanciaInteraccion = 2.6f;
    public bool monitorAbierto = false;
    public int camaraActual = 1;

    // Posiciones en el mapa de cada cámara de seguridad
    private static readonly Vector3 PosCam1 = new Vector3(0f, -2.0f, -10f);      // Camillas centrales
    private static readonly Vector3 PosCam2 = new Vector3(-14.5f, -2.0f, -10f);  // Lab químico
    private static readonly Vector3 PosCam3 = new Vector3(14.5f, -2.0f, -10f);   // Morgue fría
    private static readonly Vector3 PosCam4 = new Vector3(0f, 6.8f, -10f);       // Caseta y pasillo central

    private Camera cam;
    private CameraFollow cf;
    private Transform playerTransform;

    private float timerParpadeoRec = 0f;
    private bool recVisible = true;
    private float tiempoEnCam1 = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        cam = Camera.main;
        if (cam != null) cf = cam.GetComponent<CameraFollow>();
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

    private void Update()
    {
        if (playerTransform == null)
        {
            BuscarJugador();
            return;
        }

        if (Nivel3Manager.Instance != null && Nivel3Manager.Instance.JuegoPausadoOTerminado)
        {
            if (monitorAbierto) CerrarMonitor();
            return;
        }

        // Abrir/Cerrar monitor de cámaras EXCLUSIVAMENTE con la tecla TAB (Tabulador)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!monitorAbierto) AbrirMonitor();
            else CerrarMonitor();
        }

        if (monitorAbierto)
        {
            // Salir con Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CerrarMonitor();
                return;
            }

            // Cambiar canal de cámara con 1, 2, 3, 4
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) CambiarCamara(1);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) CambiarCamara(2);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) CambiarCamara(3);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) CambiarCamara(4);

            // Mantener la cámara enfocando el sector correspondiente
            ActualizarPosicionCamara();

            // Parpadeo del indicador REC
            timerParpadeoRec += Time.deltaTime;
            if (timerParpadeoRec >= 0.5f)
            {
                timerParpadeoRec = 0f;
                recVisible = !recVisible;
            }
        }
    }

    private void ActualizarPosicionCamara()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 posDestino = camaraActual == 1 ? PosCam1 : (camaraActual == 2 ? PosCam2 : (camaraActual == 3 ? PosCam3 : PosCam4));
        cam.transform.position = Vector3.Lerp(cam.transform.position, posDestino, Time.deltaTime * 12f);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, 6.0f, Time.deltaTime * 8f);
    }

    public void AbrirMonitor()
    {
        monitorAbierto = true;
        camaraActual = 1;
        tiempoEnCam1 = 0f;

        if (cf != null) cf.enabled = false;
        if (cam != null)
        {
            cam.transform.position = PosCam1;
            cam.orthographicSize = 6.0f;
        }
    }

    public void CerrarMonitor()
    {
        monitorAbierto = false;

        if (cam != null)
        {
            cam.orthographicSize = 3.6f;
            if (playerTransform != null)
            {
                cam.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            }
        }

        if (cf != null)
        {
            cf.enabled = true;
            if (playerTransform != null) cf.objetivo = playerTransform;
        }
    }

    public void CambiarCamara(int nuevaCam)
    {
        camaraActual = Mathf.Clamp(nuevaCam, 1, 4);
        if (cam != null)
        {
            Vector3 pos = camaraActual == 1 ? PosCam1 : (camaraActual == 2 ? PosCam2 : (camaraActual == 3 ? PosCam3 : PosCam4));
            cam.transform.position = pos;
        }
    }

    public bool EstaCerca()
    {
        if (playerTransform == null) return false;
        return Vector2.Distance(transform.position, playerTransform.position) <= distanciaInteraccion;
    }

    private void OnGUI()
    {
        if (!monitorAbierto) return;

        // Overlay cinematográfico de monitor de seguridad CRT
        float w = Screen.width;
        float h = Screen.height;

        // Marco sutil de pantalla CRT en bordes (arriba y abajo)
        Color colFrame = new Color(0.04f, 0.06f, 0.08f, 0.78f);
        GUI.color = colFrame;
        GUI.DrawTexture(new Rect(0, 0, w, 58), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, h - 52, w, 52), Texture2D.whiteTexture);
        // Tinte de señal de seguridad verdoso translúcido
        GUI.color = new Color(0.05f, 0.25f, 0.15f, 0.08f);
        GUI.DrawTexture(new Rect(0, 58, w, h - 110), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // --- BARRA SUPERIOR ---
        // Indicador REC
        if (recVisible)
        {
            GUIStyle recStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red }
            };
            GUI.Label(new Rect(24, 16, 80, 26), "● REC", recStyle);
        }

        // Título de la cámara en vivo
        string titCam = camaraActual == 1 ? "CAM 01: SALA QUIRÚRGICA (CAMILLAS)" :
                        camaraActual == 2 ? "CAM 02: LABORATORIO Y REACTIVOS" :
                        camaraActual == 3 ? "CAM 03: CÁMARAS FRÍAS DE MORGUE" :
                                            "CAM 04: CASETA Y PASILLOS NORTE";

        GUIStyle titStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.2f, 0.95f, 0.5f) }
        };
        GUI.Label(new Rect((w - 460) * 0.5f, 16, 460, 26), titCam + " [EN VIVO]", titStyle);

        // Selector de Canales (Botones 1, 2, 3, 4 en la esquina derecha superior)
        float btnW = 95f;
        float startX = w - 430f;
        for (int i = 1; i <= 4; i++)
        {
            string label = (i == 1 ? "CAM 1" : i == 2 ? "CAM 2" : i == 3 ? "CAM 3" : "CAM 4");
            if (i == camaraActual) label = "▶ " + label + " ◀";

            if (GUI.Button(new Rect(startX + (i - 1) * (btnW + 6), 12, btnW, 34), label))
            {
                CambiarCamara(i);
            }
        }

        // --- RETÍCULA Y MIRA DE CÁMARA EN EL CENTRO ---
        GUIStyle miraStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.2f, 0.9f, 0.5f, 0.35f) }
        };
        GUI.Label(new Rect((w - 100) * 0.5f, (h - 100) * 0.5f, 100, 100), "┼", miraStyle);

        // --- BARRA INFERIOR ---
        bool g1Despierta = ControladorDespertarMomiasNivel3.Instance != null && ControladorDespertarMomiasNivel3.Instance.gemela1Despierta;
        bool g2Despierta = ControladorDespertarMomiasNivel3.Instance != null && ControladorDespertarMomiasNivel3.Instance.gemela2Despierta;

        string telemetria = "MOMIA 1: " + (g1Despierta ? "<color=red>ACECHANDO</color>" : "<color=green>EN CAMILLA</color>") +
                            "  |  MOMIA 2: " + (g2Despierta ? "<color=red>PATRULLANDO</color>" : "<color=green>EN CAMILLA</color>") +
                            "  |  ESCORPIONES: " + (g2Despierta ? "<color=orange>ACTIVOS EN DRENAJES</color>" : "<color=gray>INACTIVOS</color>");

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.85f, 0.92f, 0.95f) }
        };
        GUI.Label(new Rect(24, h - 42, 600, 30), telemetria, infoStyle);

        if (GUI.Button(new Rect(w - 230, h - 44, 210, 36), "CERRAR MONITOR [ESC]"))
        {
            CerrarMonitor();
        }
    }
}
