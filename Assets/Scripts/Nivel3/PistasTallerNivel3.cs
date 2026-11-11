using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la interacción con las 3 pistas del Taller de Restauración:
/// 1. Camilla Central (Etiqueta de morgue -> Cifra 7) + susto de bandejas.
/// 2. Mesón de Químicos (Frasco y pizarra -> Cifra 3) + zona de vidrios rotos.
/// 3. Gaveta Fría (Morgue helada forzada 4s -> Cifra 9) + niebla y escorpiones.
/// </summary>
public class PistasTallerNivel3 : MonoBehaviour
{
    public enum TipoPista { CamillaCentral, MesonQuimicos, GavetaMorgue }

    [Header("Configuración")]
    public TipoPista tipoPista;
    public float distanciaInteraccion = 2.2f;
    public bool yaDescubierta = false;

    [Header("Progreso Gaveta Helada (Tipo 3)")]
    private float tiempoForzando = 0f;
    private const float TIEMPO_REQUERIDO_GAVETA = 3.8f;
    private bool jugadorEnRango = false;

    private Transform playerTransform;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.85f;
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
            if (p != null) playerTransform = p.transform;
        }
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

    private bool ComprobarToqueMantenido()
    {
        if (Camera.main == null) return false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    Vector2 worldTap = Camera.main.ScreenToWorldPoint(t.position);
                    Collider2D col = GetComponent<Collider2D>();
                    if (col != null && col.OverlapPoint(worldTap)) return true;
                    if (Vector2.Distance(worldTap, transform.position) <= 2.5f) return true;
                }
            }
        }
        else if (Input.GetMouseButton(0))
        {
            Vector2 worldTap = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldTap)) return true;
            if (Vector2.Distance(worldTap, transform.position) <= 2.5f) return true;
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

        if (yaDescubierta || (Nivel3Manager.Instance != null && Nivel3Manager.Instance.JuegoPausadoOTerminado)) return;

        Collider2D col = GetComponent<Collider2D>();
        float dist;
        if (col != null)
        {
            Vector2 closest = col.ClosestPoint(playerTransform.position);
            dist = Vector2.Distance(closest, playerTransform.position);
        }
        else
        {
            dist = Vector2.Distance(transform.position, playerTransform.position);
        }
        jugadorEnRango = (dist <= 3.5f);

        bool toqueDirecto = ComprobarToqueDirecto();
        bool toqueMantenido = ComprobarToqueMantenido();

        if (!jugadorEnRango && !toqueDirecto && !toqueMantenido)
        {
            tiempoForzando = 0f;
            return;
        }

        // Tipo 1 y 2: Presionar [F] (o Enter, Espacio o botón táctil o toque directo en pantalla)
        if (tipoPista != TipoPista.GavetaMorgue)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || TouchControlsManager.IsInteractTriggered() || TouchControlsManager.IsJumpTriggered() || toqueDirecto)
            {
                DescubrirPista();
            }
        }
        else
        {
            // Tipo 3: Mantener presionado para deshelar/forzar la gaveta (o mantener dedo sobre la gaveta)
            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space) || TouchControlsManager.IsInteractHeld || toqueMantenido)
            {
                tiempoForzando += Time.deltaTime;

                if (Nivel3Manager.Instance != null)
                {
                    float pct = Mathf.Clamp01(tiempoForzando / TIEMPO_REQUERIDO_GAVETA);
                    Nivel3Manager.Instance.MostrarProgresoInteraccion("Forzando gaveta congelada [F]...", pct);
                }

                if (tiempoForzando >= TIEMPO_REQUERIDO_GAVETA)
                {
                    DescubrirPista();
                }
            }
            else
            {
                if (tiempoForzando > 0f)
                {
                    tiempoForzando = Mathf.Max(0f, tiempoForzando - Time.deltaTime * 2f);
                }
            }
        }
    }

    private void DescubrirPista()
    {
        if (yaDescubierta) return;
        yaDescubierta = true;

        switch (tipoPista)
        {
            case TipoPista.CamillaCentral:
                // Cifra 7
                if (Nivel3Manager.Instance != null)
                {
                    Nivel3Manager.Instance.RegistrarCifra(1, 7, 
                        "FICHA CLÍNICA DE LA CAMILLA:\n\"Sujeto A — Causa de defunción: Asfixia. Año de registro: 193[7]\"\n\n¡La mano disecada bajo la sábana se contrajo violentamente al tocarla!");
                }
                // Ruido al tirar instrumental
                if (MomiaCiegaNivel3.Instance != null)
                {
                    MomiaCiegaNivel3.Instance.AlertarRuido(transform.position, "¡Tintineo de bandejas en la camilla central!");
                }
                break;

            case TipoPista.MesonQuimicos:
                // Cifra 3
                if (Nivel3Manager.Instance != null)
                {
                    Nivel3Manager.Instance.RegistrarCifra(2, 3, 
                        "MESÓN DE MEZCLA DE RESINAS:\nPizarra con fórmula: \"Solución de glicerina y alcohol al [3]% para evitar descomposición.\"\nFrasco reactivo corroborado.");
                }
                break;

            case TipoPista.GavetaMorgue:
                // Cifra 9
                if (Nivel3Manager.Instance != null)
                {
                    Nivel3Manager.Instance.RegistrarCifra(3, 9, 
                        "GAVETA METÁLICA DE LA MORGUE:\nGrabado en el acero con bisturí: \"Control de escarcha: Nivel [9]\".\n\n¡El estruendo del metal forzado resonó por todo el taller!");
                }
                // El rechinido fuerte de metal atrae a la momia ciega
                if (MomiaCiegaNivel3.Instance != null)
                {
                    MomiaCiegaNivel3.Instance.AlertarRuido(transform.position, "¡Fuerte rechinido de gaveta oxidada forzada!");
                }
                // Liberar escorpiones asustados por el frío
                LiberarEscorpionesCercanos();
                break;
        }
    }

    private void LiberarEscorpionesCercanos()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject escObj = new GameObject("Escorpion_Gaveta_" + i);
            escObj.transform.position = transform.position + new Vector3(Random.Range(-0.8f, 0.8f), -0.6f, 0f);
            escObj.AddComponent<EscorpionNivel3>();
        }
    }

    public bool EstaCerca()
    {
        if (playerTransform == null || yaDescubierta) return false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 closest = col.ClosestPoint(playerTransform.position);
            return Vector2.Distance(closest, playerTransform.position) <= 3.2f;
        }
        return Vector2.Distance(transform.position, playerTransform.position) <= 4.0f;
    }

    public string ObtenerTextoPrompt()
    {
        if (yaDescubierta) return "";
        if (tipoPista == TipoPista.CamillaCentral) return "[F] Inspeccionar Camilla y Etiqueta Forense (1ª Cifra)";
        if (tipoPista == TipoPista.MesonQuimicos) return "[F] Examinar Frasco de Reactivos y Pizarra (2ª Cifra)";
        if (tipoPista == TipoPista.GavetaMorgue) return "Mantén [F] presionado para forzar gaveta congelada (3ª Cifra)";
        return "[F] Inspeccionar pista de restauración";
    }
}
