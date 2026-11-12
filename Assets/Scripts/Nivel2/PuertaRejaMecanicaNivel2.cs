using System.Collections;
using UnityEngine;

/// <summary>
/// Reja metálica pesada que bloquea el paso entre secciones del sótano.
/// Requiere interactuar con un mecanismo de manivela para levantarla.
/// Al abrirse, destruye de inmediato cualquier colisión sólida para asegurar paso 100% libre.
/// NO TOCA NINGÚN ARCHIVO DEL NIVEL 1.
/// </summary>
public class PuertaRejaMecanicaNivel2 : MonoBehaviour
{
    [Header("Estado de la Reja")]
    public bool estaAbierta = false;
    public float distanciaInteraccion = 3.6f;
    public float tiempoApertura = 1.0f;

    [Header("Componentes Visuales")]
    public Transform rejaPuerta;
    public SpriteRenderer manivelaSr;
    public BoxCollider2D colisionBloqueo;
    public AudioSource audioSource;

    private bool jugadorEnRango = false;
    private bool abriendo = false;
    private float progresoApertura = 0f;
    private Transform playerTransform;

    private void Awake()
    {
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Audio 2D estéreo directo
        audioSource.volume = 1.0f;
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
            if (p == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) p = pc.gameObject;
            }
            if (p != null) playerTransform = p.transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!estaAbierta && !abriendo && EsJugador(collision)) jugadorEnRango = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!estaAbierta && !abriendo && EsJugador(collision)) jugadorEnRango = true;
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
                    if (Vector2.Distance(worldTap, transform.position) <= 2.8f) return true;
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldTap = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldTap)) return true;
            if (Vector2.Distance(worldTap, transform.position) <= 2.8f) return true;
        }
        return false;
    }

    private void Update()
    {
        if (estaAbierta)
        {
            jugadorEnRango = false;
            return;
        }

        BuscarJugador();

        // Verificación de proximidad infalible por distancia
        if (playerTransform != null && !abriendo)
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
        }

        bool toqueDirecto = ComprobarToqueDirecto();
        bool enRangoToque = (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= distanciaInteraccion + 2.5f);

        if ((jugadorEnRango || (toqueDirecto && enRangoToque)) && !abriendo)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || TouchControlsManager.IsInteractTriggered() || TouchControlsManager.IsJumpTriggered() || toqueDirecto)
            {
                StartCoroutine(SecuenciaAbrirReja());
            }
        }
    }

    private IEnumerator SecuenciaAbrirReja()
    {
        abriendo = true;
        jugadorEnRango = false;
        float tiempo = 0f;

        // Desactivar y DESTRUIR inmediatamente todas las colisiones sólidas para garantizar paso 100% fluido y libre
        if (colisionBloqueo != null)
        {
            colisionBloqueo.enabled = false;
            Destroy(colisionBloqueo);
        }

        Collider2D[] todosLosColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var col in todosLosColliders)
        {
            if (!col.isTrigger)
            {
                col.enabled = false;
                Destroy(col);
            }
        }

        // Sonido y alerta de chirrido de metal oxidado
        if (audioSource != null)
        {
            AudioClip clipReja = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_REJAS) ?? GenerarAudioChirridoReja();
            audioSource.PlayOneShot(clipReja);
        }
        TrampaAcusticaNivel2.EmitirRuido(transform.position, 18f, "¡CHIRRIDO METÁLICO DE REJA OXIDADA!");

        Vector3 posInicial = rejaPuerta != null ? rejaPuerta.localPosition : Vector3.zero;
        Vector3 posFinal = posInicial + new Vector3(0, 3.5f, 0); // Se eleva ampliamente hacia arriba

        while (tiempo < tiempoApertura)
        {
            tiempo += Time.deltaTime;
            progresoApertura = tiempo / tiempoApertura;

            if (rejaPuerta != null)
            {
                rejaPuerta.localPosition = Vector3.Lerp(posInicial, posFinal, progresoApertura);
            }
            if (manivelaSr != null)
            {
                manivelaSr.transform.Rotate(0, 0, 360f * Time.deltaTime);
            }
            yield return null;
        }

        estaAbierta = true;
        abriendo = false;
        Debug.Log("<color=cyan>[Reja Mecánica Nivel 2] Reja levantada y paso completamente desbloqueado.</color>");
    }

    private AudioClip GenerarAudioChirridoReja()
    {
        int rate = 44100;
        float dur = tiempoApertura + 0.2f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float screechFreq = 540f + Mathf.Sin(t * 22f) * 180f + Mathf.Sin(t * 70f) * 90f;
            float screech = Mathf.Sin(2f * Mathf.PI * screechFreq * t) * 0.45f;
            float friccion = (Random.value * 2f - 1f) * 0.35f;

            float clacks = 0f;
            float clackInterval = 0.18f;
            float modT = t % clackInterval;
            if (modT < 0.03f)
            {
                clacks = Mathf.Sin(2f * Mathf.PI * 180f * modT) * Mathf.Exp(-modT * 90f) * 0.6f;
            }

            samples[i] = Mathf.Clamp((screech + friccion + clacks) * Mathf.Exp(-t * 0.5f), -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("AudioRejaOxidada", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnGUI()
    {
        if (jugadorEnRango && !estaAbierta && !abriendo)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0, 1.2f, 0));
            if (screenPos.z > 0)
            {
                float s = ResponsiveGUIHelper.BeginScaledGUI(out float vw, out float vh, out Matrix4x4 prevMatrix);
                try
                {
                    Vector2 guiPos = ResponsiveGUIHelper.WorldScreenToVirtualGUI(screenPos, s);
                    float w = 240f;
                    float h = 26f;
                    float x = Mathf.Clamp(guiPos.x - w / 2f, 8f, vw - w - 8f);
                    float y = Mathf.Clamp(guiPos.y - h / 2f, 8f, vh - h - 8f);
                    Rect rect = new Rect(x, y, w, h);

                    GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
                    GUI.Box(rect, "");
                    GUI.color = Color.white;

                    GUIStyle style = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        richText = true
                    };
                    style.normal.textColor = Color.white;

                    GUI.Label(rect, "<color=#FFDD00>[E] Levantar Reja Oxidada</color>", style);
                }
                finally
                {
                    ResponsiveGUIHelper.EndScaledGUI(prevMatrix);
                }
            }
        }
    }
}
