using System.Collections;
using UnityEngine;

/// <summary>
/// Gemela 2 ("La Ciega / La que Escucha") — Nivel 3: 2:00 AM a 3:00 AM.
/// Regla de supervivencia: Es ciega pero tiene un oído ultrasensible.
/// Si corres con Shift, pisas cristales rotos, fuerzas la gaveta o recibes una picadura,
/// se lanza a la carrera hacia el punto del sonido.
/// Ataque con daño real y cooldown (NO muerte instantánea): permite correr y esconderse.
/// Utiliza los sprites completos y fluidos de alta definición de la momia.
/// </summary>
public class MomiaCiegaNivel3 : MonoBehaviour
{
    public enum EstadoCiega { Patrullando, Alertada, Investigando, Cazando }

    [Header("Parámetros de Velocidad")]
    public float velocidadPatrulla = 1.6f;
    public float velocidadCaceria = 4.2f;
    public float distanciaAtaque = 1.4f;
    public float danioPorGolpe = 30f;

    [Header("Estado Actual")]
    public EstadoCiega estadoActual = EstadoCiega.Patrullando;
    public bool activa = false;
    public float distanciaAlJugador = 999f;

    [Header("Circuito de Patrulla por Todo el Taller")]
    public Vector2[] puntosPatrulla;
    private int indicePatrulla = 0;
    private Vector2 destinoInvestigacion;
    private float tiempoEnAlerta = 0f;
    private float timerCooldownAtaque = 0f;

    [Header("Audio")]
    private AudioSource audioPasos;
    private AudioSource audioVoz;
    private AudioClip clipPasos;
    private AudioClip clipVozComeCloser;
    private float timerVoz = 0f;

    private Transform playerTransform;
    private PlayerController playerScript;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private Sprite[] walkSprites;
    private Sprite idleSprite;
    private Sprite attackSprite;
    private float animTimer = 0f;
    private int animFrame = 0;

    public static MomiaCiegaNivel3 Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 350;
        transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        if (GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>() == null)
        {
            GameObject l2 = new GameObject("AuraMomiaCiega");
            l2.transform.SetParent(transform, false);
            l2.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var l2Light = l2.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            l2Light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            l2Light.color = new Color(0.20f, 0.85f, 1.0f, 1f);
            l2Light.intensity = 1.5f;
            l2Light.pointLightOuterRadius = 4.5f;
        }

        audioPasos = gameObject.AddComponent<AudioSource>();
        audioPasos.loop = true;
        audioPasos.playOnAwake = false;
        audioPasos.spatialBlend = 0.85f;
        audioPasos.volume = 0.8f;
        audioPasos.minDistance = 2f;
        audioPasos.maxDistance = 14f;

        audioVoz = gameObject.AddComponent<AudioSource>();
        audioVoz.loop = false;
        audioVoz.playOnAwake = false;
        audioVoz.spatialBlend = 0.9f;
        audioVoz.volume = 0.95f;
        audioVoz.minDistance = 2f;
        audioVoz.maxDistance = 12f;

