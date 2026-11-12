using System.Collections;
using UnityEngine;

/// <summary>
/// Válvula de tubería industrial interactiva en el Nivel 2.
/// Al abrirse con [E], libera una intensa cortina de vapor ruidoso durante un tiempo limitado.
/// Este siseo constante desorienta y enmascara el sonido de los pasos del jugador en esa sala.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ValvulaVaporNivel2 : MonoBehaviour
{
    [Header("Configuración de la Válvula")]
    public float duracionVapor = 10f;
    public float tiempoRecarga = 15f;
    public bool estaActiva { get; private set; } = false;

    [Header("Componentes Visuales")]
    public SpriteRenderer ruedaValvula;
    public GameObject nubeVaporVisual;
    private AudioSource audioSource;
    private bool jugadorEnRango = false;
    private float tiempoRestante = 0f;
    private float ultimoUso = -99f;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.9f;
            audioSource.loop = true;
        }

        if (nubeVaporVisual != null) nubeVaporVisual.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) jugadorEnRango = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) jugadorEnRango = false;
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
        if (estaActiva)
        {
            tiempoRestante -= Time.deltaTime;
            if (ruedaValvula != null)
            {
                ruedaValvula.transform.Rotate(0, 0, 90f * Time.deltaTime);
            }

            if (tiempoRestante <= 0f)
            {
                CerrarValvula();
            }
        }
        else
        {
            bool toque = ComprobarToqueDirecto();
            if ((jugadorEnRango || toque) && PuedeActivarse() && (Input.GetKeyDown(KeyCode.E) || TouchControlsManager.IsInteractTriggered() || toque))
            {
                AbrirValvula();
            }
        }
    }

    private bool PuedeActivarse()
    {
        return !estaActiva && (Time.time - ultimoUso >= tiempoRecarga);
    }

    public void AbrirValvula()
    {
        estaActiva = true;
        tiempoRestante = duracionVapor;
        ultimoUso = Time.time;

        if (nubeVaporVisual != null) nubeVaporVisual.SetActive(true);

        if (audioSource != null)
        {
            if (audioSource.clip == null) audioSource.clip = GenerarClipVapor();
            audioSource.volume = 0.75f;
            audioSource.Play();
        }

        Debug.Log("<color=cyan>[Válvula Nivel 2] Válvula de vapor abierta: El sonido ambiental ocultará los pasos.</color>");
    }

    public void CerrarValvula()
    {
        estaActiva = false;
        if (nubeVaporVisual != null) nubeVaporVisual.SetActive(false);

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private AudioClip GenerarClipVapor()
    {
        int rate = 22050;
        float dur = 2.0f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            // Siseo continuo de alta frecuencia
            float t = (float)i / rate;
            float noise = (Random.value * 2f - 1f) * 0.45f;
            float hiss = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.15f;
            samples[i] = (noise + hiss);
        }

        AudioClip clip = AudioClip.Create("AudioVaporLoop", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnGUI()
    {
        if (jugadorEnRango)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0, 1.25f, 0));
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

                    string texto;
                    if (estaActiva)
                    {
                        texto = $"<color=#00FFFF>Vapor Activo ({Mathf.CeilToInt(tiempoRestante)}s)</color>";
                    }
                    else if (PuedeActivarse())
                    {
                        texto = "<color=#FFFF00>[E] Abrir Válvula de Vapor</color>";
                    }
                    else
                    {
                        int cd = Mathf.CeilToInt(tiempoRecarga - (Time.time - ultimoUso));
                        texto = $"<color=#888888>Presión Recargando ({cd}s)</color>";
                    }

                    GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);
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
