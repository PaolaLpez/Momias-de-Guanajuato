using System.Collections;
using UnityEngine;

public class PuertaSalida : MonoBehaviour
{
    public float distanciaInteraccion = 3.8f;
    private Transform playerTransform;
    private PlayerController playerScript;
    private bool playerInRange = false;

    private SpriteRenderer sr;
    private Sprite spriteCerrada;
    private Sprite spriteAbierta;

    private AudioSource audioSource;
    private AudioClip clipApertura;
    private AudioClip clipTrancada;

    private bool yaAbierta = false;
    private bool abriendo = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        // Orden fijo por delante del muro sur (556) y suelo (-500), pero por detrás del jugador (660-710)
        // Esto garantiza que el jugador NUNCA quede tapado ni detrás del portón
        sr.sortingOrder = 580;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        CargarSprites();
        GenerarSonidos();
    }

    void Start()
    {
        BuscarJugador();
        ActualizarSprite();
    }

    private void CargarSprites()
    {
        spriteCerrada = Resources.Load<Sprite>("Props/puerta_escape_closed");
        spriteAbierta = Resources.Load<Sprite>("Props/puerta_escape_opened");

        if (spriteCerrada != null && sr != null && !yaAbierta)
        {
            sr.sprite = spriteCerrada;
        }
    }

    private void ActualizarSprite()
    {
        if (sr != null)
        {
            sr.sprite = yaAbierta ? spriteAbierta : spriteCerrada;
            sr.sortingOrder = 580;
        }
    }

    void BuscarJugador()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                playerScript = p.GetComponent<PlayerController>();
            }
        }
    }

    void Update()
    {
        BuscarJugador();
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        playerInRange = (dist <= distanciaInteraccion);

        if (playerInRange && !yaAbierta && !abriendo && (Input.GetKeyDown(KeyCode.E) || TouchControlsManager.IsInteractTriggered()))
        {
            IntentarEscape();
        }
    }

    private bool EstaArriba()
    {
        if (playerTransform == null) return false;
        // Si el jugador se encuentra arriba del portón (Y superior al umbral)
        return playerTransform.position.y > (transform.position.y - 0.25f);
    }

    private bool EstaAUnoDeLosLados()
    {
        if (playerTransform == null) return false;

        // No debe estar arriba
        if (EstaArriba()) return false;

        // Debe estar forzosamente a uno de los lados (costado izquierdo o derecho)
        float diffX = Mathf.Abs(playerTransform.position.x - transform.position.x);
        return diffX >= 1.0f;
    }

    private void IntentarEscape()
    {
        bool tieneLlave = (playerScript != null && playerScript.tieneLlave) || 
                          (Level1Manager.Instance != null && Level1Manager.Instance.CurrentState == LevelState.KeyAcquired);

        if (!tieneLlave)
        {
            if (audioSource != null && clipTrancada != null)
            {
                audioSource.PlayOneShot(clipTrancada, 0.9f);
            }

            if (Level1Manager.Instance != null)
            {
                PistolController pistol = (playerScript != null) ? playerScript.GetComponent<PistolController>() : null;
                if (pistol == null || !pistol.HasPistol)
                {
                    Level1Manager.Instance.MostrarBanner("PORTÓN DE ESCAPE CERRADO CON CADENAS\nPrimero debes encontrar tu pistola en la Oficina Noroeste y la llave del museo.");
                }
                else
                {
                    Level1Manager.Instance.MostrarBanner("PORTÓN DE ESCAPE CERRADO CON CADENAS\nYa tienes tu pistola. Ahora busca la Llave del Museo en la Galería Noreste.");
                }
            }
            return;
        }

        // Si tiene la llave pero está arriba de la puerta: no se abre
        if (EstaArriba())
        {
            if (audioSource != null && clipTrancada != null)
            {
                audioSource.PlayOneShot(clipTrancada, 0.5f);
            }

            if (Level1Manager.Instance != null)
            {
                Level1Manager.Instance.MostrarBanner("COLÓCATE A UN LADO DEL PORTÓN\nNo puedes abrir la compuerta desde arriba. Sitúate a un costado.");
            }
            return;
        }

        // Debe colocarse a uno de los lados de la puerta
        if (!EstaAUnoDeLosLados())
        {
            if (Level1Manager.Instance != null)
            {
                Level1Manager.Instance.MostrarBanner("ACÉRCATE A UN COSTADO DEL PORTÓN\nDebes estar situado a uno de los lados para accionar la cerradura colonial.");
            }
            return;
        }

        // Posición correcta a los lados y con la llave: abrir compuerta
        StartCoroutine(SecuenciaApertura());
    }

    private IEnumerator SecuenciaApertura()
    {
        abriendo = true;
        yaAbierta = true;

        // Desactivar colisión sólida para permitir el paso
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;

        // 1. Sonido de desbloqueo de cerrojos, cadenas y reja pesada abriéndose
        if (audioSource != null && clipApertura != null)
        {
            audioSource.PlayOneShot(clipApertura, 1.0f);
        }

        // 2. Cambiar inmediatamente el gráfico al portón abierto con escalinatas y luz exterior
        if (sr != null && spriteAbierta != null)
        {
            sr.sprite = spriteAbierta;
        }

        // 3. Notificación triunfal en pantalla
        if (Level1Manager.Instance != null)
        {
            Level1Manager.Instance.MostrarBanner("¡CERROJOS COLONIALES ABIERTOS!\nLas pesadas compuertas de hierro se abren de par en par...");
        }

        // 4. Pausa de medio segundo para que el jugador aprecie visualmente el portón abriéndose
        yield return new WaitForSeconds(0.75f);

        // 5. Activar la pantalla de victoria
        if (Level1Manager.Instance != null)
        {
            Level1Manager.Instance.NotifyDoorInteraction();
        }
        else
        {
            Nivel1Manager manager = FindFirstObjectByType<Nivel1Manager>();
            if (manager != null)
            {
                manager.ActivarVictoria();
            }
        }

        abriendo = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void OnGUI()
    {
        if (Level1Manager.Instance != null && (Level1Manager.Instance.IsNivelCompletado || Level1Manager.Instance.IsJuegoTerminado)) return;

        if (playerInRange && !yaAbierta && !abriendo)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0, 1.8f, 0));
            if (screenPos.z > 0)
            {
                float w = 295f;
                float h = 32f;
                Rect rect = new Rect(screenPos.x - w / 2, Screen.height - screenPos.y - h / 2, w, h);

                bool tieneLlave = (playerScript != null && playerScript.tieneLlave) || 
                                  (Level1Manager.Instance != null && Level1Manager.Instance.CurrentState == LevelState.KeyAcquired);

                string texto;
                if (!tieneLlave)
                {
                    texto = "<color=#FF6666>[Portón Cerrado - Falta Llave]</color>";
                }
                else if (EstaArriba())
                {
                    texto = "<color=#FFAA33>[Colócate a un lado del portón para abrir]</color>";
                }
                else if (!EstaAUnoDeLosLados())
                {
                    texto = "<color=#FFAA33>[Acércate a un costado para abrir]</color>";
                }
                else
                {
                    texto = "<color=#55FF55><b>[E] Abrir Portón de Escape</b></color>";
                }

                GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.94f);
                GUI.Box(rect, "");
                GUI.color = Color.white;

                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 11;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                style.richText = true;

                GUI.Label(rect, texto, style);
            }
        }
    }

    private void GenerarSonidos()
    {
        clipApertura = GenerarAudioApertura();
        clipTrancada = GenerarAudioTrancada();
    }

    private AudioClip GenerarAudioApertura()
    {
        int sampleRate = 44100;
        float duration = 1.25f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Fase 1 (0.0s - 0.25s): Golpe seco de cerrojo y clic metálico
            float click = 0f;
            if (t < 0.25f)
            {
                float clickEnv = Mathf.Exp(-t * 35f);
                click = Mathf.Sin(2f * Mathf.PI * 480f * t) * clickEnv * 0.7f;
                click += (Random.value * 2f - 1f) * clickEnv * 0.4f;
            }

            // Fase 2 (0.12s - 0.65s): Cadenas pesadas cayendo / tintineo metálico
            float cadenas = 0f;
            if (t > 0.12f && t < 0.65f)
            {
                float tc = t - 0.12f;
                float cadenaEnv = Mathf.Exp(-tc * 7f) * Mathf.Sin(tc * 25f);
                float f1 = 1250f + Mathf.Sin(tc * 60f) * 400f;
                cadenas = Mathf.Sin(2f * Mathf.PI * f1 * t) * cadenaEnv * 0.35f;
            }

            // Fase 3 (0.25s - 1.25s): Rechinido solemne de portón de hierro/madera abriéndose
            float rechinido = 0f;
            if (t > 0.25f)
            {
                float tr = t - 0.25f;
                float gateEnv = Mathf.Sin(Mathf.Clamp01(tr / 1.0f) * Mathf.PI);
                float fCreak = 180f + 70f * Mathf.Sin(tr * 18f) + 40f * Mathf.Cos(tr * 9f);
                rechinido = (Mathf.Sin(2f * Mathf.PI * fCreak * t) * 0.45f + (Random.value * 2f - 1f) * 0.15f) * gateEnv;
            }

            samples[i] = Mathf.Clamp(click + cadenas + rechinido, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Porton_Apertura_Procedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarAudioTrancada()
    {
        int sampleRate = 44100;
        float duration = 0.5f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 12f);
            float hit1 = Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.5f;
            float hit2 = Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.3f;
            float rattle = (Random.value * 2f - 1f) * 0.25f;

            samples[i] = Mathf.Clamp((hit1 + hit2 + rattle) * env, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Porton_Trancado_Procedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
