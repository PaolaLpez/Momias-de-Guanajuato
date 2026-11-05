using UnityEngine;

public class MomiaController : MonoBehaviour
{
    [Header("Estadísticas")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;
    public float velocidadAcecho = 5.8f;
    public float velocidadPatrulla = 4.2f;
    public float rangoAtaque = 1.5f;
    public float danioPorSegundo = 35f;

    [Header("Detección y Sigilo")]
    public float distanciaVision = 9.5f;
    public float distanciaPerdida = 14.0f;

    [Header("Sprites de Animación")]
    public Sprite[] walkSprites;
    public Sprite idleSprite;
    public Sprite attackSprite;

    // Límites herméticos del Colosal Museo (sin salirse de la escena)
    public float museoMinX = -29.2f;
    public float museoMaxX = 29.2f;
    public float museoMinY = -20.2f;
    public float museoMaxY = 20.2f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D boxCol;
    private Transform playerTransform;
    private PlayerController playerScript;

    private float animTimer = 0f;
    private int currentFrame = 0;
    private float tiempoAturdida = 0f;
    public bool estaSiendoObservada { get; private set; } = false;
    public bool sabeDondeEstaJugador { get; private set; } = false;

    private Vector2 puntoPatrullaActual;
    private float tiempoCambioPatrulla = 0f;
    private float tiempoAtascada = 0f;
    private Vector2 ultimaPosicion;
    private int indicePatrulla = 0;

    // Circuito completo de patrulla que recorre todo el museo: hace salir a la momia de la Bóveda al pasillo
    // y la conduce por todas las salas, alternando tramos del pasillo troncal central
    private static readonly Vector2[] PuntosPatrullaMuseo = new Vector2[]
    {
        new Vector2(0f, 1.5f),     // Pasillo Central frente a Gran Bóveda (la hace salir de inmediato)
        new Vector2(-8f, 1.5f),    // Pasillo Central rumbo al Oeste
        new Vector2(-20f, 1.8f),   // Pasillo frente a Oficina Noroeste
        new Vector2(-20f, 13.5f),  // Interior Oficina Noroeste
        new Vector2(-20f, 1.8f),   // Salida de Oficina al Pasillo
        new Vector2(-20f, -1.8f),  // Pasillo frente a Cripta Sudoeste
        new Vector2(-20f, -13.5f), // Interior Cripta Sudoeste
        new Vector2(-20f, -1.8f),  // Salida de Cripta al Pasillo
        new Vector2(0f, -1.8f),    // Pasillo frente a Santuario Sur
        new Vector2(0f, -13.5f),   // Interior Santuario Sur
        new Vector2(0f, -1.8f),    // Salida de Santuario al Pasillo
        new Vector2(20f, -1.8f),   // Pasillo frente a Almacén Sudeste
        new Vector2(20f, -13.5f),  // Interior Almacén Sudeste
        new Vector2(20f, -1.8f),   // Salida de Almacén al Pasillo
        new Vector2(20f, 1.8f),    // Pasillo frente a Galería Noreste
        new Vector2(20f, 13.5f),   // Interior Galería Noreste
        new Vector2(20f, 1.8f),    // Salida de Galería al Pasillo
        new Vector2(8f, 1.5f)      // Pasillo Central rumbo al centro
    };

    [Header("Efectos de Audio de la Momia")]
    private AudioSource audioSource;
    private AudioSource shuffleSource;
    private AudioClip shuffleSound;
    private AudioClip groanSound;
    private AudioClip aggressiveGroanSound;
    private AudioClip attackSound;

    private float timerSonidoMovimiento = 0f;
    private float timerProximoGruñido = 3.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        boxCol = GetComponent<BoxCollider2D>();

        if (boxCol != null)
        {
            // Colisionador ajustado a los pies y tren inferior para respetar muros y vitrinas
            boxCol.size = new Vector2(0.65f, 0.95f);
            boxCol.offset = new Vector2(0f, -0.4f);
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.mass = 50000f; // Masa inamovible colosal: el jugador jamás podrá empujarla
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        Transform existingSh = transform.Find("Momia_ShuffleSource");
        if (existingSh != null)
        {
            shuffleSource = existingSh.GetComponent<AudioSource>();
        }
        else
        {
            GameObject shObj = new GameObject("Momia_ShuffleSource");
            shObj.transform.SetParent(transform, false);
            shuffleSource = shObj.AddComponent<AudioSource>();
        }
        if (shuffleSource != null)
        {
            shuffleSource.playOnAwake = false;
            shuffleSource.spatialBlend = 0f;
        }

        InicializarAudiosMomia();

        if (walkSprites == null || walkSprites.Length == 0)
        {
            walkSprites = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                walkSprites[i] = Resources.Load<Sprite>("Momia/momia_walk_" + i);
            }
        }
        if (idleSprite == null) idleSprite = Resources.Load<Sprite>("Momia/momia_idle");
        if (attackSprite == null) attackSprite = Resources.Load<Sprite>("Momia/momia_attack");

        vidaActual = vidaMaxima;
        velocidadPatrulla = Mathf.Max(velocidadPatrulla, 4.2f);
        velocidadAcecho = Mathf.Max(velocidadAcecho, 5.8f);
        indicePatrulla = 0;
        puntoPatrullaActual = new Vector2(0f, 0f); // Centro del pasillo principal: sale de inmediato de la Gran Bóveda
    }

