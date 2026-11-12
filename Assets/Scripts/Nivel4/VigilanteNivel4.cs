using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controlador de la Vigilante para el Nivel 4 (Panteón de Santa Paula).
/// Sistema de sigilo, linterna táctica y locomoción fiel al Nivel 2:
/// - Modo Sigilo auténtico con sprites dedicados de agachado y caminata en cuclillas (sin encogimiento artificial de escala).
/// - Linterna de mano visible con rotación direccional y cono de luz que nace exactamente del cabezal/lente.
/// - Gestión de pasos: silencio absoluto en sigilo, carrera veloz con Shift.
/// - Sistema de escondites en sarcófagos de cantera.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class VigilanteNivel4 : MonoBehaviour
{
    public static VigilanteNivel4 Instance { get; private set; }

    [Header("Velocidades y Movimiento")]
    public float velocidadCaminar = 3.6f;
    public float velocidadCorrer = 5.4f;
    public float velocidadSigilo = 1.9f;

    public static bool ModoSigiloActivo { get; private set; }
    public bool EstaCorriendo { get; private set; }
    public bool EstaOculta { get; set; }
    public EsconditeNivel4 EsconditeActual { get; set; }

    [Header("Salud / Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;
    public bool EstaMuerto => vidaActual <= 0f;

    [Header("Resistencia / Estamina")]
    public float estaminaMaxima = 100f;
    public float estaminaActual = 100f;

    public string UltimoAtacante { get; private set; } = "Carmen";

    public void RecibirDanio(float cantidad, string atacante = "Carmen")
    {
        if (EstaMuerto) return;
        UltimoAtacante = string.IsNullOrEmpty(atacante) ? "Carmen" : atacante;
        vidaActual = Mathf.Max(0f, vidaActual - cantidad);

        if (Time.time - tiempoUltimoSonidoHerida > 0.35f)
        {
            tiempoUltimoSonidoHerida = Time.time;
            if (audioSource != null && clipHeridaN2 != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.06f);
                audioSource.PlayOneShot(clipHeridaN2, 0.85f);
            }
        }

        if (Nivel4Manager.Instance != null)
        {
            Nivel4Manager.Instance.SacudirPantalla(0.45f, 18f);
        }
        if (vidaActual <= 0f)
        {
            DetenerSonidoPasos();
            if (Nivel4Manager.Instance != null)
            {
                Nivel4Manager.Instance.ActivarJumpscareGameOver(UltimoAtacante);
            }
        }
    }

    public void Curar(float cantidad)
    {
        vidaActual = Mathf.Min(vidaMaxima, vidaActual + cantidad);
    }

    [Header("Linterna de Mano Visible (Igual a Nivel 2)")]
    public bool linternaEncendida = true;
    public Light2D luzLinterna;
    private Transform pivotLinterna;
    public Transform PivotLinterna => pivotLinterna;
    private SpriteRenderer linternaSr;
    private float tiempoBamboleo = 0f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D boxCol;
    private AudioSource audioSource;
    private Vector2 inputMovimiento;
    private Vector2 direccionMirada = Vector2.down;
    public Vector2 DireccionMirada => direccionMirada;
    private Vector3 escalaOriginal;

    [Header("Sprites de Animación")]
    private Sprite[] walkSprites;
    private Sprite[] runSprites;
    private Sprite idleSprite;

    // Sprites dedicados de sigilo / agachado
    private Sprite crouchIdleSprite;
    private Sprite[] crouchWalkSprites;
    private Sprite[] crouchDownSprites;
    private Sprite[] crouchUpSprites;

    private float animTimer = 0f;
    private int animIndex = 0;
    private int animIndexCrouch = 0;

    private float timerSonidoPasos = 0f;
    private float tiempoUltimoSonidoHerida = -10f;
    private AudioSource pasosLoopAudioSource;
    private AudioClip clipCaminataN2;
    private AudioClip clipHeridaN2;
    private bool toggleSigiloC = false;
    private float timerAturdida = 0f;
    public bool EstaAturdida => timerAturdida > 0f;

    public void Aturdir(float duracion)
    {
        timerAturdida = duracion;
    }

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        boxCol = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        GameObject pasosObj = new GameObject("PasosLoop_Nivel4");
        pasosObj.transform.SetParent(transform, false);
        pasosLoopAudioSource = pasosObj.AddComponent<AudioSource>();
        pasosLoopAudioSource.playOnAwake = false;
        pasosLoopAudioSource.loop = true;
        pasosLoopAudioSource.spatialBlend = 0f;
        pasosLoopAudioSource.volume = 0f;

        clipCaminataN2 = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMINATA_N2);
        clipHeridaN2 = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_HERIDA_N2);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (boxCol != null)
        {
            boxCol.size = new Vector2(0.45f, 0.35f);
            boxCol.offset = new Vector2(0f, -0.35f);
        }

        escalaOriginal = transform.localScale;
        if (escalaOriginal == Vector3.zero) escalaOriginal = new Vector3(1.5f, 1.5f, 1f);

        CargarSprites();
        AsegurarLinternaTacticalMano();
    }

    private void CargarSprites()
    {
        // 1. Caminata y carrera normales
        walkSprites = new Sprite[12];
        for (int i = 0; i < 12; i++)
        {
            walkSprites[i] = Resources.Load<Sprite>("Vigilante/vigilante_walk_" + i);
        }

        runSprites = new Sprite[7];
        for (int i = 0; i < 7; i++)
        {
            runSprites[i] = Resources.Load<Sprite>("Vigilante/vigilante_run_" + i);
        }

        idleSprite = Resources.Load<Sprite>("Vigilante/vigilante_idle");
        if (sr != null && idleSprite != null) sr.sprite = idleSprite;

        // 2. Sprites dedicados de Modo Sigilo (Agachada) basados en la hoja de animación oficial
        crouchIdleSprite = Resources.Load<Sprite>("Vigilante/vigilante_crouch_idle");
        if (crouchIdleSprite == null) crouchIdleSprite = Resources.Load<Sprite>("Vigilante/vigilante_crouch_walk_0");

        crouchWalkSprites = new Sprite[7];
        for (int i = 0; i < 7; i++)
        {
            crouchWalkSprites[i] = Resources.Load<Sprite>("Vigilante/vigilante_crouch_walk_" + i);
        }

        crouchDownSprites = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            crouchDownSprites[i] = Resources.Load<Sprite>("Vigilante/vigilante_crouch_down_" + i);
        }

        crouchUpSprites = new Sprite[3];
        for (int i = 0; i < 3; i++)
        {
            crouchUpSprites[i] = Resources.Load<Sprite>("Vigilante/vigilante_crouch_up_" + i);
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            DetenerSonidoPasos();
            return;
        }

        if (timerAturdida > 0f)
        {
            timerAturdida -= Time.deltaTime;
            inputMovimiento = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            DetenerSonidoPasos();
            return;
        }

        if (EstaOculta)
        {
            rb.linearVelocity = Vector2.zero;
            DetenerSonidoPasos();
            return;
        }

        // 1. Captura de Input de Movimiento
        inputMovimiento = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (inputMovimiento.sqrMagnitude > 0.05f)
        {
            direccionMirada = inputMovimiento;
        }

        // La tecla [M] la controla CameraFollow según el desbloqueo de 2 reliquias en Nivel4Manager

        // 2. Control de Modo Sigilo (Ctrl mantenido o C para alternar)
        if (Input.GetKeyDown(KeyCode.C))
        {
            toggleSigiloC = !toggleSigiloC;
        }

        bool ctrlPresionado = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        ModoSigiloActivo = ctrlPresionado || toggleSigiloC;

        // 3. Control de Carrera (Shift mantenido)
        bool shiftPresionado = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        EstaCorriendo = shiftPresionado && !ModoSigiloActivo && inputMovimiento.sqrMagnitude > 0.05f;

        // 4. Control de Linterna (F)
        if (Input.GetKeyDown(KeyCode.F))
        {
            linternaEncendida = !linternaEncendida;
            if (luzLinterna != null) luzLinterna.enabled = linternaEncendida;
        }

        ActualizarRotacionLinternaMano();
        ActualizarPosturaYAnimacionVisual();
        ActualizarEmisionPasos();
    }

    private void FixedUpdate()
    {
        if (EstaOculta || Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float velActual = velocidadCaminar;
        if (ModoSigiloActivo) velActual = velocidadSigilo;
        else if (EstaCorriendo) velActual = velocidadCorrer;

        rb.linearVelocity = inputMovimiento * velActual;
    }

    private void ActualizarPosturaYAnimacionVisual()
    {
        // Se preserva la escala proporcional natural del personaje (CERO encogimiento deformado)
        transform.localScale = escalaOriginal;

        // Volteo horizontal según movimiento
        if (sr != null)
        {
            if (inputMovimiento.x < -0.05f)
            {
                sr.flipX = true;
            }
            else if (inputMovimiento.x > 0.05f)
            {
                sr.flipX = false;
            }
        }

        if (sr != null)
        {
            if (ModoSigiloActivo)
            {
                // MODO SIGILO: Animación real de cuclillas / agachada (Sneak Walk y Crouch Idle)
                if (inputMovimiento.sqrMagnitude > 0.05f)
                {
                    animTimer += Time.deltaTime;
                    float frameRate = 0.095f;
                    if (animTimer >= frameRate)
                    {
                        animTimer = 0f;
                        if (crouchWalkSprites != null && crouchWalkSprites.Length > 0)
                        {
                            animIndexCrouch = (animIndexCrouch + 1) % crouchWalkSprites.Length;
                            if (crouchWalkSprites[animIndexCrouch] != null)
                            {
                                sr.sprite = crouchWalkSprites[animIndexCrouch];
                            }
                        }
                    }
                }
                else
                {
                    if (crouchIdleSprite != null) sr.sprite = crouchIdleSprite;
                    animIndexCrouch = 0;
                }
            }
            else
            {
                // MODO NORMAL: Caminar erguida, correr o reposo
                if (inputMovimiento.sqrMagnitude > 0.05f)
                {
                    float frameRate = EstaCorriendo ? 0.065f : 0.09f;
                    animTimer += Time.deltaTime;
                    if (animTimer >= frameRate)
                    {
                        animTimer = 0f;
                        if (EstaCorriendo && runSprites != null && runSprites.Length > 0)
                        {
                            animIndex = (animIndex + 1) % runSprites.Length;
                            if (runSprites[animIndex] != null) sr.sprite = runSprites[animIndex];
                        }
                        else if (walkSprites != null && walkSprites.Length > 0)
                        {
                            animIndex = (animIndex + 1) % walkSprites.Length;
                            if (walkSprites[animIndex] != null) sr.sprite = walkSprites[animIndex];
                        }
                    }
                }
                else
                {
                    if (idleSprite != null) sr.sprite = idleSprite;
                    animIndex = 0;
                }
            }

            // Capa visual del vigilante siempre visible en primer plano
            sr.sortingOrder = 12;
        }
    }

    private void ActualizarRotacionLinternaMano()
    {
        if (pivotLinterna == null) return;

        // 1. Orientación horizontal hacia donde mira el vigilante (nunca vertical apuntando a la cara)
        if (inputMovimiento.sqrMagnitude > 0.05f)
        {
            direccionMirada = inputMovimiento.normalized;
        }

        bool mirandoIzquierda = sr != null ? sr.flipX : (direccionMirada.x < -0.05f);

        // Ángulo direccional:
        // En vista 2D lateral, la linterna SIEMPRE apunta al frente en la mano del vigilante.
        // - Derecha base: -90° (frente horizontal)
        // - Izquierda base: +90° (frente horizontal)
        // Si camina hacia arriba o abajo, se inclina suavemente +-20° como máximo para alumbrar el camino,
        // pero NUNCA rota 90° hacia la cara o el pecho.
        float anguloZ = mirandoIzquierda ? 90f : -90f;
        if (inputMovimiento.y > 0.1f)
        {
            anguloZ += (mirandoIzquierda ? -20f : 20f); // Inclinación suave hacia arriba
        }
        else if (inputMovimiento.y < -0.1f)
        {
            anguloZ += (mirandoIzquierda ? 20f : -20f); // Inclinación suave hacia abajo
        }

        Quaternion rotObjetivo = Quaternion.Euler(0, 0, anguloZ);
        pivotLinterna.rotation = Quaternion.Lerp(pivotLinterna.rotation, rotObjetivo, Time.deltaTime * 18f);

        // 2. Posicionamiento EXACTO en la mano del vigilante:
        // Medido pixel art: vigilante_idle (Y = -0.14f) y vigilante_crouch_idle (Y = -0.22f)
        Vector3 offsetMano;
        if (ModoSigiloActivo)
        {
            float xMano = mirandoIzquierda ? -0.09f : 0.09f;
            float yMano = -0.22f;
            offsetMano = new Vector3(xMano, yMano, 0f);
        }
        else
        {
            float xMano = mirandoIzquierda ? -0.09f : 0.09f;
            float yMano = -0.14f;
            offsetMano = new Vector3(xMano, yMano, 0f);
        }

        // Bamboleo natural suave al caminar
        if (inputMovimiento.sqrMagnitude > 0.05f)
        {
            tiempoBamboleo += Time.deltaTime * (EstaCorriendo ? 12f : (ModoSigiloActivo ? 5.5f : 8f));
            float offsetBob = Mathf.Sin(tiempoBamboleo) * 0.018f;
            offsetMano.y += offsetBob;
        }

        pivotLinterna.localPosition = Vector3.Lerp(pivotLinterna.localPosition, offsetMano, Time.deltaTime * 16f);

        // Inversión vertical para que el haz y la linterna no se inviertan al mirar a la izquierda
        if (linternaSr != null)
        {
            linternaSr.flipY = mirandoIzquierda;
            linternaSr.sortingOrder = (sr != null ? sr.sortingOrder : 12) + 2;
            linternaSr.color = linternaEncendida ? Color.white : new Color(0.40f, 0.40f, 0.40f, 1f);
        }

        if (luzLinterna != null) luzLinterna.enabled = linternaEncendida;
    }

    public void DetenerSonidoPasos()
    {
        if (pasosLoopAudioSource != null)
        {
            if (pasosLoopAudioSource.isPlaying) pasosLoopAudioSource.Stop();
            pasosLoopAudioSource.time = 0f;
            pasosLoopAudioSource.volume = 0f;
        }
    }

    private void ActualizarEmisionPasos()
    {
        bool juegoPausado = Time.timeScale == 0f || (Nivel4Manager.Instance != null && (Nivel4Manager.Instance.JuegoTerminado || Nivel4Manager.Instance.NivelCompletado));
        bool seMueveFisicamente = rb != null && rb.linearVelocity.sqrMagnitude > 0.08f;
        bool debeSonarCaminata = !juegoPausado && !EstaMuerto && !EstaOculta && !ModoSigiloActivo &&
                                 inputMovimiento.sqrMagnitude > 0.05f && seMueveFisicamente;

        if (!debeSonarCaminata)
        {
            DetenerSonidoPasos();
            return;
        }

        // Loop oficial de caminata/carrera del Nivel 2 (caminata.mp3, sin ruido metálico sintético)
        if (clipCaminataN2 == null)
        {
            clipCaminataN2 = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMINATA_N2);
        }
        if (pasosLoopAudioSource != null && clipCaminataN2 != null)
        {
            if (pasosLoopAudioSource.clip != clipCaminataN2)
            {
                pasosLoopAudioSource.clip = clipCaminataN2;
            }
            pasosLoopAudioSource.loop = true;
            pasosLoopAudioSource.pitch = EstaCorriendo ? 1.30f : 0.98f;
            pasosLoopAudioSource.volume = EstaCorriendo ? 0.50f : 0.32f;
            if (!pasosLoopAudioSource.isPlaying)
            {
                pasosLoopAudioSource.Play();
            }
        }

        // Alerta acústica para Carmen y La Que Escucha
        float cadencia = EstaCorriendo ? 0.32f : 0.52f;
        timerSonidoPasos += Time.deltaTime;
        if (timerSonidoPasos >= cadencia)
        {
            timerSonidoPasos = 0f;

            MomiaEncadenadaController momia = FindFirstObjectByType<MomiaEncadenadaController>();
            if (momia != null)
            {
                float radio = EstaCorriendo ? 14f : 5.8f;
                momia.NotificarRuido(transform.position, radio, EstaCorriendo ? 1.6f : 0.8f);
            }
        }
    }

    private void AsegurarLinternaTacticalMano()
    {
        // Limpiar linternas previas o duplicadas
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform ch = transform.GetChild(i);
            if (ch.name.Contains("Linterna") && ch.name != "PivotLinternaMano")
            {
                if (Application.isPlaying) Destroy(ch.gameObject);
                else DestroyImmediate(ch.gameObject);
            }
        }

        Transform tPivot = transform.Find("PivotLinternaMano");
        GameObject objPivot;
        if (tPivot == null)
        {
            objPivot = new GameObject("PivotLinternaMano");
            objPivot.transform.SetParent(transform, false);
            objPivot.transform.localPosition = new Vector3(0.09f, -0.15f, 0f);
            pivotLinterna = objPivot.transform;
        }
        else
        {
            objPivot = tPivot.gameObject;
            pivotLinterna = tPivot;
        }

        // Neutralizar la escala 1.5 del Player para que la linterna tenga tamaño proporcionado (exacto a Nivel 2)
        objPivot.transform.localScale = new Vector3(0.6666667f, 0.6666667f, 1f);

        // 1. Objeto visual de la linterna táctica
        Transform tVis = pivotLinterna.Find("VisualLinternaMano");
        GameObject objVis;
        if (tVis == null)
        {
            objVis = new GameObject("VisualLinternaMano");
            objVis.transform.SetParent(pivotLinterna, false);
            objVis.transform.localPosition = Vector3.zero;
            objVis.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        }
        else
        {
            objVis = tVis.gameObject;
        }
        objVis.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

        linternaSr = objVis.GetComponent<SpriteRenderer>();
        if (linternaSr == null) linternaSr = objVis.AddComponent<SpriteRenderer>();
        linternaSr.sprite = GenerarSpriteLinternaTactical();
        linternaSr.sortingOrder = 14;

        // 2. Cono de Luz 2D naciendo exactamente del cabezal/lente
        Transform tLuz = pivotLinterna.Find("HazLuzLinterna");
        GameObject objLuz;
        if (tLuz == null)
        {
            objLuz = new GameObject("HazLuzLinterna");
            objLuz.transform.SetParent(pivotLinterna, false);
        }
        else
        {
            objLuz = tLuz.gameObject;
        }
        objLuz.transform.localPosition = new Vector3(0f, 0.28f, 0f); // Posicionado exactamente en la lente frontal

        luzLinterna = objLuz.GetComponent<Light2D>();
        if (luzLinterna == null) luzLinterna = objLuz.AddComponent<Light2D>();

        luzLinterna.lightType = Light2D.LightType.Point;
        luzLinterna.color = new Color(1f, 0.96f, 0.84f, 1f); // Tono cálido incandescente
        luzLinterna.intensity = 1.6f;
        luzLinterna.pointLightInnerAngle = 35f;
        luzLinterna.pointLightOuterAngle = 72f;
        luzLinterna.pointLightInnerRadius = 0.4f;
        luzLinterna.pointLightOuterRadius = 9.2f;
    }

    private Sprite GenerarSpriteLinternaTactical()
    {
        // Linterna táctica metálica pura (sin alteraciones artificiales, idéntica a Nivel 2)
        int w = 14;
        int h = 6;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0, 0, 0, 0);

        Color cuerpoNegro = new Color(0.14f, 0.14f, 0.18f, 1f);
        Color metalGris = new Color(0.48f, 0.52f, 0.56f, 1f);
        Color anilloMetal = new Color(0.72f, 0.76f, 0.80f, 1f);
        Color cabezal = new Color(0.26f, 0.28f, 0.32f, 1f);
        Color lenteAmarilla = new Color(1.0f, 0.96f, 0.50f, 1f);

        // Tubo / Mango (X: 1 a 8)
        for (int x = 1; x <= 8; x++)
        {
            for (int y = 1; y <= 4; y++)
            {
                cols[y * w + x] = ((x == 3 || x == 6) ? anilloMetal : cuerpoNegro);
            }
        }
        // Base / Tapa trasera (X: 0)
        cols[2 * w + 0] = anilloMetal;
        cols[3 * w + 0] = anilloMetal;

        // Cabezal ampliado (X: 9 a 12)
        for (int x = 9; x <= 12; x++)
        {
            for (int y = 0; y <= 5; y++)
            {
                cols[y * w + x] = (x == 10) ? anilloMetal : cabezal;
            }
        }

        // Lente brillante frontal incandescente (X: 13)
        for (int y = 1; y <= 4; y++)
        {
            cols[y * w + 13] = lenteAmarilla;
        }

        tex.SetPixels(cols);
        tex.Apply();
        // Pivot en X=0.35 (en el mango donde se sujeta con la mano) y Y=0.5 (eje central)
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.35f, 0.5f), 16f);
    }
}
