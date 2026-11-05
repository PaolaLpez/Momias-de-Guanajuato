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
        bool cerca = dist <= distanciaInteraccion;

        if (ocupado)
        {
            // Mantener al jugador dentro del escondite
            playerTransform.position = transform.position;
            if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                SalirDelEscondite();
            }
        }
        else if (cerca && !JugadorEscondido)
        {
            if (Input.GetKeyDown(KeyCode.E))
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
