using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LoreEntry
{
    public string id;
    public string chapterBadge;
    public string title;
    public string location;
    public string description;

    public LoreEntry(string id, string chapterBadge, string title, string location, string description)
    {
        this.id = id;
        this.chapterBadge = chapterBadge;
        this.title = title;
        this.location = location;
        this.description = description;
    }
}

public class LoreUIController : MonoBehaviour
{
    public static LoreUIController Instance { get; private set; }

    private bool isPanelOpen = false;
    private bool isCatalogMode = false;
    private int currentNoteIndex = 0;
    private int openFrame = -1;
    private float openTime = -10f;
    private int closeFrame = -1;
    private float lastCloseTime = -10f;

    public bool IsPanelOpen => isPanelOpen;
    public bool JustClosed => (Time.unscaledTime - lastCloseTime) < 0.35f || Time.frameCount == closeFrame;
    public int CloseFrame => closeFrame;

    // Catálogo completo de crónicas del Museo de Guanajuato
    private List<LoreEntry> allNotes = new List<LoreEntry>();

    // Texturas de la nota
    private Texture2D texFondoSombra;
    private Texture2D texPapelNota;
    private Texture2D texBordeNota;
    private Texture2D texBoton;
    private Texture2D texBotonActivo;
    private Texture2D texFilaCatalog;

    // Estilos de texto
    private GUIStyle headerBadgeStyle;
    private GUIStyle locationStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle buttonActiveStyle;
    private GUIStyle instructionStyle;
    private GUIStyle catalogItemStyle;

    private Vector2 catalogScroll = Vector2.zero;
    private AudioSource audioSource;
    private AudioClip paperSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InicializarCatalogoNotas();
        CrearTexturas();
        IniciarAudio();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void InicializarCatalogoNotas()
    {
        allNotes.Clear();

        allNotes.Add(new LoreEntry(
            "Reglamento",
            "CRÓNICA I DE VI",
            "Reglamento del Museo y Toque de Queda (12:00 AM)",
            "Santuario Sur (Vestíbulo de Entrada)",
            "Aviso oficial a los custodios nocturnos:\n\n" +
            "En punto de la medianoche, las verjas exteriores se clausuran automáticamente por protocolo del cabildo. " +
            "Queda estrictamente prohibido circular por las galerías oscuras sin linterna reglamentaria. " +
            "Si se escuchan crujidos de vidrio en la Gran Bóveda, dirígete de inmediato a la Oficina Noroeste y desenfunda el arma de servicio."
        ));

        allNotes.Add(new LoreEntry(
            "Colera",
            "CRÓNICA II DE VI",
            "La Epidemia de Cólera Morbus (1833)",
            "Pasillo Central Norte",
            "Durante el implacable brote de cólera que asoló Guanajuato en 1833, cientos de difuntos fueron sepultados con premura " +
            "en las gavetas del Panteón de Santa Paula. La extrema sequedad del subsuelo y la riqueza de minerales produjeron una momificación natural insólita, " +
            "sellando para la posteridad las expresiones de asombro y agonía de quienes yacen tras el cristal."
        ));

        allNotes.Add(new LoreEntry(
            "MomiaErrante",
            "CRÓNICA III DE VI",
            "Bitácora del Custodio Mayor - La Que Acecha",
            "Pasillo Central Este",
            "Testimonio manuscrito de Don Jacinto (1894):\n\n" +
            "'La que camina en la sombra no puede dar un solo paso si tus ojos están clavados en ella. " +
            "Mantén la mirada fija y alumbra su silueta con la linterna. Si le das la espalda en la penumbra o te quedas sin luz, " +
            "avanzará en absoluto silencio hasta que sientas su aliento marchito.'"
        ));

        allNotes.Add(new LoreEntry(
            "GranBoveda",
            "CRÓNICA IV DE VI",
            "La Vitrina Central de la Bóveda - El Despertar",
            "Gran Bóveda Norte",
            "Registro de la Vitrina Mayor:\n\n" +
            "Aquí reposaba el espécimen más antiguo rescatado del panteón colonial. " +
            "Al tañer las doce campanadas, las veladoras del altar mayor se apagaron de golpe y el cristal blindado estalló en pedazos desde el interior. " +
            "La entidad deambula por las salas reclamando el reposo arrebatado por los vivos."
        ));

        allNotes.Add(new LoreEntry(
            "Cripta",
            "CRÓNICA V DE VI",
            "La Compuerta de Escape y los Túneles Coloniales",
            "Cripta Sudoeste",
            "Bajo las baldosas de la cripta corre el antiguo túnel de desagüe virreinal que conduce al exterior del museo. " +
            "La pesada compuerta de hierro está asegurada con tres pasadores coloniales forjados a mano. " +
            "Solo la Llave Maestra Colonial custodiada sobre el pedestal del santuario de la Galería puede liberar los pestillos."
        ));

        allNotes.Add(new LoreEntry(
            "Almacen",
            "CRÓNICA VI DE VI",
            "Registro de Plagas y Escarabajos de Sepulcro",
            "Almacén Sudeste",
            "Informe de Salubridad Arqueológica:\n\n" +
            "Las excavaciones recientes perturbaron los nidos de escarabajos de sepulcro (Necrophila). " +
            "Son insectos carroñeros voraces atraídos por el ruido y el calor corporal de los vivos. " +
            "No permitas que te acorralen por los flancos; un disparo certero de pistola o el calor del fuego los aniquila al instante."
        ));
    }

