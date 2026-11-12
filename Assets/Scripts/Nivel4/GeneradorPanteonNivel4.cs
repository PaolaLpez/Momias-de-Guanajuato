using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Generador Oficial del Escenario del Nivel 4 (Nivel Final):
/// "El Gran Panteón de Santa Paula y la Capilla del Descanso" (03:00 AM a 06:00 AM).
/// 
/// 100% ARTE 2D DEDICADO Y EXCLUSIVO PARA EL PANTEÓN (CERO REUTILIZACIÓN DE NIVELES ANTERIORES):
/// 1. CERO PUERTAS FALSAS NI MUROS DE OFICINA:
///    - Eliminadas completamente las puertas de madera de oficina que se repetían como paredes.
///    - Delimitación mediante verjas góticas de hierro forjado y pilares de piedra con hiedra.
///    - Pasos diáfanos y amplios (de 6 a 8 metros) que garantizan 100% navegabilidad sin atascos.
/// 
/// 2. SUELO Y CALZADAS REALISTAS DE CEMENTERIO NOCTURNO:
///    - Suelo base: "suelo_tierra_panteon" (tierra negra de sepultura húmeda, humus, fango y hojas secas).
///    - Calzada Mayor y transversales: "camino_piedra_antigua" (adoquines de cantera y lajas desgastadas).
///    - Muros perimetrales: "muro_cantera_gavetas" (arquitectura auténtica de Guanajuato con nichos apilados).
/// 
/// 3. VEGETACIÓN Y MONUMENTOS GÓTICOS FIDELIGNOS:
///    - Árboles centenarios muertos y retorcidos ("arbol_muerto_gotico_1" y doseles "arbol_muerto_gotico_2").
///    - Cipreses fúnebres ("cipres_gotico").
///    - Gran variedad de lápidas ("lapida_arco_1", "lapida_cruz_celta", "lapida_obelisco", "lapida_quebrada").
///    - Sarcófagos de piedra monumental ("sarcofago_piedra_tumba") y fosas abiertas ("fosa_abierta_tierra").
///    - Faroles victorianos de gas ("farol_gas_gotico") con luz titilante.
///    - Fachada monumental de la capilla ("capilla_fachada_gotica") y altar con cirios ("altar_gotico_cirios").
///    - Bruma nocturna suave ("capa_bruma_niebla" en SpriteRenderer, CERO sistemas de partículas).
/// 
/// 4. COLISIONES FÍSICAS SÓLIDAS:
///    - Cada lápida, árbol, pilar, muro, farol y monumento posee BoxCollider2D físico sin atravesar.
/// </summary>
[ExecuteAlways]
public class GeneradorPanteonNivel4 : MonoBehaviour
{
    public const int VERSION_PANTEON_FINAL = 95;

    // --- SPRITES 100% DEDICADOS DEL PANTEÓN (Assets/Resources/Cementerio/) ---
    private Sprite spSueloTierra;
    private Sprite spCaminoPiedra;
    private Sprite spMuroGavetas;
    private Sprite spPortonHierro;
    private Sprite spVerjaHierro;
    private Sprite spPilarHiedra;
    private Sprite spArbolMuerto1;
    private Sprite spArbolMuerto2;
    private Sprite spCipres;
    private Sprite spLapidaArco;
    private Sprite spLapidaCruzCelta;
    private Sprite spLapidaObelisco;
    private Sprite spLapidaQuebrada;
    private Sprite spSarcofago;
    private Sprite spFosaAbierta;
    private Sprite spFarolGas;
    private Sprite spCapillaFachada;
    private Sprite spAltarCirios;
    private Sprite spBrumaNiebla;

    // Sprites 100% dedicados de Reliquias Físicas y Pebeteros Sagrados
    private Sprite spReliquiaCrucifijo;
    private Sprite spReliquiaEslabon;
    private Sprite spReliquiaLibro;
    private Sprite spPebeteroCirio;
    private Sprite spHojasSecas;

    [Header("Referencias de Escena")]
    public GameObject portonSurVisual;
    public BoxCollider2D colisionPortonSur;
    [HideInInspector] public Transform transformCampanaMayor;
    [HideInInspector] public Transform transformCuerdaCampana;

    private void Awake()
    {
        VerificarYConstruir();
    }

    private void OnEnable()
    {
        VerificarYConstruir();
    }

    void CargarRecursosDedicados()
    {
        spSueloTierra = Resources.Load<Sprite>("Cementerio/suelo_tierra_panteon");
        spCaminoPiedra = Resources.Load<Sprite>("Cementerio/camino_piedra_antigua");
        spMuroGavetas = Resources.Load<Sprite>("Cementerio/muro_cantera_gavetas");
        spPortonHierro = Resources.Load<Sprite>("Cementerio/porton_hierro_ornado");
        spVerjaHierro = Resources.Load<Sprite>("Cementerio/verja_hierro_gotica");
        spPilarHiedra = Resources.Load<Sprite>("Cementerio/pilar_piedra_hiedra");
        spArbolMuerto1 = Resources.Load<Sprite>("Cementerio/arbol_muerto_gotico_1");
        spArbolMuerto2 = Resources.Load<Sprite>("Cementerio/arbol_muerto_gotico_2");
        spCipres = Resources.Load<Sprite>("Cementerio/cipres_gotico");
        spLapidaArco = Resources.Load<Sprite>("Cementerio/lapida_arco_1");
        spLapidaCruzCelta = Resources.Load<Sprite>("Cementerio/lapida_cruz_celta");
        spLapidaObelisco = Resources.Load<Sprite>("Cementerio/lapida_obelisco");
        spLapidaQuebrada = Resources.Load<Sprite>("Cementerio/lapida_quebrada");
        spSarcofago = Resources.Load<Sprite>("Cementerio/sarcofago_piedra_tumba");
        spFosaAbierta = Resources.Load<Sprite>("Cementerio/fosa_abierta_tierra");
        spFarolGas = Resources.Load<Sprite>("Cementerio/farol_gas_gotico");
        spCapillaFachada = Resources.Load<Sprite>("Cementerio/capilla_fachada_gotica");
        spAltarCirios = Resources.Load<Sprite>("Cementerio/altar_gotico_cirios");
        spBrumaNiebla = Resources.Load<Sprite>("Cementerio/capa_bruma_niebla");

        // Carga de reliquias con forma física real (Crucifijo, Eslabón forjado y Libro encuadernado)
        spReliquiaCrucifijo = Resources.Load<Sprite>("Cementerio/reliquia_crucifijo_plomo");
        spReliquiaEslabon = Resources.Load<Sprite>("Cementerio/reliquia_eslabon_hierro");
        spReliquiaLibro = Resources.Load<Sprite>("Cementerio/reliquia_libro_parroquial");
        spPebeteroCirio = Resources.Load<Sprite>("Cementerio/pebetero_cirio_bendito");

        // Fallbacks si alguno no estuviera en Resources
        if (spReliquiaCrucifijo == null) spReliquiaCrucifijo = spLapidaCruzCelta;
        if (spReliquiaEslabon == null) spReliquiaEslabon = spVerjaHierro;
        if (spReliquiaLibro == null) spReliquiaLibro = spSarcofago;
        if (spPebeteroCirio == null) spPebeteroCirio = spAltarCirios;
    }

    public void VerificarYConstruir()
    {
        ReconstruirPanteonTotal();
    }

    [ContextMenu("Reconstruir Panteón Limpio")]
    public void ReconstruirManual()
    {
        ReconstruirPanteonTotal();
    }

    public void ReconstruirPanteonTotal()
    {
        CargarRecursosDedicados();

        // 1. Eliminar cualquier escenario previo
        GameObject viejo = GameObject.Find("EscenarioPanteonNivel4");
        if (viejo != null)
        {
            if (Application.isPlaying) Destroy(viejo);
            else DestroyImmediate(viejo);
        }

        // 2. Destruir cualquier sistema de partículas para erradicar cualquier cuadro magenta
        ParticleSystem[] particulas = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var p in particulas)
        {
            if (Application.isPlaying) Destroy(p.gameObject);
            else DestroyImmediate(p.gameObject);
        }

        GameObject root = new GameObject("EscenarioPanteonNivel4");

        // 1. SUELO DEL PANTEÓN: Tierra natural oscura y avenidas empedradas
        ConstruirSuelos(root);

        // 2. MURO PERIMETRAL DE GAVETAS DE CANTERA (Nichos apilados de Guanajuato)
        ConstruirMurosPerimetrales(root);

        // 3. GRAN PORTÓN MONUMENTAL SUR DE HIERRO FORJADO Y ATRIO DE ENTRADA
        ConstruirEntradaSur(root);

        // 4. VERJAS GÓTICAS DE HIERRO Y PILARES CON HIEDRA (Delimitación de parcelas con pasos de 6m)
        ConstruirVerjasYDelimitaciones(root);

        // 5. ÁRBOLES GÓTICOS CENTENARIOS Y DOSELES RETORCIDOS
        DistribuirArbolesMuertos(root);

        // 6. SECTOR OESTE: MAUSOLEO LEROY Y CRIPTAS FAMILIARES (Reliquia 1: Crucifijo de Plomo de 1865)
        ConstruirMausoleoLeroy(root);

        // 7. SECTOR ESTE: GRAN OSARIO GENERAL Y NICHOS PROFUNDOS (Reliquia 2: Eslabón Bendito de Hierro)
        ConstruirGranOsario(root);

        // 8. SECTOR NORTE: CAPILLA DEL DESCANSO, ALTAR DE CIRIOS, LIBRO Y CAMPANA MAYOR
        ConstruirCapillaMonumental(root);

