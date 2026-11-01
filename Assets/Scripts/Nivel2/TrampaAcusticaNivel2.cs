using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public enum TipoTrampaAcustica
{
    CharcoAgua,      // Chapoteo al pisar o correr
    CristalesRotos,  // Crujido penetrante de esquirlas de vitrina
    RejaChirriante   // Chirrido metálico estridente
}

/// <summary>
/// Trampa acústica del escenario del Nivel 2.
/// Genera alertas de ruido que atraerán a la Momia que escucha.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TrampaAcusticaNivel2 : MonoBehaviour
{
    [Header("Configuración de la Trampa")]
    public TipoTrampaAcustica tipoTrampa = TipoTrampaAcustica.CharcoAgua;
    public float intensidadRuido = 1.0f; // Multiplicador de ruido
    public float radioPropagacion = 14f; // Distancia a la que la momia oirá el estruendo
    public float tiempoRecarga = 0.6f;

    [Header("Efectos Visuales y Sonoros")]
    private AudioSource audioSource;
    private float ultimoTiempoActivacion = -1f;
    private Collider2D playerColDentro;
    private float timerCadenciaPasoCharco = 0f;

    private static int charcosTocadosPorJugador = 0;
    // Estado global para silenciar pasos secos de bota mientras se pisa agua
    public static bool JugadorEnCharco => charcosTocadosPorJugador > 0;

    // Clip específico y exclusivo de charco.mp3
    private static AudioClip clipCharcoGlobal;

    private bool playerEstaDentroDeEsteCharco = false;

    // Evento estático global de ruido para que la IA de la momia ciega lo escuche
    public static event Action<Vector3, float, string> OnRuidoGenerado;

    public static void EmitirRuido(Vector3 posicion, float radio, string etiqueta)
    {
        OnRuidoGenerado?.Invoke(posicion, radio, etiqueta);
    }

    public static void ActualizarClipCharcoGlobal(AudioClip clip)
    {
        if (clip != null)
        {
            clipCharcoGlobal = clip;
            TrampaAcusticaNivel2[] todas = FindObjectsByType<TrampaAcusticaNivel2>(FindObjectsSortMode.None);
            foreach (var t in todas)
            {
                if (t != null && t.tipoTrampa == TipoTrampaAcustica.CharcoAgua && t.audioSource != null)
                {
                    t.audioSource.clip = clip;
                }
            }
        }
    }

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f; // 2D estéreo directo sin atenuación
        audioSource.playOnAwake = false;
        audioSource.loop = false; // JAMÁS en bucle infinito para no quedarse trabado
        audioSource.volume = 1.0f;

        if (tipoTrampa == TipoTrampaAcustica.CharcoAgua)
        {
            AsegurarClipCharco();
        }
    }

    private void Start()
    {
        if (tipoTrampa == TipoTrampaAcustica.CharcoAgua)
        {
            AsegurarClipCharco();
        }
    }

    private void AsegurarClipCharco()
    {
        if (clipCharcoGlobal != null && clipCharcoGlobal.loadState == AudioDataLoadState.Loaded)
        {
            if (audioSource.clip != clipCharcoGlobal)
            {
                audioSource.clip = clipCharcoGlobal;
            }
            return;
        }

        AudioClip clip = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_CHARCO);

        if (clip == null) clip = Resources.Load<AudioClip>("Audio/Nivel2/charco");
        if (clip == null) clip = Resources.Load<AudioClip>("AudioNivel2/charco");
        if (clip == null) clip = Resources.Load<AudioClip>("charco");

