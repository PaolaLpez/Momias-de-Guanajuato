using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Generador del Escenario del Nivel 3: "Las Hermanas" — Taller de Restauración y Conservación de Momias (2:00 AM a 3:00 AM).
/// Totalmente renovado con la misma o superior calidad visual que el Nivel 2:
/// - Baldosas quirúrgicas con pátina, manchas y textura realista.
/// - Suelo continuo y hermético sin ningún hueco ni vacío negro.
/// - Camillas de acero inoxidable donde las momias comienzan ACOSTADAS físicamente y se levantan secuencialmente.
/// - Consola de CCTV interactiva dentro de la caseta (se revisa a voluntad con [E]).
/// - Puertas y umbrales amplios (8m de ancho) para garantizar movimiento fluido sin atascarse.
/// - Luces URP 2D volumétricas con parpadeo realista en tubos fluorescentes.
/// - Set dressing: Pizarra con fórmulas, frascos con órganos en formol, pared de gavetas mortuorias con escarcha y niebla fría.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class GeneradorEscenarioNivel3 : MonoBehaviour
{
    public const int VERSION_LAYOUT_NIVEL3 = 23;

    private Sprite floorSprite;
    private Sprite wallSprite;
    private Sprite crateSprite;
    private Sprite doorSprite;
    private Sprite deskSprite;
    private Sprite benchSprite;
    private Sprite blankSprite;

    // Sprites procedurales de alta fidelidad y ambientación terrorífica
    private Sprite spritePisoQuirurgico;
    private Sprite spritePisoLab;
    private Sprite spritePisoMorgue;
    private Sprite spritePisoCaseta;
    private Sprite spriteMuroTaller;

    private Sprite spriteCamillaMetalica;
    private Sprite spriteMomiaAcostada1;
    private Sprite spriteMomiaAcostada2;
    private Sprite spriteMomiaCabezaGirada;
    private Sprite spriteCamillaVaciaVendas;
    private Sprite spriteArrastreSangre;

    private Sprite spritePizarraLab;
    private Sprite spriteEstanteOrganos;
    private Sprite spriteBateriaMorgue;
    private Sprite spriteLamparaCirugia;
    private Sprite spriteCharcoReactivo;
    private Sprite spriteCristalesRotos;
    private Sprite spriteNieblaHelada;

    // Props y escondites detallados de terror
    private Sprite spriteConsolaCCTVRealista;
    private Sprite spriteCajaFuerteAcorazada;
    private Sprite spriteCasilleroMetalico;
    private Sprite spriteBaulAntiguo;
    private Sprite spriteGavetaMorgueAbierta;
    private Sprite spriteBiomboQuirurgico;
    private Sprite spriteCamillaConSabana;
    private Sprite spriteBajoEscritorio;
    private Sprite spriteEtiquetaForense;
    private Sprite spriteTermometroMorgue;

    private void Awake()
    {
        VerificarYConstruir();
    }

    private void OnEnable()
    {
        VerificarYConstruir();
    }

    private void Start()
    {
        VerificarYConstruir();
    }

    public void VerificarYConstruir()
    {
        GameObject existente = GameObject.Find("TallerRestauracionNivel3");
        if (existente != null)
        {
            MarcadorVersionSubterraneoNivel2 marcador = existente.GetComponent<MarcadorVersionSubterraneoNivel2>();
            if (marcador != null && marcador.version == VERSION_LAYOUT_NIVEL3 && existente.transform.childCount > 20)
            {
                return;
            }
        }

        CargarRecursosYGenerarTexturas();
        ConstruirTallerCompleto();
    }

    private void CargarRecursosYGenerarTexturas()
    {
        HojasSpritesNivel3.LimpiarCache();

        floorSprite = Resources.Load<Sprite>("Tiles/tile_floor");
        if (floorSprite == null) floorSprite = Resources.Load<Sprite>("tile_floor");

        wallSprite = Resources.Load<Sprite>("Tiles/tile_wall");
        if (wallSprite == null) wallSprite = Resources.Load<Sprite>("tile_wall");

        crateSprite = Resources.Load<Sprite>("Tiles/prop_crate");
        if (crateSprite == null) crateSprite = Resources.Load<Sprite>("prop_crate");

        doorSprite = Resources.Load<Sprite>("Props/puerta_escape_closed");
        if (doorSprite == null) doorSprite = Resources.Load<Sprite>("prop_door");

        deskSprite = Resources.Load<Sprite>("Props/desk_opened");
        if (deskSprite == null) deskSprite = Resources.Load<Sprite>("desk_opened");

        benchSprite = Resources.Load<Sprite>("Props/prop_bench");
        if (benchSprite == null) benchSprite = Resources.Load<Sprite>("prop_bench");

        blankSprite = Resources.Load<Sprite>("Tiles/blank_square");
        if (blankSprite == null) blankSprite = CrearSpriteCuadrado();

        // Generación de texturas de alta resolución y ambientación terrorífica
        spritePisoQuirurgico = GenerarPisoBaldosasQuirurgicas();
        spritePisoLab = GenerarPisoLaboratorio();
        spritePisoMorgue = GenerarPisoMorgueAcero();
        spritePisoCaseta = GenerarPisoMaderaCaseta();
        spriteMuroTaller = GenerarSpriteMuroTaller();

        spriteCamillaMetalica = GenerarSpriteCamillaBase();
        spriteMomiaAcostada1 = GenerarSpriteMomiaAcostada(1);
        spriteMomiaAcostada2 = GenerarSpriteMomiaAcostada(2);
        spriteMomiaCabezaGirada = GenerarSpriteMomiaCabezaGirada();
        spriteCamillaVaciaVendas = GenerarSpriteCamillaVacia();
        spriteArrastreSangre = GenerarSpriteArrastreSangre();

        spritePizarraLab = GenerarSpritePizarraQuimica();
        spriteEstanteOrganos = GenerarSpriteEstanteFrascos();
        spriteBateriaMorgue = GenerarSpriteBateriaGavetasMorgue();
        spriteLamparaCirugia = GenerarSpriteLamparaCirugia();
        spriteCharcoReactivo = GenerarSpriteCharcoReactivo();
        spriteCristalesRotos = GenerarSpriteCristales();
        spriteNieblaHelada = GenerarSpriteNiebla();

        spriteConsolaCCTVRealista = GenerarSpriteConsolaCCTV();
        spriteCajaFuerteAcorazada = GenerarSpriteCajaFuerteAcorazada();
        spriteCasilleroMetalico = GenerarSpriteCasilleroMetalico();
        spriteBaulAntiguo = GenerarSpriteBaulAntiguo();
        spriteGavetaMorgueAbierta = GenerarSpriteGavetaMorgueAbierta();
        spriteBiomboQuirurgico = GenerarSpriteBiomboQuirurgico();
        spriteCamillaConSabana = GenerarSpriteCamillaConSabana();
        spriteBajoEscritorio = GenerarSpriteBajoEscritorio();
        spriteEtiquetaForense = GenerarSpriteEtiquetaForense();
        spriteTermometroMorgue = GenerarSpriteTermometroMorgue();
    }

    private Sprite CrearSpriteCuadrado()
    {
        Texture2D tex = new Texture2D(16, 16);
        Color[] cols = new Color[16 * 16];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    public void ConstruirTallerCompleto()
    {
        GameObject viejo = GameObject.Find("TallerRestauracionNivel3");
        if (viejo != null)
        {
            if (Application.isPlaying) Destroy(viejo);
            else DestroyImmediate(viejo);
        }

        GameObject viejoOld = GameObject.Find("TallerRestauracion_Root");
        if (viejoOld != null)
        {
            if (Application.isPlaying) Destroy(viejoOld);
            else DestroyImmediate(viejoOld);
        }

        GameObject root = new GameObject("TallerRestauracionNivel3");
        root.transform.position = Vector3.zero;

        MarcadorVersionSubterraneoNivel2 marcador = root.AddComponent<MarcadorVersionSubterraneoNivel2>();
        marcador.version = VERSION_LAYOUT_NIVEL3;

        // 1. Configurar Cámara y límites
        ConfigurarCamara();

        // 2. Luz Global atmosférica
        ConfigurarIluminacionGlobal(root);

        // 3. Suelos Temáticos continuos
        ConstruirSuelos(root);

        // 4. Muros con pasajes amplios sin atascos
        ConstruirMuros(root);

        // 5. Caseta de Vigilancia con CCTV interactivo y Caja Fuerte
        ConstruirCaseta(root);

        // 6. Sala Quirúrgica Central (Momias acostadas que despiertan)
        ConstruirSalaQuirurgica(root);

        // 7. Laboratorio Químico (Oeste)
        ConstruirLaboratorio(root);

        // 8. Cámaras Frías de Morgue (Este)
        ConstruirMorgueFria(root);

        // 9. Configuración del Jugador, Enemigos y Escorpiones
        ConfigurarJugadorYEnemigos(root);
    }

    private void ConfigurarCamara()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f); // Tinte ambiental hospitalario (sin vacíos negros)
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;
            cam.orthographicSize = 3.6f;

            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();
            cf.zoomCercano = 3.6f;
            cf.suavizado = 8f;
            cf.permitirTeclaM = false;

            cf.minX = -20f;
            cf.maxX = 20f;
            cf.minY = -12f;
            cf.maxY = 10f;
        }
    }

    private void ConfigurarIluminacionGlobal(GameObject root)
    {
        Light2D luzGlobal = FindFirstObjectByType<Light2D>();
        if (luzGlobal != null && luzGlobal.lightType == Light2D.LightType.Global)
        {
            luzGlobal.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            luzGlobal.intensity = 0.45f;
        }
        else
        {
            GameObject lgObj = new GameObject("LuzGlobalTaller3");
            lgObj.transform.SetParent(root.transform);
            Light2D lg = lgObj.AddComponent<Light2D>();
            lg.lightType = Light2D.LightType.Global;
            lg.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            lg.intensity = 0.45f;
        }
    }

    private void ConstruirSuelos(GameObject root)
    {
        GameObject contenedorSuelos = new GameObject("SuelosTaller");
        contenedorSuelos.transform.SetParent(root.transform);

        // Suelo Base Total Subyacente (Previene todo agujero negro en bordes)
        Color colPisoBase = new Color(0.18f, 0.20f, 0.22f, 1f);
        CrearPisoTile(contenedorSuelos, "PisoBase_TallerTotal", 0f, 0f, 56f, 34f, floorSprite, colPisoBase, -20);

        // Caseta de Vigilancia y Ala Norte: Suelo continuo de madera (sin cortes laterales ni huecos)
        CrearPisoTile(contenedorSuelos, "Piso_CasetaVigilancia", 0f, 7.8f, 48f, 9.5f, spritePisoCaseta, Color.white, -10);

        // Sala Quirúrgica Central: Baldosas cerámicas médicas con juntas
        CrearPisoTile(contenedorSuelos, "Piso_SalaQuirurgica", 0f, -2f, 16f, 14f, spritePisoQuirurgico, Color.white, -10);

        // Laboratorio de Reactivos: Suelo verdoso corroído
        CrearPisoTile(contenedorSuelos, "Piso_LaboratorioOeste", -14.5f, -2f, 16f, 14f, spritePisoLab, Color.white, -10);

        // Cámaras Frías: Placas de acero escarchado
        CrearPisoTile(contenedorSuelos, "Piso_MorgueEste", 14.5f, -2f, 16f, 14f, spritePisoMorgue, Color.white, -10);

        // Pasillos Conectores Horizontales
        CrearPisoTile(contenedorSuelos, "Piso_PasilloNorte", 0f, 3.2f, 48f, 4.5f, spritePisoQuirurgico, new Color(0.85f, 0.85f, 0.85f), -11);
    }

    private void CrearPisoTile(GameObject padre, string nombre, float x, float y, float w, float h, Sprite sp, Color color, int sorting)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp != null ? sp : blankSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.color = color;
        sr.sortingOrder = sorting;
    }

    private void ConstruirMuros(GameObject root)
    {
        GameObject contenedorMuros = new GameObject("MurosTaller");
        contenedorMuros.transform.SetParent(root.transform);

        Color colMuro = new Color(0.10f, 0.11f, 0.14f, 1f);

        // Muros Perimetrales Exteriores
        CrearMuroSolido(contenedorMuros, "Muro_Sur", 0f, -14.5f, 52f, 2f, colMuro);
        CrearMuroSolido(contenedorMuros, "Muro_Oeste", -23.5f, -1f, 2f, 29f, colMuro);
        CrearMuroSolido(contenedorMuros, "Muro_Este", 23.5f, -1f, 2f, 29f, colMuro);

        // Muro Norte con umbral para la puerta de escape
        CrearMuroSolido(contenedorMuros, "Muro_Norte_Ext_Izq", -14.5f, 11.5f, 18f, 2f, colMuro);
        CrearMuroSolido(contenedorMuros, "Muro_Norte_Ext_Der", 14.5f, 11.5f, 18f, 2f, colMuro);
        CrearMuroSolido(contenedorMuros, "Muro_Norte_Caseta_Izq", -4.5f, 11.5f, 5f, 2f, colMuro);
        CrearMuroSolido(contenedorMuros, "Muro_Norte_Caseta_Der", 4.5f, 11.5f, 5f, 2f, colMuro);

        // Muros Sur de la Caseta: Pasillo ultra amplio para caminar con holgura por debajo del escritorio y umbral central despejado
        CrearMuroSolido(contenedorMuros, "Muro_Caseta_Sur_Izq", -11.5f, 3.8f, 7f, 1.2f, colMuro);
        CrearMuroSolido(contenedorMuros, "Muro_Caseta_Sur_Der", 11.5f, 3.8f, 7f, 1.2f, colMuro);

        // Columnas divisorias entre salas con vanos de 6.5m de paso libre y corredores ensanchados
        CrearMuroSolido(contenedorMuros, "Muro_Div_LabOeste_Sup", -8.0f, 2.5f, 1.4f, 3.5f, colMuro);
        CrearMuroSolido(contenedorMuros, "Muro_Div_LabOeste_Inf", -8.0f, -8.0f, 1.4f, 4.5f, colMuro);

        CrearMuroSolido(contenedorMuros, "Muro_Div_MorgueEste_Sup", 8.0f, 2.5f, 1.4f, 3.5f, colMuro);
        CrearMuroSolido(contenedorMuros, "Muro_Div_MorgueEste_Inf", 8.0f, -8.0f, 1.4f, 4.5f, colMuro);
    }

    private void CrearMuroSolido(GameObject padre, string nombre, float x, float y, float w, float h, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteMuroTaller != null ? spriteMuroTaller : (wallSprite != null ? wallSprite : blankSprite);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.color = Color.white;
        sr.sortingOrder = 50;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);
    }

    private void ConstruirCaseta(GameObject root)
    {
        GameObject caseta = new GameObject("CasetaVigilancia");
        caseta.transform.SetParent(root.transform);

        // Luz cálida tenue de caseta de guardia
        GameObject luzC = new GameObject("LuzCaseta");
        luzC.transform.position = new Vector3(0f, 7.8f, 0f);
        luzC.transform.SetParent(caseta.transform);
        Light2D l2d = luzC.AddComponent<Light2D>();
        l2d.lightType = Light2D.LightType.Point;
        l2d.color = new Color(0.98f, 0.82f, 0.50f, 1f);
        l2d.intensity = 1.15f;
        l2d.pointLightOuterRadius = 7.0f;

        // Consola de CCTV Interactiva y Escritorio de Vigilancia (Ubicado arriba a la derecha, dejando pasillo gigante debajo)
        GameObject cctv = new GameObject("ConsolaCCTV");
        cctv.transform.position = new Vector3(8.5f, 9.0f, 0f);
        cctv.transform.SetParent(caseta.transform);
        SpriteRenderer cSr = cctv.AddComponent<SpriteRenderer>();
        cSr.sprite = spriteConsolaCCTVRealista != null ? spriteConsolaCCTVRealista : deskSprite;
        cSr.sortingOrder = 40;
        cctv.transform.localScale = new Vector3(2.2f, 1.8f, 1f);
        BoxCollider2D cctvCol = cctv.AddComponent<BoxCollider2D>();
        cctvCol.size = new Vector2(2.0f, 1.2f);
        cctvCol.offset = new Vector2(0f, -0.25f);
        cctvCol.isTrigger = false;
        cctv.AddComponent<CCTVMonitorNivel3>();

        // Resplandor verde fósforo de los monitores CRT sobre el escritorio
        GameObject luzCctv = new GameObject("LuzPantallaCCTV");
        luzCctv.transform.position = new Vector3(8.5f, 9.0f, 0f);
        luzCctv.transform.SetParent(cctv.transform);
        Light2D lCctv = luzCctv.AddComponent<Light2D>();
        lCctv.lightType = Light2D.LightType.Point;
        lCctv.color = new Color(0.20f, 0.95f, 0.65f, 1f);
        lCctv.intensity = 0.95f;
        lCctv.pointLightOuterRadius = 4.5f;

        // También sirve como escondite bajo el escritorio (sin duplicar muebles amontonados)
        EsconditeNivel3 escEscritorio = cctv.AddComponent<EsconditeNivel3>();
        escEscritorio.nombreEscondite = "Bajo el Escritorio de Seguridad";
        escEscritorio.distanciaInteraccion = 2.4f;

        // Caja Fuerte Acorazada de Seguridad (Clave 7-3-9)
        // Ubicada en la pared norte (X: -5.5f, Y: 9.2f), con 6 metros de distancia al casillero para que nada esté empalmado
        GameObject safe = new GameObject("CajaFuerteSeguridad");
        safe.transform.position = new Vector3(-5.5f, 9.2f, 0f);
        safe.transform.SetParent(caseta.transform);
        SpriteRenderer sSr = safe.AddComponent<SpriteRenderer>();
        sSr.sprite = spriteCajaFuerteAcorazada != null ? spriteCajaFuerteAcorazada : crateSprite;
        sSr.color = Color.white;
        sSr.sortingOrder = 45;
        safe.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        BoxCollider2D safeCol = safe.AddComponent<BoxCollider2D>();
        safeCol.size = new Vector2(1.5f, 1.2f);
        safeCol.offset = new Vector2(0f, -0.2f);
        safeCol.isTrigger = false;
        safe.AddComponent<KeypadCajaFuerteNivel3>();

        // Casilleros en las esquinas limpias: sin empalmarse con la caja ni bloquear el paso
        CrearEscondite(caseta, "Taquilla_Caseta", new Vector2(-11.5f, 9.5f), "Casillero Metálico de Vigilancia", spriteCasilleroMetalico, new Vector2(1.8f, 2.8f));
        CrearEscondite(caseta, "Taquilla_Caseta_Der", new Vector2(12.5f, 9.5f), "Casillero de Archivos de Guardia", spriteCasilleroMetalico, new Vector2(1.8f, 2.8f));

        // Puerta de Escape Trasera
        GameObject escapeDoor = new GameObject("PuertaEscapeNivel3");
        escapeDoor.transform.position = new Vector3(0f, 11.5f, 0f);
        escapeDoor.transform.SetParent(caseta.transform);
        SpriteRenderer dSr = escapeDoor.AddComponent<SpriteRenderer>();
        dSr.sprite = doorSprite;
        dSr.sortingOrder = 60;
        escapeDoor.transform.localScale = new Vector3(2.4f, 2.6f, 1f);

        // Luz de alarma roja sobre la puerta
        GameObject luzAlarma = new GameObject("LuzAlarmaPuerta");
        luzAlarma.transform.position = new Vector3(0f, 11.7f, 0f);
        luzAlarma.transform.SetParent(escapeDoor.transform);
        Light2D la = luzAlarma.AddComponent<Light2D>();
        la.lightType = Light2D.LightType.Point;
        la.color = Color.red;
        la.intensity = 1.3f;
        la.pointLightOuterRadius = 4.0f;
    }

    private void ConstruirSalaQuirurgica(GameObject root)
    {
        GameObject sala = new GameObject("SalaQuirurgicaCentral");
        sala.transform.SetParent(root.transform);

        // Lámpara Quirúrgica Central de Techo
        GameObject lamparaProp = new GameObject("LamparaCirugia_Prop");
        lamparaProp.transform.position = new Vector3(0f, -0.2f, 0f);
        lamparaProp.transform.SetParent(sala.transform);
        SpriteRenderer lSr = lamparaProp.AddComponent<SpriteRenderer>();
        lSr.sprite = spriteLamparaCirugia;
        lSr.sortingOrder = 250;
        lamparaProp.transform.localScale = new Vector3(2.4f, 2.4f, 1f);

        // Luz Quirúrgica Suave y Fría (Sin saturación blanca cegadora)
        GameObject luzQ = new GameObject("ReflectorQuirurgico");
        luzQ.transform.position = new Vector3(0f, -1.8f, 0f);
        luzQ.transform.SetParent(sala.transform);
        Light2D lq = luzQ.AddComponent<Light2D>();
        lq.lightType = Light2D.LightType.Point;
        lq.color = new Color(0.85f, 0.92f, 0.98f);
        lq.intensity = 1.15f;
        lq.pointLightOuterRadius = 7.5f;
        lq.pointLightInnerRadius = 0.5f;
        luzQ.AddComponent<LuzParpadeanteNivel3>().frecuencia = 5f;

        // Camilla 1 (Izquierda: Gemela 1 acostada al inicio) - Corredor central súper amplio (7.6m de paso libre)
        GameObject c1 = new GameObject("Camilla1_GemelaAcecho");
        c1.transform.position = new Vector3(-3.8f, -2.0f, 0f);
        c1.transform.SetParent(sala.transform);
        SpriteRenderer c1Sr = c1.AddComponent<SpriteRenderer>();
        c1Sr.sprite = spriteMomiaAcostada1;
        c1Sr.sortingOrder = 40;
        c1.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
        BoxCollider2D col1 = c1.AddComponent<BoxCollider2D>();
        col1.size = new Vector2(1.6f, 1.8f);
        col1.offset = new Vector2(0f, -0.4f);
        col1.isTrigger = false;

        // Camilla 2 (Derecha: Gemela 2 acostada con etiqueta forense -> Cifra 7)
        GameObject c2 = new GameObject("Camilla2_GemelaCiega_Pista1");
        c2.transform.position = new Vector3(3.8f, -2.0f, 0f);
        c2.transform.SetParent(sala.transform);
        SpriteRenderer c2Sr = c2.AddComponent<SpriteRenderer>();
        c2Sr.sprite = spriteMomiaAcostada2;
        c2Sr.sortingOrder = 40;
        c2.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
        BoxCollider2D col2 = c2.AddComponent<BoxCollider2D>();
        col2.size = new Vector2(1.6f, 1.8f);
        col2.offset = new Vector2(0f, -0.4f);
        col2.isTrigger = false;

        PistasTallerNivel3 pista1 = c2.AddComponent<PistasTallerNivel3>();
        pista1.tipoPista = PistasTallerNivel3.TipoPista.CamillaCentral;

        // Prop visual de la Etiqueta de Morgue / Sujeto A en el tobillo
        GameObject tagProp = new GameObject("EtiquetaToeTag_Visual");
        tagProp.transform.SetParent(c2.transform);
        tagProp.transform.localPosition = new Vector3(0.35f, -0.95f, 0f);
        tagProp.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        SpriteRenderer tagSr = tagProp.AddComponent<SpriteRenderer>();
        tagSr.sprite = spriteEtiquetaForense;
        tagSr.sortingOrder = 55;

        // Escondites en Sala Central: Biombo Quirúrgico y Bajo Camilla con Sábana (pegada al muro sur)
        CrearEscondite(sala, "Armario_Quirofano", new Vector2(-6.0f, -0.2f), "Detrás del Biombo Quirúrgico Plegable", spriteBiomboQuirurgico, new Vector2(2.4f, 2.5f));
        CrearEscondite(sala, "Camilla_Sabana_Colgante", new Vector2(0f, -7.5f), "Bajo Camilla con Sábana Colgante", spriteCamillaConSabana, new Vector2(2.2f, 3.2f));

        // Huellas de arrastre sangriento saliendo de las camillas quirúrgicas hacia el pasillo sur
        CrearDecalVisual(sala, "ArrastreSangre_Gemela1", new Vector2(-3.5f, -4.5f), new Vector2(3.0f, 3.6f), spriteArrastreSangre);
        CrearDecalVisual(sala, "ArrastreSangre_Gemela2", new Vector2(3.5f, -4.5f), new Vector2(3.0f, 3.6f), spriteArrastreSangre);

        // Controlador de Despertar Secuencial
        ControladorDespertarMomiasNivel3 ctrl = sala.AddComponent<ControladorDespertarMomiasNivel3>();
        ctrl.camilla1Obj = c1;
        ctrl.camilla2Obj = c2;
        ctrl.spriteMomia1Acostada = spriteMomiaAcostada1;
        ctrl.spriteMomia2Acostada = spriteMomiaAcostada2;
        ctrl.spriteMomia2CabezaGirada = spriteMomiaCabezaGirada;
        ctrl.spriteCamillaVacia = spriteCamillaVaciaVendas;
    }

    private void ConstruirLaboratorio(GameObject root)
    {
        GameObject lab = new GameObject("LaboratorioQuimicoOeste");
        lab.transform.SetParent(root.transform);

        // Luz de reactivos químicos verde esmeralda
        GameObject luzL = new GameObject("LuzReactivosLab");
        luzL.transform.position = new Vector3(-14.5f, -1.8f, 0f);
        luzL.transform.SetParent(lab.transform);
        Light2D ll = luzL.AddComponent<Light2D>();
        ll.lightType = Light2D.LightType.Point;
        ll.color = new Color(0.20f, 0.95f, 0.50f);
        ll.intensity = 1.15f;
        ll.pointLightOuterRadius = 8.0f;
        luzL.AddComponent<LuzParpadeanteNivel3>().frecuencia = 4f;

        // Pizarra de fórmulas químicas en la pared norte del lab
        GameObject pizarra = new GameObject("PizarraLab_Prop");
        pizarra.transform.position = new Vector3(-14.5f, 2.8f, 0f);
        pizarra.transform.SetParent(lab.transform);
        SpriteRenderer pSr = pizarra.AddComponent<SpriteRenderer>();
        pSr.sprite = spritePizarraLab;
        pSr.sortingOrder = 30;
        pizarra.transform.localScale = new Vector3(3.4f, 2.0f, 1f);

        // Mesón de Mezcla con reactivos (Pista 2: Cifra 3)
        // Ubicado limpio y centrado bajo la pizarra sin empalmarse con armarios
        GameObject meson = new GameObject("MesonQuimicos_Pista2");
        meson.transform.position = new Vector3(-14.5f, 0.4f, 0f);
        meson.transform.SetParent(lab.transform);
        SpriteRenderer mSr = meson.AddComponent<SpriteRenderer>();
        mSr.sprite = spriteEstanteOrganos;
        mSr.sortingOrder = 40;
        meson.transform.localScale = new Vector3(3.4f, 2.2f, 1f);
        BoxCollider2D mCol = meson.AddComponent<BoxCollider2D>();
        mCol.size = new Vector2(3.0f, 1.6f);
        mCol.offset = new Vector2(0f, -0.2f);
        mCol.isTrigger = false;

        PistasTallerNivel3 pista2 = meson.AddComponent<PistasTallerNivel3>();
        pista2.tipoPista = PistasTallerNivel3.TipoPista.MesonQuimicos;

        // Escondites en Laboratorio: Separados en paredes opuestas sin amontonarse
        // Armario en la pared oeste extrema (X: -20f, Y: 1.8f) - sin nada enfrente
        CrearEscondite(lab, "Armario_Reactivos", new Vector2(-20f, 1.8f), "Gabinete Metálico de Reactivos", spriteCasilleroMetalico, new Vector2(1.8f, 2.8f));
        // Baúl en la zona sur (X: -10.5f, Y: -4.5f)
        CrearEscondite(lab, "Baul_Reactivos", new Vector2(-10.5f, -4.5f), "Baúl Antiguo de Resinas y Formol", spriteBaulAntiguo, new Vector2(2.2f, 1.8f));

        // Trampas en el suelo: Charcos de reactivos y esquirlas de vidrio bien distribuidas
        CrearDecalVisual(lab, "CharcoReactivo_1", new Vector2(-18.5f, -4.0f), new Vector2(4.5f, 2.8f), spriteCharcoReactivo);
        CrearDecalVisual(lab, "CharcoReactivo_2", new Vector2(-14.5f, -6.0f), new Vector2(3.8f, 2.4f), spriteCharcoReactivo);
        CrearDecalVisual(lab, "CristalesRotos_1", new Vector2(-10.5f, 1.0f), new Vector2(4.2f, 3.2f), spriteCristalesRotos);
    }

    private void ConstruirMorgueFria(GameObject root)
    {
        GameObject morgue = new GameObject("MorgueFriaEste");
        morgue.transform.SetParent(root.transform);

        // Luz fría azulada de congeladores
        GameObject luzM = new GameObject("LuzMorgueFria");
        luzM.transform.position = new Vector3(14.5f, -1.8f, 0f);
        luzM.transform.SetParent(morgue.transform);
        Light2D lm = luzM.AddComponent<Light2D>();
        lm.lightType = Light2D.LightType.Point;
        lm.color = new Color(0.35f, 0.78f, 1.0f);
        lm.intensity = 1.15f;
        lm.pointLightOuterRadius = 8.5f;
        luzM.AddComponent<LuzParpadeanteNivel3>().frecuencia = 6f;

        // Batería de Gavetas Metálicas en la pared Este (Pista 3: Cifra 9)
        GameObject gaveta = new GameObject("GavetaMorgue_Pista3");
        gaveta.transform.position = new Vector3(20f, -2.5f, 0f);
        gaveta.transform.SetParent(morgue.transform);
        SpriteRenderer gSr = gaveta.AddComponent<SpriteRenderer>();
        gSr.sprite = spriteBateriaMorgue;
        gSr.sortingOrder = 40;
        gaveta.transform.localScale = new Vector3(3.2f, 4.4f, 1f);
        BoxCollider2D gCol = gaveta.AddComponent<BoxCollider2D>();
        gCol.size = new Vector2(2.6f, 3.8f);
        gCol.offset = new Vector2(0f, -0.2f);
        gCol.isTrigger = false;

        PistasTallerNivel3 pista3 = gaveta.AddComponent<PistasTallerNivel3>();
        pista3.tipoPista = PistasTallerNivel3.TipoPista.GavetaMorgue;

        // Prop visual del Termómetro/Manómetro roto de la cámara fría
        GameObject termoProp = new GameObject("TermometroMorgue_Visual");
        termoProp.transform.SetParent(gaveta.transform);
        termoProp.transform.localPosition = new Vector3(-0.45f, 0.40f, 0f);
        termoProp.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        SpriteRenderer tSr = termoProp.AddComponent<SpriteRenderer>();
        tSr.sprite = spriteTermometroMorgue;
        tSr.sortingOrder = 55;

        // Escondites en Morgue: Separados y sin amontonamiento
        // Casillero en esquina noreste limpia (Y: 2.8f) separado por 5.3m de la gaveta mortuoria (Y: -2.5f)
        CrearEscondite(morgue, "Armario_Morgue", new Vector2(20f, 2.8f), "Casillero Metálico de Forenses", spriteCasilleroMetalico, new Vector2(1.8f, 2.8f));
        CrearEscondite(morgue, "Gaveta_Abierta_Escondite", new Vector2(11.5f, 2.2f), "Gaveta de Morgue Refrigerada Abierta", spriteGavetaMorgueAbierta, new Vector2(2.2f, 3.0f));

        // Vapores de nitrógeno / niebla escarchada en el suelo
        CrearDecalVisual(morgue, "NieblaFria_1", new Vector2(14.5f, -2.5f), new Vector2(6f, 3.5f), spriteNieblaHelada);
        CrearDecalVisual(morgue, "NieblaFria_2", new Vector2(17f, -5.5f), new Vector2(5.5f, 3f), spriteNieblaHelada);
    }

    private void CrearEscondite(GameObject padre, string nombre, Vector2 pos, string etiqueta, Sprite sp, Vector2 escala)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp != null ? sp : (crateSprite != null ? crateSprite : blankSprite);
        sr.color = Color.white;
        go.transform.localScale = new Vector3(escala.x, escala.y, 1f);
        sr.sortingOrder = 35;

        // Colisionador físico sólido en la base del mueble para que el jugador NO pueda pararse encima de él y deba rodearlo
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.3f, 1.1f);
        col.offset = new Vector2(0f, -0.45f);
        col.isTrigger = false;

        EsconditeNivel3 esc = go.AddComponent<EsconditeNivel3>();
        esc.nombreEscondite = etiqueta;
        esc.distanciaInteraccion = 2.4f;
    }

    private void CrearDecalVisual(GameObject padre, string nombre, Vector2 pos, Vector2 escala, Sprite sp)
    {
        if (sp == null) return;
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = -5;
        go.transform.localScale = escala;
    }

    private void ConfigurarJugadorYEnemigos(GameObject root)
    {
        // 1. Ubicar Vigilante (Garantizar estrictamente UNA única vigilante)
        GameObject[] todosPlayers = GameObject.FindGameObjectsWithTag("Player");
        GameObject player = null;
        if (todosPlayers != null && todosPlayers.Length > 0)
        {
            player = todosPlayers[0];
            for (int i = 1; i < todosPlayers.Length; i++)
            {
                if (todosPlayers[i] != null && todosPlayers[i] != player)
                {
                    DestroyImmediate(todosPlayers[i]);
                }
            }
        }
        else
        {
            player = GameObject.Find("Vigilante");
        }

        if (player == null)
        {
            player = new GameObject("Vigilante");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 7.0f, 0f);
            player.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = player.GetComponent<BoxCollider2D>();
            if (col == null) col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.45f, 0.35f);
            col.offset = new Vector2(0f, -0.35f);

            player.AddComponent<PlayerController>();
        }
        else
        {
            player.transform.position = new Vector3(0f, 7.0f, 0f);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = player.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            BoxCollider2D col = player.GetComponent<BoxCollider2D>();
            if (col == null) col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.45f, 0.35f);
            col.offset = new Vector2(0f, -0.35f);
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.museoMinX = -22f;
            pc.museoMaxX = 22f;
            pc.museoMinY = -13.5f;
            pc.museoMaxY = 11.0f;
            pc.velocidadCaminar = 10.5f;
            pc.velocidadCorrer = 15.0f;
            pc.vidaMaxima = 150f;
            pc.vidaActual = 150f;
        }

        // Conectar cámara
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf != null) cf.objetivo = player.transform;
        }

        // Linterna halógena de seguridad de la Vigilante para atmósfera inmersiva de terror
        Transform linternaExistente = player.transform.Find("LinternaVigilante");
        if (linternaExistente == null)
        {
            GameObject linternaObj = new GameObject("LinternaVigilante");
            linternaObj.transform.SetParent(player.transform, false);
            linternaObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            Light2D fl = linternaObj.AddComponent<Light2D>();
            fl.lightType = Light2D.LightType.Point;
            fl.color = new Color(1.0f, 0.96f, 0.88f, 1f);
            fl.intensity = 1.35f;
            fl.pointLightOuterRadius = 7.0f;
            fl.pointLightInnerRadius = 0.8f;
        }

        // 2. Destruir SIEMPRE cualquier momia previa residual para crear instancias limpias garantizadas
        MomiaAcechadoraNivel3[] todasAcechadoras = Object.FindObjectsByType<MomiaAcechadoraNivel3>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < todasAcechadoras.Length; i++)
        {
            if (todasAcechadoras[i] != null) DestroyImmediate(todasAcechadoras[i].gameObject);
        }

        MomiaCiegaNivel3[] todasCiegas = Object.FindObjectsByType<MomiaCiegaNivel3>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < todasCiegas.Length; i++)
        {
            if (todasCiegas[i] != null) DestroyImmediate(todasCiegas[i].gameObject);
        }

        GameObject g1 = new GameObject("Gemela_LaQueAcecha");
        g1.transform.position = new Vector3(-2.8f, -0.5f, 0f); // Junto a su camilla quirúrgica en Quirófano
        g1.transform.SetParent(root.transform);
        g1.AddComponent<MomiaAcechadoraNivel3>();

        GameObject g2 = new GameObject("Gemela_LaCiega");
        g2.transform.position = new Vector3(2.8f, -0.5f, 0f); // Junto a su camilla quirúrgica en Quirófano
        g2.transform.SetParent(root.transform);
        g2.AddComponent<MomiaCiegaNivel3>();

        // Asegurar iluminación propia espectral y presencia visual para ambas momias
        g1.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
        SpriteRenderer srG1 = g1.GetComponent<SpriteRenderer>();
        if (srG1 != null) { srG1.enabled = true; srG1.sortingOrder = 350; srG1.sprite = HojasSpritesNivel3.MomiaFrame(0, 0); }
        if (g1.GetComponentInChildren<Light2D>() == null)
        {
            GameObject l1 = new GameObject("AuraMomiaAcechadora");
            l1.transform.SetParent(g1.transform, false);
            l1.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            Light2D l1Light = l1.AddComponent<Light2D>();
            l1Light.lightType = Light2D.LightType.Point;
            l1Light.color = new Color(1.0f, 0.22f, 0.15f, 1f);
            l1Light.intensity = 1.5f;
            l1Light.pointLightOuterRadius = 4.5f;
        }

        g2.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
        SpriteRenderer srG2 = g2.GetComponent<SpriteRenderer>();
        if (srG2 != null) { srG2.enabled = true; srG2.sortingOrder = 350; srG2.sprite = HojasSpritesNivel3.MomiaFrame(0, 0); }
        if (g2.GetComponentInChildren<Light2D>() == null)
        {
            GameObject l2 = new GameObject("AuraMomiaCiega");
            l2.transform.SetParent(g2.transform, false);
            l2.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            Light2D l2Light = l2.AddComponent<Light2D>();
            l2Light.lightType = Light2D.LightType.Point;
            l2Light.color = new Color(0.20f, 0.85f, 1.0f, 1f);
            l2Light.intensity = 1.5f;
            l2Light.pointLightOuterRadius = 4.5f;
        }

        // Conectar referencias en el controlador de despertar
        ControladorDespertarMomiasNivel3 ctrl = root.GetComponentInChildren<ControladorDespertarMomiasNivel3>();
        if (ctrl != null)
        {
            ctrl.gemelaAcechadora = g1.GetComponent<MomiaAcechadoraNivel3>();
            ctrl.gemelaCiega = g2.GetComponent<MomiaCiegaNivel3>();
            ctrl.InicializarCamillas();
        }

        // 4. Escorpiones distribuidos tácticamente en salas (Quirófano, Lab y Morgue), no agolpados en la caseta
        Vector2[] posEsc = new Vector2[]
        {
            new Vector2(-3.5f, -3.5f),  // Quirófano sur
            new Vector2(7.0f, 2.0f),    // Pasillo Este hacia Morgue
            new Vector2(-7.0f, 2.0f),   // Pasillo Oeste hacia Laboratorio
            new Vector2(13.0f, -5.0f),  // Morgue Fría
            new Vector2(-13.0f, -5.0f)  // Laboratorio Químico
        };

        for (int i = 0; i < posEsc.Length; i++)
        {
            string escName = "Escorpion_Patrulla_" + i;
            if (GameObject.Find(escName) == null)
            {
                GameObject esc = new GameObject(escName);
                esc.transform.position = posEsc[i];
                esc.transform.SetParent(root.transform);
                esc.AddComponent<EscorpionNivel3>();
            }
        }
    }

    // =========================================================================
    // GENERADORES DE TEXTURAS Y SPRITES PROCEDURALES DE ALTO TERROR Y REALISMO
    // =========================================================================

    private Sprite CrearSpriteFullRect(Texture2D tex, float ppu)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
    }

    private Sprite GenerarPisoBaldosasQuirurgicas()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color cTile = new Color(0.38f, 0.44f, 0.42f);
        Color cJunta = new Color(0.10f, 0.12f, 0.14f);
        Color cSangreSeca = new Color(0.28f, 0.05f, 0.05f, 0.92f);
        Color cBrillo = new Color(0.52f, 0.58f, 0.55f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int tx = x % 32;
                int ty = y % 32;
                bool esJunta = (tx <= 1 || ty <= 1);

                if (esJunta)
                {
                    cols[y * size + x] = cJunta;
                }
                else
                {
                    float ruido = Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.08f - 0.04f;
                    Color baseCol = new Color(cTile.r + ruido, cTile.g + ruido, cTile.b + ruido, 1f);

                    if (tx == 2 || ty == 31) baseCol = Color.Lerp(baseCol, cBrillo, 0.35f);
                    if (tx == 31 || ty == 2) baseCol = Color.Lerp(baseCol, cJunta, 0.45f);

                    // Baldosa fisurada / fracturada en zonas estratégicas
                    bool grieta1 = (x > 18 && x < 30 && Mathf.Abs(y - (x + 10)) <= 1);
                    bool grieta2 = (x > 75 && x < 90 && Mathf.Abs(y - (120 - x * 0.5f)) <= 1);
                    if (grieta1 || grieta2) baseCol = cJunta;

                    // Salpicaduras de sangre y fluidos biológicos secos
                    float dSangre1 = Vector2.Distance(new Vector2(x, y), new Vector2(48f, 44f));
                    float dSangre2 = Vector2.Distance(new Vector2(x, y), new Vector2(105f, 85f));
                    if (dSangre1 < 14f && Mathf.PerlinNoise(x * 0.3f, y * 0.3f) > 0.35f)
                    {
                        baseCol = Color.Lerp(baseCol, cSangreSeca, 0.85f);
                    }
                    else if (dSangre2 < 9f && Mathf.PerlinNoise(x * 0.4f, y * 0.4f) > 0.4f)
                    {
                        baseCol = Color.Lerp(baseCol, cSangreSeca, 0.75f);
                    }

                    cols[y * size + x] = baseCol;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarPisoLaboratorio()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color cBase = new Color(0.17f, 0.24f, 0.20f);
        Color cCorrosion = new Color(0.10f, 0.16f, 0.12f);
        Color cAcidoVerde = new Color(0.18f, 0.42f, 0.22f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float mancha = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                float oxidacion = Mathf.PerlinNoise(x * 0.22f, y * 0.22f);
                Color c = Color.Lerp(cBase, cCorrosion, mancha);

                // Marcas circulares concéntricas de reactivos y quemaduras químicas
                float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(50f, 60f));
                if (Mathf.Abs(d1 - 18f) <= 2.2f && oxidacion > 0.3f)
                {
                    c = Color.Lerp(c, cAcidoVerde, 0.75f);
                }

                float d2 = Vector2.Distance(new Vector2(x, y), new Vector2(95f, 35f));
                if (Mathf.Abs(d2 - 12f) <= 1.8f)
                {
                    c = Color.Lerp(c, cCorrosion, 0.8f);
                }

                cols[y * size + x] = c;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarPisoMorgueAcero()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color cPlaca = new Color(0.24f, 0.28f, 0.35f);
        Color cEscarcha = new Color(0.62f, 0.80f, 0.95f);
        Color cRemache = new Color(0.12f, 0.15f, 0.20f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int px = x % 16;
                int py = y % 16;
                bool diamante = (Mathf.Abs(px - 8) + Mathf.Abs(py - 8) <= 4);

                Color col = diamante ? cPlaca * 1.25f : cPlaca;

                bool remacheBorde = (x % 64 <= 2 || y % 64 <= 2);
                if (remacheBorde) col = cRemache;

                float esc = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                if (esc > 0.65f)
                {
                    float factor = (esc - 0.65f) / 0.35f;
                    col = Color.Lerp(col, cEscarcha, factor * 0.65f);
                }

                cols[y * size + x] = col;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarPisoMaderaCaseta()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color cMadera = new Color(0.24f, 0.19f, 0.15f);
        Color cClavo = new Color(0.08f, 0.08f, 0.09f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int py = y % 20;
                bool junta = (py <= 1);

                if (junta)
                {
                    cols[y * size + x] = cMadera * 0.55f;
                }
                else
                {
                    float veta = Mathf.PerlinNoise(x * 0.35f, y * 0.04f) * 0.12f - 0.06f;
                    Color c = new Color(cMadera.r + veta, cMadera.g + veta, cMadera.b + veta, 1f);

                    bool clavo = (x % 48 == 6 || x % 48 == 42) && (py == 5 || py == 15);
                    if (clavo) c = cClavo;

                    cols[y * size + x] = c;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteMuroTaller()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color cMuroBase = new Color(0.16f, 0.18f, 0.20f);
        Color cMortero = new Color(0.09f, 0.10f, 0.12f);
        Color cFranjaAmarilla = new Color(0.65f, 0.55f, 0.15f, 0.85f);
        Color cHumedad = new Color(0.08f, 0.10f, 0.11f);
        Color cSangre = new Color(0.35f, 0.06f, 0.06f, 0.90f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int fila = y / 32;
                int ox = (fila % 2 == 0) ? 0 : 32;
                int bx = (x + ox) % 64;
                int by = y % 32;

                bool junta = (bx <= 2 || by <= 2);
                Color c = junta ? cMortero : cMuroBase;

                float ruido = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.06f - 0.03f;
                c = new Color(c.r + ruido, c.g + ruido, c.b + ruido, 1f);

                float humedadGota = Mathf.PerlinNoise(x * 0.3f, y * 0.05f);
                if (humedadGota > 0.65f && y < 100) c = Color.Lerp(c, cHumedad, 0.6f);

                if (y >= 106 && y <= 118)
                {
                    bool franja = ((x + y) / 12) % 2 == 0;
                    if (franja) c = Color.Lerp(c, cFranjaAmarilla, 0.65f);
                    else c = Color.Lerp(c, cMortero, 0.75f);
                }

                bool manchaSangre = (x >= 45 && x <= 58 && y >= 35 && y <= 65 && Mathf.PerlinNoise(x * 0.4f, y * 0.2f) > 0.45f);
                if (manchaSangre) c = Color.Lerp(c, cSangre, 0.85f);

                cols[y * size + x] = c;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteCamillaBase()
    {
        int w = 64, h = 96;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cBorde = new Color(0.20f, 0.22f, 0.25f);
        Color cMetal = new Color(0.50f, 0.54f, 0.60f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x < 3 || x >= w - 3 || y < 3 || y >= h - 3) cols[y * w + x] = cBorde;
                else cols[y * w + x] = cMetal;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteMomiaAcostada(int variante)
    {
        int w = 64, h = 100;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cBordeCamilla = new Color(0.18f, 0.20f, 0.24f);
        Color cMetalInox = new Color(0.48f, 0.52f, 0.58f);
        Color cSabanaSucia = new Color(0.78f, 0.74f, 0.65f);
        Color cVendasOcre = new Color(0.62f, 0.54f, 0.40f);
        Color cPielMomia = new Color(0.45f, 0.36f, 0.24f);
        Color cCuencasNegras = new Color(0.04f, 0.03f, 0.03f);
        Color cManchaCadaverica = new Color(0.28f, 0.08f, 0.08f, 0.88f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // 1. Marco metálico exterior de la camilla
                if (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3)
                {
                    cols[y * w + x] = cBordeCamilla;
                    continue;
                }

                // Desagüe de autopsia en cabecera
                if (Vector2.Distance(new Vector2(x, y), new Vector2(32f, 6f)) <= 2.5f)
                {
                    cols[y * w + x] = cBordeCamilla;
                    continue;
                }

                // 2. Silueta anatómica del cuerpo momificado tendido
                bool enCuerpo = (x >= 15 && x <= w - 16 && y >= 10 && y <= h - 10);
                if (enCuerpo)
                {
                    // Cabeza momificada con vendajes sueltos y calavera reseca
                    if (y >= h - 26)
                    {
                        bool esCentroCara = (x >= 24 && x <= 39 && y >= h - 22);
                        if (esCentroCara)
                        {
                            // Cuencas oculares vacías
                            bool ojo1 = (x >= 27 && x <= 30 && y >= h - 17 && y <= h - 14);
                            bool ojo2 = (x >= 33 && x <= 36 && y >= h - 17 && y <= h - 14);
                            bool boca = (x >= 29 && x <= 34 && y >= h - 21 && y <= h - 20);

                            if (ojo1 || ojo2 || boca) cols[y * w + x] = cCuencasNegras;
                            else cols[y * w + x] = cPielMomia;
                        }
                        else
                        {
                            cols[y * w + x] = cVendasOcre;
                        }
                    }
                    // Pecho y manos momificadas cruzadas
                    else if (y >= 46 && y <= 62 && x >= 24 && x <= 39)
                    {
                        bool dedos = (y >= 50 && y <= 56 && (x % 3 == 0));
                        cols[y * w + x] = dedos ? cPielMomia : cVendasOcre;
                    }
                    // Sábana mortuoria con manchas cadavéricas
                    else
                    {
                        bool pliegue = (y % 6 == 0 || Mathf.Abs(x - 32) <= 1);
                        Color colS = pliegue ? cVendasOcre : cSabanaSucia;

                        // Manchas de descomposición y formol
                        if (x >= 22 && x <= 42 && y >= 30 && y <= 50 && Mathf.PerlinNoise(x * 0.2f, y * 0.2f) > 0.4f)
                        {
                            colS = Color.Lerp(colS, cManchaCadaverica, 0.70f);
                        }

                        // Etiqueta forense atada al pie para la Gemela 2
                        if (variante == 2 && x >= 36 && x <= 45 && y >= 12 && y <= 18)
                        {
                            colS = new Color(0.88f, 0.82f, 0.60f); // Etiqueta forense
                        }

                        cols[y * w + x] = colS;
                    }
                }
                else
                {
                    // Plancha de acero inoxidable de la camilla con brillo
                    float brilloX = (x >= 6 && x <= 9) ? 0.08f : 0f;
                    cols[y * w + x] = new Color(cMetalInox.r + brilloX, cMetalInox.g + brilloX, cMetalInox.b + brilloX, 1f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteMomiaCabezaGirada()
    {
        int w = 64, h = 100;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cBordeCamilla = new Color(0.18f, 0.20f, 0.24f);
        Color cMetalInox = new Color(0.48f, 0.52f, 0.58f);
        Color cSabanaSucia = new Color(0.78f, 0.74f, 0.65f);
        Color cVendasOcre = new Color(0.62f, 0.54f, 0.40f);
        Color cPielMomia = new Color(0.45f, 0.36f, 0.24f);
        Color cCuencasNegras = new Color(0.02f, 0.02f, 0.02f);
        Color cPupilaEspectral = new Color(0.95f, 0.98f, 1.0f); // Ojos blancos que miran de frente

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3)
                {
                    cols[y * w + x] = cBordeCamilla;
                    continue;
                }

                bool enCuerpo = (x >= 15 && x <= w - 16 && y >= 10 && y <= h - 10);
                if (enCuerpo)
                {
                    // Cabeza girada 90° mirando directamente al jugador
                    if (y >= h - 26)
                    {
                        bool ojo1 = (x >= 26 && x <= 29 && y >= h - 17 && y <= h - 14);
                        bool pupila1 = (x == 27 && y == h - 15);

                        bool ojo2 = (x >= 34 && x <= 37 && y >= h - 17 && y <= h - 14);
                        bool pupila2 = (x == 35 && y == h - 15);

                        bool bocaAbierta = (x >= 28 && x <= 35 && y >= h - 22 && y <= h - 20);

                        if (pupila1 || pupila2) cols[y * w + x] = cPupilaEspectral;
                        else if (ojo1 || ojo2 || bocaAbierta) cols[y * w + x] = cCuencasNegras;
                        else cols[y * w + x] = cPielMomia;
                    }
                    else
                    {
                        bool pliegue = (y % 6 == 0);
                        cols[y * w + x] = pliegue ? cVendasOcre : cSabanaSucia;
                    }
                }
                else
                {
                    cols[y * w + x] = cMetalInox;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteCamillaVacia()
    {
        int w = 64, h = 100;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cBordeCamilla = new Color(0.18f, 0.20f, 0.24f);
        Color cMetalInox = new Color(0.48f, 0.52f, 0.58f);
        Color cSabanaRevuelta = new Color(0.75f, 0.72f, 0.65f);
        Color cSangreArrastre = new Color(0.38f, 0.05f, 0.05f, 0.95f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3)
                {
                    cols[y * w + x] = cBordeCamilla;
                    continue;
                }

                // Sábana revuelta caída a un costado
                bool sabanaCaida = (x >= 8 && x <= 28 && y >= 15 && y <= 75 && Mathf.PerlinNoise(x * 0.15f, y * 0.15f) > 0.35f);
                // Huellas de garras y arrastre ensangrentado donde bajó la momia
                bool arrastre = (x >= 32 && x <= 46 && y >= 10 && y <= 55 && Mathf.Abs(x - 38) <= 5);

                if (arrastre)
                {
                    cols[y * w + x] = Color.Lerp(cMetalInox, cSangreArrastre, 0.85f);
                }
                else if (sabanaCaida)
                {
                    cols[y * w + x] = cSabanaRevuelta;
                }
                else
                {
                    cols[y * w + x] = cMetalInox;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteArrastreSangre()
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cSangreOscura = new Color(0.32f, 0.04f, 0.04f, 0.88f);
        Color cHuellaDedos = new Color(0.20f, 0.02f, 0.02f, 0.95f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Rastro de cuerpo arrastrado vertical
                float dx = Mathf.Abs(x - 32) / 16f;
                float ruido = Mathf.PerlinNoise(x * 0.25f, y * 0.1f);

                if (dx < 1.0f && ruido > 0.40f)
                {
                    float alfa = (1.0f - dx) * 0.85f;
                    // Arañazos de uñas
                    bool garra = (x % 5 == 0 && ruido > 0.55f);
                    cols[y * w + x] = garra ? cHuellaDedos : new Color(cSangreOscura.r, cSangreOscura.g, cSangreOscura.b, alfa);
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpritePizarraQuimica()
    {
        int w = 128, h = 72;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cMarco = new Color(0.28f, 0.18f, 0.11f);
        Color cPizarra = new Color(0.08f, 0.14f, 0.11f);
        Color cTiza = new Color(0.92f, 0.94f, 0.90f, 0.95f);
        Color cSangreNota = new Color(0.65f, 0.08f, 0.08f, 0.95f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool marco = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
                if (marco)
                {
                    cols[y * w + x] = cMarco;
                }
                else
                {
                    // Trazos de fórmulas orgánicas y diagramas moleculares
                    bool formula = (y >= 44 && y <= 46 && x >= 15 && x <= 75) ||
                                   (y >= 32 && y <= 34 && x >= 20 && x <= 85) ||
                                   (y >= 20 && y <= 22 && x >= 15 && x <= 60);

                    // Mensaje desesperado en rojo "NO LAS MIRES DE CERCA"
                    bool notaTerror = (y >= 54 && y <= 56 && x >= 15 && x <= 110) ||
                                      (y >= 10 && y <= 12 && x >= 70 && x <= 115);

                    if (notaTerror) cols[y * w + x] = cSangreNota;
                    else if (formula) cols[y * w + x] = cTiza;
                    else
                    {
                        float polvo = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.06f;
                        cols[y * w + x] = new Color(cPizarra.r + polvo, cPizarra.g + polvo, cPizarra.b + polvo, 1f);
                    }
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 28f);
    }

    private Sprite GenerarSpriteEstanteFrascos()
    {
        int w = 128, h = 80;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cMesonMetal = new Color(0.35f, 0.38f, 0.42f);
        Color cBordeMeson = new Color(0.18f, 0.20f, 0.22f);
        Color cTapon = new Color(0.12f, 0.12f, 0.14f);
        Color cFormolVerde = new Color(0.15f, 0.75f, 0.45f, 0.78f);
        Color cOrgano = new Color(0.42f, 0.28f, 0.18f, 0.95f);
        Color cBrilloVidrio = new Color(0.85f, 0.98f, 0.92f, 0.85f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (y < 24)
                {
                    cols[y * w + x] = (y < 4 || x < 4 || x >= w - 4) ? cBordeMeson : cMesonMetal;
                }
                else
                {
                    // 3 Frascos con especímenes forenses en formol
                    bool f1 = (x >= 14 && x <= 38 && y >= 24 && y <= 72);
                    bool f2 = (x >= 52 && x <= 76 && y >= 24 && y <= 76);
                    bool f3 = (x >= 90 && x <= 114 && y >= 24 && y <= 70);

                    if (f1 || f2 || f3)
                    {
                        int relY = y - 24;
                        if (relY >= 44)
                        {
                            cols[y * w + x] = cTapon; // Tapón negro
                        }
                        else
                        {
                            // Brillo especular del vidrio en el borde izquierdo
                            int fx = f1 ? (x - 14) : (f2 ? (x - 52) : (x - 90));
                            if (fx == 2 || fx == 3)
                            {
                                cols[y * w + x] = cBrilloVidrio;
                            }
                            // Espécimen flotante en el centro
                            else if (fx >= 8 && fx <= 16 && relY >= 12 && relY <= 34)
                            {
                                cols[y * w + x] = cOrgano;
                            }
                            else
                            {
                                cols[y * w + x] = cFormolVerde;
                            }
                        }
                    }
                    else
                    {
                        cols[y * w + x] = Color.clear;
                    }
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 28f);
    }

    private Sprite GenerarSpriteBateriaGavetasMorgue()
    {
        int w = 112, h = 140;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cAceroCepillado = new Color(0.44f, 0.48f, 0.55f);
        Color cJuntaGaveta = new Color(0.12f, 0.14f, 0.18f);
        Color cPlacaBronce = new Color(0.78f, 0.68f, 0.32f);
        Color cTiradorCromo = new Color(0.88f, 0.92f, 0.96f);
        Color cEscarcha = new Color(0.70f, 0.88f, 0.98f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Rejilla de 2 columnas x 4 filas de compartimientos de morgue
                int colIdx = x / 56;
                int filIdx = y / 35;
                int gx = x % 56;
                int gy = y % 35;

                bool bordeCompartimiento = (gx <= 2 || gx >= 53 || gy <= 2 || gy >= 32);

                if (bordeCompartimiento)
                {
                    cols[y * w + x] = cJuntaGaveta;
                }
                else
                {
                    Color baseC = cAceroCepillado;

                    // Escarcha en las juntas y esquinas
                    if (gx <= 6 || gy <= 6) baseC = Color.Lerp(baseC, cEscarcha, 0.55f);

                    // Placa con número de compartimento en bronce
                    bool esPlaca = (gx >= 20 && gx <= 36 && gy >= 22 && gy <= 28);
                    // Manija cromada con cerradura
                    bool esManija = (gx >= 16 && gx <= 40 && gy >= 10 && gy <= 14);

                    if (esPlaca) cols[y * w + x] = cPlacaBronce;
                    else if (esManija) cols[y * w + x] = cTiradorCromo;
                    else cols[y * w + x] = baseC;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 28f);
    }

    private Sprite GenerarSpriteLamparaCirugia()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];
        Vector2 centro = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), centro);
                if (d <= 29f && d >= 24f) cols[y * size + x] = new Color(0.70f, 0.74f, 0.80f);
                else if (d < 24f && d >= 21f) cols[y * size + x] = new Color(0.20f, 0.22f, 0.26f);
                else if (d < 21f)
                {
                    // 4 Focos halógenos de alta intensidad quirúrgica
                    bool foco = (Mathf.Abs(x - 32) == 9 || Mathf.Abs(y - 32) == 9);
                    cols[y * size + x] = foco ? new Color(1f, 1f, 0.95f) : new Color(0.85f, 0.92f, 0.98f, 0.95f);
                }
                else cols[y * size + x] = Color.clear;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteConsolaCCTV()
    {
        int w = 96, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cMaderaEscritorio = new Color(0.24f, 0.16f, 0.10f);
        Color cBordeMadera = new Color(0.12f, 0.08f, 0.05f);
        Color cCarcasaMonitor = new Color(0.14f, 0.16f, 0.18f);
        Color cFosforoVerde = new Color(0.18f, 0.85f, 0.50f);
        Color cFosforoOscuro = new Color(0.04f, 0.14f, 0.08f);
        Color cPapelForense = new Color(0.82f, 0.78f, 0.65f);
        Color cSelloRojo = new Color(0.75f, 0.12f, 0.12f);
        Color cHuecoOscuro = new Color(0.02f, 0.02f, 0.03f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Parte Inferior: Hueco para esconderse debajo del escritorio
                if (y < 22)
                {
                    bool pataIzq = (x < 14);
                    bool pataDer = (x >= w - 14);
                    if (pataIzq || pataDer)
                    {
                        cols[y * w + x] = (x < 3 || x >= w - 3) ? cBordeMadera : cMaderaEscritorio;
                    }
                    else
                    {
                        // Sombra profunda bajo el escritorio donde el jugador se oculta
                        cols[y * w + x] = cHuecoOscuro;
                    }
                }
                // Parte Media: Superficie del escritorio con documentos de morgue
                else if (y < 34)
                {
                    bool bordeSup = (y == 33 || y == 22 || x < 3 || x >= w - 3);
                    // Carpeta de archivos forenses
                    bool carpeta = (x >= 40 && x <= 58 && y >= 24 && y <= 31);
                    bool sello = (carpeta && x >= 46 && x <= 52 && y >= 26 && y <= 29);

                    if (bordeSup) cols[y * w + x] = cBordeMadera;
                    else if (sello) cols[y * w + x] = cSelloRojo;
                    else if (carpeta) cols[y * w + x] = cPapelForense;
                    else
                    {
                        float veta = Mathf.Sin(x * 0.4f) * 0.04f;
                        cols[y * w + x] = new Color(cMaderaEscritorio.r + veta, cMaderaEscritorio.g + veta, cMaderaEscritorio.b + veta, 1f);
                    }
                }
                // Parte Superior: Monitores CRT gemelos de CCTV
                else
                {
                    // Monitor Izquierdo (Cam 01 - Quirófano) y Monitor Derecho (Cam 02 - Morgue)
                    bool mIzqCarcasa = (x >= 8 && x <= 46 && y >= 34 && y <= 60);
                    bool mDerCarcasa = (x >= 50 && x <= 88 && y >= 34 && y <= 60);

                    if (mIzqCarcasa || mDerCarcasa)
                    {
                        int relX = mIzqCarcasa ? (x - 8) : (x - 50);
                        int relY = y - 34;
                        bool marcoPantalla = (relX <= 3 || relX >= 35 || relY <= 3 || relY >= 23);

                        if (marcoPantalla)
                        {
                            cols[y * w + x] = cCarcasaMonitor;
                        }
                        else
                        {
                            // Pantalla CRT curva con scanlines de fósforo verde
                            bool scanline = (relY % 2 == 0);
                            float ruidoCCTV = Mathf.PerlinNoise(x * 0.6f, y * 0.6f + Time.realtimeSinceStartup);
                            Color cPantalla = scanline ? cFosforoVerde : cFosforoOscuro;

                            // LED rojo de grabación REC en monitor 1
                            if (mIzqCarcasa && relX >= 28 && relX <= 31 && relY >= 18 && relY <= 20)
                            {
                                cols[y * w + x] = Color.red;
                            }
                            else
                            {
                                cols[y * w + x] = Color.Lerp(cPantalla, cFosforoOscuro, ruidoCCTV * 0.35f);
                            }
                        }
                    }
                    else
                    {
                        cols[y * w + x] = Color.clear;
                    }
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 28f);
    }

    private Sprite GenerarSpriteCharcoReactivo()
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        Vector2 centro = new Vector2(w * 0.5f, h * 0.5f);

        Color cAcidoVerde = new Color(0.20f, 0.95f, 0.40f, 0.92f);
        Color cBordeCorroido = new Color(0.08f, 0.28f, 0.12f, 0.85f);
        Color cBurbuja = new Color(0.85f, 1.0f, 0.70f, 0.95f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - centro.x) / (w * 0.45f);
                float dy = (y - centro.y) / (h * 0.35f);
                float ruido = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.35f;
                float dist = (dx * dx + dy * dy) + ruido;

                if (dist < 1.0f)
                {
                    float alfa = Mathf.SmoothStep(1.0f, 0.0f, dist);
                    bool burbuja = (Mathf.Abs(x - 28) <= 2 && Mathf.Abs(y - 32) <= 2) ||
                                   (Mathf.Abs(x - 42) <= 1 && Mathf.Abs(y - 25) <= 1);

                    if (burbuja)
                    {
                        cols[y * w + x] = cBurbuja;
                    }
                    else if (dist > 0.75f)
                    {
                        cols[y * w + x] = Color.Lerp(cBordeCorroido, Color.clear, (dist - 0.75f) / 0.25f);
                    }
                    else
                    {
                        cols[y * w + x] = new Color(cAcidoVerde.r, cAcidoVerde.g, cAcidoVerde.b, alfa * 0.88f);
                    }
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteCristales()
    {
        int w = 48, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        Color cVidrioCian = new Color(0.75f, 0.95f, 1.0f, 0.90f);
        Color cBrilloBlanco = new Color(1.0f, 1.0f, 1.0f, 0.98f);
        Color cSangreCristal = new Color(0.55f, 0.05f, 0.05f, 0.85f);

        for (int i = 0; i < 45; i++)
        {
            int cx = Random.Range(6, w - 6);
            int cy = Random.Range(6, h - 6);
            bool tieneSangre = (i % 7 == 0);

            for (int ox = -2; ox <= 2; ox++)
            {
                for (int oy = -2; oy <= 2; oy++)
                {
                    if (Mathf.Abs(ox) + Mathf.Abs(oy) <= 2)
                    {
                        int px = Mathf.Clamp(cx + ox, 0, w - 1);
                        int py = Mathf.Clamp(cy + oy, 0, h - 1);
                        if (ox == 0 && oy == 0) cols[py * w + px] = cBrilloBlanco;
                        else if (tieneSangre) cols[py * w + px] = cSangreCristal;
                        else cols[py * w + px] = cVidrioCian;
                    }
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteNiebla()
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        Vector2 centro = new Vector2(w * 0.5f, h * 0.5f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - centro.x) / (w * 0.5f);
                float dy = (y - centro.y) / (h * 0.42f);
                float dist = dx * dx + dy * dy;

                if (dist < 1.0f)
                {
                    float alfa = Mathf.SmoothStep(1.0f, 0.0f, dist);
                    float turbulencia = Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.25f;
                    cols[y * w + x] = new Color(0.70f, 0.88f, 0.98f, (alfa + turbulencia) * 0.35f);
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteCajaFuerteAcorazada()
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        Vector2 centro = new Vector2(w * 0.5f, h * 0.5f);

        Color cAceroAcorazado = new Color(0.20f, 0.22f, 0.25f);
        Color cBordeExterior = new Color(0.10f, 0.12f, 0.14f);
        Color cPernoRemache = new Color(0.85f, 0.72f, 0.30f);
        Color cDialCromo = new Color(0.70f, 0.74f, 0.80f);
        Color cSangreHuella = new Color(0.48f, 0.04f, 0.04f, 0.88f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool borde = x <= 3 || x >= w - 4 || y <= 3 || y >= h - 4;
                bool remaches = (x == 5 || x == w - 6) && (y == 5 || y == h - 6 || y == 32);
                float distCentro = Vector2.Distance(new Vector2(x, y), centro);

                if (borde)
                {
                    cols[y * w + x] = cBordeExterior;
                }
                else if (remaches)
                {
                    cols[y * w + x] = cPernoRemache;
                }
                else if (distCentro < 14f)
                {
                    // Rueda de bóveda de 3 radios
                    float angle = Mathf.Atan2(y - centro.y, x - centro.x);
                    bool radios = Mathf.Abs(Mathf.Sin(angle * 3f)) > 0.88f;
                    bool bordeDial = (distCentro >= 11f && distCentro <= 13f);
                    bool centroEje = distCentro <= 4f;

                    if (bordeDial || radios || centroEje)
                    {
                        cols[y * w + x] = cDialCromo;
                    }
                    else
                    {
                        cols[y * w + x] = new Color(0.12f, 0.14f, 0.16f);
                    }
                }
                else if (x >= 44 && x <= 56 && y >= 38 && y <= 50)
                {
                    // Teclado numérico con LED verde/rojo
                    bool ledVerde = (x == 50 && y == 48);
                    cols[y * w + x] = ledVerde ? Color.green : new Color(0.08f, 0.09f, 0.11f);
                }
                // Huella ensangrentada desesperada en la esquina inferior izquierda
                else if (x >= 8 && x <= 16 && y >= 8 && y <= 16 && Mathf.PerlinNoise(x * 0.4f, y * 0.4f) > 0.45f)
                {
                    cols[y * w + x] = cSangreHuella;
                }
                else
                {
                    float ruido = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.05f;
                    cols[y * w + x] = new Color(cAceroAcorazado.r + ruido, cAceroAcorazado.g + ruido, cAceroAcorazado.b + ruido, 1f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteCasilleroMetalico()
    {
        int w = 48, h = 80;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cChapaVerdeHospital = new Color(0.24f, 0.30f, 0.28f);
        Color cBorde = new Color(0.10f, 0.14f, 0.12f);
        Color cOxido = new Color(0.42f, 0.22f, 0.10f);
        Color cTiradorCromo = new Color(0.88f, 0.92f, 0.95f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool bordeExt = x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3;
                bool divisionPuertas = (x == 16 || x == 32);

                if (bordeExt || divisionPuertas)
                {
                    cols[y * w + x] = cBorde;
                }
                else
                {
                    // Rejillas de ventilación superiores e inferiores
                    bool rejillaSup = (y >= 64 && y <= 72 && y % 2 == 0 && x % 16 >= 4 && x % 16 <= 12);
                    bool rejillaInf = (y >= 10 && y <= 18 && y % 2 == 0 && x % 16 >= 4 && x % 16 <= 12);
                    bool manija = (y >= 38 && y <= 44 && x % 16 == 13);
                    float oxidoMancha = Mathf.PerlinNoise(x * 0.25f, y * 0.12f);

                    if (rejillaSup || rejillaInf)
                    {
                        cols[y * w + x] = new Color(0.06f, 0.08f, 0.07f);
                    }
                    else if (manija)
                    {
                        cols[y * w + x] = cTiradorCromo;
                    }
                    else if (oxidoMancha > 0.65f)
                    {
                        cols[y * w + x] = Color.Lerp(cChapaVerdeHospital, cOxido, 0.75f);
                    }
                    else
                    {
                        float ruido = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.04f;
                        cols[y * w + x] = new Color(cChapaVerdeHospital.r + ruido, cChapaVerdeHospital.g + ruido, cChapaVerdeHospital.b + ruido, 1f);
                    }
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteBaulAntiguo()
    {
        int w = 64, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cMaderaRoble = new Color(0.30f, 0.18f, 0.10f);
        Color cFlejeHierro = new Color(0.18f, 0.18f, 0.20f);
        Color cRemacheBronce = new Color(0.82f, 0.68f, 0.28f);
        Color cCerradura = new Color(0.75f, 0.60f, 0.22f);
        Color cSelloCera = new Color(0.70f, 0.12f, 0.12f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool borde = x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3;
                bool flejes = (x >= 14 && x <= 18) || (x >= 46 && x <= 50) || (y >= 22 && y <= 26);
                bool cerradura = (x >= 28 && x <= 36 && y >= 18 && y <= 28);
                bool sello = (x >= 22 && x <= 26 && y >= 30 && y <= 34);

                if (sello)
                {
                    cols[y * w + x] = cSelloCera;
                }
                else if (cerradura)
                {
                    bool ojoLlave = (x >= 31 && x <= 33 && y >= 20 && y <= 24);
                    cols[y * w + x] = ojoLlave ? Color.black : cCerradura;
                }
                else if (flejes)
                {
                    bool remache = (flejes && (y % 8 == 0 || x % 4 == 0));
                    cols[y * w + x] = remache ? cRemacheBronce : cFlejeHierro;
                }
                else if (borde)
                {
                    cols[y * w + x] = new Color(0.12f, 0.08f, 0.04f);
                }
                else
                {
                    float veta = Mathf.Sin(y * 0.45f + Mathf.PerlinNoise(x * 0.12f, y * 0.08f) * 3f) * 0.06f;
                    cols[y * w + x] = new Color(cMaderaRoble.r + veta, cMaderaRoble.g + veta, cMaderaRoble.b + veta, 1f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteGavetaMorgueAbierta()
    {
        int w = 56, h = 72;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cAceroGaveta = new Color(0.40f, 0.44f, 0.50f);
        Color cEscarcha = new Color(0.75f, 0.90f, 0.98f);
        Color cSombraInterna = new Color(0.02f, 0.03f, 0.05f);
        Color cTiradorCromo = new Color(0.92f, 0.96f, 1.0f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool borde = x <= 3 || x >= w - 4 || y <= 3 || y >= h - 4;
                bool tiradorFrontal = (y >= 6 && y <= 10 && x >= 18 && x <= 38);

                if (tiradorFrontal)
                {
                    cols[y * w + x] = cTiradorCromo;
                }
                else if (borde)
                {
                    // Borde helado con cristales de escarcha
                    cols[y * w + x] = (x <= 2 || y <= 2) ? cEscarcha : cAceroGaveta;
                }
                else
                {
                    // Interior oscuro como boca de lobo donde el vigilante se esconde de las momias
                    float fondo = (float)y / h;
                    cols[y * w + x] = Color.Lerp(cSombraInterna, new Color(0.06f, 0.08f, 0.12f), fondo * 0.4f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteBiomboQuirurgico()
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cTelaMentol = new Color(0.62f, 0.74f, 0.68f);
        Color cMarcoCromo = new Color(0.85f, 0.88f, 0.92f);
        Color cSangreSalpicada = new Color(0.52f, 0.05f, 0.05f, 0.90f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool marco = x <= 2 || x >= w - 3 || y <= 3 || y >= h - 4 || (x == 21 || x == 42);
                bool ruedas = (y <= 3 && (x == 4 || x == 21 || x == 42 || x == w - 5));

                if (marco || ruedas)
                {
                    cols[y * w + x] = cMarcoCromo;
                }
                else
                {
                    // Pliegues verticales de tela hospitalaria
                    float pliegue = Mathf.Sin((x % 21) * 0.35f) * 0.06f;
                    // Salpicadura de sangre seca
                    float mancha = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    if (mancha > 0.72f && y >= 14 && y <= 38)
                    {
                        cols[y * w + x] = cSangreSalpicada;
                    }
                    else
                    {
                        cols[y * w + x] = new Color(cTelaMentol.r + pliegue, cTelaMentol.g + pliegue, cTelaMentol.b + pliegue, 1f);
                    }
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteCamillaConSabana()
    {
        int w = 64, h = 80;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cSabanaMorgue = new Color(0.78f, 0.80f, 0.82f);
        Color cSombraPliegue = new Color(0.55f, 0.58f, 0.62f);
        Color cHuecoOculto = new Color(0.02f, 0.02f, 0.03f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Espacio bajo la camilla para esconderse
                if (y <= 14)
                {
                    cols[y * w + x] = cHuecoOculto;
                }
                else
                {
                    // Caída y ondulaciones de la sábana que cubre el cuerpo
                    float pliegues = Mathf.Sin(x * 0.55f) * 0.10f + Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.08f;
                    Color colSabana = Color.Lerp(cSombraPliegue, cSabanaMorgue, 0.5f + pliegues);

                    // Contorno sutil del cuerpo acostado
                    bool siluetaCuerpo = (x >= 18 && x <= 46 && y >= 30 && y <= 65);
                    if (siluetaCuerpo) colSabana = Color.Lerp(colSabana, Color.white, 0.20f);

                    cols[y * w + x] = colSabana;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteBajoEscritorio()
    {
        int w = 64, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cMadera = new Color(0.22f, 0.14f, 0.08f);
        Color cBorde = new Color(0.10f, 0.06f, 0.04f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool tapa = y >= 38;
                bool cajonIzq = (x <= 16 && y < 38);
                bool cajonDer = (x >= 48 && y < 38);
                bool huecoCentro = (x > 16 && x < 48 && y < 38);

                if (tapa || cajonIzq || cajonDer)
                {
                    bool tiradorCajon = (y >= 18 && y <= 22 && (x == 8 || x == 56));
                    cols[y * w + x] = tiradorCajon ? new Color(0.85f, 0.72f, 0.35f) : (x <= 2 || x >= w - 3 ? cBorde : cMadera);
                }
                else if (huecoCentro)
                {
                    // Sombra total bajo las piernas del escritorio
                    cols[y * w + x] = new Color(0.02f, 0.02f, 0.03f, 1f);
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteEtiquetaForense()
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color cCarton = new Color(0.84f, 0.76f, 0.60f);
        Color cCuerda = new Color(0.60f, 0.50f, 0.36f);
        Color cOjalMetal = new Color(0.40f, 0.34f, 0.22f);
        Color cSelloRojo = new Color(0.82f, 0.18f, 0.18f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool cuerda = (y >= 22 && (x == 15 || x == 16));
                bool etiqueta = (x >= 6 && x <= 25 && y >= 4 && y <= 22);
                bool ojal = (x >= 14 && x <= 17 && y >= 19 && y <= 21);
                bool sello = (x >= 10 && x <= 21 && y >= 8 && y <= 14);

                if (cuerda) cols[y * w + x] = cCuerda;
                else if (ojal) cols[y * w + x] = cOjalMetal;
                else if (sello) cols[y * w + x] = cSelloRojo;
                else if (etiqueta) cols[y * w + x] = cCarton;
                else cols[y * w + x] = Color.clear;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }

    private Sprite GenerarSpriteTermometroMorgue()
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        Vector2 centro = new Vector2(16, 16);

        Color cBiselBronce = new Color(0.72f, 0.58f, 0.24f);
        Color cEsferaBlanca = new Color(0.90f, 0.92f, 0.94f);
        Color cReflejoVidrio = new Color(1.0f, 1.0f, 1.0f, 0.95f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centro);
                if (dist <= 15f && dist >= 12f)
                {
                    cols[y * w + x] = cBiselBronce;
                }
                else if (dist < 12f)
                {
                    // Aguja roja señalando el número 9
                    bool aguja = (x >= 15 && x <= 22 && y >= 15 && y <= 22 && Mathf.Abs((x - 16) - (y - 16)) <= 1);
                    bool reflejo = (x + y == 28 && dist >= 6f && dist <= 10f);

                    if (reflejo) cols[y * w + x] = cReflejoVidrio;
                    else if (aguja) cols[y * w + x] = Color.red;
                    else cols[y * w + x] = cEsferaBlanca;
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return CrearSpriteFullRect(tex, 32f);
    }
}
