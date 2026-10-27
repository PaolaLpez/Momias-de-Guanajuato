using UnityEngine;
using UnityEngine.SceneManagement;

public class Nivel1Manager : MonoBehaviour
{
    private PlayerController player;
    private MomiaController momia;
    private CameraFollow camFollow;

    private bool nivelCompletado = false;
    private bool juegoTerminado = false;

    // Estilos GUI compactos
    private GUIStyle headerStyle;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;
    private Texture2D texVidaVigilante;
    private Texture2D texVidaMomia;
    private Texture2D texFondoBarra;
    private Texture2D texPanel;
    private Texture2D texPanelGameOver;
    private Texture2D texAuraMomia;
    private Texture2D[] texMomiaGritando;
    private AudioSource audioGameOver;
    private AudioClip clipGritoMomia;

    private GUIStyle gameOverPanelStyle;
    private GUIStyle gameOverTitleStyle;
    private GUIStyle gameOverReasonStyle;
    private GUIStyle gameOverButtonStyle;
    private GUIStyle textStyleCenter;

    [Header("Reloj del Museo (12:00 AM a 1:00 AM)")]
    public float duracionNivelSegundos = 240f; // 4 minutos reales = 1 hora ficticia
    private float tiempoTranscurrido = 0f;
    private string textoReloj = "12:00 AM";
    private string tiempoRestanteStr = "04:00";
    private string motivoGameOver = "La momia te atrapó en la oscuridad.\nRecuerda: no debes quitarle los ojos de encima.";

    private GUIStyle clockTitleStyle;
    private GUIStyle clockSubStyle;

    void Start()
    {
        AjustadorColisionesMuseo.AjustarTodo();
        BuscarReferencias();
        CrearTexturas();

        // En cuanto comienza el nivel, la momia se activa y comienza a acechar
        if (momia != null)
        {
            momia.gameObject.SetActive(true);
            momia.enabled = true;
        }
    }

