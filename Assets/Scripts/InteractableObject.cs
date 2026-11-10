using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum InteractableType
{
    OfficeDesk,         // 1. Escritorio de la oficina (Contenedor de la pistola)
    LoreTablet,         // 2. Fichas de bronce / Bitácoras (Pausan juego y muestran historia)
    LootedShowcase,     // 3. Vitrinas coloniales saqueadas (60% de munición +3 balas)
    CandleAltar         // 4. Altares con veladoras (Encienden Light2D permanente)
}

[RequireComponent(typeof(BoxCollider2D))]
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Tipo de Interactable")]
    [SerializeField] private InteractableType interactableType;
    [SerializeField] private bool canInteractMultipleTimes = false;
    [SerializeField] private bool hasInteracted = false;

    [Header("Indicador Visual Flotante ('E')")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 1.2f, 0);

    [Header("1. Datos: Escritorio de la Oficina")]
    [SerializeField] private Sprite deskOpenedSprite;

    [Header("2. Datos: Ficha Informativa / Lore")]
    [TextArea(3, 8)]
    [SerializeField] private string loreTitle = "Crónica de la Epidemia de Cólera (1833)";
    [TextArea(5, 12)]
    [SerializeField] private string loreDescription = "En el siglo XIX, los cuerpos enterrados en el Panteón de Santa Paula fueron momificados naturalmente debido a la sequedad y las propiedades minerales del suelo guanajuatense. Al despertar la medianoche, las ánimas no distinguen entre los vivos y las vitrinas de exhibición...";

    [Header("3. Datos: Vitrina Saqueada")]
    [Range(0, 100)]
    [SerializeField] private float ammoDropProbability = 60f;
    [SerializeField] private int ammoAmount = 3;
    [SerializeField] private Sprite emptyShowcaseSprite;

    [Header("4. Datos: Altar de Veladoras")]
    [SerializeField] private Light2D candleLight2D;
    [SerializeField] private Sprite litCandleSprite;
    [SerializeField] private Color litLightColor = new Color(1f, 0.65f, 0.25f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip interactSound;

    private SpriteRenderer spriteRenderer;
    private bool playerInRange = false;

    public void Configurar(InteractableType type, string titulo = null, string desc = null)
    {
        interactableType = type;
        if (!string.IsNullOrEmpty(titulo)) loreTitle = titulo;
        if (!string.IsNullOrEmpty(desc)) loreDescription = desc;
        if (type == InteractableType.LoreTablet)
        {
            canInteractMultipleTimes = true;
            hasInteracted = false;
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = false;
        }
        CargarSpritesPorDefecto();
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            if (interactableType == InteractableType.LoreTablet || interactableType == InteractableType.LootedShowcase || interactableType == InteractableType.OfficeDesk)
            {
                // Muebles completamente sólidos: el jugador no puede atravesarlos ni colocarse detrás
                col.isTrigger = false;
                if (interactableType == InteractableType.LoreTablet)
                {
                    canInteractMultipleTimes = true;
                    hasInteracted = false;
                }
            }
            else
            {
                col.isTrigger = true;
            }
        }

        if (interactableType == InteractableType.LoreTablet)
        {
            canInteractMultipleTimes = true;
            hasInteracted = false;
        }

        CargarSpritesPorDefecto();

        if (interactableType == InteractableType.CandleAltar && candleLight2D != null)
        {
            candleLight2D.enabled = false;
        }
    }

    private void CargarSpritesPorDefecto()
    {
        if (deskOpenedSprite == null) deskOpenedSprite = Resources.Load<Sprite>("Props/desk_opened");
        if (emptyShowcaseSprite == null) emptyShowcaseSprite = Resources.Load<Sprite>("Props/showcase_broken");
        if (litCandleSprite == null) litCandleSprite = Resources.Load<Sprite>("Props/altar_lit");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (other.collider.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void Update()
    {
        // Proximidad de respaldo para interacción generosa y fluida sin trabas de física
        Transform p = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        if (p == null)
        {
            GameObject pGo = GameObject.FindGameObjectWithTag("Player");
            if (pGo != null) p = pGo.transform;
        }

        if (p != null)
        {
            float dist = Vector2.Distance(transform.position, p.position);
            if (dist <= 3.2f)
            {
                playerInRange = true;
            }
            else if (dist > 3.8f)
            {
                playerInRange = false;
            }
        }

        bool tocadoEnPantalla = ComprobarToqueDirecto();

        if ((playerInRange || tocadoEnPantalla) && CanInteract() && (Input.GetKeyDown(KeyCode.E) || TouchControlsManager.IsInteractTriggered() || tocadoEnPantalla))
        {
            LoreUIController loreUI = LoreUIController.Instance != null ? LoreUIController.Instance : UnityEngine.Object.FindFirstObjectByType<LoreUIController>();
            // Si el panel de lore está abierto o se acaba de cerrar este frame, no reactivar inmediatamente
            if (loreUI != null && (loreUI.IsPanelOpen || loreUI.JustClosed))
            {
                return;
            }

            Interact();
        }
    }

    /// <summary>
    /// Permite tocar la ficha de historia, vitrina o altar directamente con el dedo en celular/tablet
    /// </summary>
    private bool ComprobarToqueDirecto()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Transform p = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        if (p == null)
        {
            GameObject pGo = GameObject.FindGameObjectWithTag("Player");
            if (pGo != null) p = pGo.transform;
        }

        // Solo permitir interactuar al tocar si el jugador está en un radio accesible (máximo 4.8 unidades)
        if (p != null && Vector2.Distance(transform.position, p.position) > 4.8f) return false;

        // 1. Detección táctil en celulares y tablets
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    // No confundir con la zona izquierda del joystick
                    if (t.position.x < Screen.width * 0.45f && t.position.y < Screen.height * 0.52f) continue;

                    Vector3 worldPos = cam.ScreenToWorldPoint(t.position);
                    Collider2D col = GetComponent<Collider2D>();
                    if (col != null && col.OverlapPoint(worldPos)) return true;
                    if (Vector2.Distance(worldPos, transform.position) <= 1.4f) return true;
                }
            }
        }
        // 2. Clic izquierdo con el ratón
        else if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.x < Screen.width * 0.45f && Input.mousePosition.y < Screen.height * 0.52f) return false;

            Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldPos)) return true;
            if (Vector2.Distance(worldPos, transform.position) <= 1.4f) return true;
        }

        return false;
    }

    public bool CanInteract()
    {
        if (interactableType == InteractableType.LoreTablet) return true;
        if (hasInteracted && !canInteractMultipleTimes) return false;
        return true;
    }

    public string GetPromptText()
    {
        switch (interactableType)
        {
            case InteractableType.LoreTablet: return $"[E] Leer Crónica: {loreTitle}";
            case InteractableType.CandleAltar: return "[E] Encender Veladora";
            case InteractableType.OfficeDesk: return "[E] Abrir Cajón";
            default: return "[E] Buscar";
        }
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        PlaySound();

        switch (interactableType)
        {
            case InteractableType.OfficeDesk:
                InteractOfficeDesk();
                hasInteracted = true;
                break;

            case InteractableType.LoreTablet:
                InteractLoreTablet();
                // Las fichas de historia se pueden leer y releer cuantas veces se desee
                hasInteracted = false;
                break;

            case InteractableType.LootedShowcase:
                InteractLootedShowcase();
                hasInteracted = true;
                break;

            case InteractableType.CandleAltar:
                InteractCandleAltar();
                hasInteracted = true;
                break;
        }
    }

    // 1. ESCRITORIO DE LA OFICINA
    private void InteractOfficeDesk()
    {
        if (spriteRenderer != null && deskOpenedSprite != null)
        {
            spriteRenderer.sprite = deskOpenedSprite;
        }

        if (Level1Manager.Instance != null)
        {
            Level1Manager.Instance.NotifyPistolPicked();
        }
        else
        {
            PistolController pc = Object.FindFirstObjectByType<PistolController>();
            if (pc != null) pc.EquipPistol();
        }

        Debug.Log("<color=yellow>[InteractableObject] ¡Has recogido la pistola de reglamento del escritorio! Vidrios crujen a lo lejos...</color>");
    }

    // 2. FICHAS INFORMATIVAS / LORE (PANTALLA EMERGENTE TIPO NOTA)
    private void InteractLoreTablet()
    {
        LoreUIController loreUI = LoreUIController.Instance != null ? LoreUIController.Instance : UnityEngine.Object.FindFirstObjectByType<LoreUIController>();
        if (loreUI == null)
        {
            GameObject uiGo = new GameObject("LoreUIController_Singleton");
            loreUI = uiGo.AddComponent<LoreUIController>();
        }
        loreUI.OpenLorePanel(loreTitle, loreDescription);
    }

    // 3. VITRINA COLONIAL SAQUEADA
    private void InteractLootedShowcase()
    {
        float roll = Random.Range(0f, 100f);
        if (roll <= ammoDropProbability)
        {
            PistolController pistol = Object.FindFirstObjectByType<PistolController>();
            if (pistol != null)
            {
                pistol.AddAmmo(ammoAmount);
            }

            string mensaje = $"¡ENCONTRASTE +{ammoAmount} BALAS!\nEncontraste munición en el fondo de la vitrina.";
            if (Level1Manager.Instance != null)
            {
                Level1Manager.Instance.MostrarBanner(mensaje);
            }
            Debug.Log($"<color=cyan>[InteractableObject] ¡Encontraste +{ammoAmount} balas en el fondo de la vitrina!</color>");
        }
        else
        {
            string mensaje = "VITRINA VACÍA\nSolo cenizas y polvo colonial.";
            if (Level1Manager.Instance != null)
            {
                Level1Manager.Instance.MostrarBanner(mensaje);
            }
            Debug.Log("<color=grey>[InteractableObject] Vitrina vacía. Solo cenizas y polvo.</color>");
        }

        if (spriteRenderer != null && emptyShowcaseSprite != null)
        {
            spriteRenderer.sprite = emptyShowcaseSprite;
        }
    }

    // 4. ALTAR CON VELADORAS (OFRENDA)
    private void InteractCandleAltar()
    {
        if (spriteRenderer != null && litCandleSprite != null)
        {
            spriteRenderer.sprite = litCandleSprite;
        }

        if (candleLight2D != null)
        {
            candleLight2D.enabled = true;
            candleLight2D.color = litLightColor;
        }
        else
        {
            // Crear o habilitar luz 2D
            Light2D l2d = gameObject.GetComponent<Light2D>();
            if (l2d == null) l2d = gameObject.AddComponent<Light2D>();
            l2d.lightType = Light2D.LightType.Point;
            l2d.color = litLightColor;
            l2d.pointLightOuterRadius = 4.5f;
            l2d.intensity = 1.2f;
            candleLight2D = l2d;
        }

        Debug.Log("<color=orange>[InteractableObject] Veladora encendida. La llama ilumina el sendero en la penumbra.</color>");
    }

    private void PlaySound()
    {
        if (audioSource != null && interactSound != null)
        {
            audioSource.PlayOneShot(interactSound);
        }
    }

    private void OnGUI()
    {
        if (Level1Manager.Instance != null && (Level1Manager.Instance.IsNivelCompletado || Level1Manager.Instance.IsJuegoTerminado)) return;

        LoreUIController loreUI = LoreUIController.Instance != null ? LoreUIController.Instance : UnityEngine.Object.FindFirstObjectByType<LoreUIController>();
        // Si el visor de historia está abierto, no dibujar texto flotante
        if (loreUI != null && loreUI.IsPanelOpen) return;

        if (playerInRange && CanInteract())
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + promptOffset);
            if (screenPos.z > 0)
            {
                string txt = GetPromptText();
                float w = Mathf.Clamp(txt.Length * 8.2f + 24f, 180f, 520f);
                float h = 28f;
                Rect rect = new Rect(screenPos.x - w / 2, Screen.height - screenPos.y - h / 2, w, h);

                GUI.color = new Color(1f, 0.92f, 0.45f, 0.98f);
                GUI.Box(rect, txt);
                GUI.color = Color.white;
            }
        }
    }
}
