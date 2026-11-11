using System.Collections;
using UnityEngine;

/// <summary>
/// Escondite interactivo para el Nivel 3 (Taquillas de vestuario, armarios de morgue, etc.).
/// Permite al vigilante ocultarse de las momias gemelas para romper su línea de visión y sonido.
/// </summary>
public class EsconditeNivel3 : MonoBehaviour
{
    public static bool JugadorEscondido { get; private set; } = false;

    [Header("Configuración")]
    public string nombreEscondite = "Taquilla de Mantenimiento";
    public float distanciaInteraccion = 2.2f;

    private bool ocupado = false;
    private Transform playerTransform;
    private PlayerController playerScript;
    private SpriteRenderer playerSr;
    private Collider2D playerCol;
    private Rigidbody2D playerRb;

    private SpriteRenderer srEscondite;
    private AudioSource audioSource;
    private AudioClip clipPuerta;

    private void Awake()
    {
        srEscondite = GetComponent<SpriteRenderer>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.7f;
        audioSource.volume = 0.85f;
        audioSource.playOnAwake = false;

        clipPuerta = Resources.Load<AudioClip>("AudioNivel2/puerta_reja");
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
            if (p != null)
            {
                playerTransform = p.transform;
                playerScript = p.GetComponent<PlayerController>();
                playerSr = p.GetComponentInChildren<SpriteRenderer>();
                playerCol = p.GetComponent<Collider2D>();
                playerRb = p.GetComponent<Rigidbody2D>();
            }
        }
    }

    private bool ComprobarToqueDirecto()
    {
        if (Camera.main == null) return false;

        float zDist = Mathf.Abs(Camera.main.transform.position.z);
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    Vector3 sp = new Vector3(t.position.x, t.position.y, zDist);
                    Vector2 worldTap = Camera.main.ScreenToWorldPoint(sp);
                    Collider2D col = GetComponent<Collider2D>();
                    if (col != null && col.OverlapPoint(worldTap)) return true;
                    if (Vector2.Distance(worldTap, transform.position) <= 2.8f) return true;
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Vector3 sp = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDist);
            Vector2 worldTap = Camera.main.ScreenToWorldPoint(sp);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldTap)) return true;
            if (Vector2.Distance(worldTap, transform.position) <= 2.8f) return true;
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

        if (Nivel3Manager.Instance != null && Nivel3Manager.Instance.JuegoPausadoOTerminado)
        {
            if (ocupado) SalirDelEscondite();
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool cerca = dist <= distanciaInteraccion + 1.0f;
        bool toque = ComprobarToqueDirecto();

        if (ocupado)
        {
            // Mantener al jugador dentro del escondite
            playerTransform.position = transform.position;
            if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || TouchControlsManager.IsInteractTriggered() || TouchControlsManager.IsJumpTriggered() || toque)
            {
                SalirDelEscondite();
            }
        }
        else if ((cerca || toque) && !JugadorEscondido)
        {
            if (Input.GetKeyDown(KeyCode.E) || TouchControlsManager.IsInteractTriggered() || toque)
            {
                EntrarAlEscondite();
            }
        }
    }

    public void EntrarAlEscondite()
    {
        ocupado = true;
        JugadorEscondido = true;

        if (playerScript != null) playerScript.estaEscondido = true;
        if (playerTransform != null)
        {
            SpriteRenderer[] allSrs = playerTransform.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in allSrs) r.enabled = false;
        }
        if (playerCol != null) playerCol.enabled = false;

        if (srEscondite != null) srEscondite.color = new Color(0.6f, 0.8f, 0.95f);

        if (clipPuerta != null && audioSource != null)
        {
            audioSource.pitch = 1.2f;
            audioSource.PlayOneShot(clipPuerta, 0.7f);
        }

        if (Nivel3Manager.Instance != null)
        {
            Nivel3Manager.Instance.MostrarAvisoRuido("Te has escondido en " + nombreEscondite + ". Guarda silencio...");
        }
    }

    public void SalirDelEscondite()
    {
        ocupado = false;
        JugadorEscondido = false;

        if (playerScript != null) playerScript.estaEscondido = false;
        if (playerTransform != null)
        {
            Transform vp = playerTransform.Find("VisualPivot");
            if (vp != null)
            {
                vp.gameObject.SetActive(true);
                SpriteRenderer vpSr = vp.GetComponent<SpriteRenderer>();
                if (vpSr != null) vpSr.enabled = true;
            }
        }
        if (playerCol != null) playerCol.enabled = true;

        if (srEscondite != null) srEscondite.color = Color.white;

        // Dar un pequeño empujón hacia abajo para salir
        if (playerTransform != null)
        {
            playerTransform.position = transform.position + new Vector3(0f, -0.85f, 0f);
        }

        if (clipPuerta != null && audioSource != null)
        {
            audioSource.pitch = 0.95f;
            audioSource.PlayOneShot(clipPuerta, 0.7f);
        }
    }

    private void OnGUI()
    {
        if (ocupado)
        {
            float w = 460f;
            float h = 42f;
            Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height - 65f, w, h);
            GUI.Box(r, "");
            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.35f, 0.95f, 0.55f) }
            };
            GUI.Label(r, "🔒 OCULTO EN: " + nombreEscondite + " [E / ESPACIO para salir]", s);
        }
    }

    public bool EstaCerca()
    {
        if (playerTransform == null) return false;
        return Vector2.Distance(transform.position, playerTransform.position) <= distanciaInteraccion;
    }

    public bool EstaOcupado()
    {
        return ocupado;
    }
}