    void BuscarReferencias()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerController>();
        }

        if (momia == null)
        {
            momia = FindFirstObjectByType<MomiaController>();
        }

        if (camFollow == null)
        {
            camFollow = FindFirstObjectByType<CameraFollow>();
        }
    }

    void Update()
    {
        if (nivelCompletado || juegoTerminado) return;

        // El tiempo corre en cuanto comienza el nivel
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

    void CrearTexturas()
    {
        texVidaVigilante = MakeTex(2, 2, new Color(0.2f, 0.85f, 0.35f, 0.95f)); // Verde brillante
        texVidaMomia = MakeTex(2, 2, new Color(0.95f, 0.25f, 0.2f, 0.95f));     // Rojo intenso
        texFondoBarra = MakeTex(2, 2, new Color(0.12f, 0.12f, 0.15f, 0.9f));    // Gris oscuro
        texPanel = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.1f, 0.88f));         // Fondo semitransparente compacto
        texPanelGameOver = MakeTex(2, 2, new Color(0.06f, 0.02f, 0.02f, 0.96f)); // Fondo sangre oscura
        texAuraMomia = MakeTex(2, 2, new Color(0.85f, 0.08f, 0.08f, 0.45f));    // Aura roja maldita

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

    public void ActivarVictoria()
    {
        if (juegoTerminado || nivelCompletado) return;
        nivelCompletado = true;
        Time.timeScale = 0f;
    }

    public void ActivarGameOver(string motivo = null)
    {
        if (juegoTerminado || nivelCompletado) return;
        juegoTerminado = true;
        if (!string.IsNullOrEmpty(motivo))
        {
            motivoGameOver = motivo;
        }

        if (audioGameOver == null)
        {
            audioGameOver = gameObject.AddComponent<AudioSource>();
            audioGameOver.playOnAwake = false;
        }
        if (clipGritoMomia == null) clipGritoMomia = GenerarAlaridoMomia();
        if (audioGameOver != null && clipGritoMomia != null)
        {
            audioGameOver.PlayOneShot(clipGritoMomia, 0.95f);
        }
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
            SceneManager.LoadScene("Nivel 2");
        }
        catch
        {
            SceneManager.LoadScene("Nivel1");
        }
    }

    void OnGUI()
    {
        BuscarReferencias();
        InitStyles();

        // 1. HUD SUPERIOR: VIGILANTE (IZQUIERDA)
        float barraW = 120f;
        float barraH = 10f;

        GUI.Box(new Rect(10, 10, 210, 60), "", boxStyle);
        GUI.Label(new Rect(18, 12, 190, 16), "VIGILANTE", headerStyle);
        float pctVigilante = player != null ? Mathf.Clamp01(player.vidaActual / player.vidaMaxima) : 0f;
        GUI.DrawTexture(new Rect(18, 30, barraW, barraH), texFondoBarra);
        GUI.DrawTexture(new Rect(18, 30, barraW * pctVigilante, barraH), texVidaVigilante);
        GUI.Label(new Rect(144, 27, 60, 16), $"{(int)(pctVigilante * 100)}%", textStyle);

        string objText = (player != null && player.tieneLlave) ? "<color=#FFD700>✓ Llave lista (Ve a Cripta)</color>" : "Busca la llave en Galería";
        GUI.Label(new Rect(18, 44, 190, 16), "Obj: " + objText, textStyle);

        // 2. RELOJ DEL MUSEO (CENTRO SUPERIOR: 12:00 AM A 1:00 AM)
        float relojW = 230f;
        float relojH = 50f;
        Rect relojRect = new Rect((Screen.width - relojW) / 2f, 10f, relojW, relojH);
        GUI.Box(relojRect, "", boxStyle);

        bool tiempoPoco = (duracionNivelSegundos - tiempoTranscurrido) < 45f;
        string colorHora = tiempoPoco ? "#FF4444" : "#FFD700";

        GUI.Label(new Rect(relojRect.x, relojRect.y + 3, relojW, 24), $"<color={colorHora}>⏰ {textoReloj}</color>", clockTitleStyle);
        GUI.Label(new Rect(relojRect.x, relojRect.y + 27, relojW, 18), $"<color=#CCCCCC>Escapa antes de la 1:00 AM ({tiempoRestanteStr})</color>", clockSubStyle);

        // Botón de mapa completo / cámara jugador debajo del reloj
        string mapBtnText = (camFollow != null && camFollow.EstaViendoMapaCompleto()) ? "[M] Zoom Jugador" : "[M] Ver Museo Completo";
        if (GUI.Button(new Rect((Screen.width - 160) / 2f, relojRect.yMax + 5, 160, 22), mapBtnText, buttonStyle))
        {
            if (camFollow != null) camFollow.AlternarMapaCompleto();
        }

        // 3. HUD SUPERIOR: MOMIA 1 (DERECHA)
        GUI.Box(new Rect(Screen.width - 210, 10, 200, 60), "", boxStyle);
        GUI.Label(new Rect(Screen.width - 200, 12, 180, 16), "MOMIA 1", headerStyle);
        float pctMomia = momia != null ? Mathf.Clamp01(momia.vidaActual / momia.vidaMaxima) : 1f;
        GUI.DrawTexture(new Rect(Screen.width - 200, 30, barraW, barraH), texFondoBarra);
        GUI.DrawTexture(new Rect(Screen.width - 200, 30, barraW * pctMomia, barraH), texVidaMomia);
        GUI.Label(new Rect(Screen.width - 74, 27, 60, 16), $"{(int)(pctMomia * 100)}%", textStyle);

        string alerta = (momia != null && momia.estaSiendoObservada) ? "<color=#66FF66>● Quieta (La miras)</color>" : "<color=#FF4444>▲ Acechando (No la miras)</color>";
        GUI.Label(new Rect(Screen.width - 200, 44, 180, 16), alerta, textStyle);

        // Mini guía de controles en la esquina inferior izquierda
        GUI.Label(new Rect(10, Screen.height - 24, 480, 20), "<color=#AAAAAA>Flechas: Mover | Espacio: Saltar | Z: Tolete | X: Pistola | M: Ver Mapa</color>", textStyle);

        // 4. MODAL DE VICTORIA
        if (nivelCompletado)
        {
            float w = 400f;
            float h = 210f;
            Rect center = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.Box(center, "", boxStyle);
            GUI.Label(new Rect(center.x + 15, center.y + 20, w - 30, 35), "¡FELICIDADES!", titleStyle);
            GUI.Label(new Rect(center.x + 15, center.y + 60, w - 30, 25), "Nivel 1 Superado", headerStyle);
            GUI.Label(new Rect(center.x + 15, center.y + 90, w - 30, 45), $"Has escapado del museo a tiempo ({textoReloj}) antes de la 1:00 AM.", textStyle);

            if (GUI.Button(new Rect(center.x + (w - 160) / 2f, center.y + 145, 160, 38), "SIGUIENTE", buttonStyle))
            {
                SiguienteNivel();
            }
        }

        // 5. MODAL DE GAME OVER
        if (juegoTerminado)
        {
            // Ventana emergente amplia y tenebrosa
            float w = 480f;
            float h = 422f;
            Rect center = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            // Sombra oscura exterior de fondo
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texPanel);
            GUI.color = Color.white;

            // Marco principal de Game Over
            GUI.Box(center, "", gameOverPanelStyle ?? boxStyle);

            // Borde superior sangriento
            GUI.color = new Color(0.85f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(center.x + 8, center.y + 6, w - 16, 2), texVidaMomia);
            GUI.color = Color.white;

            // Título: GAME OVER
            GUI.Label(new Rect(center.x + 15, center.y + 12, w - 30, 32), "☠ GAME OVER ☠", gameOverTitleStyle ?? titleStyle);

            // Razón de la muerte
            GUI.Label(new Rect(center.x + 20, center.y + 46, w - 40, 44), motivoGameOver, gameOverReasonStyle ?? textStyle);

            // Live Animated Screaming Mummy (debajo de la razón de Game Over)
            if (texMomiaGritando != null && texMomiaGritando.Length > 0)
            {
                int[] screamSeq = new int[] { 0, 1, 4, 5, 4, 1, 0, 2, 3, 5, 1 };
                int frameIdx = screamSeq[(int)(Time.unscaledTime * 8f) % screamSeq.Length];
                Texture2D currentFrameTex = texMomiaGritando[Mathf.Clamp(frameIdx, 0, texMomiaGritando.Length - 1)];

                if (currentFrameTex != null)
                {
                    // Temblores tenebrosos de alarido
                    float shakeX = Mathf.Sin(Time.unscaledTime * 38f) * 2.5f;
                    float shakeY = Mathf.Cos(Time.unscaledTime * 30f) * 1.5f;
                    float pulse = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 9f);

                    float mw = 175f * pulse;
                    float mh = 205f * pulse;
                    Rect mummyRect = new Rect(center.x + (w - mw) / 2f + shakeX, center.y + 96f + shakeY, mw, mh);

                    // Aura roja maldita detrás de la momia gritando
                    if (texAuraMomia != null)
                    {
                        float auraAlpha = 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 6f);
                        GUI.color = new Color(0.85f, 0.1f, 0.1f, auraAlpha);
                        GUI.DrawTexture(new Rect(mummyRect.x - 18, mummyRect.y - 12, mw + 36, mh + 24), texAuraMomia);
                        GUI.color = Color.white;
                    }

                    // Renderizado de la textura de la momia rugiendo
                    GUI.DrawTexture(mummyRect, currentFrameTex, ScaleMode.ScaleToFit);

                    // Leyenda atmosférica bajo la momia
                    GUI.Label(new Rect(center.x + 15, center.y + 306, w - 30, 22),
                        "<color=#FF5555><i>«¡El alma en pena de Guanajuato clama por tu carne! »</i></color>", textStyleCenter ?? textStyle);
                }
            }

            // Borde inferior sangriento
            GUI.color = new Color(0.85f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(center.x + 8, center.y + 338, w - 16, 2), texVidaMomia);
            GUI.color = Color.white;

            // Botón de reintentar
            if (GUI.Button(new Rect(center.x + (w - 220) / 2f, center.y + 352, 220, 42), "☠ REINTENTAR NIVEL 1 ☠", gameOverButtonStyle ?? buttonStyle))
            {
                ReintentarNivel();
            }
        }
    }

    private void InitStyles()
    {
        if (boxStyle != null) return;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = texPanel;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 11;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.95f, 0.85f, 0.6f);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.95f, 0.75f, 0.2f);

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 10;
        textStyle.richText = true;
        textStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 11;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.white;

        clockTitleStyle = new GUIStyle(GUI.skin.label);
        clockTitleStyle.fontSize = 16;
        clockTitleStyle.fontStyle = FontStyle.Bold;
        clockTitleStyle.alignment = TextAnchor.MiddleCenter;
        clockTitleStyle.richText = true;

        clockSubStyle = new GUIStyle(GUI.skin.label);
        clockSubStyle.fontSize = 10;
        clockSubStyle.fontStyle = FontStyle.Normal;
        clockSubStyle.alignment = TextAnchor.MiddleCenter;
        clockSubStyle.richText = true;

        gameOverPanelStyle = new GUIStyle(GUI.skin.box);
        gameOverPanelStyle.normal.background = texPanelGameOver ?? texPanel;

        gameOverTitleStyle = new GUIStyle(GUI.skin.label);
        gameOverTitleStyle.fontSize = 22;
        gameOverTitleStyle.fontStyle = FontStyle.Bold;
        gameOverTitleStyle.alignment = TextAnchor.MiddleCenter;
        gameOverTitleStyle.richText = true;
        gameOverTitleStyle.normal.textColor = new Color(0.95f, 0.2f, 0.2f);

        gameOverReasonStyle = new GUIStyle(GUI.skin.label);
        gameOverReasonStyle.fontSize = 11;
        gameOverReasonStyle.fontStyle = FontStyle.Bold;
        gameOverReasonStyle.alignment = TextAnchor.MiddleCenter;
        gameOverReasonStyle.richText = true;
        gameOverReasonStyle.normal.textColor = new Color(1f, 0.9f, 0.75f);

        textStyleCenter = new GUIStyle(GUI.skin.label);
        textStyleCenter.fontSize = 10;
        textStyleCenter.fontStyle = FontStyle.Italic;
        textStyleCenter.alignment = TextAnchor.MiddleCenter;
        textStyleCenter.richText = true;
        textStyleCenter.normal.textColor = new Color(1f, 0.45f, 0.45f);

        gameOverButtonStyle = new GUIStyle(GUI.skin.button);
        gameOverButtonStyle.fontSize = 12;
        gameOverButtonStyle.fontStyle = FontStyle.Bold;
        gameOverButtonStyle.alignment = TextAnchor.MiddleCenter;
        gameOverButtonStyle.normal.textColor = Color.white;
    }
}
