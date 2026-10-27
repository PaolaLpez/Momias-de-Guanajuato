using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LevelState
{
    SafeExploration,
    TheAwakening,
    KeyAcquired,
    Escape
}

public class Level1Manager : MonoBehaviour
{
    public static Level1Manager Instance { get; private set; }

    [Header("Estado Actual")]
    [SerializeField] private LevelState currentState = LevelState.SafeExploration;
    public LevelState CurrentState => currentState;

    [Header("Referencias de Entidades Clave")]
    [SerializeField] private GameObject mainMummyObject;
    [SerializeField] private Transform mummyAwakenSpawnPoint;
    [SerializeField] private List<GameObject> minorEnemySpawners = new List<GameObject>();
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Objetivos e Interactables")]
    [SerializeField] private GameObject deskObject;
    [SerializeField] private GameObject keyObject;
    [SerializeField] private GameObject securityDoorObject;

    [Header("Audio y Atmósfera")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioClip glassBreakClip;
    [SerializeField] private AudioClip awakeningMusicClip;

    [Header("UI y Escenas")]
    [SerializeField] private string nextSceneName = "Nivel 2";

    [Header("Reloj del Museo (12:00 AM a 1:00 AM)")]
    public float duracionNivelSegundos = 240f; // 4 minutos reales = 1 hora de juego
    private float tiempoTranscurrido = 0f;
    private string textoReloj = "12:00 AM";
    private string tiempoRestanteStr = "04:00";
    private bool nivelCompletado = false;
    private bool juegoTerminado = false;
    public bool IsNivelCompletado => nivelCompletado;
    public bool IsJuegoTerminado => juegoTerminado;
    private string motivoGameOver = "La momia te atrapó en la oscuridad.\nRecuerda: no debes quitarle los ojos de encima.";

    // Referencias cacheadas para el HUD
    private PlayerController player;
    private MomiaController momia;
    private CameraFollow camFollow;

    // Recursos visuales GUI
    private Texture2D texVidaVigilante;
    private Texture2D texVidaMomia;
    private Texture2D texFondoBarra;
    private Texture2D texPanel;
    private Texture2D texPanelGameOver;
    private Texture2D texAuraMomia;
    private Texture2D[] texMomiaGritando;
    private Texture2D[] texMomiaSonriendo;
    private Texture2D texAuraRadialMomia;
    private Texture2D texPanelVictoria;
    private Texture2D texBotonVictoria;
    private Texture2D texBotonVictoriaHover;
    private Texture2D texBordeVictoria;
    private AudioSource audioGameOver;
    private AudioClip clipGritoMomia;

    private GUIStyle headerStyle;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;
    private GUIStyle clockTitleStyle;
    private GUIStyle clockSubStyle;
    private GUIStyle gameOverPanelStyle;
    private GUIStyle gameOverTitleStyle;
    private GUIStyle gameOverReasonStyle;
    private GUIStyle gameOverButtonStyle;
    private GUIStyle victoryBoxStyle;
    private GUIStyle victoryTitleStyle;
    private GUIStyle victoryHeaderStyle;
    private GUIStyle victoryDescStyle;
    private GUIStyle victoryQuoteStyle;
    private GUIStyle victoryButtonStyle;
    private GUIStyle textStyleCenter;

    // Mensajes temporales en pantalla
    private string bannerMessage = "";
    private float bannerTimer = 0f;

    // Eventos globales del nivel
    public static event Action OnPistolPicked;
    public static event Action<LevelState> OnStateChanged;
    public static event Action OnKeyCollected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ResolverReferencias();
    }

    private void Start()
    {
        AjustadorColisionesMuseo.AjustarTodo();
        CrearTexturas();
        ResolverReferencias();
        SetState(LevelState.SafeExploration);

        // Iniciar música ambiental tenebrosa a volumen sutil equilibrado
        ConfigurarMusicaTenebrosaDeFondo();

        // En cuanto comienza el nivel, la momia se activa y comienza a acechar
        if (momia != null)
        {
            momia.gameObject.SetActive(true);
            momia.enabled = true;
        }

        AsegurarEscarabajosEnMuseo();
    }

    private void AsegurarEscarabajosEnMuseo()
    {
        MinorEnemyAI[] enemigos = FindObjectsByType<MinorEnemyAI>(FindObjectsSortMode.None);
        if (enemigos == null || enemigos.Length < 6)
        {
            // Escarabajos distribuidos estratégicamente por las salas del museo
            SpawnEscarabajo(new Vector3(-6f, 3f, 0f));     // Pasillo Central Oeste
            SpawnEscarabajo(new Vector3(6f, 3f, 0f));      // Pasillo Central Este
            SpawnEscarabajo(new Vector3(18f, 12f, 0f));    // Galería Noreste
            SpawnEscarabajo(new Vector3(23f, 15f, 0f));    // Galería Noreste Fondo
            SpawnEscarabajo(new Vector3(-18f, -12f, 0f));  // Cripta Sudoeste
            SpawnEscarabajo(new Vector3(-24f, -14f, 0f));  // Cripta Esquina
            SpawnEscarabajo(new Vector3(18f, -12f, 0f));   // Almacén Sudeste
            SpawnEscarabajo(new Vector3(24f, -15f, 0f));   // Almacén Fondo
            SpawnEscarabajo(new Vector3(0f, 12f, 0f));     // Bóveda Norte
            SpawnEscarabajo(new Vector3(-15f, 14f, 0f));   // Oficina Noroeste
            Debug.Log("<color=green>[Level1Manager] ¡Escarabajos colocados en las salas para ser descubiertos al explorar!</color>");
        }
    }

    public void SpawnEscarabajo(Vector3 pos)
    {
        GameObject enemy = new GameObject("Escarabajo_Tumba");
        enemy.transform.position = pos;
        enemy.transform.localScale = new Vector3(0.35f, 0.35f, 1f); // Escarabajo más pequeño

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Enemies/beetle_walk_0");
        sr.sortingOrder = 8;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        enemy.AddComponent<MinorEnemyAI>();
    }

