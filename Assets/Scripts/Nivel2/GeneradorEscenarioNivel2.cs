using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Marcador de versión para asegurar que el escenario subterráneo se reconstruya automáticamente
/// cuando el diseño cambie, sin requerir intervención manual del usuario.
/// </summary>
public class MarcadorVersionSubterraneoNivel2 : MonoBehaviour
{
    public int version = 0;
}

/// <summary>
/// Generador del Escenario Subterráneo para el Nivel 2: "Las Criptas y Sala de Máquinas".
/// - Suelo base continuo y unificado: ZERO agujeros negros ni vacíos.
/// - Conexión total entre salas con puertas y umbrales claros.
/// - Rejas mecánicas herméticas de pared a pared (imposible rodearlas).
/// - Sprites procedurales fotorrealistas para Charcos de Agua (bordes orgánicos, reflejos) y Cristales Rotos (esquirlas traslúcidas afiladas).
/// - Ratas con pixel-art detallado y visible desde la vista de escena.
/// - Sonidos acústicos realistas (chapoteo de agua denso, crujido de vidrio, reja chirriante en estéreo 2D directo).
/// TOTALMENTE INDEPENDIENTE DEL NIVEL 1.
/// </summary>
[ExecuteAlways]
public class GeneradorEscenarioNivel2 : MonoBehaviour
{
    public const int VERSION_LAYOUT_ACTUAL = 13;

    private Sprite floorSprite;
    private Sprite wallSprite;
    private Sprite crateSprite;
    private Sprite doorSprite;
    private Sprite pillarSprite;
    private Sprite archSprite;
    private Sprite blankSprite;

    private Sprite spriteCharcoAgua;
    private Sprite spriteCristalesRotos;
    private Sprite spriteRataPixelArt;
    private Sprite spriteRejaMecanica;

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
        GameObject existente = GameObject.Find("SubterraneoNivel2");
        if (existente != null)
        {
            MarcadorVersionSubterraneoNivel2 marcador = existente.GetComponent<MarcadorVersionSubterraneoNivel2>();
            if (marcador != null && marcador.version == VERSION_LAYOUT_ACTUAL && existente.transform.childCount > 20)
            {
                return; // Ya está actualizado con la versión correcta
            }
        }

