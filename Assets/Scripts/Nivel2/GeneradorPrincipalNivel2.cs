using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// El Gran Generador Eléctrico Subterráneo del Nivel 2.
/// Centraliza la restauración de las 3 fases eléctricas y activa la energía de emergencia en el subsuelo.
/// Requiere que se activen las 3 fases distintas (Calderas, Criptas, Subestación).
/// NO reproduce ningún bucle sonoro molesto que contamine el ambiente acústico.
/// NO TOCA NINGÚN ARCHIVO DEL NIVEL 1.
/// </summary>
public class GeneradorPrincipalNivel2 : MonoBehaviour
{
    public static GeneradorPrincipalNivel2 Instance { get; private set; }

    [Header("Progreso de Restauración")]
    public int fasesRequeridas = 3;
    public bool energiaRestauradaTotalmente { get; private set; } = false;

    // Control estricto de IDs únicos de fase para evitar que un solo interruptor sume más de una vez
    private HashSet<int> fasesRestauradasIds = new HashSet<int>();
    public int fasesRestauradas => fasesRestauradasIds != null ? fasesRestauradasIds.Count : 0;

    [Header("Indicadores de Fase (Luces URP 2D)")]
    public Light2D luzFase1;
    public Light2D luzFase2;
    public Light2D luzFase3;
    public Light2D luzAmbientalSubterraneo;

    [Header("Salida y Puertas")]
    public CompuertaSalidaNivel2 compuertaSalida;

    private AudioSource audioFeedback;
    private AudioSource audioLoopCercano;
    private Transform playerTransform;

    // Eventos
    public static event Action OnElectricidadTotalmenteRestaurada;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        fasesRestauradasIds.Clear();

        audioFeedback = GetComponent<AudioSource>();
        if (audioFeedback == null)
        {
            audioFeedback = gameObject.AddComponent<AudioSource>();
            audioFeedback.loop = false; // ZERO loops constantes
            audioFeedback.spatialBlend = 0f; // Estéreo claro
            audioFeedback.playOnAwake = false;
        }

        audioLoopCercano = gameObject.AddComponent<AudioSource>();
        audioLoopCercano.loop = true;
        audioLoopCercano.spatialBlend = 1f; // 3D espacializado
        audioLoopCercano.minDistance = 2.5f;
        audioLoopCercano.maxDistance = 14.0f;
        audioLoopCercano.rolloffMode = AudioRolloffMode.Linear;
        audioLoopCercano.playOnAwake = false;
        audioLoopCercano.volume = 0f;

