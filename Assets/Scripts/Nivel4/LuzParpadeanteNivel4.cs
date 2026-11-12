using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Parpadeo realista de farol de gas de hierro en el Panteón de Santa Paula.
/// Simula la oscilación natural de la llama ante las ráfagas de viento frío de Guanajuato.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LuzParpadeanteNivel4 : MonoBehaviour
{
    public float intensidadBase = 0.95f;
    public float variacion = 0.28f;
    public float velocidad = 7.5f;

    private Light2D luz;
    private float semilla;

    private void Awake()
    {
        luz = GetComponent<Light2D>();
        semilla = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (luz == null) return;
        float ruido = Mathf.PerlinNoise(semilla, Time.time * velocidad);
        luz.intensity = intensidadBase + (ruido - 0.5f) * variacion * 2f;
    }
}
