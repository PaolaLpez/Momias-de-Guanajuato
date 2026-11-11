using UnityEngine;

/// <summary>
/// Escondite interactivo para el Nivel 4 (Panteón de Santa Paula).
/// Permite a la vigilante ocultarse dentro de criptas abiertas, sarcófagos de piedra o mausoleos.
/// Incluye cooldown y desactivación física para evitar colisiones y bucles de salida inmediata.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EsconditeNivel4 : MonoBehaviour
{
    public string nombreEscondite = "Cripta de Cantera";
    public bool ocupado = false;

    private bool jugadorEnTrigger = false;
    private bool jugadorCerca = false;
    private SpriteRenderer jugadorSr;
    private Vector3 posicionJugadorPrevia;
    private float timerCooldown = 0f;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (col.size == Vector2.one) col.size = new Vector2(3.6f, 3.2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            jugadorEnTrigger = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            jugadorEnTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            jugadorEnTrigger = false;
        }
    }

    private void Update()
    {
        if (timerCooldown > 0f)
        {
            timerCooldown -= Time.deltaTime;
        }

        // Detección fiable combinando trigger y proximidad radial amplia (3.8m para sarcófagos anchos)
        if (VigilanteNivel4.Instance != null && !ocupado)
        {
            float dist = Vector2.Distance(transform.position, VigilanteNivel4.Instance.transform.position);
            jugadorCerca = jugadorEnTrigger || (dist <= 3.8f);
        }
        else if (ocupado)
        {
            jugadorCerca = true;
        }
        else
        {
            jugadorCerca = false;
        }

        if (!jugadorCerca && !ocupado) return;

        if (timerCooldown <= 0f && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (!ocupado)
            {
                EntrarEscondite();
            }
            else
            {
                SalirEscondite();
            }
        }
    }

    public void EntrarEscondite()
    {
        if (VigilanteNivel4.Instance == null) return;

        VigilanteNivel4 vig = VigilanteNivel4.Instance;
        ocupado = true;
        timerCooldown = 0.45f;
        posicionJugadorPrevia = vig.transform.position;

        vig.EstaOculta = true;
        vig.EsconditeActual = this;
        vig.transform.position = transform.position;

        // Desactivar temporalmente colisionador y simulación física para no chocar con el sarcófago sólido
        BoxCollider2D bCol = vig.GetComponent<BoxCollider2D>();
        if (bCol != null) bCol.enabled = false;

        Rigidbody2D rb = vig.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Ocultar a la vigilante y su linterna
        jugadorSr = vig.GetComponent<SpriteRenderer>();
        if (jugadorSr != null) jugadorSr.enabled = false;

        if (vig.PivotLinterna != null)
        {
            vig.PivotLinterna.gameObject.SetActive(false);
        }

        if (MomiaEncadenadaController.Instance != null)
        {
            MomiaEncadenadaController.Instance.NotificarJugadorOculto(transform.position);
        }

        MomiaNivel1Seccion[] momiasN1 = Object.FindObjectsByType<MomiaNivel1Seccion>(FindObjectsSortMode.None);
        foreach (var m1 in momiasN1)
        {
            if (m1 != null) m1.NotificarJugadorOculto(transform.position);
        }

        MomiaNivel2Seccion[] momiasN2 = Object.FindObjectsByType<MomiaNivel2Seccion>(FindObjectsSortMode.None);
        foreach (var m2 in momiasN2)
        {
            if (m2 != null) m2.NotificarJugadorOculto(transform.position);
        }

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.MostrarAviso("Te has ocultado en " + nombreEscondite + ". Guarda silencio...", 2.8f);
        }
    }

    public void SalirEscondite()
    {
        if (VigilanteNivel4.Instance == null) return;

        VigilanteNivel4 vig = VigilanteNivel4.Instance;
        ocupado = false;
        timerCooldown = 0.45f;

        // Reubicar fuera del sarcófago (al frente o en su posición previa)
        Vector3 posSalida = transform.position + Vector3.down * 1.6f;
        vig.transform.position = posSalida;

        // Reactivar física y colisiones
        BoxCollider2D bCol = vig.GetComponent<BoxCollider2D>();
        if (bCol != null) bCol.enabled = true;

        Rigidbody2D rb = vig.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        vig.EstaOculta = false;
        vig.EsconditeActual = null;

        if (jugadorSr != null) jugadorSr.enabled = true;

        if (vig.PivotLinterna != null)
        {
            vig.PivotLinterna.gameObject.SetActive(true);
        }
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f) return;

        if (ocupado)
        {
            float w = 380f;
            float h = 44f;
            Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height - 95f, w, h);
            GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.94f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.78f, 0.35f, 1f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle st = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.normal.textColor = new Color(0.95f, 0.85f, 0.45f);
            GUI.Label(r, "OCULTA EN: " + nombreEscondite.ToUpper() + "\nPresiona [E] para salir | Guarda silencio", st);
            return;
        }

        if (jugadorCerca && VigilanteNivel4.Instance != null && !VigilanteNivel4.Instance.EstaOculta)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.3f);
            if (screenPos.z <= 0) return;

            float bw = 250f;
            float bh = 28f;
            Rect br = new Rect(screenPos.x - bw * 0.5f, Screen.height - screenPos.y - bh, bw, bh);

            GUI.color = new Color(0.10f, 0.09f, 0.08f, 0.90f);
            GUI.DrawTexture(br, Texture2D.whiteTexture);
            GUI.color = new Color(0.90f, 0.75f, 0.35f, 1f);
            GUI.DrawTexture(new Rect(br.x, br.yMax - 2f, br.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle bSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bSt.normal.textColor = new Color(0.95f, 0.88f, 0.65f);
            GUI.Label(br, "[E] Ocultarse en " + nombreEscondite, bSt);
        }
    }
}
