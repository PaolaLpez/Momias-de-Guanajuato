using UnityEngine;

/// <summary>
/// Puerta Monumental del Santuario de la Capilla del Descanso:
/// - Permanece sellada hasta que el jugador recolecta las 3 Reliquias Sagradas
///   (Crucifijo de Plomo, Eslabón Bendito de Hierro y Libro Parroquial de 1865).
/// - Al reunir las 3 reliquias, el sello se rompe, las puertas se abren y se permite
///   el acceso libre al interior de la Capilla para consagrar el Altar Mayor.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PuertaCapillaNivel4 : MonoBehaviour
{
    [Header("Estado de la Puerta")]
    public bool Abierta = false;
    private BoxCollider2D colSolido;
    private BoxCollider2D colTrigger;
    private bool jugadorCerca = false;

    private void Awake()
    {
        // El primer collider es el bloqueador físico
        colSolido = GetComponent<BoxCollider2D>();
        colSolido.isTrigger = false;

        // Crear un trigger hijo para la detección e interacción con la vigilante
        GameObject trigObj = new GameObject("TriggerPuertaCapilla");
        trigObj.transform.SetParent(transform, false);
        colTrigger = trigObj.AddComponent<BoxCollider2D>();
        colTrigger.size = new Vector2(colSolido.size.x + 2.4f, colSolido.size.y + 2.8f);
        colTrigger.isTrigger = true;

        PuertaCapillaTriggerHelper helper = trigObj.AddComponent<PuertaCapillaTriggerHelper>();
        helper.padre = this;
    }

    private void Update()
    {
        if (Nivel4Manager.Instance == null) return;

        // Si ya tenemos las 3 reliquias y la puerta sigue cerrada, la abrimos automáticamente
        if (!Abierta && Nivel4Manager.Instance.ReliquiasRecolectadas >= 3)
        {
            AbrirPuertas();
        }

        if (jugadorCerca && Time.timeScale > 0f)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                if (Abierta)
                {
                    Nivel4Manager.Instance.EntrarACapillaInterior();
                }
                else
                {
                    int recolectadas = Nivel4Manager.Instance.ReliquiasRecolectadas;
                    Nivel4Manager.Instance.MostrarAviso(
                        $"Las puertas de la Capilla están selladas por el pacto de 1865. Necesitas las 3 Reliquias ({recolectadas}/3).",
                        4.0f
                    );
                }
            }
        }
    }

    public void AbrirPuertas()
    {
        Abierta = true;
        if (colSolido != null) colSolido.enabled = false;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false; // ¡Desaparece el portón cerrado para despejar el vano de entrada!

        AudioClip clipAbrir = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_PUERTA);
        if (clipAbrir != null) AudioSource.PlayClipAtPoint(clipAbrir, transform.position, 1.0f);

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.MostrarAviso("¡EL PACTO SE HA ROTO! Las puertas de la Capilla se abren. ¡Entra y toca la Campana del Alba!", 5.0f);
            Nivel4Manager.Instance.SacudirPantalla(0.85f, 22f);
        }
    }

    public void SetJugadorCerca(bool cerca)
    {
        jugadorCerca = cerca;
    }

    private void OnGUI()
    {
        if (!jugadorCerca || Time.timeScale == 0f || (Nivel4Manager.Instance != null && Nivel4Manager.Instance.enInteriorCapilla))
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0f, 1.2f, 0f));
        if (screenPos.z <= 0) return;

        if (Abierta)
        {
            float w = 340f;
            float h = 32f;
            Rect rect = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h, w, h);

            GUI.color = new Color(0.10f, 0.08f, 0.05f, 0.95f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.85f, 0.35f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle st = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            st.normal.textColor = new Color(1f, 0.92f, 0.50f);
            GUI.Label(rect, "★ [E] Entrar a la Capilla Mayor ★", st);
        }
        else
        {
            float w = 310f;
            float h = 28f;
            Rect rect = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h, w, h);

            GUI.color = new Color(0.12f, 0.08f, 0.08f, 0.92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int rec = Nivel4Manager.Instance != null ? Nivel4Manager.Instance.ReliquiasRecolectadas : 0;
            GUIStyle st = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            st.normal.textColor = new Color(1f, 0.7f, 0.7f);
            GUI.Label(rect, $"🔒 Puerta de la Capilla (Sellada: {rec}/3 Reliquias)", st);
        }
    }
}

public class PuertaCapillaTriggerHelper : MonoBehaviour
{
    public PuertaCapillaNivel4 padre;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (padre != null && (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null))
        {
            padre.SetJugadorCerca(true);
            if (padre.Abierta && Nivel4Manager.Instance != null && !Nivel4Manager.Instance.enInteriorCapilla)
            {
                Nivel4Manager.Instance.EntrarACapillaInterior();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (padre != null && (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null))
        {
            padre.SetJugadorCerca(true);
            if (padre.Abierta && Nivel4Manager.Instance != null && !Nivel4Manager.Instance.enInteriorCapilla)
            {
                Nivel4Manager.Instance.EntrarACapillaInterior();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (padre != null && (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null))
        {
            padre.SetJugadorCerca(false);
        }
    }
}
