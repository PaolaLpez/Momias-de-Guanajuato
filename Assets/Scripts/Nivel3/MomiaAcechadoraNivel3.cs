using System.Collections;
using UnityEngine;

/// <summary>
/// Gemela 1 ("La que Acecha") — Nivel 3: 2:00 AM a 3:00 AM.
/// Regla de supervivencia: Solo avanza cuando el vigilante NO la está mirando directamente.
/// Si el vigilante la mira de frente, se congela como una estatua pétrea.
/// Ataque con daño real y cooldown (NO muerte instantánea): permite correr y esconderse.
/// Utiliza los sprites completos y fluidos de alta definición de la momia.
/// </summary>
public class MomiaAcechadoraNivel3 : MonoBehaviour
{
    [Header("Parámetros de Acecho")]
    public float velocidadAcecho = 3.2f;
    public float distanciaAtaque = 1.4f;
    public float anguloConoVision = 75f;
    public float rangoMaximoVision = 16f;
    public float danioPorGolpe = 30f;

    [Header("Estado")]
    public bool estaCongelada = false;
    public float distanciaAlJugador = 999f;
    public bool activa = false;

    private float timerCooldownAtaque = 0f;

    [Header("Audio")]
    private AudioSource audioPasos;
    private AudioSource audioVoz;
    private AudioClip clipPasos;
    private AudioClip clipVozComeCloser;
    private float timerVozProximidad = 0f;

    private Transform playerTransform;
    private PlayerController playerScript;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    // Sprites de animación completos y probados
    private Sprite[] walkSprites;
    private Sprite idleSprite;
    private Sprite attackSprite;
    private float animTimer = 0f;
    private int animFrame = 0;

    private void Awake()
    {
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
            GameObject l1 = new GameObject("AuraMomiaAcechadora");
            l1.transform.SetParent(transform, false);
            l1.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var l1Light = l1.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            l1Light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            l1Light.color = new Color(1.0f, 0.22f, 0.15f, 1f);
            l1Light.intensity = 1.5f;
            l1Light.pointLightOuterRadius = 4.5f;
        }

        // Configurar AudioSources
        audioPasos = gameObject.AddComponent<AudioSource>();
        audioPasos.loop = true;
        audioPasos.playOnAwake = false;
        audioPasos.spatialBlend = 0.85f;
        audioPasos.volume = 0.75f;
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

    [Header("Circuito de Patrulla por Todo el Museo")]
    public Vector2[] puntosPatrulla;
    private int indicePatrulla = 0;

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

        puntosPatrulla = new Vector2[]
        {
            new Vector2(0f, 4.5f),     // Pasillo Central frente a Caseta (visible desde el inicio)
            new Vector2(2.5f, 3.8f),   // Entrada este
            new Vector2(6.5f, 1.0f),   // Pasillo Este hacia Morgue
            new Vector2(13.0f, -2.5f), // Morgue Fría
            new Vector2(0f, -4.5f),    // Quirófano sur
            new Vector2(-12.0f, -2.5f),// Lab Químico
            new Vector2(-2.8f, 1.0f),  // Quirófano salida norte
            new Vector2(0f, 4.5f)      // Regreso frente a Caseta
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

        Vector3 diff = playerTransform.position - transform.position;
        distanciaAlJugador = diff.magnitude;

        // Si el jugador está escondido en un armario o está lejos (> 8.0m), patrulla continuamente por el museo
        bool enModoPatrulla = EsconditeNivel3.JugadorEscondido || distanciaAlJugador > 8.0f;

        if (enModoPatrulla)
        {
            estaCongelada = false;
            sr.color = new Color(0.95f, 0.98f, 1.0f, 1f);
            ActualizarPatrulla();
        }
        else
        {
            // Comprobar si el jugador la está mirando de frente
            estaCongelada = JugadorEstaMirandoAMomia();

            if (estaCongelada)
            {
                // Congelada como estatua bajo la mirada
                if (audioPasos.isPlaying) audioPasos.Stop();
                sr.color = new Color(0.75f, 0.80f, 0.88f, 1f); // Tono pétreo
                Sprite spFrente = HojasSpritesNivel3.MomiaFrame(0, 0);
                if (spFrente != null) sr.sprite = spFrente;
                else if (idleSprite != null) sr.sprite = idleSprite;
                sr.sortingOrder = HojasSpritesNivel3.OrdenY(transform.position.y);
            }
            else
            {
                // Avanza sigilosamente hacia el jugador
                sr.color = new Color(0.95f, 0.98f, 1.0f, 1f);
                MoverHaciaJugador(diff.normalized);
            }

            // Susurro "Come closer..."
            GestionarVozProximidad();

            // Chequeo de ataque con daño y cooldown
            if (distanciaAlJugador <= distanciaAtaque && timerCooldownAtaque <= 0f)
            {
                EjecutarAtaque();
            }
        }
    }

