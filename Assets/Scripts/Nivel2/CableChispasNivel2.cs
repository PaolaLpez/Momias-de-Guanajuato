using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cable de alta tensión caído que chispea de forma autónoma.
/// Ilumina violentamente la habitación por una fracción de segundo y emite crujidos eléctricos.
/// </summary>
public class CableChispasNivel2 : MonoBehaviour
{
    [Header("Parámetros del Arco Eléctrico")]
    public float intervaloMin = 2.5f;
    public float intervaloMax = 5.5f;
    public Light2D luzChispa;
    public AudioSource audioSource;
    public ParticleSystem particulas;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (luzChispa != null) luzChispa.enabled = false;
    }

    private void Start()
    {
        StartCoroutine(CicloChispas());
    }

    private IEnumerator CicloChispas()
    {
        while (true)
        {
            float espera = Random.Range(intervaloMin, intervaloMax);
            yield return new WaitForSeconds(espera);

            // Ráfaga de 2 o 3 chispazos rápidos
            int chispas = Random.Range(2, 4);
            for (int i = 0; i < chispas; i++)
            {
                if (luzChispa != null)
                {
                    luzChispa.enabled = true;
                    luzChispa.intensity = Random.Range(1.8f, 2.5f);
                }

                if (particulas != null) particulas.Play();
                // SILENCIADO: Chispas meramente visuales sin saturar el audio ambiente
                yield return new WaitForSeconds(0.06f);
                if (luzChispa != null) luzChispa.enabled = false;
                yield return new WaitForSeconds(0.04f);
            }
        }
    }

    private AudioClip GenerarAudioChispazo()
    {
        int rate = 22050;
        float dur = 0.12f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float zap = Mathf.Sin(2f * Mathf.PI * 450f * t) * 0.4f;
            float noise = (Random.value * 2f - 1f) * 0.7f;
            samples[i] = (zap + noise) * Mathf.Exp(-t * 28f);
        }

        AudioClip clip = AudioClip.Create("AudioZapCable", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