        CargarRecursosCompletos();
    }

    private void CargarRecursosCompletos()
    {
        walkSprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            walkSprites[i] = Resources.Load<Sprite>("Momia/momia_walk_" + i);
        }

        idleSprite = Resources.Load<Sprite>("Momia/momia_idle");
        attackSprite = Resources.Load<Sprite>("Momia/momia_attack");

        Sprite spFrame0 = HojasSpritesNivel3.MomiaFrame(0, 0);
        if (spFrame0 != null) sr.sprite = spFrame0;
        else if (idleSprite != null) sr.sprite = idleSprite;

        clipPasos = Resources.Load<AudioClip>("AudioNivel3/pasos_momia_nivel3");
        if (clipPasos == null) clipPasos = Resources.Load<AudioClip>("pasos_momia_nivel3");
        if (clipPasos != null) audioPasos.clip = clipPasos;

        clipVozComeCloser = Resources.Load<AudioClip>("AudioNivel3/come_closer");
        if (clipVozComeCloser == null) clipVozComeCloser = Resources.Load<AudioClip>("come_closer");
    }

    private void Start()
    {
        BuscarJugador();
        activa = true;
        if (sr != null)
        {
            sr.enabled = true;
            sr.sortingOrder = 350;
            if (sr.sprite == null) sr.sprite = HojasSpritesNivel3.MomiaFrame(0, 0);
        }

        // Circuito extenso de patrulla que recorre TODO el taller y cruza visiblemente el pasillo central
        puntosPatrulla = new Vector2[]
        {
            new Vector2(-2.2f, 3.8f),   // Umbral visible de la Caseta (visible desde el inicio)
            new Vector2(0f, 1.0f),      // Pasillo central
            new Vector2(-6.0f, 0f),     // Pasaje a Laboratorio
            new Vector2(-13.5f, -2f),   // Mesón de reactivos
            new Vector2(0f, -4f),       // Quirófano sur
            new Vector2(13.5f, -2f),    // Morgue fría
            new Vector2(0f, 2.5f),      // Pasillo central
            new Vector2(-2.2f, 3.8f)    // Regreso a Caseta
        };
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

        if (timerCooldownAtaque > 0f) timerCooldownAtaque -= Time.deltaTime;

        if (!activa || (Nivel3Manager.Instance != null && Nivel3Manager.Instance.JuegoPausadoOTerminado))
        {
            if (audioPasos.isPlaying) audioPasos.Stop();
            return;
        }

        distanciaAlJugador = Vector2.Distance(transform.position, playerTransform.position);

        // Si el jugador está escondido en un armario o taquilla, pierde la alerta
        if (EsconditeNivel3.JugadorEscondido)
        {
            if (estadoActual == EstadoCiega.Cazando)
            {
                estadoActual = EstadoCiega.Investigando;
                tiempoEnAlerta = 0f;
            }
        }
        else
        {
            // Detectar si el jugador corre con Shift o botón táctil de carrera
            bool corriendo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || TouchControlsManager.IsRunning;
            if (corriendo && distanciaAlJugador < 18f)
            {
                AlertarRuido(playerTransform.position, "¡Tus pasos apresurados alertaron a la Gemela Ciega!");
            }
        }

        // Tinte sepia envejecido
        sr.color = new Color(0.88f, 0.78f, 0.68f, 1f);

        // Susurro macabro de proximidad
        GestionarVozProximidad();

        // Máquina de estados
        switch (estadoActual)
        {
            case EstadoCiega.Patrullando:
                ActualizarPatrulla();
                break;

            case EstadoCiega.Alertada:
            case EstadoCiega.Cazando:
                ActualizarCaceria();
                break;

            case EstadoCiega.Investigando:
                ActualizarInvestigacion();
                break;
        }

        // Chequeo de impacto con el jugador (con daño y cooldown)
        if (!EsconditeNivel3.JugadorEscondido && distanciaAlJugador <= distanciaAtaque && timerCooldownAtaque <= 0f)
        {
            EjecutarAtaque();
        }
    }

    private void ActualizarPatrulla()
    {
        if (puntosPatrulla.Length == 0) return;

        Vector2 objetivo = puntosPatrulla[indicePatrulla];
        Vector2 dir = (objetivo - (Vector2)transform.position);

        if (dir.magnitude < 0.8f)
        {
            indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
        }
        else
        {
            MoverEnDireccion(dir.normalized, velocidadPatrulla, 1f);
        }
    }

    private void ActualizarCaceria()
    {
        Vector2 destino = EsconditeNivel3.JugadorEscondido ? destinoInvestigacion : (Vector2)playerTransform.position;
        Vector2 dir = (destino - (Vector2)transform.position);

        MoverEnDireccion(dir.normalized, velocidadCaceria, 1.6f);

        tiempoEnAlerta += Time.deltaTime;
        if (tiempoEnAlerta > 7f && distanciaAlJugador > 10f)
        {
            estadoActual = EstadoCiega.Investigando;
            destinoInvestigacion = transform.position;
            tiempoEnAlerta = 0f;
        }
    }

    private void ActualizarInvestigacion()
    {
        tiempoEnAlerta += Time.deltaTime;
        if (audioPasos.isPlaying) audioPasos.Stop();
        if (idleSprite != null) sr.sprite = idleSprite;

        if (tiempoEnAlerta >= 3.0f)
        {
            estadoActual = EstadoCiega.Patrullando;
            tiempoEnAlerta = 0f;
        }
    }

    private void MoverEnDireccion(Vector2 dir, float vel, float pitchAudio)
    {
        transform.position += (Vector3)(dir * vel * Time.deltaTime);

        // Audio de pasos
        if (clipPasos != null)
        {
            audioPasos.pitch = pitchAudio;
            if (!audioPasos.isPlaying) audioPasos.Play();
        }

        // Animación fluida usando sprites sliced de alta fidelidad
        animTimer += Time.deltaTime * (vel * 2.5f);
        if (animTimer >= 1f)
        {
            animTimer = 0f;
            animFrame = (animFrame + 1) % 2;
        }

        // Determinar dirección de animación: 0 frente, 1 derecha, 2 izquierda, 3 espalda
        int dirAnim = 0;
        bool voltearX = false;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            dirAnim = 1; // Perfil / 3 cuartos
            voltearX = (dir.x < -0.05f); // Si va a la izquierda, reflejar
        }
        else
        {
            if (dir.y > 0.05f) dirAnim = 3; // Espalda
            else dirAnim = 0; // Frente
        }

        Sprite spHojas = HojasSpritesNivel3.MomiaFrame(dirAnim, animFrame);
        if (spHojas != null)
        {
            sr.sprite = spHojas;
            sr.flipX = voltearX;
        }
        else if (walkSprites != null && walkSprites[animFrame] != null)
        {
            sr.sprite = walkSprites[animFrame];
            sr.flipX = voltearX;
        }

        sr.sortingOrder = HojasSpritesNivel3.OrdenY(transform.position.y);
    }

    public void AlertarRuido(Vector2 posicionRuido, string razon = "")
    {
        if (Nivel3Manager.Instance != null && Nivel3Manager.Instance.JuegoPausadoOTerminado) return;
        if (EsconditeNivel3.JugadorEscondido) return;

        destinoInvestigacion = posicionRuido;
        estadoActual = EstadoCiega.Cazando;
        tiempoEnAlerta = 0f;

        if (!string.IsNullOrEmpty(razon) && Nivel3Manager.Instance != null)
        {
            Nivel3Manager.Instance.MostrarAvisoRuido(razon);
        }
    }

    private void GestionarVozProximidad()
    {
        if (clipVozComeCloser == null) return;

        timerVoz -= Time.deltaTime;
        if (distanciaAlJugador <= 6.5f && timerVoz <= 0f)
        {
            audioVoz.PlayOneShot(clipVozComeCloser, 0.95f);
            timerVoz = Random.Range(9f, 15f);
        }
    }

    private void EjecutarAtaque()
    {
        timerCooldownAtaque = 1.6f;
        Sprite spAtk = HojasSpritesNivel3.MomiaAtaque();
        if (spAtk != null) sr.sprite = spAtk;
        else if (attackSprite != null) sr.sprite = attackSprite;

        if (playerScript != null)
        {
            playerScript.RecibirDanio(danioPorGolpe, "Gemela Ciega");

            Rigidbody2D pRb = playerScript.GetComponent<Rigidbody2D>();
            if (pRb != null)
            {
                Vector2 empuje = (playerTransform.position - transform.position).normalized * 2.5f;
                pRb.position += empuje * 0.25f;
            }

            if (playerScript.EstaMuerto || playerScript.vidaActual <= 0f)
            {
                if (Nivel3Manager.Instance != null)
                {
                    Nivel3Manager.Instance.ActivarGameOver("¡La Gemela Ciega escuchó tus pasos y te destrozó en la oscuridad!");
                }
            }
        }
    }
}
