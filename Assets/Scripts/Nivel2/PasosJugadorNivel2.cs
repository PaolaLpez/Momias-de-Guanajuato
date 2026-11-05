using UnityEngine;

/// <summary>
/// Sistema de sonido de pasos de carrera para el Vigilante en el Nivel 2.
/// Cuando el personaje corre (Shift + Movimiento), reproduce pasos rítmicos de botas tácticas
/// resonando contra el piso de piedra húmeda y emite alertas de ruido acústico para la momia que escucha.
/// NO TOCA NINGÚN ARCHIVO DEL NIVEL 1.
/// </summary>
public class PasosJugadorNivel2 : MonoBehaviour
{
    [Header("Configuración de Pasos de Carrera")]
    public float cadenciaCarrera = 0.26f; // Intervalo entre pisadas al correr
    public float volumenPasos = 0.75f;
    public float radioAlertaRuido = 14f;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float tiempoSiguientePaso = 0f;
    private bool pieIzquierdo = true;

    private AudioClip clipPasoA;
    private AudioClip clipPasoB;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // Estéreo 2D directo
            audioSource.playOnAwake = false;
        }

        clipPasoA = GenerarAudioPasoBotaPiedra(110f, 0.95f);
        clipPasoB = GenerarAudioPasoBotaPiedra(125f, 1.05f);
    }

    private void Update()
    {
        // Silencio absoluto durante pausa, Game Over, muerte, Modo Sigilo o sobre charcos de agua
        if (Time.timeScale == 0f || SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo || TrampaAcusticaNivel2.JugadorEnCharco)
        {
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            return;
        }

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && (pc.EstaMuerto || pc.vidaActual <= 0f))
        {
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            return;
        }

        // Detectar si el jugador se está desplazando
        bool tieneEntradaMov = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        bool conVelocidad = rb != null && rb.linearVelocity.sqrMagnitude > 0.8f;
        bool enMovimiento = tieneEntradaMov || conVelocidad;

        // Detectar si está corriendo con Shift presionado
        bool estaCorriendo = enMovimiento && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (estaCorriendo)
        {
            tiempoSiguientePaso -= Time.deltaTime;
            if (tiempoSiguientePaso <= 0f)
            {
                tiempoSiguientePaso = cadenciaCarrera;
                ReproducirPasoCarrera();
            }
        }
        else
        {
            // Resetear para que al volver a correr el primer paso suene de inmediato
            tiempoSiguientePaso = 0.08f;
        }
    }

    private void ReproducirPasoCarrera()
    {
        if (audioSource == null) return;

        AudioClip clipElegido = pieIzquierdo ? clipPasoA : clipPasoB;
        pieIzquierdo = !pieIzquierdo;

        audioSource.pitch = Random.Range(0.96f, 1.04f);
        audioSource.PlayOneShot(clipElegido, volumenPasos);

        // Emitir alerta acústica al entorno (delata la posición del jugador ante Momia 2)
        TrampaAcusticaNivel2.EmitirRuido(transform.position, radioAlertaRuido, "¡PASOS DE CARRERA DEL VIGILANTE!");
    }

    /// <summary>
    /// Síntesis procedural de pisada de bota táctica de cuero y suela de caucho sobre losa de piedra/concreto.
    /// </summary>
    private AudioClip GenerarAudioPasoBotaPiedra(float tonoBase, float factorPitch)
    {
        int rate = 22050;
        float dur = 0.14f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;

            // 1. Golpe sordo grave del talón contra el suelo de piedra (Thud)
            float thud = Mathf.Sin(2f * Mathf.PI * (tonoBase * factorPitch) * t) * Mathf.Exp(-t * 42f);

            // 2. Fricción y crujido de la arenilla/polvo del sótano bajo la bota
            float grit = (Random.value * 2f - 1f) * Mathf.Exp(-t * 26f) * 0.45f;

            // 3. Resonancia corta de suelo sólido de cripta
            float reverb = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Exp(-t * 18f) * 0.25f;

            samples[i] = Mathf.Clamp(thud * 0.7f + grit + reverb, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create($"AudioPasoBota_{tonoBase}", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