#if UNITY_EDITOR
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/Nivel2/charco.mp3");
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Nivel2/charco.mp3");
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/AudioNivel2/charco.mp3");
#endif

        if (clip != null)
        {
            clip.LoadAudioData();
            clipCharcoGlobal = clip;
            audioSource.clip = clip;
        }
    }

    private void Update()
    {
        if (tipoTrampa == TipoTrampaAcustica.CharcoAgua)
        {
            ActualizarCharcoAgua();
            return;
        }

        // Cristales rotos: silenciar si no hay contacto o entra en sigilo
        if (audioSource != null && audioSource.isPlaying)
        {
            if (SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo || playerColDentro == null || Time.timeScale == 0f)
            {
                audioSource.Stop();
                if (audioSource.clip != null) audioSource.time = 0f;
            }
        }
    }

    private void ActualizarCharcoAgua()
    {
        // 1. Silencio absoluto en pausa, Game Over o Jumpscare
        if (Time.timeScale == 0f)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            return;
        }

        // 2. Si el jugador no está sobre este charco, silenciar de inmediato
        if (playerColDentro == null || !playerEstaDentroDeEsteCharco)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
                if (audioSource.clip != null) audioSource.time = 0f;
            }
            return;
        }

        // 3. Silencio absoluto en Modo Sigilo (solo se escucha el ambiente)
        if (SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            return;
        }

        // 4. Detectar si el jugador se está desplazando activamente
        Rigidbody2D rb = playerColDentro.GetComponentInParent<Rigidbody2D>();
        float velSqr = rb != null ? rb.linearVelocity.sqrMagnitude : 0f;
        bool tieneInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.05f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.05f;
        bool seMueve = velSqr > 0.08f || tieneInput;

        if (!seMueve)
        {
            // El jugador se detuvo en el charco: silenciar inmediatamente para que jamás siga sonando ni se trabe
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            return;
        }

        // 5. El jugador está pasando por el charco:
        if (audioSource.clip == null || audioSource.clip != clipCharcoGlobal)
        {
            AsegurarClipCharco();
        }

        if (audioSource.clip == null) return;

        bool corriendo = SistemaMomiaYMicrofonoNivel2.EsInputCorrerActivo();
        float volumenPaso = corriendo ? 1.0f : 0.85f;
        audioSource.pitch = corriendo ? 1.12f : 1.0f;
        audioSource.volume = volumenPaso;
        audioSource.loop = true;

        // Reanudar o reproducir fluidamente solo mientras camina por el agua
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

        timerCadenciaPasoCharco -= Time.deltaTime;
        if (timerCadenciaPasoCharco <= 0f)
        {
            timerCadenciaPasoCharco = corriendo ? 1.25f : 1.85f;
            // Alerta acústica para la Momia que escucha con cadencia equilibrada (sin saturar la IA)
            float radio = corriendo ? (radioPropagacion * 1.35f) : (radioPropagacion * 0.85f);
            OnRuidoGenerado?.Invoke(transform.position, radio, "¡CHAPOTEO DE AGUA!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponentInParent<PlayerController>() != null)
        {
            if (!playerEstaDentroDeEsteCharco)
            {
                playerEstaDentroDeEsteCharco = true;
                charcosTocadosPorJugador++;
            }
            playerColDentro = collision;
            if (tipoTrampa == TipoTrampaAcustica.CharcoAgua)
            {
                timerCadenciaPasoCharco = 0f; // Iniciar primer paso de inmediato sin retraso
                ActualizarCharcoAgua();
                return;
            }
        }
        EvaluarContacto(collision, true);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponentInParent<PlayerController>() != null)
        {
            if (!playerEstaDentroDeEsteCharco)
            {
                playerEstaDentroDeEsteCharco = true;
                charcosTocadosPorJugador++;
            }
            playerColDentro = collision;
            if (tipoTrampa == TipoTrampaAcustica.CharcoAgua)
            {
                ActualizarCharcoAgua();
                return;
            }
        }
        EvaluarContacto(collision, false);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Al salir del charco o trampa, cortar el audio de inmediato para que jamás quede sonando
        if (collision.CompareTag("Player") || collision.GetComponentInParent<PlayerController>() != null)
        {
            if (playerEstaDentroDeEsteCharco)
            {
                playerEstaDentroDeEsteCharco = false;
                charcosTocadosPorJugador = Mathf.Max(0, charcosTocadosPorJugador - 1);
            }
            playerColDentro = null;
            if (tipoTrampa == TipoTrampaAcustica.CharcoAgua)
            {
                timerCadenciaPasoCharco = 0f;
            }
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
                if (audioSource.clip != null) audioSource.time = 0f;
            }
        }
    }

    private void EvaluarContacto(Collider2D collision, bool primerContacto)
    {
        if (!collision.CompareTag("Player") && collision.GetComponentInParent<PlayerController>() == null) return;

        // En Modo Sigilo no se debe escuchar nada más que el ambiente
        if (SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                if (audioSource.clip != null) audioSource.time = 0f;
            }
            return;
        }

        Rigidbody2D rb = collision.GetComponentInParent<Rigidbody2D>();
        float velocidad = rb != null ? rb.linearVelocity.magnitude : 0f;

        if (velocidad < 0.2f)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                if (audioSource.clip != null) audioSource.time = 0f;
            }
            return;
        }

        if (Time.time - ultimoTiempoActivacion < tiempoRecarga) return;

        bool corriendo = velocidad > 4.2f || SistemaMomiaYMicrofonoNivel2.EsInputCorrerActivo();

        // Cristales rotos
        if (tipoTrampa == TipoTrampaAcustica.CristalesRotos)
        {
            float ruidoEfectivo = corriendo ? 1.5f : 0.85f;
            DispararTrampa(collision.transform.position, ruidoEfectivo, "¡CRUJIDO DE VIDRIO!");
        }
    }

    private void DispararTrampa(Vector3 posicion, float fuerza, string etiqueta)
    {
        ultimoTiempoActivacion = Time.time;

        ReproducirSonidoProcedural(fuerza);

        OnRuidoGenerado?.Invoke(posicion, radioPropagacion * fuerza, etiqueta);
        Debug.Log($"<color=yellow>[TrampaAcustica Nivel 2] {etiqueta} en {posicion} (Radio: {radioPropagacion * fuerza}m)</color>");

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            StartCoroutine(FlashVisual(sr));
        }
    }

    private System.Collections.IEnumerator FlashVisual(SpriteRenderer sr)
    {
        Color orig = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        sr.color = orig;
    }

    private void ReproducirSonidoProcedural(float volumen)
    {
        // Los charcos usan exclusivamente charco.mp3 y jamás deben ser sobreescritos por sonido procedural
        if (tipoTrampa == TipoTrampaAcustica.CharcoAgua) return;

        audioSource.loop = false;
        if (audioSource.clip == null)
        {
            audioSource.clip = GenerarClipSonido(tipoTrampa);
        }
        audioSource.volume = Mathf.Clamp01(volumen);
        audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private AudioClip GenerarClipSonido(TipoTrampaAcustica tipo)
    {
        int sampleRate = 44100;
        float dur = 0.42f;
        int total = Mathf.CeilToInt(sampleRate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / sampleRate;
            float f1 = Mathf.Sin(2f * Mathf.PI * 2600f * t) * Mathf.Exp(-t * 45f);
            float f2 = Mathf.Sin(2f * Mathf.PI * 4200f * t) * Mathf.Exp(-t * 60f);
            float snap = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 30f);
            samples[i] = (f1 * 0.4f + f2 * 0.35f + snap * 0.25f) * 0.7f;
        }

        AudioClip clip = AudioClip.Create("SFX_Cristal_" + tipo, total, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
