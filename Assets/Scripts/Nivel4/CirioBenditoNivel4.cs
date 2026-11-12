using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cirio Bendito del Camposanto (Zona Segura Temporal):
/// - En 3 pequeños altares de cantera repartidos por el panteón hay pebeteros con cirios bendecidos.
/// - El jugador interactúa con su encendedor [E] para prender el cirio.
/// - Mientras la llama esté viva (15 segundos), emite una luz dorada brillante que las momias no se atreven a cruzar.
/// - Sirve como barricada temporal de luz sagrada que ahuyenta a las momias mientras planeas tu ruta o te ocultas.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class CirioBenditoNivel4 : MonoBehaviour
{
    [Header("Configuración del Cirio")]
    public string nombreAltar = "Cirio Bendito de Santa Paula";
    public float duracionFuego = 15f;
    public float radioProteccion = 5.8f;
    public float cooldownReencendido = 6f;
    public float distanciaProximoTerceraReliquia = 28f;

    [Header("Estado")]
    public bool EstaEncendido = false;
    public float timerFuego = 0f;
    private float timerCooldown = 0f;
    private bool jugadorCerca = false;
    private bool consumidoTrasUso = false;
    private bool requiereMoverseDelSitio = false;
    private bool faseFinalDesbloqueada = false;
    private bool yaUsadoEnFaseFinal = false;
    private float timerCheckFaseFinal = 0f;

    [Header("Componentes Visuales")]
    private Light2D luzDorada;
    private SpriteRenderer srLlama;
    private CircleCollider2D colZonaSegura;
    private AudioSource audioSource;

    private void Awake()
    {
        colZonaSegura = GetComponent<CircleCollider2D>();
        colZonaSegura.isTrigger = true;
        colZonaSegura.radius = radioProteccion;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        ConfigurarLuzYLlama();
    }

    private void ConfigurarLuzYLlama()
    {
        // Luz dorada brillante bendita
        Transform tLuz = transform.Find("LuzCirioBendito");
        if (tLuz == null)
        {
            GameObject goLuz = new GameObject("LuzCirioBendito");
            goLuz.transform.SetParent(transform, false);
            goLuz.transform.localPosition = new Vector3(0f, 0.45f, 0f);

            luzDorada = goLuz.AddComponent<Light2D>();
            luzDorada.lightType = Light2D.LightType.Point;
            luzDorada.color = new Color(1.0f, 0.82f, 0.35f, 1f); // Dorado sagrado
            luzDorada.intensity = 0f;
            luzDorada.pointLightInnerRadius = 0.8f;
            luzDorada.pointLightOuterRadius = radioProteccion + 1.2f;
        }
        else
        {
            luzDorada = tLuz.GetComponent<Light2D>();
        }

        // Llama visual del pebetero
        Transform tLlama = transform.Find("LlamaVisual");
        if (tLlama != null)
        {
            srLlama = tLlama.GetComponent<SpriteRenderer>();
            if (srLlama != null) srLlama.enabled = false;
        }
    }

    private void Update()
    {
        if (timerCooldown > 0f) timerCooldown -= Time.deltaTime;

        if (VigilanteNivel4.Instance != null)
        {
            float distJugador = Vector2.Distance(transform.position, VigilanteNivel4.Instance.transform.position);
            jugadorCerca = distJugador <= radioProteccion;
            if (!EstaEncendido && requiereMoverseDelSitio && distJugador > radioProteccion + 0.5f)
            {
                requiereMoverseDelSitio = false;
            }
        }

        if (EstaEncendido)
        {
            timerFuego -= Time.deltaTime;

            // Parpadeo sutil de la llama sagrada
            if (luzDorada != null)
            {
                luzDorada.intensity = 1.75f + 0.35f * Mathf.Sin(Time.time * 8f) + 0.15f * Mathf.PerlinNoise(Time.time * 12f, 0f);
            }

            // Ahuyentar a todas las momias activas que intenten cruzar la barrera
            RepelerMomias(false);

            if (timerFuego <= 0f)
            {
                Apagar();
            }
        }
        else
        {
            if (luzDorada != null && luzDorada.intensity > 0f)
            {
                luzDorada.intensity = Mathf.MoveTowards(luzDorada.intensity, 0f, Time.deltaTime * 3f);
            }

            // Verificar desbloqueo tardío tras recoger 2 reliquias y acercarse a la 3.ª
            if (consumidoTrasUso && !faseFinalDesbloqueada)
            {
                timerCheckFaseFinal -= Time.deltaTime;
                if (timerCheckFaseFinal <= 0f)
                {
                    timerCheckFaseFinal = 0.35f;
                    ActualizarEstadoDesbloqueoTerceraReliquia();
                }
            }

            if (jugadorCerca && Time.timeScale > 0f && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
            {
                if (PuedeEncender())
                {
                    Encender();
                }
                else if (Nivel4Manager.Instance != null)
                {
                    Nivel4Manager.Instance.MostrarAviso(ObtenerMensajeBloqueo(), 2.8f);
                }
            }
        }
    }

    private bool PuedeEncender()
    {
        if (EstaEncendido || timerCooldown > 0f) return false;
        if (yaUsadoEnFaseFinal) return false;
        if (requiereMoverseDelSitio) return false;
        if (consumidoTrasUso && !faseFinalDesbloqueada) return false;
        return true;
    }

    private string ObtenerMensajeBloqueo()
    {
        if (yaUsadoEnFaseFinal)
        {
            return "Este Cirio Bendito ya entregó toda su luz sagrada.";
        }
        if (requiereMoverseDelSitio)
        {
            return "El cirio se apagó: ¡muévete de este sitio antes de intentar reavivarlo!";
        }
        if (consumidoTrasUso && !faseFinalDesbloqueada)
        {
            return "Cirio consumido. Se reactivará solo tras reunir 2 reliquias y acercarte a la 3.ª.";
        }
        return $"Espera {timerCooldown:0.0}s para volver a encender este Cirio.";
    }

    private void ActualizarEstadoDesbloqueoTerceraReliquia()
    {
        if (requiereMoverseDelSitio || Nivel4Manager.Instance == null || VigilanteNivel4.Instance == null)
            return;

        if (Nivel4Manager.Instance.ReliquiasRecolectadas >= 3)
        {
            DesbloquearFaseFinal();
            return;
        }

        if (Nivel4Manager.Instance.ReliquiasRecolectadas == 2)
        {
            ReliquiaItemNivel4[] reliquias = Object.FindObjectsByType<ReliquiaItemNivel4>(FindObjectsSortMode.None);
            Vector2 posJug = VigilanteNivel4.Instance.transform.position;
            for (int i = 0; i < reliquias.Length; i++)
            {
                ReliquiaItemNivel4 r = reliquias[i];
                if (r != null && r.gameObject.activeInHierarchy && (int)r.tipo <= 3)
                {
                    if (Vector2.Distance(posJug, r.transform.position) <= distanciaProximoTerceraReliquia)
                    {
                        DesbloquearFaseFinal();
                        break;
                    }
                }
            }
        }
    }

    private void DesbloquearFaseFinal()
    {
        faseFinalDesbloqueada = true;
        consumidoTrasUso = false;
        timerCooldown = 0f;

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.MostrarAviso("✨ La proximidad de la 3.ª reliquia reaviva los Cirios Benditos.", 3.5f);
        }
    }

    private void RepelerMomias(bool empujeInicial)
    {
        float radioDeteccion = empujeInicial ? (radioProteccion + 3.0f) : radioProteccion;

        // 1. Momia Encadenada (La Bruja)
        MomiaEncadenadaController bruja = Object.FindFirstObjectByType<MomiaEncadenadaController>();
        if (bruja != null && bruja.gameObject.activeInHierarchy && !bruja.momiaDetenida)
        {
            float dist = Vector2.Distance(bruja.transform.position, transform.position);
            if (dist < radioDeteccion && (empujeInicial || !bruja.EstaRepelidaPorFuego))
            {
                bruja.RepelerPorFuegoBendito(transform.position, radioProteccion);
            }
        }

        // 2. Momia del Nivel 1 por sección (Sector Oeste)
        MomiaNivel1Seccion[] momiasN1 = Object.FindObjectsByType<MomiaNivel1Seccion>(FindObjectsSortMode.None);
        foreach (var m1 in momiasN1)
        {
            if (m1 != null && m1.gameObject.activeInHierarchy && m1.HaDespertado && !m1.Petrificada)
            {
                float dist = Vector2.Distance(m1.transform.position, transform.position);
                if (dist < radioDeteccion)
                {
                    m1.RepelerPorFuegoBendito(transform.position, radioProteccion);
                }
            }
        }

        // 3. Momia del Nivel 2 por sección (Sector Este)
        MomiaNivel2Seccion[] momiasN2 = Object.FindObjectsByType<MomiaNivel2Seccion>(FindObjectsSortMode.None);
        foreach (var m2 in momiasN2)
        {
            if (m2 != null && m2.gameObject.activeInHierarchy && m2.HaDespertado && !m2.Petrificada)
            {
                float dist = Vector2.Distance(m2.transform.position, transform.position);
                if (dist < radioDeteccion)
                {
                    m2.RepelerPorFuegoBendito(transform.position, radioProteccion);
                }
            }
        }

        // 3. Momias Invasoras de las Catacumbas (si existieran)
        MomiaInvasoraNivel4[] invasoras = Object.FindObjectsByType<MomiaInvasoraNivel4>(FindObjectsSortMode.None);
        foreach (var m in invasoras)
        {
            if (m != null && m.gameObject.activeInHierarchy && !m.Petrificada)
            {
                float dist = Vector2.Distance(m.transform.position, transform.position);
                if (dist < radioDeteccion)
                {
                    m.RepelerPorFuegoBendito(transform.position, radioProteccion);
                }
            }
        }
    }

    public void Encender()
    {
        if (!PuedeEncender()) return;

        EstaEncendido = true;
        timerFuego = duracionFuego;
        if (srLlama != null) srLlama.enabled = true;

        // Alejar inmediatamente a Carmen si está cerca al momento de prender el Cirio
        RepelerMomias(true);

        if (audioSource != null)
        {
            AudioClip clip = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_INTERACCION);
            if (clip != null) audioSource.PlayOneShot(clip, 1.0f);
        }

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.MostrarAviso("¡ENCENDISTE EL CIRIO BENDITO! Zona segura activa durante 15 segundos.", 3.5f);
        }
    }

    public void Apagar()
    {
        EstaEncendido = false;
        timerFuego = 0f;
        timerCooldown = cooldownReencendido;
        requiereMoverseDelSitio = true;

        if (!faseFinalDesbloqueada)
        {
            consumidoTrasUso = true;
        }
        else
        {
            yaUsadoEnFaseFinal = true;
        }

        if (srLlama != null) srLlama.enabled = false;

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.MostrarAviso("El Cirio Bendito se extinguió. ¡Muévete del sitio! No podrás reactivarlo hasta acercarte a la 3.ª reliquia.", 4.0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            jugadorCerca = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            jugadorCerca = false;
            if (!EstaEncendido)
            {
                requiereMoverseDelSitio = false;
            }
        }
    }

    private void OnGUI()
    {
        if (jugadorCerca && PuedeEncender() && Time.timeScale > 0f)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0f, 1.4f, 0f));
            if (screenPos.z > 0)
            {
                float w = 240f;
                float h = 28f;
                Rect rect = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h, w, h);

                GUI.color = new Color(0.1f, 0.08f, 0.05f, 0.90f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.85f, 0.35f, 1f);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUIStyle st = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                st.normal.textColor = new Color(1f, 0.92f, 0.50f);
                GUI.Label(rect, "🔥 [E] Encender Cirio Bendito (15s)", st);
            }
        }
        else if (jugadorCerca && !EstaEncendido && Time.timeScale > 0f)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0f, 1.4f, 0f));
            if (screenPos.z > 0)
            {
                string msg = yaUsadoEnFaseFinal
                    ? "🕯️ Cirio consumido por completo"
                    : (requiereMoverseDelSitio
                        ? "🕯️ Cirio apagado — Muévete del sitio"
                        : "🕯️ Apagado (Reactivable con 2 reliquias, cerca de la 3.ª)");

                float w = Mathf.Max(260f, msg.Length * 7.2f);
                float h = 26f;
                Rect rect = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h, w, h);

                GUI.color = new Color(0.14f, 0.08f, 0.06f, 0.90f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = new Color(0.75f, 0.45f, 0.25f, 1f);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUIStyle st = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                };
                st.normal.textColor = new Color(0.95f, 0.72f, 0.52f);
                GUI.Label(rect, msg, st);
            }
        }
        else if (EstaEncendido)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0f, 1.4f, 0f));
            if (screenPos.z > 0)
            {
                float w = 180f;
                float h = 24f;
                Rect rect = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h, w, h);

                GUI.color = new Color(0.08f, 0.12f, 0.06f, 0.90f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = new Color(0.4f, 1f, 0.4f, 1f);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width * (timerFuego / duracionFuego), 2f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUIStyle st = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                };
                st.normal.textColor = new Color(0.9f, 1f, 0.6f);
                GUI.Label(rect, $"BARRERA ACTIVA: {timerFuego:0.0}s", st);
            }
        }
    }
}
