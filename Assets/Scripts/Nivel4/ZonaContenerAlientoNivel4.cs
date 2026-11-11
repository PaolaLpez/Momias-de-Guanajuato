using UnityEngine;

/// <summary>
/// Pasaje angosto entre criptas y mausoleos donde la Momia Encadenada se detiene a olfatear y escuchar.
/// Desafío Definitivo del Micrófono:
/// Si la vigilante está dentro y la Bruja pasa cerca (< 9m), la jugadora debe CONTENER EL ALIENTO.
/// Cualquier susurro, respiración fuerte o ruido que supere el umbral verde desencadena un ataque fulminante.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ZonaContenerAlientoNivel4 : MonoBehaviour
{
    public string nombreZona = "Pasaje Angosto";
    private bool jugadoraDentro = false;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            jugadoraDentro = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            jugadoraDentro = false;
        }
    }

    private void Update()
    {
        if (!jugadoraDentro || Time.timeScale == 0f || Nivel4Manager.Instance == null) return;

        MomiaEncadenadaController momia = MomiaEncadenadaController.Instance;
        if (momia == null || momia.estadoActual == MomiaEncadenadaController.EstadoMomia4.Petrificada) return;

        float distAMomia = Vector2.Distance(transform.position, momia.transform.position);

        // Si la momia está cerca del pasadizo angosto
        if (distAMomia <= 9.0f)
        {
            float ruidoMic = Nivel4Manager.Instance.NivelRuidoActual;
            float umbralVerde = Nivel4Manager.Instance.umbralRuidoMic * 0.75f;

            if (ruidoMic > umbralVerde)
            {
                // Ruido detectado al respirar o hablar en el pasaje angosto
                momia.NotificarRuido(VigilanteNivel4.Instance.transform.position, 22f, 1.5f);
                Nivel4Manager.Instance.MostrarAviso("¡TU ALIENTO ROMPIÓ EL SILENCIO! LA BRUJA SE ABALANZA", 2.0f);
            }
        }
    }

    private void OnGUI()
    {
        if (!jugadoraDentro || Time.timeScale == 0f) return;

        MomiaEncadenadaController momia = MomiaEncadenadaController.Instance;
        if (momia == null || momia.estadoActual == MomiaEncadenadaController.EstadoMomia4.Petrificada) return;

        float dist = Vector2.Distance(transform.position, momia.transform.position);
        if (dist <= 9.0f)
        {
            // Aviso de alta tensión en pantalla
            float bw = 460f;
            float bh = 34f;
            Rect br = new Rect((Screen.width - bw) * 0.5f, 62f, bw, bh);

            GUI.color = new Color(0.12f, 0.02f, 0.02f, 0.94f);
            GUI.DrawTexture(br, Texture2D.whiteTexture);
            GUI.color = (Mathf.Sin(Time.time * 8f) > 0f) ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(1f, 0.85f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(br.x, br.y, br.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(br.x, br.yMax - 2f, br.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle st = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.normal.textColor = new Color(1f, 0.95f, 0.65f);
            GUI.Label(br, "☠ ¡CONTÉN EL ALIENTO! LA BRUJA OLFATEA A CENTÍMETROS (SILENCIO ABSOLUTO) ☠", st);
        }
    }
}