    private void ActualizarPatrulla()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;

        Vector2 objetivo = puntosPatrulla[indicePatrulla];
        Vector2 diff = objetivo - (Vector2)transform.position;

        if (diff.magnitude < 0.6f)
        {
            indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
            return;
        }

        Vector2 dir = diff.normalized;
        transform.position += (Vector3)(dir * (velocidadAcecho * 0.65f) * Time.deltaTime);

        // Audio de pasos
        if (clipPasos != null && !audioPasos.isPlaying)
        {
            audioPasos.Play();
        }

        // Animación de caminata direccional
        animTimer += Time.deltaTime * 4.5f;
        if (animTimer >= 1f)
        {
            animTimer = 0f;
            animFrame = (animFrame + 1) % 2;
        }

        int dirAnim = 0;
        bool voltearX = false;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            dirAnim = 1;
            voltearX = (dir.x < -0.05f);
        }
        else
        {
            if (dir.y > 0.05f) dirAnim = 3;
            else dirAnim = 0;
        }

        Sprite spHojas = HojasSpritesNivel3.MomiaFrame(dirAnim, animFrame);
        if (spHojas != null)
        {
            sr.sprite = spHojas;
            sr.flipX = voltearX;
        }

        sr.sortingOrder = HojasSpritesNivel3.OrdenY(transform.position.y);
    }

    private bool JugadorEstaMirandoAMomia()
    {
        if (distanciaAlJugador > rangoMaximoVision) return false;

        // Solo congelarse si está lo suficientemente cerca (< 5.2m) y dentro de la pantalla
        if (distanciaAlJugador > 5.2f) return false;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 vp = mainCam.WorldToViewportPoint(transform.position);
            // Solo congelarse si está dentro de la pantalla visible
            if (vp.x < 0.05f || vp.x > 0.95f || vp.y < 0.05f || vp.y > 0.95f) return false;
        }

        Vector2 dirJugador = Vector2.right;
        if (playerScript != null)
        {
            dirJugador = playerScript.facingDirection;
        }
        else
        {
            dirJugador = playerTransform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        }

        Vector2 haciaMomia = (transform.position - playerTransform.position).normalized;
        float angulo = Vector2.Angle(dirJugador, haciaMomia);

        return angulo <= (anguloConoVision * 0.5f);
    }

    private void MoverHaciaJugador(Vector3 dir)
    {
        transform.position += dir * velocidadAcecho * Time.deltaTime;

        // Sonido de pasos
        if (clipPasos != null && !audioPasos.isPlaying)
        {
            audioPasos.Play();
        }

        // Animación de caminata usando los sprites sliced de alta fidelidad
        animTimer += Time.deltaTime * 6.0f;
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
            dirAnim = 1; // Vista de perfil/3 cuartos
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

    private void GestionarVozProximidad()
    {
        if (clipVozComeCloser == null) return;

        timerVozProximidad -= Time.deltaTime;
        if (distanciaAlJugador <= 6.5f && timerVozProximidad <= 0f)
        {
            audioVoz.PlayOneShot(clipVozComeCloser, 0.95f);
            timerVozProximidad = Random.Range(10f, 16f);
        }
    }

    private void EjecutarAtaque()
    {
        timerCooldownAtaque = 1.6f;
        Sprite spAtk = HojasSpritesNivel3.MomiaAtaque();
        if (spAtk != null) sr.sprite = spAtk;
        else if (attackSprite != null) sr.sprite = attackSprite;

        // Empujar al jugador ligeramente hacia atrás para permitirle escapar y correr
        if (playerScript != null)
        {
            playerScript.RecibirDanio(danioPorGolpe, "Gemela que Acecha");

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
                    Nivel3Manager.Instance.ActivarGameOver("¡La Gemela que Acecha te dio el golpe de gracia al apartar la mirada!");
                }
            }
        }
    }
}