        // 9. CAMPOSANTO: PARCELAS CON LÁPIDAS REALISTAS, CRUCES CELTAS, OBELISCOS, SARCÓFAGOS Y FOSAS
        DistribuirTumbasYLapidas(root);

        // 10. CIPRESES GÓTICOS ESBELTOS Y FAROLES DE GAS VICTORIANOS
        DistribuirCipresesYFaroles(root);

        // 11. TRAMPAS ACÚSTICAS (Hojas secas crujientes)
        DistribuirTrampasAcusticas(root);

        // 12. ZONAS DE SILENCIO (Desafío de contener el aliento / micrófono)
        DistribuirZonasSilencio(root);

        // 13. CAPAS DE BRUMA Y NIEBLA ESPECTRAL (SpriteRenderer sin partículas)
        DistribuirCapasBruma(root);

        // 14. CIRIOS BENDITOS DEL CAMPOSANTO (3 Zonas Seguras Temporales con Fuego Sagrado)
        DistribuirCiriosBenditos(root);

        // 15. DISTRIBUIR TODAS LAS MOMIAS HISTÓRICAS (Nivel 2, Nivel 3 y Carmen Nivel 4)
        DistribuirTodasLasMomias(root);

        // 16. CONFIGURAR CÁMARA Y SEGUIMIENTO CON SOPORTE DE VISTA PREVIA (TECLA M)
        ConfigurarCamaraYSeguimiento();

