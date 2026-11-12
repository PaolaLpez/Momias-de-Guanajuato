using UnityEngine;

/// <summary>
/// Trampa acústica del Panteón de Santa Paula:
/// - Montículos de hojas secas: crujen al pisarlas rápido o sin sigilo.
/// - En Modo Sigilo (Ctrl / tecla C) o al estar agachado, NO hacen ruido y se cruzan con seguridad.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TrampaAcusticaNivel4 : MonoBehaviour
{
    public enum TipoTrampa
    {
        HojasSecas
    }

    public TipoTrampa tipo = TipoTrampa.HojasSecas;
    public TipoTrampa tipoTrampa { get => tipo; set => tipo = value; }
    public float ruidoGenerado = 0.35f;
    private AudioSource audioSource;
    private float timerCooldown = 0f;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.85f;
    }

    private void Update()
    {
        if (timerCooldown > 0f) timerCooldown -= Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (timerCooldown > 0f || Time.timeScale == 0f) return;

        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            VigilanteNivel4 vig = VigilanteNivel4.Instance;
            if (vig == null || vig.EstaOculta) return;

            // En modo sigilo (agachado), los pasos son silenciosos y no detonan la trampa
            if (VigilanteNivel4.ModoSigiloActivo) return;

            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.4f)
            {
                DetonarTrampa(vig.EstaCorriendo);
            }
        }
    }

    private void DetonarTrampa(bool corriendo)
    {
        timerCooldown = 0.85f;

        AudioClip clip = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_HOJAS_SECAS);
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.18f);
            audioSource.PlayOneShot(clip, corriendo ? 1.0f : 0.65f);
        }

        // Notificar ruido a la Momia Encadenada y a La Que Escucha (Sector Este)
        float radioAlerta = corriendo ? 18f : 10f;
        if (MomiaEncadenadaController.Instance != null)
        {
            MomiaEncadenadaController.Instance.NotificarRuido(transform.position, radioAlerta, 1f);
        }

        MomiaNivel2Seccion[] momiasN2 = Object.FindObjectsByType<MomiaNivel2Seccion>(FindObjectsSortMode.None);
        foreach (var m2 in momiasN2)
        {
            if (m2 != null && m2.gameObject.activeInHierarchy)
            {
                m2.NotificarRuido(transform.position, radioAlerta, 1f);
            }
        }

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.MostrarAviso("¡CRUJIDO DE HOJAS SECAS! USA SIGILO (CTRL/C)", 1.8f);
        }
    }
}
