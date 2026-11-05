using System.Collections;
using UnityEngine;

public enum MinorEnemyType
{
    TombBeetle,     // Tipo A: Escarabajos de Tumba (merodeo, detección local, mordida persistente)
    ShowcaseArms    // Tipo B: Brazos de Vitrina / Restos Osificados (trampa oculta)
}

public class MinorEnemyAI : MonoBehaviour
{
    [Header("Configuración del Enemigo")]
    [SerializeField] private MinorEnemyType enemyType = MinorEnemyType.TombBeetle;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int currentHealth = 1;

    [Header("Parámetros Escarabajo de Tumba")]
    [SerializeField] private float wanderSpeed = 1.1f;         // Velocidad de merodeo tranquilo por la sala
    [SerializeField] private float beetleSpeed = 2.4f;         // Velocidad de persecución activa
    [SerializeField] private float detectionRadius = 5.5f;     // Rango de detección natural (no saben dónde estás a lo lejos)
    [SerializeField] private float loseRadius = 8.5f;          // Rango para perder al jugador si este huye
    [SerializeField] private float danioMordida = 10f;         // Daño que inflige cada mordida
    [SerializeField] private float biteCooldown = 1.2f;        // Tiempo entre mordidas sucesivas
    [SerializeField] private float slowDuration = 2.0f;        // Duración de la ralentización
    [SerializeField] private float slowMultiplier = 0.75f;     // Reduce velocidad al morder

    [Header("Parámetros Tipo B - Brazos de Vitrina")]
    [SerializeField] private float rootDuration = 1.5f;        // Inmoviliza 1.5s
    [SerializeField] private Sprite hiddenSprite;
    [SerializeField] private Sprite attackSprite;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private CircleCollider2D col;
    private Transform playerTransform;
    private PlayerMovement playerMovement;
    private PlayerController playerController;

    private bool isPlayerDetected = false;
    private bool isTrapTriggered = false;
    private bool isDead = false;
    private float nextBiteTime = 0f;

    // Merodeo autónomo por el museo
    private Vector2 wanderDirection = Vector2.zero;
    private float wanderTimer = 0f;
    private float skitterAudioTimer = 0f;

    // Coordinador global para que ataquen estrictamente UNO POR UNO (no llegan dos de junto)
    private static MinorEnemyAI escarabajoAtacantePrincipal = null;
    private static float tiempoAsignacionAtacante = 0f;

    // Animación de escarabajo (4 frames de caminata + 1 ataque)
    private Sprite[] beetleWalkFrames;
    private Sprite beetleAttack;
    private float animTimer = 0f;
    private int currentFrame = 0;
    private bool isAttacking = false;

    // Audio procedimental para escarabajos
    private AudioSource audioSource;
    private AudioClip skitterSound;
    private AudioClip biteSound;

    public void ConfigurarTipo(MinorEnemyType type)
    {
        enemyType = type;
        CargarSprites();
        if (enemyType == MinorEnemyType.ShowcaseArms && hiddenSprite != null && sr != null)
        {
            sr.sprite = hiddenSprite;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();

        // Escala reducida: escarabajos pequeños
        transform.localScale = new Vector3(0.35f, 0.35f, 1f);

        if (sr != null)
        {
            sr.sortingOrder = 8; // En el suelo (por debajo del torso del vigilante que es 10)
        }

        if (col != null)
        {
            col.radius = 0.5f;
            col.isTrigger = true;
        }

        currentHealth = maxHealth;
        CargarSprites();
        IniciarAudio();

        if (enemyType == MinorEnemyType.ShowcaseArms && hiddenSprite != null && sr != null)
        {
            sr.sprite = hiddenSprite;
        }
    }

    private void IniciarAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.4f; // Atenuación acústica por distancia en el museo

        if (skitterSound == null) skitterSound = GenerarSonidoSkitter();
        if (biteSound == null) biteSound = GenerarSonidoMordida();
    }

    private AudioClip GenerarSonidoSkitter()
    {
        int sampleRate = 44100;
        float duration = 0.25f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float mod = Mathf.Sin(2f * Mathf.PI * 40f * t);
            float noise = (Random.value * 2f - 1f) * Mathf.Max(0f, mod);
            float click = Mathf.Sin(2f * Mathf.PI * 2800f * t) * (i % 550 < 40 ? 1f : 0f);
            samples[i] = Mathf.Clamp((noise * 0.45f + click * 0.35f) * (1f - (t / duration)), -1f, 1f) * 0.5f;
        }