        BuscarLuzGlobalSiFalta();
        ActualizarLucesFases();
    }

    private void Start()
    {
        BuscarLuzGlobalSiFalta();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Si al menos una fase o el generador está encendido, sonar al estar cerca
        if (fasesRestauradas > 0 || energiaRestauradaTotalmente)
        {
            AudioClip clipLoop = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_GENERADOR);
            if (clipLoop != null && audioLoopCercano != null)
            {
                if (audioLoopCercano.clip != clipLoop)
                {
                    audioLoopCercano.clip = clipLoop;
                }
                if (!audioLoopCercano.isPlaying)
                {
                    audioLoopCercano.Play();
                }

                if (playerTransform != null)
                {
                    float dist = Vector2.Distance(transform.position, playerTransform.position);
                    float factorDist = Mathf.Clamp01(1f - (dist / 14f));
                    float volBase = (energiaRestauradaTotalmente ? 0.95f : 0.60f) * factorDist;
                    audioLoopCercano.volume = Mathf.Lerp(audioLoopCercano.volume, volBase, Time.deltaTime * 6f);
                }
            }
        }
    }

    public void BuscarLuzGlobalSiFalta()
    {
        if (luzAmbientalSubterraneo == null)
        {
            GameObject lgObj = GameObject.Find("Global Light 2D");
            if (lgObj != null) luzAmbientalSubterraneo = lgObj.GetComponent<Light2D>();

            if (luzAmbientalSubterraneo == null)
            {
                var luces = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
                foreach (var l in luces)
                {
                    if (l.lightType == Light2D.LightType.Global)
                    {
                        luzAmbientalSubterraneo = l;
                        break;
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        InterruptorFusibleNivel2.OnFaseElectricaRestaurada += RegistrarFase;
    }

    private void OnDisable()
    {
        InterruptorFusibleNivel2.OnFaseElectricaRestaurada -= RegistrarFase;
    }

    public bool EstaFaseRestaurada(int id)
    {
        return fasesRestauradasIds.Contains(id);
    }

    public void RegistrarFase(int idFase, string nombre)
    {
        if (fasesRestauradasIds.Contains(idFase))
        {
            Debug.LogWarning($"[Generador Nivel 2] La fase {idFase} ({nombre}) ya estaba registrada. Ignorando duplicado.");
            return;
        }

        fasesRestauradasIds.Add(idFase);
        int total = fasesRestauradasIds.Count;
        Debug.Log($"<color=orange>[Generador Nivel 2] Fase registrada: {nombre} ({total}/{fasesRequeridas})</color>");

        ActualizarLucesFases();

        // Sonido de pulso eléctrico puntual y limpio (SIN loop molesto)
        if (audioFeedback != null)
        {
            audioFeedback.pitch = 0.9f + (total * 0.15f);
            audioFeedback.PlayOneShot(GenerarAudioPulsoFase());
        }

        // Solo desbloquear si se han conseguido las 3 fases
        if (total >= fasesRequeridas && !energiaRestauradaTotalmente)
        {
            StartCoroutine(SecuenciaArranqueTotal());
        }
    }

    private void ActualizarLucesFases()
    {
        if (luzFase1 != null) luzFase1.color = fasesRestauradasIds.Contains(1) ? Color.green : Color.red;
        if (luzFase2 != null) luzFase2.color = fasesRestauradasIds.Contains(2) ? Color.green : Color.red;
        if (luzFase3 != null) luzFase3.color = fasesRestauradasIds.Contains(3) ? Color.green : Color.red;
    }

    private IEnumerator SecuenciaArranqueTotal()
    {
        energiaRestauradaTotalmente = true;
        Debug.Log("<color=green>[Generador Nivel 2] ¡3/3 FASES RESTAURADAS! Activando iluminación de emergencia y compuerta de escape...</color>");

        BuscarLuzGlobalSiFalta();

        // Sonido único de encendido triunfal de los transformadores
        if (audioFeedback != null)
        {
            audioFeedback.pitch = 1.0f;
            audioFeedback.PlayOneShot(GenerarAudioArranqueTransformador());
        }

        // Parpadeo dramático de encendido general
        if (luzAmbientalSubterraneo != null)
        {
            for (int i = 0; i < 6; i++)
            {
                luzAmbientalSubterraneo.intensity = (i % 2 == 0) ? 0.38f : 0.06f;
                yield return new WaitForSeconds(0.12f);
            }
            // Luz permanente cálida y brillante de emergencia en TODO el nivel
            luzAmbientalSubterraneo.intensity = 0.50f;
            luzAmbientalSubterraneo.color = new Color(1.0f, 0.88f, 0.65f, 1f);
        }

        // Desbloquear compuerta de escape a Nivel 3
        if (compuertaSalida != null)
        {
            compuertaSalida.DesbloquearCompuerta();
        }

        OnElectricidadTotalmenteRestaurada?.Invoke();
    }

    public void IluminarSubterraneoCompleto()
    {
        BuscarLuzGlobalSiFalta();
        if (luzAmbientalSubterraneo != null)
        {
            luzAmbientalSubterraneo.intensity = 0.60f;
            luzAmbientalSubterraneo.color = new Color(1.0f, 0.92f, 0.75f, 1f);
        }
    }

    private AudioClip GenerarAudioPulsoFase()
    {
        int rate = 22050;
        float dur = 0.35f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float lowClank = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 15f);
            float zap = Mathf.Sin(2f * Mathf.PI * 380f * t) * Mathf.Exp(-t * 20f);
            samples[i] = Mathf.Clamp(lowClank * 0.7f + zap * 0.4f, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create("AudioPulsoFase", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarAudioArranqueTransformador()
    {
        int rate = 22050;
        float dur = 2.4f;
        int total = Mathf.CeilToInt(rate * dur);
        float[] samples = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            float freqSubida = Mathf.Lerp(50f, 220f, t / dur);
            float surge = Mathf.Sin(2f * Mathf.PI * freqSubida * t) * 0.5f;
            float clankInicial = (t < 0.2f) ? Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.5f : 0f;
            float caida = Mathf.Exp(-(t - 1.8f) * 4f);
            if (t < 1.8f) caida = 1f;

            samples[i] = Mathf.Clamp((surge + clankInicial) * caida, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("AudioArranqueTransformador", total, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
