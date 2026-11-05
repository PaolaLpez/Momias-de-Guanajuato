using System.Collections;
using UnityEngine;

/// <summary>
/// Escorpión del subsuelo / taller de restauración — Nivel 3.
/// Se mueve por azulejos y desagües. Su picadura envenena al vigilante y emite un
/// alarido involuntario de dolor que alerta al instante a la Gemela Ciega.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class EscorpionNivel3 : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 0.85f;
    public float radioPatrulla = 2.5f;
    private Vector2 origen;
    private Vector2 destino;
    private float tiempoCambioDestino = 0f;

    [Header("Audio")]
    private AudioSource audioSource;
    private AudioClip clipAtaque;

    private SpriteRenderer sr;
    private Sprite spriteIdle;
    private Sprite spriteWalk0;
    private Sprite spriteWalk1;
    private Sprite spriteAttack;
    private float animTimer = 0f;
    private int animFrame = 0;

    private bool puedePicar = true;

    private void Awake()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 310;
        transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.8f;
        audioSource.volume = 1f;
        audioSource.playOnAwake = false;

        CargarRecursos();
    }

    private void CargarRecursos()
    {
        Sprite spInicial = HojasSpritesNivel3.EscorpionCaminar(0);
        if (spInicial != null) sr.sprite = spInicial;

        clipAtaque = Resources.Load<AudioClip>("AudioNivel3/ataque_escorpion");
        if (clipAtaque == null) clipAtaque = Resources.Load<AudioClip>("ataque_escorpion");
        if (clipAtaque == null) clipAtaque = Resources.Load<AudioClip>("AudioNivel3/yodguard-scorpion-claw-attack-3-482508");
        if (clipAtaque == null) clipAtaque = Resources.Load<AudioClip>("yodguard-scorpion-claw-attack-3-482508");
    }

    private void Start()
    {
        origen = transform.position;
        if (sr.sprite == null) sr.sprite = HojasSpritesNivel3.EscorpionCaminar(0);
        ElegirNuevoDestino();
    }

    private void Update()
    {
        if (Nivel3Manager.Instance != null && Nivel3Manager.Instance.JuegoPausadoOTerminado) return;

        tiempoCambioDestino -= Time.deltaTime;
        if (tiempoCambioDestino <= 0f || Vector2.Distance(transform.position, destino) < 0.2f)
        {
            ElegirNuevoDestino();
        }

        Vector2 diff = destino - (Vector2)transform.position;
        if (diff.sqrMagnitude > 0.005f)
        {
            Vector2 dir = diff.normalized;
            transform.position += (Vector3)(dir * velocidad * Time.deltaTime);

            // Rotación mirando hacia donde camina (el sprite tiene las pinzas hacia arriba)
            float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            float crawlWiggle = Mathf.Sin(Time.time * 12f) * 3.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, angulo + crawlWiggle);

            // Sutil balanceo de patas orgánico
            float crawlPulse = 0.85f * (1.0f + Mathf.Sin(Time.time * 12f) * 0.03f);
            transform.localScale = new Vector3(crawlPulse, 0.85f, 1f);
        }

        if (sr.sprite == null)
        {
            Sprite spLimpio = HojasSpritesNivel3.EscorpionCaminar(0);
            if (spLimpio != null) sr.sprite = spLimpio;
        }
    }

    private void ElegirNuevoDestino()
    {
        destino = origen + Random.insideUnitCircle * radioPatrulla;
        tiempoCambioDestino = Random.Range(3f, 6f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!puedePicar) return;

        if (collision.CompareTag("Player") || collision.name.Contains("Vigilante"))
        {
            StartCoroutine(EjecutarPicadura(collision.transform));
        }
    }

    private IEnumerator EjecutarPicadura(Transform jugador)
    {
        puedePicar = false;
        Sprite spAtk = HojasSpritesNivel3.EscorpionAtaque();
        if (spAtk != null) sr.sprite = spAtk;
        else if (spriteAttack != null) sr.sprite = spriteAttack;

        // Sonido de ataque de tenazas / aguijón
        if (clipAtaque != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipAtaque, 1.0f);
        }

        // Infligir daño de picadura leve a la vigilante
        PlayerController pc = jugador.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.RecibirDanio(12f, "Picadura de Alacrán");
        }

        // Efecto de veneno y dolor en el Manager
        if (Nivel3Manager.Instance != null)
        {
            Nivel3Manager.Instance.AplicarVenenoEscorpion();
        }

        // Alerta acústica para la Gemela Ciega
        if (MomiaCiegaNivel3.Instance != null)
        {
            MomiaCiegaNivel3.Instance.AlertarRuido(jugador.position, "¡Picadura de alacrán! Tu quejido de dolor atrajo a la Gemela Ciega");
        }

        yield return new WaitForSeconds(3.5f);
        puedePicar = true;
    }
}
