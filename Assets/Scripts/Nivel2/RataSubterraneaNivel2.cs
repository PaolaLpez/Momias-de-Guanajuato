using System.Collections;
using UnityEngine;

/// <summary>
/// Rata subterránea del Nivel 2.
/// Genera proceduralmente su propio sprite pixel-art de rata (cuerpo gris, cola y ojito rojo integrado)
/// para que SIEMPRE sea visible y reconocible.
/// Solo chilla y corre si el vigilante se le acerca a menos de 2 metros o la alumbra de cerca.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(CircleCollider2D))]
public class RataSubterraneaNivel2 : MonoBehaviour
{
    [Header("Comportamiento")]
    public float velocidadTranquila = 1.0f;
    public float velocidadHuida = 4.2f;
    public float distanciaAlertaJugador = 2.0f;
    public float distanciaAlertaLuz = 3.5f;

    private bool asustada = false;
    private float tiempoBajoLuz = 0f;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private AudioSource audioSource;
    private Transform playerTransform;
    private LinternaVigilanteNivel2 linterna;

    private float timerPatrulla = 0f;
    private Vector2 dirActual = Vector2.zero;

    private void Awake()
    {
        InicializarComponentes();
    }

    private void OnEnable()
    {
        InicializarComponentes();
    }

    private void InicializarComponentes()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            sr.sprite = GenerarSpriteRataPixelArt();
        }
        sr.sortingOrder = 8;
        transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) col.radius = 0.25f;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f; // Audio 2D estéreo directo sin atenuación
        audioSource.playOnAwake = false;
        audioSource.volume = 1.0f;

        if (Application.isPlaying) EscogerNuevaDireccion();
    }

    private Sprite GenerarSpriteRataPixelArt()
    {
        int w = 18;
        int h = 10;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // Pixel art nítido

        Color transparente = new Color(0, 0, 0, 0);
        Color pelajeGris = new Color(0.75f, 0.72f, 0.68f, 1f);      // Gris claro contrastado
        Color pelajeSombra = new Color(0.50f, 0.48f, 0.45f, 1f);    // Sombra del cuerpo
        Color colaRosa = new Color(0.85f, 0.65f, 0.65f, 1f);        // Cola y oreja
        Color ojoRojo = new Color(1f, 0.1f, 0.1f, 1f);              // 1 píxel de ojo rojo brillante
        Color nariz = new Color(0.25f, 0.2f, 0.2f, 1f);

        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = transparente;

        // Dibujar cuerpo ovalado de la rata (X: 3 a 12, Y: 2 a 7)
        for (int x = 4; x <= 12; x++)
        {
            for (int y = 2; y <= 6; y++)
            {
                pix[y * w + x] = (y <= 3) ? pelajeSombra : pelajeGris;
            }
        }
        // Cabeza y hocico (hacia la derecha, X: 13 a 16)
        pix[4 * w + 13] = pelajeGris; pix[5 * w + 13] = pelajeGris; pix[3 * w + 13] = pelajeSombra;
        pix[4 * w + 14] = pelajeGris; pix[3 * w + 14] = pelajeSombra;
        pix[3 * w + 15] = nariz; // Nariz/hocico en la punta

        // Oreja (X: 12, Y: 7)
        pix[7 * w + 11] = colaRosa; pix[7 * w + 12] = pelajeGris;

        // Ojo rojo brillante (1 solo píxel en X: 13, Y: 5)
        pix[5 * w + 13] = ojoRojo;

        // Patitas (Y: 1)
        pix[1 * w + 5] = pelajeSombra; pix[1 * w + 6] = pelajeSombra;
        pix[1 * w + 11] = pelajeSombra; pix[1 * w + 12] = pelajeSombra;

        // Cola larga curva hacia la izquierda (X: 0 a 4)
        pix[4 * w + 3] = colaRosa;
        pix[5 * w + 2] = colaRosa;
        pix[6 * w + 1] = colaRosa;
        pix[6 * w + 0] = colaRosa;

        tex.SetPixels(pix);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.4f), 14f);
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
            if (p != null)
            {
                playerTransform = p.transform;
                linterna = p.GetComponentInChildren<LinternaVigilanteNivel2>();
            }
        }
    }

    private void Update()
    {
        BuscarJugador();

        if (asustada)
        {
            if (playerTransform != null)
            {
                Vector2 dirHuida = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                rb.linearVelocity = dirHuida * velocidadHuida;
                sr.flipX = dirHuida.x < 0;

                if (Vector2.Distance(transform.position, playerTransform.position) > 7.5f)
                {
                    asustada = false;
                    EscogerNuevaDireccion();
                }
            }
            return;
        }

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool iluminada = (linterna != null && linterna.EstaIluminando(transform.position));

            if (dist <= distanciaAlertaJugador)
            {
                Asustarse();
                return;
            }

            if (iluminada && dist <= distanciaAlertaLuz)
            {
                tiempoBajoLuz += Time.deltaTime;
                if (tiempoBajoLuz >= 0.5f)
                {
                    Asustarse();
                    return;
                }
            }
            else
            {
                tiempoBajoLuz = Mathf.Max(0f, tiempoBajoLuz - Time.deltaTime);
            }
        }

        timerPatrulla -= Time.deltaTime;
        if (timerPatrulla <= 0f)
        {
            EscogerNuevaDireccion();
        }

        rb.linearVelocity = dirActual * velocidadTranquila;
        if (dirActual.x != 0) sr.flipX = dirActual.x < 0;
    }

    private void EscogerNuevaDireccion()
    {
        timerPatrulla = Random.Range(2.5f, 6.0f);
        if (Random.value < 0.6f)
        {
            dirActual = Vector2.zero; // Quedarse quieta en la pared
        }
        else
        {
            float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            dirActual = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        }
    }

    public void Asustarse()
    {
        if (asustada) return;
        asustada = true;

        if (audioSource != null)
        {
            AudioClip clipRaton = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_RATON) ?? GenerarAudioChillidoRata();
            audioSource.pitch = Random.Range(0.95f, 1.15f);
            audioSource.PlayOneShot(clipRaton);
        }

        TrampaAcusticaNivel2.EmitirRuido(transform.position, 11f, "¡CHILLIDO DE RATA!");
        Debug.Log("<color=yellow>[Rata Nivel 2] Rata asustada en " + transform.position + "</color>");
    }

    private AudioClip GenerarAudioChillidoRata()
    {
        int rate = 44100;
        float dur = 0.35f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float freq = 2200f + Mathf.Sin(t * 75f) * 450f;
            float squeak = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.55f;
            samples[i] = squeak * Mathf.Exp(-t * 9f);
        }

        AudioClip clip = AudioClip.Create("AudioChillidoRata", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
