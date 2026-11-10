using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaminar = 6.2f;
    public float velocidadCorrer = 9.5f;

    [Header("Estadísticas de Salud")]
    public float vidaMaxima = 150f;
    public float vidaActual = 150f;
    public bool estaEscondido = false;

    [Header("Objetivos")]
    public bool tieneLlave = false;

    [Header("Sprites de Animación")]
    public Sprite[] walkSprites;
    public Sprite[] runSprites;
    public Sprite idleSprite;
    public Sprite jumpSprite;
    public Sprite deadSprite;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform visualChild;
    private SpriteRenderer visualSr;

    private float animTimer = 0f;
    private int currentFrame = 0;
    private bool estaSaltando = false;
    private float tiempoSalto = 0f;

    // Límites herméticos del Colosal Museo (cubren todas las 6 salas sin salirse de la escena)
    public float museoMinX = -29.2f;
    public float museoMaxX = 29.2f;
    public float museoMinY = -20.8f;
    public float museoMaxY = 20.2f;

    public Vector2 facingDirection { get; private set; } = Vector2.right;
    public bool IsFacingRight => (sr != null) ? !sr.flipX : true;
    public bool EstaMuerto { get; private set; } = false;
    public bool EstaSaltando => estaSaltando;
    public float AlturaSaltoActual => estaSaltando ? Mathf.Sin((tiempoSalto / 0.52f) * Mathf.PI) * 0.85f : 0f;
    public static PlayerController Instance { get; private set; }
    [HideInInspector] public bool bloquearResetVisualPivot = false;

    [Header("Audio de la Vigilante")]
    private AudioSource audioSource;
    private AudioSource footstepAudioSource;
    private AudioClip[] footstepSounds;
    private AudioClip jumpSound;
    private AudioClip jumpLandingSound;
    private AudioClip hurtSound;
    private AudioClip deathSound;

    private float stepTimer = 0f;
    private int footstepIndex = 0;
    private float tiempoUltimoDanioSonido = -10f;

    private void OnValidate()
    {
        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null)
        {
            rootSr.sprite = null;
            rootSr.enabled = false;
        }
        if (idleSprite == null) idleSprite = Resources.Load<Sprite>("Vigilante/vigilante_idle");
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol == null)
        {
            boxCol = gameObject.AddComponent<BoxCollider2D>();
        }
        // Colisionador en los pies de la Vigilante para permitir perspectiva 2D top-down y rodear obstáculos con holgura
        boxCol.size = new Vector2(0.45f, 0.35f);
        boxCol.offset = new Vector2(0f, -0.35f);
        EstaMuerto = false;
        vidaActual = vidaMaxima;
        // Velocidades calibradas: caminar fijo y correr más pausado y controlable
        if (velocidadCaminar > 8.0f || velocidadCaminar <= 0f) velocidadCaminar = 6.2f;
        if (velocidadCorrer > 11.5f || velocidadCorrer <= 0f) velocidadCorrer = 9.5f;
        museoMinY = -20.8f;
        museoMinX = -29.2f;
        museoMaxX = 29.2f;
        museoMaxY = 20.2f;

        // Configurar VisualPivot para permitir el arco de salto independiente del collider físico
        Transform vp = transform.Find("VisualPivot");
        if (vp == null)
        {
            GameObject vpObj = new GameObject("VisualPivot");
            vpObj.transform.SetParent(transform, false);
            vpObj.transform.localPosition = Vector3.zero;
            vpObj.transform.localRotation = Quaternion.identity;
            vpObj.transform.localScale = Vector3.one;
            vp = vpObj.transform;
        }
        visualChild = vp;

        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null)
        {
            rootSr.sprite = null;
            rootSr.enabled = false;
        }

        sr = visualChild.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = visualChild.gameObject.AddComponent<SpriteRenderer>();
        }
        sr.enabled = true;
        sr.sortingOrder = 10;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        Transform existingFs = transform.Find("Vigilante_FootstepSource");
        if (existingFs != null)
        {
            footstepAudioSource = existingFs.GetComponent<AudioSource>();
        }
        else
        {
            GameObject footstepObj = new GameObject("Vigilante_FootstepSource");
            footstepObj.transform.SetParent(transform, false);
            footstepAudioSource = footstepObj.AddComponent<AudioSource>();
        }
        if (footstepAudioSource != null)
        {
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.spatialBlend = 0f;
        }

        InicializarAudiosVigilante();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        CargarSprites();
    }

    void CargarSprites()
    {
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
        jumpSprite = Resources.Load<Sprite>("Vigilante/vigilante_jump");
        deadSprite = Resources.Load<Sprite>("Vigilante/vigilante_dead");

        VigilanteCrouchSprites.Inicializar(idleSprite != null ? idleSprite.pixelsPerUnit : 64f);

        if (sr != null && idleSprite != null) sr.sprite = idleSprite;
    }

    void Update()
    {
        // Si está escondida en un armario/taquilla, ocultar totalmente su sprite y pausar movimiento
        if (estaEscondido || EsconditeNivel3.JugadorEscondido)
        {
            if (visualChild != null && visualChild.gameObject.activeSelf) visualChild.gameObject.SetActive(false);
            if (sr != null && sr.enabled) sr.enabled = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }
        else
        {
            if (visualChild != null && !visualChild.gameObject.activeSelf) visualChild.gameObject.SetActive(true);
            if (sr != null && !sr.enabled) sr.enabled = true;
        }

        // Si está muerta por ataque de la momia, congelar y mantener animación de derrota
        if (EstaMuerto)
        {
            if (deadSprite != null && sr != null)
            {
                sr.sprite = deadSprite;
            }
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // Movimiento con flechas o WASD (y Joystick táctil en celular/tablet)
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) moveX += 1f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) moveX -= 1f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) moveY += 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) moveY -= 1f;

        // Soporte táctil de palanca/joystick virtual en celulares y tablets
        Vector2 touchDir = TouchControlsManager.Movement;
        if (touchDir.sqrMagnitude > 0.04f)
        {
            moveX += touchDir.x;
            moveY += touchDir.y;
        }

        Vector2 inputDir = new Vector2(moveX, moveY).normalized;

        if (moveX > 0.1f)
        {
            facingDirection = Vector2.right;
            if (sr != null) sr.flipX = false;
        }
        else if (moveX < -0.1f)
        {
            facingDirection = Vector2.left;
            if (sr != null) sr.flipX = true;
        }
        else if (moveY > 0.1f)
        {
            facingDirection = Vector2.up;
        }
        else if (moveY < -0.1f)
        {
            facingDirection = Vector2.down;
        }

        bool corriendo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || TouchControlsManager.IsRunning;
        float vel = corriendo ? velocidadCorrer : velocidadCaminar;

        if (rb != null)
        {
            rb.linearVelocity = inputDir * vel;
        }

        // Solo limitar posición si intenta salir del museo exterior
        if (transform.position.x < museoMinX || transform.position.x > museoMaxX ||
            transform.position.y < museoMinY || transform.position.y > museoMaxY)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, museoMinX, museoMaxX);
            pos.y = Mathf.Clamp(pos.y, museoMinY, museoMaxY);
            transform.position = pos;
        }

        // Salto con barra espaciadora o botón táctil
        if ((Input.GetKeyDown(KeyCode.Space) || TouchControlsManager.IsJumpTriggered()) && !estaSaltando)
        {
            estaSaltando = true;
            tiempoSalto = 0f;
            if (audioSource != null && jumpSound != null)
            {
                audioSource.pitch = Random.Range(0.96f, 1.04f);
                audioSource.PlayOneShot(jumpSound, 0.75f);
            }
        }

        if (estaSaltando)
        {
            tiempoSalto += Time.deltaTime;
            float duracionSalto = 0.52f;
            float altura = Mathf.Sin((tiempoSalto / duracionSalto) * Mathf.PI) * 0.85f;

            if (visualChild != null)
            {
                visualChild.localPosition = new Vector3(0f, altura, 0f);
            }
            if (jumpSprite != null && sr != null)
            {
                sr.sprite = jumpSprite;
                sr.flipX = (facingDirection == Vector2.left);
            }

            if (tiempoSalto >= duracionSalto)
            {
                estaSaltando = false;
                if (visualChild != null) visualChild.localPosition = Vector3.zero;

                // Sonido sólido de aterrizaje de botas en el suelo
                if (audioSource != null && jumpLandingSound != null)
                {
                    audioSource.pitch = Random.Range(0.95f, 1.05f);
                    audioSource.PlayOneShot(jumpLandingSound, 0.65f);
                }
            }
        }
        else
        {
            if (!bloquearResetVisualPivot && visualChild != null && visualChild.localPosition != Vector3.zero)
            {
                visualChild.localPosition = Vector3.zero;
            }
            Animar(inputDir.magnitude > 0.1f, corriendo);
        }
    }

    private void Animar(bool moviendose, bool corriendo)
    {
        if (sr == null) return;

        bool enSigilo = (TouchControlsManager.IsStealth || SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo) && !corriendo && !estaSaltando;
        if (enSigilo && VigilanteCrouchSprites.EstaListo)
        {
            if (!moviendose)
            {
                if (VigilanteCrouchSprites.CrouchIdleSprite != null) sr.sprite = VigilanteCrouchSprites.CrouchIdleSprite;
                currentFrame = 0;
                animTimer = 0f;
                return;
            }

            Sprite[] crouchSprites = VigilanteCrouchSprites.CrouchWalkFrames;
            float frameRateCrouch = 0.11f;
            animTimer += Time.deltaTime;
            if (animTimer >= frameRateCrouch)
            {
                animTimer = 0f;
                if (crouchSprites != null && crouchSprites.Length > 0)
                {
                    currentFrame = (currentFrame + 1) % crouchSprites.Length;
                    if (crouchSprites[currentFrame] != null)
                    {
                        sr.sprite = crouchSprites[currentFrame];
                    }
                }
            }
            return;
        }

        if (!moviendose)
        {
            if (idleSprite != null) sr.sprite = idleSprite;
            currentFrame = 0;
            animTimer = 0f;
            stepTimer = 0.12f; // Preparado para el primer paso al reanudar marcha
            return;
        }

        // Sonido rítmico de pasos de botas en baldosas de piedra del museo
        stepTimer += Time.deltaTime;
        float stepInterval = corriendo ? 0.13f : 0.17f;
        if (stepTimer >= stepInterval)
        {
            stepTimer = 0f;
            ReproducirSonidoPaso(corriendo);
        }

        // Si corre con Shift, usa los sprites de carrera; si no, el ciclo completo de caminata
        Sprite[] activeSprites = (corriendo && runSprites != null && runSprites.Length > 0) ? runSprites : walkSprites;
        float frameRate = corriendo ? 0.038f : 0.045f;

        animTimer += Time.deltaTime;

        if (animTimer >= frameRate)
        {
            animTimer = 0f;
            if (activeSprites != null && activeSprites.Length > 0)
            {
                currentFrame = (currentFrame + 1) % activeSprites.Length;
                if (activeSprites[currentFrame] != null)
                {
                    sr.sprite = activeSprites[currentFrame];
                }
            }
        }
    }

    private void ReproducirSonidoPaso(bool corriendo)
    {
        if (estaSaltando || EstaMuerto) return;
        // Silencio absoluto si está sobre un charco de agua o en modo sigilo
        if (TrampaAcusticaNivel2.JugadorEnCharco || SistemaMomiaYMicrofonoNivel2.ModoSigiloActivo) return;
        if (footstepAudioSource == null || footstepSounds == null || footstepSounds.Length == 0) return;

        AudioClip clip = footstepSounds[footstepIndex];
        footstepIndex = (footstepIndex + 1) % footstepSounds.Length;

        footstepAudioSource.pitch = Random.Range(corriendo ? 1.08f : 0.96f, corriendo ? 1.18f : 1.04f);
        float vol = corriendo ? 0.55f : 0.42f;
        footstepAudioSource.PlayOneShot(clip, vol);
    }

    public void RecibirDanio(float danio, string causante = "momia")
    {
        if (EstaMuerto) return;

        vidaActual -= danio;
        StartCoroutine(FlashHerida());

        // Sonido de quejido vocal de dolor de la vigilante
        if (Time.time - tiempoUltimoDanioSonido > 0.40f)
        {
            tiempoUltimoDanioSonido = Time.time;
            if (audioSource != null && hurtSound != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.06f);
                audioSource.PlayOneShot(hurtSound, 0.85f);
            }
        }

        if (vidaActual <= 0f)
        {
            vidaActual = 0f;
            Morir(causante);
        }
    }

    private IEnumerator FlashHerida()
    {
        if (sr != null)
        {
            sr.color = new Color(1f, 0.35f, 0.35f, 1f);
            yield return new WaitForSeconds(0.1f);
            if (!EstaMuerto && sr != null)
            {
                sr.color = Color.white;
            }
        }
    }

    // Animación de derrota y mensaje específico de Game Over
    private void Morir(string causante = "momia")
    {
        if (EstaMuerto) return;
        EstaMuerto = true;

        // Sonido dramático de lamento de derrota y caída
        if (audioSource != null && deathSound != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(deathSound, 1.0f);
        }

        if (sr != null && deadSprite != null)
        {
            sr.sprite = deadSprite;
            sr.color = Color.white;
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Ocultar pistola en la mano para no quedar flotando sobre el cuerpo
        PistolController pistol = GetComponent<PistolController>();
        if (pistol != null)
        {
            pistol.OcultarArma();
        }

        string motivo = "La momia te atrapó en la oscuridad.\nRecuerda: no debes quitarle los ojos de encima.";
        if (causante != null && causante.ToLower().Contains("escarabajo"))
        {
            motivo = "¡Los escarabajos de tumba te han devorado!\nUsa la pistola para eliminarlos antes de que te alcancen.";
        }

        Debug.Log($"<color=red>[PlayerController] ¡Fin de la partida! Causa: {causante}</color>");

        // Mostrar pantalla de Game Over con botón de Reintentar
        if (Nivel3Manager.Instance != null)
        {
            Nivel3Manager.Instance.ActivarGameOver("¡" + causante + " te dio el golpe mortal en el taller forense!");
        }
        else if (Level1Manager.Instance != null)
        {
            Level1Manager.Instance.ActivarGameOver(motivo);
        }
        else
        {
            Nivel1Manager manager = FindFirstObjectByType<Nivel1Manager>();
            if (manager != null)
            {
                manager.ActivarGameOver(motivo);
            }
        }
    }

    private void LateUpdate()
    {
        if (sr != null)
        {
            // Ordenamiento por profundidad 2.5D con baseline positivo para nunca quedar bajo el piso
            float footY = transform.position.y - 0.75f;
            sr.sortingOrder = 500 + Mathf.RoundToInt(-footY * 10f);
        }
    }

    private void InicializarAudiosVigilante()
    {
        jumpSound = Resources.Load<AudioClip>("Audio/jump");
        if (jumpSound == null) jumpSound = GenerarSonidoSaltoProcedural();

        jumpLandingSound = GenerarSonidoAterrizajeProcedural();
        hurtSound = GenerarSonidoDolorProcedural();
        deathSound = GenerarSonidoMuerteProcedural();

        footstepSounds = new AudioClip[3];
        footstepSounds[0] = GenerarSonidoPasoProcedural(1.0f);
        footstepSounds[1] = GenerarSonidoPasoProcedural(1.08f);
        footstepSounds[2] = GenerarSonidoPasoProcedural(0.93f);
    }

    private AudioClip GenerarSonidoPasoProcedural(float pitchMod)
    {
        int sampleRate = 44100;
        float duration = 0.13f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;

            // Golpe inicial de tacón en baldosa (grave resonante)
            float heel = Mathf.Sin(2f * Mathf.PI * (135f * pitchMod) * t) * Mathf.Exp(-t * 38f) * 0.7f;
            // Suela de cuero y fricción con el piso
            float sole = Mathf.Sin(2f * Mathf.PI * (920f * pitchMod) * t) * Mathf.Exp(-t * 52f) * 0.35f;
            // Ruido sutil de polvo / textura de piedra
            float noise = ((float)new System.Random(i + (int)(pitchMod * 1000)).NextDouble() * 2f - 1f) * Mathf.Exp(-t * 70f) * 0.25f;

            samples[i] = Mathf.Clamp((heel + sole + noise) * (1f - progress * 0.5f), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Paso_Bota_" + pitchMod, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoAterrizajeProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;

            // Doble impacto sólido de botas pesadas al caer
            float thump1 = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 26f);
            float thump2 = (t > 0.03f) ? Mathf.Sin(2f * Mathf.PI * 140f * (t - 0.03f)) * Mathf.Exp(-(t - 0.03f) * 32f) * 0.6f : 0f;
            float leather = Mathf.Sin(2f * Mathf.PI * 480f * t) * Mathf.Exp(-t * 40f) * 0.35f;

            samples[i] = Mathf.Clamp((thump1 + thump2 + leather) * (1f - progress), -1f, 1f) * 0.85f;
        }

        AudioClip clip = AudioClip.Create("Salto_Aterrizaje", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoDolorProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.26f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;

            // Formante vocal de dolor descendente (420Hz a 210Hz)
            float freq = Mathf.Lerp(420f, 210f, progress);
            float vocal = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.55f + Mathf.Sin(2f * Mathf.PI * freq * 2.1f * t) * 0.3f;
            // Ruido de jadeo/exhalación súbita
            float breath = ((float)new System.Random(i).NextDouble() * 2f - 1f) * Mathf.Exp(-t * 18f) * 0.35f;
            float env = Mathf.Sin(Mathf.Clamp01(progress * 1.4f) * Mathf.PI) * Mathf.Exp(-t * 8f);

            samples[i] = Mathf.Clamp((vocal + breath) * env, -1f, 1f) * 0.85f;
        }

        AudioClip clip = AudioClip.Create("Vigilante_Dolor", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoMuerteProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.75f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;

            // 1. Grito de angustia agudo decayendo (580Hz a 160Hz)
            float freq = Mathf.Lerp(580f, 160f, Mathf.Clamp01(t / 0.45f));
            float scream = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.25f;
            float screamEnv = (t < 0.45f) ? Mathf.Sin((t / 0.45f) * Mathf.PI) : 0f;

            // 2. Ruido áspero de choque / caída
            float fallThud = 0f;
            if (t > 0.35f)
            {
                float t2 = t - 0.35f;
                fallThud = Mathf.Sin(2f * Mathf.PI * 75f * t2) * Mathf.Exp(-t2 * 14f) * 0.7f;
            }

            samples[i] = Mathf.Clamp(scream * screamEnv * 0.85f + fallThud, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Vigilante_Muerte", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoSaltoProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.20f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;
            // Tono dinámico ascendente de 260Hz a 540Hz
            float freq = Mathf.Lerp(260f, 540f, progress);
            float envelope = Mathf.Sin(progress * Mathf.PI);
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = ((float)new System.Random(i).NextDouble() * 2f - 1f) * 0.12f;
            samples[i] = (wave * 0.45f + noise * envelope) * envelope * 0.65f;
        }

        AudioClip clip = AudioClip.Create("JumpSoundProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