        Debug.Log("<color=green>[GeneradorPanteonNivel4] ¡Panteón gótico de Santa Paula reconstruido con 19 texturas dedicadas exclusivas y CERO reutilización!</color>");
    }

    private void ConfigurarCamaraYSeguimiento()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = GameObject.FindGameObjectWithTag("MainCamera");
            if (camGo != null) cam = camGo.GetComponent<Camera>();
        }

        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 4.2f;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);

            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();
            cf.zoomCercano = 4.2f;
            cf.zoomMapaCompleto = 44f;
            cf.centroVistaPanoramica = new Vector3(0f, 4f, -10f);
            cf.suavizado = 8f;
            cf.velocidadZoom = 7f;
            cf.permitirTeclaM = true;
            cf.minX = -44f;
            cf.maxX = 44f;
            cf.minY = -34f;
            cf.maxY = 42f;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) cf.objetivo = player.transform;
        }
    }

    // =========================================================================
    // 1. SUELOS: TIERRA OSCURA NATURAL Y RED DE CALZADAS Y PASILLOS LATERALES
    // =========================================================================
    void ConstruirSuelos(GameObject root)
    {
        // Capa 1: Gran manto de tierra de camposanto (104 x 88 metros)
        CrearSuperficieTiled(root, "Piso_Tierra_Camposanto_Base", 0f, 2f, 104f, 88f, spSueloTierra, Color.white, -20);

        // Capa 2: Calzada Central Sur (une la Entrada Sur con la Avenida Transversal Sur)
        CrearSuperficieTiled(root, "Calzada_Central_Sur_Adoquin", 0f, -22f, 7f, 16f, spCaminoPiedra, Color.white, -15);

        // Capa 3: Calzada Central Media (llega hasta la verja clausurada a Y = 10)
        CrearSuperficieTiled(root, "Calzada_Central_Media_Adoquin", 0f, -3f, 7f, 22f, spCaminoPiedra, Color.white, -15);

        // Capa 4: Avenida Transversal Sur (eje distribuidor inferior que conecta Oeste y Este a Y = -16)
        CrearSuperficieTiled(root, "Avenida_Transversal_Sur", 0f, -16f, 76f, 6f, spCaminoPiedra, Color.white, -15);

        // Capa 5: Calzada Lateral Oeste (Pasillo procesional hacia Mausoleo Leroy y acceso lateral a la Iglesia)
        CrearSuperficieTiled(root, "Calzada_Lateral_Oeste_Leroy", -24f, 1f, 6.5f, 38f, spCaminoPiedra, Color.white, -15);

        // Capa 6: Calzada Lateral Este (Pasillo procesional hacia Gran Osario y acceso lateral a la Iglesia)
        CrearSuperficieTiled(root, "Calzada_Lateral_Este_Osario", 24f, 1f, 6.5f, 38f, spCaminoPiedra, Color.white, -15);

        // Capa 7: Avenida Transversal Norte / Atrio de la Capilla (conecta ambos pasillos laterales frente a la Iglesia)
        CrearSuperficieTiled(root, "Avenida_Transversal_Norte_Capilla", 0f, 18f, 76f, 8f, spCaminoPiedra, Color.white, -15);

        // Capa 8: Atrio de Entrada Sur
        CrearSuperficieTiled(root, "Atrio_Entrada_Sur", 0f, -28f, 36f, 12f, spCaminoPiedra, Color.white, -14);

        // Capa 9: Suelo del Santuario y Claustro de la Capilla (Abarca todo el alrededor de la Iglesia)
        CrearSuperficieTiled(root, "Piso_Claustro_Capilla", 0f, 28f, 52f, 22f, spCaminoPiedra, new Color(0.70f, 0.70f, 0.75f, 1f), -14);
    }

    // =========================================================================
    // 2. MURO PERIMETRAL DE GAVETAS DE CANTERA (Nichos de Guanajuato)
    // =========================================================================
    void ConstruirMurosPerimetrales(GameObject root)
    {
        // Muros de 3.5 metros de espesor con la textura auténtica de gavetas y placas sepulcrales grabadas
        CrearMuroGavetas(root, "MuroPerimetro_Norte", 0f, 42f, 96f, 3.5f);
        CrearMuroGavetas(root, "MuroPerimetro_Sur_Izq", -25f, -35.5f, 42f, 3.5f);
        CrearMuroGavetas(root, "MuroPerimetro_Sur_Der", 25f, -35.5f, 42f, 3.5f);
        CrearMuroGavetas(root, "MuroPerimetro_Oeste", -46f, 3f, 3.5f, 78f);
        CrearMuroGavetas(root, "MuroPerimetro_Este", 46f, 3f, 3.5f, 78f);
    }

    // =========================================================================
    // 3. ENTRADA SUR Y GRAN PORTÓN MONUMENTAL DE HIERRO
    // =========================================================================
    void ConstruirEntradaSur(GameObject root)
    {
        // El Gran Portón Sur: 8 metros de ancho con textura ornamental de filigrana y lanzas
        GameObject go = new GameObject("GranPortonSur_Salida");
        go.transform.SetParent(root.transform);
        go.transform.position = new Vector3(0f, -35.5f, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spPortonHierro;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(8f, 3.5f);
        sr.sortingOrder = 8;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(8f, 3.5f);
        col.isTrigger = false;

        portonSurVisual = go;
        colisionPortonSur = col;

        // Pilares colosales de piedra con hiedra a los lados del portón
        CrearPilarStone(root, new Vector3(-4.5f, -35.5f, 0), 1.6f, 3.8f);
        CrearPilarStone(root, new Vector3(4.5f, -35.5f, 0), 1.6f, 3.8f);

        // Cartel Histórico de Carmen "La Bruja" con farol victoriano
        CrearCartelHistorico(root, 5.0f, -28.5f);

        // Obelisco doliente en el atrio
        CrearObelisco(root, new Vector3(-5.0f, -28.5f, 0));

        // ATRIO SUR-ESTE: Mesa de la Sacristía y Reliquia 3 (Libro Parroquial de 1865) en la zona inferior
        GameObject mesaSacristia = new GameObject("MesaSacristia");
        mesaSacristia.transform.SetParent(root.transform, false);
        mesaSacristia.transform.localPosition = new Vector3(15.5f, -29.0f, 0);

        SpriteRenderer srMesa = mesaSacristia.AddComponent<SpriteRenderer>();
        srMesa.sprite = spSarcofago;
        srMesa.sortingOrder = 7;
        mesaSacristia.transform.localScale = new Vector3(0.95f, 0.85f, 1f);

        BoxCollider2D colMesa = mesaSacristia.AddComponent<BoxCollider2D>();
        colMesa.size = new Vector2(1.8f, 1.2f);
        colMesa.isTrigger = false;

        GameObject rel = new GameObject("Reliquia_LibroParroquial");
        rel.transform.SetParent(mesaSacristia.transform, false);
        rel.transform.localPosition = new Vector3(0f, 0.45f, 0);
        SpriteRenderer srR = rel.AddComponent<SpriteRenderer>();
        srR.sprite = spReliquiaLibro != null ? spReliquiaLibro : spSarcofago;
        srR.color = Color.white;
        srR.sortingOrder = 9;
        rel.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        BoxCollider2D colRel = rel.AddComponent<BoxCollider2D>();
        colRel.size = new Vector2(2.4f, 2.4f);
        colRel.isTrigger = true;

        ReliquiaItemNivel4 rScript = rel.AddComponent<ReliquiaItemNivel4>();
        rScript.tipo = ReliquiaItemNivel4.TipoInteractivo.LibroParroquial;
        rScript.nombreMostrado = "Libro Parroquial de 1865";

        GameObject luzRel = new GameObject("LuzDoradaLibro");
        luzRel.transform.SetParent(rel.transform, false);
        Light2D lLibro = luzRel.AddComponent<Light2D>();
        lLibro.lightType = Light2D.LightType.Point;
        lLibro.color = new Color(1f, 0.88f, 0.45f, 1f);
        lLibro.intensity = 1.85f;
        lLibro.pointLightInnerRadius = 1.0f;
        lLibro.pointLightOuterRadius = 8.5f;

        CrearFarolGas(root, new Vector3(15.5f, -26.8f, 0));
    }

    void CrearCartelHistorico(GameObject root, float x, float y)
    {
        GameObject cartel = new GameObject("Cartel_HistoriaBruja");
        cartel.transform.SetParent(root.transform);
        cartel.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = cartel.AddComponent<SpriteRenderer>();
        sr.sprite = spLapidaArco;
        sr.color = new Color(0.85f, 0.85f, 0.90f, 1f);
        sr.sortingOrder = 7;
        cartel.transform.localScale = new Vector3(1.6f, 1.8f, 1f);

        BoxCollider2D colSolido = cartel.AddComponent<BoxCollider2D>();
        colSolido.size = new Vector2(1.5f, 1.0f);
        colSolido.offset = new Vector2(0f, -0.4f);
        colSolido.isTrigger = false;

        GameObject trigObj = new GameObject("TriggerLecturaCartel");
        trigObj.transform.SetParent(cartel.transform, false);
        BoxCollider2D colTrig = trigObj.AddComponent<BoxCollider2D>();
        colTrig.size = new Vector2(3.2f, 3.2f);
        colTrig.isTrigger = true;

        ReliquiaItemNivel4 item = trigObj.AddComponent<ReliquiaItemNivel4>();
        item.tipo = ReliquiaItemNivel4.TipoInteractivo.CartelLore;
        item.nombreMostrado = "Crónica de Carmen 'La Bruja'";

        CrearFarolGas(cartel, new Vector3(0f, 1.4f, 0f));
    }

    // =========================================================================
    // 4. VERJAS GÓTICAS Y DELIMITACIONES DE PARCELAS (PASO TOTALMENTE DESPEJADO)
    // =========================================================================
    void ConstruirVerjasYDelimitaciones(GameObject root)
    {
        // Se elimina la pared y el portón frontal para que el jugador nunca quede encerrado
        // y pueda transitar con total fluidez hacia la Capilla y hacia ambos laterales.

        // Faroles de gas góticos iluminando la calzada central y cruces
        CrearFarolGas(root, new Vector3(-4.5f, -12f, 0));
        CrearFarolGas(root, new Vector3(4.5f, -12f, 0));
        CrearFarolGas(root, new Vector3(-4.5f, 6f, 0));
        CrearFarolGas(root, new Vector3(4.5f, 6f, 0));

        // Faroles en los pasajes laterales
        CrearFarolGas(root, new Vector3(-20.0f, -8f, 0));
        CrearFarolGas(root, new Vector3(20.0f, -8f, 0));
        CrearFarolGas(root, new Vector3(-20.0f, 10f, 0));
        CrearFarolGas(root, new Vector3(20.0f, 10f, 0));

        // Verjas perimetrales decorativas bajas delimitando las parcelas de tumbas (dejando accesos amplios)
        CrearVerjaTramo(root, -31f, -14f, 10f, 0.8f);
        CrearVerjaTramo(root, 31f, -14f, 10f, 0.8f);
    }

    void CrearVerjaTramo(GameObject root, float x, float y, float w, float h)
    {
        GameObject go = new GameObject($"VerjaHierro_{x}_{y}");
        go.transform.SetParent(root.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spVerjaHierro;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.sortingOrder = 6;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);
        col.isTrigger = false;

        // Pilares en los extremos
        if (h > w)
        {
            CrearPilarStone(root, new Vector3(x, y - h / 2f, 0), 1.0f, 1.2f);
            CrearPilarStone(root, new Vector3(x, y + h / 2f, 0), 1.0f, 1.2f);
        }
        else
        {
            CrearPilarStone(root, new Vector3(x - w / 2f, y, 0), 1.2f, 1.0f);
            CrearPilarStone(root, new Vector3(x + w / 2f, y, 0), 1.2f, 1.0f);
        }
    }

    void CrearPilarStone(GameObject root, Vector3 pos, float w, float h)
    {
        GameObject pil = new GameObject("PilarPiedraHiedra");
        pil.transform.SetParent(root.transform, false);
        pil.transform.localPosition = pos;

        SpriteRenderer sr = pil.AddComponent<SpriteRenderer>();
        sr.sprite = spPilarHiedra;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(w, h);
        sr.sortingOrder = 7;

        BoxCollider2D col = pil.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);
        col.isTrigger = false;
    }

    void CrearPilarGoticoNave(GameObject root, Vector3 localPos)
    {
        GameObject pil = new GameObject("PilarGoticoNave");
        pil.transform.SetParent(root.transform, false);
        pil.transform.localPosition = localPos;

        SpriteRenderer sr = pil.AddComponent<SpriteRenderer>();
        sr.sprite = spPilarHiedra;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(1.1f, 1.8f);
        sr.sortingOrder = 6;

        BoxCollider2D col = pil.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 1.2f);
        col.offset = new Vector2(0f, -0.2f);
        col.isTrigger = false;
    }

    // =========================================================================
    // 5. ÁRBOLES GÓTICOS CENTENARIOS Y DOSELES RETORCIDOS
    // =========================================================================
    void DistribuirArbolesMuertos(GameObject root)
    {
        // Árboles retorcidos con raíces profundas y ramas secas (fieles a las referencias)
        Vector2[] posArboles1 = new Vector2[]
        {
            new Vector2(-10f, -28f), new Vector2(10f, -28f),
            new Vector2(-38f, -28f), new Vector2(38f, -28f),
            new Vector2(-38f, -14f), new Vector2(38f, -14f),
            new Vector2(-38f, 14f),  new Vector2(38f, 14f),
            new Vector2(-14f, 14f),  new Vector2(14f, 14f),
            new Vector2(-22f, 32f),  new Vector2(22f, 32f)
        };

        for (int i = 0; i < posArboles1.Length; i++)
        {
            GameObject arb = new GameObject($"ArbolMuertoGotico_{i}");
            arb.transform.SetParent(root.transform);
            arb.transform.position = new Vector3(posArboles1[i].x, posArboles1[i].y, 0);

            SpriteRenderer sr = arb.AddComponent<SpriteRenderer>();
            sr.sprite = spArbolMuerto1;
            sr.sortingOrder = 12;
            arb.transform.localScale = new Vector3(2.0f, 2.2f, 1f);

            // Colisión sólida del tronco en la base (la vigilante puede caminar alrededor de las ramas)
            BoxCollider2D col = arb.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.8f);
            col.offset = new Vector2(0f, -1.3f);
            col.isTrigger = false;
        }
    }

    // =========================================================================
    // 6. SECTOR OESTE: MAUSOLEO LEROY (Reliquia 1: Crucifijo de Plomo de 1865)
    // =========================================================================
    void ConstruirMausoleoLeroy(GameObject root)
    {
        GameObject sectorOeste = new GameObject("SectorOeste_Leroy");
        sectorOeste.transform.SetParent(root.transform);

        GameObject mausoleo = new GameObject("Mausoleo_Leroy_Medicos");
        mausoleo.transform.SetParent(sectorOeste.transform);
        mausoleo.transform.position = new Vector3(-28f, 6f, 0);

        // Muros de nichos y cantera del mausoleo
        CrearMuroGavetas(mausoleo, "MuroLeroy_Norte", 0f, 4.5f, 14f, 1.8f);
        CrearMuroGavetas(mausoleo, "MuroLeroy_Oeste", -6f, 0f, 1.8f, 9f);
        CrearMuroGavetas(mausoleo, "MuroLeroy_Este", 6f, 0f, 1.8f, 9f);

        // Fachada frontal con entrada amplia de 6 metros abierta
        CrearMuroGavetas(mausoleo, "MuroLeroy_Frontal_Izq", -4.5f, -4.5f, 3f, 1.8f);
        CrearMuroGavetas(mausoleo, "MuroLeroy_Frontal_Der", 4.5f, -4.5f, 3f, 1.8f);

        // Pilares colosales de cantera con hiedra flanqueando la entrada abierta
        CrearPilarStone(mausoleo, new Vector3(-3.0f, -4.5f, 0), 1.2f, 2.4f);
        CrearPilarStone(mausoleo, new Vector3(3.0f, -4.5f, 0), 1.2f, 2.4f);

        // Faroles victorianos en la entrada
        CrearFarolGas(mausoleo, new Vector3(-3.2f, -5.8f, 0));
        CrearFarolGas(mausoleo, new Vector3(3.2f, -5.8f, 0));

        // Pedestal que sostiene la Reliquia 1: Crucifijo de Plomo de 1865
        GameObject pedestal = new GameObject("Pedestal_Crucifijo");
        pedestal.transform.SetParent(mausoleo.transform);
        pedestal.transform.localPosition = new Vector3(0f, 1.2f, 0);

        SpriteRenderer srPed = pedestal.AddComponent<SpriteRenderer>();
        srPed.sprite = spPilarHiedra;
        srPed.drawMode = SpriteDrawMode.Sliced;
        srPed.size = new Vector2(1.2f, 1.6f);
        srPed.sortingOrder = 7;

        BoxCollider2D colPed = pedestal.AddComponent<BoxCollider2D>();
        colPed.size = new Vector2(1.2f, 1.6f);
        colPed.isTrigger = false;

        GameObject rel = new GameObject("Reliquia_CrucifijoPlomo");
        rel.transform.SetParent(pedestal.transform, false);
        rel.transform.localPosition = new Vector3(0f, 0.60f, 0);
        SpriteRenderer srR = rel.AddComponent<SpriteRenderer>();
        srR.sprite = spReliquiaCrucifijo != null ? spReliquiaCrucifijo : spLapidaCruzCelta;
        srR.color = Color.white;
        srR.sortingOrder = 9;
        rel.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        BoxCollider2D colRel = rel.AddComponent<BoxCollider2D>();
        colRel.size = new Vector2(2.4f, 2.4f);
        colRel.isTrigger = true;

        ReliquiaItemNivel4 rScript = rel.AddComponent<ReliquiaItemNivel4>();
        rScript.tipo = ReliquiaItemNivel4.TipoInteractivo.CrucifijoPlomo;
        rScript.nombreMostrado = "Crucifijo de Plomo de 1865";

        // Luz mística dorada sobre el Crucifijo
        GameObject luzRel = new GameObject("LuzDoradaCrucifijo");
        luzRel.transform.SetParent(rel.transform, false);
        Light2D lCruc = luzRel.AddComponent<Light2D>();
        lCruc.lightType = Light2D.LightType.Point;
        lCruc.color = new Color(1f, 0.88f, 0.45f, 1f);
        lCruc.intensity = 1.85f;
        lCruc.pointLightInnerRadius = 1.0f;
        lCruc.pointLightOuterRadius = 8.5f;

        // Sarcófagos de piedra (Escondites seguros)
        CrearSarcofagoEscondite(mausoleo, "Sarcófago del Dr. Remigio Leroy", new Vector3(-2.8f, -1.2f, 0));
        CrearSarcofagoEscondite(sectorOeste, "Cripta de los Frailes Agonizantes", new Vector3(-28f, -18f, 0));

        // Faroles victorianos
        CrearFarolGas(sectorOeste, new Vector3(-28f, -13f, 0));
        CrearFarolGas(sectorOeste, new Vector3(-18f, 6f, 0));
        CrearFarolGas(mausoleo, new Vector3(0f, -3.2f, 0));
    }

    // =========================================================================
    // 7. SECTOR ESTE: GRAN OSARIO (Reliquia 2: Eslabón Bendito de Hierro)
    // =========================================================================
    void ConstruirGranOsario(GameObject root)
    {
        GameObject sectorEste = new GameObject("SectorEste_Osario");
        sectorEste.transform.SetParent(root.transform);

        GameObject osario = new GameObject("Osario_General_Catacumbas");
        osario.transform.SetParent(sectorEste.transform);
        osario.transform.position = new Vector3(28f, 6f, 0);

        CrearMuroGavetas(osario, "MuroOsario_Norte", 0f, 4.5f, 14f, 1.8f);
        CrearMuroGavetas(osario, "MuroOsario_Oeste", -6f, 0f, 1.8f, 9f);
        CrearMuroGavetas(osario, "MuroOsario_Este", 6f, 0f, 1.8f, 9f);

        // Muros frontales dejando el acceso abierto y despejado de 6 metros
        CrearMuroGavetas(osario, "MuroOsario_Frontal_Izq", -4.5f, -4.5f, 3f, 1.8f);
        CrearMuroGavetas(osario, "MuroOsario_Frontal_Der", 4.5f, -4.5f, 3f, 1.8f);

        // Pilares colosales de piedra con hiedra flanqueando la entrada abierta
        CrearPilarStone(osario, new Vector3(-3.0f, -4.5f, 0), 1.2f, 2.4f);
        CrearPilarStone(osario, new Vector3(3.0f, -4.5f, 0), 1.2f, 2.4f);

        // Faroles de gas encendidos iluminando la entrada del Osario
        CrearFarolGas(osario, new Vector3(-3.2f, -5.8f, 0));
        CrearFarolGas(osario, new Vector3(3.2f, -5.8f, 0));

        // Pedestal que sostiene la Reliquia 2: Eslabón Bendito de Hierro
        GameObject pedestal = new GameObject("Pedestal_Eslabon");
        pedestal.transform.SetParent(osario.transform);
        pedestal.transform.localPosition = new Vector3(0f, 1.2f, 0);

        SpriteRenderer srPed = pedestal.AddComponent<SpriteRenderer>();
        srPed.sprite = spPilarHiedra;
        srPed.drawMode = SpriteDrawMode.Sliced;
        srPed.size = new Vector2(1.2f, 1.6f);
        srPed.sortingOrder = 7;

        BoxCollider2D colPed = pedestal.AddComponent<BoxCollider2D>();
        colPed.size = new Vector2(1.2f, 1.6f);
        colPed.isTrigger = false;

        GameObject rel = new GameObject("Reliquia_EslabonBendito");
        rel.transform.SetParent(pedestal.transform, false);
        rel.transform.localPosition = new Vector3(0f, 0.60f, 0);
        SpriteRenderer srR = rel.AddComponent<SpriteRenderer>();
        srR.sprite = spReliquiaEslabon != null ? spReliquiaEslabon : spVerjaHierro;
        srR.color = Color.white;
        srR.sortingOrder = 9;
        rel.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        BoxCollider2D colRel = rel.AddComponent<BoxCollider2D>();
        colRel.size = new Vector2(2.4f, 2.4f);
        colRel.isTrigger = true;

        ReliquiaItemNivel4 rScript = rel.AddComponent<ReliquiaItemNivel4>();
        rScript.tipo = ReliquiaItemNivel4.TipoInteractivo.EslabonBendito;
        rScript.nombreMostrado = "Eslabón Bendito de Hierro";

        // Luz mística dorada sobre el Eslabón (muy visible y radiante desde la calzada)
        GameObject luzRel = new GameObject("LuzDoradaEslabon");
        luzRel.transform.SetParent(rel.transform, false);
        Light2D lEsl = luzRel.AddComponent<Light2D>();
        lEsl.lightType = Light2D.LightType.Point;
        lEsl.color = new Color(1f, 0.88f, 0.45f, 1f);
        lEsl.intensity = 1.85f;
        lEsl.pointLightInnerRadius = 1.0f;
        lEsl.pointLightOuterRadius = 8.5f;

        CrearSarcofagoEscondite(osario, "Nicho Profundo de las Catacumbas", new Vector3(2.8f, -1.2f, 0));
        CrearSarcofagoEscondite(sectorEste, "Fosa Común Abandonada", new Vector3(28f, -18f, 0));

        CrearFarolGas(sectorEste, new Vector3(28f, -13f, 0));
        CrearFarolGas(sectorEste, new Vector3(18f, 6f, 0));
        CrearFarolGas(osario, new Vector3(0f, -3.2f, 0));
    }

    // =========================================================================
    // 8. SECTOR NORTE: CAPILLA DEL DESCANSO (EXTERIOR MONUMENTAL INTEGRADO)
    // =========================================================================
    void ConstruirCapillaMonumental(GameObject root)
    {
        GameObject capilla = new GameObject("Capilla_Del_Descanso_Monumental");
        capilla.transform.SetParent(root.transform);
        capilla.transform.position = new Vector3(0f, 24f, 0);

        // Fachada Monumental Gótica Completa e Integrada (Aspecto natural, imponente y solemne)
        GameObject fachada = new GameObject("FachadaExteriorCapilla");
        fachada.transform.SetParent(capilla.transform, false);
        fachada.transform.localPosition = Vector3.zero;
        SpriteRenderer srF = fachada.AddComponent<SpriteRenderer>();
        srF.sprite = spCapillaFachada;
        srF.sortingOrder = 9;
        fachada.transform.localScale = new Vector3(3.4f, 3.4f, 1f);

        // Colisiones sólidas precisas de la Capilla Exterior (directamente bajo 'capilla' con escala 1,1,1):
        // 1. Muro frontal ala oeste (de X = -11.5 a X = -1.8)
        GameObject colOesteObj = new GameObject("Colision_MuroOeste_Capilla");
        colOesteObj.transform.SetParent(capilla.transform, false);
        colOesteObj.transform.localPosition = new Vector3(-6.65f, -4.6f, 0);
        BoxCollider2D colOeste = colOesteObj.AddComponent<BoxCollider2D>();
        colOeste.size = new Vector2(9.7f, 1.6f);
        colOeste.isTrigger = false;

        // 2. Muro frontal ala este (de X = +1.8 a X = +11.5)
        GameObject colEsteObj = new GameObject("Colision_MuroEste_Capilla");
        colEsteObj.transform.SetParent(capilla.transform, false);
        colEsteObj.transform.localPosition = new Vector3(6.65f, -4.6f, 0);
        BoxCollider2D colEste = colEsteObj.AddComponent<BoxCollider2D>();
        colEste.size = new Vector2(9.7f, 1.6f);
        colEste.isTrigger = false;

        // 3. Muro posterior/techo (evita cruzar hacia el norte sobre la estructura)
        GameObject colTechoObj = new GameObject("Colision_Techo_Capilla");
        colTechoObj.transform.SetParent(capilla.transform, false);
        colTechoObj.transform.localPosition = new Vector3(0f, 0.5f, 0);
        BoxCollider2D colTecho = colTechoObj.AddComponent<BoxCollider2D>();
        colTecho.size = new Vector2(24f, 8.0f);
        colTecho.isTrigger = false;

        // Muros de cantera laterales que integran la Capilla al perímetro del Panteón
        // (Evitan que el jugador camine hacia el vacío detrás de la iglesia)
        CrearMuroGavetas(capilla, "MuroAlaOesteCapilla", -24.5f, 0.2f, 26f, 3.2f);
        CrearMuroGavetas(capilla, "MuroAlaEsteCapilla", 24.5f, 0.2f, 26f, 3.2f);

        // Atrio de entrada frontal pavimentado en el camposanto
        GameObject atrioExterior = new GameObject("AtrioExteriorCapilla");
        atrioExterior.transform.SetParent(capilla.transform, false);
        atrioExterior.transform.localPosition = new Vector3(0f, -5.8f, 0);
        SpriteRenderer srAtrio = atrioExterior.AddComponent<SpriteRenderer>();
        srAtrio.sprite = spCaminoPiedra;
        srAtrio.drawMode = SpriteDrawMode.Tiled;
        srAtrio.size = new Vector2(9.6f, 4.2f);
        srAtrio.color = new Color(0.85f, 0.82f, 0.75f, 1f);
        srAtrio.sortingOrder = 3;

        // Pilares de piedra con hiedra y faroles que custodian el atrio monumental
        CrearPilarStone(capilla, new Vector3(-5.2f, -5.8f, 0), 1.2f, 2.2f);
        CrearPilarStone(capilla, new Vector3(5.2f, -5.8f, 0), 1.2f, 2.2f);
        CrearFarolGas(capilla, new Vector3(-5.2f, -4.5f, 0));
        CrearFarolGas(capilla, new Vector3(5.2f, -4.5f, 0));

        // PORTAL MONUMENTAL DE ENTRADA A LA CAPILLA
        // Al reunir las 3 reliquias sagradas, el portal se desbloquea y permite entrar al interior
        GameObject puerta = new GameObject("PuertaCapilla_Santuario");
        puerta.transform.SetParent(capilla.transform, false);
        puerta.transform.localPosition = new Vector3(0f, -4.6f, 0);

        SpriteRenderer srPuerta = puerta.AddComponent<SpriteRenderer>();
        srPuerta.sprite = spPortonHierro;
        srPuerta.drawMode = SpriteDrawMode.Sliced;
        srPuerta.size = new Vector2(3.6f, 2.4f);
        srPuerta.color = new Color(0.40f, 0.40f, 0.45f, 1f);
        srPuerta.sortingOrder = 10;

        BoxCollider2D colPuerta = puerta.AddComponent<BoxCollider2D>();
        colPuerta.size = new Vector2(3.6f, 1.4f);
        colPuerta.isTrigger = false;

        puerta.AddComponent<PuertaCapillaNivel4>();

        // Sarcófagos y criptas familiares en las alas exteriores del atrio
        CrearSarcofagoEscondite(capilla, "Cripta de los Benefactores de Guanajuato", new Vector3(-8.8f, -4.8f, 0));
        CrearSarcofagoEscondite(capilla, "Mausoleo de Cantera del Párroco", new Vector3(8.8f, -4.8f, 0));

        // (El Libro Parroquial de 1865 ahora se ubica en el Atrio Sur-Este del mapa)

        // Árboles y cipreses góticos decorativos en el exterior
        CrearArbolGoticoDecorativo(capilla, new Vector3(-15.0f, -6.5f, 0), spArbolMuerto1);
        CrearArbolGoticoDecorativo(capilla, new Vector3(15.0f, -6.5f, 0), spArbolMuerto2);
        CrearCipresDecorativo(capilla, new Vector3(-18.5f, -2.5f, 0));
        CrearCipresDecorativo(capilla, new Vector3(18.5f, -2.5f, 0));
        CrearBrumaGotica(capilla, new Vector3(0f, -6.5f, 0), new Vector3(4.5f, 2.8f, 1f));

        // Construir el Recinto Interior Sagrado de la Capilla
        ConstruirRecintoInteriorCapilla(root);
    }

    // =========================================================================
    // 8.1 RECINTO SAGRADO INTERIOR DE LA CAPILLA MAYOR (Y = 160f)
    // =========================================================================
    void ConstruirRecintoInteriorCapilla(GameObject root)
    {
        GameObject interior = new GameObject("Recinto_Interior_CapillaMayor");
        interior.transform.SetParent(root.transform);
        interior.transform.position = new Vector3(0f, 160f, 0);

        // Suelo de losas de cantera de la catedral interior
        GameObject sueloInterior = new GameObject("SueloInteriorCatedral");
        sueloInterior.transform.SetParent(interior.transform, false);
        sueloInterior.transform.localPosition = Vector3.zero;
        SpriteRenderer srSuelo = sueloInterior.AddComponent<SpriteRenderer>();
        srSuelo.sprite = spCaminoPiedra;
        srSuelo.drawMode = SpriteDrawMode.Tiled;
        srSuelo.size = new Vector2(11.4f, 19.5f);
        srSuelo.color = new Color(0.65f, 0.65f, 0.72f, 1f); // Tono solemne de piedra de templo
        srSuelo.sortingOrder = 3;

        // Pasillo central ceremonial de mármol y oro hacia el altar
        GameObject pasillo = new GameObject("PasilloCeremonialInterior");
        pasillo.transform.SetParent(interior.transform, false);
        pasillo.transform.localPosition = Vector3.zero;
        SpriteRenderer srPas = pasillo.AddComponent<SpriteRenderer>();
        srPas.sprite = spCaminoPiedra;
        srPas.drawMode = SpriteDrawMode.Tiled;
        srPas.size = new Vector2(2.8f, 18.5f);
        srPas.color = new Color(0.92f, 0.82f, 0.62f, 1f);
        srPas.sortingOrder = 4;

        // Muros perimetrales interiores de cantera gótica (Habitación sagrada cerrada)
        GameObject murosInt = new GameObject("MurosInteriorCapilla");
        murosInt.transform.SetParent(interior.transform, false);

        // Muro Norte (Cabecera y Presbiterio)
        CrearMuroGavetas(murosInt, "MuroInt_Norte", 0f, 9.8f, 12.6f, 1.8f);

        // Muro Sur (Pared del Portal con vano de entrada)
        CrearMuroGavetas(murosInt, "MuroInt_Sur_Izq", -3.6f, -9.8f, 4.4f, 1.8f);
        CrearMuroGavetas(murosInt, "MuroInt_Sur_Der", 3.6f, -9.8f, 4.4f, 1.8f);
        // Collider para cerrar el fondo sur una vez dentro
        BoxCollider2D colSurCierre = murosInt.AddComponent<BoxCollider2D>();
        colSurCierre.size = new Vector2(12.6f, 1.4f);
        colSurCierre.offset = new Vector2(0f, -10.0f);
        colSurCierre.isTrigger = false;

        // Muros Laterales Oeste y Este
        CrearMuroGavetas(murosInt, "MuroInt_Oeste", -6.0f, 0f, 1.6f, 20.0f);
        CrearMuroGavetas(murosInt, "MuroInt_Este", 6.0f, 0f, 1.6f, 20.0f);

        // Filas de columnas y arquerías góticas a los costados del pasillo
        float[] alturasColumnas = new float[] { -6.5f, -2.5f, 1.5f, 5.5f };
        for (int i = 0; i < alturasColumnas.Length; i++)
        {
            float yCol = alturasColumnas[i];
            CrearPilarGoticoNave(interior, new Vector3(-4.0f, yCol, 0));
            CrearPilarGoticoNave(interior, new Vector3(4.0f, yCol, 0));

            // Faroles de gas en los pilares iluminando la nave
            CrearFarolGas(interior, new Vector3(-4.0f, yCol + 0.9f, 0));
            CrearFarolGas(interior, new Vector3(4.0f, yCol + 0.9f, 0));
        }

        // Altar Mayor Sagrado al fondo del presbiterio (Y = 7.8f)
        GameObject altarMayor = new GameObject("AltarMayorPresbiterio");
        altarMayor.transform.SetParent(interior.transform, false);
        altarMayor.transform.localPosition = new Vector3(0f, 7.8f, 0);
        SpriteRenderer srAltar = altarMayor.AddComponent<SpriteRenderer>();
        srAltar.sprite = spAltarCirios;
        srAltar.sortingOrder = 6;
        altarMayor.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

        BoxCollider2D colAltar = altarMayor.AddComponent<BoxCollider2D>();
        colAltar.size = new Vector2(2.4f, 1.4f);
        colAltar.isTrigger = false;

        // Luz sacra dorada sobre el Altar Mayor
        GameObject luzAltar = new GameObject("LuzAltarMayor");
        luzAltar.transform.SetParent(altarMayor.transform, false);
        Light2D lAlt = luzAltar.AddComponent<Light2D>();
        lAlt.lightType = Light2D.LightType.Point;
        lAlt.color = new Color(1f, 0.90f, 0.50f, 1f);
        lAlt.intensity = 2.4f;
        lAlt.pointLightInnerRadius = 1.0f;
        lAlt.pointLightOuterRadius = 7.5f;

        // =====================================================================
        // LA CAMPANA MAYOR DEL ALBA (EN EL CENTRO DE LA CAPILLA INTERIOR)
        // =====================================================================
        GameObject campana = new GameObject("CampanaMayor_Alba");
        campana.transform.SetParent(interior.transform, false);
        campana.transform.localPosition = new Vector3(0f, 0.5f, 0); // Centro de la nave interior
        transformCampanaMayor = campana.transform;

        // Visual de la Campana de Bronce suspendida en la nave
        GameObject visualCampana = new GameObject("CampanaBronceVisual");
        visualCampana.transform.SetParent(campana.transform, false);
        visualCampana.transform.localPosition = new Vector3(0f, 2.2f, 0);
        SpriteRenderer srCamp = visualCampana.AddComponent<SpriteRenderer>();
        srCamp.sprite = spPortonHierro;
        srCamp.color = new Color(0.96f, 0.82f, 0.38f, 1f); // Bronce sagrado resplandeciente
        srCamp.sortingOrder = 11;
        visualCampana.transform.localScale = new Vector3(0.70f, 0.75f, 1f);

        // Cuerda dorada suspendida para que el jugador la toque directamente adentro
        GameObject cuerda = new GameObject("CuerdaCampana");
        cuerda.transform.SetParent(campana.transform, false);
        cuerda.transform.localPosition = new Vector3(0f, 0.6f, 0);
        SpriteRenderer srCuerda = cuerda.AddComponent<SpriteRenderer>();
        srCuerda.sprite = spVerjaHierro;
        srCuerda.color = new Color(0.98f, 0.90f, 0.50f, 1f); // Seda dorada
        srCuerda.sortingOrder = 12;
        cuerda.transform.localScale = new Vector3(0.22f, 2.6f, 1f);
        transformCuerdaCampana = cuerda.transform;

        BoxCollider2D colCamp = campana.AddComponent<BoxCollider2D>();
        colCamp.size = new Vector2(4.8f, 4.4f);
        colCamp.offset = new Vector2(0f, 0.3f);
        colCamp.isTrigger = true;

        ReliquiaItemNivel4 cScript = campana.AddComponent<ReliquiaItemNivel4>();
        cScript.tipo = ReliquiaItemNivel4.TipoInteractivo.CampanarioAlba;
        cScript.nombreMostrado = "Cuerda de la Campana Mayor del Alba (6:00 AM)";

        // Luz celestial dorada sobre la Campana y la Cuerda
        GameObject luzCampana = new GameObject("LuzCampanaMayor");
        luzCampana.transform.SetParent(campana.transform, false);
        luzCampana.transform.localPosition = new Vector3(0f, 1.2f, 0);
        Light2D lC = luzCampana.AddComponent<Light2D>();
        lC.lightType = Light2D.LightType.Point;
        lC.color = new Color(1f, 0.92f, 0.55f, 1f);
        lC.intensity = 2.6f;
        lC.pointLightInnerRadius = 1.4f;
        lC.pointLightOuterRadius = 11.0f;

        // Luz tenue ambiental para el recinto sagrado
        GameObject luzAmb = new GameObject("LuzAmbienteInterior");
        luzAmb.transform.SetParent(interior.transform, false);
        Light2D lInt = luzAmb.AddComponent<Light2D>();
        lInt.lightType = Light2D.LightType.Point;
        lInt.color = new Color(0.85f, 0.80f, 0.65f, 1f);
        lInt.intensity = 1.2f;
        lInt.pointLightInnerRadius = 3.0f;
        lInt.pointLightOuterRadius = 18.0f;
    }

    void CrearArbolGoticoDecorativo(GameObject parent, Vector3 posLocal, Sprite sp)
    {
        GameObject arb = new GameObject("ArbolGotico_Claustro");
        arb.transform.SetParent(parent.transform, false);
        arb.transform.localPosition = posLocal;

        SpriteRenderer sr = arb.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = 12;
        arb.transform.localScale = new Vector3(1.8f, 2.0f, 1f);

        BoxCollider2D col = arb.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.7f);
        col.offset = new Vector2(0f, -1.2f);
        col.isTrigger = false;
    }

    void CrearCipresDecorativo(GameObject parent, Vector3 posLocal)
    {
        GameObject cip = new GameObject("Cipres_Claustro");
        cip.transform.SetParent(parent.transform, false);
        cip.transform.localPosition = posLocal;

        SpriteRenderer sr = cip.AddComponent<SpriteRenderer>();
        sr.sprite = spCipres;
        sr.sortingOrder = 10;
        cip.transform.localScale = new Vector3(1.5f, 1.9f, 1f);

        BoxCollider2D col = cip.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);
        col.offset = new Vector2(0f, -0.9f);
        col.isTrigger = false;
    }

    void CrearBrumaGotica(GameObject parent, Vector3 posLocal, Vector3 escala)
    {
        GameObject b = new GameObject("BrumaGotica_Claustro");
        b.transform.SetParent(parent.transform, false);
        b.transform.localPosition = posLocal;

        SpriteRenderer sr = b.AddComponent<SpriteRenderer>();
        sr.sprite = spBrumaNiebla;
        sr.color = new Color(0.85f, 0.90f, 1f, 0.45f);
        sr.sortingOrder = 11;
        b.transform.localScale = escala;
    }

    // =========================================================================
    // 9. CAMPOSANTO: PARCELAS CON LÁPIDAS REALISTAS, OBELISCOS Y SARCÓFAGOS
    // =========================================================================
    void DistribuirTumbasYLapidas(GameObject root)
    {
        // 1. Cuadrante Suroeste (entre Calzada Central y Lateral Oeste)
        GenerarParcelaCementerio(root, -34f, -24f, 3, 3, 4.2f, 3.4f, 0);

        // 2. Cuadrante Sureste (entre Calzada Central y Lateral Este)
        GenerarParcelaCementerio(root, 22f, -24f, 3, 3, 4.2f, 3.4f, 2);

        // 3. Cuadrante Medio Oeste (frente a Mausoleo Leroy)
        GenerarParcelaCementerio(root, -36f, -6f, 3, 3, 4.0f, 3.4f, 1);

        // 4. Cuadrante Medio Este (frente a Gran Osario)
        GenerarParcelaCementerio(root, 24f, -6f, 3, 3, 4.0f, 3.4f, 3);

        // 5. Cuadrante Noroeste (ala norte exterior, sin invadir la calzada transversal Y = 18)
        GenerarParcelaCementerio(root, -34f, 10f, 3, 2, 4.0f, 3.4f, 4);

        // 6. Cuadrante Noreste (ala norte exterior, sin invadir la calzada transversal Y = 18)
        GenerarParcelaCementerio(root, 24f, 10f, 3, 2, 4.0f, 3.4f, 5);
    }

    void GenerarParcelaCementerio(GameObject root, float origenX, float origenY, int cols, int filas, float espacioX, float espacioY, int semilla)
    {
        int id = 0;
        for (int f = 0; f < filas; f++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = origenX + c * espacioX;
                float y = origenY + f * espacioY;
                int tipo = (semilla + id) % 5;

                // 1. Fosa abierta con tierra removida o fosa sellada en tierra
                GameObject fosa = new GameObject($"Fosa_{origenX}_{origenY}_{id}");
                fosa.transform.SetParent(root.transform);
                fosa.transform.position = new Vector3(x, y - 0.4f, 0);

                SpriteRenderer srFosa = fosa.AddComponent<SpriteRenderer>();
                srFosa.sprite = spFosaAbierta;
                srFosa.sortingOrder = -8;
                fosa.transform.localScale = new Vector3(1.3f, 1.1f, 1f);

                // 2. Lápida o monumento en la cabecera
                GameObject mon = new GameObject($"Monumento_{origenX}_{origenY}_{id}");
                mon.transform.SetParent(root.transform);
                mon.transform.position = new Vector3(x, y + 0.5f, 0);

                SpriteRenderer srM = mon.AddComponent<SpriteRenderer>();
                srM.sortingOrder = 5;

                BoxCollider2D colM = mon.AddComponent<BoxCollider2D>();
                colM.isTrigger = false;

                switch (tipo)
                {
                    case 0: // Cruz Celta Monumental
                        srM.sprite = spLapidaCruzCelta;
                        mon.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                        colM.size = new Vector2(0.9f, 1.4f);
                        colM.offset = new Vector2(0f, -0.1f);
                        break;
                    case 1: // Obelisco Gótico Tapered
                        srM.sprite = spLapidaObelisco;
                        mon.transform.localScale = new Vector3(1.2f, 1.3f, 1f);
                        colM.size = new Vector2(0.8f, 1.6f);
                        colM.offset = new Vector2(0f, -0.1f);
                        break;
                    case 2: // Sarcófago de Piedra
                        srM.sprite = spSarcofago;
                        mon.transform.localScale = new Vector3(1.3f, 1.1f, 1f);
                        colM.size = new Vector2(2.2f, 1.1f);
                        break;
                    case 3: // Lápida Quebrada hundida
                        srM.sprite = spLapidaQuebrada;
                        mon.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                        colM.size = new Vector2(0.9f, 0.8f);
                        break;
                    default: // Lápida con arco labrado y cruz
                        srM.sprite = spLapidaArco;
                        mon.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                        colM.size = new Vector2(0.9f, 1.1f);
                        colM.offset = new Vector2(0f, -0.1f);
                        break;
                }

                id++;
            }
        }
    }

    // =========================================================================
    // 10. CIPRESES GÓTICOS ESBELTOS Y FAROLES DE GAS VICTORIANOS
    // =========================================================================
    void DistribuirCipresesYFaroles(GameObject root)
    {
        Vector2[] posCipreses = new Vector2[]
        {
            // Borde de la Calzada Central
            new Vector2(-5.5f, -24f), new Vector2(5.5f, -24f),
            new Vector2(-5.5f, -12f), new Vector2(5.5f, -12f),
            new Vector2(-5.5f, 0f),   new Vector2(5.5f, 0f),
            new Vector2(-5.5f, 12f),  new Vector2(5.5f, 12f),

            // Perímetro Oeste y Este
            new Vector2(-42f, -22f), new Vector2(-42f, -6f), new Vector2(-42f, 10f),
            new Vector2(42f, -22f),  new Vector2(42f, -6f),  new Vector2(42f, 10f),

            // Atrio Capilla Norte
            new Vector2(-18f, 20f), new Vector2(18f, 20f),
            new Vector2(-21f, 36f), new Vector2(21f, 36f)
        };

        for (int i = 0; i < posCipreses.Length; i++)
        {
            GameObject cip = new GameObject($"CipresGotico_{i}");
            cip.transform.SetParent(root.transform);
            cip.transform.position = new Vector3(posCipreses[i].x, posCipreses[i].y, 0);

            SpriteRenderer sr = cip.AddComponent<SpriteRenderer>();
            sr.sprite = spCipres;
            sr.sortingOrder = 10;
            cip.transform.localScale = new Vector3(1.6f, 2.0f, 1f);

            BoxCollider2D col = cip.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.7f, 0.7f);
            col.offset = new Vector2(0f, -1.0f);
            col.isTrigger = false;
        }

        // Faroles victorianos en los laterales de la Calzada Central (vía 100% despejada al centro)
        CrearFarolGas(root, new Vector3(-4.2f, -25f, 0));
        CrearFarolGas(root, new Vector3(4.2f, -25f, 0));
        CrearFarolGas(root, new Vector3(-4.2f, -6f, 0));
        CrearFarolGas(root, new Vector3(4.2f, -6f, 0));
        CrearFarolGas(root, new Vector3(-4.2f, 16f, 0));
        CrearFarolGas(root, new Vector3(4.2f, 16f, 0));

        // Faroles en los cruces transversales
        CrearFarolGas(root, new Vector3(-16f, -16f, 0));
        CrearFarolGas(root, new Vector3(16f, -16f, 0));
        CrearFarolGas(root, new Vector3(-16f, 6f, 0));
        CrearFarolGas(root, new Vector3(16f, 6f, 0));
    }

    void CrearFarolGas(GameObject root, Vector3 pos)
    {
        GameObject farol = new GameObject("Farol_Gas_Gotico");
        farol.transform.SetParent(root.transform, false);
        farol.transform.localPosition = pos;

        SpriteRenderer sr = farol.AddComponent<SpriteRenderer>();
        sr.sprite = spFarolGas;
        sr.sortingOrder = 9;
        farol.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        BoxCollider2D colF = farol.AddComponent<BoxCollider2D>();
        colF.size = new Vector2(0.5f, 0.5f);
        colF.offset = new Vector2(0f, -0.4f);
        colF.isTrigger = false;

        GameObject luz = new GameObject("LuzGas");
        luz.transform.SetParent(farol.transform);
        luz.transform.localPosition = new Vector3(0f, 0.6f, 0);

        Light2D l = luz.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(1f, 0.84f, 0.42f, 1f);
        l.intensity = 1.15f;
        l.pointLightInnerRadius = 0.6f;
        l.pointLightOuterRadius = 8.0f;

        luz.AddComponent<LuzParpadeanteNivel4>();
    }

    void CrearObelisco(GameObject root, Vector3 pos)
    {
        GameObject ob = new GameObject("ObeliscoDoliente");
        ob.transform.SetParent(root.transform);
        ob.transform.position = pos;

        SpriteRenderer sr = ob.AddComponent<SpriteRenderer>();
        sr.sprite = spLapidaObelisco;
        sr.sortingOrder = 7;
        ob.transform.localScale = new Vector3(1.5f, 1.6f, 1f);

        BoxCollider2D col = ob.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.1f, 1.4f);
        col.offset = new Vector2(0f, -0.4f);
        col.isTrigger = false;
    }

    // =========================================================================
    // 11. TRAMPAS ACÚSTICAS (Hojas secas crujientes)
    // =========================================================================
    void DistribuirTrampasAcusticas(GameObject root)
    {
        if (spHojasSecas == null) spHojasSecas = CrearSpriteHojasSecas();

        // Hojas secas bajo cipreses (Centro, Sur y Oeste — con sprite de hojas reales, nunca spFosaAbierta)
        Vector2[] posHojas = new Vector2[]
        {
            new Vector2(-8f, -28f),  new Vector2(6f, -28f),
            new Vector2(-28f, -16f),
            new Vector2(-24f, 6f),
            new Vector2(0f, 2f)
        };

        for (int i = 0; i < posHojas.Length; i++)
        {
            GameObject hj = new GameObject($"HojasSecasCipres_{i}");
            hj.transform.SetParent(root.transform);
            hj.transform.position = new Vector3(posHojas[i].x, posHojas[i].y, 0);
            hj.transform.rotation = Quaternion.Euler(0f, 0f, (i * 67f) % 360f);

            SpriteRenderer sr = hj.AddComponent<SpriteRenderer>();
            sr.sprite = spHojasSecas;
            sr.color = Color.white;
            sr.sortingOrder = 1;
            hj.transform.localScale = new Vector3(1.35f, 1.2f, 1f);

            BoxCollider2D col = hj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 1.15f);
            col.isTrigger = true;

            TrampaAcusticaNivel4 trampa = hj.AddComponent<TrampaAcusticaNivel4>();
            trampa.tipo = TrampaAcusticaNivel4.TipoTrampa.HojasSecas;
        }

        // Hojas secas en la sección de La Que Escucha (Sector Este - Gran Osario)
        Vector2[] posHojasEste = new Vector2[]
        {
            new Vector2(16f, -3f),
            new Vector2(20f, -12f),
            new Vector2(24f, -6f),
            new Vector2(28f, 2.2f),
            new Vector2(15f, 12f),
            new Vector2(22f, 2f)
        };

        for (int i = 0; i < posHojasEste.Length; i++)
        {
            GameObject hj = new GameObject($"HojasSecasEste_{i}");
            hj.transform.SetParent(root.transform);
            hj.transform.position = new Vector3(posHojasEste[i].x, posHojasEste[i].y, 0);
            hj.transform.rotation = Quaternion.Euler(0f, 0f, (i * 53f) % 360f);

            SpriteRenderer sr = hj.AddComponent<SpriteRenderer>();
            sr.sprite = spHojasSecas;
            sr.color = Color.white;
            sr.sortingOrder = 1;
            hj.transform.localScale = new Vector3(1.55f, 1.35f, 1f);

            BoxCollider2D col = hj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.45f, 1.15f);
            col.isTrigger = true;

            TrampaAcusticaNivel4 trampa = hj.AddComponent<TrampaAcusticaNivel4>();
            trampa.tipo = TrampaAcusticaNivel4.TipoTrampa.HojasSecas;
        }
    }

    Sprite CrearSpriteHojasSecas()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int p = 0; p < pixels.Length; p++) pixels[p] = new Color(0f, 0f, 0f, 0f);

        Color[] paleta = new Color[]
        {
            new Color(0.74f, 0.48f, 0.16f, 0.96f), // Ocre dorado seco
            new Color(0.65f, 0.34f, 0.12f, 0.96f), // Ámbar tostado
            new Color(0.52f, 0.27f, 0.10f, 0.96f), // Marrón hoja muerta
            new Color(0.80f, 0.60f, 0.22f, 0.94f), // Amarillo paja seco
            new Color(0.56f, 0.22f, 0.09f, 0.96f)  // Rojizo otoño
        };

        System.Random rng = new System.Random(4021);
        int totalHojas = 28;
        for (int h = 0; h < totalHojas; h++)
        {
            // Distribución orgánica en elipse centrada
            double aRnd = rng.NextDouble() * Mathf.PI * 2.0;
            double rNorm = System.Math.Sqrt(rng.NextDouble());
            float cx = 64f + (float)(System.Math.Cos(aRnd) * rNorm * 40.0);
            float cy = 64f + (float)(System.Math.Sin(aRnd) * rNorm * 32.0);
            float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float len = 10.5f + (float)rng.NextDouble() * 5.5f;
            float wid = 5.4f + (float)rng.NextDouble() * 2.6f;
            Color colBase = paleta[h % paleta.Length];
            Color colBorde = new Color(colBase.r * 0.48f, colBase.g * 0.42f, colBase.b * 0.35f, 0.98f);
            Color colNervio = new Color(colBase.r * 0.62f, colBase.g * 0.54f, colBase.b * 0.42f, 0.98f);

            float cosA = Mathf.Cos(ang);
            float sinA = Mathf.Sin(ang);
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - len - 3));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(cx + len + 3));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - len - 3));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(cy + len + 3));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float u = dx * cosA + dy * sinA;
                    float v = -dx * sinA + dy * cosA;

                    // Tallo curvo corto sobresaliendo de la base de la hoja
                    if (u >= -len - 3.2f && u < -len * 0.80f && Mathf.Abs(v) <= 0.80f)
                    {
                        pixels[y * size + x] = colBorde;
                        continue;
                    }

                    float uNorm = Mathf.Abs(u) / len;
                    if (uNorm > 1f) continue;

                    // Silueta orgánica redondeada de hoja de roble/otoño (sin puntas rígidas tipo cristal)
                    float ondaLobulo = 0.84f + 0.16f * Mathf.Cos(u * 0.85f);
                    float ovalo = Mathf.Sqrt(Mathf.Max(0f, 1f - uNorm * uNorm));
                    float anchoEnU = wid * ovalo * ondaLobulo;
                    if (anchoEnU <= 0.15f) continue;

                    float distRel = Mathf.Abs(v) / anchoEnU;
                    if (distRel <= 1f)
                    {
                        Color c = colBase;
                        if (distRel > 0.78f) c = colBorde;
                        else if (Mathf.Abs(v) < 0.72f && uNorm < 0.82f) c = colNervio;
                        pixels[y * size + x] = c;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    // =========================================================================
    // 12. ZONAS DE SILENCIO (Desafío del micrófono)
    // =========================================================================
    void DistribuirZonasSilencio(GameObject root)
    {
        Vector2[] posZonas = new Vector2[]
        {
            new Vector2(-16f, -8f),  // Pasaje Criptas Oeste
            new Vector2(16f, -8f),   // Pasaje Catacumbas Este
            new Vector2(0f, 8f),     // Cruce Norte de las Ánimas
            new Vector2(-28f, 10f),  // Portal de Entrada al Mausoleo Leroy
            new Vector2(28f, 10f)    // Portal de Entrada al Gran Osario
        };

        for (int i = 0; i < posZonas.Length; i++)
        {
            GameObject z = new GameObject($"ZonaSilencioAliento_{i}");
            z.transform.SetParent(root.transform);
            z.transform.position = new Vector3(posZonas[i].x, posZonas[i].y, 0);

            BoxCollider2D col = z.AddComponent<BoxCollider2D>();
            col.size = new Vector2(5.0f, 5.0f);
            col.isTrigger = true;

            ZonaContenerAlientoNivel4 zScript = z.AddComponent<ZonaContenerAlientoNivel4>();
            zScript.nombreZona = $"Pasaje del Silencio {i + 1}";
        }
    }

    // =========================================================================
    // 13. CAPAS DE BRUMA Y NIEBLA ESPECTRAL (SpriteRenderer, CERO partículas)
    // =========================================================================
    void DistribuirCapasBruma(GameObject root)
    {
        Vector2[] posBruma = new Vector2[]
        {
            new Vector2(0f, -28f), new Vector2(-18f, -22f), new Vector2(18f, -22f),
            new Vector2(0f, -14f), new Vector2(-28f, -14f), new Vector2(28f, -14f),
            new Vector2(0f, 0f),   new Vector2(-20f, 4f),   new Vector2(20f, 4f),
            new Vector2(0f, 16f),  new Vector2(-16f, 26f),  new Vector2(16f, 26f)
        };

        for (int i = 0; i < posBruma.Length; i++)
        {
            GameObject b = new GameObject($"BrumaEspectral_{i}");
            b.transform.SetParent(root.transform);
            b.transform.position = new Vector3(posBruma[i].x, posBruma[i].y, 0);

            SpriteRenderer sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = spBrumaNiebla;
            sr.sortingOrder = 11;
            b.transform.localScale = new Vector3(4.5f, 3.5f, 1f);
        }
    }

    // =========================================================================
    // UTILIDADES DE CONSTRUCCIÓN
    // =========================================================================
    void CrearSuperficieTiled(GameObject root, string nombre, float x, float y, float w, float h, Sprite sp, Color color, int sorting)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(root.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.color = color;
        sr.sortingOrder = sorting;
    }

    void CrearMuroGavetas(GameObject root, string nombre, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spMuroGavetas;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.sortingOrder = 6;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);
        col.isTrigger = false;
    }

    void CrearSarcofagoEscondite(GameObject root, string nombre, Vector3 posLocal)
    {
        GameObject sarcofago = new GameObject("Sarcofago_" + nombre);
        sarcofago.transform.SetParent(root.transform);
        sarcofago.transform.localPosition = posLocal;

        SpriteRenderer sr = sarcofago.AddComponent<SpriteRenderer>();
        sr.sprite = spSarcofago;
        sr.sortingOrder = 6;
        sarcofago.transform.localScale = new Vector3(1.6f, 1.4f, 1f);

        BoxCollider2D colSolido = sarcofago.AddComponent<BoxCollider2D>();
        colSolido.size = new Vector2(2.4f, 1.4f);
        colSolido.isTrigger = false;

        GameObject trigObj = new GameObject("TriggerEscondite");
        trigObj.transform.SetParent(sarcofago.transform, false);
        BoxCollider2D colTrig = trigObj.AddComponent<BoxCollider2D>();
        colTrig.size = new Vector2(3.2f, 2.6f);
        colTrig.isTrigger = true;

        EsconditeNivel4 escScript = trigObj.AddComponent<EsconditeNivel4>();
        escScript.nombreEscondite = nombre;
    }

    void DistribuirTodasLasMomias(GameObject root)
    {
        // 1. Carmen "La Bruja" (La Momia Encadenada - patrulla todo el panteón)
        MomiaEncadenadaController momia = FindFirstObjectByType<MomiaEncadenadaController>();
        if (momia == null)
        {
            GameObject mGo = new GameObject("MomiaEncadenada");
            mGo.transform.position = new Vector3(0f, -1.5f, 0);
            mGo.transform.localScale = new Vector3(1.58f, 1.58f, 1f);
            mGo.AddComponent<SpriteRenderer>();
            momia = mGo.AddComponent<MomiaEncadenadaController>();
        }
        else
        {
            momia.transform.position = new Vector3(0f, -1.5f, 0);
            momia.transform.localScale = new Vector3(1.58f, 1.58f, 1f);
        }
        momia.momiaDetenida = false;

        // 2. Momia del Nivel 1 (La Que Camina - confinada al Sector Oeste / Mausoleo Leroy, despierta a las 04:00 AM)
        MomiaNivel1Seccion momiaN1 = FindFirstObjectByType<MomiaNivel1Seccion>();
        if (momiaN1 == null)
        {
            GameObject n1Go = new GameObject("MomiaNivel1_LaQueCamina");
            n1Go.transform.position = new Vector3(-18f, 0f, 0);
            n1Go.transform.localScale = new Vector3(1.55f, 1.55f, 1f);
            n1Go.AddComponent<SpriteRenderer>();
            momiaN1 = n1Go.AddComponent<MomiaNivel1Seccion>();
        }
        else
        {
            momiaN1.transform.position = new Vector3(-18f, 0f, 0);
            momiaN1.transform.localScale = new Vector3(1.55f, 1.55f, 1f);
        }
        momiaN1.nombreSeccion = "Sector Oeste (Mausoleo Leroy)";
        momiaN1.horaAparicion = 4;
        momiaN1.minutoAparicion = 0;
        momiaN1.zonaMin = new Vector2(-36f, -14f);
        momiaN1.zonaMax = new Vector2(-10f, 15f);

        // 3. Momia del Nivel 2 (La Que Escucha - confinada al Sector Este / Gran Osario, despierta a las 04:00 AM)
        MomiaNivel2Seccion momiaN2 = FindFirstObjectByType<MomiaNivel2Seccion>();
        if (momiaN2 == null)
        {
            GameObject n2Go = new GameObject("MomiaNivel2_LaQueEscucha");
            n2Go.transform.position = new Vector3(18f, 0f, 0);
            n2Go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            n2Go.AddComponent<SpriteRenderer>();
            momiaN2 = n2Go.AddComponent<MomiaNivel2Seccion>();
        }
        else
        {
            momiaN2.transform.position = new Vector3(18f, 0f, 0);
            momiaN2.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        }
        momiaN2.nombreSeccion = "Sector Este (Gran Osario)";
        momiaN2.horaAparicion = 4;
        momiaN2.minutoAparicion = 0;
        momiaN2.zonaMin = new Vector2(10f, -14f);
        momiaN2.zonaMax = new Vector2(36f, 15f);

        // Mantener eliminadas las momias invasoras genéricas previas
        MomiaInvasoraNivel4[] previas = FindObjectsByType<MomiaInvasoraNivel4>(FindObjectsSortMode.None);
        for (int i = 0; i < previas.Length; i++)
        {
            if (previas[i] != null)
            {
                if (Application.isPlaying) Destroy(previas[i].gameObject);
                else DestroyImmediate(previas[i].gameObject);
            }
        }
    }

    void DistribuirCiriosBenditos(GameObject root)
    {
        GameObject grupoCirios = new GameObject("CiriosBenditos_Camposanto");
        grupoCirios.transform.SetParent(root.transform);

        // 1. Altar Suroeste (Avenida Transversal Sur, camino al Mausoleo Leroy)
        CrearAltarCirioBendito(grupoCirios, "Altar_Cirio_Suroeste", new Vector3(-16f, -16f, 0));

        // 2. Altar Sureste (Avenida Transversal Sur, camino al Gran Osario)
        CrearAltarCirioBendito(grupoCirios, "Altar_Cirio_Sureste", new Vector3(16f, -16f, 0));

        // 3. Altar Central (Glorieta de la Cruz, frente al Atrio de la Capilla)
        CrearAltarCirioBendito(grupoCirios, "Altar_Cirio_Central", new Vector3(0f, 12f, 0));
    }

    void CrearAltarCirioBendito(GameObject parent, string nombre, Vector3 pos)
    {
        GameObject altar = new GameObject(nombre);
        altar.transform.SetParent(parent.transform);
        altar.transform.position = pos;

        SpriteRenderer sr = altar.AddComponent<SpriteRenderer>();
        sr.sprite = spPebeteroCirio != null ? spPebeteroCirio : spAltarCirios;
        sr.sortingOrder = 8;
        altar.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        // Colisión física sólida para no atravesar la cantera del pebetero
        BoxCollider2D colSolido = altar.AddComponent<BoxCollider2D>();
        colSolido.size = new Vector2(1.1f, 0.9f);
        colSolido.offset = new Vector2(0f, -0.1f);
        colSolido.isTrigger = false;

        // Script de Cirio Bendito con zona segura que repele momias
        CirioBenditoNivel4 cirio = altar.AddComponent<CirioBenditoNivel4>();
        cirio.nombreAltar = "Cirio Bendito de Santa Paula";
        cirio.duracionFuego = 15f;
        cirio.radioProteccion = 5.8f;

        // Llama visual del pebetero (apagada por defecto)
        GameObject goLlama = new GameObject("LlamaVisual");
        goLlama.transform.SetParent(altar.transform, false);
        goLlama.transform.localPosition = new Vector3(0f, 0.45f, 0);
        SpriteRenderer srLlama = goLlama.AddComponent<SpriteRenderer>();
        srLlama.sprite = spFarolGas;
        srLlama.color = new Color(1.0f, 0.80f, 0.25f, 1f);
        srLlama.sortingOrder = 10;
        srLlama.enabled = false;
    }
}

/// <summary>
/// Sensor trigger interior de la Capilla del Descanso.
/// Al entrar el jugador a la nave para ofrendar en el Altar Mayor, atenúa suavemente la opacidad
/// de la fachada exterior monumental para permitir una visibilidad diáfana del interior y del Altar.
/// Al salir, restaura la opacidad completa.
/// </summary>
public class DetectorInteriorCapilla : MonoBehaviour
{
    public SpriteRenderer srFachada;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            if (srFachada != null)
            {
                srFachada.color = new Color(1f, 1f, 1f, 0.38f);
            }

            if (Nivel4Manager.Instance != null)
            {
                Nivel4Manager.Instance.EjecutarAnimacionEntradaCapilla();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (srFachada != null && (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null))
        {
            if (srFachada.color.a > 0.40f)
            {
                srFachada.color = new Color(1f, 1f, 1f, 0.38f);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (srFachada != null && (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null))
        {
            srFachada.color = Color.white;
        }
    }
}
