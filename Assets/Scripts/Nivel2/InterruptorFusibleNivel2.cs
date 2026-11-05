using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Caja de fusibles eléctrica de alta tensión en el Nivel 2.
/// Cada una representa una fase eléctrica fundamental (1: Calderas, 2: Criptas, 3: Subestación).
/// Al activarla, se restaura la fase correspondiente, emite chispas y un estruendo sonoro.
/// NO TOCA NINGÚN ARCHIVO DEL NIVEL 1.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class InterruptorFusibleNivel2 : MonoBehaviour
{
    [Header("Identificación de Fase")]
    public string nombreFase = "Fase 1 - Cuarto de Calderas";
    public int idFase = 1;
    public bool estaActivado { get; private set; } = false;

    [Header("Conexión Directa al Generador")]
    public GeneradorPrincipalNivel2 generador;

    [Header("Componentes Visuales")]
    public SpriteRenderer cajaSr;
    public Light2D luzIndicadora;
    public ParticleSystem particulasChispas;

    private bool jugadorEnRango = false;
    private AudioSource audioSource;
    private Transform playerTransform;

    // Evento estático de notificación
    public static event Action<int, string> OnFaseElectricaRestaurada;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.8f, 2.8f); // Área de interacción amplia y cómoda

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // Audio directo estéreo
            audioSource.volume = 1.0f;
        }

        if (cajaSr == null) cajaSr = GetComponent<SpriteRenderer>();
        if (cajaSr != null && cajaSr.sprite == null)
        {
            cajaSr.sprite = GenerarSpritePanelIndustrial(idFase);
        }

        ActualizarVisuales();
        StartCoroutine(CicloChispasInactivas());
    }

    private void Start()
    {
        BuscarJugador();
        if (generador == null) generador = GeneradorPrincipalNivel2.Instance;
    }

    private void OnDestroy()
    {
        // Limpieza de eventos
        OnFaseElectricaRestaurada = null;
    }

    private void BuscarJugador()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) p = GameObject.Find("Vigilante");
            if (p == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) p = pc.gameObject;
            }
            if (p != null) playerTransform = p.transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (EsJugador(collision) && !estaActivado) jugadorEnRango = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (EsJugador(collision) && !estaActivado) jugadorEnRango = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (EsJugador(collision)) jugadorEnRango = false;
    }

    private bool EsJugador(Collider2D col)
    {
        return col.CompareTag("Player") || col.GetComponent<PlayerController>() != null || col.gameObject.name.Contains("Vigilante");
    }

    private void Update()
    {
        BuscarJugador();

        if (playerTransform != null && !estaActivado)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= 3.2f) jugadorEnRango = true;
            else if (dist > 4.0f) jugadorEnRango = false;
        }

        if (jugadorEnRango && !estaActivado && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
        {
            ActivarFusible();
        }
    }

    private IEnumerator CicloChispasInactivas()
    {
        while (!estaActivado)
        {
            float espera = UnityEngine.Random.Range(2.2f, 4.2f);
            yield return new WaitForSeconds(espera);
            if (estaActivado) yield break;

            // Ráfaga de 2 chispitas eléctricas que llaman la atención visualmente en la penumbra
            for (int i = 0; i < 2; i++)
            {
                if (luzIndicadora != null)
                {
                    luzIndicadora.color = (i == 0) ? new Color(0.3f, 0.8f, 1f, 1f) : new Color(1f, 0.85f, 0.2f, 1f);
                    luzIndicadora.intensity = 2.2f;
                }
                if (particulasChispas != null) particulasChispas.Play();
                // SILENCIADO: Cero reproducción de audio repetitivo de fondo. Solo visual.
                yield return new WaitForSeconds(0.06f);
                if (luzIndicadora != null && !estaActivado)
                {
                    luzIndicadora.color = new Color(1f, 0.2f, 0.15f, 1f);
                    luzIndicadora.intensity = 0.6f;
                }
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    public void ActivarFusible()
    {
        if (estaActivado) return;
        estaActivado = true;
        jugadorEnRango = false;

        ActualizarVisuales();
        StartCoroutine(SecuenciaActivacion());
    }

    private IEnumerator SecuenciaActivacion()
    {
        // 1. Sonido de encender generador / fusible
        if (audioSource != null)
        {
            AudioClip clipEncender = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_ENCENDER) ?? GenerarAudioPalancaChispas();
            audioSource.PlayOneShot(clipEncender);
        }

        // 2. Chispas visuales
        if (particulasChispas != null)
        {
            particulasChispas.Play();
        }

        // 3. Estruendo sonoro que alerta a la momia que escucha
        yield return new WaitForSeconds(0.12f);
        TrampaAcusticaNivel2.EmitirRuido(transform.position, 28f, $"¡ESTRUENDO ELÉCTRICO: {nombreFase}!");

        // 4. Parpadeo de la luz indicadora hasta fijarse en verde
        if (luzIndicadora != null)
        {
            for (int i = 0; i < 5; i++)
            {
                luzIndicadora.enabled = !luzIndicadora.enabled;
                yield return new WaitForSeconds(0.07f);
            }
            luzIndicadora.enabled = true;
            luzIndicadora.color = new Color(0.2f, 1f, 0.4f, 1f); // Verde de fase activa
            luzIndicadora.intensity = 1.4f;
            luzIndicadora.pointLightOuterRadius = 5.5f;
        }

        Debug.Log($"<color=lime>[Fusible Nivel 2] {nombreFase} activada.</color>");

        // Registrar fase directamente y mediante evento
        if (generador != null)
        {
            generador.RegistrarFase(idFase, nombreFase);
        }
        else if (GeneradorPrincipalNivel2.Instance != null)
        {
            GeneradorPrincipalNivel2.Instance.RegistrarFase(idFase, nombreFase);
        }

        OnFaseElectricaRestaurada?.Invoke(idFase, nombreFase);
    }

    private void ActualizarVisuales()
    {
        if (luzIndicadora != null)
        {
            luzIndicadora.color = estaActivado ? new Color(0.2f, 1f, 0.4f, 1f) : new Color(1f, 0.2f, 0.15f, 1f);
            luzIndicadora.intensity = estaActivado ? 1.4f : 0.65f;
        }
    }

    public static Sprite GenerarSpritePanelIndustrial(int id)
    {
        int w = 24;
        int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] cols = new Color[w * h];

        Color metalGris = new Color(0.24f, 0.26f, 0.28f, 1f);
        Color bordeMetal = new Color(0.40f, 0.42f, 0.45f, 1f);
        Color franjaAmarilla = new Color(0.95f, 0.80f, 0.15f, 1f);
        Color franjaNegra = new Color(0.10f, 0.10f, 0.12f, 1f);
        Color cobre = new Color(0.85f, 0.45f, 0.20f, 1f);
        Color ledRojo = new Color(1.0f, 0.25f, 0.20f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Borde metálico exterior
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                {
                    cols[y * w + x] = bordeMetal;
                }
                // Franja de peligro superior (hazard stripes)
                else if (y >= h - 6 && y <= h - 2)
                {
                    bool stripe = ((x + y) / 3) % 2 == 0;
                    cols[y * w + x] = stripe ? franjaAmarilla : franjaNegra;
                }
                // LED piloto
                else if (y >= h - 9 && y <= h - 7 && x >= 10 && x <= 13)
                {
                    cols[y * w + x] = ledRojo;
                }
                // Palanca / Interruptor central de cuchilla
                else if (y >= 10 && y <= 20 && x >= 9 && x <= 14)
                {
                    cols[y * w + x] = (x == 11 || x == 12) ? cobre : bordeMetal;
                }
                // Placa identificadora inferior
                else if (y >= 3 && y <= 7 && x >= 5 && x <= 18)
                {
                    cols[y * w + x] = franjaAmarilla;
                }
                // Fondo del panel
                else
                {
                    cols[y * w + x] = metalGris;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }

    private AudioClip GenerarAudioChispazoBreve()
    {
        int rate = 22050;
        float dur = 0.08f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];
        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float spark = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 35f);
            float zap = Mathf.Sin(2f * Mathf.PI * 450f * t) * Mathf.Exp(-t * 25f);
            samples[i] = Mathf.Clamp(spark * 0.7f + zap * 0.3f, -0.9f, 0.9f);
        }
        AudioClip clip = AudioClip.Create("ChispazoFusible", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarAudioPalancaChispas()
    {
        int rate = 22050;
        float dur = 0.45f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float clank = Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 20f);
            float spark = (UnityEngine.Random.value * 2f - 1f) * (t > 0.10f ? Mathf.Exp(-(t - 0.10f) * 8f) : 0f) * 0.85f;
            float buzz = Mathf.Sin(2f * Mathf.PI * 120f * t) * (t > 0.10f ? 0.3f : 0f);
            samples[i] = Mathf.Clamp(clank * 0.6f + spark + buzz, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("AudioFusibleActivado", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnGUI()
    {
        if (jugadorEnRango && !estaActivado)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + new Vector3(0, 1.4f, 0));
            if (screenPos.z > 0)
            {
                float w = 280f;
                float h = 30f;
                Rect rect = new Rect(screenPos.x - w / 2, Screen.height - screenPos.y - h / 2, w, h);

                GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
                GUI.Box(rect, "");
                GUI.color = Color.white;

                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
                style.normal.textColor = Color.white;

                GUI.Label(rect, $"<color=#FFDD00>[E] Restaurar {nombreFase}</color>", style);
            }
        }
    }
}