        CargarRecursosYSpritesProcedurales();
        ConstruirComplejoSubterraneo();
    }

    [ContextMenu("Reconstruir Subterráneo Nivel 2")]
    public void ReconstruirManual()
    {
        CargarRecursosYSpritesProcedurales();
        ConstruirComplejoSubterraneo();
    }

    private void CargarRecursosYSpritesProcedurales()
    {
        floorSprite = Resources.Load<Sprite>("Tiles/tile_floor");
        wallSprite = Resources.Load<Sprite>("Tiles/tile_wall");
        crateSprite = Resources.Load<Sprite>("Tiles/prop_crate");
        doorSprite = Resources.Load<Sprite>("Tiles/prop_door");
        pillarSprite = Resources.Load<Sprite>("Tiles/prop_pillar");
        archSprite = Resources.Load<Sprite>("Props/prop_arch");
        blankSprite = Resources.Load<Sprite>("Tiles/blank_square");

        if (blankSprite == null) blankSprite = CrearSpriteCuadrado();

        // Generar sprites detallados procedurales para trampas, ratas y rejas
        spriteCharcoAgua = GenerarSpriteCharcoRealista();
        spriteCristalesRotos = GenerarSpriteCristalesRealistas();
        spriteRataPixelArt = GenerarSpriteRataPixelArt();
        spriteRejaMecanica = GenerarSpriteRejaMecanica();
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

    public void ConstruirComplejoSubterraneo()
    {
        GameObject existente = GameObject.Find("SubterraneoNivel2");
        if (existente != null)
        {
            if (Application.isPlaying) Destroy(existente);
            else DestroyImmediate(existente);
        }

        GameObject root = new GameObject("SubterraneoNivel2");
        root.transform.position = Vector3.zero;

        MarcadorVersionSubterraneoNivel2 marcador = root.AddComponent<MarcadorVersionSubterraneoNivel2>();
        marcador.version = VERSION_LAYOUT_ACTUAL;

        // Paleta de colores atmosféricos del sótano colonial gótico
        Color colPisoBase = new Color(0.24f, 0.25f, 0.27f, 1f);      // Suelo de piedra colonial
        Color colPisoCalderas = new Color(0.27f, 0.23f, 0.21f, 1f);  // Zona de herrumbre y calderas
        Color colPisoCriptas = new Color(0.21f, 0.23f, 0.29f, 1f);   // Zona de losas fúnebres
        Color colPisoGenerador = new Color(0.25f, 0.28f, 0.28f, 1f); // Zona de alta tensión
        Color colMuroSolido = new Color(0.12f, 0.12f, 0.15f, 1f);    // Muros oscuros macizos

        // =========================================================================
        // 1. SUELO BASE TOTAL Y CONTINUO (ZERO ESPACIOS VACÍOS, ZERO HUECOS NEGROS)
        // =========================================================================
        // Capa base completa que cubre absolutamente todo el subsuelo (X: -26 a 26, Y: -20 a 22)
        CrearPiso(root, "PisoBase_SubterraneoTotal", 0f, 1f, 52f, 42f, colPisoBase, -2);

        // Capas temáticas diferenciadas por sectores sobre el suelo base
        CrearPiso(root, "PisoSector_Calderas", -16f, -3f, 18f, 24f, colPisoCalderas, -1);
        CrearPiso(root, "PisoSector_Criptas", 16f, -3f, 18f, 24f, colPisoCriptas, -1);
        CrearPiso(root, "PisoSector_GeneradorNorte", 0f, 14f, 46f, 14f, colPisoGenerador, -1);
        CrearPiso(root, "PisoSector_VestibuloSur", 0f, -14f, 14f, 12f, colPisoBase, -1);

        // =========================================================================
        // 2. MUROS PERIMETRALES EXTERIORES (HERMÉTICOS)
        // =========================================================================
        // Muro Sur completo
        CrearMuro(root, "Muro_Perimetro_Sur", 0f, -20.5f, 54f, 2.0f, colMuroSolido);

        // Muros Laterales Este y Oeste
        CrearMuro(root, "Muro_Perimetro_Oeste", -26.5f, 1f, 2.0f, 43f, colMuroSolido);
        CrearMuro(root, "Muro_Perimetro_Este", 26.5f, 1f, 2.0f, 43f, colMuroSolido);

        // Muro Norte con apertura central para la Gran Compuerta de Salida
        CrearMuro(root, "Muro_Perimetro_Norte_Izq", -16.5f, 22.5f, 21f, 2.0f, colMuroSolido);
        CrearMuro(root, "Muro_Perimetro_Norte_Der", 16.5f, 22.5f, 21f, 2.0f, colMuroSolido);
        CrearMuro(root, "Muro_Perimetro_Norte_Dintel", 0f, 24.2f, 12f, 2.0f, colMuroSolido);

        // =========================================================================
        // 3. MUROS INTERIORES HERMÉTICOS Y SELLADOS (IMPOSIBLE BYPASEAR REJAS)
        // =========================================================================
        // --- Sector Sur: Muro divisorio entre Vestíbulo Central y Calderas (X = -7.5) ---
        // Tramo Sur que delimita el vestíbulo dejando libre el paso hacia la reja (Y: -20.5 a -14.5)
        CrearMuro(root, "Muro_Div_Oeste_Sur", -7.5f, -17.5f, 1.8f, 6.0f, colMuroSolido);

        // Umbral horizontal a Calderas Sur (Y = -11):
        // Muro exterior oeste: X: -26.5 a -12.5 (ancho 14m, centro -19.5)
        CrearMuro(root, "Muro_Horiz_Calderas_Sur_Ext", -19.5f, -11f, 14f, 1.8f, colMuroSolido);
        // Muro conector interior: X: -8.5 a -6.6 (ancho 1.9m, centro -7.55)
        CrearMuro(root, "Muro_Horiz_Calderas_Sur_Int", -7.55f, -11f, 1.9f, 1.8f, colMuroSolido);
        // Apertura exacta de 4.0m entre X = -12.5 y X = -8.5 sellada herméticamente por REJA 1

        // --- Sector Sur: Muro divisorio entre Vestíbulo Central y Criptas (X = 7.5) ---
        // Tramo Sur que delimita el vestíbulo dejando libre el paso hacia la reja (Y: -20.5 a -14.5)
        CrearMuro(root, "Muro_Div_Este_Sur", 7.5f, -17.5f, 1.8f, 6.0f, colMuroSolido);

        // Umbral horizontal a Criptas Sur (Y = -11):
        CrearMuro(root, "Muro_Horiz_Criptas_Sur_Int", 7.55f, -11f, 1.9f, 1.8f, colMuroSolido);
        CrearMuro(root, "Muro_Horiz_Criptas_Sur_Ext", 19.5f, -11f, 14f, 1.8f, colMuroSolido);
        // Apertura exacta de 4.0m entre X = 8.5 y X = 12.5 sellada herméticamente por REJA 2

        // --- Muros divisorios macizos del Pasillo Central Troncal (Y: -11 a 8) ---
        // Pared Oeste continua SIN HUECOS NI HUECOS LATERALES (sella Calderas)
        CrearMuro(root, "Muro_Pasillo_Oeste_Solido", -7.5f, -1.5f, 1.8f, 19.0f, colMuroSolido);

        // Pared Este continua SIN HUECOS NI HUECOS LATERALES (sella Criptas)
        CrearMuro(root, "Muro_Pasillo_Este_Solido", 7.5f, -1.5f, 1.8f, 19.0f, colMuroSolido);

        // --- Muros divisorios Norte entre Salas y la Subestación (Y = 8) ---
        // Muro Norte de Calderas (Y = 8):
        CrearMuro(root, "Muro_Norte_Calderas_Ext", -19.5f, 8f, 14f, 1.8f, colMuroSolido);
        CrearMuro(root, "Muro_Norte_Calderas_Int", -7.55f, 8f, 1.9f, 1.8f, colMuroSolido);
        // Apertura exacta de 4.0m entre X = -12.5 y X = -8.5 sellada por REJA 3

        // Muro Norte de Criptas (Y = 8):
        CrearMuro(root, "Muro_Norte_Criptas_Int", 7.55f, 8f, 1.9f, 1.8f, colMuroSolido);
        CrearMuro(root, "Muro_Norte_Criptas_Ext", 19.5f, 8f, 14f, 1.8f, colMuroSolido);
        // Apertura exacta de 4.0m entre X = 8.5 y X = 12.5 sellada por REJA 4

        // Muro divisor entre Pasillo Central y Subestación (Y = 8):
        CrearMuro(root, "Muro_Pasillo_Norte_Izq", -4.8f, 8f, 3.6f, 1.8f, colMuroSolido);
        CrearMuro(root, "Muro_Pasillo_Norte_Der", 4.8f, 8f, 3.6f, 1.8f, colMuroSolido);
        // Apertura central de 6.0m entre X = -3.0 y X = 3.0 sellada por REJA CENTRAL 5

        // =========================================================================
        // 4. REJAS MECÁNICAS HERMÉTICAS (5 COMPUERTAS TÁCTICAS DISTRIBUIDAS)
        // =========================================================================
        // Reja 1: Acceso Sur a Calderas (Y = -11)
        CrearRejaHermetica(root, "Reja_Acceso_Calderas", new Vector2(-10.5f, -11f), 4.2f, 1.8f);

        // Reja 2: Acceso Sur a Criptas (Y = -11)
        CrearRejaHermetica(root, "Reja_Acceso_Criptas", new Vector2(10.5f, -11f), 4.2f, 1.8f);

        // Reja 3: Acceso Noroeste de Calderas a la Subestación Norte (Y = 8)
        CrearRejaHermetica(root, "Reja_Subestacion_Oeste", new Vector2(-10.5f, 8f), 4.2f, 1.8f);

        // Reja 4: Acceso Noreste de Criptas a la Subestación Norte (Y = 8)
        CrearRejaHermetica(root, "Reja_Subestacion_Este", new Vector2(10.5f, 8f), 4.2f, 1.8f);

        // Reja 5: Acceso Central del Pasillo Troncal a la Subestación Norte (Y = 8)
        CrearRejaHermetica(root, "Reja_Central_Subestacion", new Vector2(0f, 8f), 6.2f, 1.8f);

        // =========================================================================
        // 5. GRAN COMPUERTA DE SALIDA AL NIVEL 3 Y GENERADOR PRINCIPAL
        // =========================================================================
        // Generador Principal en el centro de la Subestación Norte
        GameObject genObj = CrearGeneradorCentral(root, 0f, 14f);
        GeneradorPrincipalNivel2 genScript = genObj.GetComponent<GeneradorPrincipalNivel2>();

        // Gran Compuerta de Salida en la pared Norte (X: 0, Y: 21.0)
        CrearGranCompuertaSalida(root, 0f, 21.0f, genScript);

        // =========================================================================
        // 6. LAS 3 CAJAS DE FUSIBLES (EN SALAS DIFERENTES, AMPLIAS Y VISIBLES)
        // =========================================================================
        // Fase 1: Sala de Calderas (Pared Oeste amplia, X: -21, Y: -2)
        CrearCajaFusible(root, -21.0f, -2.0f, 1, "Fase 1 (Calderas)", genScript);

        // Fase 2: Criptas y Osarios (Pared Este amplia, X: 21, Y: -2)
        CrearCajaFusible(root, 21.0f, -2.0f, 2, "Fase 2 (Criptas)", genScript);

        // Fase 3: Subestación Eléctrica Norte (Panel Noroeste, X: -16, Y: 16)
        CrearCajaFusible(root, -16.0f, 16.0f, 3, "Fase 3 (Subestación)", genScript);

        // =========================================================================
        // 7. ESCONDITES ESTRATÉGICOS (6 ESCONDITES REPARTIDOS EN TODAS LAS ÁREAS)
        // =========================================================================
        // 1. Casillero del Vigilante en el Vestíbulo Sur
        CrearEscondite(root, "Taquilla_Vestibulo", new Vector2(-4.5f, -17f), "Casillero del Vigilante", new Vector2(1.8f, 2.6f));

        // 2. Taquilla de Mantenimiento en Calderas Sur
        CrearEscondite(root, "Taquilla_Calderas", new Vector2(-24f, -4f), "Taquilla de Tuberías", new Vector2(1.8f, 2.6f));

        // 3. Ataúd Colonial Abierto en las Criptas Sur
        CrearEscondite(root, "Ataud_Criptas", new Vector2(24f, -7f), "Ataúd Funerario Abierto", new Vector2(2.4f, 2.2f));

        // 4. Casillero de Seguridad en la Subestación Norte
        CrearEscondite(root, "Taquilla_Subestacion", new Vector2(18f, 16f), "Gabinete Eléctrico Seguro", new Vector2(1.8f, 2.6f));

        // 5. Taquilla antigua en el Pasillo Central Troncal (Nuevo escondite)
        CrearEscondite(root, "Taquilla_PasilloCentral", new Vector2(5.8f, -1f), "Casillero de Herramientas", new Vector2(1.8f, 2.6f));

        // 6. Armario de repuestos en Calderas Norte (Nuevo escondite)
        CrearEscondite(root, "Taquilla_CalderasNorte", new Vector2(-24f, 13f), "Armario de Presión", new Vector2(1.8f, 2.6f));

        // =========================================================================
        // 8. RATAS SUBTERRÁNEAS (5 NIDOS PEQUEÑOS Y PROPORCIONALES)
        // =========================================================================
        // Rata 1: Drenaje sur de Calderas
        CrearRataPixelArt(root, "Rata_Drenaje_Calderas", new Vector2(-22f, -17f));

        // Rata 2: Tuberías altas de Calderas
        CrearRataPixelArt(root, "Rata_Tuberias_Calderas", new Vector2(-23f, 4f));

        // Rata 3: Osario de las Criptas
        CrearRataPixelArt(root, "Rata_Osario_Criptas", new Vector2(22f, 4f));

        // Rata 4: Tumbas profundas de las Criptas
        CrearRataPixelArt(root, "Rata_Tumbas_Criptas", new Vector2(24f, -15f));

        // Rata 5: Cables de la Subestación Norte
        CrearRataPixelArt(root, "Rata_Subestacion_Norte", new Vector2(14f, 18f));

        // =========================================================================
        // 9. TRAMPAS ACÚSTICAS REALISTAS (CHARCOS ORGÁNICOS Y CRISTALES AFILADOS)
        // =========================================================================
        // Charco 1: En el Pasillo Central
        CrearTrampaConSprite(root, "Charco_Pasillo_Central", TipoTrampaAcustica.CharcoAgua, new Vector2(0f, -4f), new Vector2(5.5f, 3.2f), spriteCharcoAgua);

        // Charco 2: Fuga de agua en Calderas
        CrearTrampaConSprite(root, "Charco_Fuga_Calderas", TipoTrampaAcustica.CharcoAgua, new Vector2(-15f, -15f), new Vector2(4.8f, 2.8f), spriteCharcoAgua);

        // Cristales Rotos 1: Vitrina destruida en las Criptas
        CrearTrampaConSprite(root, "Cristales_Criptas", TipoTrampaAcustica.CristalesRotos, new Vector2(16f, -4f), new Vector2(5.0f, 3.5f), spriteCristalesRotos);

        // Cristales Rotos 2: Focos rotos en la entrada a la Subestación Norte
        CrearTrampaConSprite(root, "Cristales_Subestacion", TipoTrampaAcustica.CristalesRotos, new Vector2(-9f, 11f), new Vector2(4.2f, 2.8f), spriteCristalesRotos);

        // =========================================================================
        // 10. MOBILIARIO Y AMBIENTACIÓN GÓTICA (CON PASO TOTALMENTE DESPEJADO)
        // =========================================================================
        // Columnas góticas decorativas pegadas a muros (sin collider para no estrechar los pasillos)
        CrearPilar(root, -6.6f, 0f, false);
        CrearPilar(root, 6.6f, 0f, false);
        CrearPilar(root, -6.6f, -7f, false);
        CrearPilar(root, 6.6f, -7f, false);

        // Arcos de piedra en las entradas a la Subestación
        CrearArco(root, -10.5f, 7.5f);
        CrearArco(root, 10.5f, 7.5f);

        // Pilas de cajas bien pegadas a muros
        CrearPilaCajas(root, -24.5f, 2f, 1.6f);
        CrearPilaCajas(root, 24.5f, 0f, 1.6f);

        // Tubería de vapor ambiental en Calderas
        CrearVaporAmbiental(root, new Vector2(-15f, 2f));

        // Arco eléctrico con chispas en la Subestación
        CrearCableChispas(root, new Vector2(-6f, 18f));

        // Rejillas de alcantarillado subterráneas de alta visibilidad con resplandor ambiental
        SistemaMomiaYMicrofonoNivel2.AsegurarTodasLasAlcantarillas(root.transform);

        Debug.Log("<color=green>[GeneradorEscenarioNivel2] ¡Escenario subterráneo reconstruido con éxito (Versión " + VERSION_LAYOUT_ACTUAL + ")!</color>");
    }

    private void CrearPiso(GameObject padre, string nombre, float x, float y, float w, float h, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = floorSprite != null ? floorSprite : blankSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    private void CrearMuro(GameObject padre, string nombre, float x, float y, float w, float h, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = wallSprite != null ? wallSprite : blankSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.color = color;
        sr.sortingOrder = 5;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);
    }

    private void CrearRejaHermetica(GameObject padre, string nombre, Vector2 pos, float ancho, float alto)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(pos.x, pos.y, 0);

        // Visual de la reja con barrotes metálicos y óxido
        GameObject rejaVisual = new GameObject("RejaVisual");
        rejaVisual.transform.SetParent(go.transform);
        rejaVisual.transform.localPosition = Vector3.zero;

        SpriteRenderer sr = rejaVisual.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRejaMecanica != null ? spriteRejaMecanica : (wallSprite != null ? wallSprite : blankSprite);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(ancho, alto + 0.4f);
        sr.color = Color.white;
        sr.sortingOrder = 6;

        // Manivela / mecanismo de elevación
        GameObject manivela = new GameObject("ManivelaReja");
        manivela.transform.SetParent(go.transform);
        manivela.transform.localPosition = new Vector3(-ancho * 0.45f, 0f, 0f);
        SpriteRenderer srManivela = manivela.AddComponent<SpriteRenderer>();
        srManivela.sprite = blankSprite;
        srManivela.color = new Color(0.75f, 0.65f, 0.45f);
        manivela.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        srManivela.sortingOrder = 7;

        // Collider sólido de bloqueo situado directamente en el GameObject de la reja
        BoxCollider2D colBloqueo = go.AddComponent<BoxCollider2D>();
        colBloqueo.size = new Vector2(ancho, alto);
        colBloqueo.isTrigger = false;

        // Trigger amplio de interacción en un hijo dedicado
        GameObject triggerObj = new GameObject("TriggerInteraccion");
        triggerObj.transform.SetParent(go.transform);
        triggerObj.transform.localPosition = Vector3.zero;
        BoxCollider2D trigger = triggerObj.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(ancho + 2.4f, alto + 3.2f);
        trigger.isTrigger = true;

        PuertaRejaMecanicaNivel2 rejaScript = go.AddComponent<PuertaRejaMecanicaNivel2>();
        rejaScript.rejaPuerta = rejaVisual.transform;
        rejaScript.manivelaSr = srManivela;
        rejaScript.colisionBloqueo = colBloqueo;
        rejaScript.distanciaInteraccion = 3.6f;
    }

    private void CrearGranCompuertaSalida(GameObject padre, float x, float y, GeneradorPrincipalNivel2 gen)
    {
        GameObject go = new GameObject("CompuertaSalida_Nivel3");
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = doorSprite != null ? doorSprite : blankSprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(6.0f, 3.8f);
        sr.color = new Color(0.9f, 0.35f, 0.35f, 1f); // Roja mientras no hay electricidad
        sr.sortingOrder = 6;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(8.5f, 6.0f);
        col.offset = new Vector2(0f, -0.8f);
        col.isTrigger = true;

        CompuertaSalidaNivel2 comp = go.AddComponent<CompuertaSalidaNivel2>();
        comp.compuertaSr = sr;
        comp.compuertaCerradaSprite = sr.sprite;
        comp.distanciaInteraccion = 6.0f;
        comp.luzAcceso = CrearLuzPunto(go, new Vector3(0, 1.4f, 0), Color.red, 2.2f, 8.0f);

        // Cartel luminoso superior gigante de SALIDA AL NIVEL 3
        GameObject cartel = new GameObject("Cartel_SalidaEmergencia");
        cartel.transform.SetParent(go.transform);
        cartel.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        SpriteRenderer cartelSr = cartel.AddComponent<SpriteRenderer>();
        cartelSr.sprite = blankSprite;
        cartelSr.color = new Color(0.1f, 0.7f, 0.3f, 0.95f); // Verde salida de emergencia
        cartel.transform.localScale = new Vector3(5.5f, 0.85f, 1f);
        cartelSr.sortingOrder = 7;

        // Pilares decorativos de la compuerta: SIN COLLIDERS para garantizar paso libre y despejado
        CrearPilar(padre, x - 4.5f, y, false);
        CrearPilar(padre, x + 4.5f, y, false);

        if (gen != null) gen.compuertaSalida = comp;
    }

    private GameObject CrearGeneradorCentral(GameObject padre, float x, float y)
    {
        GameObject go = new GameObject("GeneradorPrincipal");
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pillarSprite != null ? pillarSprite : blankSprite;
        sr.color = new Color(0.35f, 0.40f, 0.45f, 1f);
        go.transform.localScale = new Vector3(4.5f, 4.5f, 1f);
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.4f, 2.4f);

        GeneradorPrincipalNivel2 gen = go.AddComponent<GeneradorPrincipalNivel2>();
        gen.luzFase1 = CrearLuzPunto(go, new Vector3(-0.8f, 0.8f, 0), Color.red, 1.0f, 3.8f);
        gen.luzFase2 = CrearLuzPunto(go, new Vector3(0f, 0.8f, 0), Color.red, 1.0f, 3.8f);
        gen.luzFase3 = CrearLuzPunto(go, new Vector3(0.8f, 0.8f, 0), Color.red, 1.0f, 3.8f);

        return go;
    }

    private void CrearCajaFusible(GameObject padre, float x, float y, int id, string nombreFase, GeneradorPrincipalNivel2 gen)
    {
        GameObject go = new GameObject($"CajaFusible_{id}");
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = InterruptorFusibleNivel2.GenerarSpritePanelIndustrial(id);
        sr.color = Color.white;
        go.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        sr.sortingOrder = 6;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.6f, 2.6f);
        col.isTrigger = true;

        InterruptorFusibleNivel2 fusible = go.AddComponent<InterruptorFusibleNivel2>();
        fusible.idFase = id;
        fusible.nombreFase = nombreFase;
        fusible.generador = gen;
        fusible.cajaSr = sr;
        fusible.luzIndicadora = CrearLuzPunto(go, new Vector3(0, 0.4f, 0), Color.red, 0.85f, 4.2f);

        // Cartel identificador de fase iluminado
        GameObject cartel = new GameObject("RotuloFase");
        cartel.transform.SetParent(go.transform);
        cartel.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        SpriteRenderer cartelSr = cartel.AddComponent<SpriteRenderer>();
        cartelSr.sprite = blankSprite;
        cartelSr.color = new Color(0.95f, 0.75f, 0.15f, 0.95f);
        cartel.transform.localScale = new Vector3(2.4f, 0.35f, 1f);
        cartelSr.sortingOrder = 7;
    }

    private void CrearEscondite(GameObject padre, string nombre, Vector2 pos, string etiqueta, Vector2 tamano)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(pos.x, pos.y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = crateSprite != null ? crateSprite : blankSprite;
        sr.color = new Color(0.30f, 0.32f, 0.36f);
        go.transform.localScale = new Vector3(tamano.x, tamano.y, 1f);
        sr.sortingOrder = 6;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.8f, 1.8f);
        col.isTrigger = true;

        EsconditeNivel2 esc = go.AddComponent<EsconditeNivel2>();
        esc.nombreEscondite = etiqueta;
        esc.spriteCerrado = sr.sprite;
    }

    private void CrearRataPixelArt(GameObject padre, string nombre, Vector2 pos)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(pos.x, pos.y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRataPixelArt;
        sr.color = Color.white;
        go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
        sr.sortingOrder = 8;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.22f;

        go.AddComponent<RataSubterraneaNivel2>();
    }

    private void CrearTrampaConSprite(GameObject padre, string nombre, TipoTrampaAcustica tipo, Vector2 pos, Vector2 tamano, Sprite spriteRealista)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(pos.x, pos.y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRealista;
        sr.color = Color.white;
        go.transform.localScale = new Vector3(tamano.x / 4.5f, tamano.y / 2.8f, 1f);
        sr.sortingOrder = 1;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(tamano.x * 0.9f, tamano.y * 0.85f);
        col.isTrigger = true;

        TrampaAcusticaNivel2 trampa = go.AddComponent<TrampaAcusticaNivel2>();
        trampa.tipoTrampa = tipo;
        trampa.radioPropagacion = (tipo == TipoTrampaAcustica.CristalesRotos) ? 16f : 12f;
    }

    private void CrearPilar(GameObject padre, float x, float y, bool conCollider = true)
    {
        GameObject go = new GameObject("PilarSubterraneo");
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pillarSprite != null ? pillarSprite : blankSprite;
        sr.color = new Color(0.35f, 0.35f, 0.40f);
        go.transform.localScale = new Vector3(1.4f, 2.0f, 1f);
        sr.sortingOrder = 5;

        if (conCollider)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.55f, 0.55f);
        }
    }

    private void CrearArco(GameObject padre, float x, float y)
    {
        GameObject go = new GameObject("ArcoPiedra");
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = archSprite != null ? archSprite : (pillarSprite != null ? pillarSprite : blankSprite);
        sr.color = new Color(0.32f, 0.32f, 0.38f);
        go.transform.localScale = new Vector3(3.2f, 2.2f, 1f);
        sr.sortingOrder = 5;
    }

    private void CrearPilaCajas(GameObject padre, float x, float y, float escala)
    {
        GameObject go = new GameObject("PilaCajas");
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = crateSprite != null ? crateSprite : blankSprite;
        sr.color = new Color(0.40f, 0.34f, 0.28f);
        go.transform.localScale = new Vector3(escala, escala, 1f);
        sr.sortingOrder = 5;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
    }

    private void CrearVaporAmbiental(GameObject padre, Vector2 pos)
    {
        GameObject go = new GameObject("VaporCalderas_Ambiental");
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(pos.x, pos.y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = blankSprite;
        sr.size = new Vector2(2.5f, 1.5f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.color = new Color(0.85f, 0.9f, 0.95f, 0.45f);
        sr.sortingOrder = 6;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.5f, 1.5f);
        col.isTrigger = true;

        ObstaculoVaporAutomaticoNivel2 obstaculo = go.AddComponent<ObstaculoVaporAutomaticoNivel2>();
        obstaculo.tiempoApagado = 6.0f;
        obstaculo.tiempoActivo = 2.5f;
        obstaculo.spriteChorro = sr;
        obstaculo.colliderDanio = col;
    }

    private void CrearCableChispas(GameObject padre, Vector2 pos)
    {
        GameObject go = new GameObject("CableChispas_Subestacion");
        go.transform.SetParent(padre.transform);
        go.transform.position = new Vector3(pos.x, pos.y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = blankSprite;
        sr.color = new Color(0.85f, 0.85f, 0.2f, 0.8f);
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        sr.sortingOrder = 6;

        CableChispasNivel2 chispas = go.AddComponent<CableChispasNivel2>();
        chispas.luzChispa = CrearLuzPunto(go, Vector3.zero, new Color(0.4f, 0.7f, 1.0f), 0f, 4.0f);
    }

    private Light2D CrearLuzPunto(GameObject padre, Vector3 offset, Color color, float intensidad, float radio)
    {
        GameObject luzGo = new GameObject("LuzPunto2D");
        luzGo.transform.SetParent(padre.transform);
        luzGo.transform.localPosition = offset;

        Light2D l2d = luzGo.AddComponent<Light2D>();
        l2d.lightType = Light2D.LightType.Point;
        l2d.color = color;
        l2d.intensity = intensidad;
        l2d.pointLightOuterRadius = radio;
        l2d.pointLightInnerRadius = radio * 0.15f;
        return l2d;
    }

    // =========================================================================
    // GENERADORES PROCEDURALES DE TEXTURAS REALISTAS
    // =========================================================================

    public static Sprite GenerarSpriteCharcoRealista()
    {
        int w = 128;
        int h = 72;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.5f;
        float rx = w * 0.46f;
        float ry = h * 0.44f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                float angle = Mathf.Atan2(dy, dx);

                // Silueta ondulada orgánica irregular
                float deformacion = 1.0f + 0.12f * Mathf.Sin(angle * 4f) + 0.08f * Mathf.Sin(angle * 7f + 1.2f) + 0.05f * Mathf.Cos(angle * 11f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / deformacion;

                if (dist > 1.0f)
                {
                    cols[y * w + x] = new Color(0, 0, 0, 0);
                }
                else
                {
                    float alphaBorde = Mathf.Clamp01((1.0f - dist) / 0.28f);
                    float prof = Mathf.Clamp01(1.0f - dist);

                    // Tono de agua subterránea verdoso-azulada translúcida
                    Color colorAgua = Color.Lerp(new Color(0.12f, 0.24f, 0.34f, 0.50f), new Color(0.08f, 0.16f, 0.25f, 0.85f), prof);
                    colorAgua.a *= alphaBorde;

                    // Brillo y reflejos en la superficie del agua
                    float brillo = Mathf.Sin((x + y * 1.6f) * 0.32f) * Mathf.Cos(x * 0.18f);
                    if (brillo > 0.65f && dist < 0.75f)
                    {
                        float intBrillo = (brillo - 0.65f) / 0.35f * 0.45f;
                        colorAgua = Color.Lerp(colorAgua, new Color(0.75f, 0.90f, 1.0f, 0.95f), intBrillo);
                    }

                    cols[y * w + x] = colorAgua;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 24f);
    }

    public static Sprite GenerarSpriteCristalesRealistas()
    {
        int w = 128;
        int h = 96;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0, 0, 0, 0);

        System.Random rnd = new System.Random(42);
        int numEsquirlas = 38;

        for (int e = 0; e < numEsquirlas; e++)
        {
            float cx = (float)rnd.NextDouble() * (w - 24) + 12;
            float cy = (float)rnd.NextDouble() * (h - 24) + 12;
            int radio = rnd.Next(3, 9);
            int tipo = rnd.Next(0, 3);

            for (int dy = -radio; dy <= radio; dy++)
            {
                for (int dx = -radio; dx <= radio; dx++)
                {
                    int px = Mathf.RoundToInt(cx + dx);
                    int py = Mathf.RoundToInt(cy + dy);
                    if (px < 0 || px >= w || py < 0 || py >= h) continue;

                    bool dentro = false;
                    bool borde = false;

                    if (tipo == 0) // Triángulo filoso
                    {
                        dentro = (dy >= -radio && dy <= dx && dx <= radio);
                        borde = (dy == -radio || dy == dx || dx == radio);
                    }
                    else if (tipo == 1) // Esquirla alargada en ángulo
                    {
                        dentro = Mathf.Abs(dx - dy * 1.5f) <= 1.4f && (Mathf.Abs(dx) + Mathf.Abs(dy) <= radio * 1.4f);
                        borde = Mathf.Abs(dx - dy * 1.5f) > 0.7f;
                    }
                    else // Fragmento poligonal
                    {
                        dentro = (Mathf.Abs(dx) + Mathf.Abs(dy * 1.2f)) <= radio;
                        borde = (Mathf.Abs(dx) + Mathf.Abs(dy * 1.2f)) >= radio - 1;
                    }

                    if (dentro)
                    {
                        if (borde)
                        {
                            cols[py * w + px] = new Color(1.0f, 1.0f, 1.0f, 0.95f); // Borde brillante
                        }
                        else
                        {
                            cols[py * w + px] = new Color(0.78f, 0.92f, 0.96f, 0.65f); // Vidrio translúcido
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 24f);
    }

    public static Sprite GenerarSpriteRataPixelArt()
    {
        int w = 22;
        int h = 12;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0, 0, 0, 0);

        Color pelajeGris = new Color(0.74f, 0.71f, 0.68f, 1f);
        Color pelajeSombra = new Color(0.48f, 0.45f, 0.42f, 1f);
        Color colaRosa = new Color(0.88f, 0.68f, 0.68f, 1f);
        Color ojoRojo = new Color(1.0f, 0.12f, 0.12f, 1f);
        Color hocico = new Color(0.30f, 0.22f, 0.22f, 1f);

        // Cuerpo ovalado
        for (int x = 6; x <= 16; x++)
        {
            for (int y = 3; y <= 8; y++)
            {
                cols[y * w + x] = (y <= 4) ? pelajeSombra : pelajeGris;
            }
        }
        // Cabeza y hocico
        for (int x = 16; x <= 19; x++)
        {
            for (int y = 4; y <= 7; y++)
            {
                cols[y * w + x] = pelajeGris;
            }
        }
        cols[5 * w + 20] = hocico;
        // Oreja
        cols[8 * w + 16] = colaRosa;
        cols[8 * w + 17] = pelajeGris;
        // Ojo rojo (un solo pixel)
        cols[6 * w + 18] = ojoRojo;
        // Patitas
        cols[2 * w + 8] = pelajeSombra;
        cols[2 * w + 9] = pelajeSombra;
        cols[2 * w + 14] = pelajeSombra;
        cols[2 * w + 15] = pelajeSombra;
        // Cola curvada
        cols[5 * w + 5] = colaRosa;
        cols[6 * w + 4] = colaRosa;
        cols[7 * w + 3] = colaRosa;
        cols[7 * w + 2] = colaRosa;
        cols[8 * w + 1] = colaRosa;

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 14f);
    }

    public static Sprite GenerarSpriteRejaMecanica()
    {
        int w = 64;
        int h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0, 0, 0, 0);

        Color hierro = new Color(0.28f, 0.28f, 0.32f, 0.98f);
        Color hierroOscuro = new Color(0.18f, 0.18f, 0.22f, 1f);
        Color oxido = new Color(0.55f, 0.35f, 0.20f, 0.95f);
        Color remache = new Color(0.70f, 0.65f, 0.55f, 1f);

        // Barras horizontales
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < 5; y++) cols[y * w + x] = ((x + y) % 5 == 0) ? oxido : hierro;
            for (int y = h - 5; y < h; y++) cols[y * w + x] = ((x + y) % 4 == 0) ? oxido : hierro;
            for (int y = (h / 2) - 2; y < (h / 2) + 2; y++) cols[y * w + x] = hierro;
        }

        // Barrotes verticales con puntas
        for (int barX = 4; barX < w; barX += 8)
        {
            for (int y = 0; y < h; y++)
            {
                cols[y * w + barX] = hierroOscuro;
                cols[y * w + barX + 1] = ((barX + y) % 7 == 0) ? oxido : hierro;
                cols[y * w + barX + 2] = hierro;
            }
            if (barX + 1 < w) cols[(h - 1) * w + barX + 1] = hierro;
        }

        // Remaches
        for (int barX = 4; barX < w; barX += 8)
        {
            cols[2 * w + barX + 1] = remache;
            cols[(h / 2) * w + barX + 1] = remache;
            cols[(h - 3) * w + barX + 1] = remache;
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}
