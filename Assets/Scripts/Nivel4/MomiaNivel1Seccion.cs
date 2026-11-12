using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Incorporación fiel de la Momia del Nivel 1 ("La Que Camina") al Nivel 4,
/// confinada estrictamente a su sección asignada del panteón mientras Carmen
/// (La Momia Encadenada) recorre todo el mapa.
/// - Mecánica original del Nivel 1: si la vigilante la mira de frente sin muros
///   de por medio, se congela como estatua; al darle la espalda o perder línea de vista, avanza y acecha.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MomiaNivel1Seccion : MonoBehaviour
{
    [Header("Sección Confinada del Panteón")]
    public string nombreSeccion = "Sector Oeste (Mausoleo Leroy)";
    public Vector2 zonaMin = new Vector2(-36f, -14f);
    public Vector2 zonaMax = new Vector2(-10f, 15f);
    public Vector2[] puntosPatrulla;

    [Header("Horario de Aparición (Por Secciones)")]
    public int horaAparicion = 4;
    public int minutoAparicion = 0;
    public bool HaDespertado { get; private set; } = false;

    [Header("Estadísticas y Detección (Fiel al Nivel 1)")]
    public float velocidadPatrulla = 2.85f;
    public float velocidadAcecho = 4.25f;
    public float distanciaVision = 9.5f;
    public float distanciaPerdida = 13.5f;
    public float rangoAtaque = 1.65f;
    public float danioAtaque = 20f;
    public float cooldownAtaque = 1.45f;

    [Header("Estado")]
    public bool Petrificada = false;
    public bool estaSiendoObservada { get; private set; } = false;
    public bool sabeDondeEstaJugador { get; private set; } = false;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private SpriteRenderer sr;
    private SpriteRenderer srSombraPies;
    private Transform playerTransform;
    private float progresoAparicion = 0f;

    private Sprite[] walkSprites;
    private Sprite idleSprite;
    private Sprite attackSprite;

    private int indicePatrulla = 0;
    private Vector2 destinoActual;
    private float animTimer = 0f;
    private int currentFrame = 0;
    private float timerAtaque = 0f;
    private float timerRepelerFuego = 0f;
    private Vector2 centroFuegoRepulsor = Vector2.zero;
    private float timerAlejamientoEscondite = 0f;
    private float timerPermanenciaHabitacion = 0f;
    private Vector2 posEvitarEscondite = Vector2.zero;

    // Anti-atasco
    private Vector2 ultimaPosicion;
    private float timerCheckStuck = 0f;
    private float timerAtascada = 0f;
    private float timerDesvio = 0f;
    private Vector2 dirDesvio = Vector2.zero;

    // Audio fiel al Nivel 1
    private AudioSource audioSource;
    private AudioSource shuffleSource;
    private AudioClip shuffleSound;
    private AudioClip groanSound;
    private AudioClip aggressiveGroanSound;
    private AudioClip attackSound;
    private float timerSonidoMovimiento = 0f;
    private float timerProximoGruñido = 4.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        boxCol = GetComponent<BoxCollider2D>();

        transform.localScale = new Vector3(1.55f, 1.55f, 1f);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.mass = 50000f;

        boxCol.size = new Vector2(0.36f, 0.32f);
        boxCol.offset = new Vector2(0f, -0.48f);
        boxCol.edgeRadius = 0.05f;
        boxCol.isTrigger = false;

        CargarSprites();
        ConfigurarSombraPies();
        ConfigurarAudios();
        InicializarPatrullaPorDefecto();

        // Permanece latente y oculta hasta que el reloj alcance la hora asignada (04:00 AM)
        HaDespertado = false;
        progresoAparicion = 0f;
        if (sr != null)
        {
            sr.enabled = false;
            sr.color = new Color(1f, 1f, 1f, 0f);
        }
        if (srSombraPies != null) srSombraPies.enabled = false;
        boxCol.enabled = false;
        rb.simulated = false;
    }

    private void Start()
    {
        if (VigilanteNivel4.Instance != null)
        {
            playerTransform = VigilanteNivel4.Instance.transform;
        }
        ultimaPosicion = transform.position;
    }

    private void CargarSprites()
    {
        walkSprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            walkSprites[i] = Resources.Load<Sprite>("Momia/momia_walk_" + i);
        }
        idleSprite = Resources.Load<Sprite>("Momia/momia_idle");
        attackSprite = Resources.Load<Sprite>("Momia/momia_attack");

        if (sr != null)
        {
            sr.sprite = idleSprite != null ? idleSprite : walkSprites[0];
            sr.sortingOrder = 12;
            sr.color = Color.white;
        }
    }

    private void ConfigurarSombraPies()
    {
        int w = 44, h = 18;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - w * 0.5f) / (w * 0.5f);
                float ny = (y - h * 0.5f) / (h * 0.5f);
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a * a * 0.62f));
            }
        }
        tex.Apply();

        GameObject sombra = new GameObject("SombraPiesMomiaN1");
        sombra.transform.SetParent(transform, false);
        sombra.transform.localPosition = new Vector3(0f, -0.92f, 0f);
        srSombraPies = sombra.AddComponent<SpriteRenderer>();
        srSombraPies.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        srSombraPies.sortingOrder = 4;
        srSombraPies.enabled = false;
    }

    private void InicializarPatrullaPorDefecto()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            puntosPatrulla = new Vector2[]
            {
                new Vector2(-18f, 0f),
                new Vector2(-13f, 5f),
                new Vector2(-18f, 13f),
                new Vector2(-12f, 10f),
                new Vector2(-13f, -6f),
                new Vector2(-22f, -11f),
                new Vector2(-28f, -1f)
            };
        }
        indicePatrulla = 0;
        destinoActual = puntosPatrulla[0];
    }

    private void Update()
    {
        if (Petrificada || Time.timeScale == 0f || (Nivel4Manager.Instance != null && Nivel4Manager.Instance.enInteriorCapilla))
        {
            if (rb != null && rb.simulated) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (playerTransform == null && VigilanteNivel4.Instance != null)
        {
            playerTransform = VigilanteNivel4.Instance.transform;
        }

        if (!HaDespertado)
        {
            int minutosObjetivo = horaAparicion * 60 + minutoAparicion;
            if (Nivel4Manager.Instance != null && Nivel4Manager.Instance.TotalMinutosActual >= minutosObjetivo)
            {
                DespertarEnSeccion();
            }
            else
            {
                return;
            }
        }

        if (progresoAparicion < 1f)
        {
            progresoAparicion = Mathf.MoveTowards(progresoAparicion, 1f, Time.deltaTime / 0.8f);
        }

        if (timerAtaque > 0f) timerAtaque -= Time.deltaTime;
        if (timerRepelerFuego > 0f) timerRepelerFuego -= Time.deltaTime;
        if (timerAlejamientoEscondite > 0f) timerAlejamientoEscondite -= Time.deltaTime;
        if (timerDesvio > 0f) timerDesvio -= Time.deltaTime;

        // Mantener confinada dentro de su sección del mapa
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, zonaMin.x, zonaMax.x);
        pos.y = Mathf.Clamp(pos.y, zonaMin.y, zonaMax.y);
        transform.position = pos;

        // Si fue repelida por un Cirio Bendito o debe alejarse de un escondite/habitación, salir hacia el punto opuesto
        bool ocultaAhora = VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta;
        if (timerRepelerFuego > 0f || (timerAlejamientoEscondite > 0f && (ocultaAhora || Vector2.Distance(transform.position, posEvitarEscondite) < 10.5f)))
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (sr != null) sr.color = new Color(1f, 1f, 1f, progresoAparicion);
            MoverConEvasion(destinoActual, velocidadAcecho * 0.92f);
            return;
        }

        // Evitar que se quede rondando dentro del Mausoleo Leroy o frente a su entrada si perdió al jugador
        bool enMausoleo = transform.position.x >= -35.5f && transform.position.x <= -20.5f && transform.position.y >= 0.2f && transform.position.y <= 12f;
        if (enMausoleo && (!sabeDondeEstaJugador || ocultaAhora))
        {
            timerPermanenciaHabitacion += Time.deltaTime;
            if (timerPermanenciaHabitacion >= 2.2f)
            {
                timerPermanenciaHabitacion = 0f;
                AlejarDePuntoEnSeccion(new Vector2(-28f, 6f), 8.5f);
            }
        }
        else
        {
            timerPermanenciaHabitacion = 0f;
        }

        // Regla fiel del Nivel 1: Se congela completamente si la vigilante la mira de frente sin obstáculos
        estaSiendoObservada = ComprobarSiJugadorMira();
        if (estaSiendoObservada)
        {
            DetenerseCongelada();
            return;
        }

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (sr != null) sr.color = new Color(1f, 1f, 1f, progresoAparicion);

        ActualizarDeteccionEnSeccion();

        // Gruñidos guturales del Nivel 1
        timerProximoGruñido -= Time.deltaTime;
        if (timerProximoGruñido <= 0f)
        {
            timerProximoGruñido = sabeDondeEstaJugador ? Random.Range(3.2f, 5.5f) : Random.Range(6.0f, 10.0f);
            EmitirGruñido(sabeDondeEstaJugador);
        }

        if (sabeDondeEstaJugador && playerTransform != null)
        {
            PerseguirEnSeccion();
        }
        else
        {
            PatrullarEnSeccion();
        }
    }

    private void DespertarEnSeccion()
    {
        HaDespertado = true;
        progresoAparicion = 0f;

        // Si la vigilante está justo encima del punto inicial, emerger en el punto más distante dentro de su sección
        if (playerTransform != null && puntosPatrulla != null && puntosPatrulla.Length > 0 &&
            Vector2.Distance(transform.position, playerTransform.position) < 4.5f)
        {
            Vector2 mejorPunto = puntosPatrulla[0];
            float maxDist = -1f;
            for (int i = 0; i < puntosPatrulla.Length; i++)
            {
                float d = Vector2.Distance(puntosPatrulla[i], playerTransform.position);
                if (d > maxDist)
                {
                    maxDist = d;
                    mejorPunto = puntosPatrulla[i];
                }
            }
            transform.position = new Vector3(mejorPunto.x, mejorPunto.y, 0f);
        }

        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        boxCol.enabled = true;
        if (sr != null)
        {
            sr.enabled = true;
            sr.color = new Color(1f, 1f, 1f, 0f);
        }
        if (srSombraPies != null) srSombraPies.enabled = true;
        ultimaPosicion = transform.position;

        EmitirGruñido(false);
        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.SacudirPantalla(0.45f, 10f);
        }
    }

    public void NotificarJugadorOculto(Vector2 posEscondite)
    {
        if (!HaDespertado || Petrificada) return;
        if (Vector2.Distance(transform.position, posEscondite) <= 15f || sabeDondeEstaJugador)
        {
            AlejarDePuntoEnSeccion(posEscondite, 10.5f);
        }
    }

    private void AlejarDePuntoEnSeccion(Vector2 puntoEvitar, float duracion)
    {
        sabeDondeEstaJugador = false;
        posEvitarEscondite = puntoEvitar;
        timerAlejamientoEscondite = duracion;

        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
        {
            int mejorFueraHab = -1;
            float maxDFuera = -1f;
            int mejorGeneral = indicePatrulla;
            float maxD = -1f;
            for (int i = 0; i < puntosPatrulla.Length; i++)
            {
                Vector2 p = puntosPatrulla[i];
                float d = Vector2.Distance(p, puntoEvitar);
                if (d > maxD)
                {
                    maxD = d;
                    mejorGeneral = i;
                }
                bool fueraMausoleo = !(p.x >= -35.5f && p.x <= -20.5f && p.y >= 0.2f && p.y <= 12f);
                if (fueraMausoleo && d > maxDFuera)
                {
                    maxDFuera = d;
                    mejorFueraHab = i;
                }
            }
            int elegido = (mejorFueraHab >= 0) ? mejorFueraHab : mejorGeneral;
            indicePatrulla = elegido;
            destinoActual = puntosPatrulla[elegido];
        }
    }

    private bool EstaDentroDeSeccion(Vector2 punto)
    {
        return punto.x >= zonaMin.x && punto.x <= zonaMax.x &&
               punto.y >= zonaMin.y && punto.y <= zonaMax.y;
    }

    private void ActualizarDeteccionEnSeccion()
    {
        if (playerTransform == null || (VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta))
        {
            sabeDondeEstaJugador = false;
            return;
        }

        Vector2 posPlayer = playerTransform.position;
        if (!EstaDentroDeSeccion(posPlayer))
        {
            if (sabeDondeEstaJugador)
            {
                sabeDondeEstaJugador = false;
                destinoActual = puntosPatrulla[indicePatrulla];
            }
            return;
        }

        float distancia = Vector2.Distance(transform.position, posPlayer);
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
                destinoActual = puntosPatrulla[indicePatrulla];
            }
        }
    }

    private bool ComprobarSiJugadorMira()
    {
        if (playerTransform == null || VigilanteNivel4.Instance == null) return false;
        if (VigilanteNivel4.Instance.EstaOculta) return false;

        Vector2 vecHaciaMomia = (Vector2)transform.position - (Vector2)playerTransform.position;
        float distancia = vecHaciaMomia.magnitude;
        if (distancia > 16f || distancia < 0.05f) return false;

        Vector2 dirHaciaMomia = vecHaciaMomia / distancia;
        Vector2 dirJugador = VigilanteNivel4.Instance.DireccionMirada;
        if (dirJugador.sqrMagnitude < 0.01f)
        {
            SpriteRenderer srPlayer = playerTransform.GetComponent<SpriteRenderer>();
            dirJugador = (srPlayer != null && srPlayer.flipX) ? Vector2.left : Vector2.right;
        }

        if (Vector2.Dot(dirJugador.normalized, dirHaciaMomia) <= 0.26f) return false;

        Vector2 origenOjos = (Vector2)playerTransform.position + dirHaciaMomia * 0.55f;
        float distRestante = Mathf.Max(0.1f, distancia - 0.55f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origenOjos, dirHaciaMomia, distRestante);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider != null && !h.collider.isTrigger && h.collider.gameObject != gameObject && h.collider.gameObject != playerTransform.gameObject)
            {
                return false;
            }
        }

        return true;
    }

    private void DetenerseCongelada()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (shuffleSource != null && shuffleSource.isPlaying)
        {
            shuffleSource.Stop();
        }
        if (sr != null)
        {
            if (idleSprite != null) sr.sprite = idleSprite;
            sr.color = new Color(0.85f, 0.85f, 0.95f, progresoAparicion);
        }
        currentFrame = 0;
        animTimer = 0f;
    }

    private void PatrullarEnSeccion()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        destinoActual = puntosPatrulla[indicePatrulla];
        if (Vector2.Distance(transform.position, destinoActual) <= 1.25f)
        {
            indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
            destinoActual = puntosPatrulla[indicePatrulla];
        }

        MoverConEvasion(destinoActual, velocidadPatrulla);
    }

    private void PerseguirEnSeccion()
    {
        Vector2 posObjetivo = playerTransform.position;
        posObjetivo.x = Mathf.Clamp(posObjetivo.x, zonaMin.x, zonaMax.x);
        posObjetivo.y = Mathf.Clamp(posObjetivo.y, zonaMin.y, zonaMax.y);

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= rangoAtaque)
        {
            rb.linearVelocity = Vector2.zero;
            if (sr != null && attackSprite != null) sr.sprite = attackSprite;
            IntentarAtacarJugador();
            return;
        }

        MoverConEvasion(posObjetivo, velocidadAcecho);
    }

    private void IntentarAtacarJugador()
    {
        if (timerAtaque > 0f || VigilanteNivel4.Instance == null || VigilanteNivel4.Instance.EstaOculta) return;
        timerAtaque = cooldownAtaque;

        if (audioSource != null && attackSound != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(attackSound, 0.85f);
        }

        VigilanteNivel4.Instance.RecibirDanio(danioAtaque, "Momia1");
        if (VigilanteNivel4.Instance.vidaActual <= 0f)
        {
            if (Nivel4Manager.Instance != null) Nivel4Manager.Instance.ActivarJumpscareGameOver("Momia1");
        }
        else if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.MostrarAviso($"¡LA MOMIA DEL MUSEO TE ALCANZÓ EN SU SECTOR! (❤ {Mathf.CeilToInt(VigilanteNivel4.Instance.vidaActual)})", 2.2f);
            Nivel4Manager.Instance.SacudirPantalla(0.42f, 14f);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!HaDespertado || Petrificada || estaSiendoObservada || timerRepelerFuego > 0f) return;
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<VigilanteNivel4>() != null)
        {
            if (sr != null && attackSprite != null) sr.sprite = attackSprite;
            IntentarAtacarJugador();
        }
    }

    private float ProbarDistanciaLibre(Vector2 origen, Vector2 dirPrueba, float maxDist, out Vector2 normalChoque)
    {
        normalChoque = Vector2.zero;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origen, 0.28f, dirPrueba, maxDist);
        float menorDist = maxDist;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject && !hit.collider.CompareTag("Player"))
            {
                if (hit.distance <= 0.02f && Vector2.Dot(dirPrueba, hit.normal) >= -0.05f) continue;
                if (hit.distance < menorDist)
                {
                    menorDist = hit.distance;
                    normalChoque = hit.normal;
                }
            }
        }
        return menorDist;
    }

    private Vector2 ElegirDesvio(Vector2 origen, Vector2 dirDeseada, Vector2 normalChoque)
    {
        Vector2 perpA = normalChoque != Vector2.zero ? Vector2.Perpendicular(normalChoque).normalized : Vector2.Perpendicular(dirDeseada).normalized;
        Vector2 perpB = -perpA;
        if (Vector2.Dot(perpB, dirDeseada) > Vector2.Dot(perpA, dirDeseada))
        {
            Vector2 tmp = perpA;
            perpA = perpB;
            perpB = tmp;
        }

        Vector2[] candidatas = new Vector2[]
        {
            perpA,
            perpB,
            (Quaternion.Euler(0f, 0f, 45f) * dirDeseada).normalized,
            (Quaternion.Euler(0f, 0f, -45f) * dirDeseada).normalized,
            (Quaternion.Euler(0f, 0f, 75f) * dirDeseada).normalized,
            (Quaternion.Euler(0f, 0f, -75f) * dirDeseada).normalized,
            (Quaternion.Euler(0f, 0f, 105f) * dirDeseada).normalized,
            (Quaternion.Euler(0f, 0f, -105f) * dirDeseada).normalized
        };

        Vector2 mejor = perpA;
        float mejorPuntaje = -999f;
        for (int i = 0; i < candidatas.Length; i++)
        {
            float libre = ProbarDistanciaLibre(origen, candidatas[i], 1.6f, out _);
            float alineacion = Mathf.Max(0f, Vector2.Dot(candidatas[i], dirDeseada));
            float puntaje = libre * 1.4f + alineacion * 1.2f;
            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejor = candidatas[i];
            }
        }
        if (normalChoque != Vector2.zero)
        {
            mejor = (mejor + normalChoque * 0.25f).normalized;
        }
        return mejor;
    }

    private void MoverConEvasion(Vector2 objetivo, float vel)
    {
        Vector2 pos = transform.position;
        Vector2 origenCast = boxCol != null && boxCol.enabled ? (Vector2)boxCol.bounds.center : pos;
        Vector2 dirDeseada = (objetivo - pos).normalized;
        if (dirDeseada.sqrMagnitude < 0.001f) dirDeseada = Vector2.right;

        Vector2 dir = dirDeseada;
        float distLibre = ProbarDistanciaLibre(origenCast, dirDeseada, 0.85f, out Vector2 normal);
        if (distLibre < 0.82f)
        {
            if (timerDesvio <= 0f)
            {
                dirDesvio = ElegirDesvio(origenCast, dirDeseada, normal);
                timerDesvio = 0.45f;
            }
            dir = Vector2.Lerp(dirDeseada, dirDesvio, 0.85f).normalized;
        }
        else if (timerDesvio > 0f)
        {
            dir = Vector2.Lerp(dirDesvio, dirDeseada, 0.50f).normalized;
        }

        timerCheckStuck += Time.deltaTime;
        if (timerCheckStuck >= 0.25f)
        {
            if (Vector2.Distance(pos, ultimaPosicion) < 0.10f)
            {
                timerAtascada += timerCheckStuck;
                if (timerAtascada >= 0.55f)
                {
                    dirDesvio = ElegirDesvio(origenCast, dirDeseada, Vector2.zero);
                    timerDesvio = 1.0f;
                    if (!sabeDondeEstaJugador && puntosPatrulla != null && puntosPatrulla.Length > 0)
                    {
                        indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
                        destinoActual = puntosPatrulla[indicePatrulla];
                    }
                    timerAtascada = 0f;
                }
            }
            else
            {
                timerAtascada = 0f;
            }
            ultimaPosicion = pos;
            timerCheckStuck = 0f;
        }

        rb.linearVelocity = dir * vel;
        if (sr != null)
        {
            if (dir.x > 0.08f) sr.flipX = false;
            else if (dir.x < -0.08f) sr.flipX = true;
        }

        AnimarCaminar();
    }

    private void AnimarCaminar()
    {
        if (sr == null) return;

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
                if (walkSprites[currentFrame] != null) sr.sprite = walkSprites[currentFrame];
            }
        }
    }

    public void RepelerPorFuegoBendito(Vector3 centroFuego, float radio)
    {
        if (!HaDespertado || Petrificada) return;
        centroFuegoRepulsor = centroFuego;
        sabeDondeEstaJugador = false;
        if (timerRepelerFuego <= 0f)
        {
            timerRepelerFuego = 3.2f;
            // Buscar el punto de patrulla de su sección más alejado del cirio
            if (puntosPatrulla != null && puntosPatrulla.Length > 0)
            {
                int mejor = indicePatrulla;
                float maxD = -1f;
                for (int i = 0; i < puntosPatrulla.Length; i++)
                {
                    float d = Vector2.Distance(puntosPatrulla[i], centroFuego);
                    if (d > maxD)
                    {
                        maxD = d;
                        mejor = i;
                    }
                }
                indicePatrulla = mejor;
                destinoActual = puntosPatrulla[mejor];
            }
        }
    }

    public void Petrificar()
    {
        Petrificada = true;
        sabeDondeEstaJugador = false;
        if (shuffleSource != null && shuffleSource.isPlaying)
        {
            shuffleSource.Stop();
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (sr != null)
        {
            if (idleSprite != null) sr.sprite = idleSprite;
            sr.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        }
    }

    private void ConfigurarAudios()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        GameObject shObj = new GameObject("MomiaN1_ShuffleSource");
        shObj.transform.SetParent(transform, false);
        shuffleSource = shObj.AddComponent<AudioSource>();
        shuffleSource.playOnAwake = false;
        shuffleSource.loop = true;
        shuffleSource.spatialBlend = 0f;
        shuffleSource.volume = 0f;

        shuffleSound = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMINATA_MOMIA);
        groanSound = GenerarSonidoGruñidoProcedural(false);
        aggressiveGroanSound = GenerarSonidoGruñidoProcedural(true);
        attackSound = GenerarSonidoAtaqueMomiaProcedural();
    }

    private void ReproducirPasoArrastre()
    {
        if (shuffleSource == null) return;
        if (shuffleSound == null) shuffleSound = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMINATA_MOMIA);
        if (shuffleSound == null) return;

        float dist = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 30f;
        if (dist > 20f || Time.timeScale == 0f || estaSiendoObservada || Petrificada || !HaDespertado)
        {
            if (shuffleSource.isPlaying) shuffleSource.Stop();
            return;
        }

        float factor = Mathf.Pow(Mathf.Clamp01(1f - (dist / 20f)), 1.25f);
        if (shuffleSource.clip != shuffleSound) shuffleSource.clip = shuffleSound;
        shuffleSource.pitch = sabeDondeEstaJugador ? 1.15f : 0.95f;
        shuffleSource.volume = factor * 0.85f;
        if (!shuffleSource.isPlaying) shuffleSource.Play();
    }

    private void EmitirGruñido(bool agresivo)
    {
        if (audioSource == null) return;
        float dist = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 25f;
        float factor = Mathf.Clamp01(1f - (dist / 26f));
        if (factor <= 0.04f) return;

        AudioClip clip = agresivo ? aggressiveGroanSound : groanSound;
        if (clip != null)
        {
            audioSource.pitch = Random.Range(0.88f, 1.05f);
            audioSource.PlayOneShot(clip, factor * 0.80f);
        }
    }

    private AudioClip GenerarSonidoArrastreProcedural()
    {
        int sampleRate = 22050;
        float duration = 0.22f;
        int count = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[count];
        System.Random rng = new System.Random(101);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duration;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float scrape = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.35f;
            float env = Mathf.Sin(prog * Mathf.PI);
            samples[i] = Mathf.Clamp((noise * 0.55f + scrape) * env * Mathf.Exp(-t * 6f), -1f, 1f) * 0.75f;
        }
        AudioClip clip = AudioClip.Create("MomiaN1_Arrastre", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoGruñidoProcedural(bool agresivo)
    {
        int sampleRate = 22050;
        float duration = agresivo ? 1.0f : 0.85f;
        int count = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[count];
        float baseFreq = agresivo ? 80f : 62f;
        System.Random rng = new System.Random(agresivo ? 202 : 201);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duration;
            float freq = baseFreq + Mathf.Sin(2f * Mathf.PI * 18f * t) * (agresivo ? 16f : 11f);
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.25f;
            float throat = Mathf.Sin(2f * Mathf.PI * (agresivo ? 820f : 680f) * t) * Mathf.Exp(-Mathf.Pow(prog - 0.4f, 2f) * 12f) * 0.22f;
            float rasp = ((float)rng.NextDouble() * 2f - 1f) * (agresivo ? 0.20f : 0.12f);
            float env = Mathf.Sin(prog * Mathf.PI) * (1f - prog * 0.25f);
            samples[i] = Mathf.Clamp((wave + throat + rasp) * env, -1f, 1f) * 0.80f;
        }
        AudioClip clip = AudioClip.Create(agresivo ? "MomiaN1_GrunidoAgr" : "MomiaN1_Grunido", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoAtaqueMomiaProcedural()
    {
        int sampleRate = 22050;
        float duration = 0.6f;
        int count = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[count];
        System.Random rng = new System.Random(303);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duration;
            float hiss = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.45f;
            float growl = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 60f, prog) * t) * 0.55f;
            float env = Mathf.Sin(prog * Mathf.PI);
            samples[i] = Mathf.Clamp((hiss + growl) * env, -1f, 1f) * 0.85f;
        }
        AudioClip clip = AudioClip.Create("MomiaN1_Ataque", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
