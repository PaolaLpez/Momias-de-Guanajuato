using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor General del Nivel 4 (Nivel Final / Clímax): "El Panteón de Santa Paula y la Campana del Alba".
/// Administra el reloj de 03:00 AM a 06:00 AM, la recolección de las 3 reliquias sagradas,
/// la consagración del altar, el toque de la campana mayor, el amanecer cinemático,
/// las sacudidas de pantalla por el grito paralizante, la crónica histórica de Carmen "La Bruja" y la victoria final.
/// </summary>
public class Nivel4Manager : MonoBehaviour
{
    public static Nivel4Manager Instance { get; private set; }

    [Header("Estado del Nivel")]
    public bool JuegoTerminado = false;
    public bool NivelCompletado = false;
    public bool AltarConsagrado = false;
    public bool CampanaTocada = false;
    public int ReliquiasRecolectadas = 0;
    public bool ModalLoreAbierto = false;
    public bool MenuConfigAbierto { get; private set; } = false;

    [Header("Reliquias Específicas")]
    public bool TieneCrucifijo = false;
    public bool TieneEslabon = false;
    public bool TieneLibro = false;

    private bool animacionCampanaEnCurso = false;
    private bool entradaCapillaAnimada = false;
    private bool avisoTiempoAgotadoMostrado = false;
    private bool avisoMomiasMuseoMostrado = false;
    private bool momiasInvasorasDespertadas = false;

    [Header("Reloj de la Madrugada (03:00 AM a 06:00 AM)")]
    public float duracionNivelSegundos = 300f; // 5 minutos reales = transición hacia las 6:00 AM
    private float tiempoTranscurrido = 0f;
    private string textoReloj = "03:00 AM";
    public int HoraActual { get; private set; } = 3;
    public int MinutoActual { get; private set; } = 0;
    public int TotalMinutosActual { get; private set; } = 180;

    [Header("Micrófono y Detección de Ruido")]
    public float umbralRuidoMic = 0.34f;
    private class CanalMicNivel4
    {
        public string nombre;
        public AudioClip clip;
    }
    private readonly List<CanalMicNivel4> canalesMic = new List<CanalMicNivel4>();
    private readonly float[] bufferMic = new float[128];
    private float nivelRuidoActual = 0f;
    public float NivelRuidoActual => nivelRuidoActual;
    private bool micInicializado = false;
    private float pisoRuidoAdaptativoMic = 0.015f;
    private float sensibilidadMic = 0.72f;
    private float timerReintentoMic = 0f;
    private float timerDeteccionMicCooldown = 0f;
    private int indiceCanalMicForzado = -1; // -1 = Automático (todos)
    private string nombreDispositivoMicActivo = "Detectando...";
    private float volumenAmbiente = 0.48f;

    [Header("Iluminación y Amanecer")]
    public Light2D luzGlobalPanteon;
    private Color colorNoche = new Color(0.08f, 0.10f, 0.18f, 1f); // Azul frío de medianoche
    private Color colorAmanecer = new Color(1.0f, 0.82f, 0.65f, 1f); // Oro y rosa del alba sobre Guanajuato
    private float progresoAmanecer = 0f;

    [Header("Sacudida de Pantalla (Screen Shake)")]
    private float timerSacudida = 0f;
    private float intensidadSacudida = 0f;
    private Vector2 offsetShakeGUI = Vector2.zero;

    [Header("Jumpscare de Momia Encadenada")]
    private bool jumpscareActivo = false;
    private float timerJumpscare = 0f;
    private const float DURACION_JUMPSCARE = 1.85f;
    private string asesinaJumpscare = "Carmen";
    private Texture2D texJumpscareBruja;
    private Texture2D texJumpscareMomia1;
    private Texture2D texJumpscareMomia2;

    [Header("Transición Interior Capilla")]
    public bool enInteriorCapilla = false;
    public float fadeInteriorAlpha = 0f;

    [Header("Secuencia Final Guanajuato")]
    private Texture2D texFinalGuanajuato;
    private float timerAnimacionFinal = 0f;

    [Header("Audio")]
    private AudioSource audioAmbiente;
    private AudioSource audioSfx;
    private AudioClip clipViento;
    private AudioClip clipCampana;

