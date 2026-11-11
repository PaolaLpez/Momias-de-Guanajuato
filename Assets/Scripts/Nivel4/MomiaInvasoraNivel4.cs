using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Momias Invasoras de las Catacumbas para el Nivel 4 (Panteón de Santa Paula):
/// Fieles a las mecánicas originales de los Niveles 2 y 3:
/// 1. MomiaMuseoNivel2:
///    - Visión con linterna y sigilo. Si la vigilante va agachada en sigilo o se oculta en sarcófago, no la detecta.
/// 2. HermanaCiegaNivel3:
///    - Es completamente ciega. Posee oído ultrasensible: si caminas o vas en sigilo, pasa a tu lado sin tocarte.
///    - Si corres con Shift o haces ruido en el micrófono, se lanza a la carrera hacia el sonido.
/// 3. HermanaAcechadoraNivel3:
///    - Mecánica "Weeping Angel": Si la vigilante la mira de frente, se congela como estatua pétrea.
///    - Al darle la espalda o apagar la linterna, acecha a gran velocidad.
/// - Todas las momias infligen 15 de daño con cooldown de 2.2s (NO muerte instantánea) para permitir un gameplay justo y ganable.
/// - Todas respetan las zonas seguras de los Cirios Benditos (huyen despavoridas de la luz dorada).
/// - Se petrifican al sonar la Campana Mayor a las 6:00 AM.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class MomiaInvasoraNivel4 : MonoBehaviour
{
    public enum TipoInvasora
    {
        MomiaMuseoNivel2,
        HermanaCiegaNivel3,
        HermanaAcechadoraNivel3
    }

    [Header("Tipo y Configuración")]
    public TipoInvasora tipo = TipoInvasora.MomiaMuseoNivel2;
    public float velocidadPatrulla = 1.8f;
    public float velocidadCaza = 3.3f;
    public float danioAtaque = 15f;
    public float cooldownAtaque = 2.2f;

    [Header("Estado")]
    public bool Activa = true;
    public bool Petrificada = false;
    public bool EstaCongelada = false; // Para la Hermana Acechadora
    private float timerAtaque = 0f;
    private float timerRepelerFuego = 0f;
    private Vector2 direccionHuidaFuego = Vector2.zero;

    [Header("Patrulla Local")]
    public Vector2[] puntosPatrulla;
    private int indicePatrulla = 0;
    private Vector2 destinoInvestigacion;
    private bool investigandoRuido = false;
    private float timerInvestigacion = 0f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform playerTransform;

    [Header("Sprites")]
    private Sprite[] walkSprites;
    private Sprite idleSprite;
    private float animTimer = 0f;
    private int animIndex = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.radius = 0.60f;
        col.isTrigger = false;

        CargarSprites();
    }

    private void Start()
    {
        // Inicializar circuito de patrulla por omisión si no fue asignado
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            Vector2 pos = transform.position;
            puntosPatrulla = new Vector2[]
            {
                pos,
                pos + new Vector2(4.5f, 0f),
                pos + new Vector2(4.5f, -4.5f),
                pos + new Vector2(-4.5f, -4.5f),
                pos + new Vector2(-4.5f, 2.5f)
            };
        }
    }

    private void CargarSprites()
    {
        List<Sprite> list = new List<Sprite>();

        if (tipo == TipoInvasora.MomiaMuseoNivel2)
        {
            // Cargar los 6 frames individuales limpios de la Momia del Nivel 2
            for (int i = 0; i < 6; i++)
            {
                Sprite sp = Resources.Load<Sprite>("Momia2/momia2_walk_" + i);
                if (sp != null) list.Add(sp);
            }
            idleSprite = Resources.Load<Sprite>("Momia2/momia2_pose1_stalk");
        }
        else if (tipo == TipoInvasora.HermanaCiegaNivel3)
        {
            // Cargar los 6 frames individuales limpios de la Momia Ciega del Nivel 3
            for (int i = 0; i < 6; i++)
            {
                Sprite sp = Resources.Load<Sprite>("Momia/momia_walk_" + i);
                if (sp != null) list.Add(sp);
            }
            idleSprite = Resources.Load<Sprite>("Momia/momia_idle");
        }
        else // HermanaAcechadoraNivel3
        {
            // Cargar los 3 frames individuales de la Momia Acechadora
            for (int i = 0; i < 3; i++)
            {
                Sprite sp = Resources.Load<Sprite>("Momia3/momia3_walk_" + i);
                if (sp != null) list.Add(sp);
            }
            idleSprite = Resources.Load<Sprite>("Momia3/momia3_idle");
            if (list.Count == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    Sprite sp = Resources.Load<Sprite>("Momia/momia_walk_" + i);
                    if (sp != null) list.Add(sp);
                }
            }
        }

        walkSprites = list.ToArray();
        if (walkSprites != null && walkSprites.Length > 0 && sr != null)
        {
            sr.sprite = idleSprite != null ? idleSprite : walkSprites[0];
            sr.sortingOrder = 9;
            sr.color = Color.white;
        }
    }

    private void Update()
    {
        if (Petrificada || !Activa || Time.timeScale == 0f || (Nivel4Manager.Instance != null && Nivel4Manager.Instance.enInteriorCapilla))
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (playerTransform == null && VigilanteNivel4.Instance != null)
        {
            playerTransform = VigilanteNivel4.Instance.transform;
        }

        if (timerAtaque > 0f) timerAtaque -= Time.deltaTime;
        if (timerRepelerFuego > 0f) timerRepelerFuego -= Time.deltaTime;
        if (timerInvestigacion > 0f) timerInvestigacion -= Time.deltaTime;

        ActualizarComportamiento();
        ActualizarAnimacion();
    }

    private void ActualizarComportamiento()
    {
        if (playerTransform == null) return;

        // 1. Huida por el fuego bendito de un Cirio
        if (timerRepelerFuego > 0f)
        {
            rb.linearVelocity = direccionHuidaFuego * velocidadCaza;
            EstaCongelada = false;
            if (sr != null) sr.color = Color.white;
            return;
        }

        Vector2 posActual = transform.position;
        Vector2 posPlayer = playerTransform.position;
        float dist = Vector2.Distance(posActual, posPlayer);

        bool vigilanteOculta = VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaOculta;
        if (vigilanteOculta)
        {
            PatrullarSector();
            return;
        }

        bool corriendo = VigilanteNivel4.Instance != null && VigilanteNivel4.Instance.EstaCorriendo;
        bool enSigilo = VigilanteNivel4.ModoSigiloActivo;

        // 2. Comportamiento según el tipo de momia
        switch (tipo)
        {
            case TipoInvasora.HermanaCiegaNivel3:
                // Ciega total: CERO visión. Solo reacciona a pisadas fuertes al correr o micrófono
                if (corriendo && dist < 16.0f)
                {
                    investigandoRuido = true;
                    destinoInvestigacion = posPlayer;
                    timerInvestigacion = 3.5f;
                }

                if (investigandoRuido && timerInvestigacion > 0f)
                {
                    Vector2 dirRuido = (destinoInvestigacion - posActual).normalized;
                    rb.linearVelocity = dirRuido * velocidadCaza;
                    if (Vector2.Distance(posActual, destinoInvestigacion) < 1.2f)
                    {
                        investigandoRuido = false;
                    }
                }
                else
                {
                    PatrullarSector();
                }
                EstaCongelada = false;
                if (sr != null) sr.color = Color.white;
                break;

            case TipoInvasora.HermanaAcechadoraNivel3:
                // Mecánica Weeping Angel: se congela si la vigilante la está mirando de frente
                bool mirandola = JugadorMiraHaciaMomia();
                if (mirandola && dist < 13.0f)
                {
                    // Se congela inmóvil como una estatua pétrea
                    EstaCongelada = true;
                    rb.linearVelocity = Vector2.zero;
                    if (sr != null)
                    {
                        sr.color = new Color(0.70f, 0.75f, 0.85f, 1f); // Tono de piedra
                        if (idleSprite != null) sr.sprite = idleSprite;
                    }
                }
                else
                {
                    // No la mira: se desliza veloz hacia la jugadora
                    EstaCongelada = false;
                    if (sr != null) sr.color = Color.white;

                    if (dist < 14.0f)
                    {
                        Vector2 dirCaza = (posPlayer - posActual).normalized;
                        rb.linearVelocity = dirCaza * velocidadCaza;
                    }
                    else
                    {
                        PatrullarSector();
                    }
                }
                break;

            case TipoInvasora.MomiaMuseoNivel2:
            default:
                // Momia del Museo: Visión clásica evadible con sigilo y distancia
                float rangoVision = enSigilo ? 2.8f : 8.0f;
                if (dist < rangoVision)
                {
                    Vector2 dirCaza = (posPlayer - posActual).normalized;
                    rb.linearVelocity = dirCaza * velocidadCaza;
                }
                else
                {
                    PatrullarSector();
                }
                EstaCongelada = false;
                if (sr != null) sr.color = Color.white;
                break;
        }

        if (sr != null && rb.linearVelocity.x != 0f)
        {
            sr.flipX = rb.linearVelocity.x < 0f;
        }
    }

    private bool JugadorMiraHaciaMomia()
    {
        if (playerTransform == null) return false;
        SpriteRenderer srPlayer = playerTransform.GetComponent<SpriteRenderer>();
        if (srPlayer == null) return false;

        Vector2 dirMirada = srPlayer.flipX ? Vector2.left : Vector2.right;
        Vector2 dirHaciaMomia = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;

        return Vector2.Dot(dirMirada, dirHaciaMomia) > 0.20f;
    }

    private void PatrullarSector()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 destino = puntosPatrulla[indicePatrulla];
        Vector2 pos = transform.position;
        float d = Vector2.Distance(pos, destino);

        if (d <= 1.0f)
        {
            indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
        }
        else
        {
            Vector2 dir = (destino - pos).normalized;
            rb.linearVelocity = dir * velocidadPatrulla;
        }
    }

    private void ActualizarAnimacion()
    {
        if (sr == null || walkSprites == null || walkSprites.Length == 0 || EstaCongelada) return;

        if (rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 0.12f)
            {
                animTimer = 0f;
                animIndex = (animIndex + 1) % walkSprites.Length;
                sr.sprite = walkSprites[animIndex];
            }
        }
        else
        {
            if (idleSprite != null) sr.sprite = idleSprite;
        }
    }

    public void RepelerPorFuegoBendito(Vector3 centroFuego, float radio)
    {
        direccionHuidaFuego = ((Vector2)transform.position - (Vector2)centroFuego).normalized;
        timerRepelerFuego = 3.0f;
        investigandoRuido = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Petrificada || timerAtaque > 0f || EstaCongelada) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<VigilanteNivel4>() != null)
        {
            AtacarJugador();
        }
    }

    private void AtacarJugador()
    {
        timerAtaque = cooldownAtaque;

        if (VigilanteNivel4.Instance != null)
        {
            VigilanteNivel4.Instance.RecibirDanio(danioAtaque);

            string nombreMomia = "UNA MOMIA";
            if (tipo == TipoInvasora.MomiaMuseoNivel2) nombreMomia = "LA MOMIA DEL MUSEO";
            else if (tipo == TipoInvasora.HermanaCiegaNivel3) nombreMomia = "LA HERMANA CIEGA";
            else if (tipo == TipoInvasora.HermanaAcechadoraNivel3) nombreMomia = "LA HERMANA ACECHADORA";

            if (VigilanteNivel4.Instance.vidaActual <= 0f)
            {
                if (Nivel4Manager.Instance != null)
                {
                    Nivel4Manager.Instance.ActivarJumpscareGameOver();
                }
            }
            else
            {
                if (Nivel4Manager.Instance != null)
                {
                    Nivel4Manager.Instance.MostrarAviso($"¡{nombreMomia} TE HA ALCANZADO! (❤ {Mathf.CeilToInt(VigilanteNivel4.Instance.vidaActual)})", 2.2f);
                    Nivel4Manager.Instance.SacudirPantalla(0.45f, 15f);
                }
            }
        }
    }

    public void Petrificar()
    {
        Petrificada = true;
        Activa = false;
        rb.linearVelocity = Vector2.zero;
        if (sr != null)
        {
            sr.color = new Color(0.40f, 0.40f, 0.40f, 1f); // Ceniza petrificada
        }
    }
}
