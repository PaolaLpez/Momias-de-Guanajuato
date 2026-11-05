using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Efecto de parpadeo realista de tubo fluorescente dañado en hospital forense — Nivel 3.
/// Genera micro-caídas de tensión y zumbidos intermitentes para ambientar el terror.
/// </summary>
public class LuzParpadeanteNivel3 : MonoBehaviour
{
    private Light2D luz;
    private float intensidadBase;
    private float timerProximoParpadeo;
    private bool estaParpadeando = false;
    private float duracionParpadeo = 0f;

    public float frecuencia = 3.5f; // Segundos promedio entre fallas

    private void Awake()
    {
        luz = GetComponent<Light2D>();
        if (luz != null)
        {
            intensidadBase = luz.intensity;
        }
        timerProximoParpadeo = Random.Range(1.5f, frecuencia);
    }

    private void Update()
    {
        if (luz == null) return;

        if (estaParpadeando)
        {
            duracionParpadeo -= Time.deltaTime;
            // Ruido caótico durante el parpadeo
            luz.intensity = Random.value > 0.45f ? intensidadBase * Random.Range(0.05f, 0.35f) : intensidadBase;

            if (duracionParpadeo <= 0f)
            {
                estaParpadeando = false;
                luz.intensity = intensidadBase;
                timerProximoParpadeo = Random.Range(frecuencia * 0.6f, frecuencia * 1.5f);
            }
        }
        else
        {
            timerProximoParpadeo -= Time.deltaTime;
            if (timerProximoParpadeo <= 0f)
            {
                estaParpadeando = true;
                duracionParpadeo = Random.Range(0.12f, 0.42f);
            }
        }
    }
}
