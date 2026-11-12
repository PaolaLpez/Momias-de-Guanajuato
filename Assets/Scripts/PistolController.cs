using UnityEngine;

public class PistolController : MonoBehaviour
{
    [Header("Estado del Arma")]
    [SerializeField] private bool hasPistol = false;
    [SerializeField] private int maxAmmo = 6;
    [SerializeField] private int currentAmmo = 6;
    [SerializeField] private float fireRate = 0.32f;
    [SerializeField] private int damagePerShot = 1;

    [Header("Posición de la Mano")]
    [SerializeField] private Vector3 manoDerechaOffset = new Vector3(0.18f, 0.05f, 0f);
    [SerializeField] private Vector3 manoIzquierdaOffset = new Vector3(-0.18f, 0.05f, 0f);

    [Header("Ángulos de Apuntado (Subir y Bajar)")]
    [SerializeField] private float maxAnguloSubir = 80f;   // Puede apuntar casi verticalmente hacia arriba
    [SerializeField] private float maxAnguloBajar = -80f;  // Puede apuntar casi verticalmente hacia el suelo

    [Header("Referencias Visuales")]
    [SerializeField] private Transform gunPivot;
    [SerializeField] private SpriteRenderer gunRenderer;
    [SerializeField] private Transform firePoint;

    [Header("Efectos")]
    [SerializeField] private Sprite muzzleFlashSprite;
    [SerializeField] private Sprite impactSprite;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip dryFireSound;
    [SerializeField] private AudioClip equipPistolSound;

    private float nextFireTime = 0f;
    private Camera mainCamera;
    private float currentAimAngle = 0f;

    public bool HasPistol => hasPistol;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (shootSound == null)
        {
            shootSound = Resources.Load<AudioClip>("Audio/gunshot");
            if (shootSound == null) shootSound = GenerarSonidoDisparoProcedural();
        }
        if (dryFireSound == null)
        {
            dryFireSound = GenerarSonidoClicSecoProcedural();
        }
        if (equipPistolSound == null)
        {
            equipPistolSound = GenerarSonidoEquiparPistola();
        }

        muzzleFlashSprite = Resources.Load<Sprite>("FX/muzzle_flash");
        impactSprite = Resources.Load<Sprite>("FX/bullet_impact");

