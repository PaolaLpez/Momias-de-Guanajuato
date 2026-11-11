using System.Collections;
using UnityEngine;

/// <summary>
/// Permite al jugador esconderse en armarios metálicos o cajas mortuorias en el Nivel 2.
/// Mientras está escondido, la Momia que escucha no puede detectarlo.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EsconditeNivel2 : MonoBehaviour
{
    [Header("Configuración del Escondite")]
    public string nombreEscondite = "Taquilla de Mantenimiento";
    public Sprite spriteCerrado;
    public Sprite spriteAbierto;
    public Vector3 offsetJugadorDentro = Vector3.zero;

    private bool jugadorEnRango = false;
    private bool jugadorDentro = false;
    private GameObject playerObj;
    private SpriteRenderer playerSr;
    private Collider2D playerCol;
    private Rigidbody2D playerRb;
    private SpriteRenderer esconditeSr;
    private AudioSource audioSource;

    public bool EstaOcupado => jugadorDentro;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        esconditeSr = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // Audio 2D estéreo directo
            audioSource.volume = 1.0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorEnRango = true;
            playerObj = collision.gameObject;
            playerSr = playerObj.GetComponent<SpriteRenderer>();
            playerCol = playerObj.GetComponent<Collider2D>();
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !jugadorDentro)
        {
            jugadorEnRango = false;
        }
    }

    private void AsegurarJugador()
    {
        if (playerObj == null)
        {
            if (PlayerController.Instance != null)
            {
                playerObj = PlayerController.Instance.gameObject;
            }
            else
            {
                playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Vigilante");
            }
            if (playerObj != null)
            {
                playerCol = playerObj.GetComponent<Collider2D>();
                playerRb = playerObj.GetComponent<Rigidbody2D>();
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
        AsegurarJugador();

        float distJugador = playerObj != null ? Vector2.Distance(transform.position, playerObj.transform.position) : 999f;
        bool cerca = distJugador <= 3.2f || jugadorEnRango;
        bool toque = ComprobarToqueDirecto();

        if (jugadorDentro)
        {
            // Mantener al jugador bloqueado dentro del escondite y 100% invisible
            if (playerObj != null)
            {
                playerObj.transform.position = transform.position + offsetJugadorDentro;
                if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
            }
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.estaEscondido = true;
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || TouchControlsManager.IsInteractTriggered() || TouchControlsManager.IsJumpTriggered() || toque)
            {
                SalirEscondite();
            }
        }
        else if ((cerca || toque) && (Input.GetKeyDown(KeyCode.E) || TouchControlsManager.IsInteractTriggered() || toque))
        {
            EntrarEscondite();
        }
    }

    private void EntrarEscondite()
    {
        AsegurarJugador();
        jugadorDentro = true;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.estaEscondido = true;
        }

        if (playerObj != null)
        {
            playerObj.transform.position = transform.position + offsetJugadorDentro;
            if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
        }
        if (playerCol != null) playerCol.enabled = false;

        if (spriteCerrado != null && esconditeSr != null)
        {
            esconditeSr.sprite = spriteCerrado;
        }

        ReproducirSonidoPuerta(true);

        // Emitir alerta acústica para la Momia: si entras de pie sin sigilo, la puerta/losa retumba con fuerza
        bool enSigilo = SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo;
        float radioRuido = enSigilo ? 4.5f : 18.0f;
        string etiqueta = enSigilo
            ? "¡CIERRE SUAVE DE ESCONDITE!"
            : (nombreEscondite.Contains("Ataúd") || nombreEscondite.Contains("Sarcófago")
                ? "¡ESTRUENDO DE LOSA SEPULCRAL DE CRIPTA!"
                : "¡GOLPE METÁLICO DE TAQUILLA!");

        TrampaAcusticaNivel2.EmitirRuido(transform.position, radioRuido, etiqueta);
        Debug.Log($"<color=cyan>[Escondite Nivel 2] El jugador se ha ocultado en: {nombreEscondite} (Ruido: {radioRuido}m)</color>");
    }

    public void SalirEscondite()
    {
        AsegurarJugador();
        jugadorDentro = false;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.estaEscondido = false;
        }

        if (playerCol != null) playerCol.enabled = true;
        if (playerObj != null)
        {
            playerObj.transform.position = transform.position + new Vector3(0f, -0.65f, 0f);
        }

        if (spriteAbierto != null && esconditeSr != null)
        {
            esconditeSr.sprite = spriteAbierto;
        }

        ReproducirSonidoPuerta(false);

        // Emitir alerta al salir
        bool enSigilo = SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo;
        float radioRuido = enSigilo ? 3.5f : 14.0f;
        string etiqueta = enSigilo ? "¡SALIDA EN SIGILO!" : "¡APERTURA DE ESCONDITE!";
        TrampaAcusticaNivel2.EmitirRuido(transform.position, radioRuido, etiqueta);
        Debug.Log($"<color=cyan>[Escondite Nivel 2] El jugador ha salido de: {nombreEscondite}</color>");
    }

    private void ReproducirSonidoPuerta(bool cerrando)
    {
        if (audioSource != null)
        {
            audioSource.pitch = cerrando ? 0.9f : 1.1f;
            audioSource.PlayOneShot(GenerarAudioPuertaMetal());
        }
    }

    private AudioClip GenerarAudioPuertaMetal()
    {
        int rate = 22050;
        float dur = 0.22f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float clank = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.3f;
            float noise = (Random.value * 2f - 1f) * 0.2f;
            samples[i] = (clank + noise) * Mathf.Exp(-t * 14f);
        }

        AudioClip clip = AudioClip.Create("AudioMetalLocker", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnGUI()
    {
        if (jugadorEnRango)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0, 1.35f, 0));
            if (screenPos.z > 0)
            {
                float w = 210f;
                float h = 26f;
                Rect rect = new Rect(screenPos.x - w / 2, Screen.height - screenPos.y - h / 2, w, h);

                string texto = jugadorDentro ? "<color=#FFCC00>[E] Salir del Escondite</color>" : $"<color=#00FFAA>[E] Ocultarse en {nombreEscondite}</color>";

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
        }
    }
}
