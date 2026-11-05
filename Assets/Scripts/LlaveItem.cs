using UnityEngine;

public class LlaveItem : MonoBehaviour
{
    [Header("Ajustes de Recogida")]
    public float distanciaRecogida = 2.6f; // Radio amplio y accesible
    
    private Transform playerTransform;
    private Vector3 posInicial;
    private SpriteRenderer sr;
    private Vector3 baseScale;

    void Start()
    {
        posInicial = transform.position;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 15; // Por encima de alfombras y suelo
            sr.enabled = false;   // Oculta hasta encontrar la pistola
        }
        Transform ped = transform.Find("Pedestal_Llave");
        if (ped != null) ped.gameObject.SetActive(false);

        baseScale = transform.localScale;
        BuscarJugador();
    }

    void BuscarJugador()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        BuscarJugador();

        // 0. Condición de progresión: La llave solo es visible y recogible SI la guardia ya encontró la pistola
        PistolController pistol = (playerTransform != null) ? playerTransform.GetComponent<PistolController>() : null;
        bool tienePistola = (pistol != null && pistol.HasPistol);

        Transform pedestal = transform.Find("Pedestal_Llave");
        if (!tienePistola)
        {
            if (sr != null) sr.enabled = false;
            if (pedestal != null) pedestal.gameObject.SetActive(false);
            return; // No se puede ver ni recoger
        }

        // Si ya tiene la pistola, hacer visible la llave y su pedestal
        if (sr != null) sr.enabled = true;
        if (pedestal != null) pedestal.gameObject.SetActive(true);

        // 1. Efecto flotante suave
        float flotar = Mathf.Sin(Time.time * 3.5f) * 0.18f;
        transform.position = new Vector3(posInicial.x, posInicial.y + flotar, posInicial.z);

        // 2. Efecto de pulsación dorada en escala para máxima visibilidad
        float pulso = 1f + Mathf.Sin(Time.time * 4.5f) * 0.12f;
        transform.localScale = baseScale * pulso;

        if (playerTransform == null) return;

        // 3. Detectar si el jugador está cerca para recogerla
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= distanciaRecogida)
        {
            PlayerController pc = playerTransform.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.tieneLlave = true;
            }

            if (Level1Manager.Instance != null)
            {
                Level1Manager.Instance.NotifyKeyPicked();
            }

            // Reproducir sonido de tintineo metálico de llaves
            AudioClip keySound = GenerarSonidoTintineoLlaves();
            Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(keySound, soundPos, 1.0f);

            Debug.Log("<color=yellow>¡Llave del museo encontrada y recogida con éxito!</color>");
            gameObject.SetActive(false);
        }
    }

    private static AudioClip GenerarSonidoTintineoLlaves()
    {
        int sampleRate = 44100;
        float duration = 0.65f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Tres toques metálicos ligeros (tintineo de llaves de hierro antiguas)
            float ch1 = Mathf.Sin(2f * Mathf.PI * 2100f * t) * Mathf.Exp(-t * 12f);
            float ch2 = 0f;
            if (t > 0.08f)
            {
                float t2 = t - 0.08f;
                ch2 = Mathf.Sin(2f * Mathf.PI * 2850f * t2) * Mathf.Exp(-t2 * 14f) * 0.8f;
            }
            float ch3 = 0f;
            if (t > 0.17f)
            {
                float t3 = t - 0.17f;
                ch3 = Mathf.Sin(2f * Mathf.PI * 3400f * t3) * Mathf.Exp(-t3 * 16f) * 0.9f;
            }

            // Resonancia armónica dorada
            float chime = Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 6f) * 0.35f;

            samples[i] = Mathf.Clamp(ch1 + ch2 + ch3 + chime, -1f, 1f) * 0.75f;
        }

        AudioClip clip = AudioClip.Create("Llave_Tintineo", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, distanciaRecogida);
        Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.5f, 0.1f));
    }
}
