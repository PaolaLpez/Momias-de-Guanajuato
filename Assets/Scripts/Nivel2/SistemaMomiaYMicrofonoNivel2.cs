using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema integral para el Nivel 2 (sin modificar archivos preexistentes):
/// 1. Garantiza el movimiento y las animaciones 100% fluidas del Vigilante (caminar, correr, saltar, girar) idénticas al Nivel 1.
/// 2. Organiza los 3 cuadrantes del HUD exclusivamente en la parte superior (Izquierda, Centro con Reloj en vivo, Derecha solo con Barra de Micrófono).
/// 3. Impide desactivar o bajar la sensibilidad del micrófono en el HUD (solo ajustable desde el menú de Configuración con ESC).
/// 4. Ejecuta el Jumpscare a pantalla completa al perder y muestra UN SOLO aviso de Game Over.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class SistemaMomiaYMicrofonoNivel2 : MonoBehaviour
{
    private const string NombreEscenaNivel2 = "Nivel 2";
    private const float DuracionSecuenciaJumpscare = 1.55f;
    private const float DuracionNivelSegundos = 180f; // 3 minutos reales = de 1:00 AM a 2:00 AM

    public static SistemaMomiaYMicrofonoNivel2 Instance { get; private set; }
    public static bool ModoSigiloActivo { get; private set; }

    public static void ResetearModoSigiloGlobal()
    {
        ModoSigiloActivo = false;
        if (Instance != null)
        {
            Instance.sigiloAlternadoC = false;
        }
    }

    private PlayerController playerScript;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private SpriteRenderer playerRootSr;
    private SpriteRenderer playerVisualSr;
    private AudioSource playerFootstepAudio;
    private DetectorMicrofonoNivel2 detectorMic;
    private MomiaNivel2Controller momia2;

    // Reflexión para mantener sincronizadas las referencias internas de PlayerController (incluso tras OnValidate o Hot Reload)
    private static FieldInfo fieldPlayerRb;
    private static FieldInfo fieldPlayerSr;
    private static FieldInfo fieldPlayerVisualChild;
    private static FieldInfo fieldPlayerFootstepSounds;
    private static FieldInfo fieldPlayerHurtSound;
    private static FieldInfo fieldPlayerFootstepSource;
    private static PropertyInfo propPlayerFacingDirection;
    private static MethodInfo methodPlayerAwake;

    // Modo Sigilo de la Guardia (Ctrl mantenido o tecla C para alternar) y su animación de caminata sigilosa
    private bool sigiloAlternadoC;
    private float timerAnimCaminataSigilo;
    private int frameAnimCaminataSigilo;
    private float fasePasoSigilo;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool TeclaFisicaPresionadaWin(int vKey)
    {
        try
        {
            if (!Application.isFocused) return false;
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }
        catch
        {
            return false;
        }
    }
#endif

    public static bool EsInputCorrerActivo()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || TouchControlsManager.IsRunning;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!shift) shift = TeclaFisicaPresionadaWin(0x10); // VK_SHIFT