        AudioClip clip = AudioClip.Create("Escarabajo_Skitter", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoMordida()
    {
        int sampleRate = 44100;
        float duration = 0.20f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float hiss = (Random.value * 2f - 1f) * Mathf.Exp(-t * 15f) * 0.6f;
            float snap = Mathf.Sin(2f * Mathf.PI * 900f * t) * Mathf.Exp(-t * 30f) * 0.5f;
            samples[i] = Mathf.Clamp(hiss + snap, -1f, 1f) * 0.7f;
        }

        AudioClip clip = AudioClip.Create("Escarabajo_Mordida", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlaySkitterSound()
    {
        if (audioSource != null && skitterSound != null)
        {
            audioSource.pitch = Random.Range(0.85f, 1.25f);
            audioSource.PlayOneShot(skitterSound, 0.45f);
        }
    }

    private void PlayBiteSound()
    {
        if (audioSource != null && biteSound != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.15f);
            audioSource.PlayOneShot(biteSound, 0.85f);
        }
    }

    private void CargarSprites()
    {
        beetleWalkFrames = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            beetleWalkFrames[i] = Resources.Load<Sprite>($"Enemies/beetle_walk_{i}");
        }
        beetleAttack = Resources.Load<Sprite>("Enemies/beetle_attack");
        if (hiddenSprite == null) hiddenSprite = Resources.Load<Sprite>("Enemies/showcase_arms_hidden");
        if (attackSprite == null) attackSprite = Resources.Load<Sprite>("Enemies/showcase_arms_attack");
    }

    private void Start()
    {
        FindPlayer();
        if (enemyType == MinorEnemyType.TombBeetle && sr != null)
        {
            if (sr.sprite == null && beetleWalkFrames != null && beetleWalkFrames[0] != null)
            {
                sr.sprite = beetleWalkFrames[0];
            }
        }

        // Semilla aleatoria inicial de merodeo
        wanderTimer = Random.Range(0.5f, 2.5f);
        wanderDirection = Random.insideUnitCircle.normalized;
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
            playerMovement = p.GetComponent<PlayerMovement>();
            playerController = p.GetComponent<PlayerController>();
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        if (enemyType == MinorEnemyType.TombBeetle)
        {
            ActualizarComportamientoEscarabajo();
        }
    }

