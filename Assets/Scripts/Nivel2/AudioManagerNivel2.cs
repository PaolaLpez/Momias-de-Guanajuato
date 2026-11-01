using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Gestor centralizado de audio para el Nivel 2.
/// Carga los clips de audio MP3 desde Downloads o Resources/Audio/Nivel2 de forma directa y asncrona.
/// Si un archivo externo no est disponible, provee sntesis procedural de respaldo automtica.
/// CERO dependencias con Nivel 1.
/// </summary>
public class AudioManagerNivel2 : MonoBehaviour
{
    public static AudioManagerNivel2 Instance { get; private set; }

    private static readonly Dictionary<string, AudioClip> cacheClips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

    // Nombres estndar de identificacin
    public const string CLIP_REJAS = "rejas";
    public const string CLIP_CHARCO = "charco";
    public const string CLIP_ENCENDER = "encender";
    public const string CLIP_GENERADOR = "generador";
    public const string CLIP_RATON = "raton";
    public const string CLIP_PERDER = "perder";
    public const string CLIP_ATAQUE = "ataque";
    public const string CLIP_CAMINATA = "caminata";
    public const string CLIP_SONIDO_MOMIA = "sonido_momia";
    public const string CLIP_FONDO = "fondo";
    public const string CLIP_HERIDA = "herida_personaje";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInicializar()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("AudioManagerNivel2");
            go.AddComponent<AudioManagerNivel2>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CargarTodosLosClips();
    }

    public static AudioClip ObtenerClip(string nombreClave)
    {
        if (cacheClips.TryGetValue(nombreClave, out AudioClip clip) && clip != null)
        {
            return clip;
        }

        // Intento rápido desde Resources en sus posibles variantes de ruta
        AudioClip rClip = Resources.Load<AudioClip>("Audio/Nivel2/" + nombreClave);
        if (rClip == null) rClip = Resources.Load<AudioClip>("AudioNivel2/" + nombreClave);
        if (rClip == null) rClip = Resources.Load<AudioClip>(nombreClave);

        if (rClip != null)
        {
            cacheClips[nombreClave] = rClip;
            return rClip;
        }

        return null;
    }

    public void CargarTodosLosClips()
    {
        // Rutas directas en Downloads y Assets
        CargarClip(CLIP_REJAS, @"C:\Users\User\Downloads\rejas.mp3", "rejas");
        CargarClip(CLIP_CHARCO, @"C:\Users\User\Downloads\charco.mp3", "charco");
        CargarClip(CLIP_ENCENDER, @"C:\Users\User\Downloads\encender.mp3", "encender");
        CargarClip(CLIP_GENERADOR, @"C:\Users\User\Downloads\generador.mp3", "generador");
        CargarClip(CLIP_RATON, @"C:\Users\User\Downloads\raton.mp3", "raton");
        CargarClip(CLIP_PERDER, @"C:\Users\User\Downloads\perder.mp3", "perder");
        CargarClip(CLIP_ATAQUE, @"C:\Users\User\Downloads\ataque.mp3", "ataque");
        CargarClip(CLIP_CAMINATA, @"C:\Users\User\Downloads\caminata.mp3", "caminata");
        CargarClip(CLIP_SONIDO_MOMIA, @"C:\Users\User\Downloads\sonido momia.mp3", "sonido_momia");
        CargarClip(CLIP_FONDO, @"C:\Users\User\Downloads\fondo.mp3", "fondo");
        CargarClip(CLIP_HERIDA, @"C:\Users\User\Downloads\herida personaje.mp3", "herida_personaje");
    }

    private void CargarClip(string clave, string rutaArchivoDirecta, string nombreRecurso)
    {
        // Primero intentar Resources si ya fue importado
        AudioClip rClip = Resources.Load<AudioClip>("Audio/Nivel2/" + nombreRecurso);
        if (rClip == null) rClip = Resources.Load<AudioClip>("AudioNivel2/" + nombreRecurso);
        if (rClip == null) rClip = Resources.Load<AudioClip>(nombreRecurso);

        if (rClip != null)
        {
            rClip.LoadAudioData();
            cacheClips[clave] = rClip;
            if (clave == CLIP_CHARCO)
            {
                TrampaAcusticaNivel2.ActualizarClipCharcoGlobal(rClip);
            }
        }

        // Siempre intentar cargar desde el archivo físico directo para garantizar la fidelidad del archivo indicado
        if (File.Exists(rutaArchivoDirecta))
        {
            StartCoroutine(CargarAudioWebRequest(clave, rutaArchivoDirecta));
        }
        else
        {
            string fallbackAssets = Path.Combine(Application.dataPath, "Audio", "Nivel2", Path.GetFileName(rutaArchivoDirecta));
            if (File.Exists(fallbackAssets))
            {
                StartCoroutine(CargarAudioWebRequest(clave, fallbackAssets));
            }
        }
    }

    private IEnumerator CargarAudioWebRequest(string clave, string rutaArchivo)
    {
        string uri = "file:///" + rutaArchivo.Replace("\\", "/");
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    clip.name = clave;
                    clip.LoadAudioData();
                    cacheClips[clave] = clip;
                    if (clave == CLIP_CHARCO)
                    {
                        TrampaAcusticaNivel2.ActualizarClipCharcoGlobal(clip);
                    }
                }
            }
        }
    }
}
