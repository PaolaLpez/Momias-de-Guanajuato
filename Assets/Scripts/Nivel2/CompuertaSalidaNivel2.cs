using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Compuerta pesada de evacuación del subsuelo en el Nivel 2.
/// Solo puede abrirse cuando el generador principal ha restaurado las 3 fases eléctricas.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CompuertaSalidaNivel2 : MonoBehaviour
{
    [Header("Estado")]
    public bool estaDesbloqueada = false;
    public float distanciaInteraccion = 6.0f;

    [Header("Visuales y Luces")]
    public SpriteRenderer compuertaSr;
    public Sprite compuertaCerradaSprite;
    public Sprite compuertaAbiertaSprite;
    public Light2D luzAcceso;

    private bool jugadorEnRango = false;
    private AudioSource audioSource;
    private Transform playerTransform;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1.0f;

        ActualizarVisuales();
    }

    private void Start()
    {
        BuscarJugador();
        GarantizarLimitesJugador();
    }

    private void BuscarJugador()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) p = GameObject.Find("Vigilante");
            if (p == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) p = pc.gameObject;
            }
            if (p != null) playerTransform = p.transform;
        }
    }

    private void GarantizarLimitesJugador()
    {
        if (playerTransform != null)
        {
            PlayerController pc = playerTransform.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (pc.museoMaxY < 23.5f) pc.museoMaxY = 23.5f;
                if (pc.museoMinY > -20.0f) pc.museoMinY = -20.0f;
                if (pc.museoMinX > -26.5f) pc.museoMinX = -26.5f;
                if (pc.museoMaxX < 26.5f) pc.museoMaxX = 26.5f;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (EsJugador(collision))
        {
            jugadorEnRango = true;
            if (estaDesbloqueada || (GeneradorPrincipalNivel2.Instance != null && GeneradorPrincipalNivel2.Instance.fasesRestauradas >= 3))
            {
                CompletarNivel2();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (EsJugador(collision))
        {
            jugadorEnRango = true;
            if (estaDesbloqueada || (GeneradorPrincipalNivel2.Instance != null && GeneradorPrincipalNivel2.Instance.fasesRestauradas >= 3))
            {
                CompletarNivel2();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (EsJugador(collision)) jugadorEnRango = false;
    }

    private bool EsJugador(Collider2D col)
    {
        return col.CompareTag("Player") || col.GetComponent<PlayerController>() != null || col.gameObject.name.Contains("Vigilante");
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
                    if (Vector2.Distance(worldTap, transform.position) <= 3.5f) return true;
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldTap = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldTap)) return true;
            if (Vector2.Distance(worldTap, transform.position) <= 3.5f) return true;
        }
        return false;
    }

    private void Update()
    {
        BuscarJugador();
        GarantizarLimitesJugador();

        // Autodesbloqueo instantáneo si se completaron las 3 fases
        if (!estaDesbloqueada && GeneradorPrincipalNivel2.Instance != null && GeneradorPrincipalNivel2.Instance.fasesRestauradas >= 3)
        {
            DesbloquearCompuerta();
        }

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= distanciaInteraccion + 1.2f)
            {
                jugadorEnRango = true;
            }
            else if (dist > distanciaInteraccion + 2.0f)
            {
                jugadorEnRango = false;
            }

            // Escape automático al caminar directamente al umbral abierto
            if (estaDesbloqueada && dist <= 3.5f)
            {
                CompletarNivel2();
                return;
            }
        }

        bool toqueDirecto = ComprobarToqueDirecto();
        bool enRangoToque = (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= distanciaInteraccion + 2.8f);

        // Interacción por teclado o pantalla táctil
        if ((jugadorEnRango || (toqueDirecto && enRangoToque)) && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || TouchControlsManager.IsInteractTriggered() || TouchControlsManager.IsJumpTriggered() || toqueDirecto))
        {
            if (estaDesbloqueada || (GeneradorPrincipalNivel2.Instance != null && GeneradorPrincipalNivel2.Instance.fasesRestauradas >= 3))
            {
                CompletarNivel2();
            }
            else
            {
                // Alerta sonora de bloqueo
                if (audioSource != null)
                {
                    audioSource.pitch = 0.7f;
                    audioSource.PlayOneShot(GenerarAudioDesbloqueoHydraulico());
                }
                Debug.Log("<color=red>[Compuerta Nivel 2] Bloqueada. Restaura las 3 fases eléctricas.</color>");
            }
        }
    }

    public void DesbloquearCompuerta()
    {
        estaDesbloqueada = true;
        ActualizarVisuales();

        if (audioSource != null)
        {
            audioSource.PlayOneShot(GenerarAudioDesbloqueoHydraulico());
        }
        Debug.Log("<color=green>[Compuerta Nivel 2] ¡Compuerta electromagnética abierta hacia el Nivel 3!</color>");
    }

    private void ActualizarVisuales()
    {
        if (compuertaSr != null)
        {
            compuertaSr.sprite = estaDesbloqueada ? compuertaAbiertaSprite : compuertaCerradaSprite;
            compuertaSr.color = estaDesbloqueada ? new Color(0.6f, 1f, 0.6f) : new Color(0.9f, 0.4f, 0.4f);
        }

        if (luzAcceso != null)
        {
            luzAcceso.color = estaDesbloqueada ? new Color(0.2f, 1f, 0.4f, 1f) : Color.red;
            luzAcceso.intensity = estaDesbloqueada ? 2.5f : 0.8f;
            luzAcceso.pointLightOuterRadius = estaDesbloqueada ? 14f : 6.0f;
            if (estaDesbloqueada) StartCoroutine(PulsarLuzSalida());
        }
    }

    private IEnumerator PulsarLuzSalida()
    {
        while (estaDesbloqueada)
        {
            float t = (Mathf.Sin(Time.time * 3.5f) + 1f) * 0.5f;
            if (luzAcceso != null)
            {
                luzAcceso.intensity = Mathf.Lerp(1.8f, 2.8f, t);
            }
            yield return null;
        }
    }

    private void CompletarNivel2()
    {
        // Iluminación total brillante al completar el nivel
        if (GeneradorPrincipalNivel2.Instance != null)
        {
            GeneradorPrincipalNivel2.Instance.IluminarSubterraneoCompleto();
        }

        Nivel2Manager manager = Nivel2Manager.Instance != null ? Nivel2Manager.Instance : FindFirstObjectByType<Nivel2Manager>();
        if (manager != null)
        {
            manager.ActivarVictoria();
        }
    }

    private AudioClip GenerarAudioDesbloqueoHydraulico()
    {
        int rate = 22050;
        float dur = 0.8f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float lowClank = Mathf.Sin(2f * Mathf.PI * 75f * t) * Mathf.Exp(-t * 6f);
            float release = (Random.value * 2f - 1f) * (t < 0.4f ? 0.35f : 0.05f);
            samples[i] = Mathf.Clamp(lowClank + release, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("AudioHydraulicGate", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnGUI()
    {
        if (jugadorEnRango)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0, -1.8f, 0));
            if (screenPos.z > 0)
            {
                float s = ResponsiveGUIHelper.BeginScaledGUI(out float vw, out float vh, out Matrix4x4 prevMatrix);
                try
                {
                    Vector2 guiPos = ResponsiveGUIHelper.WorldScreenToVirtualGUI(screenPos, s);
                    float w = Mathf.Min(380f, vw - 20f);
                    float h = 34f;
                    float x = Mathf.Clamp(guiPos.x - w / 2f, 8f, vw - w - 8f);
                    float y = Mathf.Clamp(guiPos.y - h / 2f, 8f, vh - h - 8f);
                    Rect rect = new Rect(x, y, w, h);

                    string texto = estaDesbloqueada 
                        ? "<color=#66FF66><b>[E] CRUZAR COMPUERTA Y ESCAPAR</b></color>" 
                        : "<color=#FF5555><b>[COMPUERTA BLOQUEADA - RESTAURA LAS 3 FASES]</b></color>";

                    GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
                    GUI.Box(rect, "");
                    GUI.color = Color.white;

                    GUIStyle style = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        richText = true
                    };
                    style.normal.textColor = Color.white;

                    GUI.Label(rect, texto, style);
                }
                finally
                {
                    ResponsiveGUIHelper.EndScaledGUI(prevMatrix);
                }
            }
        }
    }
}