    private void ActualizarComportamientoEscarabajo()
    {
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 1. Detección natural (no tienen GPS global; solo detectan si el jugador se acerca a su sala)
        if (!isPlayerDetected)
        {
            if (distToPlayer <= detectionRadius)
            {
                isPlayerDetected = true;
                PlaySkitterSound();
            }
        }
        else
        {
            if (distToPlayer > loseRadius)
            {
                // Si el jugador corre y se aleja bastante, el escarabajo pierde el rastro
                isPlayerDetected = false;
                wanderTimer = Random.Range(1.5f, 3f);
                wanderDirection = Random.insideUnitCircle.normalized;
            }
        }

        Vector2 moveDir = Vector2.zero;
        float currentSpeed = 0f;

        if (isPlayerDetected)
        {
            // Sistema de turnos: SOLO UN ESCARABAJO ATACA A LA VEZ (es uno por uno, no dos de junto)
            if (escarabajoAtacantePrincipal == null || 
                escarabajoAtacantePrincipal == this || 
                escarabajoAtacantePrincipal.isDead || 
                !escarabajoAtacantePrincipal.gameObject.activeInHierarchy || 
                Time.time > tiempoAsignacionAtacante + 8.0f)
            {
                escarabajoAtacantePrincipal = this;
                tiempoAsignacionAtacante = Time.time;
            }

            bool esMiTurnoDeAtacar = (escarabajoAtacantePrincipal == this);

            if (esMiTurnoDeAtacar)
            {
                // ATACAR DESDE UN LADO (NO MONTARSE ARRIBA DEL JUGADOR)
                float flancoX = transform.position.x < playerTransform.position.x ? -0.75f : 0.75f;
                Vector2 targetPos = new Vector2(playerTransform.position.x + flancoX, playerTransform.position.y - 0.25f);
                float distAlFlanco = Vector2.Distance(transform.position, targetPos);
                float distCentroJugador = Vector2.Distance(transform.position, playerTransform.position);

                if (distCentroJugador < 0.62f)
                {
                    // Si se acerca de más al torso, apartarse hacia su lado
                    moveDir = (transform.position.x < playerTransform.position.x ? Vector2.left : Vector2.right);
                    currentSpeed = beetleSpeed;
                }
                else if (distAlFlanco > 0.15f)
                {
                    // Avanzar hacia el flanco lateral del vigilante
                    moveDir = (targetPos - (Vector2)transform.position).normalized;
                    currentSpeed = beetleSpeed;
                }
                else
                {
                    // Ya está en el flanco lateral exacto: se frena al lado del vigilante
                    moveDir = Vector2.zero;
                    currentSpeed = 0f;
                }

                // Mirar al jugador desde su flanco
                if (sr != null)
                {
                    sr.flipX = (transform.position.x > playerTransform.position.x);
                }

                // Ruido de correteo frecuente al perseguir
                skitterAudioTimer += Time.fixedDeltaTime;
                if (skitterAudioTimer >= 1.4f)
                {
                    skitterAudioTimer = 0f;
                    PlaySkitterSound();
                }

                // Morder desde el lado si está en rango
                if (distCentroJugador <= 1.05f)
                {
                    IntentarMorderJugador(playerTransform.gameObject);
                }
            }
            else
            {
                // OTRO ESCARABAJO YA TIENE EL TURNO DE ATAQUE
                // Mantener perímetro de espera a distancia (~3.5m) sin invadir ni atacar juntos
                if (distToPlayer < 3.2f)
                {
                    // Retroceder ligeramente para darle espacio al combate individual
                    moveDir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                    currentSpeed = wanderSpeed * 0.8f;
                }
                else if (distToPlayer > 4.2f)
                {
                    // Mantenerse a distancia visible en la sala esperando su turno
                    moveDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                    currentSpeed = wanderSpeed * 0.9f;
                }
                else
                {
                    // Acecho lateral en círculos a la espera
                    Vector2 dirToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                    moveDir = new Vector2(-dirToPlayer.y, dirToPlayer.x);
                    currentSpeed = wanderSpeed * 0.5f;
                }
            }
        }
        else
        {
            // MODO MERODEO / PATRULLA LOCAL: Camina y pausa aleatoriamente en su habitación
            wanderTimer -= Time.fixedDeltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(2.0f, 4.5f);
                // 65% probabilidad de caminar, 35% de quedarse quieto olfateando
                if (Random.value < 0.65f)
                {
                    wanderDirection = Random.insideUnitCircle.normalized;
                }
                else
                {
                    wanderDirection = Vector2.zero;
                }
            }

            moveDir = wanderDirection;
            currentSpeed = wanderSpeed;

            // Ruido ocasional de patitas sobre el suelo si el jugador está a distancia audible
            skitterAudioTimer += Time.fixedDeltaTime;
            if (skitterAudioTimer >= 3.2f)
            {
                skitterAudioTimer = 0f;
                if (distToPlayer < 12f)
                {
                    PlaySkitterSound();
                }
            }
        }

        // Si está ejecutando la animación momentánea de mordida, frena unos instantes
        if (isAttacking)
        {
            currentSpeed = 0f;
        }

        if (rb != null)
        {
            rb.linearVelocity = moveDir * currentSpeed;
        }

