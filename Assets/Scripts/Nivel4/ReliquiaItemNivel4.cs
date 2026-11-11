using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Objeto interactivo de misión para el Nivel 4:
/// - Reliquias sagradas (Crucifijo de Plomo, Incensario de Copal, Cuerda del Campanario).
/// - Altar Mayor de la Capilla (para consagrar las reliquias).
/// - Campana Mayor del Alba (para desatar el amanecer sobre Guanajuato).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ReliquiaItemNivel4 : MonoBehaviour
{
    public enum TipoInteractivo
    {
        CrucifijoPlomo = 1,
        EslabonBendito = 2,
        LibroParroquial = 3,
        AltarMayor = 10,
        CampanarioAlba = 20,
        CartelLore = 30
    }

    public TipoInteractivo tipo = TipoInteractivo.CrucifijoPlomo;
    public string nombreMostrado = "Reliquia Sagrada";

    private bool jugadorCerca = false;
    private Light2D luzPunto;
    private Vector3 posBaseMundo;
    private bool posInicialRegistrada = false;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.size = new Vector2(3.6f, 3.6f);
        }

        // Luz mística pulsante dorada para destacar los objetos sagrados en la penumbra
        Transform tLuz = transform.Find("LuzSacra");
        if (tLuz == null && (int)tipo < 10)
        {
            GameObject objLuz = new GameObject("LuzSacra");
            objLuz.transform.SetParent(transform, false);
            objLuz.transform.localPosition = Vector3.zero;

            luzPunto = objLuz.AddComponent<Light2D>();
            luzPunto.lightType = Light2D.LightType.Point;
            luzPunto.color = new Color(1.0f, 0.90f, 0.45f, 1f); // Dorado sagrado brillante
            luzPunto.intensity = 1.45f;
            luzPunto.pointLightInnerRadius = 0.4f;
            luzPunto.pointLightOuterRadius = 4.8f;
        }
    }

    private void Start()
    {
        posBaseMundo = transform.position;
        posInicialRegistrada = true;
    }

    private void Update()
    {
        // 1. Levitation y brillo místico para hacer las reliquias claramente visibles
        if ((int)tipo <= 3)
        {
            if (!posInicialRegistrada)
            {
                posBaseMundo = transform.position;
                posInicialRegistrada = true;
            }
            transform.position = posBaseMundo + Vector3.up * (Mathf.Sin(Time.time * 2.8f) * 0.08f);

            if (luzPunto != null)
            {
                luzPunto.intensity = 1.35f + 0.45f * Mathf.Sin(Time.time * 3.2f);
            }
        }

        // 2. Detección directa por distancia (100% infalible ante colisionadores de pedestales y altares)
        if (VigilanteNivel4.Instance != null)
        {
            float radioMax = (tipo == TipoInteractivo.AltarMayor || tipo == TipoInteractivo.CampanarioAlba) ? 4.5f : 3.2f;
            float dist = Vector2.Distance(transform.position, VigilanteNivel4.Instance.transform.position);
            jugadorCerca = dist <= radioMax;
        }

        if (!jugadorCerca || Time.timeScale == 0f) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            Interactuar();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            jugadorCerca = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
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
        }
    }

    private void Interactuar()
    {
        if (Nivel4Manager.Instance == null) return;

        if ((int)tipo <= 3)
        {
            // Recoger reliquia
            Nivel4Manager.Instance.RecolectarReliquia((int)tipo, nombreMostrado);
            AudioClip clip = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_RELIQUIA);
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, 1.0f);
            Destroy(gameObject);
        }
        else if (tipo == TipoInteractivo.AltarMayor)
        {
            // Consagrar en el altar
            Nivel4Manager.Instance.InteractuarConAltar();
        }
        else if (tipo == TipoInteractivo.CampanarioAlba)
        {
            // Tocar la campana mayor
            Nivel4Manager.Instance.TocarCampanaMayor();
        }
        else if (tipo == TipoInteractivo.CartelLore)
        {
            // Abrir o cerrar el modal de la crónica y cartel
            Nivel4Manager.Instance.ModalLoreAbierto = !Nivel4Manager.Instance.ModalLoreAbierto;
        }
    }

    private void OnGUI()
    {
        if (!jugadorCerca || Time.timeScale == 0f || (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta))
            return;

        Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.4f) : Vector3.zero;
        if (screenPos.z <= 0) return;

        string textoPrompt = "";
        if ((int)tipo <= 3)
        {
            textoPrompt = "[E] Recoger " + nombreMostrado;
        }
        else if (tipo == TipoInteractivo.CampanarioAlba)
        {
            int rel = Nivel4Manager.Instance != null ? Nivel4Manager.Instance.ReliquiasRecolectadas : 0;
            textoPrompt = rel >= 3
                ? "★ [E] ¡TAÑER LA CAMPANA DEL ALBA Y ROMPER LA MALDICIÓN! ★"
                : $"Campana del Alba (Reúne las 3 Reliquias: {rel}/3)";
        }
        else if (tipo == TipoInteractivo.CartelLore)
        {
            textoPrompt = "📜 [E / L] Leer Cartel Histórico de Carmen 'La Bruja'";
        }

        float bw = Mathf.Max(260f, textoPrompt.Length * 9.5f);
        float bh = 30f;
        Rect br = new Rect(screenPos.x - bw * 0.5f, Screen.height - screenPos.y - bh, bw, bh);

        GUI.color = new Color(0.12f, 0.10f, 0.08f, 0.94f);
        GUI.DrawTexture(br, Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.72f, 0.35f, 1f);
        GUI.DrawTexture(new Rect(br.x, br.y, br.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(br.x, br.yMax - 2f, br.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle bSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        bSt.normal.textColor = new Color(1f, 0.94f, 0.78f);
        GUI.Label(br, textoPrompt, bSt);
    }
}