#endif
        return shift;
    }

    // Control para volver a cerrar rejas abiertas con [E] (Opción 2: Encerrar a la Momia)
    private PuertaRejaMecanicaNivel2 rejaAbiertaCercanaParaCerrar;
    private readonly HashSet<PuertaRejaMecanicaNivel2> rejasCerrandose = new HashSet<PuertaRejaMecanicaNivel2>();
    private readonly Dictionary<PuertaRejaMecanicaNivel2, float> tiempoDesdeAperturaReja = new Dictionary<PuertaRejaMecanicaNivel2, float>();

    // Temporizador para emitir alerta acústica al correr con Shift o caminar sin sigilo cerca de la Momia
    private float timerAlertaCorrer;
    private float timerChequeoAlcantarillas;

    // Reloj en tiempo real (1:00 AM -> 2:00 AM)
    private float tiempoTranscurridoNivel;
    private string textoReloj = "01:00 AM";
    private string textoCuentaRegresiva = "03:00";

    // Estados de fin de partida y menú de configuración
    private bool jumpscareActivado;
    private float tiempoMuerteJumpscare;
    private string motivoPersonalizadoMuerte;
    private bool victoriaActivada;
    private bool menuConfiguracionAbierto;

    // Alerta temporal de ruido en el escenario
    private string alertaRuidoTexto = "";
    private float tiempoAlertaRuido;

    // Texturas y Audio para Jumpscare y HUD
    private Texture2D[] texturasJumpscare;
    private Texture2D texBlanca;
    private AudioSource audioJumpscare;
    private AudioSource audioAmbienteFondo;
    private AudioClip clipJumpscare;
    private AudioClip clipFondoNivel;

    // Estilos OnGUI
    private GUIStyle estiloHeaderCuadrante;
    private GUIStyle estiloTextoCuadrante;
    private GUIStyle estiloRelojHora;
    private GUIStyle estiloRelojSub;
    private GUIStyle estiloBarraMicTexto;
    private GUIStyle estiloTituloMuerte;
    private GUIStyle estiloSubtituloMuerte;
    private GUIStyle estiloBotonModal;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegistrarAutoArranqueEditorNivel2()
    {
        UnityEditor.EditorApplication.delayCall += VerificarEscenaEnEditor;
        UnityEditor.EditorApplication.hierarchyChanged += VerificarEscenaEnEditor;
    }

    private static void VerificarEscenaEnEditor()
    {
        if (Application.isPlaying) return;
        Scene escenaActual = SceneManager.GetActiveScene();
        if (escenaActual.IsValid() && escenaActual.name == NombreEscenaNivel2)
        {
            InstanciarSistemaEnNivel2();
        }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegistrarAutoArranqueNivel2()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;

        Scene escenaActual = SceneManager.GetActiveScene();
        if (escenaActual.IsValid() && escenaActual.name == NombreEscenaNivel2)
        {
            InstanciarSistemaEnNivel2();
        }
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        ResetearModoSigiloGlobal();
        if (escena.name == NombreEscenaNivel2)
        {
            Time.timeScale = 1f;
            InstanciarSistemaEnNivel2();
        }
    }

    private static void InstanciarSistemaEnNivel2()
    {
        SistemaMomiaYMicrofonoNivel2 existente = FindFirstObjectByType<SistemaMomiaYMicrofonoNivel2>();
        if (existente != null)
        {
            existente.EmbellecerEscenarioSubterraneoNivel2();
            existente.ConfigurarJugadorYMicrofono();
            existente.ConfigurarMomia2();
            return;
        }

        GameObject objSistema = new GameObject("SistemaMomiaYMicrofonoNivel2");
        objSistema.AddComponent<SistemaMomiaYMicrofonoNivel2>();
    }

    private static void InicializarCacheReflexionPlayer()
    {
        if (fieldPlayerRb == null)
            fieldPlayerRb = typeof(PlayerController).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fieldPlayerSr == null)
            fieldPlayerSr = typeof(PlayerController).GetField("sr", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fieldPlayerVisualChild == null)
            fieldPlayerVisualChild = typeof(PlayerController).GetField("visualChild", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fieldPlayerFootstepSounds == null)
            fieldPlayerFootstepSounds = typeof(PlayerController).GetField("footstepSounds", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fieldPlayerHurtSound == null)
            fieldPlayerHurtSound = typeof(PlayerController).GetField("hurtSound", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fieldPlayerFootstepSource == null)
            fieldPlayerFootstepSource = typeof(PlayerController).GetField("footstepAudioSource", BindingFlags.Instance | BindingFlags.NonPublic);
        if (propPlayerFacingDirection == null)
            propPlayerFacingDirection = typeof(PlayerController).GetProperty("facingDirection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (methodPlayerAwake == null)
            methodPlayerAwake = typeof(PlayerController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    private void Awake()
    {
        Instance = this;
        InicializarCacheReflexionPlayer();

        if (Application.isPlaying)
        {
            Time.timeScale = 1f;
            jumpscareActivado = false;
            victoriaActivada = false;
            menuConfiguracionAbierto = false;
            tiempoTranscurridoNivel = 0f;
            sigiloAlternadoC = false;
            ModoSigiloActivo = false;
        }

        CargarTexturasJumpscare();
        EmbellecerEscenarioSubterraneoNivel2();
        ConfigurarJugadorYMicrofono();
        ConfigurarMomia2();

        if (Application.isPlaying)
        {
            audioJumpscare = GetComponent<AudioSource>();
            if (audioJumpscare == null) audioJumpscare = gameObject.AddComponent<AudioSource>();
            audioJumpscare.playOnAwake = false;
            audioJumpscare.loop = false;
            audioJumpscare.spatialBlend = 0f;
            audioJumpscare.ignoreListenerPause = true;
            clipJumpscare = GenerarAudioJumpscareTerror();

            audioAmbienteFondo = gameObject.AddComponent<AudioSource>();
            audioAmbienteFondo.playOnAwake = false;
            audioAmbienteFondo.loop = true;
            audioAmbienteFondo.spatialBlend = 0f;
            audioAmbienteFondo.volume = 0.22f; // Volumen equilibrado de música de fondo ambiental
            clipFondoNivel = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_FONDO);
            if (clipFondoNivel == null) clipFondoNivel = Resources.Load<AudioClip>("Audio/Nivel2/fondo");
            if (clipFondoNivel == null) clipFondoNivel = Resources.Load<AudioClip>("AudioNivel2/fondo");
            if (clipFondoNivel == null) clipFondoNivel = Resources.Load<AudioClip>("fondo");
            if (clipFondoNivel != null)
            {
                clipFondoNivel.LoadAudioData();
                audioAmbienteFondo.clip = clipFondoNivel;
                audioAmbienteFondo.Play();
            }

            // Desactivar el HUD duplicado del Nivel2Manager antiguo para evitar textos encimados/entrecortados
            Nivel2Manager oldMgr = FindFirstObjectByType<Nivel2Manager>();
            if (oldMgr != null && oldMgr.enabled)
            {
                oldMgr.enabled = false;
            }

            // Reposicionar Momia 2 lejos de la jugadora (en la Subestación Norte) al arrancar la partida
            if (momia2 != null)
            {
                momia2.transform.position = new Vector3(0f, 17.5f, 0f);
            }
        }
    }

    private void OnEnable()
    {
        Instance = this;
        InicializarCacheReflexionPlayer();
        EmbellecerEscenarioSubterraneoNivel2();
        ConfigurarJugadorYMicrofono();
        ConfigurarMomia2();

        Nivel2Manager oldMgr = FindFirstObjectByType<Nivel2Manager>();
        if (oldMgr != null && oldMgr.enabled)
        {
            oldMgr.enabled = false;
        }

        if (Application.isPlaying)
        {
            if (!jumpscareActivado && !victoriaActivada && !menuConfiguracionAbierto)
            {
                Time.timeScale = 1f;
            }

            TrampaAcusticaNivel2.OnRuidoGenerado -= AlGenerarseRuidoEnMundo;
            TrampaAcusticaNivel2.OnRuidoGenerado += AlGenerarseRuidoEnMundo;
            InterruptorFusibleNivel2.OnFaseElectricaRestaurada -= AlRestaurarFaseIndividual;
            InterruptorFusibleNivel2.OnFaseElectricaRestaurada += AlRestaurarFaseIndividual;
            GeneradorPrincipalNivel2.OnElectricidadTotalmenteRestaurada -= AlRestaurarElectricidad;
            GeneradorPrincipalNivel2.OnElectricidadTotalmenteRestaurada += AlRestaurarElectricidad;
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            ResetearModoSigiloGlobal();
        }
        TrampaAcusticaNivel2.OnRuidoGenerado -= AlGenerarseRuidoEnMundo;
        InterruptorFusibleNivel2.OnFaseElectricaRestaurada -= AlRestaurarFaseIndividual;
        GeneradorPrincipalNivel2.OnElectricidadTotalmenteRestaurada -= AlRestaurarElectricidad;
    }

    private void OnDestroy()
    {
        ResetearModoSigiloGlobal();
        if (Instance == this) Instance = null;
        if (texBlanca != null)
        {
            if (Application.isPlaying) Destroy(texBlanca);
            else DestroyImmediate(texBlanca);
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            ResetearModoSigiloGlobal();
        }
        EmbellecerEscenarioSubterraneoNivel2();
        ConfigurarJugadorYMicrofono();
        ConfigurarMomia2();
        if (Application.isPlaying && !jumpscareActivado && !victoriaActivada && !menuConfiguracionAbierto)
        {
            Time.timeScale = 1f;
        }
    }

    private void AlRestaurarFaseIndividual(int idFase, string nombreFase)
    {
        int totalFases = GeneradorPrincipalNivel2.Instance != null ? GeneradorPrincipalNivel2.Instance.fasesRestauradas : idFase;
        if (totalFases >= 3)
        {
            alertaRuidoTexto = "¡3/3 FASES ACTIVAS! LA MOMIA CORRE A BLOQUEAR LA SALIDA NORTE";
            tiempoAlertaRuido = 5.0f;
        }
        else
        {
            alertaRuidoTexto = $"¡FASE {totalFases}/3 ACTIVA! LA VELOCIDAD DE LA MOMIA HA AUMENTADO";
            tiempoAlertaRuido = 4.2f;
        }
    }

    private void AlRestaurarElectricidad()
    {
        alertaRuidoTexto = "¡3/3 FASES! ¡LA MOMIA CUSTODIA LA SALIDA! ESQUÍVALA HACIA LA COMPUERTA NORTE";
        tiempoAlertaRuido = 5.5f;
    }

    private void Update()
    {
        if (decoEscenarioRef == null)
        {
            EmbellecerEscenarioSubterraneoNivel2();
        }

        timerChequeoAlcantarillas += Time.deltaTime;
        if (timerChequeoAlcantarillas >= 2.0f)
        {
            timerChequeoAlcantarillas = 0f;
            GameObject raiz = GameObject.Find("SubterraneoNivel2");
            if (raiz != null)
            {
                AsegurarTodasLasAlcantarillas(decoEscenarioRef != null ? decoEscenarioRef : raiz.transform);
            }
        }

        if (playerScript == null || detectorMic == null || playerRb == null)
        {
            ConfigurarJugadorYMicrofono();
        }

        if (momia2 == null)
        {
            ConfigurarMomia2();
        }

        if (!Application.isPlaying) return;

        // Desactivar el OnGUI antiguo de Nivel2Manager para que los 3 cuadrantes superiores luzcan limpios y sin duplicados
        if (Nivel2Manager.Instance != null && Nivel2Manager.Instance.enabled)
        {
            Nivel2Manager.Instance.enabled = false;
        }

        // Verificar si se completó el nivel (cuando CompuertaSalidaNivel2 llama a ActivarVictoria y pone Time.timeScale = 0 con 3/3 fases)
        if (!jumpscareActivado && !victoriaActivada && !menuConfiguracionAbierto)
        {
            if (GeneradorPrincipalNivel2.Instance != null &&
                GeneradorPrincipalNivel2.Instance.fasesRestauradas >= 3 &&
                Time.timeScale == 0f)
            {
                victoriaActivada = true;
                return;
            }
        }

        // Garantizar siempre el movimiento y las animaciones fluidas del Vigilante antes de que corra PlayerController.Update()
        GarantizarMovimientoJugador();

        if (!jumpscareActivado && !victoriaActivada && playerScript != null && (playerScript.EstaMuerto || playerScript.vidaActual <= 0f))
        {
            // Permitir que la animación de la Momia rasguñando a la Guardia se vea claramente en escena (~0.85s) antes del Jumpscare
            bool esperandoAnimacionRasguno = momia2 != null &&
                                             momia2.EstaEjecutandoAtaqueRasguno &&
                                             momia2.TiempoRasgunandoActual < 0.85f;
            if (!esperandoAnimacionRasguno)
            {
                ActivarJumpscareGameOver(null);
            }
        }

        if (jumpscareActivado)
        {
            // Garantizar silencio absoluto de pasos durante todo el Jumpscare y la pantalla de Game Over
            if (playerFootstepAudio != null && playerFootstepAudio.isPlaying)
            {
                playerFootstepAudio.Stop();
                if (playerFootstepAudio.clip != null) playerFootstepAudio.time = 0f;
                playerFootstepAudio.volume = 0f;
                playerFootstepAudio.loop = false;
            }
            tiempoMuerteJumpscare += Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.R))
            {
                ReiniciarNivel2();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                IrAlMenuPrincipal();
            }
            return;
        }

        if (victoriaActivada)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
            {
                IrAlMenuPrincipal();
            }
            return;
        }

        // Si el plano del subsuelo está abierto, EXCLUSIVAMENTE ESC puede cerrar el plano (E no lo cierra, ni abre configuración)
        if (PlanoSubterraneoNivel2.VisorPlanoAbierto)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (PlanoSubterraneoNivel2.Instance != null)
                {
                    PlanoSubterraneoNivel2.Instance.CerrarVisorPlano();
                }
            }
            return;
        }

        // Abrir / Cerrar menú de Configuración del juego con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AlternarMenuConfiguracion();
            return;
        }

        if (menuConfiguracionAbierto)
        {
            return;
        }

        // Asegurar que el tiempo corra normalmente durante la partida activa
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        if (tiempoAlertaRuido > 0f)
        {
            tiempoAlertaRuido -= Time.deltaTime;
        }

        EmitirAlertaAcusticaSiCorre();
        ActualizarInteraccionCierreDeRejas();
        ActualizarRelojNivel2();
        ActualizarSonidoPasosJugador();
    }

    /// <summary>
    /// Permite a la Guardia volver a bajar/cerrar cualquier PuertaRejaMecanicaNivel2 que esté abierta
    /// presionando [E] o [Espacio] para poder encerrar a la Momia (Opción 2).
    /// </summary>
    private void ActualizarInteraccionCierreDeRejas()
    {
        rejaAbiertaCercanaParaCerrar = null;
        if (playerTransform == null) return;
        if (DetectorMicrofonoNivel2.VerificarSiJugadorEstaEscondido()) return;

        PuertaRejaMecanicaNivel2[] rejas = FindObjectsByType<PuertaRejaMecanicaNivel2>(FindObjectsSortMode.None);
        float mejorDist = float.MaxValue;

        for (int i = 0; i < rejas.Length; i++)
        {
            PuertaRejaMecanicaNivel2 r = rejas[i];
            if (r == null) continue;

            if (!r.estaAbierta || rejasCerrandose.Contains(r))
            {
                tiempoDesdeAperturaReja.Remove(r);
                continue;
            }

            if (!tiempoDesdeAperturaReja.ContainsKey(r))
            {
                tiempoDesdeAperturaReja[r] = Time.time;
            }

            // Pequeño margen de 0.35s tras abrirse para que no se cierre en el mismo instante
            if (Time.time - tiempoDesdeAperturaReja[r] < 0.35f) continue;

            float d = Vector2.Distance(playerTransform.position, r.transform.position);
            if (d <= r.distanciaInteraccion + 1.0f && d < mejorDist)
            {
                mejorDist = d;
                rejaAbiertaCercanaParaCerrar = r;
            }
        }

        if (rejaAbiertaCercanaParaCerrar != null)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || TouchControlsManager.IsInteractTriggered() || TouchControlsManager.IsJumpTriggered())
            {
                StartCoroutine(SecuenciaCerrarRejaParaEncerrar(rejaAbiertaCercanaParaCerrar));
            }
        }
    }

    private IEnumerator SecuenciaCerrarRejaParaEncerrar(PuertaRejaMecanicaNivel2 reja)
    {
        if (reja == null || !reja.estaAbierta || rejasCerrandose.Contains(reja)) yield break;

        rejasCerrandose.Add(reja);
        rejaAbiertaCercanaParaCerrar = null;

        float anchoReja = reja.name.Contains("Central") ? 6.2f : 4.2f;
        Vector3 posReja = reja.transform.position;

        // Si la Guardia o la Momia están justo debajo de la hoja de la reja al cerrarse, apartarlas suavemente del umbral
        if (playerTransform != null &&
            Mathf.Abs(playerTransform.position.y - posReja.y) < 1.15f &&
            Mathf.Abs(playerTransform.position.x - posReja.x) < anchoReja * 0.55f)
        {
            float signoGuardia = (playerTransform.position.y >= posReja.y) ? 1f : -1f;
            playerTransform.position = new Vector3(playerTransform.position.x, posReja.y + signoGuardia * 1.25f, playerTransform.position.z);
        }

        if (momia2 != null &&
            Mathf.Abs(momia2.transform.position.y - posReja.y) < 1.25f &&
            Mathf.Abs(momia2.transform.position.x - posReja.x) < anchoReja * 0.55f)
        {
            float signoMomia = (momia2.transform.position.y >= posReja.y) ? 1f : -1f;
            momia2.transform.position = new Vector3(momia2.transform.position.x, posReja.y + signoMomia * 1.35f, momia2.transform.position.z);
        }

        // Restaurar la colisión física sólida de la reja para que bloquee 100% el paso
        if (reja.colisionBloqueo == null)
        {
            BoxCollider2D nuevoCol = reja.gameObject.AddComponent<BoxCollider2D>();
            nuevoCol.isTrigger = false;
            nuevoCol.size = new Vector2(anchoReja, 1.8f);
            nuevoCol.offset = Vector2.zero;
            reja.colisionBloqueo = nuevoCol;
        }
        else
        {
            reja.colisionBloqueo.enabled = true;
            reja.colisionBloqueo.isTrigger = false;
        }

        reja.estaAbierta = false;

        if (momia2 != null)
        {
            momia2.IgnorarColisionesSoloConRatas();
        }

        TrampaAcusticaNivel2.EmitirRuido(posReja, 15f, "¡GOLPE METÁLICO DE REJA CERRADA!");

        Vector3 posAlta = reja.rejaPuerta != null ? reja.rejaPuerta.localPosition : new Vector3(0f, 3.5f, 0f);
        Vector3 posCerrada = Vector3.zero;
        float duracionCierre = 0.48f;
        float t = 0f;

        while (t < duracionCierre)
        {
            t += Time.deltaTime;
            float prog = Mathf.Clamp01(t / duracionCierre);
            if (reja.rejaPuerta != null)
            {
                reja.rejaPuerta.localPosition = Vector3.Lerp(posAlta, posCerrada, prog);
            }
            if (reja.manivelaSr != null)
            {
                reja.manivelaSr.transform.Rotate(0f, 0f, -360f * Time.deltaTime);
            }
            yield return null;
        }

        if (reja.rejaPuerta != null)
        {
            reja.rejaPuerta.localPosition = posCerrada;
        }

        rejasCerrandose.Remove(reja);
    }

    /// <summary>
    /// Emite la onda de alerta acústica para la Momia 2 cuando la Guardia corre con Shift,
    /// o cuando camina a velocidad normal SIN Modo Sigilo estando cerca de la Momia (< 9.5m).
    /// Si lleva el Modo Sigilo activo (Ctrl o C), sus pasos son 100% silenciosos.
    /// </summary>
    private void EmitirAlertaAcusticaSiCorre()
    {
        if (playerRb == null || playerTransform == null) return;
        if (DetectorMicrofonoNivel2.VerificarSiJugadorEstaEscondido()) return;

        bool corriendo = !ModoSigiloActivo &&
                         EsInputCorrerActivo() &&
                         playerRb.linearVelocity.sqrMagnitude > 1.8f;

        if (corriendo)
        {
            timerAlertaCorrer -= Time.deltaTime;
            if (timerAlertaCorrer <= 0f)
            {
                timerAlertaCorrer = 0.36f;
                TrampaAcusticaNivel2.EmitirRuido(playerTransform.position, 15f, "¡PASOS DE CARRERA DEL VIGILANTE!");
            }
            return;
        }

        // Si la jugadora está en Modo Sigilo (agachada con C o Ctrl), sus pasos son 100% silenciosos
        if (ModoSigiloActivo)
        {
            return;
        }

        // Si camina de pie cerca de la Momia (<= 6.5m), la Momia escucha sus pasos y la cazará.
        // Para pasar a un lado de ella es OBLIGATORIO agacharse en Modo Sigilo.
        bool caminandoDePie = playerRb.linearVelocity.sqrMagnitude > 0.08f;
        if (caminandoDePie && momia2 != null && momia2.distanciaAlJugador <= 6.5f)
        {
            timerAlertaCorrer -= Time.deltaTime;
            if (timerAlertaCorrer <= 0f)
            {
                timerAlertaCorrer = 0.28f;
                TrampaAcusticaNivel2.EmitirRuido(playerTransform.position, 8.5f, "¡PASOS DE PIE CERCA DE LA MOMIA!");
            }
        }
        else
        {
            timerAlertaCorrer = 0.15f;
        }
    }

    /// <summary>
    /// Controla la reproducción de caminata.mp3 de forma receptiva e inmediata:
    /// - Solo suena mientras el jugador presione movimiento y se desplace físicamente.
    /// - Al soltar las teclas o detenerse, SE DETIENE DE INMEDIATO (sin resonar tras frenar).
    /// - En Modo Sigilo se silencia por completo (solo se escucha el ambiente).
    /// </summary>
    private void ActualizarSonidoPasosJugador()
    {
        if (playerFootstepAudio == null && playerScript != null)
        {
            if (fieldPlayerFootstepSource != null)
            {
                playerFootstepAudio = fieldPlayerFootstepSource.GetValue(playerScript) as AudioSource;
            }
            if (playerFootstepAudio == null)
            {
                playerFootstepAudio = playerScript.GetComponent<AudioSource>();
            }
        }

        if (playerFootstepAudio == null || playerScript == null || playerRb == null) return;

        bool escondido = DetectorMicrofonoNivel2.VerificarSiJugadorEstaEscondido();
        bool muerto = playerScript.EstaMuerto || playerScript.vidaActual <= 0f;
        bool saltando = playerScript.EstaSaltando;
        bool enPausaOGameOver = jumpscareActivado || victoriaActivada || Time.timeScale == 0f;

        Vector2 inputMov = LeerInputMovimientoConSoporteSigilo();
        bool tieneInputMovimiento = inputMov.sqrMagnitude > 0.05f;
        bool seMueveFisicamente = playerRb.linearVelocity.sqrMagnitude > 0.08f;
        bool enCharco = TrampaAcusticaNivel2.JugadorEnCharco;

        // Solo suena si camina/corre activamente y NO está en sigilo ni en charco de agua ni muerto ni en Game Over/Pausa
        bool debeSonarCaminata = tieneInputMovimiento && seMueveFisicamente && !saltando && !muerto && !escondido && !ModoSigiloActivo && !enPausaOGameOver && !enCharco;

        AudioClip clipCaminata = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_CAMINATA);

        if (debeSonarCaminata && clipCaminata != null)
        {
            bool corriendo = EsInputCorrerActivo();

            if (playerFootstepAudio.clip != clipCaminata)
            {
                playerFootstepAudio.clip = clipCaminata;
            }
            playerFootstepAudio.loop = true;
            playerFootstepAudio.pitch = corriendo ? 1.30f : 0.98f;
            playerFootstepAudio.volume = corriendo ? 0.50f : 0.32f;

            if (!playerFootstepAudio.isPlaying)
            {
                playerFootstepAudio.Play();
            }
        }
        else
        {
            // Cortar en seco al instante cuando se frena, se sueltan las teclas, entra en sigilo o pisa un charco
            // para que caminata.mp3 jamás continúe sonando al soltar el movimiento
            if (playerFootstepAudio.isPlaying)
            {
                playerFootstepAudio.Stop();
            }
            if (playerFootstepAudio.clip != null) playerFootstepAudio.time = 0f;
            playerFootstepAudio.volume = 0f;

            AudioSource mainAudio = playerScript.GetComponent<AudioSource>();
            if (mainAudio != null && mainAudio != playerFootstepAudio && mainAudio.isPlaying && mainAudio.clip == clipCaminata)
            {
                mainAudio.Stop();
                if (mainAudio.clip != null) mainAudio.time = 0f;
            }
        }
    }

    private void ActualizarRelojNivel2()
    {
        tiempoTranscurridoNivel += Time.deltaTime;
        float pct = Mathf.Clamp01(tiempoTranscurridoNivel / DuracionNivelSegundos);
        int minFicticios = Mathf.FloorToInt(pct * 60f);

        if (pct >= 1f)
        {
            textoReloj = "02:00 AM";
            textoCuentaRegresiva = "00:00";
            ActivarJumpscareGameOver("¡HAN DADO LAS 2:00 AM! Se agotó tu tiempo en el subsuelo.");
        }
        else
        {
            textoReloj = string.Format("01:{0:D2} AM", minFicticios);
            float segsRestantes = Mathf.Max(0f, DuracionNivelSegundos - tiempoTranscurridoNivel);
            int minR = Mathf.FloorToInt(segsRestantes / 60f);
            int segR = Mathf.FloorToInt(segsRestantes % 60f);
            textoCuentaRegresiva = string.Format("{0:D2}:{1:D2}", minR, segR);
        }
    }

    /// <summary>
    /// Garantiza que PlayerController tenga cargados sus 12 frames de caminata y 7 de carrera,
    /// que su campo privado 'sr' apunte SIEMPRE al SpriteRenderer de 'VisualPivot' (y no al SpriteRenderer
    /// raíz desactivado que OnValidate asigna por error), y que su Rigidbody2D tenga interpolación activa.
    /// </summary>
    private void GarantizarMovimientoJugador()
    {
        if (playerScript == null || playerTransform == null) return;

        InicializarCacheReflexionPlayer();

        bool faltaInicializar = false;
        if (playerScript.walkSprites == null || playerScript.walkSprites.Length < 12 || playerScript.walkSprites[0] == null ||
            playerScript.runSprites == null || playerScript.runSprites.Length < 7 || playerScript.runSprites[0] == null ||
            playerScript.idleSprite == null || playerScript.jumpSprite == null)
        {
            faltaInicializar = true;
        }

        Transform vp = playerTransform.Find("VisualPivot");
        if (vp == null)
        {
            faltaInicializar = true;
        }

        if (fieldPlayerRb != null && fieldPlayerRb.GetValue(playerScript) == null)
        {
            faltaInicializar = true;
        }

        if (fieldPlayerFootstepSounds != null && fieldPlayerFootstepSounds.GetValue(playerScript) == null)
        {
            faltaInicializar = true;
        }

        if (faltaInicializar && methodPlayerAwake != null)
        {
            methodPlayerAwake.Invoke(playerScript, null);
            vp = playerTransform.Find("VisualPivot");
            playerVisualSr = null;
        }

        if (playerRootSr == null)
        {
            playerRootSr = playerScript.GetComponent<SpriteRenderer>();
        }
        if (vp != null && playerVisualSr == null)
        {
            playerVisualSr = vp.GetComponent<SpriteRenderer>();
        }

        bool escondido = DetectorMicrofonoNivel2.VerificarSiJugadorEstaEscondido();

        // Actualizar el Modo Sigilo de la Guardia:
        // Por defecto camina erguida normal. SOLO se agacha si:
        // - Se mantiene presionada la tecla Ctrl (LeftControl o RightControl)
        // - Se presiona la tecla C (o combinación Ctrl+C) para alternar
        // - Se toca el botón táctil [SIGILO] en pantalla
        // Correr con Shift, saltar con Espacio o esconderse desactiva el sigilo de inmediato.
        if (Application.isPlaying)
        {
            bool corriendoInput = EsInputCorrerActivo();
            bool saltandoInput = playerScript != null && playerScript.EstaSaltando;

            if (Input.GetKeyDown(KeyCode.C))
            {
                sigiloAlternadoC = !sigiloAlternadoC;
                if (TouchControlsManager.Instance != null)
                {
                    TouchControlsManager.Instance.SetStealth(sigiloAlternadoC);
                }
            }

            if (corriendoInput || saltandoInput || escondido)
            {
                sigiloAlternadoC = false;
                if (TouchControlsManager.Instance != null)
                {
                    TouchControlsManager.Instance.SetStealth(false);
                }
            }

            bool ctrlMantenido = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool touchSigilo = TouchControlsManager.IsStealth;

            ModoSigiloActivo = !escondido && !corriendoInput && !saltandoInput && (ctrlMantenido || sigiloAlternadoC || touchSigilo);
        }
        else
        {
            ModoSigiloActivo = false;
            sigiloAlternadoC = false;
        }

        // Apagar siempre el SpriteRenderer raíz para que jamás dibuje un sprite estático encima de VisualPivot
        if (playerRootSr != null && playerRootSr.enabled)
        {
            playerRootSr.enabled = false;
        }

        if (vp != null)
        {
            if (!ModoSigiloActivo)
            {
                vp.localRotation = Quaternion.identity;
                vp.localScale = Vector3.one;
                if (!playerScript.EstaSaltando)
                {
                    vp.localPosition = Vector3.zero;
                }
            }
            if (fieldPlayerVisualChild != null && !ReferenceEquals(fieldPlayerVisualChild.GetValue(playerScript), vp))
            {
                fieldPlayerVisualChild.SetValue(playerScript, vp);
            }
        }

        // Asegurar que PlayerController.sr apunte SIEMPRE al SpriteRenderer activo de VisualPivot
        if (playerVisualSr != null)
        {
            playerVisualSr.enabled = !escondido;
            if (fieldPlayerSr != null && !ReferenceEquals(fieldPlayerSr.GetValue(playerScript), playerVisualSr))
            {
                fieldPlayerSr.SetValue(playerScript, playerVisualSr);
                if (playerVisualSr.sprite == null && playerScript.idleSprite != null)
                {
                    playerVisualSr.sprite = playerScript.idleSprite;
                }
            }

            if (momia2 != null && momia2.EstaEjecutandoAtaqueRasguno)
            {
                playerVisualSr.color = (Time.frameCount % 4 < 2) ? new Color(1f, 0.42f, 0.42f, 1f) : Color.white;
            }
            else if (ModoSigiloActivo)
            {
                playerVisualSr.color = Color.white;
            }
            else
            {
                playerVisualSr.color = Color.white;
            }
        }

        // Atenuar o silenciar por completo los pasos de PlayerController cuando la Guardia está en Modo Sigilo
        // Y asignar el clip de caminata y herida personalizados para el Nivel 2
        if (playerScript != null)
        {
            if (fieldPlayerHurtSound != null)
            {
                AudioClip clipHerida = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_HERIDA);
                if (clipHerida != null && !object.ReferenceEquals(fieldPlayerHurtSound.GetValue(playerScript), clipHerida))
                {
                    fieldPlayerHurtSound.SetValue(playerScript, clipHerida);
                }
            }

            if (fieldPlayerFootstepSounds != null)
            {
                // Limpiar la matriz de pasos de PlayerController para que no ejecute PlayOneShot repetitivos de caminata.mp3
                AudioClip[] arr = fieldPlayerFootstepSounds.GetValue(playerScript) as AudioClip[];
                if (arr == null || arr.Length > 0)
                {
                    fieldPlayerFootstepSounds.SetValue(playerScript, new AudioClip[0]);
                }
            }
        }

        if (playerFootstepAudio == null && playerScript != null)
        {
            if (fieldPlayerFootstepSource != null)
            {
                playerFootstepAudio = fieldPlayerFootstepSource.GetValue(playerScript) as AudioSource;
            }
            if (playerFootstepAudio == null)
            {
                playerFootstepAudio = playerScript.GetComponent<AudioSource>();
            }
        }

        // Mantener activo el sonido ambiental de fondo si no está reproduciéndose
        if (audioAmbienteFondo != null && !jumpscareActivado && !victoriaActivada && !menuConfiguracionAbierto)
        {
            audioAmbienteFondo.volume = 0.22f;
            if (audioAmbienteFondo.clip == null)
            {
                audioAmbienteFondo.clip = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_FONDO);
                if (audioAmbienteFondo.clip == null) audioAmbienteFondo.clip = Resources.Load<AudioClip>("Audio/Nivel2/fondo");
                if (audioAmbienteFondo.clip == null) audioAmbienteFondo.clip = Resources.Load<AudioClip>("AudioNivel2/fondo");
                if (audioAmbienteFondo.clip == null) audioAmbienteFondo.clip = Resources.Load<AudioClip>("fondo");
            }
            if (audioAmbienteFondo.clip != null && !audioAmbienteFondo.isPlaying)
            {
                audioAmbienteFondo.Play();
            }
        }

        // Mantener escala y velocidades equilibradas para el Nivel 2:
        // Caminar normal es más lento y pausado (5.2f), correr con Shift alcanza la velocidad rápida (10.2f), y sigilo es pausado y sigiloso (2.8f)
        playerTransform.localScale = new Vector3(1.5f, 1.5f, 1f);
        playerTransform.localRotation = Quaternion.identity;

        float velCorrer = 9.5f;
        float velCaminarNormal = 6.2f;
        float velSigilo = 2.8f;

        if (ModoSigiloActivo)
        {
            playerScript.velocidadCaminar = velSigilo;
            playerScript.velocidadCorrer = velSigilo;
            playerScript.bloquearResetVisualPivot = true;
        }
        else
        {
            playerScript.velocidadCaminar = velCaminarNormal;
            playerScript.velocidadCorrer = velCorrer;
            playerScript.bloquearResetVisualPivot = false;
        }

        playerScript.museoMinX = -26.5f;
        playerScript.museoMaxX = 26.5f;
        playerScript.museoMinY = -20.0f;
        playerScript.museoMaxY = 23.5f;

        if (playerRb != null && !escondido)
        {
            if (playerRb.bodyType != RigidbodyType2D.Dynamic) playerRb.bodyType = RigidbodyType2D.Dynamic;
            if (!playerRb.simulated) playerRb.simulated = true;
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;
            if (playerRb.interpolation != RigidbodyInterpolation2D.Interpolate)
            {
                playerRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            if (playerRb.collisionDetectionMode != CollisionDetectionMode2D.Continuous)
            {
                playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            // Aplicar velocidad directa para garantizar respuesta 100% inmediata de la tecla Shift (Correr)
            if (!ModoSigiloActivo)
            {
                Vector2 inputMov = LeerInputMovimientoConSoporteSigilo();
                if (inputMov.sqrMagnitude > 0.01f)
                {
                    bool corriendo = EsInputCorrerActivo();
                    playerRb.linearVelocity = inputMov * (corriendo ? velCorrer : velCaminarNormal);
                }
            }
        }
    }

    /// <summary>
    /// Lee las teclas de dirección (WASD y Flechas) incluyendo soporte directo de teclado físico en Windows
    /// para que al mantener presionado Ctrl (Modo Sigilo) en el Editor de Unity, los atajos Ctrl+W/A/S/D
    /// del Editor jamás bloqueen el movimiento ni la animación de caminata en sigilo.
    /// </summary>
    private static Vector2 LeerInputMovimientoConSoporteSigilo()
    {
        float moveX = 0f;
        float moveY = 0f;

        bool right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        bool left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (ModoSigiloActivo)
        {
            right = right || TeclaFisicaPresionadaWin(0x44) || TeclaFisicaPresionadaWin(0x27); // D o RightArrow
            left = left || TeclaFisicaPresionadaWin(0x41) || TeclaFisicaPresionadaWin(0x25);   // A o LeftArrow
            up = up || TeclaFisicaPresionadaWin(0x57) || TeclaFisicaPresionadaWin(0x26);       // W o UpArrow
            down = down || TeclaFisicaPresionadaWin(0x53) || TeclaFisicaPresionadaWin(0x28);   // S o DownArrow
        }
#endif

        if (right) moveX += 1f;
        if (left) moveX -= 1f;
        if (up) moveY += 1f;
        if (down) moveY -= 1f;

        Vector2 touchJoy = TouchControlsManager.Movement;
        if (touchJoy.sqrMagnitude > 0.04f)
        {
            moveX += touchJoy.x;
            moveY += touchJoy.y;
        }

        Vector2 dir = new Vector2(moveX, moveY);
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || Time.timeScale == 0f) return;
        if (playerScript == null || playerTransform == null || playerScript.EstaMuerto) return;
        if (DetectorMicrofonoNivel2.VerificarSiJugadorEstaEscondido()) return;

        Transform vp = playerTransform.Find("VisualPivot");
        if (vp != null && playerVisualSr == null)
        {
            playerVisualSr = vp.GetComponent<SpriteRenderer>();
        }

        if (!ModoSigiloActivo)
        {
            timerAnimCaminataSigilo = 0f;
            frameAnimCaminataSigilo = 0;
            if (vp != null && !playerScript.EstaSaltando)
            {
                vp.localScale = Vector3.one;
                vp.localRotation = Quaternion.identity;
                vp.localPosition = Vector3.zero;
            }
            if (playerScript != null)
            {
                playerScript.bloquearResetVisualPivot = false;

                // Restaurar de inmediato el sprite erguido cuando no esté en sigilo
                if (!playerScript.EstaSaltando && !playerScript.EstaMuerto && playerVisualSr != null && playerScript.idleSprite != null)
                {
                    Vector2 vel = playerRb != null ? playerRb.linearVelocity : Vector2.zero;
                    if (vel.sqrMagnitude < 0.05f)
                    {
                        if (VigilanteCrouchSprites.EstaListo && playerVisualSr.sprite == VigilanteCrouchSprites.CrouchIdleSprite)
                        {
                            playerVisualSr.sprite = playerScript.idleSprite;
                        }
                    }
                }
            }
            return;
        }

        // MODO SIGILO ACTIVO: Asegurar que el pivot no sea reseteado por PlayerController
        if (playerScript != null)
        {
            playerScript.bloquearResetVisualPivot = true;
        }

        // MODO SIGILO ACTIVO: Garantizar movimiento fluido y animación de caminata sigilosa paso a paso
        Vector2 inputSigilo = LeerInputMovimientoConSoporteSigilo();
        if (playerRb != null && inputSigilo.sqrMagnitude > 0.01f)
        {
            playerRb.linearVelocity = inputSigilo * playerScript.velocidadCaminar;
        }

        bool caminandoEnSigilo = inputSigilo.sqrMagnitude > 0.01f ||
                                 (playerRb != null && playerRb.linearVelocity.sqrMagnitude > 0.15f);

        if (caminandoEnSigilo)
        {
            Vector2 dirActiva = inputSigilo.sqrMagnitude > 0.01f
                ? inputSigilo
                : playerRb.linearVelocity.normalized;

            if (dirActiva.x > 0.1f)
            {
                if (playerVisualSr != null) playerVisualSr.flipX = false;
                if (propPlayerFacingDirection != null) propPlayerFacingDirection.SetValue(playerScript, Vector2.right, null);
            }
            else if (dirActiva.x < -0.1f)
            {
                if (playerVisualSr != null) playerVisualSr.flipX = true;
                if (propPlayerFacingDirection != null) propPlayerFacingDirection.SetValue(playerScript, Vector2.left, null);
            }
            else if (dirActiva.y > 0.1f)
            {
                if (propPlayerFacingDirection != null) propPlayerFacingDirection.SetValue(playerScript, Vector2.up, null);
            }
            else if (dirActiva.y < -0.1f)
            {
                if (propPlayerFacingDirection != null) propPlayerFacingDirection.SetValue(playerScript, Vector2.down, null);
            }

            // Reproducir ciclo de caminata agachada en sigilo con los sprites auténticos proporcionados
            if (VigilanteCrouchSprites.EstaListo && VigilanteCrouchSprites.CrouchWalkFrames != null && VigilanteCrouchSprites.CrouchWalkFrames.Length > 0)
            {
                timerAnimCaminataSigilo += Time.deltaTime;
                if (timerAnimCaminataSigilo >= 0.11f)
                {
                    timerAnimCaminataSigilo = 0f;
                    frameAnimCaminataSigilo = (frameAnimCaminataSigilo + 1) % VigilanteCrouchSprites.CrouchWalkFrames.Length;
                }
                Sprite spPaso = VigilanteCrouchSprites.CrouchWalkFrames[frameAnimCaminataSigilo % VigilanteCrouchSprites.CrouchWalkFrames.Length];
                if (spPaso != null && playerVisualSr != null)
                {
                    playerVisualSr.sprite = spPaso;
                }

                if (vp != null && !playerScript.EstaSaltando)
                {
                    vp.localPosition = Vector3.zero;
                    vp.localScale = Vector3.one;
                    vp.localRotation = Quaternion.identity;
                }
            }
            else if (playerVisualSr != null && playerScript.walkSprites != null && playerScript.walkSprites.Length > 0)
            {
                timerAnimCaminataSigilo += Time.deltaTime;
                if (timerAnimCaminataSigilo >= 0.13f)
                {
                    timerAnimCaminataSigilo = 0f;
                    frameAnimCaminataSigilo = (frameAnimCaminataSigilo + 1) % playerScript.walkSprites.Length;
                }
                Sprite spPaso = playerScript.walkSprites[frameAnimCaminataSigilo % playerScript.walkSprites.Length];
                if (spPaso != null)
                {
                    playerVisualSr.sprite = spPaso;
                }
            }
        }
        else
        {
            timerAnimCaminataSigilo = 0.14f;
            frameAnimCaminataSigilo = 0;

            if (VigilanteCrouchSprites.EstaListo && VigilanteCrouchSprites.CrouchIdleSprite != null)
            {
                if (playerVisualSr != null && !playerScript.EstaSaltando)
                {
                    playerVisualSr.sprite = VigilanteCrouchSprites.CrouchIdleSprite;
                }

                if (vp != null && !playerScript.EstaSaltando)
                {
                    vp.localPosition = Vector3.zero;
                    vp.localScale = Vector3.one;
                    vp.localRotation = Quaternion.identity;
                }
            }
            else
            {
                if (playerVisualSr != null && playerScript.idleSprite != null && !playerScript.EstaSaltando)
                {
                    playerVisualSr.sprite = playerScript.idleSprite;
                }

                // Postura agachada quieta en Modo Sigilo (crouch)
                if (vp != null && !playerScript.EstaSaltando)
                {
                    float respSigilo = Mathf.Sin(Time.time * 2.2f) * 0.015f;
                    vp.localPosition = new Vector3(0f, -0.40f, 0f);
                    vp.localScale = new Vector3(1.26f, 0.65f + respSigilo, 1f);
                    vp.localRotation = Quaternion.identity;
                }
            }
        }
    }

    private void AlternarMenuConfiguracion()
    {
        menuConfiguracionAbierto = !menuConfiguracionAbierto;
        Time.timeScale = menuConfiguracionAbierto ? 0f : 1f;
    }

    public void ConfigurarJugadorYMicrofono()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponent<PlayerController>();
            playerRb = playerObj.GetComponent<Rigidbody2D>();
            playerRootSr = playerObj.GetComponent<SpriteRenderer>();

            Transform vp = playerTransform.Find("VisualPivot");
            if (vp != null)
            {
                playerVisualSr = vp.GetComponent<SpriteRenderer>();
            }

            if (playerScript != null)
            {
                playerScript.velocidadCaminar = 6.2f;
                playerScript.velocidadCorrer = 9.5f;
                playerScript.museoMinX = -26.5f;
                playerScript.museoMaxX = 26.5f;
                playerScript.museoMinY = -20.0f;
                playerScript.museoMaxY = 23.5f;

                VigilanteCrouchSprites.Inicializar(playerScript.idleSprite != null ? playerScript.idleSprite.pixelsPerUnit : 64f);
            }

            if (playerRb != null)
            {
                playerRb.gravityScale = 0f;
                playerRb.freezeRotation = true;
                playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                playerRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
            if (pm != null) pm.canMove = true;

            // Desactivar PasosJugadorNivel2 duplicado para que solo suenen los pasos fluidos nativos de PlayerController
            PasosJugadorNivel2 pasosDup = playerObj.GetComponent<PasosJugadorNivel2>();
            if (pasosDup != null && pasosDup.enabled)
            {
                pasosDup.enabled = false;
            }

            // Ocultar el rectángulo pixelado "VisualLinternaMano" para no tapar las manos ni la animación limpia de la Vigilante
            Transform linternaSub = playerTransform.Find("LinternaSubterraneo");
            if (linternaSub != null)
            {
                Transform visualMano = linternaSub.Find("VisualLinternaMano");
                if (visualMano != null)
                {
                    SpriteRenderer srMano = visualMano.GetComponent<SpriteRenderer>();
                    if (srMano != null) srMano.enabled = false;
                }
            }

            detectorMic = playerObj.GetComponent<DetectorMicrofonoNivel2>();
            if (detectorMic == null)
            {
                detectorMic = playerObj.AddComponent<DetectorMicrofonoNivel2>();
            }

            if (Application.isPlaying)
            {
                GarantizarMovimientoJugador();
            }
        }
    }

    public void ConfigurarMomia2()
    {
        momia2 = FindFirstObjectByType<MomiaNivel2Controller>();
        if (momia2 != null)
        {
            momia2.transform.localScale = Vector3.one;
            momia2.transform.localRotation = Quaternion.identity;
            momia2.CargarSpritesMomia2();
            return;
        }

        GameObject objExistente = GameObject.Find("Momia2_LaQueEscucha");
        GameObject objMomia2 = objExistente != null ? objExistente : new GameObject("Momia2_LaQueEscucha");
        if (objExistente == null)
        {
            objMomia2.transform.position = new Vector3(0f, 3.5f, 0f);
        }
        objMomia2.transform.localScale = Vector3.one;
        objMomia2.transform.localRotation = Quaternion.identity;

        SpriteRenderer sr = objMomia2.GetComponent<SpriteRenderer>();
        if (sr == null) sr = objMomia2.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 500;

        Rigidbody2D rb = objMomia2.GetComponent<Rigidbody2D>();
        if (rb == null) rb = objMomia2.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CapsuleCollider2D col = objMomia2.GetComponent<CapsuleCollider2D>();
        if (col == null) col = objMomia2.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Vertical;
        col.size = new Vector2(0.46f, 0.46f);
        col.offset = new Vector2(0f, 0.28f);

        momia2 = objMomia2.GetComponent<MomiaNivel2Controller>();
        if (momia2 == null)
        {
            momia2 = objMomia2.AddComponent<MomiaNivel2Controller>();
        }
    }

    // =========================================================================
    // AMBIENTACIÓN VISUAL 100% SUBTERRÁNEA (CATACUMBAS, CALDERAS Y SUBESTACIÓN)
    // CERO ELEMENTOS DEL NIVEL 1 - TODO EL ARTE ES EXCLUSIVO DEL SUBSUELO
    // =========================================================================

    private Transform decoEscenarioRef;
    private static Sprite spPisoSubterraneoDrenaje;
    private static Sprite spPisoCalderasIndustrial;
    private static Sprite spPisoCriptasFunerarias;
    private static Sprite spPisoSubestacionElectrica;
    private static Sprite spMuroSubterraneoHumedo;
    private static Sprite spGeneradorIndustrial;
    private static Sprite spTaquillaCerrada;
    private static Sprite spTaquillaOcupada;
    private static Sprite spSarcofagoPiedraCerrado;
    private static Sprite spSarcofagoPiedraOcupado;
    private static Sprite spRejaPortcullis;
    private static Sprite spManivelaValvula;
    private static Sprite spCajaFusibles;
    private static Sprite spPlacaFase;
    private static Sprite spCompuertaBlindadaCerrada;
    private static Sprite spCompuertaBlindadaAbierta;
    private static Sprite spCartelSalidaLuminoso;
    private static Sprite spNubeVaporSuave;
    private static Sprite spCableChispas;
    private static Sprite spPilarSubterraneo;
    private static Sprite spArcoSubterraneo;
    private static Sprite spBarrilesOxido;
    private static Sprite spCalderaVapor;
    private static Sprite spNichoOsario;
    private static Sprite spTransformadorElectrico;
    private static Sprite spRejillaAlcantarilla;

    /// <summary>
    /// Aplica una estética 100% subterránea (oscura, húmeda, industrial y de catacumbas)
    /// eliminando cualquier sprite heredado del Nivel 1 (sin alfombras, vitrinas ni estatuas de salón)
    /// y corrigiendo el colisionador de GeneradorPrincipal para que la Momia 2 jamás se trabe.
    /// </summary>
    public void EmbellecerEscenarioSubterraneoNivel2()
    {
        GameObject raizEscenario = GameObject.Find("SubterraneoNivel2");
        if (raizEscenario == null) return;

        // Si existía el contenedor anterior con props del Nivel 1, eliminarlo de inmediato
        Transform decoAntigua = raizEscenario.transform.Find("DecoracionColonialMuseoSubterraneo");
        if (decoAntigua != null)
        {
            if (Application.isPlaying) Destroy(decoAntigua.gameObject);
            else DestroyImmediate(decoAntigua.gameObject);
        }

        InicializarSpritesEscenarioNivel2();

        Transform[] todos = raizEscenario.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            Transform t = todos[i];
            if (t == null) continue;
            string n = t.name;
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();

            // 1. PISOS SUBTERRÁNEOS DIFERENCIADOS POR ZONA (Drenaje, Calderas de Acero, Criptas de Losa, Subestación)
            if (sr != null && (n.StartsWith("PisoBase_") || n.StartsWith("PisoSector_")))
            {
                Vector2 tamOriginal = sr.size;
                sr.drawMode = SpriteDrawMode.Tiled;

                if (n.Contains("Calderas"))
                {
                    sr.sprite = spPisoCalderasIndustrial;
                    sr.color = new Color(0.55f, 0.46f, 0.42f, 1f); // Acero oxidado y hollín de calderas
                }
                else if (n.Contains("Criptas"))
                {
                    sr.sprite = spPisoCriptasFunerarias;
                    sr.color = new Color(0.45f, 0.48f, 0.56f, 1f); // Losas sepulcrales frías de catacumba
                }
                else if (n.Contains("Generador") || n.Contains("Subestacion"))
                {
                    sr.sprite = spPisoSubestacionElectrica;
                    sr.color = new Color(0.46f, 0.52f, 0.50f, 1f); // Concreto de búnker con trincheras de cables
                }
                else
                {
                    sr.sprite = spPisoSubterraneoDrenaje;
                    sr.color = new Color(0.48f, 0.50f, 0.52f, 1f); // Adoquín subterráneo húmedo con musgo y drenaje
                }

                sr.size = tamOriginal;
                continue;
            }

            // 2. MUROS SUBTERRÁNEOS HÚMEDOS: Sillería pesada de sótano con humedad, musgo y refuerzos de hierro
            if (sr != null && n.StartsWith("Muro_"))
            {
                if (spMuroSubterraneoHumedo != null)
                {
                    Vector2 tamOriginal = sr.size;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.sprite = spMuroSubterraneoHumedo;
                    sr.size = tamOriginal;
                }
                sr.color = new Color(0.50f, 0.52f, 0.56f, 1f);
                continue;
            }

            // 3. GENERADOR PRINCIPAL: Dinamo subterráneo pesado y colisionador ajustado (sin bloqueo de 10.8m)
            if (n == "GeneradorPrincipal")
            {
                t.localScale = new Vector3(1.55f, 1.55f, 1f);
                if (sr != null && spGeneradorIndustrial != null)
                {
                    sr.sprite = spGeneradorIndustrial;
                    GeneradorPrincipalNivel2 genComp = t.GetComponent<GeneradorPrincipalNivel2>();
                    if (genComp == null || genComp.fasesRestauradas == 0)
                    {
                        sr.color = new Color(0.68f, 0.72f, 0.78f, 1f);
                    }
                }

                BoxCollider2D colGen = t.GetComponent<BoxCollider2D>();
                if (colGen != null && !colGen.isTrigger)
                {
                    colGen.size = new Vector2(1.55f, 1.35f);
                    colGen.offset = Vector2.zero;
                }

                Transform trigRep = t.Find("TriggerReparacionGenerador");
                if (trigRep != null)
                {
                    BoxCollider2D colTrig = trigRep.GetComponent<BoxCollider2D>();
                    if (colTrig != null) colTrig.size = new Vector2(4.6f, 4.6f);
                }
                continue;
            }

            // 4. ESCONDITES SUBTERRÁNEOS: Taquillas oxidadas de mantenimiento y Sarcófago de piedra de cripta
            EsconditeNivel2 esc = t.GetComponent<EsconditeNivel2>();
            if (esc != null && sr != null)
            {
                bool esAtaud = n.Contains("Ataud");
                if (!esAtaud)
                {
                    t.localScale = new Vector3(1.25f, 1.35f, 1f);
                    esc.spriteCerrado = spTaquillaOcupada;
                    esc.spriteAbierto = spTaquillaCerrada;
                    sr.sprite = esc.EstaOcupado ? spTaquillaOcupada : spTaquillaCerrada;
                    sr.color = Color.white;
                }
                else
                {
                    t.localScale = new Vector3(1.35f, 1.40f, 1f);
                    esc.spriteCerrado = spSarcofagoPiedraOcupado;
                    esc.spriteAbierto = spSarcofagoPiedraCerrado;
                    sr.sprite = esc.EstaOcupado ? spSarcofagoPiedraOcupado : spSarcofagoPiedraCerrado;
                    sr.color = Color.white;
                }
                continue;
            }

            // 5. REJAS MECÁNICAS Y VÁLVULAS DE HIERRO OXIDADO
            if (n == "RejaVisual" && sr != null && spRejaPortcullis != null)
            {
                Vector2 tamReja = sr.size;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.sprite = spRejaPortcullis;
                sr.size = tamReja;
                sr.color = Color.white;
                continue;
            }

            if (n == "ManivelaReja" && sr != null && spManivelaValvula != null)
            {
                sr.sprite = spManivelaValvula;
                sr.color = Color.white;
                t.localScale = new Vector3(0.95f, 0.95f, 1f);
                continue;
            }

            // 6. CAJAS DE FUSIBLES Y RÓTULOS DE FASE INDUSTRIALES
            InterruptorFusibleNivel2 caja = t.GetComponent<InterruptorFusibleNivel2>();
            if (caja != null && sr != null && spCajaFusibles != null)
            {
                t.localScale = new Vector3(1.35f, 1.35f, 1f);
                sr.sprite = spCajaFusibles;
                continue;
            }

            if (n == "RotuloFase" && sr != null && spPlacaFase != null)
            {
                sr.sprite = spPlacaFase;
                sr.color = Color.white;
                t.localScale = new Vector3(0.95f, 0.42f, 1f);
                continue;
            }

            // 7. COMPUERTA BLINDADA SUBTERRÁNEA Y CARTEL DE SALIDA
            CompuertaSalidaNivel2 compSalida = t.GetComponent<CompuertaSalidaNivel2>();
            if (compSalida != null && sr != null)
            {
                Vector2 tamComp = sr.size;
                compSalida.compuertaCerradaSprite = spCompuertaBlindadaCerrada;
                compSalida.compuertaAbiertaSprite = spCompuertaBlindadaAbierta;
                sr.sprite = spCompuertaBlindadaCerrada;
                sr.size = tamComp;
                continue;
            }

            if (n == "Cartel_SalidaEmergencia" && sr != null && spCartelSalidaLuminoso != null)
            {
                sr.sprite = spCartelSalidaLuminoso;
                sr.color = Color.white;
                t.localScale = new Vector3(1.65f, 0.52f, 1f);
                continue;
            }

            // 8. VAPOR DE CALDERAS Y ARCO ELÉCTRICO DE SUBESTACIÓN
            if (n == "VaporCalderas_Ambiental" && sr != null && spNubeVaporSuave != null)
            {
                sr.drawMode = SpriteDrawMode.Simple;
                sr.sprite = spNubeVaporSuave;
                t.localScale = new Vector3(1.35f, 1.10f, 1f);
                continue;
            }

            if (n == "CableChispas_Subestacion" && sr != null && spCableChispas != null)
            {
                sr.sprite = spCableChispas;
                sr.color = Color.white;
                t.localScale = new Vector3(0.95f, 0.95f, 1f);
                continue;
            }

            // 9. REEMPLAZAR PILARES, ARCOS Y CAJAS DEL NIVEL 1 POR ESTRUCTURAS 100% SUBTERRÁNEAS
            if (sr != null)
            {
                if (n == "PilarSubterraneo" && spPilarSubterraneo != null)
                {
                    sr.sprite = spPilarSubterraneo;
                    sr.color = Color.white;
                    t.localScale = new Vector3(1.25f, 1.45f, 1f);
                }
                else if (n == "ArcoPiedra" && spArcoSubterraneo != null)
                {
                    sr.sprite = spArcoSubterraneo;
                    sr.color = Color.white;
                    t.localScale = new Vector3(2.2f, 1.6f, 1f);
                }
                else if (n == "PilaCajas" && spBarrilesOxido != null)
                {
                    sr.sprite = spBarrilesOxido;
                    sr.color = Color.white;
                    t.localScale = new Vector3(1.35f, 1.35f, 1f);
                }
            }
        }

        // 10. AMBIENTACIÓN EXCLUSIVA DEL SUBSUELO (Calderas de vapor, Nichos de Osario, Transformadores y Alcantarillas)
        Transform decoSub = raizEscenario.transform.Find("AmbientacionSubterraneaExclusivaNivel2");
        if (decoSub == null)
        {
            GameObject decoRoot = new GameObject("AmbientacionSubterraneaExclusivaNivel2");
            decoRoot.transform.SetParent(raizEscenario.transform, false);
            decoEscenarioRef = decoRoot.transform;

            // Sala de Calderas (Oeste): Hornos de caldera de hierro fundido y barriles oxidados junto a los muros
            CrearDetalleSubterraneo(decoRoot.transform, "CalderaIndustrial_Norte", spCalderaVapor, new Vector3(-18.5f, 5.2f, 0f), new Vector3(1.45f, 1.45f, 1f), 4);
            CrearDetalleSubterraneo(decoRoot.transform, "CalderaIndustrial_Sur", spCalderaVapor, new Vector3(-18.5f, -8.2f, 0f), new Vector3(1.45f, 1.45f, 1f), 4);
            CrearDetalleSubterraneo(decoRoot.transform, "BarrilesCalderas_Sur", spBarrilesOxido, new Vector3(-9.5f, -17.5f, 0f), new Vector3(1.25f, 1.25f, 1f), 4);

            // Criptas y Osarios (Este): Nichos de cráneos/huesos de catacumba empotrados en los muros
            CrearDetalleSubterraneo(decoRoot.transform, "OsarioCatacumba_Norte", spNichoOsario, new Vector3(23.5f, 5.2f, 0f), new Vector3(1.4f, 1.4f, 1f), 4);
            CrearDetalleSubterraneo(decoRoot.transform, "OsarioCatacumba_Centro", spNichoOsario, new Vector3(23.5f, -3.5f, 0f), new Vector3(1.4f, 1.4f, 1f), 4);
            CrearDetalleSubterraneo(decoRoot.transform, "OsarioCatacumba_Sur", spNichoOsario, new Vector3(18.5f, -18.0f, 0f), new Vector3(1.4f, 1.4f, 1f), 4);
            CrearDetalleSubterraneo(decoRoot.transform, "OsarioCatacumba_Interior", spNichoOsario, new Vector3(9.4f, 1.5f, 0f), new Vector3(1.3f, 1.3f, 1f), 4);

            //Subestación Eléctrica (Norte): Transformadores de alta tensión de bobina
            CrearDetalleSubterraneo(decoRoot.transform, "Transformador_Noroeste", spTransformadorElectrico, new Vector3(-21.5f, 19.2f, 0f), new Vector3(1.4f, 1.4f, 1f), 4);
            CrearDetalleSubterraneo(decoRoot.transform, "Transformador_Noreste", spTransformadorElectrico, new Vector3(21.5f, 19.2f, 0f), new Vector3(1.4f, 1.4f, 1f), 4);
        }
        else
        {
            decoEscenarioRef = decoSub;
        }

        // =========================================================================
        // GARANTIZAR LAS 6 REJILLAS DE ALCANTARILLA EN TODAS LAS SALAS Y PASILLOS
        // Siempre se configuran y actualizan, sin depender de si decoSub existía o no.
        // =========================================================================
        Transform contenedorAlcantarillas = decoEscenarioRef != null ? decoEscenarioRef : raizEscenario.transform;
        AsegurarTodasLasAlcantarillas(contenedorAlcantarillas);

        // Agregar un poco más de ratas subterráneas repartidas en los distintos sectores del mapa
        AsegurarRataSubterranea(raizEscenario.transform, "Rata_Pasillo_DrenajeSur", new Vector2(-2.2f, -13.5f));
        AsegurarRataSubterranea(raizEscenario.transform, "Rata_Pasillo_DrenajeNorte", new Vector2(2.4f, 1.8f));
        AsegurarRataSubterranea(raizEscenario.transform, "Rata_Calderas_Centro", new Vector2(-13.5f, -3.5f));
        AsegurarRataSubterranea(raizEscenario.transform, "Rata_Criptas_Sarcofagos", new Vector2(13.5f, -8.5f));
        AsegurarRataSubterranea(raizEscenario.transform, "Rata_Subestacion_Oeste", new Vector2(-13.5f, 13.5f));
        AsegurarRataSubterranea(raizEscenario.transform, "Rata_Subestacion_Centro", new Vector2(5.5f, 12.5f));
    }

    private static void AsegurarDetalleSubterraneo(Transform parent, string nombre, Sprite sprite, Vector3 pos, Vector3 escala, int sortingOrder)
    {
        if (parent.Find(nombre) != null) return;
        CrearDetalleSubterraneo(parent, nombre, sprite, pos, escala, sortingOrder);
    }

    private static void AsegurarRataSubterranea(Transform parent, string nombre, Vector2 pos)
    {
        if (parent.Find(nombre) != null) return;
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.22f;

        go.AddComponent<RataSubterraneaNivel2>();
    }

    private static void CrearDetalleSubterraneo(Transform parent, string nombre, Sprite sprite, Vector3 pos, Vector3 escala, int sortingOrder)
    {
        if (sprite == null) return;
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        obj.transform.localScale = escala;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = sortingOrder;
    }

    public static Sprite ObtenerSpriteAlcantarilla()
    {
        if (spRejillaAlcantarilla != null) return spRejillaAlcantarilla;

        // Rejilla de hierro fundido antiguo del siglo XIX (Guanajuato minero):
        // 48x48 píxeles, PPU 38f para un diámetro compacto y natural de ~1.1 unidades.
        // Tonalidades de hierro oxidado, pátina oscura, aro de cantera húmeda y ranuras profundas de drenaje.
        spRejillaAlcantarilla = CrearSpriteProcedural(48, 48, 38f, (x, y) =>
        {
            float dx = x - 23.5f;
            float dy = y - 23.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 22.5f) return Color.clear;

            // Borde exterior biselado: encaje de piedra/cantera vieja y mortero oscuro
            if (r >= 20.0f)
            {
                float granoPiedra = Mathf.Sin(x * 1.5f + y * 1.1f) * 0.02f;
                float osc = (dy < 0) ? -0.025f : 0.015f;
                float vCantera = 0.14f + granoPiedra + osc;
                return new Color(vCantera * 1.05f, vCantera * 0.98f, vCantera * 0.90f, 1f);
            }

            // Marco circular de hierro fundido desgastado y oxidado (1890s)
            if (r >= 16.5f)
            {
                float oxido = Mathf.Max(0f, Mathf.Sin(x * 0.45f + y * 0.35f) * Mathf.Cos(x * 0.3f - y * 0.5f));
                Color hierroViejo = new Color(0.18f, 0.16f, 0.14f, 1f);
                Color hierroOxidado = new Color(0.32f, 0.18f, 0.11f, 1f);

                // Cuatro muescas/pernos antiguos en cruz
                bool muescaN = (Mathf.Abs(dx) <= 1.5f && Mathf.Abs(dy - 18.2f) <= 1.5f);
                bool muescaS = (Mathf.Abs(dx) <= 1.5f && Mathf.Abs(dy + 18.2f) <= 1.5f);
                bool muescaE = (Mathf.Abs(dx - 18.2f) <= 1.5f && Mathf.Abs(dy) <= 1.5f);
                bool muescaO = (Mathf.Abs(dx + 18.2f) <= 1.5f && Mathf.Abs(dy) <= 1.5f);
                if (muescaN || muescaS || muescaE || muescaO)
                {
                    return new Color(0.10f, 0.09f, 0.08f, 1f); // Hendidura profunda
                }

                return Color.Lerp(hierroViejo, hierroOxidado, oxido * 0.65f);
            }

            // Hendidura / bisel interior de asentamiento de la tapa
            if (r >= 14.8f)
            {
                return new Color(0.08f, 0.07f, 0.06f, 1f);
            }

            // =====================================================================
            // REJILLA INTERIOR: BARRAS GRUESAS DE HIERRO FORJADO ANTIGUO
            // =====================================================================
            // Cruceta estructural central
            bool cruceta = (Mathf.Abs(dx) <= 1.6f || Mathf.Abs(dy) <= 1.6f);

            // Anillo intermedio concéntrico
            bool nervaduraCircular = Mathf.Abs(r - 8.5f) <= 1.1f;

            // Ranuras paralelas de drenaje de aguas negras
            int modX = Mathf.Abs((int)dx) % 5;
            bool barraHierro = (modX <= 1);

            if (cruceta || nervaduraCircular || barraHierro)
            {
                // Hierro envejecido con corrosión oscura
                float desbaste = Mathf.Sin(x * 0.8f + y * 0.6f) * 0.02f;
                float luzBorde = (dy > 0f) ? 0.025f : -0.02f;
                float baseH = 0.22f + desbaste + luzBorde;
                Color colBarra = new Color(baseH * 1.05f, baseH * 0.95f, baseH * 0.88f, 1f);

                // Manchas sutiles de óxido minero
                if (((x * 5 + y * 11) % 7) == 0)
                {
                    colBarra = Color.Lerp(colBarra, new Color(0.35f, 0.20f, 0.12f, 1f), 0.5f);
                }
                return colBarra;
            }

            // Abismo interior del drenaje: sombra negra subterránea profunda
            return new Color(0.04f, 0.035f, 0.03f, 1f);
        });

        return spRejillaAlcantarilla;
    }

    public static void AsegurarAlcantarillaEnPosicion(Transform parent, string nombre, Vector3 pos)
    {
        Sprite sp = ObtenerSpriteAlcantarilla();
        Transform t = parent.Find(nombre);
        GameObject go = t != null ? t.gameObject : new GameObject(nombre);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        // Escala 1.05f natural y realista para una tapa de alcantarilla de drenaje (no gigantesca)
        go.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = Color.white;
        sr.sortingOrder = 3; // Por encima de los suelos (-1/-2) y charcos (1)

        // Limpiar cualquier luz residual para que mantenga la estética de hierro viejo y oscuro del subsuelo
        Transform luzExistente = go.transform.Find("LuzAlcantarilla");
        if (luzExistente != null)
        {
            if (Application.isPlaying) Object.Destroy(luzExistente.gameObject);
            else Object.DestroyImmediate(luzExistente.gameObject);
        }
    }

    public static void AsegurarTodasLasAlcantarillas(Transform root)
    {
        if (root == null) return;

        // Las alcantarillas NO son de adorno: existen EXACTAMENTE 4 puntos estratégicos
        // correspondientes a las 4 áreas/salas cerradas por donde la Momia transita por drenaje:
        // 1. Pasillo Central (Nodo 6): (0, -8)
        // 2. Sala de Calderas Oeste (Nodo 11): (-17, -2)
        // 3. Criptas y Osarios Este (Nodo 16): (17, -2)
        // 4. Subestación Eléctrica Norte (Nodo 20): (0, 10.4)
        string[] nombresValidos = new string[]
        {
            "Alcantarilla_PasilloCentral",
            "Alcantarilla_Calderas",
            "Alcantarilla_Criptas",
            "Alcantarilla_Subestacion"
        };

        HashSet<string> setValidos = new HashSet<string>(nombresValidos);
        List<GameObject> sobrantes = new List<GameObject>();

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("Alcantarilla", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!setValidos.Contains(child.name))
                {
                    sobrantes.Add(child.gameObject);
                }
            }
        }

        foreach (var obj in sobrantes)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        // Colocar y asegurar las 4 estratégicas
        AsegurarAlcantarillaEnPosicion(root, "Alcantarilla_PasilloCentral", new Vector3(0f, -8.0f, 0f));
        AsegurarAlcantarillaEnPosicion(root, "Alcantarilla_Calderas", new Vector3(-17.0f, -2.0f, 0f));
        AsegurarAlcantarillaEnPosicion(root, "Alcantarilla_Criptas", new Vector3(17.0f, -2.0f, 0f));
        AsegurarAlcantarillaEnPosicion(root, "Alcantarilla_Subestacion", new Vector3(0f, 10.4f, 0f));
    }

    private static void InicializarSpritesEscenarioNivel2()
    {
        if (spPisoSubterraneoDrenaje != null && spMuroSubterraneoHumedo != null && spGeneradorIndustrial != null)
            return;

        // 1A. Piso Subterráneo de Drenaje Central (64x64, PPU 64): Adoquín húmedo oscuro con musgo y canaleta
        spPisoSubterraneoDrenaje = CrearSpriteProcedural(64, 64, 64f, (x, y) =>
        {
            int fila = y / 16;
            int offset = (fila % 2) * 16;
            int bx = (x + offset) % 32;
            int by = y % 16;

            bool junta = (bx == 0 || by == 0 || bx == 31 || by == 15);
            if (junta)
            {
                // Musgo subterráneo oscuro en las juntas húmedas
                bool musgo = ((x * 7 + y * 13) % 11) < 3;
                return musgo ? new Color(0.14f, 0.22f, 0.16f, 1f) : new Color(0.12f, 0.13f, 0.15f, 1f);
            }

            float ruido = Mathf.Sin(x * 0.45f + y * 0.35f) * 0.03f + Mathf.Cos(x * 0.9f - y * 0.7f) * 0.02f;
            float v = 0.36f + (((fila + (x + offset) / 32) % 3 - 1) * 0.025f) + ruido;
            if (by == 14 || bx == 1) v += 0.05f;
            if (by == 1 || bx == 30) v -= 0.05f;
            return new Color(v * 0.92f, v * 0.96f, v * 1.04f, 1f);
        });

        // 1B. Piso Industrial de Sala de Calderas (64x64, PPU 64): Placa metálica antiderrapante con óxido
        spPisoCalderasIndustrial = CrearSpriteProcedural(64, 64, 64f, (x, y) =>
        {
            bool bordePlaca = (x == 0 || y == 0 || x == 32 || y == 32);
            if (bordePlaca) return new Color(0.14f, 0.12f, 0.11f, 1f);

            // Patrón de rombos antiderrapantes (diamond plate)
            int rx = x % 8;
            int ry = y % 8;
            bool relieve = (rx + ry == 4) || (rx - ry == 3) || (ry - rx == 3);

            // Manchas de óxido y hollín térmico
            float oxido = Mathf.Max(0f, Mathf.Sin(x * 0.22f + y * 0.18f) * Mathf.Cos(x * 0.15f - y * 0.27f));
            Color baseMetal = relieve ? new Color(0.46f, 0.44f, 0.42f, 1f) : new Color(0.32f, 0.30f, 0.29f, 1f);
            Color colOxido = new Color(0.48f, 0.26f, 0.16f, 1f);
            return Color.Lerp(baseMetal, colOxido, oxido * 0.55f);
        });

        // 1C. Piso de Criptas y Osarios (64x64, PPU 64): Losas funerarias agrietadas de piedra fría
        spPisoCriptasFunerarias = CrearSpriteProcedural(64, 64, 64f, (x, y) =>
        {
            int lx = x % 32;
            int ly = y % 32;
            if (lx == 0 || ly == 0 || lx == 31 || ly == 31)
                return new Color(0.10f, 0.11f, 0.14f, 1f);

            // Grieta diagonal en la losa funeraria
            bool grieta = Mathf.Abs((lx - 10) - (ly - 8) * 0.8f) < 0.9f && lx > 6 && lx < 22 && ((x / 32 + y / 32) % 2 == 0);
            if (grieta) return new Color(0.14f, 0.15f, 0.18f, 1f);

            // Borde grabado sepulcral
            bool grabado = ((lx == 4 || lx == 27) && ly >= 4 && ly <= 27) || ((ly == 4 || ly == 27) && lx >= 4 && lx <= 27);
            float v = 0.38f + Mathf.Sin(x * 0.3f + y * 0.5f) * 0.025f;
            if (grabado) v -= 0.07f;
            return new Color(v * 0.90f, v * 0.94f, v * 1.06f, 1f);
        });

        // 1D. Piso de Subestación Eléctrica (64x64, PPU 64): Concreto oscuro de búnker con rejillas de cables
        spPisoSubestacionElectrica = CrearSpriteProcedural(64, 64, 64f, (x, y) =>
        {
            if (x == 0 || y == 0) return new Color(0.14f, 0.16f, 0.18f, 1f);

            // Canaleta metálica de cableado de alta tensión a lo largo del borde
            if (x >= 26 && x <= 37)
            {
                if (x == 26 || x == 37) return new Color(0.55f, 0.46f, 0.16f, 1f); // Borde de precaución desgastado
                bool rejilla = (y % 4 == 0);
                return rejilla ? new Color(0.36f, 0.40f, 0.44f, 1f) : new Color(0.12f, 0.16f, 0.18f, 1f);
            }

            float v = 0.35f + Mathf.Sin(x * 0.7f + y * 0.7f) * 0.02f;
            return new Color(v * 0.92f, v * 0.98f, v * 0.96f, 1f);
        });

        // 2. Muro Subterráneo Húmedo de Sótano y Catacumba (64x64, PPU 64)
        spMuroSubterraneoHumedo = CrearSpriteProcedural(64, 64, 64f, (x, y) =>
        {
            int fila = y / 16;
            int offsetX = (fila % 2 == 0) ? 0 : 16;
            int bx = (x + offsetX) % 32;
            int by = y % 16;

            bool mortero = (by == 0 || by == 15 || bx == 0 || bx == 31);
            if (mortero) return new Color(0.10f, 0.11f, 0.13f, 1f);

            bool biselSup = (by == 13 || by == 14 || bx == 1);
            bool biselInf = (by == 1 || by == 2 || bx == 30);

            float humedad = Mathf.Max(0f, Mathf.Sin(x * 0.25f) * Mathf.Cos(y * 0.35f)) * 0.08f;
            float rugosidad = Mathf.Sin(x * 0.6f + fila * 2.1f) * 0.035f;
            float v = 0.38f - humedad + rugosidad;

            if (biselSup) v += 0.10f;
            else if (biselInf) v -= 0.10f;

            return new Color(v * 0.92f, v * 0.96f, v * 1.04f, 1f);
        });

        // 3. Generador Principal Industrial Subterráneo (96x96, PPU 36)
        spGeneradorIndustrial = CrearSpriteProcedural(96, 96, 36f, (x, y) =>
        {
            if (x < 4 || x > 91 || y < 4 || y > 91) return Color.clear;

            if (y < 16)
            {
                bool franja = ((x + y) / 6) % 2 == 0;
                return franja ? new Color(0.68f, 0.52f, 0.12f, 1f) : new Color(0.14f, 0.15f, 0.18f, 1f);
            }

            if (x == 6 || x == 89 || y == 16 || y == 89)
                return new Color(0.12f, 0.14f, 0.16f, 1f);

            // Bobinas de cobre envejecido
            if ((x >= 10 && x <= 26 || x >= 69 && x <= 85) && y >= 24 && y <= 78)
            {
                if (y % 5 == 0) return new Color(0.28f, 0.15f, 0.08f, 1f);
                float brillo = 1f - Mathf.Abs(((x % 16) - 8f) / 8f);
                return Color.Lerp(new Color(0.52f, 0.28f, 0.14f, 1f), new Color(0.82f, 0.52f, 0.26f, 1f), brillo);
            }

            float dx = x - 47.5f;
            float dy = y - 54f;
            float distNucleo = Mathf.Sqrt(dx * dx + dy * dy);
            if (distNucleo <= 18f)
            {
                if (distNucleo >= 15f) return new Color(0.55f, 0.46f, 0.24f, 1f);
                if (distNucleo <= 6f) return new Color(0.25f, 0.85f, 0.78f, 1f);
                float ang = Mathf.Atan2(dy, dx);
                bool aspa = Mathf.Sin(ang * 6f) > 0.2f;
                return aspa ? new Color(0.18f, 0.52f, 0.58f, 1f) : new Color(0.10f, 0.20f, 0.26f, 1f);
            }

            float acero = 0.32f + Mathf.Sin(y * 0.5f) * 0.03f;
            return new Color(acero * 0.90f, acero * 0.95f, acero * 1.02f, 1f);
        });

        // 4. Taquilla Subterránea Oxidada (Cerrada y Ocupada)
        spTaquillaCerrada = CrearSpriteTaquillaSubterranea(false);
        spTaquillaOcupada = CrearSpriteTaquillaSubterranea(true);

        // 5. Sarcófago de Piedra Agrietada de las Criptas (Cerrado y Ocupado)
        spSarcofagoPiedraCerrado = CrearSpriteSarcofagoCripta(false);
        spSarcofagoPiedraOcupado = CrearSpriteSarcofagoCripta(true);

        // 6. Reja Subterránea Oxidada
        spRejaPortcullis = CrearSpriteProcedural(64, 64, 32f, (x, y) =>
        {
            bool travesano = (y >= 8 && y <= 13) || (y >= 30 && y <= 35) || (y >= 52 && y <= 57);
            bool barrote = (x % 10 >= 3 && x % 10 <= 6);
            if (!travesano && !barrote) return Color.clear;
            if (y < 5 && (x % 10 < 4 || x % 10 > 5)) return Color.clear;

            float oxido = Mathf.Max(0f, Mathf.Sin(x * 0.4f + y * 0.3f)) * 0.35f;
            Color metal = (x % 10 == 4) ? new Color(0.42f, 0.44f, 0.46f, 1f) : new Color(0.24f, 0.26f, 0.28f, 1f);
            return Color.Lerp(metal, new Color(0.45f, 0.24f, 0.14f, 1f), oxido);
        });

        // 7. Válvula de Hierro Oxidado (ManivelaReja)
        spManivelaValvula = CrearSpriteProcedural(48, 48, 48f, (x, y) =>
        {
            float dx = x - 23.5f;
            float dy = y - 23.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 21f) return Color.clear;
            if (r >= 15.5f) return new Color(0.58f, 0.22f, 0.16f, 1f);
            if (r <= 5.5f) return new Color(0.48f, 0.42f, 0.32f, 1f);
            float ang = Mathf.Atan2(dy, dx);
            if (Mathf.Abs(Mathf.Sin(ang * 3f)) < 0.22f) return new Color(0.42f, 0.38f, 0.34f, 1f);
            return Color.clear;
        });

        // 8. Caja de Fusibles Subterránea
        spCajaFusibles = CrearSpriteProcedural(64, 64, 40f, (x, y) =>
        {
            if (x < 8 || x > 55 || y < 4 || y > 59) return Color.clear;
            if (y > 52 || y < 10)
                return (x >= 22 && x <= 28 || x >= 36 && x <= 42) ? new Color(0.36f, 0.32f, 0.28f, 1f) : Color.clear;
            if (x == 8 || x == 55 || y == 10 || y == 52) return new Color(0.14f, 0.16f, 0.18f, 1f);
            if (x >= 14 && x <= 49 && y >= 34 && y <= 46)
                return (y == 34 || y == 46 || x == 14 || x == 49) ? new Color(0.48f, 0.42f, 0.24f, 1f) : new Color(0.16f, 0.65f, 0.60f, 1f);
            if (x >= 42 && x <= 48 && y >= 16 && y <= 30)
                return y >= 25 ? new Color(0.75f, 0.18f, 0.14f, 1f) : new Color(0.55f, 0.48f, 0.24f, 1f);
            if (x >= 14 && x <= 36 && y >= 16 && y <= 24)
                return (((x + y) / 4) % 2 == 0) ? new Color(0.75f, 0.60f, 0.12f, 1f) : new Color(0.14f, 0.15f, 0.18f, 1f);
            return new Color(0.28f, 0.32f, 0.36f, 1f);
        });

        // 9. Placa Industrial de Fase (RotuloFase)
        spPlacaFase = CrearSpriteProcedural(64, 28, 32f, (x, y) =>
        {
            if (x < 2 || x > 61 || y < 2 || y > 25) return Color.clear;
            if (x <= 4 || x >= 59 || y <= 4 || y >= 23) return new Color(0.45f, 0.38f, 0.22f, 1f);
            bool franja = ((x + y) / 6) % 2 == 0;
            return franja ? new Color(0.72f, 0.58f, 0.14f, 1f) : new Color(0.14f, 0.16f, 0.18f, 1f);
        });

        // 10. Compuerta de Bóveda Subterránea
        spCompuertaBlindadaCerrada = CrearSpriteCompuertaNorte(false);
        spCompuertaBlindadaAbierta = CrearSpriteCompuertaNorte(true);

        // 11. Cartel Enrejado de Salida Subterránea
        spCartelSalidaLuminoso = CrearSpriteProcedural(64, 24, 32f, (x, y) =>
        {
            if (x < 2 || x > 61 || y < 2 || y > 21) return Color.clear;
            if (x <= 4 || x >= 59 || y <= 4 || y >= 19) return new Color(0.18f, 0.20f, 0.22f, 1f);
            bool letra = (y >= 8 && y <= 15) && (x % 8 >= 2 && x % 8 <= 5) && x >= 10 && x <= 53;
            return letra ? new Color(0.75f, 0.95f, 0.80f, 1f) : new Color(0.08f, 0.48f, 0.24f, 1f);
        });

        // 12. Nube de Vapor Suave
        spNubeVaporSuave = CrearSpriteProcedural(64, 64, 32f, (x, y) =>
        {
            float nx = (x - 31.5f) / 31.5f;
            float ny = (y - 31.5f) / 31.5f;
            float d1 = Mathf.Sqrt(nx * nx + ny * ny);
            float d2 = Mathf.Sqrt((nx - 0.25f) * (nx - 0.25f) + (ny + 0.15f) * (ny + 0.15f));
            float d3 = Mathf.Sqrt((nx + 0.25f) * (nx + 0.25f) + (ny - 0.18f) * (ny - 0.18f));
            float alpha = Mathf.Clamp01(Mathf.Clamp01(1f - d1 * 1.15f) * 0.5f + Mathf.Clamp01(1f - d2 * 1.45f) * 0.35f + Mathf.Clamp01(1f - d3 * 1.45f) * 0.35f);
            alpha = alpha * alpha * 0.48f;
            return new Color(0.78f, 0.82f, 0.86f, alpha);
        });

        // 13. Cable con Chispas
        spCableChispas = CrearSpriteProcedural(48, 48, 48f, (x, y) =>
        {
            float dx = x - 23.5f;
            float dy = y - 23.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 22f) return Color.clear;
            if (r < 5f) return new Color(0.85f, 0.98f, 1f, 1f);
            float ang = Mathf.Atan2(dy, dx);
            if (Mathf.Abs(Mathf.Sin(ang * 5f + r * 0.45f)) < 0.18f && r < 18f)
                return new Color(1f, 0.88f, 0.25f, 0.95f);
            return new Color(0.25f, 0.68f, 0.95f, Mathf.Clamp01(1f - r / 20f) * 0.28f);
        });

        // 14. Pilar Subterráneo de Piedra Agrietada con Abrazaderas de Hierro (Reemplaza prop_pillar del Nivel 1)
        spPilarSubterraneo = CrearSpriteProcedural(48, 64, 32f, (x, y) =>
        {
            if (x < 8 || x > 39 || y < 2 || y > 61) return Color.clear;
            // Base y capitel de roca subterránea
            bool capitel = (y < 10 || y > 53);
            if (!capitel && (x < 12 || x > 35)) return Color.clear;

            // Abrazaderas de hierro oxidado
            if (y >= 20 && y <= 23 || y >= 40 && y <= 43)
                return new Color(0.28f, 0.22f, 0.18f, 1f);

            // Musgo húmedo en la base del pilar
            float musgo = (y < 18) ? (18 - y) / 18f * 0.35f : 0f;
            float sombra = 0.38f - Mathf.Abs(x - 23.5f) / 28f * 0.16f;
            Color piedra = new Color(sombra * 0.92f, sombra * 0.96f, sombra * 1.04f, 1f);
            return Color.Lerp(piedra, new Color(0.16f, 0.26f, 0.18f, 1f), musgo);
        });

        // 15. Arco Subterráneo de Tubería y Piedra (Reemplaza prop_arch del Nivel 1)
        spArcoSubterraneo = CrearSpriteProcedural(64, 48, 32f, (x, y) =>
        {
            if (x < 2 || x > 61 || y < 2 || y > 45) return Color.clear;
            bool columnaIzq = (x >= 4 && x <= 14);
            bool columnaDer = (x >= 49 && x <= 59);
            bool dintelTuberia = (y >= 34 && y <= 44);
            if (!columnaIzq && !columnaDer && !dintelTuberia) return Color.clear;

            if (y == 36 || y == 42) return new Color(0.45f, 0.26f, 0.16f, 1f); // Tubería de vapor oxidada en el dintel
            float v = 0.34f + Mathf.Sin(x * 0.4f + y * 0.4f) * 0.04f;
            return new Color(v * 0.92f, v * 0.95f, v, 1f);
        });

        // 16. Barriles Industriales Oxidados (Reemplaza prop_crate del Nivel 1)
        spBarrilesOxido = CrearSpriteProcedural(48, 48, 32f, (x, y) =>
        {
            if (x < 6 || x > 41 || y < 3 || y > 44) return Color.clear;
            float dx = Mathf.Abs(x - 23.5f);
            if (dx > 16f) return Color.clear;

            // Aros de refuerzo del tambor metálico
            if (y <= 6 || y >= 41 || Mathf.Abs(y - 18) <= 1 || Mathf.Abs(y - 30) <= 1)
                return new Color(0.22f, 0.20f, 0.18f, 1f);

            float cil = 1f - (dx / 16f) * 0.45f;
            return new Color(0.48f * cil, 0.26f * cil, 0.16f * cil, 1f);
        });

        // 17. Caldera Subterránea de Vapor con Horno Incandescente (Exclusivo de Sala de Calderas)
        spCalderaVapor = CrearSpriteProcedural(64, 64, 32f, (x, y) =>
        {
            float dx = x - 31.5f;
            float dy = y - 31.5f;
            if (Mathf.Abs(dx) > 24f || y < 4 || y > 59) return Color.clear;

            // Rejilla del horno inferior con brasas naranjas tenues
            if (Mathf.Abs(dx) <= 12f && y >= 10 && y <= 22)
            {
                bool barrote = (x % 3 == 0);
                return barrote ? new Color(0.16f, 0.14f, 0.14f, 1f) : new Color(0.85f, 0.35f, 0.10f, 1f);
            }

            // Manómetro de presión superior
            float distMan = Mathf.Sqrt(dx * dx + (y - 44f) * (y - 44f));
            if (distMan <= 7f)
                return distMan >= 5.5f ? new Color(0.58f, 0.46f, 0.22f, 1f) : new Color(0.82f, 0.86f, 0.80f, 1f);

            float cil = 1f - Mathf.Abs(dx) / 26f * 0.5f;
            return new Color(0.34f * cil, 0.26f * cil, 0.24f * cil, 1f);
        });

        // 18. Nicho de Osario de Catacumba (Exclusivo de Criptas y Osarios)
        spNichoOsario = CrearSpriteProcedural(64, 48, 32f, (x, y) =>
        {
            if (x < 4 || x > 59 || y < 4 || y > 43) return Color.clear;
            if (x <= 8 || x >= 55 || y <= 8 || y >= 39 || (y >= 22 && y <= 25))
                return new Color(0.28f, 0.30f, 0.35f, 1f); // Marco de piedra de nicho funerario

            // Interior oscuro con cráneos y huesos antiguos apilados
            int celdaX = (x - 9) % 11;
            int celdaY = (y - 9) % 13;
            float distCraneo = Mathf.Sqrt((celdaX - 5f) * (celdaX - 5f) + (celdaY - 6f) * (celdaY - 6f));
            if (distCraneo <= 4.2f)
            {
                // Cuencas oculares oscuras del cráneo
                if (celdaY >= 5 && celdaY <= 7 && (celdaX == 3 || celdaX == 7))
                    return new Color(0.10f, 0.10f, 0.12f, 1f);
                return new Color(0.68f, 0.64f, 0.54f, 1f);
            }
            return new Color(0.10f, 0.11f, 0.14f, 1f);
        });

        // 19. Transformador de Alta Tensión (Exclusivo de Subestación Norte)
        spTransformadorElectrico = CrearSpriteProcedural(64, 64, 32f, (x, y) =>
        {
            if (x < 8 || x > 55 || y < 4 || y > 59) return Color.clear;
            // Aisladores cerámicos superiores con discos
            if (y >= 42)
            {
                bool colAislador = (Mathf.Abs(x - 18) <= 4 || Mathf.Abs(x - 32) <= 4 || Mathf.Abs(x - 46) <= 4);
                if (!colAislador) return Color.clear;
                return (y % 4 == 0) ? new Color(0.20f, 0.26f, 0.30f, 1f) : new Color(0.38f, 0.52f, 0.56f, 1f);
            }
            // Aletas de enfriamiento del transformador
            bool aleta = (x % 4 == 0) && x >= 12 && x <= 51 && y >= 10 && y <= 38;
            return aleta ? new Color(0.18f, 0.22f, 0.25f, 1f) : new Color(0.30f, 0.35f, 0.40f, 1f);
        });

        // 20. Rejilla de Alcantarilla Subterránea de Hierro Forjado (Alta Definición y Contraste)
        spRejillaAlcantarilla = CrearSpriteProcedural(64, 64, 32f, (x, y) =>
        {
            float dx = x - 31.5f;
            float dy = y - 31.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 30.5f) return Color.clear;

            // Borde exterior biselado de hierro oscuro y sombra
            if (r >= 28.5f) return new Color(0.12f, 0.13f, 0.15f, 1f);

            // Marco circular ancho de hierro fundido (con brillo metálico plateado superior y sombra inferior)
            if (r >= 22.5f)
            {
                // Tornillos / remaches hexagonales en los 4 puntos cardinales
                bool remacheNorte = (Mathf.Abs(dx) <= 2.2f && Mathf.Abs(dy - 25.5f) <= 2.2f);
                bool remacheSur = (Mathf.Abs(dx) <= 2.2f && Mathf.Abs(dy + 25.5f) <= 2.2f);
                bool remacheEste = (Mathf.Abs(dx - 25.5f) <= 2.2f && Mathf.Abs(dy) <= 2.2f);
                bool remacheOeste = (Mathf.Abs(dx + 25.5f) <= 2.2f && Mathf.Abs(dy) <= 2.2f);
                if (remacheNorte || remacheSur || remacheEste || remacheOeste)
                {
                    return new Color(0.92f, 0.90f, 0.85f, 1f); // Remache brillante de acero
                }

                float brilloMarco = 0.52f + (dy / 25f) * 0.18f;
                return new Color(brilloMarco * 0.94f, brilloMarco * 0.96f, brilloMarco * 1.02f, 1f);
            }

            // Anillo interior de retención
            if (r >= 20.5f) return new Color(0.18f, 0.20f, 0.22f, 1f);

            // Nervaduras centrales / cruceta de acero grueso
            bool cruceta = (Mathf.Abs(dx) <= 2.2f || Mathf.Abs(dy) <= 2.2f);
            if (cruceta)
            {
                float brilloCruceta = 0.65f - (Mathf.Abs(dx) + Mathf.Abs(dy)) * 0.02f;
                return new Color(brilloCruceta * 0.95f, brilloCruceta * 0.98f, brilloCruceta * 1.05f, 1f);
            }

            // Anillo concéntrico intermedio
            if (Mathf.Abs(r - 11.5f) <= 1.4f)
            {
                return new Color(0.60f, 0.63f, 0.68f, 1f);
            }

            // Barrotes paralelos de rejilla de drenaje
            int modX = Mathf.Abs((int)dx) % 6;
            bool barrote = (modX <= 1);
            if (barrote)
            {
                return new Color(0.58f, 0.62f, 0.66f, 1f); // Acero claro visible
            }

            // Ranura profunda de drenaje: negro abismo subterráneo
            return new Color(0.04f, 0.05f, 0.07f, 1f);
        });
    }

    private static Sprite CrearSpriteTaquillaSubterranea(bool ocupada)
    {
        return CrearSpriteProcedural(48, 72, 32f, (x, y) =>
        {
            if (x < 4 || x > 43 || y < 2 || y > 69) return Color.clear;
            if (x <= 6 || x >= 41 || y <= 4 || y >= 67)
                return new Color(0.14f, 0.16f, 0.18f, 1f);
            if (x == 9 || x == 38 || y == 7 || y == 64)
                return new Color(0.20f, 0.22f, 0.25f, 1f);

            // Rejillas de ventilación
            if (x >= 13 && x <= 34 && y >= 48 && y <= 60 && (y % 3 == 0))
            {
                if (ocupada && y >= 51 && y <= 57 && (Mathf.Abs(x - 19) <= 2 || Mathf.Abs(x - 28) <= 2))
                    return new Color(0.25f, 0.85f, 0.55f, 1f);
                return new Color(0.08f, 0.09f, 0.11f, 1f);
            }
            if (x >= 13 && x <= 34 && y >= 11 && y <= 20 && (y % 3 == 0))
                return new Color(0.08f, 0.09f, 0.11f, 1f);

            // Manija oxidada
            if (x >= 32 && x <= 36 && y >= 31 && y <= 40)
                return new Color(0.48f, 0.42f, 0.34f, 1f);

            float oxido = Mathf.Max(0f, Mathf.Sin(x * 0.35f + y * 0.25f)) * 0.28f;
            float grad = 0.32f + (1f - Mathf.Abs(x - 24f) / 20f) * 0.08f;
            Color baseMetal = new Color(grad * 0.88f, grad * 0.94f, grad, 1f);
            return Color.Lerp(baseMetal, new Color(0.38f, 0.22f, 0.15f, 1f), oxido);
        });
    }

    private static Sprite CrearSpriteSarcofagoCripta(bool ocupado)
    {
        return CrearSpriteProcedural(48, 72, 32f, (x, y) =>
        {
            float anchoMax = (y < 48)
                ? Mathf.Lerp(13f, 20f, y / 48f)
                : Mathf.Lerp(20f, 14f, (y - 48f) / 24f);

            float dx = Mathf.Abs(x - 23.5f);
            if (y < 3 || y > 68 || dx > anchoMax) return Color.clear;

            // Borde de piedra cincelada oscura
            if (dx >= anchoMax - 2.2f || y <= 5 || y >= 66)
                return new Color(0.22f, 0.24f, 0.28f, 1f);

            // Relieve sepulcral en la losa de piedra
            bool cruzVert = dx <= 2.0f && y >= 18 && y <= 56;
            bool cruzHoriz = dx <= 8.5f && y >= 42 && y <= 46;
            if (cruzVert || cruzHoriz)
            {
                return ocupado
                    ? new Color(0.25f, 0.82f, 0.55f, 1f)
                    : new Color(0.20f, 0.22f, 0.26f, 1f);
            }

            float grieta = Mathf.Sin(y * 0.35f + dx * 0.4f) * 0.03f;
            float basePiedra = 0.38f + grieta;
            return new Color(basePiedra * 0.92f, basePiedra * 0.95f, basePiedra * 1.04f, 1f);
        });
    }

    private static Sprite CrearSpriteCompuertaNorte(bool abierta)
    {
        return CrearSpriteProcedural(96, 64, 32f, (x, y) =>
        {
            if (x < 2 || x > 93 || y < 2 || y > 61) return Color.clear;
            if (x < 10 || x > 85 || y > 53)
            {
                bool remache = (x % 8 == 4 && y % 8 == 4);
                return remache ? new Color(0.52f, 0.46f, 0.38f, 1f) : new Color(0.24f, 0.26f, 0.30f, 1f);
            }
            if (abierta)
            {
                float luz = 0.28f + (y / 64f) * 0.40f;
                return new Color(luz * 0.35f, luz * 0.85f, luz * 0.55f, 0.85f);
            }
            if (Mathf.Abs(x - 47.5f) <= 1.5f) return new Color(0.10f, 0.12f, 0.14f, 1f);
            if (y >= 24 && y <= 32)
            {
                bool franja = ((x + y) / 6) % 2 == 0;
                return franja ? new Color(0.72f, 0.56f, 0.14f, 1f) : new Color(0.16f, 0.18f, 0.20f, 1f);
            }
            return new Color(0.34f, 0.37f, 0.42f, 1f);
        });
    }

    private static Sprite CrearSpriteProcedural(int ancho, int alto, float ppu, System.Func<int, int, Color> generadorPixel)
    {
        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color[] pixels = new Color[ancho * alto];
        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                pixels[y * ancho + x] = generadorPixel(x, y);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0f, 0f, ancho, alto),
            new Vector2(0.5f, 0.5f),
            ppu,
            0,
            SpriteMeshType.FullRect
        );
    }

    private void CargarTexturasJumpscare()
    {
        texturasJumpscare = new Texture2D[4];
        for (int i = 0; i < 4; i++)
        {
            texturasJumpscare[i] = Resources.Load<Texture2D>("Momia2/momia2_scream_" + i);
            if (texturasJumpscare[i] == null)
            {
                Sprite sp = MomiaNivel2Controller.CargarSpriteSeguro("Momia2/momia2_scream_" + i);
                if (sp != null) texturasJumpscare[i] = sp.texture;
            }
        }

        if (texturasJumpscare[0] == null)
        {
            Texture2D texBase = Resources.Load<Texture2D>("Momia2/momia2_pose0_scream");
            if (texBase != null) texturasJumpscare[0] = texBase;
        }
    }

    private void AlGenerarseRuidoEnMundo(Vector3 posicion, float radio, string etiqueta)
    {
        alertaRuidoTexto = etiqueta;
        tiempoAlertaRuido = 2.0f;

        if (momia2 != null)
        {
            bool esVoz = etiqueta != null && etiqueta.Contains("MICRÓFONO");
            if (esVoz)
            {
                momia2.ReaccionarAVozMicrofono(posicion, radio);
            }
            else
            {
                Color colorOnda = new Color(0.55f, 0.82f, 0.95f, 0.48f);
                momia2.GenerarOndaAcusticaMomia(posicion, Mathf.Clamp(radio * 0.28f, 2.0f, 5.5f), colorOnda);
            }
        }
    }

    private void ActivarJumpscareGameOver(string motivoEspecial)
    {
        if (jumpscareActivado) return;
        jumpscareActivado = true;
        menuConfiguracionAbierto = false;
        tiempoMuerteJumpscare = 0f;
        motivoPersonalizadoMuerte = motivoEspecial;

        if (Nivel2Manager.Instance != null)
        {
            Nivel2Manager.Instance.enabled = false;
        }

        Time.timeScale = 0f;

        // Silenciar inmediatamente cualquier sonido de pasos de la vigilante o charcos al perder
        if (playerFootstepAudio != null)
        {
            playerFootstepAudio.Stop();
            if (playerFootstepAudio.clip != null) playerFootstepAudio.time = 0f;
            playerFootstepAudio.volume = 0f;
            playerFootstepAudio.loop = false;
        }

        if (playerScript != null)
        {
            AudioSource[] todasLasFuentes = playerScript.GetComponentsInChildren<AudioSource>(true);
            foreach (var fuente in todasLasFuentes)
            {
                if (fuente != null && fuente != audioJumpscare)
                {
                    fuente.Stop();
                    if (fuente.clip != null) fuente.time = 0f;
                    fuente.volume = 0f;
                    fuente.loop = false;
                }
            }
        }

        PasosJugadorNivel2 pasosCarrera = FindFirstObjectByType<PasosJugadorNivel2>();
        if (pasosCarrera != null)
        {
            AudioSource asP = pasosCarrera.GetComponent<AudioSource>();
            if (asP != null) { asP.Stop(); asP.time = 0f; }
            pasosCarrera.enabled = false;
        }

        TrampaAcusticaNivel2[] trampas = FindObjectsByType<TrampaAcusticaNivel2>(FindObjectsSortMode.None);
        foreach (var trampa in trampas)
        {
            AudioSource asT = trampa.GetComponent<AudioSource>();
            if (asT != null) { asT.Stop(); asT.time = 0f; asT.loop = false; }
        }

        if (audioJumpscare != null)
        {
            AudioClip clipPerder = AudioManagerNivel2.ObtenerClip(AudioManagerNivel2.CLIP_PERDER);
            if (clipPerder != null)
            {
                clipJumpscare = clipPerder;
            }
            else if (clipJumpscare == null)
            {
                clipJumpscare = GenerarAudioJumpscareTerror();
            }
            audioJumpscare.PlayOneShot(clipJumpscare, 1.0f);
        }
    }

    private void ReiniciarNivel2()
    {
        Time.timeScale = 1f;
        Scene escena = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escena.name);
    }

    private void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    private static AudioClip GenerarAudioJumpscareTerror()
    {
        int sampleRate = 22050;
        float duracion = 1.45f;
        int totalMuestras = Mathf.CeilToInt(sampleRate * duracion);
        float[] muestras = new float[totalMuestras];
        System.Random rng = new System.Random(66613);

        for (int i = 0; i < totalMuestras; i++)
        {
            float t = (float)i / sampleRate;
            float prog = t / duracion;

            float slam = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(115f, 32f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 4.5f) * 0.95f;
            float vibrato = Mathf.Sin(2f * Mathf.PI * 26f * t) * 35f;
            float gritoAgudo = Mathf.Sin(2f * Mathf.PI * (690f + vibrato - prog * 140f) * t) * 0.48f;
            float gritoDisonante = Mathf.Sin(2f * Mathf.PI * (985f - vibrato * 1.3f) * t) * 0.38f;
            float gutural = Mathf.Sin(2f * Mathf.PI * 148f * t) * 0.45f;
            float ruido = ((float)rng.NextDouble() * 2f - 1f) * 0.52f;

            float env = 1f;
            if (prog < 0.03f) env = prog / 0.03f;
            else if (prog > 0.72f) env = Mathf.Clamp01(1f - ((prog - 0.72f) / 0.28f));

            float mix = (slam + (gritoAgudo + gritoDisonante + gutural + ruido) * env);
            muestras[i] = Mathf.Clamp((float)System.Math.Tanh(mix * 1.65f), -0.98f, 0.98f);
        }

        AudioClip clip = AudioClip.Create("Jumpscare_Momia2_Terror", totalMuestras, 1, sampleRate, false);
        clip.SetData(muestras, 0);
        return clip;
    }

    private void AsegurarEstilosGUI()
    {
        if (texBlanca == null)
        {
            texBlanca = new Texture2D(1, 1);
            texBlanca.SetPixel(0, 0, Color.white);
            texBlanca.Apply();
        }

        if (estiloHeaderCuadrante != null) return;

        estiloHeaderCuadrante = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            clipping = TextClipping.Overflow,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        estiloHeaderCuadrante.normal.textColor = new Color(0.96f, 0.85f, 0.45f);

        estiloTextoCuadrante = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Overflow,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        estiloTextoCuadrante.normal.textColor = new Color(0.90f, 0.92f, 0.95f);

        estiloRelojHora = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        estiloRelojHora.normal.textColor = new Color(1f, 0.86f, 0.25f);

        estiloRelojSub = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        estiloRelojSub.normal.textColor = new Color(0.85f, 0.88f, 0.92f);

        estiloBarraMicTexto = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        estiloBarraMicTexto.normal.textColor = Color.white;

        estiloTituloMuerte = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        estiloTituloMuerte.normal.textColor = new Color(0.96f, 0.18f, 0.14f);

        estiloSubtituloMuerte = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            richText = true
        };
        estiloSubtituloMuerte.normal.textColor = new Color(0.92f, 0.88f, 0.86f);

        estiloBotonModal = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
    }

    private void OnGUI()
    {
        AsegurarEstilosGUI();

        float s = ResponsiveGUIHelper.BeginScaledGUI(out float vw, out float vh, out Matrix4x4 prevMatrix, 860f, 480f);
        try
        {
            if (jumpscareActivado)
            {
                DibujarSecuenciaJumpscareYGameOverUnico(vw, vh);
                return;
            }

            if (victoriaActivada)
            {
                DibujarModalVictoriaUnico(vw, vh);
                return;
            }

            // Si el plano del subsuelo está abierto en pantalla, ocultar completamente el HUD para no encimarlo
            if (PlanoSubterraneoNivel2.VisorPlanoAbierto)
            {
                return;
            }

            DibujarVinetaTerrorPantalla(vw, vh);

            // === LOS 3 CUADRANTES EXCLUSIVAMENTE EN LA PARTE SUPERIOR ===
            DibujarTresCuadrantesSuperiores(vw, vh);

            // Aviso flotante para volver a cerrar una reja abierta y encerrar a la Momia (Opción 2)
            if (rejaAbiertaCercanaParaCerrar != null && rejaAbiertaCercanaParaCerrar.estaAbierta)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 screenPos = cam.WorldToScreenPoint(rejaAbiertaCercanaParaCerrar.transform.position + new Vector3(0f, 1.2f, 0f));
                    if (screenPos.z > 0f)
                    {
                        Vector2 guiPos = ResponsiveGUIHelper.WorldScreenToVirtualGUI(screenPos, s);
                        float w = 265f;
                        float h = 26f;
                        float px = Mathf.Clamp(guiPos.x - w * 0.5f, 8f, vw - w - 8f);
                        float py = Mathf.Clamp(guiPos.y - h * 0.5f, 8f, vh - h - 8f);
                        Rect rectPrompt = new Rect(px, py, w, h);
                        DibujarCajaConBorde(rectPrompt, new Color(0.08f, 0.08f, 0.12f, 0.92f), new Color(0.95f, 0.65f, 0.20f, 0.85f));
                        GUI.Label(rectPrompt, "<color=#FFAA33><b>[E] Bajar Reja Oxidada (Encerrar)</b></color>", estiloRelojSub);
                    }
                }
            }

            // Pie de controles discreto abajo
            GUI.Label(new Rect(12f, vh - 22f, Mathf.Min(920f, vw - 24f), 18f),
                "<color=#888888>WASD/Flechas: Mover | Ctrl/C: Modo Sigilo | Shift: Correr (¡Hace ruido!) | Espacio: Saltar | E: Interactuar | M/P: Plano | ESC: Configuración</color>",
                estiloTextoCuadrante);

            if (menuConfiguracionAbierto)
            {
                DibujarModalConfiguracionJuego(vw, vh);
            }
        }
        finally
        {
            ResponsiveGUIHelper.EndScaledGUI(prevMatrix);
        }
    }

    /// <summary>
    /// Dibuja los 3 cuadrantes alineados simétricamente en la franja superior de la pantalla:
    /// 1. Izquierda: Vigilante (Salud + Modo Activo nítido + 3 Fases de Energía).
    /// 2. Centro: Reloj en tiempo real (01:00 AM -> 02:00 AM + Cuenta regresiva).
    /// 3. Derecha: Únicamente la Barra del Micrófono (sin botones ni sliders) y el estado de la Momia.
    /// </summary>
    private void DibujarTresCuadrantesSuperiores(float vw, float vh)
    {
        float margen = 10f;
        float topY = 10f;
        float altoCuadrante = 92f;

        // Calcular anchos responsivos para que nunca se encimen entre sí
        float anchoDisponible = vw - (margen * 4f);
        float anchoIzq = Mathf.Clamp(anchoDisponible * 0.34f, 250f, 350f);
        float anchoDer = Mathf.Clamp(anchoDisponible * 0.34f, 250f, 350f);
        float anchoCentro = Mathf.Clamp(anchoDisponible * 0.26f, 200f, 280f);

        // -------------------------------------------------------------------------
        // CUADRANTE 1 (IZQUIERDA SUPERIOR): SALUD DEL VIGILANTE, MODO Y FASES DE ENERGÍA
        // -------------------------------------------------------------------------
        Rect rectIzq = new Rect(margen, topY, anchoIzq, altoCuadrante);
        DibujarCajaConBorde(rectIzq, new Color(0.05f, 0.06f, 0.09f, 0.90f), new Color(0.35f, 0.40f, 0.48f, 0.75f));

        float vidaPct = 1f;
        if (playerScript != null && playerScript.vidaMaxima > 0f)
        {
            vidaPct = Mathf.Clamp01(playerScript.vidaActual / playerScript.vidaMaxima);
        }

        // Fila 1: Título de Vigilante + Barra de salud limpia a su lado
        GUI.Label(new Rect(rectIzq.x + 10f, rectIzq.y + 4f, 105f, 18f),
            "VIGILANTE: " + Mathf.CeilToInt(vidaPct * 100f) + "%", estiloHeaderCuadrante);

        float barraSaludX = rectIzq.x + 118f;
        float barraSaludW = rectIzq.width - 128f;
        DibujarRectanguloColor(new Rect(barraSaludX, rectIzq.y + 8f, barraSaludW, 10f), new Color(0.18f, 0.08f, 0.08f, 0.95f));
        Color colSalud = Color.Lerp(new Color(0.92f, 0.18f, 0.15f), new Color(0.22f, 0.85f, 0.42f), vidaPct);
        DibujarRectanguloColor(new Rect(barraSaludX + 1f, rectIzq.y + 9f, (barraSaludW - 2f) * vidaPct, 8f), colSalud);

        // Fila 2: Indicador dedicado y nítido del Modo Activo (sin entrecortarse ni encimarse)
        string estadoModoStr;
        bool shiftCorriendo = EsInputCorrerActivo() &&
                              (playerRb != null && playerRb.linearVelocity.sqrMagnitude > 0.8f);
        if (ModoSigiloActivo)
            estadoModoStr = "<color=#55DDFF><b>[MODO SIGILO]</b> Silencioso (Paso lento)</color>";
        else if (shiftCorriendo)
            estadoModoStr = "<color=#FFAA33><b>[CORRIENDO]</b> Rápido (¡Peligro de ruido!)</color>";
        else
            estadoModoStr = "<color=#99BBDD><b>[CAMINATA NORMAL]</b></color>";

        GUI.Label(new Rect(rectIzq.x + 10f, rectIzq.y + 23f, rectIzq.width - 20f, 18f), estadoModoStr, estiloTextoCuadrante);

        // Fila 3: Progreso de las 3 fases eléctricas
        GeneradorPrincipalNivel2 gen = GeneradorPrincipalNivel2.Instance;
        int fases = gen != null ? gen.fasesRestauradas : 0;
        int maxFases = gen != null ? gen.fasesRequeridas : 3;
        bool f1 = gen != null && gen.EstaFaseRestaurada(1);
        bool f2 = gen != null && gen.EstaFaseRestaurada(2);
        bool f3 = gen != null && gen.EstaFaseRestaurada(3);

        string strF1 = f1 ? "<color=#00FFAA>[✓]1.Calderas</color>" : "<color=#888888>[ ]1.Calderas</color>";
        string strF2 = f2 ? "<color=#00FFAA>[✓]2.Criptas</color>" : "<color=#888888>[ ]2.Criptas</color>";
        string strF3 = f3 ? "<color=#00FFAA>[✓]3.Subest.</color>" : "<color=#888888>[ ]3.Subest.</color>";

        string textoEnergia = fases >= maxFases
            ? "<color=#00FFAA><b>¡ENERGÍA 100%! COMPUERTA ABIERTA</b></color>"
            : string.Format("Energía: <b>{0}/{1}</b> Fases activadas", fases, maxFases);

        GUI.Label(new Rect(rectIzq.x + 10f, rectIzq.y + 41f, rectIzq.width - 20f, 17f), textoEnergia, estiloTextoCuadrante);
        GUI.Label(new Rect(rectIzq.x + 10f, rectIzq.y + 58f, rectIzq.width - 16f, 17f),
            string.Format("{0}  {1}  {2}", strF1, strF2, strF3), estiloTextoCuadrante);

        float pctEnergia = Mathf.Clamp01((float)fases / Mathf.Max(1, maxFases));
        DibujarRectanguloColor(new Rect(rectIzq.x + 10f, rectIzq.y + 76f, rectIzq.width - 20f, 5f), new Color(0.12f, 0.14f, 0.18f, 0.95f));
        DibujarRectanguloColor(new Rect(rectIzq.x + 10f, rectIzq.y + 76f, (rectIzq.width - 20f) * pctEnergia, 5f),
            fases >= maxFases ? new Color(0.2f, 0.9f, 0.45f) : new Color(0.95f, 0.76f, 0.18f));

        // -------------------------------------------------------------------------
        // CUADRANTE 2 (CENTRO SUPERIOR): RELOJ Y CUENTA DE TIEMPO EN VIVO
        // -------------------------------------------------------------------------
        float centroX = (vw - anchoCentro) * 0.5f;
        Rect rectCentro = new Rect(centroX, topY, anchoCentro, altoCuadrante);
        bool pocoTiempo = (DuracionNivelSegundos - tiempoTranscurridoNivel) < 40f;
        Color bordeReloj = pocoTiempo ? new Color(0.95f, 0.22f, 0.18f, 0.90f) : new Color(0.55f, 0.48f, 0.25f, 0.80f);
        DibujarCajaConBorde(rectCentro, new Color(0.05f, 0.06f, 0.09f, 0.90f), bordeReloj);

        string colorRelojHex = pocoTiempo ? "#FF4444" : "#FFD700";
        GUI.Label(new Rect(rectCentro.x, rectCentro.y + 7f, rectCentro.width, 24f),
            string.Format("<color={0}>⏱ {1}</color>", colorRelojHex, textoReloj), estiloRelojHora);

        GUI.Label(new Rect(rectCentro.x, rectCentro.y + 34f, rectCentro.width, 18f),
            string.Format("<color=#FFFFFF>Tiempo restante: <b>{0}</b></color>", textoCuentaRegresiva), estiloRelojSub);

        string subtituloCentro;
        if (tiempoAlertaRuido > 0f && !string.IsNullOrEmpty(alertaRuidoTexto))
        {
            subtituloCentro = "<color=#FF5555>⚠ " + alertaRuidoTexto + "</color>";
        }
        else if (fases >= maxFases)
        {
            subtituloCentro = "<color=#00FFAA>▲ Ve a la Compuerta Norte ▲</color>";
        }
        else
        {
            subtituloCentro = "<color=#AAAAAA>NIVEL 2: Escapa antes de las 2:00 AM</color>";
        }

        GUI.Label(new Rect(rectCentro.x + 4f, rectCentro.y + 56f, rectCentro.width - 8f, 18f), subtituloCentro, estiloRelojSub);

        // -------------------------------------------------------------------------
        // CUADRANTE 3 (DERECHA SUPERIOR): SOLO BARRA DE MICRÓFONO Y ESTADO DE MOMIA
        // -------------------------------------------------------------------------
        Rect rectDer = new Rect(vw - anchoDer - margen, topY, anchoDer, altoCuadrante);
        bool alertaMic = detectorMic != null && detectorMic.SuperandoUmbral;
        bool momiaEnAlerta = momia2 != null && momia2.estadoActual != MomiaNivel2Controller.EstadoMomia2.AcechandoEnOscuridad;

        Color bordeDer = (alertaMic || momiaEnAlerta)
            ? new Color(0.95f, 0.20f, 0.18f, 0.95f)
            : new Color(0.35f, 0.42f, 0.52f, 0.75f);

        DibujarCajaConBorde(rectDer, new Color(0.05f, 0.06f, 0.09f, 0.90f), bordeDer);

        GUI.Label(new Rect(rectDer.x + 10f, rectDer.y + 6f, rectDer.width - 20f, 18f),
            "MICRÓFONO ACÚSTICO", estiloHeaderCuadrante);

        // Barra única del micrófono (sin botones ni sliders en el HUD)
        float barraX = rectDer.x + 10f;
        float barraY = rectDer.y + 27f;
        float barraAncho = rectDer.width - 20f;
        float barraAlto = 20f;

        DibujarRectanguloColor(new Rect(barraX, barraY, barraAncho, barraAlto), new Color(0.12f, 0.14f, 0.18f, 0.95f));

        float nivel = detectorMic != null ? Mathf.Clamp01(detectorMic.NivelVolumenActual) : 0f;
        float umbral = detectorMic != null ? Mathf.Clamp01(detectorMic.UmbralDisparo) : 0.25f;
        float db = detectorMic != null ? detectorMic.DecibeliosAprox : -60f;

        Color colorBarra;
        if (nivel >= umbral)
            colorBarra = new Color(0.95f, 0.18f, 0.15f, 0.95f);
        else if (nivel >= umbral * 0.7f)
            colorBarra = new Color(0.95f, 0.72f, 0.18f, 0.95f);
        else
            colorBarra = new Color(0.22f, 0.85f, 0.45f, 0.95f);

        DibujarRectanguloColor(new Rect(barraX + 2f, barraY + 2f, (barraAncho - 4f) * nivel, barraAlto - 4f), colorBarra);

        // Línea amarilla del umbral de peligro
        float posUmbralX = barraX + Mathf.Clamp(umbral * barraAncho, 4f, barraAncho - 4f);
        DibujarRectanguloColor(new Rect(posUmbralX - 1.5f, barraY - 1f, 3f, barraAlto + 2f), new Color(1f, 0.92f, 0.30f, 1f));

        string textoBarra;
        if (detectorMic != null && detectorMic.JugadorEnEsconditeSeguro)
            textoBarra = "OCULTO EN ESCONDITE (SEGURO)";
        else if (alertaMic)
            textoBarra = "¡RUIDO DETECTADO! (" + Mathf.RoundToInt(db) + " dB)";
        else
            textoBarra = string.Format("Voz: {0:0} dB ({1:0}%)", db, nivel * 100f);

        GUI.Label(new Rect(barraX + 4f, barraY, barraAncho - 8f, barraAlto), textoBarra, estiloBarraMicTexto);

        // Línea compacta inferior: estado de escucha de la Momia 2
        string estadoMomiaStr = "<color=#99AABB>Momia 2: Acechando en silencio</color>";
        if (momia2 != null)
        {
            if (momia2.EstaEjecutandoAtaqueRasguno)
                estadoMomiaStr = "<color=#FF2222><b>Momia 2: ¡TE ESTÁ RASGUÑANDO!</b></color>";
            else if (momia2.EstaUsandoAlcantarilla)
                estadoMomiaStr = "<color=#55EEDD><b>Momia 2: ¡Moviéndose por alcantarillas!</b></color>";
            else if (momia2.EstaGolpeandoRejaEncerrada)
                estadoMomiaStr = "<color=#FFAA33><b>Momia 2: ¡ENCERRADA! Golpeando la reja</b></color>";
            else if (momia2.estadoActual == MomiaNivel2Controller.EstadoMomia2.CazandoContactoLetal)
                estadoMomiaStr = "<color=#FF3333><b>Momia 2: ¡TE ESTÁ CAZANDO!</b></color>";
            else if (momia2.estadoActual == MomiaNivel2Controller.EstadoMomia2.AlertaAcusticaGrito ||
                     momia2.estadoActual == MomiaNivel2Controller.EstadoMomia2.InvestigandoSonido)
                estadoMomiaStr = "<color=#FFAA33><b>Momia 2: ¡Rastreando tu sonido!</b></color>";
            else if (momia2.distanciaAlJugador < 12f)
                estadoMomiaStr = ModoSigiloActivo
                    ? string.Format("<color=#55DDFF>Momia cerca (~{0:0.0}m) - Sigilo activo</color>", momia2.distanciaAlJugador)
                    : string.Format("<color=#EEDD88>Momia cerca (~{0:0.0}m) - Usa [Ctrl/C] Sigilo</color>", momia2.distanciaAlJugador);
        }

        GUI.Label(new Rect(rectDer.x + 10f, rectDer.y + 57f, rectDer.width - 20f, 20f), estadoMomiaStr, estiloTextoCuadrante);
    }

    /// <summary>
    /// Menú de Configuración del Juego (accesible solo con ESC).
    /// ÚNICO lugar donde se puede ajustar la sensibilidad del micrófono o el volumen general.
    /// </summary>
    private void DibujarModalConfiguracionJuego(float vw, float vh)
    {
        DibujarRectanguloColor(new Rect(0f, 0f, vw, vh), new Color(0f, 0f, 0f, 0.78f));

        float w = Mathf.Min(480f, vw - 24f);
        float h = Mathf.Min(295f, vh - 24f);
        float x = (vw - w) * 0.5f;
        float y = (vh - h) * 0.5f;

        DibujarCajaConBorde(new Rect(x, y, w, h),
            new Color(0.06f, 0.07f, 0.10f, 0.98f),
            new Color(0.85f, 0.72f, 0.30f, 0.95f));

        GUI.Label(new Rect(x + 20f, y + 14f, w - 40f, 30f),
            "<color=#FFD700>CONFIGURACIÓN DEL JUEGO</color>", estiloRelojHora);

        // 1. Volumen General del Juego
        float volActual = AudioListener.volume;
        GUI.Label(new Rect(x + 28f, y + 58f, 220f, 20f),
            "Volumen del Juego: " + Mathf.RoundToInt(volActual * 100f) + "%", estiloTextoCuadrante);
        float nuevoVol = GUI.HorizontalSlider(new Rect(x + 215f, y + 63f, w - 245f, 18f), volActual, 0f, 1f);
        if (Mathf.Abs(nuevoVol - volActual) > 0.001f)
        {
            AudioListener.volume = nuevoVol;
            PlayerPrefs.SetFloat("VolumenMusica", nuevoVol);
            PlayerPrefs.Save();
        }

        // 2. Sensibilidad del Micrófono (exclusivo en Configuración)
        if (detectorMic != null)
        {
            int pctSens = Mathf.RoundToInt(detectorMic.sensibilidad * 100f);
            GUI.Label(new Rect(x + 28f, y + 98f, 220f, 20f),
                "Sensibilidad del Micrófono: " + pctSens + "%", estiloTextoCuadrante);
            float nuevaSens = GUI.HorizontalSlider(new Rect(x + 215f, y + 103f, w - 245f, 18f), detectorMic.sensibilidad, 0.15f, 0.95f);
            if (Mathf.Abs(nuevaSens - detectorMic.sensibilidad) > 0.001f)
            {
                detectorMic.CambiarSensibilidadDesdeConfiguracion(nuevaSens);
            }

            GUI.Label(new Rect(x + 28f, y + 134f, w - 56f, 20f),
                "Dispositivo activo: <color=#00FFAA>" + detectorMic.NombreDispositivo + "</color>", estiloTextoCuadrante);

            if (GUI.Button(new Rect(x + 28f, y + 160f, w - 56f, 30f), "Cambiar Dispositivo de Micrófono", estiloBotonModal))
            {
                detectorMic.CambiarDispositivoDesdeConfiguracion();
            }
        }

        float btnW = (w - 76f) * 0.5f;
        if (GUI.Button(new Rect(x + 28f, y + h - 68f, btnW, 44f), "CONTINUAR [ESC]", estiloBotonModal))
        {
            AlternarMenuConfiguracion();
        }

        if (GUI.Button(new Rect(x + w - 28f - btnW, y + h - 68f, btnW, 44f), "MENÚ PRINCIPAL", estiloBotonModal))
        {
            IrAlMenuPrincipal();
        }
    }

    private void DibujarModalVictoriaUnico(float vw, float vh)
    {
        DibujarRectanguloColor(new Rect(0f, 0f, vw, vh), new Color(0.02f, 0.05f, 0.04f, 0.94f));

        float w = Mathf.Min(500f, vw - 24f);
        float h = Mathf.Min(230f, vh - 24f);
        float x = (vw - w) * 0.5f;
        float y = (vh - h) * 0.5f;

        DibujarCajaConBorde(new Rect(x, y, w, h),
            new Color(0.06f, 0.08f, 0.10f, 0.96f),
            new Color(0.25f, 0.90f, 0.45f, 0.95f));

        GUI.Label(new Rect(x + 20f, y + 18f, w - 40f, 36f),
            "<color=#00FFAA>¡NIVEL 2 SUPERADO!</color>", estiloTituloMuerte);

        GUI.Label(new Rect(x + 24f, y + 66f, w - 48f, 65f),
            "Has restaurado las 3 fases eléctricas y escapado de La Que Escucha a las " + textoReloj + ".",
            estiloSubtituloMuerte);

        if (GUI.Button(new Rect(x + (w - 240f) * 0.5f, y + h - 68f, 240f, 44f), "INICIAR NIVEL 3 ▶", estiloBotonModal))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Nivel 3");
        }
    }

    private void DibujarVinetaTerrorPantalla(float vw, float vh)
    {
        if (momia2 == null || !Application.isPlaying) return;

        float factorPeligro = 0f;
        if (momia2.distanciaAlJugador < 10f)
        {
            factorPeligro = Mathf.Clamp01(1f - (momia2.distanciaAlJugador / 10f)) * 0.38f;
        }
        if (momia2.estadoActual == MomiaNivel2Controller.EstadoMomia2.AlertaAcusticaGrito ||
            momia2.estadoActual == MomiaNivel2Controller.EstadoMomia2.CazandoContactoLetal)
        {
            factorPeligro = Mathf.Max(factorPeligro, 0.32f + Mathf.Sin(Time.time * 12f) * 0.10f);
        }

        if (factorPeligro <= 0.01f) return;

        Color colorSangre = new Color(0.55f, 0.02f, 0.02f, factorPeligro);
        float grosor = Mathf.Lerp(20f, 80f, factorPeligro);

        DibujarRectanguloColor(new Rect(0f, 0f, vw, grosor), colorSangre);
        DibujarRectanguloColor(new Rect(0f, vh - grosor, vw, grosor), colorSangre);
        DibujarRectanguloColor(new Rect(0f, 0f, grosor, vh), colorSangre);
        DibujarRectanguloColor(new Rect(vw - grosor, 0f, grosor, vh), colorSangre);
    }

    /// <summary>
    /// FASE 1 (0 a 1.55s): Jumpscare violento a pantalla completa donde la Momia se abalanza hacia la cámara.
    /// FASE 2 (> 1.55s): Termina el jumpscare y aparece UN SOLO aviso limpio de Game Over sin amontonarse.
    /// </summary>
    private void DibujarSecuenciaJumpscareYGameOverUnico(float vw, float vh)
    {
        if (tiempoMuerteJumpscare < DuracionSecuenciaJumpscare)
        {
            float t = tiempoMuerteJumpscare / DuracionSecuenciaJumpscare;

            bool flashSangre = Mathf.Sin(tiempoMuerteJumpscare * 55f) > 0.35f;
            Color colorFondoJumpscare = flashSangre
                ? new Color(0.38f, 0.01f, 0.02f, 0.98f)
                : new Color(0.02f, 0.01f, 0.02f, 0.98f);
            DibujarRectanguloColor(new Rect(0f, 0f, vw, vh), colorFondoJumpscare);

            if (texturasJumpscare == null || texturasJumpscare.Length == 0 || texturasJumpscare[0] == null)
            {
                CargarTexturasJumpscare();
            }

            if (texturasJumpscare != null && texturasJumpscare.Length > 0)
            {
                int idx = Mathf.FloorToInt(tiempoMuerteJumpscare * 18f) % texturasJumpscare.Length;
                Texture2D texActual = texturasJumpscare[idx] ?? texturasJumpscare[0];

                if (texActual != null)
                {
                    float progresoEmbestida = Mathf.Clamp01(t * 1.45f);
                    float curvaImpacto = 1f - Mathf.Pow(1f - progresoEmbestida, 3f);

                    float multiplicadorAlto = Mathf.Lerp(0.85f, 2.25f, curvaImpacto);
                    float altoImg = vh * multiplicadorAlto;
                    float aspectRatio = (float)texActual.width / Mathf.Max(1f, texActual.height);
                    float anchoImg = altoImg * aspectRatio;

                    float intensidadShake = Mathf.Lerp(38f, 14f, t);
                    float shakeX = Mathf.Sin(tiempoMuerteJumpscare * 95f) * intensidadShake + Random.Range(-12f, 12f);
                    float shakeY = Mathf.Cos(tiempoMuerteJumpscare * 110f) * intensidadShake + Random.Range(-12f, 12f);

                    float focoRostroY = Mathf.Lerp((vh - altoImg) * 0.5f, -altoImg * 0.06f, curvaImpacto);

                    Rect rectJumpscare = new Rect(
                        (vw - anchoImg) * 0.5f + shakeX,
                        focoRostroY + shakeY,
                        anchoImg,
                        altoImg
                    );

                    Color prevColor = GUI.color;
                    if (flashSangre && Random.value > 0.5f)
                    {
                        GUI.color = new Color(1f, 0.78f, 0.75f, 1f);
                    }
                    GUI.DrawTexture(rectJumpscare, texActual, ScaleMode.ScaleToFit, true);
                    GUI.color = prevColor;
                }
            }

            Color bordeRojo = new Color(0.75f, 0.02f, 0.02f, 0.55f);
            DibujarRectanguloColor(new Rect(0f, 0f, vw, 36f), bordeRojo);
            DibujarRectanguloColor(new Rect(0f, vh - 36f, vw, 36f), bordeRojo);
            return;
        }

        // === FASE 2: UN SOLO AVISO LIMPIO DE GAME OVER ===
        DibujarRectanguloColor(new Rect(0f, 0f, vw, vh), new Color(0.08f, 0.01f, 0.02f, 0.96f));

        float panelAncho = Mathf.Min(520f, vw - 24f);
        float panelAlto = Mathf.Min(235f, vh - 24f);
        float panelX = (vw - panelAncho) * 0.5f;
        float panelY = (vh - panelAlto) * 0.5f;

        DibujarCajaConBorde(new Rect(panelX, panelY, panelAncho, panelAlto),
            new Color(0.06f, 0.07f, 0.09f, 0.96f),
            new Color(0.88f, 0.18f, 0.15f, 0.95f));

        GUI.Label(new Rect(panelX + 20f, panelY + 18f, panelAncho - 40f, 38f),
            "GAME OVER", estiloTituloMuerte);

        string causa = !string.IsNullOrEmpty(motivoPersonalizadoMuerte)
            ? motivoPersonalizadoMuerte
            : (momia2 != null ? momia2.ultimoSonidoEscuchado : "El sonido delató tu posición en la oscuridad.");

        GUI.Label(new Rect(panelX + 24f, panelY + 64f, panelAncho - 48f, 75f),
            "La momia que escucha te atrapó en la oscuridad.\n" +
            "Causa: " + causa + "\n" +
            "Recuerda: camina con sigilo y mantén silencio en tu micrófono.",
            estiloSubtituloMuerte);

        float goBtnW = (panelAncho - 90f) * 0.5f;
        if (GUI.Button(new Rect(panelX + 35f, panelY + panelAlto - 66f, goBtnW, 44f), "REINTENTAR [R]", estiloBotonModal))
        {
            ReiniciarNivel2();
        }

        if (GUI.Button(new Rect(panelX + panelAncho - 35f - goBtnW, panelY + panelAlto - 66f, goBtnW, 44f), "MENÚ PRINCIPAL", estiloBotonModal))
        {
            IrAlMenuPrincipal();
        }
    }

    private void DibujarCajaConBorde(Rect rect, Color colorFondo, Color colorBorde)
    {
        DibujarRectanguloColor(new Rect(rect.x - 1.5f, rect.y - 1.5f, rect.width + 3f, rect.height + 3f), colorBorde);
        DibujarRectanguloColor(rect, colorFondo);
    }

    private void DibujarRectanguloColor(Rect rect, Color color)
    {
        if (texBlanca == null) return;
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texBlanca);
        GUI.color = prev;
    }
}