    private void IniciarAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (paperSound == null) paperSound = GenerarSonidoPapel();
    }

    private AudioClip GenerarSonidoPapel()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float rustle = (Random.value * 2f - 1f) * Mathf.Exp(-t * 20f) * 0.45f;
            float scrape = Mathf.Sin(2f * Mathf.PI * 1800f * t) * (Random.value * 0.25f);
            samples[i] = Mathf.Clamp(rustle + scrape, -1f, 1f) * 0.65f;
        }

        AudioClip clip = AudioClip.Create("Papel_Rustle", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void CrearTexturas()
    {
        texFondoSombra = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.78f));
        texPapelNota = MakeTex(2, 2, new Color(0.96f, 0.92f, 0.82f, 0.98f));
        texBordeNota = MakeTex(2, 2, new Color(0.55f, 0.38f, 0.12f, 1f));
        texBoton = MakeTex(2, 2, new Color(0.28f, 0.18f, 0.08f, 1f));
        texBotonActivo = MakeTex(2, 2, new Color(0.55f, 0.35f, 0.12f, 1f));
        texFilaCatalog = MakeTex(2, 2, new Color(0.90f, 0.85f, 0.73f, 0.9f));
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void Update()
    {
        // Atajo global: Tecla [N] para abrir el catálogo de crónicas desde cualquier punto
        if (!isPanelOpen)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                OpenLoreIndex();
            }
            return;
        }

        // CRUCIAL: Si la nota se abrió en este mismo frame o hace menos de 0.25 segundos,
        // no procesar teclas de cierre (evita que la misma pulsación de [E] que la abrió la cierre inmediatamente
        // debido a que otros scripts se ejecutan antes en el mismo frame).
        if (Time.frameCount == openFrame || (Time.unscaledTime - openTime) < 0.25f)
        {
            return;
        }

        // Cerrar con E o Escape (Espacio se reserva para salto del vigilante)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || TouchControlsManager.IsInteractTriggered())
        {
            CloseLorePanel();
            return;
        }

        // Navegación rápida entre notas con A / D o Flechas
        if (!isCatalogMode)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PreviousNote();
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextNote();
            }
        }

        // Alternar modo catálogo con [Tab] o [I]
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            isCatalogMode = !isCatalogMode;
            PlayPaperSound();
        }
    }

    private int BuscarIndiceNota(string title)
    {
        if (string.IsNullOrEmpty(title)) return 0;

        string cleanTitle = NormalizarTexto(title);

        // 1. Coincidencia directa o contenida con el título oficial
        for (int i = 0; i < allNotes.Count; i++)
        {
            string noteTitleClean = NormalizarTexto(allNotes[i].title);
            if (noteTitleClean.Contains(cleanTitle) || cleanTitle.Contains(noteTitleClean))
            {
                return i;
            }
        }

        // 2. Búsqueda inteligente por conceptos clave de cada sala histórica
        if (cleanTitle.Contains("reglamento") || cleanTitle.Contains("toque") || cleanTitle.Contains("12:00") || cleanTitle.Contains("cronica i")) return 0;
        if (cleanTitle.Contains("colera") || cleanTitle.Contains("morbus") || cleanTitle.Contains("1833") || cleanTitle.Contains("cronica ii")) return 1;
        if (cleanTitle.Contains("momia") || cleanTitle.Contains("acecha") || cleanTitle.Contains("custodio") || cleanTitle.Contains("cronica iii")) return 2;
        if (cleanTitle.Contains("boveda") || cleanTitle.Contains("vitrina") || cleanTitle.Contains("despertar") || cleanTitle.Contains("cronica iv")) return 3;
        if (cleanTitle.Contains("cripta") || cleanTitle.Contains("compuerta") || cleanTitle.Contains("escape") || cleanTitle.Contains("tunel") || cleanTitle.Contains("cronica v")) return 4;
        if (cleanTitle.Contains("almacen") || cleanTitle.Contains("escarabajo") || cleanTitle.Contains("plaga") || cleanTitle.Contains("cronica vi")) return 5;

        return -1;
    }

    private static string NormalizarTexto(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.ToLowerInvariant()
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("ñ", "n");
    }

    public void OpenLorePanel(string title, string description)
    {
        int idx = BuscarIndiceNota(title);

        if (idx >= 0)
        {
            currentNoteIndex = idx;
        }
        else
        {
            // Si es una nota custom no catalogada, verificar si ya existe antes de crear duplicado
            int existente = allNotes.FindIndex(n => n.title.Equals(title, System.StringComparison.OrdinalIgnoreCase));
            if (existente >= 0)
            {
                currentNoteIndex = existente;
            }
            else
            {
                LoreEntry temp = new LoreEntry("Custom", $"CRÓNICA {allNotes.Count + 1}", title, "Museo de Guanajuato", description);
                allNotes.Add(temp);
                currentNoteIndex = allNotes.Count - 1;
            }
        }

        isCatalogMode = false;
        isPanelOpen = true;
        openFrame = Time.frameCount;
        openTime = Time.unscaledTime;
        PlayPaperSound();

        // Pausa del juego para lectura cómoda
        Time.timeScale = 0f;
    }

    public void OpenLoreIndex()
    {
        isCatalogMode = true;
        isPanelOpen = true;
        openFrame = Time.frameCount;
        openTime = Time.unscaledTime;
        PlayPaperSound();
        Time.timeScale = 0f;
    }

    public void NextNote()
    {
        if (allNotes.Count == 0) return;
        currentNoteIndex = (currentNoteIndex + 1) % allNotes.Count;
        PlayPaperSound();
    }

    public void PreviousNote()
    {
        if (allNotes.Count == 0) return;
        currentNoteIndex = (currentNoteIndex - 1 + allNotes.Count) % allNotes.Count;
        PlayPaperSound();
    }

    public void CloseLorePanel()
    {
        isPanelOpen = false;
        closeFrame = Time.frameCount;
        lastCloseTime = Time.unscaledTime;
        PlayPaperSound();

        // Reanudar tiempo
        Time.timeScale = 1f;
    }

    private void PlayPaperSound()
    {
        if (audioSource != null && paperSound != null)
        {
            audioSource.PlayOneShot(paperSound, 0.85f);
        }
    }

    private void SetAllTextColors(GUIStyle style, Color col)
    {
        if (style == null) return;
        style.normal.textColor = col;
        style.hover.textColor = col;
        style.active.textColor = col;
        style.focused.textColor = col;
        style.onNormal.textColor = col;
        style.onHover.textColor = col;
        style.onActive.textColor = col;
        style.onFocused.textColor = col;
    }

    private float lastScale = -1f;

    private void InitStyles(float s)
    {
        if (titleStyle != null && Mathf.Approximately(s, lastScale)) return;
        lastScale = s;

        Color inkDark = new Color(0.12f, 0.07f, 0.03f, 1f);
        Color inkBrown = new Color(0.40f, 0.22f, 0.08f, 1f);

        headerBadgeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(12 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        SetAllTextColors(headerBadgeStyle, inkBrown);

        locationStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(11 * s),
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        SetAllTextColors(locationStyle, new Color(0.35f, 0.25f, 0.15f, 1f));

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        SetAllTextColors(titleStyle, inkDark);

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(13 * s),
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            richText = true
        };
        SetAllTextColors(bodyStyle, inkDark);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(11 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        buttonStyle.normal.background = texBoton;
        buttonStyle.normal.textColor = new Color(1f, 0.92f, 0.75f);
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;

        buttonActiveStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(11 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        buttonActiveStyle.normal.background = texBotonActivo;
        buttonActiveStyle.normal.textColor = Color.white;
        buttonActiveStyle.hover.textColor = Color.white;
        buttonActiveStyle.active.textColor = Color.white;

        instructionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(10 * s),
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        SetAllTextColors(instructionStyle, new Color(0.35f, 0.25f, 0.15f, 1f));

        catalogItemStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(12 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        catalogItemStyle.normal.background = texFilaCatalog;
        SetAllTextColors(catalogItemStyle, inkDark);
    }

    private void OnGUI()
    {
        if (!isPanelOpen) return;

        ResponsiveGUIHelper.BeginScaledGUI(out float vw, out float vh, out Matrix4x4 prevMatrix, 740f, 430f);
        try
        {
            float s = 1f;
            InitStyles(s);

            // 1. Fondo oscurecido en pantalla completa
            GUI.DrawTexture(new Rect(0, 0, vw, vh), texFondoSombra);

            // 2. Dimensiones responsivas de la nota tipo pergamino
            float w = Mathf.Min(640f * s, vw - 24f);
            float h = Mathf.Min(370f * s, vh - 24f);
            Rect noteRect = new Rect((vw - w) / 2f, (vh - h) / 2f, w, h);

            // Borde decorativo
            GUI.DrawTexture(new Rect(noteRect.x - 4 * s, noteRect.y - 4 * s, noteRect.width + 8 * s, noteRect.height + 8 * s), texBordeNota);
            // Papel de pergamino
            GUI.DrawTexture(noteRect, texPapelNota);

            // Si tocan la pantalla fuera del pergamino (fondo oscuro), cerrar la nota con comodidad
            if (Event.current.type == EventType.MouseDown && !noteRect.Contains(Event.current.mousePosition) && (Time.unscaledTime - openTime) >= 0.35f)
            {
                CloseLorePanel();
                return;
            }

            if (isCatalogMode)
            {
                RenderCatalogMode(noteRect, s);
            }
            else
            {
                RenderReaderMode(noteRect, s);
            }
        }
        finally
        {
            ResponsiveGUIHelper.EndScaledGUI(prevMatrix);
        }
    }

    private void RenderReaderMode(Rect noteRect, float s)
    {
        if (allNotes.Count == 0) return;
        currentNoteIndex = Mathf.Clamp(currentNoteIndex, 0, allNotes.Count - 1);
        LoreEntry entry = allNotes[currentNoteIndex];

        // 0. Pestañas superiores para ver y seleccionar todas las crónicas de un vistazo
        float tabW = (noteRect.width - 30f * s) / Mathf.Max(1, allNotes.Count);
        float tabH = 26f * s;
        for (int i = 0; i < allNotes.Count; i++)
        {
            Rect tabRect = new Rect(noteRect.x + 15 * s + i * tabW, noteRect.y + 8 * s, tabW - 3 * s, tabH);
            bool esActual = (i == currentNoteIndex);
            string tabLabel = $"Crónica {i + 1}";
            if (GUI.Button(tabRect, tabLabel, esActual ? buttonActiveStyle : buttonStyle))
            {
                currentNoteIndex = i;
                isCatalogMode = false;
                PlayPaperSound();
            }
        }

        // 1. Encabezado de la nota
        GUI.Label(new Rect(noteRect.x + 15 * s, noteRect.y + 38 * s, noteRect.width - 30 * s, 18 * s),
            $"<color=#5C240A>📜 ARCHIVO HISTÓRICO - MUSEO DE GUANAJUATO   [ {entry.chapterBadge} ]</color>", headerBadgeStyle);

        GUI.Label(new Rect(noteRect.x + 15 * s, noteRect.y + 56 * s, noteRect.width - 30 * s, 18 * s),
            $"<color=#4A3520>📍 {entry.location}</color>", locationStyle);

        GUI.Label(new Rect(noteRect.x + 15 * s, noteRect.y + 74 * s, noteRect.width - 30 * s, 24 * s),
            $"<color=#241103>{entry.title.ToUpper()}</color>", titleStyle);

        // Línea divisoria decorativa
        GUI.DrawTexture(new Rect(noteRect.x + 25 * s, noteRect.y + 100 * s, noteRect.width - 50 * s, 2), texBordeNota);

        // 2. Contenido de la historia
        float bodyH = noteRect.height - 162f * s;
        string cleanDesc = $"<color=#180D04>{entry.description}</color>";
        GUI.Label(new Rect(noteRect.x + 25 * s, noteRect.y + 106 * s, noteRect.width - 50 * s, bodyH), cleanDesc, bodyStyle);

        // 3. Barra de navegación y botones inferiores
        float btnY = noteRect.y + noteRect.height - 50f * s;
        float btnH = 28f * s;

        // Botón Anterior
        float btnW1 = 110f * s;
        if (GUI.Button(new Rect(noteRect.x + 25 * s, btnY, btnW1, btnH), "◀ ANTERIOR [A]", buttonStyle))
        {
            PreviousNote();
        }

        // Botón Ver Todas (Catálogo)
        float btnW2 = 135f * s;
        if (GUI.Button(new Rect(noteRect.x + 30 * s + btnW1, btnY, btnW2, btnH), "VER TODAS [Tab]", buttonActiveStyle))
        {
            isCatalogMode = true;
            PlayPaperSound();
        }

        // Botón Siguiente
        float btnW3 = 110f * s;
        if (GUI.Button(new Rect(noteRect.x + 35 * s + btnW1 + btnW2, btnY, btnW3, btnH), "SIGUIENTE [D] ▶", buttonStyle))
        {
            NextNote();
        }

        // Botón Guardar / Cerrar
        float btnW4 = 145f * s;
        if (GUI.Button(new Rect(noteRect.x + noteRect.width - btnW4 - 25 * s, btnY, btnW4, btnH), "GUARDAR NOTA [E]", buttonStyle))
        {
            CloseLorePanel();
        }

        GUI.Label(new Rect(noteRect.x + 15 * s, noteRect.y + noteRect.height - 20f * s, noteRect.width - 30 * s, 18 * s),
            "<color=#443322>Presiona [E] para guardar  |  [A]/[D] o Pestañas para cambiar  |  [Tab] Índice</color>", instructionStyle);
    }

    private void RenderCatalogMode(Rect noteRect, float s)
    {
        // Encabezado del catálogo
        GUI.Label(new Rect(noteRect.x + 15 * s, noteRect.y + 10 * s, noteRect.width - 30 * s, 22 * s),
            "<color=#241103>📜 ÍNDICE GENERAL DE CRÓNICAS Y BITÁCORAS DEL MUSEO</color>", titleStyle);

        GUI.Label(new Rect(noteRect.x + 15 * s, noteRect.y + 34 * s, noteRect.width - 30 * s, 18 * s),
            "<color=#4A3520>Selecciona cualquier crónica para leer su historia completa:</color>", locationStyle);

        GUI.DrawTexture(new Rect(noteRect.x + 25 * s, noteRect.y + 54 * s, noteRect.width - 50 * s, 2), texBordeNota);

        // Lista scrolleable de crónicas
        float listY = noteRect.y + 60 * s;
        float listH = noteRect.height - 116 * s;
        float itemH = 46f * s;
        Rect viewRect = new Rect(0, 0, noteRect.width - 70 * s, allNotes.Count * itemH);
        catalogScroll = GUI.BeginScrollView(new Rect(noteRect.x + 25 * s, listY, noteRect.width - 50 * s, listH), catalogScroll, viewRect);

        for (int i = 0; i < allNotes.Count; i++)
        {
            LoreEntry entry = allNotes[i];
            Rect itemRect = new Rect(4 * s, i * itemH + 2f * s, viewRect.width - 8 * s, itemH - 4f * s);

            bool esActual = (i == currentNoteIndex);
            string label = $" {(esActual ? "▶" : "▪")}  <color=#180D04><b>{entry.title}</b></color>\n      <color=#5A3818><i>{entry.location}</i></color>";

            if (GUI.Button(itemRect, label, esActual ? buttonActiveStyle : catalogItemStyle))
            {
                currentNoteIndex = i;
                isCatalogMode = false;
                PlayPaperSound();
            }
        }

        GUI.EndScrollView();

        // Botones de acción en catálogo
        float btnY = noteRect.y + noteRect.height - 48f * s;
        float btnH = 28f * s;

        if (GUI.Button(new Rect(noteRect.x + 30 * s, btnY, 190 * s, btnH), "◀ VOLVER A LA LECTURA", buttonStyle))
        {
            isCatalogMode = false;
            PlayPaperSound();
        }

        if (GUI.Button(new Rect(noteRect.x + noteRect.width - 150 * s, btnY, 120 * s, btnH), "CERRAR [E]", buttonStyle))
        {
            CloseLorePanel();
        }
    }
}