        ConstruirPistolaEnLaMano();
        if (hasPistol)
        {
            EquipPistol();
        }
        else
        {
            UpdateWeaponVisibility();
        }
    }

    private void ConstruirPistolaEnLaMano()
    {
        Transform existingPivot = transform.Find("GunPivot");
        if (existingPivot != null)
        {
            gunPivot = existingPivot;
            gunRenderer = gunPivot.GetComponentInChildren<SpriteRenderer>();
            firePoint = gunPivot.Find("FirePoint");
            if (firePoint == null && gunRenderer != null)
            {
                firePoint = gunRenderer.transform.Find("FirePoint");
            }
            return;
        }

        // 1. Pivote exactamente en la mano del Vigilante
        GameObject pivotObj = new GameObject("GunPivot");
        pivotObj.transform.SetParent(transform);
        pivotObj.transform.localPosition = manoDerechaOffset;
        gunPivot = pivotObj.transform;

        // 2. Sprite de la pistola sostenida por la empuñadura
        GameObject gunObj = new GameObject("GunSprite");
        gunObj.transform.SetParent(gunPivot);
        gunObj.transform.localPosition = new Vector3(0.12f, 0.02f, 0f);
        gunObj.transform.localScale = new Vector3(1.0f, 1.0f, 1f);

        gunRenderer = gunObj.AddComponent<SpriteRenderer>();
        gunRenderer.sprite = Resources.Load<Sprite>("Props/prop_pistol");
        gunRenderer.sortingOrder = 12; // En la mano frente al cuerpo

        // 3. Punta del cañón para el disparo y destello
        GameObject fp = new GameObject("FirePoint");
        fp.transform.SetParent(gunObj.transform);
        fp.transform.localPosition = new Vector3(0.28f, 0.04f, 0f);
        firePoint = fp.transform;
    }

    private void OnEnable()
    {
        Level1Manager.OnPistolPicked += EquipPistol;
    }

    private void OnDisable()
    {
        Level1Manager.OnPistolPicked -= EquipPistol;
    }

    public void EquipPistol()
    {
        hasPistol = true;
        currentAmmo = maxAmmo;
        UpdateWeaponVisibility();

        // Sonido de recámara metálica al encontrar y equipar la pistola
        if (audioSource != null)
        {
            if (equipPistolSound == null) equipPistolSound = GenerarSonidoEquiparPistola();
            audioSource.PlayOneShot(equipPistolSound, 1.0f);
        }

        Debug.Log("<color=yellow>[PistolController] ¡Pistola de reglamento equipada en la mano! Apunta arriba/abajo con el ratón.</color>");
    }

    public void AddAmmo(int amount)
    {
        currentAmmo += amount;
        Debug.Log($"<color=cyan>[PistolController] Munición recogida (+{amount}). Total: {currentAmmo} balas</color>");
    }

    private void UpdateWeaponVisibility()
    {
        if (gunRenderer != null)
        {
            gunRenderer.enabled = hasPistol;
        }
    }

    public void OcultarArma()
    {
        if (gunRenderer != null)
        {
            gunRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (!hasPistol) return;

        HandleAiming();
        HandleShooting();
    }

    private void HandleAiming()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || gunPivot == null) return;

        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        // La orientación de la pistola depende estrictamente de hacia dónde mira la guardia (de frente)
        // NUNCA se hace hacia atrás: siempre está de frente a ella y solo apunta enfrente, arriba y abajo
        PlayerController pc = GetComponent<PlayerController>();
        SpriteRenderer playerSr = GetComponent<SpriteRenderer>();
        bool guardiaMiraDerecha = (pc != null) ? pc.IsFacingRight : ((playerSr != null) ? !playerSr.flipX : true);

        // Ubicación de la mano siempre al frente de la guardia (acompañando el salto si está en el aire)
        float jumpVisualY = (pc != null) ? pc.AlturaSaltoActual : 0f;
        Vector3 baseHandPos = guardiaMiraDerecha ? manoDerechaOffset : manoIzquierdaOffset;
        gunPivot.localPosition = baseHandPos + new Vector3(0f, jumpVisualY, 0f);

        Vector2 aimDirection = (mouseWorldPos - gunPivot.position);
        float rawAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        if (guardiaMiraDerecha)
        {
            // Guardia de frente hacia la DERECHA:
            // Apunta hacia enfrente (0°), hacia arriba (hasta +75°) y hacia abajo (hasta -75°)
            // Si el cursor pasa detrás de la guardia (hacia la izquierda), no se da la vuelta hacia atrás;
            // se mantiene en el frente apuntando hacia el extremo vertical correspondiente
            if (aimDirection.x >= 0f)
            {
                currentAimAngle = Mathf.Clamp(rawAngle, -75f, 75f);
            }
            else
            {
                // El ratón está detrás de la guardia: mantener al frente limitando al ángulo más cercano arriba o abajo
                currentAimAngle = (aimDirection.y >= 0f) ? 75f : -75f;
            }

            gunPivot.rotation = Quaternion.Euler(0, 0, currentAimAngle);

            if (gunRenderer != null)
            {
                gunRenderer.flipY = false;
                gunRenderer.transform.localPosition = new Vector3(0.12f, 0.02f, 0f);
            }
        }
        else
        {
            // Guardia de frente hacia la IZQUIERDA:
            // Apunta hacia enfrente (180°), hacia arriba (hasta 105°) y hacia abajo (hasta 255°)
            // Si el cursor pasa detrás de la guardia (hacia la derecha), no se voltea hacia atrás
            float normAngle = rawAngle < 0 ? rawAngle + 360f : rawAngle;

            if (aimDirection.x <= 0f)
            {
                currentAimAngle = Mathf.Clamp(normAngle, 105f, 255f);
            }
            else
            {
                // El ratón está detrás de la guardia: mantener al frente limitando al ángulo más cercano arriba o abajo
                currentAimAngle = (aimDirection.y >= 0f) ? 105f : 255f;
            }

            gunPivot.rotation = Quaternion.Euler(0, 0, currentAimAngle);

            if (gunRenderer != null)
            {
                gunRenderer.flipY = true;
                gunRenderer.transform.localPosition = new Vector3(0.12f, -0.02f, 0f);
            }
        }
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                nextFireTime = Time.time + fireRate;
                Shoot();
            }
            else
            {
                // Clic seco sin balas
                if (audioSource != null && dryFireSound != null)
                {
                    audioSource.PlayOneShot(dryFireSound);
                }
                Debug.Log("<color=red>[PistolController] *Clic seco* Sin munición. Busca balas en las vitrinas.</color>");
            }
        }
    }

    private void Shoot()
    {
        currentAmmo--;

        // 1. Sonido de disparo
        if (audioSource != null && shootSound != null)
        {
            audioSource.pitch = Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(shootSound, 1.0f);
        }

        // 2. Destello en boca del cañón
        if (firePoint != null)
        {
            GameObject flash = new GameObject("MuzzleFlash_FX");
            flash.transform.position = firePoint.position;
            flash.transform.rotation = firePoint.rotation;
            flash.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
            sr.sprite = muzzleFlashSprite;
            sr.sortingOrder = 15;

            Destroy(flash, 0.1f);
        }

        // 3. Mecánica de Disparo Raycast2D
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)gunPivot.position;
        Vector2 direction = gunPivot != null ? (Vector2)gunPivot.right : (Vector2)transform.right;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, 32f);

        if (hit.collider != null)
        {
            // Ignorar al propio vigilante
            if (hit.collider.CompareTag("Player") || hit.collider.transform.IsChildOf(transform))
            {
                return;
            }

            // 4. Inmunidad de la Momia Principal
            if (hit.collider.GetComponent<MomiaController>() != null || hit.collider.name.Contains("Momia"))
            {
                Debug.Log("<color=yellow>[PistolController] Inmune: Las balas no dañan a la Momia Principal.</color>");
                SpawnImpactEffect(hit.point, hit.normal);
                return;
            }

            // 5. Destrucción de Criaturas y Trampas Menores
            MinorEnemyAI enemy = hit.collider.GetComponent<MinorEnemyAI>();
            if (enemy == null) enemy = hit.collider.GetComponentInParent<MinorEnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damagePerShot);
                SpawnImpactEffect(hit.point, hit.normal);
                return;
            }

            // 6. Impacto en paredes o muebles
            SpawnImpactEffect(hit.point, hit.normal);
        }
    }

    private void SpawnImpactEffect(Vector2 point, Vector2 normal)
    {
        GameObject impact = new GameObject("BulletImpact_FX");
        impact.transform.position = point;
        impact.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        SpriteRenderer sr = impact.AddComponent<SpriteRenderer>();
        sr.sprite = impactSprite;
        sr.sortingOrder = 15;

        Destroy(impact, 0.25f);
    }

    private void OnGUI()
    {
        if (!hasPistol) return;
        if (Level1Manager.Instance != null && (Level1Manager.Instance.IsNivelCompletado || Level1Manager.Instance.IsJuegoTerminado)) return;

        ResponsiveGUIHelper.BeginScaledGUI(out float vw, out float vh, out Matrix4x4 prevMatrix, 920f, 520f);
        try
        {
            // Indicador de Munición en la parte superior izquierda (bajo el HUD de la Vigilante)
            float panelW = 210f;
            float panelH = 32f;
            Rect rect = new Rect(10f, 78f, panelW, panelH);

            GUI.color = new Color(0.1f, 0.12f, 0.15f, 0.88f);
            GUI.Box(rect, "");
            GUI.color = Color.white;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 11;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            string ammoColor = currentAmmo > 2 ? "#FFD700" : (currentAmmo > 0 ? "#FFA500" : "#FF4444");
            GUI.Label(rect, $"<color={ammoColor}>PISTOLA: {currentAmmo} BALAS</color>\n<size=9><color=#CCCCCC>[Clic Izquierdo: Disparar | Apuntar con Ratón]</color></size>", style);
        }
        finally
        {
            ResponsiveGUIHelper.EndScaledGUI(prevMatrix);
        }
    }

    private AudioClip GenerarSonidoDisparoProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.30f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Explosión de pólvora
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 24f);
            // Golpe seco y grave (frecuencia decreciente de 160Hz a 40Hz)
            float freq = Mathf.Lerp(160f, 40f, t / duration);
            float punch = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 16f) * 0.9f;
            // Chasquido metálico
            float snap = Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 55f) * 0.5f;

            samples[i] = Mathf.Clamp(noise + punch + snap, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Disparo_Procedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoClicSecoProcedural()
    {
        int sampleRate = 44100;
        float duration = 0.08f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float click = Mathf.Sin(2f * Mathf.PI * 2200f * t) * Mathf.Exp(-t * 110f);
            samples[i] = click * 0.8f;
        }

        AudioClip clip = AudioClip.Create("ClicSeco_Procedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoEquiparPistola()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // 1. Corredera tirada hacia atrás (deslizamiento y clic mecánico)
            float slide = (Random.value * 2f - 1f) * Mathf.Exp(-t * 22f) * 0.35f;
            float click1 = Mathf.Sin(2f * Mathf.PI * 1900f * t) * Mathf.Exp(-t * 45f) * 0.5f;

            // 2. Resorte cerrando con fuerza y cerrojo encajando (t = 0.16s)
            float lockClack = 0f;
            if (t > 0.16f)
            {
                float t2 = t - 0.16f;
                float metalSnap = Mathf.Sin(2f * Mathf.PI * 1350f * t2) * Mathf.Exp(-t2 * 35f);
                float heavyThump = Mathf.Sin(2f * Mathf.PI * 280f * t2) * Mathf.Exp(-t2 * 25f);
                lockClack = (metalSnap + heavyThump) * 0.85f;
            }

            samples[i] = Mathf.Clamp(slide + click1 + lockClack, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Pistola_Equipar", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
