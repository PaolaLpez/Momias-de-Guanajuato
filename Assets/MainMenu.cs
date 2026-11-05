using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestor del Menú Principal de "Momias de Guanajuato: La Noche del Museo".
/// Administra la navegación entre paneles, la música ambiental inmersiva, el control
/// dinámico de volumen (con slider y teclas +/-), efectos de sonido para botones
/// y la salida inmediata del juego tanto en ejecutable como en el Editor de Unity.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Paneles de la Interfaz")]
    [SerializeField] private GameObject menuPrincipalPanel;
    [SerializeField] private GameObject opcionesPanel;
    [SerializeField] private Slider sliderVolumen;

    [Header("Banda Sonora y Efectos de Audio")]
    [SerializeField] private AudioClip musicaInicio;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField, Range(0f, 1f)] private float volumenPredeterminado = 0.8f;

    private AudioClip sfxClick;
    private AudioClip sfxHover;
    private bool cambiandoDeEscena = false;

    private void Awake()
    {
        // 1. Configurar AudioSources
        ConfigurarAudioSource();
        ConfigurarSFX();

        // 2. Conectar fondo animado cinemático
        ConfigurarFondoAnimado();
    }

    private void Start()
    {
        // 3. Auto-recuperación de referencias UI por si no están enlazadas en el inspector
        AutoRecuperarReferenciasUI();

        // Estado inicial de los paneles
        if (opcionesPanel != null) opcionesPanel.SetActive(false);
        if (menuPrincipalPanel != null) menuPrincipalPanel.SetActive(true);

        // 4. Configurar Slider de Volumen
        float volumenGuardado = PlayerPrefs.GetFloat("VolumenMusica", volumenPredeterminado);
        AplicarVolumenGlobal(volumenGuardado);

        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.value = volumenGuardado;
            sliderVolumen.onValueChanged.RemoveAllListeners();
            sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
        }

        // 5. Vincular efectos de sonido de clic y cursor a todos los botones del menú
        VincularSonidosABotones();
    }

    private void Update()
    {
        // Controles rápidos de accesibilidad para subir/bajar volumen con el teclado
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
        {
            float nuevoVol = Mathf.Clamp01(AudioListener.volume + 0.05f);
            CambiarVolumen(nuevoVol);
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            float nuevoVol = Mathf.Clamp01(AudioListener.volume - 0.05f);
            CambiarVolumen(nuevoVol);
        }
    }

    private void AutoRecuperarReferenciasUI()
    {
        if (menuPrincipalPanel == null)
        {
            GameObject mp = GameObject.Find("MenuPrincipal");
            if (mp != null) menuPrincipalPanel = mp;
        }

        if (opcionesPanel == null)
        {
            GameObject op = GameObject.Find("PanelOpciones");
            if (op != null) opcionesPanel = op;
        }

        if (sliderVolumen == null)
        {
            sliderVolumen = FindAnyObjectByType<Slider>();
            if (sliderVolumen == null && opcionesPanel != null)
            {
                sliderVolumen = opcionesPanel.GetComponentInChildren<Slider>(true);
            }
        }
    }

    private void ConfigurarAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // Audio 2D estéreo frontal envolvente

        float volumenActual = PlayerPrefs.GetFloat("VolumenMusica", volumenPredeterminado);
        audioSource.volume = volumenActual;

        // Cargar audio del menú
        if (musicaInicio == null)
        {
            musicaInicio = Resources.Load<AudioClip>("MenuMusic");
            if (musicaInicio == null) musicaInicio = Resources.Load<AudioClip>("Audio/audio_videojuego");
            if (musicaInicio == null) musicaInicio = Resources.Load<AudioClip>("Audio/videojuego_inicio");
            if (musicaInicio == null) musicaInicio = Resources.Load<AudioClip>("videojuego_inicio");
        }

        if (musicaInicio != null)
        {
            audioSource.clip = musicaInicio;
            audioSource.Play();
            Debug.Log("<color=green>[MainMenu] Música ambiental iniciada correctamente.</color>");
        }
        else
        {
            // Cargar directamente desde el disco si no está en Resources
            string rutaWav = Application.dataPath + "/Resources/MenuMusic.wav";
            string rutaAudioWav = Application.dataPath + "/Audio/videojuego_inicio.wav";
            string rutaMp3 = Application.dataPath + "/Audio/audio_videojuego.mp3";

            string rutaFinal = null;
            AudioType audioType = AudioType.WAV;

            if (System.IO.File.Exists(rutaWav)) { rutaFinal = rutaWav; audioType = AudioType.WAV; }
            else if (System.IO.File.Exists(rutaAudioWav)) { rutaFinal = rutaAudioWav; audioType = AudioType.WAV; }
            else if (System.IO.File.Exists(rutaMp3)) { rutaFinal = rutaMp3; audioType = AudioType.MPEG; }

            if (rutaFinal != null)
            {
                StartCoroutine(CargarAudioDesdeArchivo(rutaFinal, audioType));
            }
            else
            {
                musicaInicio = GeneradorMusicaMenu.GenerarMusicaTerror("MomiasGuanajuato_MenuTheme", 30f);
                audioSource.clip = musicaInicio;
                audioSource.Play();
            }
        }
    }

    private IEnumerator CargarAudioDesdeArchivo(string ruta, AudioType audioType)
    {
        string uri = "file:///" + ruta.Replace("\\", "/");
        using (UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(uwr);
                if (clip != null)
                {
                    musicaInicio = clip;
                    if (audioSource != null)
                    {
                        audioSource.clip = musicaInicio;
                        audioSource.Play();
                        Debug.Log("<color=green>[MainMenu] Audio cargado desde archivo con éxito.</color>");
                    }
                    yield break;
                }
            }
        }

        // Respaldo de atmósfera de terror procedural si falla la carga externa
        if (musicaInicio == null && audioSource != null)
        {
            musicaInicio = GeneradorMusicaMenu.GenerarMusicaTerror("MomiasGuanajuato_MenuTheme", 30f);
            audioSource.clip = musicaInicio;
            audioSource.Play();
        }
    }

    private void ConfigurarSFX()
    {
        GameObject sfxObj = new GameObject("MenuSFX");
        sfxObj.transform.SetParent(transform);
        sfxSource = sfxObj.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f; // 2D UI SFX

        sfxClick = Resources.Load<AudioClip>("Audio/button_click");
        if (sfxClick == null) sfxClick = GenerarSonidoClickProcedural();

        sfxHover = Resources.Load<AudioClip>("Audio/button_hover");
        if (sfxHover == null) sfxHover = GenerarSonidoHoverProcedural();
    }

    private void VincularSonidosABotones()
    {
        Button[] botones = FindObjectsByType<Button>();
        foreach (Button btn in botones)
        {
            if (btn == null) continue;

            // Sonido de clic
            btn.onClick.AddListener(ReproducirSonidoClick);

            // Sonido de cursor/hover
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { ReproducirSonidoHover(); });
            trigger.triggers.Add(entry);
        }
    }

    public void ReproducirSonidoClick()
    {
        if (sfxSource != null && sfxClick != null)
        {
            sfxSource.PlayOneShot(sfxClick, 0.85f);
        }
    }

    public void ReproducirSonidoHover()
    {
        if (sfxSource != null && sfxHover != null)
        {
            sfxSource.PlayOneShot(sfxHover, 0.45f);
        }
    }

    public void CambiarVolumen(float nuevoVolumen)
    {
        float volClamped = Mathf.Clamp01(nuevoVolumen);
        AplicarVolumenGlobal(volClamped);

        if (sliderVolumen != null && !Mathf.Approximately(sliderVolumen.value, volClamped))
        {
            sliderVolumen.value = volClamped;
        }

        PlayerPrefs.SetFloat("VolumenMusica", volClamped);
        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[MainMenu] Volumen actualizado a: {Mathf.RoundToInt(volClamped * 100f)}%</color>");
    }

    private void AplicarVolumenGlobal(float vol)
    {
        // Controla el volumen maestro general del juego
        AudioListener.volume = vol;
        if (audioSource != null) audioSource.volume = vol;
    }

    public void Jugar()
    {
        if (cambiandoDeEscena) return;
        cambiandoDeEscena = true;
        ReproducirSonidoClick();
        StartCoroutine(TransicionANivel1());
    }

    private IEnumerator TransicionANivel1()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            float volInicial = audioSource.volume;
            float duracionFade = 0.5f;
            float tiempo = 0f;

            while (tiempo < duracionFade)
            {
                tiempo += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(volInicial, 0f, tiempo / duracionFade);
                yield return null;
            }
        }

        SceneManager.LoadScene("Nivel1");
    }

    public void AbrirOpciones()
    {
        ReproducirSonidoClick();
        if (menuPrincipalPanel != null) menuPrincipalPanel.SetActive(false);
        if (opcionesPanel != null)
        {
            opcionesPanel.SetActive(true);
            // Asegurar que el slider refleje el volumen actual al abrir el panel
            if (sliderVolumen != null)
            {
                sliderVolumen.value = AudioListener.volume;
            }
        }
    }

    public void CerrarOpciones()
    {
        ReproducirSonidoClick();
        if (opcionesPanel != null) opcionesPanel.SetActive(false);
        if (menuPrincipalPanel != null) menuPrincipalPanel.SetActive(true);
    }

    public void Salir()
    {
        Debug.Log("<color=red>[MainMenu] Saliendo del juego...</color>");
        ReproducirSonidoClick();

#if UNITY_EDITOR
        // Detener la ejecución directamente en el Editor de Unity
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Cerrar la aplicación en una build ejecutable (.exe)
        Application.Quit();
#endif
    }

    private void ConfigurarFondoAnimado()
    {
        GameObject fondoObj = GameObject.Find("Fondo");
        if (fondoObj != null)
        {
            FondoMenuAnimado fma = fondoObj.GetComponent<FondoMenuAnimado>();
            if (fma == null)
            {
                fondoObj.AddComponent<FondoMenuAnimado>();
            }
        }
    }

    // Efectos de sonido procedurales góticos para respuesta táctil inmediata
    private AudioClip GenerarSonidoClickProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.12f;
        int count = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-t * 35f);
            // Golpe seco metálico con resonancia en 440Hz y 880Hz
            float tone = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.4f;
            samples[i] = tone * decay * 0.8f;
        }

        AudioClip clip = AudioClip.Create("MenuClickProcedural", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoHoverProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.15f;
        int count = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin((t / duration) * Mathf.PI);
            // Resonancia suave oscura de 220Hz a 180Hz
            float freq = Mathf.Lerp(220f, 180f, t / duration);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            samples[i] = tone * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("MenuHoverProcedural", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
