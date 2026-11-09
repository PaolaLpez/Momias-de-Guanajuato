using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema avanzado de controles táctiles para Celulares, Tablets y WebGL:
/// 1. JOYSTICK FLOTANTE DINÁMICO:
///    - Completamente invisible cuando no se toca la pantalla.
///    - Aparece exactamente donde pongas el dedo en la mitad izquierda de la pantalla.
///    - Desaparece automáticamente en cuanto levantas el dedo.
/// 2. CONTROL ANALÓGICO DE VELOCIDAD (Caminar vs Correr):
///    - Empujar suave el joystick (< 75% del radio) = CAMINA.
///    - Empujar a fondo el joystick (>= 75% del radio) = CORRE automáticamente.
///    - Botón de Correr: permite forzar modo carrera fija.
/// 3. INTERACCIÓN TÁCTIL DIRECTA:
///    - Tocar en pantalla objetos, fichas de historia del Nivel 1 o puertas para interactuar directamente.
/// </summary>
public class TouchControlsManager : MonoBehaviour
{
    public static TouchControlsManager Instance { get; private set; }

    // --- Entradas Globales accesibles por cualquier script ---
    public static Vector2 Movement { get; private set; } = Vector2.zero;
    public static float Horizontal => Movement.x;
    public static float Vertical => Movement.y;

    public static bool IsRunning => (Instance != null && Instance.botonCorrerForzado);
    public static bool IsStealth => (Instance != null && Instance.sigiloActivo);
    public static bool IsInteractHeld { get; private set; } = false;
    public static bool IsJumpHeld { get; private set; } = false;
    public static bool IsVoiceSimulated { get; set; } = false;

    private static bool justPressedInteract = false;
    private static bool justPressedJump = false;

    public static bool IsInteractTriggered()
    {
        return Input.GetKeyDown(KeyCode.E) || justPressedInteract;
    }

    public static bool IsJumpTriggered()
    {
        return Input.GetKeyDown(KeyCode.Space) || justPressedJump;
    }

    // --- Componentes UI ---
    private Canvas touchCanvas;
    private CanvasScaler canvasScaler;
    private RectTransform canvasRect;

    // Joystick Flotante
    private RectTransform joystickBaseRect;
    private RectTransform joystickKnobRect;
    private Image joystickBaseImage;
    private Image joystickKnobImage;

    // Botones de acción (Abajo a la derecha)
    private RectTransform btnInteractRect;
    private RectTransform btnJumpRect;
    private RectTransform btnRunRect;
    private RectTransform btnStealthRect;
    private RectTransform btnMapRect;
    private RectTransform btnCCTVRect;

    private Image btnRunImage;
    private Text btnRunText;
    private Text btnRunSubText;
    private Image btnStealthImage;

    // Estados de control
    private bool controlesVisibles = true;
    private bool joystickCorriendo = false;
    private bool botonCorrerForzado = false;
    private bool sigiloActivo = false;