    void Start()
    {
        BuscarJugador();
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

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, museoMinX, museoMaxX);
        pos.y = Mathf.Clamp(pos.y, museoMinY, museoMaxY);
        transform.position = pos;

        if (playerScript != null && playerScript.EstaMuerto)
        {
            Detenerse();
            return;
        }

        if (tiempoAturdida > 0f)
        {
            tiempoAturdida -= Time.deltaTime;
            Detenerse();
            if (sr != null) sr.color = Color.red;
            return;
        }

        // Regla 1: Congelarse si es observada de frente
        estaSiendoObservada = ComprobarSiJugadorMira();

        if (estaSiendoObservada)
        {
            Detenerse();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeAll; // Bloqueo físico absoluto: el jugador no puede empujarla
            }
            if (sr != null && idleSprite != null) sr.sprite = idleSprite;
            if (sr != null) sr.color = new Color(0.85f, 0.85f, 0.95f, 1f);
            return;
        }

        // Si no está siendo observada, permitir traslación manteniendo rotación congelada
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (sr != null) sr.color = Color.white;

        ActualizarDeteccionJugador();

        // Gruñidos periódicos tenebrosos que resuenan por los pasillos del museo
        if (playerScript != null && !playerScript.EstaMuerto && gameObject.activeInHierarchy)
        {
            timerProximoGruñido -= Time.deltaTime;
            if (timerProximoGruñido <= 0f)
            {
                timerProximoGruñido = sabeDondeEstaJugador ? Random.Range(3.0f, 5.2f) : Random.Range(5.5f, 9.5f);
                EmitirGruñido(sabeDondeEstaJugador);
            }
        }

        if (sabeDondeEstaJugador && playerTransform != null)
        {
            PerseguirJugador();
        }
        else
        {
            PatrullarColosalMuseo();
        }
    }

    private void LateUpdate()
    {
        if (sr != null)
        {
            // Profundidad Top-Down 2.5D con baseline positivo para no quedar bajo el suelo
            float footY = transform.position.y - 1.0f;
            sr.sortingOrder = 500 + Mathf.RoundToInt(-footY * 10f);
        }
    }

    private void ActualizarDeteccionJugador()
    {
        if (playerTransform == null)
        {
            sabeDondeEstaJugador = false;
            return;
        }

        float distancia = Vector2.Distance(transform.position, playerTransform.position);

        if (!sabeDondeEstaJugador)
        {
            if (distancia <= distanciaVision)
            {
                sabeDondeEstaJugador = true;
            }
        }
        else
        {
            if (distancia > distanciaPerdida)
            {
                sabeDondeEstaJugador = false;
                EscogerNuevoPuntoPatrulla();
            }
        }
    }

    private void PatrullarColosalMuseo()
    {
        tiempoCambioPatrulla += Time.deltaTime;

        // Detector dinámico de atasco: si casi no se ha desplazado en 1.1s, cambiar de rumbo de inmediato
        if (Vector2.Distance(transform.position, ultimaPosicion) < 0.15f)
        {
            tiempoAtascada += Time.deltaTime;
        }
        else
        {
            tiempoAtascada = 0f;
            ultimaPosicion = transform.position;
        }

        // Cambiar de objetivo de patrulla si llegó, si pasó tiempo suficiente o si quedó atascada
        if (tiempoCambioPatrulla >= 5.5f || tiempoAtascada >= 1.0f || Vector2.Distance(transform.position, puntoPatrullaActual) < 1.4f)
        {
            EscogerNuevoPuntoPatrulla();
        }

        Vector2 objetivo = CalcularPuntoHaciaObjetivo(puntoPatrullaActual);
        Vector2 dir = (objetivo - (Vector2)transform.position).normalized;

        if (sr != null)
        {
            if (dir.x > 0.1f) sr.flipX = false;
            else if (dir.x < -0.1f) sr.flipX = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = dir * velocidadPatrulla;
        }
        AnimarCaminar();
    }

    private void EscogerNuevoPuntoPatrulla()
    {
        tiempoCambioPatrulla = 0f;
        tiempoAtascada = 0f;
        ultimaPosicion = transform.position;

        // Avanzar por el circuito de salas del museo para que la momia explore todo el edificio
        if (PuntosPatrullaMuseo != null && PuntosPatrullaMuseo.Length > 0)
        {
            indicePatrulla = (indicePatrulla + 1) % PuntosPatrullaMuseo.Length;
            Vector2 basePos = PuntosPatrullaMuseo[indicePatrulla];
            // Ligera variación para trayectorias orgánicas no idénticas
            float jitterX = Random.Range(-0.5f, 0.5f);
            float jitterY = Random.Range(-0.5f, 0.5f);
            puntoPatrullaActual = new Vector2(basePos.x + jitterX, basePos.y + jitterY);
        }
        else
        {
            float rx = Random.Range(-20f, 20f);
            float ry = Random.Range(-13f, 13f);
            puntoPatrullaActual = new Vector2(rx, ry);
        }
    }

    private bool ComprobarSiJugadorMira()
    {
        if (playerTransform == null || playerScript == null) return false;

        Vector2 vecHaciaMomia = (transform.position - playerTransform.position);
        float distancia = vecHaciaMomia.magnitude;

        // Si está a más de 16 unidades, no se congela
        if (distancia > 16f) return false;

        Vector2 dirHaciaMomia = vecHaciaMomia.normalized;
        Vector2 direccionJugador = playerScript.facingDirection;

        // Debe estar dentro del cono visual frontal del vigilante
        float dot = Vector2.Dot(direccionJugador, dirHaciaMomia);
        if (dot <= 0.40f) return false;

        // Comprobación de oclusión por muros: raycast desde fuera del collider del vigilante hacia la momia
        Vector2 origenOjos = (Vector2)playerTransform.position + dirHaciaMomia * 0.6f;
        float distRestante = Mathf.Max(0.1f, distancia - 0.6f);
        RaycastHit2D hit = Physics2D.Raycast(origenOjos, dirHaciaMomia, distRestante);
        if (hit.collider != null && hit.collider.gameObject != gameObject && hit.collider.gameObject != playerTransform.gameObject && !hit.collider.isTrigger)
        {
            // Hay un muro u obstáculo bloqueando la línea de visión
            return false;
        }

        return true;
    }

    private void PerseguirJugador()
    {
        if (playerTransform == null) return;

        float distancia = Vector2.Distance(playerTransform.position, transform.position);

        if (distancia <= rangoAtaque)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (sr != null && attackSprite != null) sr.sprite = attackSprite;

            // Rugido feroz al atacar de cerca
            if (audioSource != null && attackSound != null && !audioSource.isPlaying)
            {
                float distAtk = Vector2.Distance(transform.position, playerTransform.position);
                float vol = Mathf.Clamp01(1f - (distAtk / 16f)) * 0.95f;
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(attackSound, vol);
            }

            if (playerScript != null)
            {
                playerScript.RecibirDanio(danioPorSegundo * Time.deltaTime, "momia");
            }
        }
        else
        {
            if (Vector2.Distance(transform.position, ultimaPosicion) < 0.12f)
            {
                tiempoAtascada += Time.deltaTime;
            }
            else
            {
                tiempoAtascada = 0f;
                ultimaPosicion = transform.position;
            }

            // Navegación inteligente respetando muros: si el jugador está en otra sala/pasillo,
            // la momia se dirige primero hacia el vano de la puerta en lugar de chocar contra el muro
            Vector2 objetivo = CalcularPuntoHaciaObjetivo(playerTransform.position);

            if (tiempoAtascada >= 1.2f)
            {
                // Si la momia se frena un momento por un obstáculo o esquina, dirigirse de inmediato al centro del pasillo
                objetivo = new Vector2(0f, 0f);
            }

            Vector2 dir = (objetivo - (Vector2)transform.position).normalized;

            if (sr != null)
            {
                if (dir.x > 0.1f) sr.flipX = false;
                else if (dir.x < -0.1f) sr.flipX = true;
            }

            if (rb != null)
            {
                rb.linearVelocity = dir * velocidadAcecho;
            }
            AnimarCaminar();
        }
    }

    /// <summary>
    /// Identifica la zona espacial del museo (0: Pasillo Troncal, 1: Oficina, 2: Gran Bóveda, 3: Galería, 4: Cripta, 5: Santuario, 6: Almacén).
    /// </summary>
    private int ObtenerZona(Vector2 pos)
    {
        if (pos.y > 4.2f)
        {
            if (pos.x < -10f) return 1; // Oficina Noroeste
            if (pos.x > 10f) return 3;  // Galería Noreste
            return 2;                   // Gran Bóveda Norte
        }
        else if (pos.y < -4.2f)
        {
            if (pos.x < -10f) return 4; // Cripta Sudoeste
            if (pos.x > 10f) return 6;  // Almacén Sudeste
            return 5;                   // Santuario Sur
        }
        return 0; // Pasillo Troncal Central
    }

    private void ObtenerDatosPuerta(int zona, out float doorX, out float hallY, out float roomY)
    {
        switch (zona)
        {
            case 1: doorX = -20f; hallY = 1.5f; roomY = 6.0f; break; // Oficina Noroeste
            case 2: doorX = 0f;   hallY = 1.5f; roomY = 6.0f; break; // Gran Bóveda Norte
            case 3: doorX = 20f;  hallY = 1.5f; roomY = 6.0f; break; // Galería Noreste
            case 4: doorX = -20f; hallY = -1.5f; roomY = -6.0f; break; // Cripta Sudoeste
            case 5: doorX = 0f;   hallY = -1.5f; roomY = -6.0f; break; // Santuario Sur
            case 6: doorX = 20f;  hallY = -1.5f; roomY = -6.0f; break; // Almacén Sudeste
            default: doorX = 0f;  hallY = 0f;    roomY = 0f; break;
        }
    }

    /// <summary>
    /// Calcula el punto de paso hacia el objetivo respetando los muros y usando las puertas del museo.
    /// Garantiza tránsito vertical libre y sin atascos entre salas y pasillos.
    /// </summary>
    private Vector2 CalcularPuntoHaciaObjetivo(Vector2 destinoFinal)
    {
        Vector2 miPos = transform.position;
        int miZona = ObtenerZona(miPos);
        int zonaDest = ObtenerZona(destinoFinal);

        // Si ya estamos en la misma zona (ambos en la misma sala o ambos en el pasillo troncal), ir directo
        if (miZona == zonaDest)
        {
            return destinoFinal;
        }

        // Si la momia está dentro de una sala (Zonas 1-6), su prioridad absoluta es SALIR al pasillo
        if (miZona > 0)
        {
            ObtenerDatosPuerta(miZona, out float doorX, out float hallY, out float roomY);
            bool esNorte = (miZona <= 3);

            if (esNorte)
            {
                // Si aún está dentro de la sala (Y > 5.2f), alinearse primero en X con la puerta
                if (miPos.y > 5.2f)
                {
                    if (Mathf.Abs(miPos.x - doorX) > 0.5f)
                    {
                        return new Vector2(doorX, Mathf.Max(6.0f, miPos.y));
                    }
                }
                // Atravesar directo hacia el pasillo
                return new Vector2(doorX, hallY);
            }
            else
            {
                if (miPos.y < -5.2f)
                {
                    if (Mathf.Abs(miPos.x - doorX) > 0.5f)
                    {
                        return new Vector2(doorX, Mathf.Min(-6.0f, miPos.y));
                    }
                }
                return new Vector2(doorX, hallY);
            }
        }

        // Si la momia está en el Pasillo Troncal (Zona 0) y el destino está dentro de una sala
        if (zonaDest > 0)
        {
            ObtenerDatosPuerta(zonaDest, out float targetDoorX, out float targetHallY, out float targetRoomY);

            // 1. Desplazarse por el pasillo hasta alinearse frente a la puerta
            if (Mathf.Abs(miPos.x - targetDoorX) > 0.6f)
            {
                return new Vector2(targetDoorX, targetHallY);
            }

            // 2. Atravesar verticalmente el vano hacia el interior de la sala
            return new Vector2(targetDoorX, targetRoomY);
        }

        return destinoFinal;
    }

    /// <summary>
    /// Mantiene la dirección limpia hacia el objetivo.
    /// </summary>
    private Vector2 EvitarObstaculos(Vector2 dir)
    {
        return dir;
    }

    private void Detenerse()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        currentFrame = 0;
        animTimer = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (rb != null && estaSiendoObservada)
            {
                rb.linearVelocity = Vector2.zero;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (rb != null && estaSiendoObservada)
            {
                rb.linearVelocity = Vector2.zero;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            if (playerScript != null)
            {
                playerScript.RecibirDanio(danioPorSegundo * Time.deltaTime, "momia");
            }
        }
    }

    private void AnimarCaminar()
    {
        if (sr == null) return;

        // Sonido rítmico de pasos y vendajes secos arrastrándose por el suelo del museo
        timerSonidoMovimiento += Time.deltaTime;
        if (timerSonidoMovimiento >= 0.36f)
        {
            timerSonidoMovimiento = 0f;
            ReproducirPasoArrastre();
        }

        animTimer += Time.deltaTime;
        if (animTimer >= 0.12f)
        {
            animTimer = 0f;
            if (walkSprites != null && walkSprites.Length > 0)
            {
                currentFrame = (currentFrame + 1) % walkSprites.Length;
                if (walkSprites[currentFrame] != null)
                {
                    sr.sprite = walkSprites[currentFrame];
                }
            }
        }
    }

    private void ReproducirPasoArrastre()
    {
        if (shuffleSource == null || shuffleSound == null) return;
        float dist = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 25f;
        // Volumen decreciente con la distancia (audible hasta 20 unidades)
        float factorDistancia = Mathf.Clamp01(1f - (dist / 20f));
        if (factorDistancia <= 0.02f) return;

        shuffleSource.pitch = Random.Range(0.90f, 1.10f);
        shuffleSource.PlayOneShot(shuffleSound, factorDistancia * 0.70f);
    }

    private void EmitirGruñido(bool agresivo)
    {
        if (audioSource == null) return;
        float dist = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 25f;
        // Los lamentos y gruñidos retumban más lejos: audibles hasta 26 unidades
        float factorDistancia = Mathf.Clamp01(1f - (dist / 26f));
        if (factorDistancia <= 0.04f) return;

        AudioClip clip = agresivo ? aggressiveGroanSound : groanSound;
        if (clip != null)
        {
            audioSource.pitch = Random.Range(0.88f, 1.05f);
            audioSource.PlayOneShot(clip, factorDistancia * 0.85f);
        }
    }

    public void RecibirDanio(float danio)
    {
        vidaActual -= danio;
        tiempoAturdida = 0.5f;

        if (vidaActual <= 0f)
        {
            vidaActual = 0f;
            gameObject.SetActive(false);
        }
    }

    private void InicializarAudiosMomia()
    {
        shuffleSound = GenerarSonidoArrastreProcedural();
        groanSound = GenerarSonidoGruñidoProcedural(false);
        aggressiveGroanSound = GenerarSonidoGruñidoProcedural(true);
        attackSound = GenerarSonidoAtaqueMomiaProcedural();
    }

    private AudioClip GenerarSonidoArrastreProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;

            // Fricción áspera de lino y polvo sobre baldosas
            float noise = ((float)new System.Random(i).NextDouble() * 2f - 1f);
            float scrape = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.35f;
            float env = Mathf.Sin(progress * Mathf.PI);

            samples[i] = Mathf.Clamp((noise * 0.55f + scrape) * env * Mathf.Exp(-t * 6f), -1f, 1f) * 0.8f;
        }

        AudioClip clip = AudioClip.Create("Momia_Arrastre", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoGruñidoProcedural(bool agresivo)
    {
        int sampleRate = 44100;
        float duration = agresivo ? 1.10f : 0.90f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float baseFreq = agresivo ? 80f : 62f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duration;

            // Modulación de frecuencia gutural (vibrato de ultratumba)
            float fm = Mathf.Sin(2f * Mathf.PI * 18f * t) * (agresivo ? 16f : 11f);
            float freq = baseFreq + fm;

            float baseWave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.25f;
            float throat = Mathf.Sin(2f * Mathf.PI * (agresivo ? 820f : 680f) * t) * Mathf.Exp(-Mathf.Pow(prog - 0.4f, 2f) * 12f) * 0.22f;
            float raspNoise = ((float)new System.Random(i).NextDouble() * 2f - 1f) * (agresivo ? 0.22f : 0.14f);

            float env = Mathf.Sin(prog * Mathf.PI) * (1f - prog * 0.25f);
            samples[i] = Mathf.Clamp((baseWave + throat + raspNoise) * env, -1f, 1f) * 0.85f;
        }

        AudioClip clip = AudioClip.Create(agresivo ? "Momia_Gruñido_Agresivo" : "Momia_Gruñido", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoAtaqueMomiaProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.65f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duration;

            // Siseo agresivo y mordisco gutural
            float hiss = ((float)new System.Random(i).NextDouble() * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.45f;
            float growl = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 60f, prog) * t) * 0.55f;
            float env = Mathf.Sin(prog * Mathf.PI);

            samples[i] = Mathf.Clamp((hiss + growl) * env, -1f, 1f) * 0.90f;
        }

        AudioClip clip = AudioClip.Create("Momia_Ataque_Siseo", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