    private string bannerAvisoTexto = "";
    private float bannerAvisoTimer = 0f;
    private string anuncioCentroTitulo = "";
    private string anuncioCentroSubtitulo = "";
    private float anuncioCentroTimer = 0f;
    private float anuncioCentroDuracion = 1f;
    private MomiaNivel2Seccion momiaNivel2Cache;
    private float timerAvisoMicCooldown = 0f;
    private Vector2 scrollPosLore = Vector2.zero;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        ConfigurarAudios();
        ConfigurarMicrofono();
        CargarTexturas();
    }

    private void OnDestroy()
    {
        DetenerMicrofono();
    }

    private void DetenerMicrofono()
    {
        try
        {
            for (int i = 0; i < canalesMic.Count; i++)
            {
                string dev = canalesMic[i].nombre == "Predeterminado" ? null : canalesMic[i].nombre;
                if (Microphone.IsRecording(dev))
                {
                    Microphone.End(dev);
                }
            }
            canalesMic.Clear();
            micInicializado = false;
        }
        catch { }
    }

    private void Start()
    {
        BuscarLuzGlobal();
        MostrarAviso("PANTEÓN DE SANTA PAULA (03:00 AM) — Reúne las 3 Reliquias antes del Alba [M: Mapa]", 7.0f);
    }

    private void ConfigurarAudios()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenMusica", 1f));
        volumenAmbiente = Mathf.Clamp(PlayerPrefs.GetFloat("Nivel4_VolumenAmbiente", 0.48f), 0f, 1f);

        audioAmbiente = gameObject.AddComponent<AudioSource>();
        audioAmbiente.loop = true;
        audioAmbiente.volume = volumenAmbiente;
        audioAmbiente.spatialBlend = 0f;

        audioSfx = gameObject.AddComponent<AudioSource>();
        audioSfx.spatialBlend = 0f;

        SilenciarMusicaExternaPersistente();

        if (AudioManagerNivel4.Instance == null)
        {
            GameObject amGo = new GameObject("AudioManagerNivel4");
            amGo.AddComponent<AudioManagerNivel4>();
        }

        clipViento = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_FONDO_4);
        clipCampana = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMPANA);

        if (clipViento != null)
        {
            audioAmbiente.clip = clipViento;
            audioAmbiente.Play();
        }
        else if (AudioManagerNivel4.Instance != null)
        {
            AudioManagerNivel4.Instance.CargarClipExternoSiFalta(
                AudioManagerNivel4.CLIP_FONDO_4,
                @"C:\Users\User\Downloads\fondo 4.mp3",
                clip =>
                {
                    if (clip != null && audioAmbiente != null)
                    {
                        clipViento = clip;
                        audioAmbiente.clip = clip;
                        audioAmbiente.Play();
                    }
                });
        }
    }

    private void SilenciarMusicaExternaPersistente()
    {
        AudioSource[] todasLasFuentes = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        string nombreEscenaActual = gameObject.scene.name;
        foreach (AudioSource src in todasLasFuentes)
        {
            if (src == null || src == audioAmbiente || src == audioSfx) continue;
            if (AudioManagerNivel4.Instance != null && src.gameObject == AudioManagerNivel4.Instance.gameObject) continue;
            if (src.gameObject.scene.name != nombreEscenaActual)
            {
                src.Stop();
                src.volume = 0f;
            }
        }
    }

    private void ConfigurarMicrofono()
    {
        DetenerMicrofono();
        sensibilidadMic = Mathf.Clamp(PlayerPrefs.GetFloat("Nivel2_MicSensibilidad_V2", 0.72f), 0.15f, 0.95f);
        umbralRuidoMic = Mathf.Lerp(0.54f, 0.26f, sensibilidadMic);
        try
        {
            string[] dispositivos = Microphone.devices;
            if (dispositivos != null && dispositivos.Length > 0)
            {
                int maxDev = Mathf.Min(dispositivos.Length, 4);
                for (int i = 0; i < maxDev; i++)
                {
                    string dev = dispositivos[i];
                    if (string.IsNullOrEmpty(dev)) continue;
                    try
                    {
                        if (Microphone.IsRecording(dev)) Microphone.End(dev);
                        AudioClip clip = Microphone.Start(dev, true, 3, 16000);
                        if (clip != null)
                        {
                            canalesMic.Add(new CanalMicNivel4 { nombre = dev, clip = clip });
                        }
                    }
                    catch { }
                }
            }

            if (canalesMic.Count == 0)
            {
                AudioClip clipDef = Microphone.Start(null, true, 3, 16000);
                if (clipDef != null)
                {
                    canalesMic.Add(new CanalMicNivel4 { nombre = "Predeterminado", clip = clipDef });
                }
            }

            micInicializado = canalesMic.Count > 0;
            nombreDispositivoMicActivo = micInicializado
                ? (indiceCanalMicForzado >= 0 && indiceCanalMicForzado < canalesMic.Count
                    ? canalesMic[indiceCanalMicForzado].nombre
                    : $"Automático ({canalesMic[0].nombre})")
                : "Sin micrófono";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Nivel4Manager] Micrófono no disponible: " + e.Message);
            micInicializado = false;
            nombreDispositivoMicActivo = "Micrófono no disponible";
        }
    }

    public void AlternarMenuConfiguracion()
    {
        MenuConfigAbierto = !MenuConfigAbierto;
        Time.timeScale = MenuConfigAbierto ? 0f : 1f;
    }

    public void CambiarSensibilidadMic(float nuevaSens)
    {
        sensibilidadMic = Mathf.Clamp(nuevaSens, 0.15f, 0.95f);
        umbralRuidoMic = Mathf.Lerp(0.54f, 0.26f, sensibilidadMic);
        PlayerPrefs.SetFloat("Nivel2_MicSensibilidad_V2", sensibilidadMic);
        PlayerPrefs.Save();
    }

    public void CambiarDispositivoMic()
    {
        if (canalesMic.Count == 0)
        {
            ConfigurarMicrofono();
            return;
        }
        indiceCanalMicForzado++;
        if (indiceCanalMicForzado >= canalesMic.Count) indiceCanalMicForzado = -1;
        nombreDispositivoMicActivo = (indiceCanalMicForzado >= 0 && indiceCanalMicForzado < canalesMic.Count)
            ? canalesMic[indiceCanalMicForzado].nombre
            : $"Automático ({canalesMic[0].nombre})";
    }

    private void CargarTexturas()
    {
        Sprite spScream = Resources.Load<Sprite>("Momia4/momia4_scream");
        if (spScream != null)
        {
            texJumpscareBruja = spScream.texture;
        }

        Sprite spM1 = Resources.Load<Sprite>("Momia/momia_scream_3");
        if (spM1 == null) spM1 = Resources.Load<Sprite>("Momia/momia_attack");
        if (spM1 != null) texJumpscareMomia1 = spM1.texture;

        Sprite spM2 = Resources.Load<Sprite>("Momia2/momia2_pose0_scream");
        if (spM2 == null) spM2 = Resources.Load<Sprite>("Momia2/momia2_scream_2");
        if (spM2 != null) texJumpscareMomia2 = spM2.texture;

        texFinalGuanajuato = Resources.Load<Texture2D>("Cementerio/final_victoria_guanajuato");
        if (texFinalGuanajuato == null)
        {
            Sprite spFin = Resources.Load<Sprite>("Cementerio/final_victoria_guanajuato");
            if (spFin != null) texFinalGuanajuato = spFin.texture;
        }
    }

    private void BuscarLuzGlobal()
    {
        Light2D[] luces = FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        string escenaActual = gameObject.scene.name;
        luzGlobalPanteon = null;

        foreach (var l in luces)
        {
            if (l != null && l.lightType == Light2D.LightType.Global && l.gameObject.scene.name == escenaActual)
            {
                if (luzGlobalPanteon == null || l.gameObject.name == "Global Light 2D")
                {
                    luzGlobalPanteon = l;
                }
            }
        }

        foreach (var l in luces)
        {
            if (l != null && l != luzGlobalPanteon && l.lightType == Light2D.LightType.Global)
            {
                l.enabled = false;
                Destroy(l.gameObject);
            }
        }

        if (luzGlobalPanteon != null)
        {
            if (!luzGlobalPanteon.gameObject.activeSelf) luzGlobalPanteon.gameObject.SetActive(true);
            if (!luzGlobalPanteon.enabled) luzGlobalPanteon.enabled = true;
            luzGlobalPanteon.color = colorNoche;
            luzGlobalPanteon.intensity = 0.12f;
        }
    }

    private void CargarEscenaLimpia(string nombreEscena)
    {
        Time.timeScale = 1f;
        DetenerMicrofono();
        Light2D[] luces = FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in luces)
        {
            if (l != null && l.lightType == Light2D.LightType.Global)
            {
                l.enabled = false;
            }
        }
        SceneManager.LoadScene(nombreEscena);
    }

    private void Update()
    {
        if (JuegoTerminado || NivelCompletado) return;

        // Menú de Configuración y Audio con [ESC]
        if (MenuConfigAbierto)
        {
            ActualizarLecturaMicrofono(false);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                AlternarMenuConfiguracion();
            }
            return;
        }

        if (!ModalLoreAbierto && Input.GetKeyDown(KeyCode.Escape))
        {
            AlternarMenuConfiguracion();
            return;
        }

        // Control de atajo para abrir la Crónica de Lore con L o Tab
        if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Tab))
        {
            ModalLoreAbierto = !ModalLoreAbierto;
        }

        // Control del mapa completo con [M]: bloqueado hasta encontrar 2 reliquias
        bool mapaDesbloqueado = ReliquiasRecolectadas >= 2 && !enInteriorCapilla;
        CameraFollow cf = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : FindFirstObjectByType<CameraFollow>();
        if (cf != null)
        {
            cf.permitirTeclaM = mapaDesbloqueado;
        }

        if (Input.GetKeyDown(KeyCode.M) && !mapaDesbloqueado && !enInteriorCapilla)
        {
            MostrarAviso($"🔒 El mapa [M] se desbloquea al encontrar 2 reliquias ({ReliquiasRecolectadas}/2).", 3.0f);
        }

        if (ModalLoreAbierto)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                ModalLoreAbierto = false;
            }
            return;
        }

        // 1. Progresión del Reloj de 03:00 AM a 06:00 AM (avanza estrictamente por tiempo real sin saltos por reliquia)
        tiempoTranscurrido += Time.deltaTime;
        float pTiempo = Mathf.Clamp01(tiempoTranscurrido / duracionNivelSegundos);
        float p = (CampanaTocada || NivelCompletado) ? 1.0f : pTiempo;

        // De 03:00 a 06:00 son 180 minutos
        int totalMinutos = 3 * 60 + Mathf.FloorToInt(p * 180f);
        int horas = totalMinutos / 60;
        int minutos = totalMinutos % 60;
        TotalMinutosActual = totalMinutos;
        HoraActual = horas;
        MinutoActual = minutos;
        textoReloj = string.Format("{0:D2}:{1:D2} AM", horas, minutos);

        // Anuncio a las 04:00 AM cuando salen las momias del museo hacia el Oeste y Este
        if (horas >= 4 && !avisoMomiasMuseoMostrado)
        {
            avisoMomiasMuseoMostrado = true;
            MostrarAnuncioCentro(
                "⏰ 04:00 AM — ¡LAS MOMIAS HAN SALIDO DEL MUSEO!",
                "Ahora acechan en el Sector Oeste (Mausoleo) y el Sector Este (Gran Osario)",
                6.5f);
            SacudirPantalla(0.65f, 14f);
        }

        // Si se acaba el tiempo (06:00 AM), el jugador NO pierde automáticamente.
        // La noche entra en la "Hora Crítica": hasta que no llegue a la capilla y toque la campana, todo está en peligro.
        if (pTiempo >= 1.0f && !CampanaTocada && !avisoTiempoAgotadoMostrado)
        {
            avisoTiempoAgotadoMostrado = true;
            MostrarAviso("¡06:00 AM! ¡EL TIEMPO SE HA AGOTADO! ¡LLEGA A LA CAPILLA Y TOCA LA CAMPANA O ESTARÁS PERDIDO!", 6.0f);
            SacudirPantalla(0.85f, 20f);
        }

        // 2. Efecto de Amanecer Gradual y Niebla Dinámica (la luz y el cielo avanzan segundo a segundo poco a poco)
        ActualizarEfectoHoraViva(p);

        // 3. El amanecer pleno y la victoria SOLO se desatan cuando el jugador TOCA LA CAMPANA
        // (Nunca automáticamente por el simple paso del tiempo)
        if (CampanaTocada && !NivelCompletado)
        {
            progresoAmanecer = Mathf.MoveTowards(progresoAmanecer, 1.0f, Time.deltaTime * 0.40f);
            AplicarAmanecer(progresoAmanecer);
        }

        // 4. Actualización de Sacudida de Pantalla
        if (timerSacudida > 0f)
        {
            timerSacudida -= Time.deltaTime;
            offsetShakeGUI = new Vector2(Random.Range(-intensidadSacudida, intensidadSacudida), Random.Range(-intensidadSacudida, intensidadSacudida));
        }
        else
        {
            offsetShakeGUI = Vector2.zero;
        }

        if (bannerAvisoTimer > 0f)
        {
            bannerAvisoTimer -= Time.deltaTime;
        }
        if (anuncioCentroTimer > 0f)
        {
            anuncioCentroTimer -= Time.deltaTime;
        }
        if (timerAvisoMicCooldown > 0f)
        {
            timerAvisoMicCooldown -= Time.deltaTime;
        }

        // 5. Lectura de micrófono EXCLUSIVA para la zona de Momia 2 ("La Que Escucha") desde las 04:00 AM
        if (EstaJugadorEnZonaMomia2())
        {
            ActualizarLecturaMicrofono(true);
        }
        else
        {
            nivelRuidoActual = 0f;
        }
    }

    public bool EstaJugadorEnZonaMomia2()
    {
        if (JuegoTerminado || NivelCompletado || enInteriorCapilla) return false;
        if (VigilanteNivel4.Instance == null || VigilanteNivel4.Instance.EstaOculta || VigilanteNivel4.Instance.EstaMuerto) return false;

        if (momiaNivel2Cache == null)
        {
            momiaNivel2Cache = FindFirstObjectByType<MomiaNivel2Seccion>();
        }
        if (momiaNivel2Cache == null || !momiaNivel2Cache.HaDespertado || momiaNivel2Cache.Petrificada) return false;

        Vector3 p = VigilanteNivel4.Instance.transform.position;
        return p.x >= 9f && p.x <= 37f && p.y >= -15f && p.y <= 16f;
    }

    public void MostrarAnuncioCentro(string titulo, string subtitulo, float duracion)
    {
        anuncioCentroTitulo = titulo;
        anuncioCentroSubtitulo = subtitulo;
        anuncioCentroTimer = duracion;
        anuncioCentroDuracion = Mathf.Max(0.1f, duracion);
    }

    public void SacudirPantalla(float duracion, float intensidad)
    {
        timerSacudida = duracion;
        intensidadSacudida = intensidad;
    }

    private void ActualizarLecturaMicrofono(bool emitirAlerta)
    {
        float dt = Time.unscaledDeltaTime;
        if (!micInicializado || canalesMic.Count == 0)
        {
            timerReintentoMic += dt;
            if (timerReintentoMic >= 3.5f)
            {
                timerReintentoMic = 0f;
                ConfigurarMicrofono();
            }
            return;
        }

        if (timerDeteccionMicCooldown > 0f) timerDeteccionMicCooldown -= dt;

        float mejorBruto = 0f;
        string mejorNombre = canalesMic[0].nombre;
        int inicioC = (indiceCanalMicForzado >= 0 && indiceCanalMicForzado < canalesMic.Count) ? indiceCanalMicForzado : 0;
        int finC = (indiceCanalMicForzado >= 0 && indiceCanalMicForzado < canalesMic.Count) ? indiceCanalMicForzado + 1 : canalesMic.Count;

        for (int c = inicioC; c < finC; c++)
        {
            CanalMicNivel4 canal = canalesMic[c];
            if (canal == null || canal.clip == null || canal.clip.samples <= bufferMic.Length) continue;
            string devQuery = canal.nombre == "Predeterminado" ? null : canal.nombre;
            int micPos = Microphone.GetPosition(devQuery);
            if (micPos <= 0) continue;

            int posInicio = Mathf.Clamp(micPos - bufferMic.Length, 0, canal.clip.samples - bufferMic.Length);
            if (!canal.clip.GetData(bufferMic, posInicio)) continue;

            float sumSq = 0f;
            float picoMax = 0f;
            for (int i = 0; i < bufferMic.Length; i++)
            {
                float s = Mathf.Abs(bufferMic[i]);
                sumSq += s * s;
                if (s > picoMax) picoMax = s;
            }
            float rms = Mathf.Sqrt(sumSq / bufferMic.Length);
            float nivelCanal = Mathf.Max(rms * 2.8f, picoMax * 0.85f);
            if (nivelCanal > mejorBruto)
            {
                mejorBruto = nivelCanal;
                mejorNombre = canal.nombre;
            }
        }
        if (indiceCanalMicForzado < 0 && mejorBruto > 0.005f)
        {
            nombreDispositivoMicActivo = mejorNombre;
        }

        // Filtro adaptativo del piso de estática ambiental del micrófono (igual que Nivel 2)
        if (mejorBruto < pisoRuidoAdaptativoMic + 0.012f)
        {
            pisoRuidoAdaptativoMic = Mathf.Lerp(pisoRuidoAdaptativoMic, Mathf.Max(0.015f, mejorBruto * 1.2f), dt * 4f);
        }
        else
        {
            pisoRuidoAdaptativoMic = Mathf.Lerp(pisoRuidoAdaptativoMic, Mathf.Max(0.015f, mejorBruto * 0.65f), dt * 0.5f);
        }

        float senalLimpia = Mathf.Max(0f, mejorBruto - Mathf.Max(0.015f, pisoRuidoAdaptativoMic));
        float ganancia = Mathf.Lerp(2.8f, 9.5f, sensibilidadMic);
        float objetivoNorm = Mathf.Clamp01(senalLimpia * ganancia);
        float velLerp = objetivoNorm > nivelRuidoActual ? 18f : 8f;
        nivelRuidoActual = Mathf.Lerp(nivelRuidoActual, objetivoNorm, dt * velLerp);
        if (nivelRuidoActual < 0.01f) nivelRuidoActual = 0f;

        if (!emitirAlerta) return;

        // Si el jugador hace ruido por micrófono en el Sector Este, alerta únicamente a Momia 2 ("La Que Escucha")
        if (nivelRuidoActual > umbralRuidoMic && timerDeteccionMicCooldown <= 0f)
        {
            timerDeteccionMicCooldown = 0.9f;
            if (momiaNivel2Cache == null)
            {
                momiaNivel2Cache = FindFirstObjectByType<MomiaNivel2Seccion>();
            }
            if (momiaNivel2Cache != null && VigilanteNivel4.Instance != null)
            {
                momiaNivel2Cache.NotificarRuido(VigilanteNivel4.Instance.transform.position, 24f, 2.5f);
                if (timerAvisoMicCooldown <= 0f)
                {
                    timerAvisoMicCooldown = 1.6f;
                    MostrarAviso("🎙 ¡TU VOZ RESONÓ EN EL GRAN OSARIO! LA QUE ESCUCHA SE ACERCA", 2.0f);
                }
            }
        }
    }

    public void RecolectarReliquia(int id, string nombre)
    {
        if (id == 1) TieneCrucifijo = true;
        else if (id == 2) TieneEslabon = true;
        else if (id == 3) TieneLibro = true;

        ReliquiasRecolectadas++;
        MostrarAviso("¡RELIQUIA ENCONTRADA: " + nombre.ToUpper() + "!", 3.5f);
        SacudirPantalla(0.45f, 12f);

        // CLÍMAX AL RECOGER 2 RELIQUIAS: Ruptura del sello e invasión de las momias de los niveles anteriores
        if (ReliquiasRecolectadas == 2 && !momiasInvasorasDespertadas)
        {
            DespertarMomiasCatacumbas();
        }

        // AL RECOGER LAS 3 RELIQUIAS: Apertura sagrada de la Capilla
        if (ReliquiasRecolectadas >= 3)
        {
            PuertaCapillaNivel4 puerta = FindFirstObjectByType<PuertaCapillaNivel4>();
            if (puerta != null)
            {
                puerta.AbrirPuertas();
            }
            AltarConsagrado = true; // El requisito del altar queda superado automáticamente
            MostrarAviso("¡EL SELLO DE 1865 HA CAÍDO! Las puertas de la Capilla se abren. ¡Entra y toca la Campana del Alba!", 6.0f);
            SacudirPantalla(0.9f, 22f);
        }
    }

    public void DespertarMomiasCatacumbas()
    {
        momiasInvasorasDespertadas = true;
        MostrarAviso("¡2 RELIQUIAS REUNIDAS! ¡Mapa [M] DESBLOQUEADO — Solo falta 1 Reliquia para abrir la Capilla!", 5.0f);
        SacudirPantalla(0.8f, 15f);
    }

    private void ActualizarEfectoHoraViva(float p)
    {
        if (NivelCompletado) return;

        // Gradiente continuo de amanecer: la luz y el cielo avanzan suavemente de 03:00 AM a 06:00 AM
        // p = 0.0 (03:00 AM): Azul gélido de noche cerrada
        // p = 0.35 (04:03 AM): Azul marino profundo
        // p = 0.65 (04:57 AM): Violeta espectral previo al alba
        // p = 0.85 (05:33 AM): Tinte violeta-cálido del amanecer
        // p = 1.0 (06:00 AM / Campana): Destellos dorados del alba sobre Guanajuato

        Color col0 = new Color(0.03f, 0.04f, 0.08f, 1f); // Noche cerrada
        Color col1 = new Color(0.06f, 0.08f, 0.16f, 1f); // Penumbra azul marina
        Color col2 = new Color(0.16f, 0.09f, 0.22f, 1f); // Violeta espectral
        Color col3 = new Color(0.34f, 0.18f, 0.28f, 1f); // Crepúsculo matutino
        Color col4 = new Color(0.96f, 0.78f, 0.55f, 1f); // Amanecer dorado

        Color targetSky;
        Color targetLight;
        if (p < 0.35f)
        {
            float t = p / 0.35f;
            targetSky = Color.Lerp(col0, col1, t);
            targetLight = Color.Lerp(new Color(0.08f, 0.10f, 0.18f, 1f), new Color(0.12f, 0.15f, 0.24f, 1f), t);
        }
        else if (p < 0.70f)
        {
            float t = (p - 0.35f) / 0.35f;
            targetSky = Color.Lerp(col1, col2, t);
            targetLight = Color.Lerp(new Color(0.12f, 0.15f, 0.24f, 1f), new Color(0.24f, 0.16f, 0.30f, 1f), t);
        }
        else if (p < 0.95f)
        {
            float t = (p - 0.70f) / 0.25f;
            targetSky = Color.Lerp(col2, col3, t);
            targetLight = Color.Lerp(new Color(0.24f, 0.16f, 0.30f, 1f), new Color(0.48f, 0.30f, 0.38f, 1f), t);
        }
        else
        {
            float t = (p - 0.95f) / 0.05f;
            targetSky = Color.Lerp(col3, col4, t);
            targetLight = Color.Lerp(new Color(0.48f, 0.30f, 0.38f, 1f), new Color(1.0f, 0.82f, 0.65f, 1f), t);
        }

        // La intensidad de la luz va subiendo poco a poco de forma continua
        // De 0.16f en noche cerrada (04:15 AM) hasta 0.80f antes de las 6:00 AM, y 1.25f con el toque de campana
        float baseIntensity = Mathf.Lerp(0.16f, 0.80f, p);
        if (CampanaTocada)
        {
            baseIntensity = Mathf.Lerp(baseIntensity, 1.25f, progresoAmanecer);
            targetSky = Color.Lerp(targetSky, col4, progresoAmanecer);
            targetLight = Color.Lerp(targetLight, colorAmanecer, progresoAmanecer);
        }

        CameraFollow cfMap = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : FindFirstObjectByType<CameraFollow>();
        bool enVistaMapa = cfMap != null && cfMap.EstaViendoMapaCompleto();
        if (enVistaMapa)
        {
            baseIntensity = Mathf.Max(baseIntensity, 0.88f);
        }

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, targetSky, Time.deltaTime * 2.5f);
        }

        if (luzGlobalPanteon != null)
        {
            luzGlobalPanteon.color = Color.Lerp(luzGlobalPanteon.color, targetLight, Time.deltaTime * 2.5f);
            luzGlobalPanteon.intensity = Mathf.Lerp(luzGlobalPanteon.intensity, baseIntensity, Time.deltaTime * 2.5f);
        }
    }

    public void InteractuarConAltar()
    {
        // El paso del altar ha sido eliminado a petición del diseño: el objetivo directo es la Campana
        TocarCampanaMayor();
    }

    /// <summary>
    /// Transición cinemática al cruzar el portal exterior de la Capilla hacia el Recinto Sagrado Interior.
    /// </summary>
    public void EntrarACapillaInterior()
    {
        if (enInteriorCapilla) return;
        StartCoroutine(SecuenciaTransicionInteriorCapilla());
    }

    private System.Collections.IEnumerator SecuenciaTransicionInteriorCapilla()
    {
        enInteriorCapilla = true;

        // 1. Fundido suave a negro
        float tFade = 0f;
        while (tFade < 0.45f)
        {
            tFade += Time.unscaledDeltaTime;
            fadeInteriorAlpha = Mathf.Clamp01(tFade / 0.45f);
            yield return null;
        }
        fadeInteriorAlpha = 1.0f;

        AudioClip clipPuerta = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_PUERTA);
        if (clipPuerta != null && audioSfx != null)
        {
            audioSfx.PlayOneShot(clipPuerta, 1.0f);
        }

        // 2. Teletransportar al vigilante dentro de la Capilla Interior (Y = 152f)
        VigilanteNivel4 vig = VigilanteNivel4.Instance;
        if (vig != null)
        {
            vig.transform.position = new Vector3(0f, 152f, 0f);
            Rigidbody2D rb = vig.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // 3. Ajustar límites de cámara y posición para el recinto interior
        CameraFollow cf = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cf != null)
        {
            cf.minX = -5.0f;
            cf.maxX = 5.0f;
            cf.minY = 149f;
            cf.maxY = 168f;
            cf.zoomCercano = 4.2f;
        }
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 152f, -10f);
        }

        yield return new WaitForSecondsRealtime(0.25f);

        // 4. Fundido de vuelta desde el negro
        tFade = 0f;
        while (tFade < 0.50f)
        {
            tFade += Time.unscaledDeltaTime;
            fadeInteriorAlpha = Mathf.Clamp01(1.0f - (tFade / 0.50f));
            yield return null;
        }
        fadeInteriorAlpha = 0f;

        MostrarAviso("★ ¡ESTÁS EN LA CAPILLA MAYOR! ¡ALCANZA LA CUERDA Y TOCA LA CAMPANA DEL ALBA! ★", 6.0f);
    }

    /// <summary>
    /// Secuencia animada de entrada al cruzar el portal de la Capilla Mayor.
    /// La vigilante avanza hacia la nave y la cámara enfoca el campanario sagrado.
    /// </summary>
    public void EjecutarAnimacionEntradaCapilla()
    {
        if (entradaCapillaAnimada || CampanaTocada) return;
        entradaCapillaAnimada = true;
        StartCoroutine(SecuenciaEntradaCapilla());
    }

    private System.Collections.IEnumerator SecuenciaEntradaCapilla()
    {
        VigilanteNivel4 vig = VigilanteNivel4.Instance;
        if (vig == null) yield break;

        MostrarAviso("¡HAS ENTRADO A LA CAPILLA MAYOR! ¡ALCANZA LA CUERDA Y TOCA LA CAMPANA DEL ALBA!", 5.0f);

        AudioClip chime = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_RELIQUIA);
        if (chime != null && audioSfx != null) audioSfx.PlayOneShot(chime, 0.9f);

        Rigidbody2D rb = vig.GetComponent<Rigidbody2D>();
        float timer = 0f;
        while (timer < 1.1f)
        {
            timer += Time.deltaTime;
            if (rb != null) rb.linearVelocity = new Vector2(0f, 2.8f); // Da unos pasos adelante hacia el centro de la nave
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    public void TocarCampanaMayor()
    {
        if (CampanaTocada || animacionCampanaEnCurso) return;

        if (ReliquiasRecolectadas < 3)
        {
            MostrarAviso($"Necesitas reunir las 3 Reliquias Sagradas para bendecir y tañer la Campana ({ReliquiasRecolectadas}/3).", 3.5f);
            return;
        }

        StartCoroutine(SecuenciaAnimacionCampanaAlba());
    }

    private System.Collections.IEnumerator SecuenciaAnimacionCampanaAlba()
    {
        animacionCampanaEnCurso = true;
        CampanaTocada = true;

        // 1. Congelar al jugador y enfocar la acción
        VigilanteNivel4 vig = VigilanteNivel4.Instance;
        Rigidbody2D rbVig = vig != null ? vig.GetComponent<Rigidbody2D>() : null;
        if (rbVig != null) rbVig.linearVelocity = Vector2.zero;

        GeneradorPanteonNivel4 gen = FindFirstObjectByType<GeneradorPanteonNivel4>();
        Transform trCuerda = gen != null ? gen.transformCuerdaCampana : null;
        Transform trCampana = gen != null ? gen.transformCampanaMayor : null;

        // Posicionar a la vigilante en la cuerda
        if (vig != null && trCampana != null)
        {
            vig.transform.position = trCampana.position + new Vector3(0.40f, -0.6f, 0f);
            SpriteRenderer vigSr = vig.GetComponent<SpriteRenderer>();
            if (vigSr != null) vigSr.flipX = true; // Mirando hacia la cuerda
        }

        MostrarAviso("¡TAÑENDO LA CAMPANA DEL ALBA...!", 3.0f);

        Vector3 posOriginalCuerda = trCuerda != null ? trCuerda.localPosition : Vector3.zero;
        Vector3 escalaOriginalCuerda = trCuerda != null ? trCuerda.localScale : Vector3.one;

        // 2. Tres repiques sagrados monumentales con animación física de la cuerda
        for (int repique = 1; repique <= 3; repique++)
        {
            // Tirón hacia abajo de la cuerda
            float tTiron = 0f;
            while (tTiron < 0.20f)
            {
                tTiron += Time.deltaTime;
                float pT = tTiron / 0.20f;
                if (trCuerda != null)
                {
                    trCuerda.localPosition = posOriginalCuerda + new Vector3(0f, -0.50f * Mathf.Sin(pT * Mathf.PI * 0.5f), 0f);
                    trCuerda.localScale = new Vector3(escalaOriginalCuerda.x, escalaOriginalCuerda.y * 1.25f, escalaOriginalCuerda.z);
                }
                yield return null;
            }

            // Sonido monumental del tañido de campana
            AudioClip bell = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMPANA);
            if (bell != null)
            {
                AudioSource.PlayClipAtPoint(bell, trCampana != null ? trCampana.position : Vector3.zero, 1.0f);
            }

            SacudirPantalla(0.65f, 16f + repique * 8f);

            // Rebote de retorno de la cuerda
            float tRebote = 0f;
            while (tRebote < 0.30f)
            {
                tRebote += Time.deltaTime;
                float pR = tRebote / 0.30f;
                if (trCuerda != null)
                {
                    float damp = Mathf.Sin(pR * Mathf.PI * 3f) * (1f - pR) * 0.15f;
                    trCuerda.localPosition = posOriginalCuerda + new Vector3(0f, damp, 0f);
                    trCuerda.localScale = Vector3.Lerp(trCuerda.localScale, escalaOriginalCuerda, pR);
                }
                yield return null;
            }

            if (trCuerda != null)
            {
                trCuerda.localPosition = posOriginalCuerda;
                trCuerda.localScale = escalaOriginalCuerda;
            }

            yield return new WaitForSeconds(0.30f);
        }

        MostrarAviso("¡LA CAMPANA DEL ALBA HA DESPERTADO EL DÍA! ¡LAS MOMIAS SE HAN PETRIFICADO!", 6.0f);

        // 3. Petrificar a Carmen "La Bruja"
        MomiaEncadenadaController bruja = FindFirstObjectByType<MomiaEncadenadaController>();
        if (bruja != null)
        {
            bruja.PetrificarMomia();
        }

        // 4. Petrificar a todas las momias de sección e invasoras
        MomiaNivel1Seccion[] momiasN1 = Object.FindObjectsByType<MomiaNivel1Seccion>(FindObjectsSortMode.None);
        foreach (var m1 in momiasN1)
        {
            if (m1 != null) m1.Petrificar();
        }

        MomiaNivel2Seccion[] momiasN2 = Object.FindObjectsByType<MomiaNivel2Seccion>(FindObjectsSortMode.None);
        foreach (var m2 in momiasN2)
        {
            if (m2 != null) m2.Petrificar();
        }

        MomiaInvasoraNivel4[] invasoras = Object.FindObjectsByType<MomiaInvasoraNivel4>(FindObjectsSortMode.None);
        foreach (var mi in invasoras)
        {
            mi.Petrificar();
        }

        // 5. Abrir el Gran Portón Sur monumental
        if (gen != null)
        {
            if (gen.colisionPortonSur != null) gen.colisionPortonSur.enabled = false;
            if (gen.portonSurVisual != null)
            {
                SpriteRenderer srP = gen.portonSurVisual.GetComponent<SpriteRenderer>();
                if (srP != null) srP.color = new Color(0.45f, 0.95f, 0.45f, 1f);
            }
        }

        // 6. Transición de iluminación hacia el amanecer pleno
        float tAmanecer = 0f;
        while (tAmanecer < 2.0f)
        {
            tAmanecer += Time.deltaTime;
            progresoAmanecer = Mathf.Clamp01(tAmanecer / 2.0f);
            AplicarAmanecer(progresoAmanecer);
            yield return null;
        }

        DesatarVictoriaFinal();
    }

    private void AplicarAmanecer(float p)
    {
        if (luzGlobalPanteon != null)
        {
            luzGlobalPanteon.color = Color.Lerp(colorNoche, colorAmanecer, p);
            luzGlobalPanteon.intensity = Mathf.Lerp(0.12f, 1.15f, p);
        }
    }

    public void DesatarVictoriaFinal()
    {
        NivelCompletado = true;
        JuegoTerminado = true;
        Time.timeScale = 0f;

        AudioClip clipVic = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_VICTORIA);
        if (clipVic != null && audioSfx != null)
        {
            audioSfx.PlayOneShot(clipVic, 1.0f);
        }
    }

    public void ActivarJumpscareGameOver(string asesina = "Carmen")
    {
        if (jumpscareActivo || NivelCompletado) return;

        asesinaJumpscare = string.IsNullOrEmpty(asesina) ? "Carmen" : asesina;
        jumpscareActivo = true;
        JuegoTerminado = true;
        timerJumpscare = 0f;

        if (VigilanteNivel4.Instance != null)
        {
            VigilanteNivel4.Instance.DetenerSonidoPasos();
        }

        AudioClip clipPerder = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_PERDER_N2);
        if (clipPerder == null) clipPerder = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_PERDER);
        if (clipPerder != null && audioSfx != null)
        {
            audioSfx.PlayOneShot(clipPerder, 1.0f);
        }
    }

    public void MostrarAviso(string texto, float duracion)
    {
        bannerAvisoTexto = texto;
        bannerAvisoTimer = duracion;
    }

    private void OnGUI()
    {
        if (fadeInteriorAlpha > 0.01f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeInteriorAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (fadeInteriorAlpha >= 0.98f) return;
        }

        if (jumpscareActivo)
        {
            DibujarJumpscareYGameOver();
            return;
        }

        if (NivelCompletado)
        {
            DibujarModalVictoriaFinal();
            return;
        }

        if (ModalLoreAbierto)
        {
            DibujarModalCronicaLore();
            return;
        }

        if (MenuConfigAbierto)
        {
            DibujarModalConfiguracionYAudio();
            return;
        }

        // 1. HUD Superior (Consistente con Nivel 2 y 3)
        DibujarHUDSuperior();

        // 1.1 Marcadores de Mapa si está activa la vista panorámica
        CameraFollow cfFollowMap = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : FindFirstObjectByType<CameraFollow>();
        if (cfFollowMap != null && cfFollowMap.EstaViendoMapaCompleto())
        {
            DibujarMarcadoresMapa();
        }

        // 2. Banner de Avisos
        if (bannerAvisoTimer > 0f)
        {
            float bw = Mathf.Min(600f, Screen.width - 40f);
            float bh = 34f;
            Rect br = new Rect((Screen.width - bw) * 0.5f + offsetShakeGUI.x, 72f + offsetShakeGUI.y, bw, bh);

            GUI.color = new Color(0.10f, 0.08f, 0.06f, 0.94f);
            GUI.DrawTexture(br, Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.75f, 0.25f, 1f);
            GUI.DrawTexture(new Rect(br.x, br.y, br.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(br.x, br.yMax - 2f, br.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle bSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bSt.normal.textColor = new Color(1f, 0.90f, 0.50f);
            GUI.Label(br, bannerAvisoTexto, bSt);
        }

        // 2.1 Medidor de Micrófono EXCLUSIVO al entrar a la zona de Momia 2 ("La Que Escucha") desde las 04:00 AM
        if (EstaJugadorEnZonaMomia2())
        {
            DibujarMedidorMicrofonoMomia2();
        }

        // 3. Anuncio central a mitad de pantalla (ej. 04:00 AM ¡Las Momias han salido del Museo!)
        if (anuncioCentroTimer > 0f)
        {
            DibujarAnuncioCentroPantalla();
        }

        // 4. Barra inferior de controles (estilo consistente con niveles anteriores)
        DibujarBarraControlesInferior();
    }

    private void DibujarBarraControlesInferior()
    {
        float h = 24f;
        Rect barRect = new Rect(0f, Screen.height - h, Screen.width, h);
        GUI.color = new Color(0.03f, 0.03f, 0.04f, 0.86f);
        GUI.DrawTexture(barRect, Texture2D.whiteTexture);
        GUI.color = new Color(0.72f, 0.58f, 0.32f, 0.45f);
        GUI.DrawTexture(new Rect(0f, barRect.y, Screen.width, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };
        st.normal.textColor = new Color(0.75f, 0.75f, 0.78f);
        GUI.Label(
            barRect,
            "WASD: Mover  |  Shift: Correr  |  Ctrl/C: Sigilo  |  F: Linterna  |  E: Interactuar  |  [M] Mapa  |  [L] Historia  |  [ESC] Configuración y Audio",
            st);
    }

    private void DibujarModalConfiguracionYAudio()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.80f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = Mathf.Min(500f, Screen.width - 32f);
        float h = 365f;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        GUI.color = new Color(0.85f, 0.72f, 0.30f, 0.95f);
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), Texture2D.whiteTexture);
        GUI.color = new Color(0.06f, 0.07f, 0.10f, 0.98f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titSt.normal.textColor = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(x + 16f, y + 14f, w - 32f, 28f), "CONFIGURACIÓN Y AUDIO", titSt);

        GUIStyle lblSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        lblSt.normal.textColor = new Color(0.92f, 0.90f, 0.85f);

        // 1. Volumen General
        float volGeneral = AudioListener.volume;
        GUI.Label(new Rect(x + 26f, y + 56f, 210f, 22f), $"Volumen General: {Mathf.RoundToInt(volGeneral * 100f)}%", lblSt);
        float nuevoVolGeneral = GUI.HorizontalSlider(new Rect(x + 235f, y + 61f, w - 261f, 18f), volGeneral, 0f, 1f);
        if (Mathf.Abs(nuevoVolGeneral - volGeneral) > 0.001f)
        {
            AudioListener.volume = nuevoVolGeneral;
            PlayerPrefs.SetFloat("VolumenMusica", nuevoVolGeneral);
            PlayerPrefs.Save();
        }

        // 2. Música Ambiental
        GUI.Label(new Rect(x + 26f, y + 90f, 210f, 22f), $"Música Ambiental: {Mathf.RoundToInt(volumenAmbiente * 100f)}%", lblSt);
        float nuevoVolAmb = GUI.HorizontalSlider(new Rect(x + 235f, y + 95f, w - 261f, 18f), volumenAmbiente, 0f, 1f);
        if (Mathf.Abs(nuevoVolAmb - volumenAmbiente) > 0.001f)
        {
            volumenAmbiente = nuevoVolAmb;
            if (audioAmbiente != null) audioAmbiente.volume = volumenAmbiente;
            PlayerPrefs.SetFloat("Nivel4_VolumenAmbiente", volumenAmbiente);
            PlayerPrefs.Save();
        }

        // 3. Sensibilidad del Micrófono
        GUI.Label(new Rect(x + 26f, y + 124f, 210f, 22f), $"Sensibilidad Micrófono: {Mathf.RoundToInt(sensibilidadMic * 100f)}%", lblSt);
        float nuevaSens = GUI.HorizontalSlider(new Rect(x + 235f, y + 129f, w - 261f, 18f), sensibilidadMic, 0.15f, 0.95f);
        if (Mathf.Abs(nuevaSens - sensibilidadMic) > 0.001f)
        {
            CambiarSensibilidadMic(nuevaSens);
        }

        // 4. Dispositivo activo + medidor de prueba en vivo
        GUI.Label(new Rect(x + 26f, y + 158f, w - 52f, 22f),
            $"Micrófono activo: <color=#55FFAA>{nombreDispositivoMicActivo}</color>", lblSt);

        Rect testBg = new Rect(x + 26f, y + 186f, w - 52f, 14f);
        GUI.color = new Color(0.16f, 0.14f, 0.16f, 1f);
        GUI.DrawTexture(testBg, Texture2D.whiteTexture);

        float ratioTest = micInicializado ? Mathf.Clamp01(nivelRuidoActual / Mathf.Max(0.05f, umbralRuidoMic * 1.4f)) : 0f;
        bool pasaUmbral = micInicializado && nivelRuidoActual > umbralRuidoMic;
        GUI.color = pasaUmbral
            ? new Color(0.98f, 0.25f, 0.25f, 1f)
            : (ratioTest > 0.45f ? new Color(0.95f, 0.72f, 0.18f, 1f) : new Color(0.25f, 0.85f, 0.40f, 1f));
        GUI.DrawTexture(new Rect(testBg.x, testBg.y, testBg.width * ratioTest, testBg.height), Texture2D.whiteTexture);

        float markX = testBg.x + testBg.width * Mathf.Clamp01(1f / 1.4f);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(markX, testBg.y - 2f, 2f, testBg.height + 4f), Texture2D.whiteTexture);

        GUIStyle subMicSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft
        };
        subMicSt.normal.textColor = pasaUmbral ? new Color(1f, 0.45f, 0.45f) : new Color(0.72f, 0.76f, 0.80f);
        string msgPrueba = !micInicializado
            ? "Sin dispositivo de micrófono detectado en este equipo."
            : (pasaUmbral ? "¡Voz detectada sobre el umbral de La Que Escucha!" : "Prueba hablar aquí: la barra subirá hasta la marca blanca.");
        GUI.Label(new Rect(x + 26f, y + 204f, w - 52f, 18f), msgPrueba, subMicSt);

        GUIStyle btnSt = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        if (GUI.Button(new Rect(x + 26f, y + 230f, w - 52f, 32f), "Cambiar Dispositivo de Micrófono", btnSt))
        {
            CambiarDispositivoMic();
        }

        float bw = (w - 72f) / 3f;
        float by = y + 296f;
        if (GUI.Button(new Rect(x + 26f, by, bw, 42f), "CONTINUAR [ESC]", btnSt))
        {
            AlternarMenuConfiguracion();
        }
        if (GUI.Button(new Rect(x + 36f + bw, by, bw, 42f), "REINICIAR", btnSt))
        {
            CargarEscenaLimpia("Nivel 4");
        }
        if (GUI.Button(new Rect(x + 46f + bw * 2f, by, bw, 42f), "MENÚ PRINCIPAL", btnSt))
        {
            CargarEscenaLimpia("MenuPrincipal");
        }
    }

    private void DibujarAnuncioCentroPantalla()
    {
        float alpha = Mathf.Clamp01(anuncioCentroTimer / 0.85f);
        float bw = Mathf.Min(740f, Screen.width - 60f);
        float bh = 104f;
        Rect cr = new Rect(
            (Screen.width - bw) * 0.5f + offsetShakeGUI.x,
            (Screen.height - bh) * 0.45f + offsetShakeGUI.y,
            bw,
            bh);

        GUI.color = new Color(0.06f, 0.03f, 0.03f, 0.93f * alpha);
        GUI.DrawTexture(cr, Texture2D.whiteTexture);

        GUI.color = new Color(0.92f, 0.28f, 0.22f, alpha);
        GUI.DrawTexture(new Rect(cr.x, cr.y, cr.width, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cr.x, cr.yMax - 3f, cr.width, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cr.x, cr.y, 3f, cr.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cr.xMax - 3f, cr.y, 3f, cr.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle tituloSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        tituloSt.normal.textColor = new Color(1f, 0.85f, 0.30f, alpha);
        GUI.Label(new Rect(cr.x + 14f, cr.y + 12f, cr.width - 28f, 44f), anuncioCentroTitulo, tituloSt);

        GUIStyle subSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        subSt.normal.textColor = new Color(0.95f, 0.92f, 0.88f, alpha);
        GUI.Label(new Rect(cr.x + 16f, cr.y + 56f, cr.width - 32f, 36f), anuncioCentroSubtitulo, subSt);
    }

    private void DibujarMedidorMicrofonoMomia2()
    {
        float mw = 230f;
        float mh = 42f;
        Rect mr = new Rect(Screen.width - mw - 14f, 74f, mw, mh);

        GUI.color = new Color(0.06f, 0.05f, 0.07f, 0.90f);
        GUI.DrawTexture(mr, Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.28f, 0.22f, 0.9f);
        GUI.DrawTexture(new Rect(mr.x, mr.y, mr.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(mr.x, mr.yMax - 2f, mr.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float ratio = micInicializado ? Mathf.Clamp01(nivelRuidoActual / Mathf.Max(0.01f, umbralRuidoMic * 1.4f)) : 0f;
        bool alertaMic = micInicializado && nivelRuidoActual > umbralRuidoMic;

        GUIStyle lblMic = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        lblMic.normal.textColor = alertaMic ? new Color(1f, 0.32f, 0.32f) : new Color(0.95f, 0.82f, 0.55f);
        string estadoMic = !micInicializado
            ? "🎙 MIC (SECTOR ESTE · SIN DISP.)"
            : (alertaMic ? "🎙 ¡VOZ DETECTADA! (TE OYE)" : "🎙 MIC · LA QUE ESCUCHA");
        GUI.Label(new Rect(mr.x + 8f, mr.y + 3f, mr.width - 16f, 16f), estadoMic, lblMic);

        Rect bgBar = new Rect(mr.x + 8f, mr.y + 23f, mr.width - 16f, 11f);
        GUI.color = new Color(0.18f, 0.14f, 0.14f, 1f);
        GUI.DrawTexture(bgBar, Texture2D.whiteTexture);

        GUI.color = alertaMic
            ? new Color(0.98f, 0.20f, 0.20f, 1f)
            : (ratio > 0.45f ? new Color(0.95f, 0.72f, 0.18f, 1f) : new Color(0.25f, 0.85f, 0.40f, 1f));
        GUI.DrawTexture(new Rect(bgBar.x, bgBar.y, bgBar.width * ratio, bgBar.height), Texture2D.whiteTexture);

        // Marca visual del umbral de alerta
        float umbralX = bgBar.x + bgBar.width * Mathf.Clamp01(1f / 1.4f);
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.DrawTexture(new Rect(umbralX, bgBar.y - 1f, 2f, bgBar.height + 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DibujarHUDSuperior()
    {
        float barH = 68f;
        Rect hudRect = new Rect(0, 0, Screen.width, barH);

        GUI.color = new Color(0.05f, 0.06f, 0.08f, 0.94f);
        GUI.DrawTexture(hudRect, Texture2D.whiteTexture);
        GUI.color = new Color(0.72f, 0.58f, 0.32f, 1f);
        GUI.DrawTexture(new Rect(0, barH - 2f, Screen.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 1. SECCIÓN IZQUIERDA: SALUD
        float hp = 100f;
        float maxHp = 100f;
        if (VigilanteNivel4.Instance != null)
        {
            hp = VigilanteNivel4.Instance.vidaActual;
            maxHp = VigilanteNivel4.Instance.vidaMaxima;
        }

        float barW = 120f;
        float barHVal = 14f;

        GUIStyle lblHp = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        lblHp.normal.textColor = new Color(1.0f, 0.40f, 0.40f);
        GUI.Label(new Rect(12, 10, 120, 20), $"❤ SALUD: {(int)hp}/{(int)maxHp}", lblHp);

        GUI.color = new Color(0.18f, 0.08f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(12, 34, barW, barHVal), Texture2D.whiteTexture);
        float hpPct = Mathf.Clamp01(hp / maxHp);
        GUI.color = hpPct > 0.35f ? new Color(0.20f, 0.85f, 0.30f, 1f) : new Color(0.95f, 0.20f, 0.20f, 1f);
        GUI.DrawTexture(new Rect(12, 34, barW * hpPct, barHVal), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. SECCIÓN RELIQUIAS Y ESTADO (Columna fija no solapada)
        float relX = 145f;
        float relW = 285f;

        GUIStyle relTitleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        relTitleSt.normal.textColor = new Color(0.95f, 0.85f, 0.55f);
        string txtReliquias = $"✝ RELIQUIAS: {ReliquiasRecolectadas}/3";
        GUI.Label(new Rect(relX, 6, relW, 18), txtReliquias, relTitleSt);

        GUIStyle checkSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft
        };
        checkSt.normal.textColor = Color.white;
        string tagCrucifijo = TieneCrucifijo ? "<color=#55FF55>[✓] Crucifijo</color>" : "<color=#FFAA55>[ ] Crucifijo (Oeste ⯇)</color>";
        string tagEslabon = TieneEslabon ? "<color=#55FF55>[✓] Eslabón</color>" : "<color=#FFAA55>[ ] Eslabón (Este ⯈)</color>";
        string tagLibro = TieneLibro ? "<color=#55FF55>[✓] Libro</color>" : "<color=#FFAA55>[ ] Libro (Sur ⯆)</color>";
        GUI.Label(new Rect(relX, 24, relW, 18), $"{tagCrucifijo}   {tagEslabon}", checkSt);

        GUIStyle capSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft
        };
        string txtCapilla = CampanaTocada
            ? "<color=#55FF55>Santuario: ¡Campana Tañida! Victoria</color>"
            : (ReliquiasRecolectadas >= 3
                ? "<color=#FFFF55>Santuario: ¡Puertas Abiertas! Entra a la Capilla</color>"
                : "<color=#FF7777>Santuario: Capilla Sellada (Faltan Reliquias)</color>");
        GUI.Label(new Rect(relX, 42, relW, 18), $"{tagLibro}  |  {txtCapilla}", checkSt);

        // 4. SECCIÓN DERECHA: BOTÓN DE MAPA
        float rightW = 125f;
        float rightX = Screen.width - rightW - 10f;

        CameraFollow cfFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : FindFirstObjectByType<CameraFollow>();
        bool mapaDisponible = ReliquiasRecolectadas >= 2 && !enInteriorCapilla;
        bool viendoMapa = mapaDisponible && cfFollow != null && cfFollow.EstaViendoMapaCompleto();
        string btnTxt = !mapaDisponible
            ? $"🔒 [M] MAPA ({ReliquiasRecolectadas}/2)"
            : (viendoMapa ? "🔍 [M] VIGILANTE" : "🗺️ [M] MAPA");

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = !mapaDisponible
            ? new Color(0.18f, 0.16f, 0.16f, 1f)
            : (viendoMapa ? new Color(0.2f, 0.6f, 1f, 1f) : new Color(0.25f, 0.25f, 0.28f, 1f));
        if (GUI.Button(new Rect(rightX, 16f, 118f, 34f), btnTxt))
        {
            if (mapaDisponible && cfFollow != null)
            {
                cfFollow.AlternarMapaCompleto();
            }
            else if (!enInteriorCapilla)
            {
                MostrarAviso($"🔒 El mapa [M] se desbloquea al encontrar 2 reliquias ({ReliquiasRecolectadas}/2).", 3.0f);
            }
        }
        GUI.backgroundColor = prevBg;

        // 3. SECCIÓN CENTRAL: RELOJ VIVO DE LA MADRUGADA (Centrado en el espacio disponible entre reliquias y derecha)
        float clkMinX = relX + relW + 10f;
        float clkMaxX = rightX - 10f;
        float clkAreaW = Mathf.Max(120f, clkMaxX - clkMinX);
        float clkW = Mathf.Min(160f, clkAreaW);
        float clkX = clkMinX + (clkAreaW - clkW) * 0.5f;

        GUIStyle clkSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        clkSt.normal.textColor = new Color(1f, 0.92f, 0.40f);
        GUI.Label(new Rect(clkX, 8, clkW, 24), "⏰ " + textoReloj, clkSt);

        GUIStyle subClk = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter
        };
        bool tiempoAgotado = (tiempoTranscurrido >= duracionNivelSegundos) && !CampanaTocada;
        subClk.normal.textColor = tiempoAgotado ? new Color(1.0f, 0.30f, 0.30f) : new Color(0.75f, 0.75f, 0.82f);
        string subTxt = tiempoAgotado ? "¡HORA CRÍTICA! TOCA LA CAMPANA" : "AMANECER (06:00 AM)";
        GUI.Label(new Rect(clkX, 34, clkW, 18), subTxt, subClk);
    }

    private void DibujarMarcadoresMapa()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Puntos cardinales de interés en mundo
        Vector3 posLeroy = new Vector3(-28f, 7.2f, 0f);
        Vector3 posOsario = new Vector3(28f, 7.2f, 0f);
        Vector3 posLibro = new Vector3(15.5f, -29.0f, 0f);
        Vector3 posCapilla = new Vector3(0f, 25f, 0f);

        DibujarInsigniaMapa(cam, posLeroy, TieneCrucifijo ? "✓ MAUSOLEO LEROY" : "⯇ LEROY (CRUCIFIJO)", TieneCrucifijo ? Color.green : new Color(1f, 0.75f, 0.2f));
        DibujarInsigniaMapa(cam, posOsario, TieneEslabon ? "✓ GRAN OSARIO" : "⯈ GRAN OSARIO (ESLABÓN)", TieneEslabon ? Color.green : new Color(1f, 0.75f, 0.2f));
        DibujarInsigniaMapa(cam, posLibro, TieneLibro ? "✓ LIBRO PARROQUIAL" : "⯆ ATRIO SUR (LIBRO)", TieneLibro ? Color.green : new Color(1f, 0.75f, 0.2f));
        DibujarInsigniaMapa(cam, posCapilla, CampanaTocada ? "✓ CAPILLA (CAMPANA TAÑIDA)" : "⯅ CAPILLA (CAMPANARIO DEL ALBA)", CampanaTocada ? Color.green : new Color(0.4f, 0.85f, 1f));

        if (VigilanteNivel4.Instance != null)
        {
            DibujarInsigniaMapa(cam, VigilanteNivel4.Instance.transform.position, "★ TÚ (VIGILANTE)", new Color(0.2f, 1f, 0.4f));
        }
    }

    private void DibujarInsigniaMapa(Camera cam, Vector3 worldPos, string label, Color col)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0f) return;

        float sx = screenPos.x;
        float sy = Screen.height - screenPos.y;

        float bw = 175f;
        float bh = 20f;
        Rect r = new Rect(sx - bw * 0.5f, sy - bh * 0.5f, bw, bh);

        GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = col;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), Texture2D.whiteTexture);

        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        st.normal.textColor = col;
        GUI.Label(r, label, st);
        GUI.color = Color.white;
    }

    /// <summary>
    /// Modal interactivo de la Crónica de Lore: Carmen "La Bruja" / La Momia Encadenada (1894).
    /// Despliega los 7 pilares canónicos de lore, anatomía, vestimenta, paleta de color, animaciones y sonido.
    /// </summary>
    private void DibujarModalCronicaLore()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        float pw = Mathf.Min(700f, Screen.width - 32f);
        float ph = Mathf.Min(450f, Screen.height - 32f);
        Rect pr = new Rect((Screen.width - pw) * 0.5f, (Screen.height - ph) * 0.5f, pw, ph);

        // Borde sepulcral envejecido
        GUI.color = new Color(0.65f, 0.50f, 0.25f, 1f);
        GUI.DrawTexture(new Rect(pr.x - 3, pr.y - 3, pr.width + 6, pr.height + 6), Texture2D.whiteTexture);
        GUI.color = new Color(0.08f, 0.07f, 0.07f, 0.99f);
        GUI.DrawTexture(pr, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Encabezado
        GUIStyle tHead = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        tHead.normal.textColor = new Color(0.85f, 0.65f, 0.30f);
        GUI.Label(new Rect(pr.x + 10, pr.y + 10, pw - 20, 18), "ARCHIVO HISTÓRICO Y BITÁCORA DEL PANTEÓN DE SANTA PAULA (1894)", tHead);

        GUIStyle tTit = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        tTit.normal.textColor = new Color(1f, 0.88f, 0.45f);
        GUI.Label(new Rect(pr.x + 10, pr.y + 28, pw - 20, 24), "⚜ CARMEN \"LA BRUJA\" — LA MOMIA ENCADENADA ⚜", tTit);

        GUI.color = new Color(0.65f, 0.50f, 0.25f, 0.6f);
        GUI.DrawTexture(new Rect(pr.x + 30, pr.y + 54, pw - 60, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Estilos para el texto de la crónica
        GUIStyle tBody = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            richText = true
        };
        tBody.normal.textColor = new Color(0.92f, 0.90f, 0.85f);

        string loreCompleto =
            "<color=#FFE082><size=13><b>📜 HISTORIA DEL PANTEÓN DE SANTA PAULA (1894)</b></size></color>\n" +
            "Declarada muerta por catalepsia en el siglo XIX, Carmen \"La Bruja\" fue sepultada en vida y atada con gruesas cadenas de hierro y candados benditos por temor a sus rituales. Hoy su espíritu recorre el camposanto arrastrando sus grilletes en busca de venganza.\n\n" +

            "<color=#FFE082><size=13><b>🕯 ELEMENTOS DEL PANTEÓN</b></size></color>\n" +
            "• <b>Las 3 Reliquias:</b> Encuentra el Crucifijo de Plomo, el Eslabón Bendito y el Libro Parroquial repartidos entre las criptas.\n" +
            "• <b>Capilla y Campana del Alba:</b> Consagra las 3 reliquias en el Altar Mayor y haz sonar la Campana antes o al amanecer (06:00 AM) para romper la maldición.\n" +
            "• <b>Cirios y Sarcófagos [E]:</b> Enciende cirios sagrados para repeler a las momias u ocúltate dentro de nichos y sarcófagos cuando acechen.\n" +
            "• <b>La Hora de las Momias (04:00 AM):</b> Además de Carmen, a las 04:00 AM las momias escapadas del museo acechan el Sector Oeste (Mausoleo) y el Sector Este (Gran Osario, donde las hojas secas y tu micrófono alertan a La Que Escucha).\n" +
            "• <b>Sigilo [Ctrl / C] y Linterna [F]:</b> Agáchate para no hacer ruido al caminar y apaga la linterna cuando Carmen esté cerca.";

        Rect viewArea = new Rect(pr.x + 20, pr.y + 60, pw - 40, ph - 115);
        float contentH = 340f;
        Rect contentArea = new Rect(0, 0, pw - 65, contentH);

        scrollPosLore = GUI.BeginScrollView(viewArea, scrollPosLore, contentArea);
        GUI.Label(contentArea, loreCompleto, tBody);
        GUI.EndScrollView();

        // Botón Cerrar
        float btnW = 200f;
        float btnH = 34f;
        if (GUI.Button(new Rect(pr.x + (pw - btnW) * 0.5f, pr.y + ph - 44, btnW, btnH), "CERRAR EXPEDIENTE [E / L]"))
        {
            ModalLoreAbierto = false;
        }
    }

    private void DibujarJumpscareYGameOver()
    {
        timerJumpscare += Time.unscaledDeltaTime;

        // Si el jugador pulsa cualquier tecla o click, salta inmediatamente al cartel de derrota
        if (timerJumpscare < DURACION_JUMPSCARE && !Input.anyKeyDown)
        {
            float shakeX = Random.Range(-18f, 18f);
            float shakeY = Random.Range(-18f, 18f);

            GUI.color = new Color(0.04f, 0.01f, 0.01f, 0.98f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            Texture2D texActiva = texJumpscareBruja;
            if (asesinaJumpscare == "Momia1" && texJumpscareMomia1 != null) texActiva = texJumpscareMomia1;
            else if (asesinaJumpscare == "Momia2" && texJumpscareMomia2 != null) texActiva = texJumpscareMomia2;

            if (texActiva != null)
            {
                float prog = Mathf.Clamp01(timerJumpscare / DURACION_JUMPSCARE);
                float zoom = Mathf.Lerp(0.86f, 1.02f, prog);
                float alto = Screen.height * zoom;
                float ancho = alto * ((float)texActiva.width / Mathf.Max(1f, texActiva.height));
                float yPos = Mathf.Max(4f, (Screen.height - alto) * 0.25f);
                Rect rJ = new Rect((Screen.width - ancho) * 0.5f + shakeX, yPos + shakeY, ancho, alto);
                GUI.DrawTexture(rJ, texActiva, ScaleMode.ScaleToFit);
            }
            return;
        }

        // Cartel Monumental de HAS PERDIDO
        Time.timeScale = 0f;
        GUI.color = new Color(0.02f, 0.02f, 0.03f, 0.95f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float pw = 560f;
        float ph = 340f;
        Rect pr = new Rect((Screen.width - pw) * 0.5f, (Screen.height - ph) * 0.5f, pw, ph);

        // Marco gótico carmesí oscuro con bordes decorativos
        GUI.color = new Color(0.75f, 0.12f, 0.12f, 1f);
        GUI.DrawTexture(new Rect(pr.x - 3, pr.y - 3, pr.width + 6, pr.height + 6), Texture2D.whiteTexture);
        GUI.color = new Color(0.08f, 0.07f, 0.09f, 0.98f);
        GUI.DrawTexture(pr, Texture2D.whiteTexture);
        GUI.color = new Color(0.95f, 0.20f, 0.20f, 1f);
        GUI.DrawTexture(new Rect(pr.x, pr.y, pr.width, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(pr.x, pr.yMax - 4f, pr.width, 4f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Título monumental
        GUIStyle tSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        tSt.normal.textColor = new Color(1f, 0.22f, 0.22f);
        GUI.Label(new Rect(pr.x, pr.y + 18, pw, 34), "☠ ¡HAS PERDIDO! ☠", tSt);

        // Subtítulo y consejos personalizados según la momia que eliminó al jugador
        string subtituloDerrota = "CARMEN \"LA BRUJA\" CONSUMIÓ TU ALMA EN EL PANTEÓN";
        string cuerpoDerrota =
            "Las cadenas de Carmen 'La Bruja' te atraparon antes de que pudieras tañer la Campana del Alba en la Capilla Mayor.\n\n" +
            "🕯 CONSEJOS PARA SOBREVIVIR:\n" +
            "• Presiona [Ctrl] o [C] para entrar en MODO SIGILO y apaga la linterna [F] si se acerca.\n" +
            "• Evita correr o pisar hojas secas sin sigilo.\n" +
            "• Pulsa [E] cerca de sarcófagos y criptas para ocultarte cuando escuches cadenas.";

        if (asesinaJumpscare == "Momia1")
        {
            subtituloDerrota = "LA MOMIA DEL MUSEO (\"LA QUE CAMINA\") TE ALCANZÓ";
            cuerpoDerrota =
                "La momia que salió del museo se abalanzó sobre ti en cuanto dejaste de mirarla en el Sector Oeste.\n\n" +
                "🕯 CONSEJOS PARA SOBREVIVIR:\n" +
                "• Mírala de frente para congelarla al instante como una estatua de piedra.\n" +
                "• No avances dándole la espalda dentro o fuera del Mausoleo Leroy.\n" +
                "• Enciende un Cirio Bendito o escóndete [E] en una cripta para que se aleje.";
        }
        else if (asesinaJumpscare == "Momia2")
        {
            subtituloDerrota = "\"LA QUE ESCUCHA\" TE RASTREÓ POR TU RUIDO";
            cuerpoDerrota =
                "La momia ciega que salió del museo escuchó tu voz, tus pasos o las hojas secas del Sector Este.\n\n" +
                "🕯 CONSEJOS PARA SOBREVIVIR:\n" +
                "• Vigila el medidor de MICRÓFONO al entrar al Gran Osario (guarda silencio).\n" +
                "• Presiona [Ctrl] o [C] para entrar en MODO SIGILO (silencia tus pasos y las hojas secas).\n" +
                "• Pulsa [E] en una cripta o enciende un Cirio Bendito para hacerla retroceder.";
        }

        GUIStyle subSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        subSt.normal.textColor = new Color(0.92f, 0.78f, 0.45f);
        GUI.Label(new Rect(pr.x, pr.y + 54, pw, 22), subtituloDerrota, subSt);

        // Separador fino carmesí
        GUI.color = new Color(0.5f, 0.15f, 0.15f, 0.8f);
        GUI.DrawTexture(new Rect(pr.x + 36, pr.y + 80, pw - 72, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Descripción y consejos
        GUIStyle descSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        descSt.normal.textColor = new Color(0.88f, 0.88f, 0.90f);
        GUI.Label(new Rect(pr.x + 24, pr.y + 92, pw - 48, 126), cuerpoDerrota, descSt);

        // Botones de acción
        float btnW = 210f;
        float btnH = 44f;
        float btnY = pr.yMax - 60f;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        if (GUI.Button(new Rect(pr.x + (pw - btnW * 2f - 24f) * 0.5f, btnY, btnW, btnH), "↺ REINTENTAR (R)", btnStyle) ||
            (Time.timeScale == 0f && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))))
        {
            CargarEscenaLimpia("Nivel 4");
        }

        if (GUI.Button(new Rect(pr.x + (pw - btnW * 2f - 24f) * 0.5f + btnW + 24f, btnY, btnW, btnH), "🏠 MENÚ PRINCIPAL (Esc)", btnStyle) ||
            (Time.timeScale == 0f && Input.GetKeyDown(KeyCode.Escape)))
        {
            CargarEscenaLimpia("MenuPrincipal");
        }
    }

    private void DibujarModalVictoriaFinal()
    {
        timerAnimacionFinal += Time.unscaledDeltaTime;
        float t = timerAnimacionFinal;

        // 1. Imagen de fondo completa de la victoria en Guanajuato (Ilustración del amanecer)
        float alphaBg = Mathf.Clamp01(t / 1.4f);
        GUI.color = new Color(0.04f, 0.04f, 0.06f, 1f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        if (texFinalGuanajuato != null)
        {
            GUI.color = new Color(1f, 1f, 1f, alphaBg);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texFinalGuanajuato, ScaleMode.ScaleAndCrop);
        }
        GUI.color = Color.white;

        // 2. Animación de "FIN":
        // Aparece con fundido suave (1.0s - 2.2s), se mantiene (2.2s - 3.8s) y luego se desvanece suavemente (3.8s - 5.4s)
        float aFin = 0f;
        if (t >= 1.0f && t < 2.2f)
        {
            aFin = Mathf.Clamp01((t - 1.0f) / 1.2f);
        }
        else if (t >= 2.2f && t < 3.8f)
        {
            aFin = 1.0f;
        }
        else if (t >= 3.8f && t < 5.4f)
        {
            aFin = Mathf.Clamp01(1.0f - (t - 3.8f) / 1.6f); // ¡Animación de desvanecimiento exacta solicitada!
        }

        if (aFin > 0.01f)
        {
            GUIStyle finStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp((int)(Screen.height * 0.12f), 42, 88),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            // Sombra monumental profunda
            finStyle.normal.textColor = new Color(0.02f, 0.01f, 0.04f, aFin * 0.90f);
            GUI.Label(new Rect(0, Screen.height * 0.38f + 3f, Screen.width, 90f), "F   I   N", finStyle);

            // Letras doradas sagradas del amanecer
            finStyle.normal.textColor = new Color(1.0f, 0.92f, 0.65f, aFin);
            GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 90f), "F   I   N", finStyle);
        }

        // 3. Texto de créditos que va subiendo suavemente (Scrolling Credits Animation a partir de t = 4.8s)
        if (t >= 4.8f)
        {
            float tCred = t - 4.8f;
            float climbProgress = Mathf.Clamp01(tCred / 4.5f);
            // Curva suave ease-out para un ascenso cinematográfico
            float ease = Mathf.Sin(climbProgress * Mathf.PI * 0.5f);

            float yStart = Screen.height + 40f;
            float yTarget = Mathf.Max(25f, (Screen.height - 355f) * 0.5f);
            float currentY = Mathf.Lerp(yStart, yTarget, ease);
            float alphaCred = Mathf.Clamp01(tCred / 1.2f);

            float boxW = Mathf.Min(620f, Screen.width - 40f);
            float boxH = 345f;
            Rect boxRect = new Rect((Screen.width - boxW) * 0.5f, currentY, boxW, boxH);

            // Fondo semitransparente oscuro con ribete dorado de sol
            GUI.color = new Color(0.06f, 0.05f, 0.08f, alphaCred * 0.86f);
            GUI.DrawTexture(boxRect, Texture2D.whiteTexture);

            GUI.color = new Color(0.98f, 0.82f, 0.38f, alphaCred * 0.95f);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y, boxRect.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.yMax - 3f, boxRect.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Encabezado "GRACIAS POR JUGAR"
            GUIStyle stHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            stHeader.normal.textColor = new Color(1f, 0.88f, 0.40f, alphaCred);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 14, boxW, 28), "✦ ¡GRACIAS POR JUGAR! ✦", stHeader);

            GUIStyle stSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            stSub.normal.textColor = new Color(0.95f, 0.80f, 0.65f, alphaCred);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 44, boxW, 20), "MOMIAS DE GUANAJUATO: LA NOCHE DEL MUSEO", stSub);

            // Narrativa de conclusión
            GUIStyle stLore = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            stLore.normal.textColor = new Color(0.90f, 0.90f, 0.92f, alphaCred);
            GUI.Label(new Rect(boxRect.x + 24, boxRect.y + 68, boxW - 48, 54),
                "Has tañido la Campana del Alba, rompiendo la maldición ancestral de 1865.\n" +
                "El sol baña Guanajuato y las almas del Panteón de Santa Paula descansan en paz.", stLore);

            // Línea divisoria decorativa
            GUI.color = new Color(0.85f, 0.70f, 0.30f, alphaCred * 0.60f);
            GUI.DrawTexture(new Rect(boxRect.x + 40, boxRect.y + 130, boxW - 80, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Créditos con los nombres de autoras requeridos
            GUIStyle stCred = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            stCred.normal.textColor = new Color(0.95f, 0.75f, 0.35f, alphaCred);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 138, boxW, 18), "ELABORADO POR:", stCred);

            GUIStyle stNombres = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            stNombres.normal.textColor = new Color(1f, 1f, 1f, alphaCred);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 160, boxW, 24), "Paola Jaqueline López Mata", stNombres);

            GUIStyle stY = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter
            };
            stY.normal.textColor = new Color(0.85f, 0.85f, 0.85f, alphaCred);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 184, boxW, 16), "&", stY);

            GUI.Label(new Rect(boxRect.x, boxRect.y + 200, boxW, 24), "Jennifer Ailin Medina Hernández", stNombres);

            // Botones interactivos (visibles a medida que avanza la subida o mediante hotkeys)
            if (climbProgress >= 0.5f)
            {
                float btnW = 190f;
                float btnH = 40f;
                float btnY = boxRect.yMax - 54f;

                GUIStyle btnSt = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                if (GUI.Button(new Rect(boxRect.x + (boxW - btnW * 2f - 20f) * 0.5f, btnY, btnW, btnH), "↺ REINTENTAR (R)", btnSt) ||
                    Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
                {
                    CargarEscenaLimpia("Nivel 4");
                }

                if (GUI.Button(new Rect(boxRect.x + (boxW - btnW * 2f - 20f) * 0.5f + btnW + 20f, btnY, btnW, btnH), "🏠 MENÚ PRINCIPAL (Esc)", btnSt) ||
                    Input.GetKeyDown(KeyCode.Escape))
                {
                    CargarEscenaLimpia("MenuPrincipal");
                }
            }
        }
    }
}