    // Variables del Joystick Flotante
    private int activeFingerId = -1;
    private Vector2 joystickOriginCanvasPos;
    private float joystickRadius = 90f;
    private const float UMBRAL_CORRER_JOYSTICK = 0.72f; // Si se empuja más del 72%, activa correr

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInicializar()
    {
        if (Instance == null)
        {
            GameObject managerObj = new GameObject("TouchControlsManager");
            managerObj.AddComponent<TouchControlsManager>();
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
        DontDestroyOnLoad(gameObject);

        GarantizarEventSystem();
        CrearInterfazTactil();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GarantizarEventSystem();
        sigiloActivo = false;
        botonCorrerForzado = false;
        ActualizarColorBotonStealth();
        ActualizarVisualBotonCorrer();
        ActualizarVisibilidadSegunNivel(scene.name);
    }

    public void SetStealth(bool active)
    {
        sigiloActivo = active;
        ActualizarColorBotonStealth();
    }

    private void GarantizarEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }
    }

    private void ActualizarVisibilidadSegunNivel(string sceneName)
    {
        string s = sceneName.ToLower();
        bool esNivel2 = s.Contains("nivel 2") || s.Contains("nivel2");
        bool esNivel3 = s.Contains("nivel 3") || s.Contains("nivel3");

        if (btnStealthRect != null) btnStealthRect.gameObject.SetActive(esNivel2);
        if (btnMapRect != null) btnMapRect.gameObject.SetActive(esNivel2);
        if (btnCCTVRect != null) btnCCTVRect.gameObject.SetActive(esNivel3);

        // Restablecer joystick oculto al cambiar de escena
        OcultarJoystick();
    }

    private void CrearInterfazTactil()
    {
        // 1. Crear Canvas de Pantalla Completa
        GameObject canvasObj = new GameObject("TouchControls_Canvas");
        canvasObj.transform.SetParent(transform, false);

        touchCanvas = canvasObj.AddComponent<Canvas>();
        touchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        touchCanvas.sortingOrder = 9999;

        canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Texturas generadas proceduralmente con fondo sólido oscuro y borde grueso de alto contraste
        Sprite circleSprite = CrearSpriteCirculo(160, 5, new Color(0.08f, 0.12f, 0.18f, 0.65f), new Color(0.3f, 0.8f, 1f, 0.85f));
        Sprite knobSprite = CrearSpriteCirculo(96, 4, new Color(0.25f, 0.45f, 0.65f, 0.90f), new Color(1f, 1f, 1f, 0.95f));
        Sprite actionSpriteE = CrearSpriteCirculo(160, 6, new Color(0.12f, 0.11f, 0.16f, 0.92f), new Color(1f, 0.85f, 0.18f, 1f));
        Sprite actionSpriteJump = CrearSpriteCirculo(160, 6, new Color(0.08f, 0.14f, 0.20f, 0.92f), new Color(0.28f, 0.88f, 1f, 1f));
        Sprite actionSpriteRun = CrearSpriteCirculo(160, 6, new Color(0.16f, 0.11f, 0.08f, 0.92f), new Color(1f, 0.65f, 0.18f, 1f));
        Sprite actionSpriteStealth = CrearSpriteCirculo(160, 6, new Color(0.16f, 0.09f, 0.20f, 0.92f), new Color(0.85f, 0.45f, 1f, 1f));
        Sprite actionSpriteMap = CrearSpriteCirculo(160, 6, new Color(0.20f, 0.15f, 0.08f, 0.92f), new Color(0.95f, 0.78f, 0.35f, 1f));

        // 2. JOYSTICK FLOTANTE (Inicia oculto)
        GameObject joyBase = new GameObject("Joystick_Base_Flotante");
        joyBase.transform.SetParent(canvasObj.transform, false);
        joystickBaseRect = joyBase.AddComponent<RectTransform>();
        joystickBaseRect.anchorMin = Vector2.zero;
        joystickBaseRect.anchorMax = Vector2.zero;
        joystickBaseRect.pivot = new Vector2(0.5f, 0.5f);
        joystickBaseRect.sizeDelta = new Vector2(joystickRadius * 2f, joystickRadius * 2f);

        joystickBaseImage = joyBase.AddComponent<Image>();
        joystickBaseImage.sprite = circleSprite;
        joystickBaseImage.color = Color.white;
        joystickBaseImage.raycastTarget = false;

        GameObject joyKnob = new GameObject("Joystick_Knob");
        joyKnob.transform.SetParent(joyBase.transform, false);
        joystickKnobRect = joyKnob.AddComponent<RectTransform>();
        joystickKnobRect.anchorMin = new Vector2(0.5f, 0.5f);
        joystickKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
        joystickKnobRect.pivot = new Vector2(0.5f, 0.5f);
        joystickKnobRect.anchoredPosition = Vector2.zero;
        joystickKnobRect.sizeDelta = new Vector2(76f, 76f);

        joystickKnobImage = joyKnob.AddComponent<Image>();
        joystickKnobImage.sprite = knobSprite;
        joystickKnobImage.color = Color.white;
        joystickKnobImage.raycastTarget = false;

        // Comienza oculto: solo aparecerá donde toques la pantalla
        joystickBaseRect.gameObject.SetActive(false);

        // 3. BOTÓN "E" (Interactuar / Abrir / Fichas de Historia / Ocultarse)
        btnInteractRect = CrearBotonAccion(canvasObj.transform, "Btn_Interactuar",
            new Vector2(-115f, 135f), new Vector2(106f, 106f),
            actionSpriteE, "E", "ACCIÓN", () =>
            {
                justPressedInteract = true;
            });

        // 4. BOTÓN "SALTO" (Espacio)
        btnJumpRect = CrearBotonAccion(canvasObj.transform, "Btn_Salto",
            new Vector2(-235f, 85f), new Vector2(92f, 92f),
            actionSpriteJump, "▲", "SALTO", () =>
            {
                justPressedJump = true;
            });

        // 5. BOTÓN "CORRER" (Opción de alternar Correr vs Caminar)
        btnRunRect = CrearBotonAccion(canvasObj.transform, "Btn_Correr",
            new Vector2(-95f, 260f), new Vector2(92f, 92f),
            actionSpriteRun, "👟", "CAMINAR", () =>
            {
                botonCorrerForzado = !botonCorrerForzado;
                ActualizarVisualBotonCorrer();
            });
        btnRunImage = btnRunRect.GetComponent<Image>();
        Transform iconT = btnRunRect.Find("IconText");
        if (iconT != null) btnRunText = iconT.GetComponent<Text>();
        Transform subT = btnRunRect.Find("SubText");
        if (subT != null) btnRunSubText = subT.GetComponent<Text>();

        // 6. BOTÓN "SIGILO" (Nivel 2 / Modo Agachado / Ctrl) - Toggle
        btnStealthRect = CrearBotonAccion(canvasObj.transform, "Btn_Sigilo",
            new Vector2(-215f, 195f), new Vector2(92f, 92f),
            actionSpriteStealth, "👣", "SIGILO", () =>
            {
                sigiloActivo = !sigiloActivo;
                ActualizarColorBotonStealth();
            });
        btnStealthImage = btnStealthRect.GetComponent<Image>();

        // 7. BOTÓN "PLANO" (Nivel 2 / Mapa Arquitectónico del Subsuelo)
        btnMapRect = CrearBotonAccion(canvasObj.transform, "Btn_Plano",
            new Vector2(-215f, 305f), new Vector2(92f, 92f),
            actionSpriteMap, "🗺", "PLANO", () =>
            {
                if (PlanoSubterraneoNivel2.Instance != null)
                {
                    PlanoSubterraneoNivel2.Instance.AlternarVisorPlano();
                }
            });

        // 8. BOTÓN "CCTV" (Nivel 3 / Cámaras de Seguridad)
        Sprite actionSpriteCCTV = CrearSpriteCirculo(160, 6, new Color(0.08f, 0.18f, 0.14f, 0.92f), new Color(0.25f, 0.95f, 0.65f, 1f));
        btnCCTVRect = CrearBotonAccion(canvasObj.transform, "Btn_CCTV",
            new Vector2(-215f, 195f), new Vector2(92f, 92f),
            actionSpriteCCTV, "📹", "CÁMARAS", () =>
            {
                if (CCTVMonitorNivel3.Instance != null)
                {
                    if (!CCTVMonitorNivel3.Instance.monitorAbierto) CCTVMonitorNivel3.Instance.AbrirMonitor();
                    else CCTVMonitorNivel3.Instance.CerrarMonitor();
                }
            });

        // 9. BOTÓN OCULTAR/MOSTRAR TÁCTIL (Arriba a la izquierda, discreto)
        CrearBotonToggleUI(canvasObj.transform);

        ActualizarVisibilidadSegunNivel(SceneManager.GetActiveScene().name);
        ActualizarVisualBotonCorrer();
        ActualizarColorBotonStealth();
    }

    private void ActualizarVisualBotonCorrer()
    {
        if (btnRunImage != null)
        {
            // Activo (corriendo): tono dorado cálido; Fijo (caminando): tono normal oscuro elegante
            btnRunImage.color = botonCorrerForzado ? new Color(1f, 0.72f, 0.15f, 1f) : new Color(0.22f, 0.28f, 0.35f, 0.92f);
        }
        if (btnRunText != null)
        {
            btnRunText.color = Color.white;
            btnRunText.text = botonCorrerForzado ? "⚡" : "👟";
        }
        if (btnRunSubText != null)
        {
            btnRunSubText.text = botonCorrerForzado ? "CORRER" : "CAMINAR";
            btnRunSubText.color = botonCorrerForzado ? new Color(1f, 0.95f, 0.5f, 1f) : new Color(0.9f, 0.95f, 1f, 1f);
        }
    }

    private void ActualizarColorBotonStealth()
    {
        if (btnStealthImage != null)
        {
            btnStealthImage.color = sigiloActivo ? new Color(1f, 0.45f, 1f, 1f) : new Color(0.25f, 0.16f, 0.32f, 0.92f);
        }
        Transform subT = btnStealthRect != null ? btnStealthRect.Find("SubText") : null;
        if (subT != null)
        {
            Text subTxt = subT.GetComponent<Text>();
            if (subTxt != null)
            {
                subTxt.text = sigiloActivo ? "AGACHADA" : "SIGILO";
                subTxt.color = sigiloActivo ? new Color(1f, 0.85f, 1f, 1f) : new Color(0.92f, 0.88f, 1f, 1f);
            }
        }
    }

    private RectTransform CrearBotonAccion(Transform parent, string nombre, Vector2 posAnchored, Vector2 size, Sprite sprite, string icono, string textoSub, UnityEngine.Events.UnityAction accion)
    {
        GameObject btnObj = new GameObject(nombre);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posAnchored;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cb.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(accion);

        EventTrigger et = btnObj.AddComponent<EventTrigger>();
        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener((d) =>
        {
            if (nombre == "Btn_Interactuar") { justPressedInteract = true; IsInteractHeld = true; }
            else if (nombre == "Btn_Salto") { justPressedJump = true; IsJumpHeld = true; }
        });
        et.triggers.Add(down);

        EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener((d) =>
        {
            if (nombre == "Btn_Interactuar") { IsInteractHeld = false; }
            else if (nombre == "Btn_Salto") { IsJumpHeld = false; }
        });
        et.triggers.Add(up);

        // 1. Contenedor Superior del Ícono / Letra (65% superior, completamente separado del texto)
        GameObject txtObj = new GameObject("IconText");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0f, 0.35f);
        txtRt.anchorMax = new Vector2(1f, 0.95f);
        txtRt.pivot = new Vector2(0.5f, 0.5f);
        txtRt.anchoredPosition = Vector2.zero;
        txtRt.sizeDelta = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = icono;
        txt.fontSize = (int)(size.y * 0.40f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.raycastTarget = false; // Permite clics en laptop sin bloquear el botón

        Outline outl = txtObj.AddComponent<Outline>();
        outl.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outl.effectDistance = new Vector2(2f, -2f);

        // 2. Contenedor Inferior del Subtítulo (35% inferior, perfectamente visible y legible)
        if (!string.IsNullOrEmpty(textoSub))
        {
            GameObject subObj = new GameObject("SubText");
            subObj.transform.SetParent(btnObj.transform, false);
            RectTransform subRt = subObj.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0.05f);
            subRt.anchorMax = new Vector2(1f, 0.38f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            subRt.anchoredPosition = Vector2.zero;
            subRt.sizeDelta = Vector2.zero;

            Text subTxt = subObj.AddComponent<Text>();
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.text = textoSub;
            subTxt.fontSize = (int)(size.y * 0.17f);
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.fontStyle = FontStyle.Bold;
            subTxt.color = new Color(1f, 0.95f, 0.75f, 1f); // Marfil dorado nítido
            subTxt.raycastTarget = false; // Permite clics en laptop sin bloquear el botón

            Outline subOutl = subObj.AddComponent<Outline>();
            subOutl.effectColor = new Color(0f, 0f, 0f, 0.95f);
            subOutl.effectDistance = new Vector2(1.5f, -1.5f);
        }

        return rt;
    }

    private void CrearBotonToggleUI(Transform parent)
    {
        GameObject toggleObj = new GameObject("Btn_ToggleTouch");
        toggleObj.transform.SetParent(parent, false);

        RectTransform rt = toggleObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(110f, 32f);

        Image img = toggleObj.AddComponent<Image>();
        img.color = new Color(0.08f, 0.12f, 0.18f, 0.65f);

        Button btn = toggleObj.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(toggleObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = "🎮 TÁCTIL";
        txt.fontSize = 13;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;
        txt.color = new Color(0.7f, 0.9f, 1f, 0.9f);

        btn.onClick.AddListener(() =>
        {
            controlesVisibles = !controlesVisibles;
            string scName = SceneManager.GetActiveScene().name.ToLower();
            bool esN2 = scName.Contains("nivel 2") || scName.Contains("nivel2");
            bool esN3 = scName.Contains("nivel 3") || scName.Contains("nivel3");

            if (btnInteractRect != null) btnInteractRect.gameObject.SetActive(controlesVisibles);
            if (btnJumpRect != null) btnJumpRect.gameObject.SetActive(controlesVisibles);
            if (btnRunRect != null) btnRunRect.gameObject.SetActive(controlesVisibles);
            if (btnStealthRect != null) btnStealthRect.gameObject.SetActive(controlesVisibles && esN2);
            if (btnMapRect != null) btnMapRect.gameObject.SetActive(controlesVisibles && esN2);
            if (btnCCTVRect != null) btnCCTVRect.gameObject.SetActive(controlesVisibles && esN3);
            if (!controlesVisibles) OcultarJoystick();
            txt.text = controlesVisibles ? "🎮 TÁCTIL" : "🎮 OCULTO";
        });
    }

    private void Update()
    {
        // Si el jugador corre (Shift o botón correr), desactivar modo sigilo automáticamente
        if (IsRunning && sigiloActivo)
        {
            sigiloActivo = false;
            ActualizarColorBotonStealth();
        }

        // Soporte de clic con ratón / touchpad en laptop para todos los botones del HUD
        if (controlesVisibles)
        {
            Camera cam = (touchCanvas != null && touchCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? touchCanvas.worldCamera : null;

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mPos = Input.mousePosition;
                if (btnInteractRect != null && btnInteractRect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(btnInteractRect, mPos, cam))
                {
                    justPressedInteract = true;
                    IsInteractHeld = true;
                }
                else if (btnJumpRect != null && btnJumpRect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(btnJumpRect, mPos, cam))
                {
                    justPressedJump = true;
                    IsJumpHeld = true;
                }
                else if (btnRunRect != null && btnRunRect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(btnRunRect, mPos, cam))
                {
                    botonCorrerForzado = !botonCorrerForzado;
                    ActualizarVisualBotonCorrer();
                }
                else if (btnStealthRect != null && btnStealthRect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(btnStealthRect, mPos, cam))
                {
                    sigiloActivo = !sigiloActivo;
                    ActualizarColorBotonStealth();
                }
                else if (btnMapRect != null && btnMapRect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(btnMapRect, mPos, cam))
                {
                    if (PlanoSubterraneoNivel2.Instance != null)
                    {
                        PlanoSubterraneoNivel2.Instance.AlternarVisorPlano();
                    }
                }
                else if (btnCCTVRect != null && btnCCTVRect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(btnCCTVRect, mPos, cam))
                {
                    if (CCTVMonitorNivel3.Instance != null)
                    {
                        if (!CCTVMonitorNivel3.Instance.monitorAbierto) CCTVMonitorNivel3.Instance.AbrirMonitor();
                        else CCTVMonitorNivel3.Instance.CerrarMonitor();
                    }
                }
            }

            if (Input.GetMouseButton(0))
            {
                Vector2 mPos = Input.mousePosition;
                if (btnInteractRect != null && btnInteractRect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(btnInteractRect, mPos, cam))
                {
                    IsInteractHeld = true;
                }
                if (btnJumpRect != null && btnJumpRect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(btnJumpRect, mPos, cam))
                {
                    IsJumpHeld = true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                IsInteractHeld = false;
                IsJumpHeld = false;
            }
        }

        if (!controlesVisibles) return;

        // Gestionar el Joystick Flotante (Aparece al tocar y desaparece al soltar)
        ProcesarEntradaJoystickFlotante();
    }

    private void ProcesarEntradaJoystickFlotante()
    {
        // 1. Manejo táctil en Celulares / Tablets (Multi-touch)
        if (Input.touchCount > 0)
        {
            bool dedoEncontrado = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                // Si no hay dedo activo en joystick, buscar si este toque inicia en la mitad izquierda
                if (activeFingerId == -1)
                {
                    if (t.phase == TouchPhase.Began && EsZonaValidaJoystick(t.position))
                    {
                        activeFingerId = t.fingerId;
                        MostrarJoystickEn(t.position);
                        dedoEncontrado = true;
                        break;
                    }
                }
                else if (t.fingerId == activeFingerId)
                {
                    dedoEncontrado = true;
                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        ActualizarArrastreJoystick(t.position);
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        OcultarJoystick();
                    }
                    break;
                }
            }

            if (!dedoEncontrado && activeFingerId != -1)
            {
                OcultarJoystick();
            }
        }
        // 2. Soporte para mouse en PC / simulador en Editor
        else if (Input.GetMouseButtonDown(0))
        {
            if (activeFingerId == -1 && EsZonaValidaJoystick(Input.mousePosition))
            {
                activeFingerId = 999; // ID de mouse
                MostrarJoystickEn(Input.mousePosition);
            }
        }
        else if (activeFingerId == 999)
        {
            if (Input.GetMouseButton(0))
            {
                ActualizarArrastreJoystick(Input.mousePosition);
            }
            else
            {
                OcultarJoystick();
            }
        }
        else
        {
            if (activeFingerId != -1 || Movement != Vector2.zero)
            {
                OcultarJoystick();
            }
        }
    }

    private bool EsZonaValidaJoystick(Vector2 screenPos)
    {
        // Mitad izquierda de la pantalla, debajo de la barra superior
        return (screenPos.x < Screen.width * 0.48f) && (screenPos.y < Screen.height * 0.82f);
    }

    private void MostrarJoystickEn(Vector2 screenPos)
    {
        if (joystickBaseRect == null || joystickKnobRect == null || canvasRect == null) return;

        Vector2 localCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            touchCanvas.worldCamera,
            out localCanvasPos
        );

        joystickOriginCanvasPos = localCanvasPos;
        joystickBaseRect.anchoredPosition = localCanvasPos;
        joystickKnobRect.anchoredPosition = Vector2.zero;

        // Hacerlo visible inmediatamente donde se tocó
        joystickBaseRect.gameObject.SetActive(true);
        Movement = Vector2.zero;
        joystickCorriendo = false;
    }

    private void ActualizarArrastreJoystick(Vector2 screenPos)
    {
        if (joystickBaseRect == null || joystickKnobRect == null || canvasRect == null) return;

        Vector2 currentCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            touchCanvas.worldCamera,
            out currentCanvasPos
        );

        Vector2 offset = currentCanvasPos - joystickOriginCanvasPos;
        Vector2 clampedOffset = Vector2.ClampMagnitude(offset, joystickRadius);
        joystickKnobRect.anchoredPosition = clampedOffset;

        float dist = clampedOffset.magnitude;
        if (dist > 10f)
        {
            // Vector de movimiento normalizado (-1 a +1)
            Movement = clampedOffset / joystickRadius;

            // Caminar es la opción fija predeterminada. Correr es una opción separada (botón o Shift)
            joystickCorriendo = false;
        }
        else
        {
            Movement = Vector2.zero;
            joystickCorriendo = false;
        }
    }

    private void OcultarJoystick()
    {
        activeFingerId = -1;
        Movement = Vector2.zero;
        joystickCorriendo = false;

        if (joystickKnobRect != null) joystickKnobRect.anchoredPosition = Vector2.zero;
        if (joystickBaseRect != null && joystickBaseRect.gameObject.activeSelf)
        {
            joystickBaseRect.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        justPressedInteract = false;
        justPressedJump = false;
        IsVoiceSimulated = false;
    }

    private Sprite CrearSpriteCirculo(int size, int borderThickness, Color fillColor, Color borderColor)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = size * 0.5f;
        float radius = center - 2f;
        float innerRadius = radius - borderThickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                if (dist > radius)
                {
                    float alpha = Mathf.Clamp01(radius + 1.2f - dist);
                    tex.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * alpha));
                }
                else if (dist >= innerRadius)
                {
                    tex.SetPixel(x, y, borderColor);
                }
                else
                {
                    tex.SetPixel(x, y, fillColor);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