        // 2. Animación de patas
        if (!isAttacking && moveDir.sqrMagnitude > 0.01f)
        {
            animTimer += Time.fixedDeltaTime;
            float frameInterval = isPlayerDetected ? 0.075f : 0.12f;
            if (animTimer >= frameInterval)
            {
                animTimer = 0f;
                currentFrame = (currentFrame + 1) % 4;
                if (sr != null && beetleWalkFrames != null && beetleWalkFrames[currentFrame] != null)
                {
                    sr.sprite = beetleWalkFrames[currentFrame];
                }
            }

            if (sr != null)
            {
                if (moveDir.x > 0.05f) sr.flipX = false;
                else if (moveDir.x < -0.05f) sr.flipX = true;
            }
        }
    }

    // Impacto y contacto persistente con el jugador
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Player"))
        {
            if (enemyType == MinorEnemyType.TombBeetle)
            {
                IntentarMorderJugador(other.gameObject);
            }
            else if (enemyType == MinorEnemyType.ShowcaseArms && !isTrapTriggered)
            {
                isTrapTriggered = true;
                if (sr != null && attackSprite != null)
                {
                    sr.sprite = attackSprite;
                }
                PlayerMovement pm = other.GetComponent<PlayerMovement>();
                PlayerController pc = other.GetComponent<PlayerController>();
                StartCoroutine(ApplyRootDebuff(pm, pc));
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;

        // Mientras el escarabajo siga en contacto con el vigilante, muerde repetidamente según el cooldown
        if (other.CompareTag("Player") && enemyType == MinorEnemyType.TombBeetle)
        {
            IntentarMorderJugador(other.gameObject);
        }
    }

    private void IntentarMorderJugador(GameObject playerObj)
    {
        if (Time.time < nextBiteTime) return;

        PlayerController pc = playerObj.GetComponent<PlayerController>();
        PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();

        // Si la guardia está saltando en el aire con la barra espaciadora, esquiva la mordida del escarabajo
        if (pc != null && pc.EstaSaltando)
        {
            return;
        }

        nextBiteTime = Time.time + biteCooldown;

        // 1. Infligir daño al vigilante
        if (pc != null)
        {
            pc.RecibirDanio(danioMordida, "escarabajo");
            Debug.Log($"<color=red>[MinorEnemyAI] ¡El escarabajo mordió al vigilante! -{danioMordida} de vida. (Sigue vivo y al acecho)</color>");
        }

        // 2. Ralentización breve
        if (pm != null || pc != null)
        {
            StartCoroutine(ApplySlowDebuff(pm, pc));
        }

        // 3. Sonido y animación de mordida (¡NO DESAPARECE, SIGUE VIVO!)
        PlayBiteSound();
        StartCoroutine(AnimarMordida());
    }

    private IEnumerator AnimarMordida()
    {
        isAttacking = true;
        if (sr != null && beetleAttack != null)
        {
            sr.sprite = beetleAttack;
        }

        yield return new WaitForSeconds(0.18f);

        isAttacking = false;
        if (sr != null && beetleWalkFrames != null && beetleWalkFrames[0] != null)
        {
            sr.sprite = beetleWalkFrames[currentFrame];
        }
    }

    private IEnumerator ApplySlowDebuff(PlayerMovement pm, PlayerController pc)
    {
        if (pm != null) pm.speed *= slowMultiplier;
        if (pc != null)
        {
            pc.velocidadCaminar *= slowMultiplier;
            pc.velocidadCorrer *= slowMultiplier;
        }

        yield return new WaitForSeconds(slowDuration);

        if (pm != null) pm.speed /= slowMultiplier;
        if (pc != null)
        {
            pc.velocidadCaminar /= slowMultiplier;
            pc.velocidadCorrer /= slowMultiplier;
        }
    }

    private IEnumerator ApplyRootDebuff(PlayerMovement pm, PlayerController pc)
    {
        if (pm != null) pm.canMove = false;
        float oldWalk = 0f, oldRun = 0f;
        if (pc != null)
        {
            oldWalk = pc.velocidadCaminar;
            oldRun = pc.velocidadCorrer;
            pc.velocidadCaminar = 0f;
            pc.velocidadCorrer = 0f;
        }

        Debug.Log("<color=red>[MinorEnemyAI] ¡Brazos momificados te han atrapado desde la vitrina! Inmovilizado 1.5s.</color>");

        float timer = 0f;
        while (timer < rootDuration && isTrapTriggered)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (pm != null) pm.canMove = true;
        if (pc != null)
        {
            pc.velocidadCaminar = oldWalk;
            pc.velocidadCorrer = oldRun;
        }

        // Regresar a sprite oculto tras soltar
        if (sr != null && hiddenSprite != null)
        {
            sr.sprite = hiddenSprite;
        }
        isTrapTriggered = false;
        Debug.Log("<color=green>[MinorEnemyAI] Liberado de las garras de la vitrina.</color>");
    }

    // ÚNICAMENTE se destruye al dispararle con la pistola
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            isDead = true;
            Die();
        }
    }

    private void OnDisable()
    {
        LiberarTurnoDeAtaque();
    }

    private void LiberarTurnoDeAtaque()
    {
        if (escarabajoAtacantePrincipal == this)
        {
            escarabajoAtacantePrincipal = null;
        }
    }

    private void LateUpdate()
    {
        if (sr != null)
        {
            // Escarabajo en el suelo (ligeramente por debajo del personaje pero siempre sobre el piso)
            float footY = transform.position.y - 0.1f;
            sr.sortingOrder = 500 + Mathf.RoundToInt(-footY * 10f) - 2;
        }
    }

    private void Die()
    {
        LiberarTurnoDeAtaque();

        // Efecto visual de aplastamiento / impacto con pistola
        GameObject impact = new GameObject("Polvo_Muerte");
        impact.transform.position = transform.position;
        impact.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        SpriteRenderer srImp = impact.AddComponent<SpriteRenderer>();
        srImp.sprite = Resources.Load<Sprite>("FX/bullet_impact");
        srImp.color = new Color(0.35f, 0.95f, 0.45f, 0.95f); // Tono verdoso insecto
        srImp.sortingOrder = 15;
        Destroy(impact, 0.35f);

        isTrapTriggered = false;
        Destroy(gameObject);
    }
}
