using System.Collections;
using UnityEngine;

/// <summary>
/// Teclado numérico y Caja Fuerte en la Caseta de Vigilancia — Nivel 3.
/// Código correcto: 7 - 3 - 9.
/// Al abrirse, otorga la Llave Maestra para escapar por la puerta trasera.
/// Si se comete un error, activa una alarma estridente que atrae a la Gemela Ciega directo a la caseta.
/// </summary>
public class KeypadCajaFuerteNivel3 : MonoBehaviour
{
    [Header("Estado")]
    public bool cajaAbierta = false;
    public float distanciaInteraccion = 2.4f;
    private bool modalAbierto = false;

    private string codigoIngresado = "";
    private const string CODIGO_CORRECTO = "739";
    private string mensajeError = "";
    private float timerError = 0f;

    private Transform playerTransform;
    private AudioSource audioSource;

    public static KeypadCajaFuerteNivel3 Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
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

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    Vector2 worldTap = Camera.main.ScreenToWorldPoint(t.position);
                    Collider2D col = GetComponent<Collider2D>();
                    if (col != null && col.OverlapPoint(worldTap)) return true;
                    if (Vector2.Distance(worldTap, transform.position) <= 2.2f) return true;
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldTap = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldTap)) return true;
            if (Vector2.Distance(worldTap, transform.position) <= 2.2f) return true;
        }
        return false;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            BuscarJugador();
            return;
        }

        if (cajaAbierta || (Nivel3Manager.Instance != null && Nivel3Manager.Instance.JuegoPausadoOTerminado)) return;

        bool cerca = EstaCerca();
        bool toque = ComprobarToqueDirecto();

        if (Input.GetKeyDown(KeyCode.F) || ((cerca || toque) && (TouchControlsManager.IsInteractTriggered() || toque)))
        {
            if (modalAbierto) CerrarKeypad();
            else if (cerca || toque) AbrirKeypad();
        }
        else if (cerca && !modalAbierto && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            AbrirKeypad();
        }
        else if (modalAbierto)
        {
            Collider2D col = GetComponent<Collider2D>();
            float d = col != null ? Vector2.Distance(col.ClosestPoint(playerTransform.position), playerTransform.position) : Vector2.Distance(transform.position, playerTransform.position);
            if (d > 5.5f) CerrarKeypad();
        }

        if (modalAbierto)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CerrarKeypad();
            }

            // Entrada por teclado físico
            ProcesarEntradaTeclado();
        }

        if (timerError > 0f)
        {
            timerError -= Time.deltaTime;
            if (timerError <= 0f) mensajeError = "";
        }
    }

    private void ProcesarEntradaTeclado()
    {
        for (int i = 0; i <= 9; i++)
        {
            KeyCode key = KeyCode.Alpha0 + i;
            KeyCode keyPad = KeyCode.Keypad0 + i;
            if (Input.GetKeyDown(key) || Input.GetKeyDown(keyPad))
            {
                AgregarDigito(i.ToString());
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            BorrarUltimo();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            VerificarCodigo();
        }
    }

    public void AbrirKeypad()
    {
        modalAbierto = true;
        codigoIngresado = "";
        mensajeError = "";
    }

    public void CerrarKeypad()
    {
        modalAbierto = false;
    }

    public void AgregarDigito(string d)
    {
        if (codigoIngresado.Length < 3)
        {
            codigoIngresado += d;
            if (codigoIngresado.Length == 3)
            {
                VerificarCodigo();
            }
        }
    }

    public void BorrarUltimo()
    {
        if (codigoIngresado.Length > 0)
        {
            codigoIngresado = codigoIngresado.Substring(0, codigoIngresado.Length - 1);
        }
    }

    public void VerificarCodigo()
    {
        if (codigoIngresado == CODIGO_CORRECTO)
        {
            cajaAbierta = true;
            modalAbierto = false;

            if (Nivel3Manager.Instance != null)
            {
                Nivel3Manager.Instance.AbrirCajaFuerteExito();
            }
        }
        else
        {
            // Código incorrecto: Alarma estridente
            mensajeError = "¡CÓDIGO INVÁLIDO! ACCESO DENEGADO";
            timerError = 2.5f;
            codigoIngresado = "";

            if (MomiaCiegaNivel3.Instance != null)
            {
                MomiaCiegaNivel3.Instance.AlertarRuido(transform.position, "¡Alarma de la caja fuerte! La Gemela Ciega corre a la caseta");
            }
        }
    }

    // Texturas procedurales de alta calidad para el panel de la caja fuerte
    private Texture2D texFondoAcero;
    private Texture2D texPantallaCRT;
    private Texture2D texBotonMetal;
    private Texture2D texBotonPulsado;

    private void InicializarTexturasUI()
    {
        if (texFondoAcero != null) return;

        // 1. Textura de acero blindado con remaches y óxido
        texFondoAcero = new Texture2D(64, 64);
        Color colAcero = new Color(0.14f, 0.16f, 0.18f);
        Color[] colsAcero = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float ruido = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.08f - 0.04f;
                bool borde = x <= 2 || x >= 61 || y <= 2 || y >= 61;
                colsAcero[y * 64 + x] = borde ? new Color(0.35f, 0.38f, 0.42f) : new Color(colAcero.r + ruido, colAcero.g + ruido, colAcero.b + ruido, 0.98f);
            }
        }
        texFondoAcero.SetPixels(colsAcero);
        texFondoAcero.Apply();

        // 2. Pantalla CRT con verde fósforo y líneas de escaneo
        texPantallaCRT = new Texture2D(32, 32);
        Color[] colsCRT = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float scanline = (y % 2 == 0) ? 0.7f : 1.0f;
                colsCRT[y * 32 + x] = new Color(0.02f * scanline, 0.18f * scanline, 0.08f * scanline, 1f);
            }
        }
        texPantallaCRT.SetPixels(colsCRT);
        texPantallaCRT.Apply();

        // 3. Botones metálicos biselados
        texBotonMetal = new Texture2D(32, 32);
        Color[] colsBtn = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool bordeSupIzq = (x == 1 || y == 30);
                bool bordeInfDer = (x == 30 || y == 1);
                if (bordeSupIzq) colsBtn[y * 32 + x] = new Color(0.45f, 0.48f, 0.52f);
                else if (bordeInfDer) colsBtn[y * 32 + x] = new Color(0.10f, 0.11f, 0.12f);
                else colsBtn[y * 32 + x] = new Color(0.24f, 0.26f, 0.28f);
            }
        }
        texBotonMetal.SetPixels(colsBtn);
        texBotonMetal.Apply();
    }

    private void OnGUI()
    {
        if (!modalAbierto || cajaAbierta) return;
        InicializarTexturasUI();

        ResponsiveGUIHelper.BeginScaledGUI(out float vw, out float vh, out Matrix4x4 prevMatrix, 860f, 480f);
        try
        {
            // Oscurecer fondo de juego
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, vw, vh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = Mathf.Min(350f, vw - 24f);
            float h = Mathf.Min(400f, vh - 24f);
            float x = (vw - w) * 0.5f;
            float y = (vh - h) * 0.5f;

            // Panel de Acero de la Caja Fuerte
            GUI.DrawTexture(new Rect(x, y, w, h), texFondoAcero);

            // Marco exterior reforzado de bronce/acero
            GUI.color = new Color(0.55f, 0.45f, 0.25f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, w, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - 3f, w, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, 3f, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + w - 3f, y, 3f, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Título Industrial
            GUIStyle estiloHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.35f) }
            };
            GUI.Label(new Rect(x + 10, y + 8, w - 20, 18), "CÁMARA ACORAZADA — CAJA DE SEGURIDAD", estiloHeader);

            GUIStyle estiloSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.65f, 0.72f, 0.80f) }
            };
            GUI.Label(new Rect(x + 10, y + 26, w - 20, 14), "MUSEO REGIONAL • SISTEMA COMBINATORIO 1937", estiloSub);

            // Pantalla Digital CRT de Fósforo Verde (o Rojo si hay error)
            Rect rectPantalla = new Rect(x + 25, y + 44, w - 50, 52);
            GUI.DrawTexture(rectPantalla, texPantallaCRT);

            // Marco de la pantalla
            Color colBordePantalla = timerError > 0f ? Color.red : new Color(0.15f, 0.8f, 0.35f);
            GUI.color = colBordePantalla;
            GUI.DrawTexture(new Rect(rectPantalla.x, rectPantalla.y, rectPantalla.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rectPantalla.x, rectPantalla.y + rectPantalla.height - 2f, rectPantalla.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rectPantalla.x, rectPantalla.y, 2f, rectPantalla.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rectPantalla.x + rectPantalla.width - 2f, rectPantalla.y, 2f, rectPantalla.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Texto en pantalla digital
            string displayStr = "[ " + (codigoIngresado.Length > 0 ? codigoIngresado[0].ToString() : "_") + " ]   " +
                                "[ " + (codigoIngresado.Length > 1 ? codigoIngresado[1].ToString() : "_") + " ]   " +
                                "[ " + (codigoIngresado.Length > 2 ? codigoIngresado[2].ToString() : "_") + " ]";

            GUIStyle estiloDisplay = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = timerError > 0f ? Color.red : new Color(0.2f, 1.0f, 0.45f) }
            };
            GUI.Label(new Rect(rectPantalla.x, rectPantalla.y + 4, rectPantalla.width, 28), displayStr, estiloDisplay);

            // Estado bajo la pantalla
            string txtEstado = timerError > 0f ? "⚠ ¡ALARMA! CÓDIGO INVÁLIDO ⚠" : "INGRESE 3 CIFRAS";
            GUIStyle estiloSubPantalla = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = timerError > 0f ? Color.red : new Color(0.45f, 0.85f, 0.55f) }
            };
            GUI.Label(new Rect(rectPantalla.x, rectPantalla.y + 32, rectPantalla.width, 16), txtEstado, estiloSubPantalla);

            // Teclado Numérico 3x3
            float btnW = 68f;
            float btnH = 38f;
            float gapX = 10f;
            float gapY = 8f;
            float startX = x + (w - (btnW * 3 + gapX * 2)) * 0.5f;
            float startY = y + 104f;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
            };

            int num = 1;
            for (int fila = 0; fila < 3; fila++)
            {
                for (int col = 0; col < 3; col++)
                {
                    string sNum = num.ToString();
                    Rect rBtn = new Rect(startX + col * (btnW + gapX), startY + fila * (btnH + gapY), btnW, btnH);
                    if (GUI.Button(rBtn, sNum, btnStyle))
                    {
                        AgregarDigito(sNum);
                    }
                    num++;
                }
            }

            // Fila inferior: DEL, 0, OK
            float fila4Y = startY + 3 * (btnH + gapY);

            GUIStyle btnDelStyle = new GUIStyle(btnStyle);
            btnDelStyle.fontSize = 14;
            btnDelStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
            if (GUI.Button(new Rect(startX, fila4Y, btnW, btnH), "DEL", btnDelStyle))
            {
                BorrarUltimo();
            }

            if (GUI.Button(new Rect(startX + (btnW + gapX), fila4Y, btnW, btnH), "0", btnStyle))
            {
                AgregarDigito("0");
            }

            GUIStyle btnOkStyle = new GUIStyle(btnStyle);
            btnOkStyle.fontSize = 14;
            btnOkStyle.normal.textColor = new Color(0.3f, 1f, 0.5f);
            if (GUI.Button(new Rect(startX + 2 * (btnW + gapX), fila4Y, btnW, btnH), "OK", btnOkStyle))
            {
                VerificarCodigo();
            }

            // Libreta de pistas de ayuda
            int? c1 = Nivel3Manager.Instance != null ? Nivel3Manager.Instance.cifra1 : null;
            int? c2 = Nivel3Manager.Instance != null ? Nivel3Manager.Instance.cifra2 : null;
            int? c3 = Nivel3Manager.Instance != null ? Nivel3Manager.Instance.cifra3 : null;

            string ayudaLibreta = "PISTAS:  " +
                                  "Camilla: [" + (c1.HasValue ? c1.Value.ToString() : "_") + "]  " +
                                  "Lab: [" + (c2.HasValue ? c2.Value.ToString() : "_") + "]  " +
                                  "Morgue: [" + (c3.HasValue ? c3.Value.ToString() : "_") + "]";

            GUIStyle estiloPistas = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.3f) }
            };
            GUI.Label(new Rect(x + 10, y + 296f, w - 20, 16), ayudaLibreta, estiloPistas);

            // Botón Salir / Cancelar
            GUIStyle btnSalir = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            if (GUI.Button(new Rect(x + (w - 180f) * 0.5f, y + h - 46f, 180f, 32f), "CERRAR KEYPAD [F / ESC]", btnSalir))
            {
                CerrarKeypad();
            }
        }
        finally
        {
            ResponsiveGUIHelper.EndScaledGUI(prevMatrix);
        }
    }

    public bool EstaCerca()
    {
        if (playerTransform == null || cajaAbierta) return false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 closest = col.ClosestPoint(playerTransform.position);
            return Vector2.Distance(closest, playerTransform.position) <= 2.8f;
        }
        return Vector2.Distance(transform.position, playerTransform.position) <= 4.0f;
    }
}