    public void ResolverReferencias()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerController>();
        }

        if (mainMummyObject == null)
        {
            GameObject m = GameObject.Find("Momia1_LaQueCamina");
            if (m == null)
            {
                MomiaController mc = FindFirstObjectByType<MomiaController>();
                if (mc != null) m = mc.gameObject;
            }
            mainMummyObject = m;
        }

        if (momia == null)
        {
            if (mainMummyObject != null) momia = mainMummyObject.GetComponent<MomiaController>();
            if (momia == null) momia = FindFirstObjectByType<MomiaController>();
        }

        if (camFollow == null)
        {
            camFollow = FindFirstObjectByType<CameraFollow>();
        }

        if (deskObject == null) deskObject = GameObject.Find("Escritorio_Oficina");
        if (keyObject == null) keyObject = GameObject.Find("LlaveMuseo");
        if (securityDoorObject == null) securityDoorObject = GameObject.Find("PuertaSeguridad");

        // Buscar todos los spawners del mapa (incluyendo inactivos)
        MinorEnemySpawner[] spawners = FindObjectsByType<MinorEnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        minorEnemySpawners.Clear();
        foreach (var s in spawners)
        {
            if (s != null)
            {
                minorEnemySpawners.Add(s.gameObject);
                s.gameObject.SetActive(true);
                s.enabled = true;
            }
        }
    }

    public void RegistrarSpawner(GameObject spawnerObj)
    {
        if (!minorEnemySpawners.Contains(spawnerObj))
        {
            minorEnemySpawners.Add(spawnerObj);
            spawnerObj.SetActive(true);
        }
    }

    public void ConfigurarMomia(GameObject momia, Transform spawnPt = null)
    {
        mainMummyObject = momia;
        mummyAwakenSpawnPoint = spawnPt;
        if (currentState == LevelState.SafeExploration && mainMummyObject != null)
        {
            mainMummyObject.SetActive(false);
        }
    }

    public void SetState(LevelState newState)
    {
        currentState = newState;
        Debug.Log($"<color=orange>[Level1Manager] Transición de Estado a: {currentState}</color>");

        switch (currentState)
        {
            case LevelState.SafeExploration:
                HandleSafeExploration();
                break;

            case LevelState.TheAwakening:
                HandleTheAwakening();
                break;

            case LevelState.KeyAcquired:
                HandleKeyAcquired();
                break;

            case LevelState.Escape:
                HandleEscape();
                break;
        }

        OnStateChanged?.Invoke(currentState);
    }

    private void HandleSafeExploration()
    {
        // 1. Momia Principal activa y acechando inmediatamente al comenzar el nivel
        if (mainMummyObject != null)
        {
            mainMummyObject.SetActive(true);
            MomiaController mc = mainMummyObject.GetComponent<MomiaController>();
            if (mc != null) mc.enabled = true;
        }

        // 2. Spawners de escarabajos activos acechando desde el inicio
        SetSpawnersActive(true);

        // 3. Posicionar jugador en la entrada inferior central
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (playerSpawnPoint != null) player.transform.position = playerSpawnPoint.position;
            else player.transform.position = new Vector3(0f, -15.5f, 0f);
        }

        // 4. Llave oculta hasta que la guardia encuentre la pistola en la Oficina
        if (keyObject != null)
        {
            keyObject.SetActive(false);
        }

        MostrarBanner("HORA: 12:00 AM - TURNO DE VIGILANCIA\nObjetivo: Busca tu pistola en el escritorio de la Oficina Noroeste para defenderte.");
    }

    private void HandleTheAwakening()
    {
        // 1. Despertar la Momia Principal y activar su IA de acecho
        if (mainMummyObject != null)
        {
            mainMummyObject.SetActive(true);
            if (mummyAwakenSpawnPoint != null)
            {
                mainMummyObject.transform.position = mummyAwakenSpawnPoint.position;
            }
            else
            {
                mainMummyObject.transform.position = new Vector3(0f, 14f, 0f);
            }

            MomiaController mc = mainMummyObject.GetComponent<MomiaController>();
            if (mc != null) mc.enabled = true;
        }

        // 2. Activar generadores de criaturas secundarias y engendrar oleada de escarabajos por todo el museo
        SetSpawnersActive(true);
        EngendrarOleadaEscarabajosPorPistola();

        // 3. La Llave ahora es visible en la Galería Noreste una vez encontrada la pistola
        if (keyObject != null)
        {
            keyObject.SetActive(true);
        }

        // 4. Sonido ambiental de vidrios rompiéndose
        if (ambientAudioSource != null && glassBreakClip != null)
        {
            ambientAudioSource.PlayOneShot(glassBreakClip);
        }

        PistolController pistol = FindFirstObjectByType<PistolController>();
        int numBalas = pistol != null ? pistol.CurrentAmmo : 6;
        MostrarBanner($"¡PISTOLA ENCONTRADA CON {numBalas} BALAS!\n¡Escarabajos de tumba han emergido por todo el museo! Usa tu arma para eliminarlos.\nAhora la Llave del Museo es visible en la Galería Noreste.");
        Debug.Log($"<color=yellow>[Level1Manager] ¡Pistola encontrada con {numBalas} balas! Llave ahora visible en la Galería.</color>");
        Debug.Log("<color=red>[Level1Manager] ¡El Despertar ha comenzado! La Momia Principal y amenazas secundarias están activas.</color>");
    }

    private void EngendrarOleadaEscarabajosPorPistola()
    {
        // Al encontrar el arma reglamentaria, nidos de escarabajos emergen por todas las salas
        SpawnEscarabajo(new Vector3(-10f, 0f, 0f));    // Pasillo Central Oeste
        SpawnEscarabajo(new Vector3(10f, 0f, 0f));     // Pasillo Central Este
        SpawnEscarabajo(new Vector3(0f, -1.5f, 0f));   // Pasillo Central Cruce
        SpawnEscarabajo(new Vector3(16f, 14f, 0f));    // Galería Noreste Entrada
        SpawnEscarabajo(new Vector3(22f, 11f, 0f));    // Galería Noreste Pasillo
        SpawnEscarabajo(new Vector3(15f, -15f, 0f));   // Almacén Entrada
        SpawnEscarabajo(new Vector3(24f, -12f, 0f));   // Almacén Lateral
        SpawnEscarabajo(new Vector3(-14f, -12f, 0f));  // Cripta Entrada
        SpawnEscarabajo(new Vector3(-23f, -16f, 0f));  // Cripta Fondo
        SpawnEscarabajo(new Vector3(-4f, 16f, 0f));    // Gran Bóveda Oeste
        SpawnEscarabajo(new Vector3(4f, 16f, 0f));     // Gran Bóveda Este
        SpawnEscarabajo(new Vector3(-16f, 12f, 0f));   // Salida de Oficina Noroeste

        Debug.Log("<color=orange>[Level1Manager] ¡Oleada de escarabajos de tumba ha surgido por todo el museo al encontrar la pistola!</color>");
    }

    private void HandleKeyAcquired()
    {
        OnKeyCollected?.Invoke();
        MostrarBanner("¡LLAVE DEL MUSEO OBTENIDA!\nLa Puerta de Seguridad en la esquina inferior izquierda está desbloqueada.\n¡Escapa!");
        Debug.Log("<color=green>[Level1Manager] Llave obtenida. La puerta de seguridad está ahora desbloqueada.</color>");
    }

    private void HandleEscape()
    {
        Debug.Log($"<color=green>[Level1Manager] ¡Nivel 1 completado con éxito! Cargando {nextSceneName}...</color>");
        Time.timeScale = 1f;
        StartCoroutine(TransicionANivel2());
    }

    private System.Collections.IEnumerator TransicionANivel2()
    {
        MostrarBanner("¡HAS ESCAPADO DEL MUSEO CON ÉXITO!\nCargando Nivel 2...");
        yield return new WaitForSeconds(1.2f);

        try
        {
            SceneManager.LoadScene(nextSceneName);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Level1Manager] No se pudo cargar {nextSceneName}: {ex.Message}");
        }
    }

    // Método disparado por el Escritorio al recoger la pistola
    public void NotifyPistolPicked()
    {
        if (currentState == LevelState.SafeExploration)
        {
            OnPistolPicked?.Invoke();
            SetState(LevelState.TheAwakening);
        }
    }

    // Método disparado al tomar la llave
    public void NotifyKeyPicked()
    {
        SetState(LevelState.KeyAcquired);
    }

    // Método disparado al interactuar con la puerta de seguridad
    public void NotifyDoorInteraction()
    {
        bool tieneLlave = (currentState == LevelState.KeyAcquired) || 
                          (player != null && player.tieneLlave);

        if (tieneLlave)
        {
            SetState(LevelState.Escape);
            ActivarVictoria();
        }
        else
        {
            MostrarBanner("PUERTA DE SEGURIDAD BLOQUEADA\nNecesitas la Llave del Museo para abrir los cerrojos coloniales.");
            Debug.Log("[Level1Manager] La puerta de seguridad está fuertemente bloqueada. Necesitas la llave del museo.");
        }
    }

    public void ActivarVictoria()
    {
        if (juegoTerminado || nivelCompletado) return;
        nivelCompletado = true;
        Time.timeScale = 0f;

        // Detener música ambiental de fondo
        if (ambientAudioSource != null && ambientAudioSource.isPlaying)
        {
            ambientAudioSource.Stop();
        }

        // Reproducir audio de felicitaciones/victoria: dragon-studio-evil-laughter-494314
        AudioSource audioVic = GetComponent<AudioSource>();
        if (audioVic == null) audioVic = gameObject.AddComponent<AudioSource>();
        audioVic.ignoreListenerPause = true;
        AudioClip clipVic = Resources.Load<AudioClip>("Audio_Victoria");
        if (clipVic == null) clipVic = Resources.Load<AudioClip>("dragon-studio-evil-laughter-494314");
        if (clipVic != null)
        {
            audioVic.PlayOneShot(clipVic, 1.0f);
        }
    }

    public void ActivarGameOver(string motivo = null)
    {
        if (juegoTerminado || nivelCompletado) return;
        juegoTerminado = true;
        if (!string.IsNullOrEmpty(motivo))
        {
            motivoGameOver = motivo;
        }

        // Detener la música de fondo para que el alarido y el Game Over resalten
        if (ambientAudioSource != null && ambientAudioSource.isPlaying)
        {
            ambientAudioSource.Stop();
        }

        if (audioGameOver == null)
        {
            audioGameOver = gameObject.AddComponent<AudioSource>();
            audioGameOver.playOnAwake = false;
        }

        // Reproducir audio de Game Over: magiaz-scream-of-terror-325532
        AudioClip clipGameOver = Resources.Load<AudioClip>("Audio_GameOver");
        if (clipGameOver == null) clipGameOver = Resources.Load<AudioClip>("magiaz-scream-of-terror-325532");
        if (clipGameOver != null)
        {
            audioGameOver.PlayOneShot(clipGameOver, 1.0f);
        }
        else
        {
            if (clipGritoMomia == null) clipGritoMomia = GenerarAlaridoMomia();
            if (audioGameOver != null && clipGritoMomia != null)
            {
                audioGameOver.PlayOneShot(clipGritoMomia, 0.95f);
            }
        }
    }

    private void ConfigurarMusicaTenebrosaDeFondo()
    {
        if (ambientAudioSource == null)
        {
            ambientAudioSource = GetComponent<AudioSource>();
            if (ambientAudioSource == null) ambientAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ambientAudioSource.playOnAwake = false;
        ambientAudioSource.loop = true;
        ambientAudioSource.spatialBlend = 0f; // 2D estéreo ambiental envolvente

        // Cargar audio de fondo del Nivel 1: arctsound-horror-576085
        AudioClip clipMusica = Resources.Load<AudioClip>("Audio_Nivel1");
        if (clipMusica == null) clipMusica = Resources.Load<AudioClip>("GameplayMusic");
        if (clipMusica == null) clipMusica = Resources.Load<AudioClip>("arctsound-horror-576085");
        if (clipMusica == null) clipMusica = Resources.Load<AudioClip>("Audio/audio_videojuego");
        if (clipMusica == null) clipMusica = Resources.Load<AudioClip>("audio_videojuego");
        if (clipMusica == null) clipMusica = Resources.Load<AudioClip>("MenuMusic");

        // Si no se encuentra en disco, generar atmósfera tenebrosa procedural en loop continuo
        if (clipMusica == null)
        {
            clipMusica = GenerarMusicaTenebrosaProcedural();
        }

        if (clipMusica != null)
        {
            ambientAudioSource.clip = clipMusica;
            float volumenGuardado = PlayerPrefs.GetFloat("VolumenMusica", 0.8f);
            // Volumen sutil y equilibrado para que no opaque los pasos, disparos ni gruñidos
            ambientAudioSource.volume = Mathf.Clamp(volumenGuardado * 0.28f, 0.12f, 0.32f);
            ambientAudioSource.Play();
            Debug.Log("<color=green>[Level1Manager] Música tenebrosa de fondo iniciada en loop equilibrado.</color>");
        }
    }

    private AudioClip GenerarMusicaTenebrosaProcedural()
    {
        int sampleRate = 44100;
        float duration = 8.0f; // 8 segundos de bucle continuo tenebroso
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // 1. Drone sub-grave oscuro en bucle (45Hz y 68Hz con modulación lenta)
            float lfo = Mathf.Sin(2f * Mathf.PI * (1f / duration) * t);
            float drone1 = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.45f;
            float drone2 = Mathf.Sin(2f * Mathf.PI * 67.5f * t) * (0.35f + lfo * 0.12f);

            // 2. Armónico fantasmal disonante (108Hz)
            float eeriePad = Mathf.Sin(2f * Mathf.PI * 108f * t) * (0.20f + lfo * 0.08f);

            // 3. Textura de aire frío y polvo colonial
            float coldWind = ((float)new System.Random(i).NextDouble() * 2f - 1f) * 0.06f;

            samples[i] = Mathf.Clamp(drone1 + drone2 + eeriePad + coldWind, -1f, 1f) * 0.75f;
        }

        AudioClip clip = AudioClip.Create("Musica_Tenebrosa_Procedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarAlaridoMomia()
    {
        int sampleRate = 44100;
        float duration = 1.35f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freqBase = 115f + 75f * Mathf.Sin(t * 15f) + 35f * Mathf.Exp(-t * 2.2f);
            float carrier1 = Mathf.Sin(2f * Mathf.PI * freqBase * t);
            float carrier2 = Mathf.Sin(2f * Mathf.PI * (freqBase * 1.52f) * t);
            float rumble = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.45f;
            float screech = (UnityEngine.Random.value * 2f - 1f) * 0.35f;

            float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            samples[i] = Mathf.Clamp((carrier1 * 0.4f + carrier2 * 0.3f + rumble * 0.25f + screech * 0.35f) * env * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Momia_Alarido_Scream", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void ReintentarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Nivel1");
    }

    public void SiguienteNivel()
    {
        Time.timeScale = 1f;
        try
        {
            SceneManager.LoadScene(nextSceneName);
        }
        catch
        {
            SceneManager.LoadScene("Nivel1");
        }
    }

    private void SetSpawnersActive(bool active)
    {
        foreach (var spawner in minorEnemySpawners)
        {
            if (spawner != null)
            {
                spawner.SetActive(active);
            }
        }
    }

    public void MostrarBanner(string mensaje)
    {
        bannerMessage = mensaje;
        bannerTimer = 5.5f;
    }

    private void CrearTexturas()
    {
        if (texVidaVigilante == null) texVidaVigilante = MakeTex(2, 2, new Color(0.2f, 0.85f, 0.35f, 0.95f)); // Verde brillante
        if (texVidaMomia == null) texVidaMomia = MakeTex(2, 2, new Color(0.95f, 0.25f, 0.2f, 0.95f));         // Rojo intenso
        if (texFondoBarra == null) texFondoBarra = MakeTex(2, 2, new Color(0.12f, 0.12f, 0.15f, 0.9f));       // Gris oscuro
        if (texPanel == null) texPanel = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.1f, 0.92f));                 // Fondo oscuro elegante
        if (texPanelGameOver == null) texPanelGameOver = MakeTex(2, 2, new Color(0.06f, 0.02f, 0.02f, 0.96f)); // Fondo sangre oscura
        if (texAuraMomia == null) texAuraMomia = MakeTex(2, 2, new Color(0.85f, 0.08f, 0.08f, 0.45f));        // Aura roja maldita
        if (texBordeVictoria == null) texBordeVictoria = MakeTex(2, 2, new Color(0.95f, 0.75f, 0.15f, 0.95f)); // Borde dorado
        if (texPanelVictoria == null) texPanelVictoria = Resources.Load<Texture2D>("Momia/tex_panel_victoria");
        if (texBotonVictoria == null) texBotonVictoria = Resources.Load<Texture2D>("Momia/tex_boton_victoria");
        if (texBotonVictoriaHover == null) texBotonVictoriaHover = Resources.Load<Texture2D>("Momia/tex_boton_victoria_hover");
        if (texAuraRadialMomia == null) texAuraRadialMomia = Resources.Load<Texture2D>("Momia/tex_aura_radial");

        if (texMomiaGritando == null || texMomiaGritando.Length != 6)
        {
            texMomiaGritando = new Texture2D[6];
            for (int i = 0; i < 6; i++)
            {
                Texture2D t = Resources.Load<Texture2D>($"Momia/momia_scream_{i}");
                if (t == null)
                {
                    Sprite s = Resources.Load<Sprite>($"Momia/momia_scream_{i}");
                    if (s != null) t = s.texture;
                }
                texMomiaGritando[i] = t;
            }
        }

        if (texMomiaSonriendo == null || texMomiaSonriendo.Length != 6)
        {
            texMomiaSonriendo = new Texture2D[6];
            for (int i = 0; i < 6; i++)
            {
                Texture2D t = Resources.Load<Texture2D>($"Momia/momia_smile_{i}");
                if (t == null)
                {
                    Sprite s = Resources.Load<Sprite>($"Momia/momia_smile_{i}");
                    if (s != null) t = s.texture;
                }
                texMomiaSonriendo[i] = t;
            }
        }

        if (clipGritoMomia == null)
        {
            clipGritoMomia = GenerarAlaridoMomia();
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void Update()
    {
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
        }

        if (nivelCompletado || juegoTerminado) return;

        // El tiempo corre en cuanto comienza el nivel (de 12:00 AM a 1:00 AM)
        tiempoTranscurrido += Time.deltaTime;
        float pct = Mathf.Clamp01(tiempoTranscurrido / duracionNivelSegundos);
        float minFicticios = pct * 60f;
        int m = (int)minFicticios;

        if (pct >= 1f)
        {
            textoReloj = "01:00 AM";
            tiempoRestanteStr = "00:00";
            ActivarGameOver("¡HA SONADO LA 1:00 AM!\nSe acabó tu tiempo de guardia y las momias se apoderaron del museo.");
        }
        else
        {
            textoReloj = $"12:{m:D2} AM";
            float segsRestantes = Mathf.Max(0f, duracionNivelSegundos - tiempoTranscurrido);
            int minR = (int)(segsRestantes / 60f);
            int segR = (int)(segsRestantes % 60f);
            tiempoRestanteStr = $"{minR:D2}:{segR:D2}";
        }
    }

    private float lastScale = -1f;

    private void OnGUI()
    {
        ResolverReferencias();

        // Factor de escala responsivo basado en altura de pantalla (1.0 en 540p, 1.33 en 720p, 2.0 en 1080p Web)
        float s = Mathf.Clamp(Screen.height / 540f, 1.0f, 2.2f);
        InitStyles(s);

        // Si el panel de lectura histórica está abierto, pausamos el dibujo del HUD para no tapar la nota
        LoreUIController loreUI = LoreUIController.Instance != null ? LoreUIController.Instance : UnityEngine.Object.FindFirstObjectByType<LoreUIController>();
        if (loreUI != null && loreUI.IsPanelOpen)
        {
            return;
        }

        // =========================================================================
        // PANTALLAS DE FIN DE NIVEL: OCULTAR TODO EL HUD PARA QUE NADA LAS OBSTRUYA
        // =========================================================================
        if (nivelCompletado)
        {
            RenderModalVictoria(s);
            return;
        }

        if (juegoTerminado)
        {
            RenderModalGameOver(s);
            return;
        }

        // 1. HUD SUPERIOR: VIGILANTE (IZQUIERDA)
        float boxW = 210f * s;
        float boxH = 64f * s;
        float barraW = 120f * s;
        float barraH = 11f * s;
        float pad = 10f * s;

        GUI.Box(new Rect(pad, pad, boxW, boxH), "", boxStyle);
        GUI.Label(new Rect(pad + 8 * s, pad + 2 * s, boxW - 16 * s, 18 * s), "VIGILANTE", headerStyle);
        float pctVigilante = player != null ? Mathf.Clamp01(player.vidaActual / player.vidaMaxima) : 0f;
        GUI.DrawTexture(new Rect(pad + 8 * s, pad + 20 * s, barraW, barraH), texFondoBarra);
        GUI.DrawTexture(new Rect(pad + 8 * s, pad + 20 * s, barraW * pctVigilante, barraH), texVidaVigilante);
        GUI.Label(new Rect(pad + 8 * s + barraW + 6 * s, pad + 17 * s, 50 * s, 18 * s), $"{(int)(pctVigilante * 100)}%", textStyle);

        bool tieneLlaveActual = (player != null && player.tieneLlave) || (currentState == LevelState.KeyAcquired);
        PistolController pistolRef = player != null ? player.GetComponent<PistolController>() : null;
        bool tienePistolaActual = (pistolRef != null && pistolRef.HasPistol);

        string objText;
        if (tieneLlaveActual)
            objText = "<color=#FFD700>✓ Llave lista (Ve a Cripta)</color>";
        else if (tienePistolaActual)
            objText = "<color=#66FF66>✓ Pistola lista. Busca la llave</color>";
        else
            objText = "Busca tu pistola en la Oficina Noroeste";

        GUI.Label(new Rect(pad + 8 * s, pad + 35 * s, boxW - 16 * s, 24 * s), "Obj: " + objText, textStyle);

        // 2. RELOJ DEL MUSEO (CENTRO SUPERIOR)
        float relojW = 270f * s;
        float relojH = 54f * s;
        Rect relojRect = new Rect((Screen.width - relojW) / 2f, pad, relojW, relojH);
        GUI.Box(relojRect, "", boxStyle);

        bool tiempoPoco = (duracionNivelSegundos - tiempoTranscurrido) < 45f;
        string colorHora = tiempoPoco ? "#FF4444" : "#FFD700";

        GUI.Label(new Rect(relojRect.x, relojRect.y + 4 * s, relojW, 26 * s), $"<color={colorHora}>⏰ {textoReloj}</color>", clockTitleStyle);
        GUI.Label(new Rect(relojRect.x, relojRect.y + 30 * s, relojW, 20 * s), $"<color=#CCCCCC>Escapa antes de la 1:00 AM ({tiempoRestanteStr})</color>", clockSubStyle);

        // Botón de mapa
        string mapBtnText = (camFollow != null && camFollow.EstaViendoMapaCompleto()) ? "[M] Zoom Jugador" : "[M] Ver Museo Completo";
        float mapBtnW = 170f * s;
        float mapBtnH = 24f * s;
        if (GUI.Button(new Rect((Screen.width - mapBtnW) / 2f, relojRect.yMax + 4 * s, mapBtnW, mapBtnH), mapBtnText, buttonStyle))
        {
            if (camFollow != null) camFollow.AlternarMapaCompleto();
        }

        // 3. HUD SUPERIOR: MOMIA 1 (DERECHA)
        float momiaX = Screen.width - boxW - pad;
        GUI.Box(new Rect(momiaX, pad, boxW, boxH), "", boxStyle);
        GUI.Label(new Rect(momiaX + 8 * s, pad + 2 * s, boxW - 16 * s, 18 * s), "MOMIA 1", headerStyle);
        float pctMomia = momia != null ? Mathf.Clamp01(momia.vidaActual / momia.vidaMaxima) : 1f;
        GUI.DrawTexture(new Rect(momiaX + 8 * s, pad + 20 * s, barraW, barraH), texFondoBarra);
        GUI.DrawTexture(new Rect(momiaX + 8 * s, pad + 20 * s, barraW * pctMomia, barraH), texVidaMomia);
        GUI.Label(new Rect(momiaX + 8 * s + barraW + 6 * s, pad + 17 * s, 50 * s, 18 * s), $"{(int)(pctMomia * 100)}%", textStyle);

        string alerta = (momia != null && momia.estaSiendoObservada) ? "<color=#66FF66>● Quieta (La miras)</color>" : "<color=#FF4444>▲ Acechando (No la miras)</color>";
        GUI.Label(new Rect(momiaX + 8 * s, pad + 35 * s, boxW - 16 * s, 24 * s), alerta, textStyle);

        // 4. BANNER TEMPORAL
        if (bannerTimer > 0f && !string.IsNullOrEmpty(bannerMessage))
        {
            float bw = Mathf.Min(560f * s, Screen.width - 40f);
            float bh = 60f * s;
            Rect bRect = new Rect((Screen.width - bw) / 2f, relojRect.yMax + 32 * s, bw, bh);

            GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
            GUI.Box(bRect, "");
            GUI.color = Color.white;

            GUIStyle bStyle = new GUIStyle(GUI.skin.label);
            bStyle.fontSize = Mathf.RoundToInt(12 * s);
            bStyle.fontStyle = FontStyle.Bold;
            bStyle.alignment = TextAnchor.MiddleCenter;
            bStyle.richText = true;
            bStyle.normal.textColor = new Color(1f, 0.9f, 0.4f);

            GUI.Label(bRect, bannerMessage, bStyle);
        }

        // 5. GUÍA DE CONTROLES INFERIOR
        float ctrlW = 560f * s;
        float ctrlH = 24 * s;
        GUI.Label(new Rect(10 * s, Screen.height - ctrlH - 4 * s, ctrlW, ctrlH), "<color=#AAAAAA>Flechas: Mover | Espacio: Saltar | Z: Tolete | X: Pistola | L: Luces | M: Ver Mapa</color>", textStyle);
    }

    private void RenderModalVictoria(float s)
    {
        float w = Mathf.Min(520f * s, Screen.width - 30f);
        float h = Mathf.Min(440f * s, Screen.height - 30f);
        Rect center = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

        // Sombra de fondo oscura en toda la pantalla
        GUI.color = new Color(0f, 0f, 0f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texPanel);
        GUI.color = Color.white;

        // Marco del modal
        GUI.Box(center, "", victoryBoxStyle ?? boxStyle);

        GUI.Label(new Rect(center.x + 16 * s, center.y + 13 * s, w - 30 * s, 34 * s),
            "<color=#200505>★  ¡ F E L I C I D A D E S !  ★</color>", victoryTitleStyle ?? titleStyle);
        GUI.Label(new Rect(center.x + 15 * s, center.y + 12 * s, w - 30 * s, 34 * s),
            "<color=#FFDF66>★  ¡ F E L I C I D A D E S !  ★</color>", victoryTitleStyle ?? titleStyle);

        GUI.Label(new Rect(center.x + 15 * s, center.y + 46 * s, w - 30 * s, 22 * s),
            "<color=#E2B030>◈  N I V E L   1   C O M P L E T A D O  ◈</color>", victoryHeaderStyle ?? headerStyle);

        GUI.Label(new Rect(center.x + 20 * s, center.y + 68 * s, w - 40 * s, 24 * s),
            $"<color=#CCCCCC>Has escapado del museo a tiempo (</color><color=#FFD700><b>{textoReloj}</b></color><color=#CCCCCC>) antes de la 1:00 AM.</color>", victoryDescStyle ?? textStyle);

        if (texBordeVictoria != null)
        {
            GUI.color = new Color(0.9f, 0.72f, 0.22f, 0.75f);
            GUI.DrawTexture(new Rect(center.x + 35 * s, center.y + 92 * s, w - 70 * s, 2), texBordeVictoria);
            GUI.color = Color.white;
        }

        if (texMomiaSonriendo != null && texMomiaSonriendo.Length > 0)
        {
            int[] smileSeq = new int[] { 0, 1, 2, 1, 0, 4, 0, 3, 0, 1 };
            int frameIdx = smileSeq[(int)(Time.unscaledTime * 5f) % smileSeq.Length];
            Texture2D currentFrameTex = texMomiaSonriendo[Mathf.Clamp(frameIdx, 0, texMomiaSonriendo.Length - 1)];

            if (currentFrameTex != null)
            {
                float swayX = Mathf.Sin(Time.unscaledTime * 3.5f) * 3f;
                float swayY = Mathf.Cos(Time.unscaledTime * 2.8f) * 2f;
                float pulse = 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 5f);

                float mw = 180f * s * pulse;
                float mh = 210f * s * pulse;
                Rect mummyRect = new Rect(center.x + (w - mw) / 2f + swayX, center.y + 100f * s + swayY, mw, mh);

                Texture2D auraTex = texAuraRadialMomia ?? texAuraMomia;
                if (auraTex != null)
                {
                    float auraAlpha = 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 4.5f);
                    GUI.color = new Color(1f, 0.8f, 0.3f, auraAlpha);
                    float auraPad = 48f * s;
                    GUI.DrawTexture(new Rect(mummyRect.x - auraPad / 2f, mummyRect.y - auraPad / 2f, mw + auraPad, mh + auraPad), auraTex);
                    GUI.color = Color.white;
                }

                GUI.DrawTexture(mummyRect, currentFrameTex, ScaleMode.ScaleToFit);

                GUI.Label(new Rect(center.x + 15 * s, center.y + 316 * s, w - 30 * s, 22 * s),
                    "<color=#FFCC44><i>« ¿Creíste que escapar fue difícil? »</i></color>", victoryQuoteStyle ?? textStyleCenter ?? textStyle);
                GUI.Label(new Rect(center.x + 15 * s, center.y + 338 * s, w - 30 * s, 22 * s),
                    "<color=#FF5544><i>Las catacumbas del Nivel 2 te esperan...</i></color>", victoryQuoteStyle ?? textStyleCenter ?? textStyle);
            }
        }

        if (texBordeVictoria != null)
        {
            GUI.color = new Color(0.9f, 0.72f, 0.22f, 0.75f);
            GUI.DrawTexture(new Rect(center.x + 35 * s, center.y + 364 * s, w - 70 * s, 2), texBordeVictoria);
            GUI.color = Color.white;
        }

        float btnW = 250f * s;
        float btnH = 46f * s;
        if (GUI.Button(new Rect(center.x + (w - btnW) / 2f, center.y + 374 * s, btnW, btnH), "▶   INICIAR NIVEL 2   ◀", victoryButtonStyle ?? buttonStyle))
        {
            SiguienteNivel();
        }
    }

    private void RenderModalGameOver(float s)
    {
        float w = Mathf.Min(520f * s, Screen.width - 30f);
        float h = Mathf.Min(440f * s, Screen.height - 30f);
        Rect center = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

        GUI.color = new Color(0f, 0f, 0f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texPanel);
        GUI.color = Color.white;

        GUI.Box(center, "", gameOverPanelStyle ?? boxStyle);

        GUI.color = new Color(0.85f, 0.15f, 0.15f, 0.9f);
        GUI.DrawTexture(new Rect(center.x + 8 * s, center.y + 6 * s, w - 16 * s, 3 * s), texVidaMomia);
        GUI.color = Color.white;

        GUI.Label(new Rect(center.x + 15 * s, center.y + 12 * s, w - 30 * s, 34 * s), "☠ GAME OVER ☠", gameOverTitleStyle ?? titleStyle);
        GUI.Label(new Rect(center.x + 20 * s, center.y + 48 * s, w - 40 * s, 48 * s), motivoGameOver, gameOverReasonStyle ?? textStyle);

        if (texMomiaGritando != null && texMomiaGritando.Length > 0)
        {
            int[] screamSeq = new int[] { 0, 1, 4, 5, 4, 1, 0, 2, 3, 5, 1 };
            int frameIdx = screamSeq[(int)(Time.unscaledTime * 8f) % screamSeq.Length];
            Texture2D currentFrameTex = texMomiaGritando[Mathf.Clamp(frameIdx, 0, texMomiaGritando.Length - 1)];

            if (currentFrameTex != null)
            {
                float shakeX = Mathf.Sin(Time.unscaledTime * 38f) * 2.5f;
                float shakeY = Mathf.Cos(Time.unscaledTime * 30f) * 1.5f;
                float pulse = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 9f);

                float mw = 180f * s * pulse;
                float mh = 210f * s * pulse;
                Rect mummyRect = new Rect(center.x + (w - mw) / 2f + shakeX, center.y + 100f * s + shakeY, mw, mh);

                if (texAuraMomia != null)
                {
                    float auraAlpha = 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 6f);
                    GUI.color = new Color(0.85f, 0.1f, 0.1f, auraAlpha);
                    GUI.DrawTexture(new Rect(mummyRect.x - 18 * s, mummyRect.y - 12 * s, mw + 36 * s, mh + 24 * s), texAuraMomia);
                    GUI.color = Color.white;
                }

                GUI.DrawTexture(mummyRect, currentFrameTex, ScaleMode.ScaleToFit);

                GUI.Label(new Rect(center.x + 15 * s, center.y + 320 * s, w - 30 * s, 24 * s),
                    "<color=#FF5555><i>« ¡El alma en pena de Guanajuato clama por tu carne! »</i></color>", textStyleCenter ?? textStyle);
            }
        }

        GUI.color = new Color(0.85f, 0.15f, 0.15f, 0.9f);
        GUI.DrawTexture(new Rect(center.x + 8 * s, center.y + h - 68 * s, w - 16 * s, 2 * s), texVidaMomia);
        GUI.color = Color.white;

        float btnW = 250f * s;
        float btnH = 46f * s;
        if (GUI.Button(new Rect(center.x + (w - btnW) / 2f, center.y + h - 58 * s, btnW, btnH), "REINTENTAR NIVEL 1", gameOverButtonStyle ?? buttonStyle))
        {
            ReintentarNivel();
        }
    }

    private void InitStyles(float s)
    {
        if (boxStyle != null && Mathf.Approximately(s, lastScale)) return;
        lastScale = s;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = texPanel;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = Mathf.RoundToInt(13 * s);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.95f, 0.85f, 0.6f);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.RoundToInt(22 * s);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.95f, 0.75f, 0.2f);

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = Mathf.RoundToInt(12 * s);
        textStyle.richText = true;
        textStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = Mathf.RoundToInt(13 * s);
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.white;

        clockTitleStyle = new GUIStyle(GUI.skin.label);
        clockTitleStyle.fontSize = Mathf.RoundToInt(18 * s);
        clockTitleStyle.fontStyle = FontStyle.Bold;
        clockTitleStyle.alignment = TextAnchor.MiddleCenter;
        clockTitleStyle.richText = true;

        clockSubStyle = new GUIStyle(GUI.skin.label);
        clockSubStyle.fontSize = Mathf.RoundToInt(12 * s);
        clockSubStyle.fontStyle = FontStyle.Normal;
        clockSubStyle.alignment = TextAnchor.MiddleCenter;
        clockSubStyle.richText = true;

        gameOverPanelStyle = new GUIStyle(GUI.skin.box);
        gameOverPanelStyle.normal.background = texPanelGameOver ?? texPanel;

        gameOverTitleStyle = new GUIStyle(GUI.skin.label);
        gameOverTitleStyle.fontSize = Mathf.RoundToInt(26 * s);
        gameOverTitleStyle.fontStyle = FontStyle.Bold;
        gameOverTitleStyle.alignment = TextAnchor.MiddleCenter;
        gameOverTitleStyle.richText = true;
        gameOverTitleStyle.normal.textColor = new Color(0.95f, 0.2f, 0.2f);

        gameOverReasonStyle = new GUIStyle(GUI.skin.label);
        gameOverReasonStyle.fontSize = Mathf.RoundToInt(14 * s);
        gameOverReasonStyle.fontStyle = FontStyle.Bold;
        gameOverReasonStyle.alignment = TextAnchor.MiddleCenter;
        gameOverReasonStyle.richText = true;
        gameOverReasonStyle.normal.textColor = new Color(1f, 0.9f, 0.75f);

        textStyleCenter = new GUIStyle(GUI.skin.label);
        textStyleCenter.fontSize = Mathf.RoundToInt(13 * s);
        textStyleCenter.fontStyle = FontStyle.Italic;
        textStyleCenter.alignment = TextAnchor.MiddleCenter;
        textStyleCenter.richText = true;
        textStyleCenter.normal.textColor = new Color(1f, 0.45f, 0.45f);

        gameOverButtonStyle = new GUIStyle(GUI.skin.button);
        gameOverButtonStyle.fontSize = Mathf.RoundToInt(15 * s);
        gameOverButtonStyle.fontStyle = FontStyle.Bold;
        gameOverButtonStyle.alignment = TextAnchor.MiddleCenter;
        gameOverButtonStyle.normal.textColor = Color.white;

        victoryBoxStyle = new GUIStyle(GUI.skin.box);
        victoryBoxStyle.normal.background = texPanelVictoria ?? texPanel;

        victoryTitleStyle = new GUIStyle(GUI.skin.label);
        victoryTitleStyle.fontSize = Mathf.RoundToInt(28 * s);
        victoryTitleStyle.fontStyle = FontStyle.Bold;
        victoryTitleStyle.alignment = TextAnchor.MiddleCenter;
        victoryTitleStyle.richText = true;
        victoryTitleStyle.normal.textColor = new Color(1f, 0.88f, 0.4f);

        victoryHeaderStyle = new GUIStyle(GUI.skin.label);
        victoryHeaderStyle.fontSize = Mathf.RoundToInt(15 * s);
        victoryHeaderStyle.fontStyle = FontStyle.Bold;
        victoryHeaderStyle.alignment = TextAnchor.MiddleCenter;
        victoryHeaderStyle.richText = true;
        victoryHeaderStyle.normal.textColor = new Color(0.92f, 0.74f, 0.28f);

        victoryDescStyle = new GUIStyle(GUI.skin.label);
        victoryDescStyle.fontSize = Mathf.RoundToInt(14 * s);
        victoryDescStyle.fontStyle = FontStyle.Normal;
        victoryDescStyle.alignment = TextAnchor.MiddleCenter;
        victoryDescStyle.richText = true;
        victoryDescStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        victoryQuoteStyle = new GUIStyle(GUI.skin.label);
        victoryQuoteStyle.fontSize = Mathf.RoundToInt(14 * s);
        victoryQuoteStyle.fontStyle = FontStyle.Italic;
        victoryQuoteStyle.alignment = TextAnchor.MiddleCenter;
        victoryQuoteStyle.richText = true;
        victoryQuoteStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);

        victoryButtonStyle = new GUIStyle(GUI.skin.button);
        victoryButtonStyle.fontSize = Mathf.RoundToInt(16 * s);
        victoryButtonStyle.fontStyle = FontStyle.Bold;
        victoryButtonStyle.alignment = TextAnchor.MiddleCenter;
        victoryButtonStyle.richText = true;
        victoryButtonStyle.normal.background = texBotonVictoria ?? texPanel;
        victoryButtonStyle.hover.background = texBotonVictoriaHover ?? texPanel;
        victoryButtonStyle.active.background = texBotonVictoriaHover ?? texPanel;
        victoryButtonStyle.normal.textColor = new Color(1f, 0.88f, 0.45f);
        victoryButtonStyle.hover.textColor = new Color(1f, 1f, 0.85f);
        victoryButtonStyle.active.textColor = Color.white;
    }
}
