using System.Collections;
using UnityEngine;

/// <summary>
/// Tubería subterránea que expulsa chorros de vapor caliente de forma autónoma y cíclica.
/// Bloquea el paso y genera ruidos intermitentes que pueden alertar a la momia o empujar al jugador.
/// </summary>
public class ObstaculoVaporAutomaticoNivel2 : MonoBehaviour
{
    [Header("Tiempos del Ciclo Autónomo")]
    public float tiempoApagado = 4.5f;
    public float tiempoActivo = 2.8f;
    public float retrasoInicial = 0f;

    [Header("Efectos")]
    public SpriteRenderer spriteChorro;
    public BoxCollider2D colliderDanio;
    public AudioSource audioSource;
    public Vector2 direccionEmpuje = Vector2.right;

    public bool EstaExpulsando { get; private set; } = false;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (colliderDanio == null) colliderDanio = GetComponent<BoxCollider2D>();
        if (colliderDanio != null) colliderDanio.isTrigger = true;
    }

    private void Start()
    {
        StartCoroutine(CicloVapor());
    }

    private IEnumerator CicloVapor()
    {
        if (retrasoInicial > 0f) yield return new WaitForSeconds(retrasoInicial);

        while (true)
        {
            // Estado Inactivo (Se puede cruzar)
            EstaExpulsando = false;
            if (spriteChorro != null) spriteChorro.enabled = false;
            if (colliderDanio != null) colliderDanio.enabled = false;
            yield return new WaitForSeconds(tiempoApagado);

            // Estado Activo (Chorro de vapor bloqueando el camino)
            EstaExpulsando = true;
            if (spriteChorro != null) spriteChorro.enabled = true;
            if (colliderDanio != null) colliderDanio.enabled = true;

            // SILENCIADO: Vapor meramente visual y de fuerza física, sin saturar con ruido continuo
            yield return new WaitForSeconds(tiempoActivo);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (EstaExpulsando && collision.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Empujar al jugador hacia atrás impidiéndole cruzar
                rb.AddForce(direccionEmpuje * 450f);
            }
        }
    }

    private AudioClip GenerarAudioEscapeVapor()
    {
        int rate = 22050;
        float dur = tiempoActivo;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float noise = (Random.value * 2f - 1f) * 0.4f;
            float high = Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.15f;
            float envelope = Mathf.SmoothStep(0f, 1f, t * 8f) * Mathf.SmoothStep(1f, 0f, (t - (dur - 0.2f)) * 5f);
            samples[i] = (noise + high) * Mathf.Clamp01(envelope);
        }

        AudioClip clip = AudioClip.Create("AudioChorroVapor", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
