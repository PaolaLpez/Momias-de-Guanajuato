using UnityEngine;

[ExecuteAlways]
public class GeneradorEscenarioMuseo : MonoBehaviour
{
    private Sprite floorSprite;
    private Sprite wallSprite;
    private Sprite carpetSprite;
    private Sprite statueSprite;
    private Sprite pillarSprite;
    private Sprite crateSprite;
    private Sprite plantSprite;
    private Sprite torchSprite;
    private Sprite showcaseSprite;
    private Sprite keySprite;
    private Sprite doorSprite;
    private Sprite archSprite;
    private Sprite paintingSprite;
    private Sprite chandelierSprite;
    private Sprite benchSprite;

    private void Awake()
    {
        ConstruirSiEsNecesario();
    }

    private void OnEnable()
    {
        ConstruirSiEsNecesario();
    }

    private void Start()
    {
        ConstruirSiEsNecesario();
    }

    public void ConstruirSiEsNecesario()
    {
        GameObject existente = GameObject.Find("GranMuseo2D");
        if (existente != null && existente.transform.childCount > 10) return;

        CargarRecursos();
        ConstruirColosalMuseoTenebroso();
    }

    [ContextMenu("Reconstruir Museo Ahora")]
    public void ReconstruirMuseoManual()
    {
        CargarRecursos();
        ConstruirColosalMuseoTenebroso();
    }

    void CargarRecursos()
    {
        floorSprite = Resources.Load<Sprite>("Tiles/tile_floor");
        wallSprite = Resources.Load<Sprite>("Tiles/tile_wall");
        carpetSprite = Resources.Load<Sprite>("Tiles/prop_carpet");
        statueSprite = Resources.Load<Sprite>("Tiles/prop_statue");
        pillarSprite = Resources.Load<Sprite>("Tiles/prop_pillar");
        crateSprite = Resources.Load<Sprite>("Tiles/prop_crate");
        plantSprite = Resources.Load<Sprite>("Tiles/prop_plant");
        torchSprite = Resources.Load<Sprite>("Tiles/prop_torch");
        showcaseSprite = Resources.Load<Sprite>("Tiles/prop_showcase");
        keySprite = Resources.Load<Sprite>("Tiles/prop_key");
        doorSprite = Resources.Load<Sprite>("Tiles/prop_door");

        archSprite = Resources.Load<Sprite>("Props/prop_arch");
        paintingSprite = Resources.Load<Sprite>("Props/prop_painting");
        chandelierSprite = Resources.Load<Sprite>("Props/prop_chandelier");
        benchSprite = Resources.Load<Sprite>("Props/prop_bench");
    }

    public void ConstruirColosalMuseoTenebroso()
    {
        GameObject existente = GameObject.Find("GranMuseo2D");
        if (existente != null)
        {
            if (Application.isPlaying) Destroy(existente);
            else DestroyImmediate(existente);
        }

        GameObject root = new GameObject("GranMuseo2D");
        root.transform.position = Vector3.zero;

        // ==========================================
        // 1. PISOS DEL MUSEO (PASILLOS AMPLIOS Y EXTENSOS)
        // Pasillo central ahora mide 9.5 unidades de alto para total libertad
        // ==========================================
        CrearPiso(root, "Piso_Troncal_Central", 0f, 0f, 66f, 9.5f);

        // Pisos de las 6 Grandes Salas
        CrearPiso(root, "Piso_Oficina_Noroeste", -20f, 13f, 21f, 16f);
        CrearPiso(root, "Piso_GranBoveda_Norte", 0f, 13f, 21f, 16f);
        CrearPiso(root, "Piso_Galeria_Noreste", 20f, 13f, 21f, 16f);

        CrearPiso(root, "Piso_Cripta_Sudoeste", -20f, -13f, 21f, 16f);
        CrearPiso(root, "Piso_Santuario_Sur", 0f, -13f, 21f, 16f);
        CrearPiso(root, "Piso_Almacen_Sudeste", 20f, -13f, 21f, 16f);

        // ==========================================
        // 2. ALFOMBRAS GÓTICAS DE GALA (ANCHO Y DESPEJADO)
        // ==========================================
        // Troncal central: 4.8 unidades de ancho, 100% libre de obstáculos
        CrearAlfombra(root, new Vector2(0f, 0f), new Vector2(62f, 4.8f));

        // Pasillos alfombrados hacia cada una de las 6 salas (4.0 unidades de ancho)
        CrearAlfombra(root, new Vector2(-20f, 8.5f), new Vector2(4.0f, 13f));  // Hacia Oficina
        CrearAlfombra(root, new Vector2(0f, 8.5f), new Vector2(4.5f, 13f));    // Hacia Bóveda Momia
        CrearAlfombra(root, new Vector2(20f, 8.5f), new Vector2(4.0f, 13f));   // Hacia Galería

        CrearAlfombra(root, new Vector2(-20f, -8.5f), new Vector2(4.0f, 13f)); // Hacia Cripta/Salida
        CrearAlfombra(root, new Vector2(0f, -8.5f), new Vector2(4.5f, 13f));   // Hacia Santuario Llave
        CrearAlfombra(root, new Vector2(20f, -8.5f), new Vector2(4.0f, 13f));  // Hacia Almacén

        // ==========================================
        // 3. MUROS PERIMETRALES EXTERIORES (66 x 42 UNIDADES)
        // ==========================================
        CrearMuro(root, new Vector2(0f, 21.5f), new Vector2(66f, 1.8f));   // Muro Norte
        CrearMuro(root, new Vector2(0f, -21.5f), new Vector2(66f, 1.8f));  // Muro Sur
        CrearMuro(root, new Vector2(-32f, 0f), new Vector2(1.8f, 43f));    // Muro Oeste
        CrearMuro(root, new Vector2(32f, 0f), new Vector2(1.8f, 43f));     // Muro Este

        // ==========================================
        // 4. MUROS INTERNOS CON ENTRADAS EXTRA ANCHAS (6 UNIDADES DE PASO LIBRE)
        // Límite del pasillo central: Y = +4.8 (Norte) y Y = -4.8 (Sur)
        // ==========================================
        // --- Límite Norte (Y = +4.8) ---
        // Vano 1 (Oficina): X = -21.0 a -19.0 (LIBRE 2.0 unidades)
        // Vano 2 (Bóveda): X = -1.0 a +1.0 (LIBRE 2.0 unidades)
        // Vano 3 (Galería): X = +19.0 a +21.0 (LIBRE 2.0 unidades)
        CrearMuro(root, new Vector2(-26.75f, 4.8f), new Vector2(11.5f, 1.6f));
        CrearMuro(root, new Vector2(-10.0f, 4.8f), new Vector2(18.0f, 1.6f));
        CrearMuro(root, new Vector2(10.0f, 4.8f), new Vector2(18.0f, 1.6f));
        CrearMuro(root, new Vector2(26.75f, 4.8f), new Vector2(11.5f, 1.6f));

        // --- Límite Sur (Y = -4.8) ---
        // Vano 4 (Cripta/Salida): X = -21.0 a -19.0 (LIBRE 2.0 unidades)
        // Vano 5 (Santuario): X = -1.0 a +1.0 (LIBRE 2.0 unidades)
        // Vano 6 (Almacén): X = +19.0 a +21.0 (LIBRE 2.0 unidades)
        CrearMuro(root, new Vector2(-26.75f, -4.8f), new Vector2(11.5f, 1.6f));
        CrearMuro(root, new Vector2(-10.0f, -4.8f), new Vector2(18.0f, 1.6f));
        CrearMuro(root, new Vector2(10.0f, -4.8f), new Vector2(18.0f, 1.6f));
        CrearMuro(root, new Vector2(26.75f, -4.8f), new Vector2(11.5f, 1.6f));

        // --- Muros divisorios verticales entre salas ---
        CrearMuro(root, new Vector2(-10.25f, 13.2f), new Vector2(1.5f, 15.5f)); // Divide Oficina y Bóveda
        CrearMuro(root, new Vector2(10.25f, 13.2f), new Vector2(1.5f, 15.5f));  // Divide Bóveda y Galería
        CrearMuro(root, new Vector2(-10.25f, -13.2f), new Vector2(1.5f, 15.5f)); // Divide Cripta y Santuario
        CrearMuro(root, new Vector2(10.25f, -13.2f), new Vector2(1.5f, 15.5f));  // Divide Santuario y Almacén

        // ==========================================
        // 5. ARCOS MONUMENTALES (DECORATIVOS, SIN COLISIÓN)
        // ==========================================
        CrearArcoPuerta(root, "Arco_Oficina", new Vector2(-20f, 4.8f));
        CrearArcoPuerta(root, "Arco_GranBoveda", new Vector2(0f, 4.8f));
        CrearArcoPuerta(root, "Arco_Galeria", new Vector2(20f, 4.8f));

        CrearArcoPuerta(root, "Arco_CriptaSalida", new Vector2(-20f, -4.8f));
        CrearArcoPuerta(root, "Arco_SantuarioLlave", new Vector2(0f, -4.8f));
        CrearArcoPuerta(root, "Arco_Almacen", new Vector2(20f, -4.8f));

        // Antorchas en las paredes al lado de los arcos
        CrearAntorcha(root, new Vector2(-23.5f, 4.8f));
        CrearAntorcha(root, new Vector2(-16.5f, 4.8f));

        CrearAntorcha(root, new Vector2(-4.2f, 4.8f));
        CrearAntorcha(root, new Vector2(4.2f, 4.8f));

        CrearAntorcha(root, new Vector2(16.5f, 4.8f));
        CrearAntorcha(root, new Vector2(23.5f, 4.8f));

        CrearAntorcha(root, new Vector2(-23.5f, -4.8f));
        CrearAntorcha(root, new Vector2(-16.5f, -4.8f));

        CrearAntorcha(root, new Vector2(-4.2f, -4.8f));
        CrearAntorcha(root, new Vector2(4.2f, -4.8f));

        CrearAntorcha(root, new Vector2(16.5f, -4.8f));
        CrearAntorcha(root, new Vector2(23.5f, -4.8f));

        // ==========================================
        // 6. DETALLES Y DECORACIONES EN LAS PAREDES (PASILLOS 100% DESPEJADOS)
        // Todo ubicado en los muros; ¡cero muebles en medio de la alfombra!
        // ==========================================
        // Cuadros al óleo pegados a los muros
        CrearCuadroOleo(root, new Vector2(-10.25f, 3.8f));
        CrearCuadroOleo(root, new Vector2(10.25f, 3.8f));
        CrearCuadroOleo(root, new Vector2(-27.5f, 3.8f));
        CrearCuadroOleo(root, new Vector2(27.5f, 3.8f));

        CrearCuadroOleo(root, new Vector2(-10.25f, -3.8f));
        CrearCuadroOleo(root, new Vector2(10.25f, -3.8f));

        CrearCuadroOleo(root, new Vector2(-20f, 20.4f));
        CrearCuadroOleo(root, new Vector2(20f, 20.4f));
        CrearCuadroOleo(root, new Vector2(-20f, -20.4f));
        CrearCuadroOleo(root, new Vector2(20f, -20.4f));

        // Lámparas de araña en el techo (elevadas, sin colisiones)
        CrearLamparaArana(root, new Vector2(0f, 0f));
        CrearLamparaArana(root, new Vector2(-15f, 0f));
        CrearLamparaArana(root, new Vector2(15f, 0f));
        CrearLamparaArana(root, new Vector2(-20f, 12f));
        CrearLamparaArana(root, new Vector2(0f, 12f));
        CrearLamparaArana(root, new Vector2(20f, 12f));
        CrearLamparaArana(root, new Vector2(-20f, -12f));
        CrearLamparaArana(root, new Vector2(0f, -12f));
        CrearLamparaArana(root, new Vector2(20f, -12f));

        // Bancas pegadas estrictamente a los muros laterales DENTRO de las salas (NO en el pasillo)
        CrearBanca(root, new Vector2(-29.5f, 12f)); // Muro oeste oficina
        CrearBanca(root, new Vector2(29.5f, 12f));  // Muro este galería
        CrearBanca(root, new Vector2(-29.5f, -12f)); // Muro oeste cripta
        CrearBanca(root, new Vector2(29.5f, -12f));  // Muro este almacén

        // Columnas ornamentales pegadas a las esquinas perimetrales
        CrearColumna(root, new Vector2(-30f, 19f));
        CrearColumna(root, new Vector2(30f, 19f));
        CrearColumna(root, new Vector2(-30f, -19f));
        CrearColumna(root, new Vector2(30f, -19f));

        // =========================================================================
        // VITRINAS DE MOMIAS EN TODOS Y CADA UNO DE LOS CUARTOS DEL MUSEO
        // =========================================================================
        // 1. Oficina Noroeste: Vitrinas de momias de archivo histórico
        CrearVitrina(root, new Vector2(-26f, 19.5f));
        CrearVitrina(root, new Vector2(-14f, 19.5f));
        CrearVitrina(root, new Vector2(-29.5f, 15f));

        // 2. Gran Bóveda Norte: Salón de las Momias Mayores
        CrearVitrinaRota(root, new Vector2(0f, 18.5f)); // La vitrina rota de donde escapó la Momia 1
        CrearVitrina(root, new Vector2(-6f, 19.5f));
        CrearVitrina(root, new Vector2(6f, 19.5f));
        CrearVitrina(root, new Vector2(-9.2f, 14f));
        CrearVitrina(root, new Vector2(9.2f, 14f));

        // 3. Galería Noreste: Vitrinas de exhibición dorada
        CrearVitrina(root, new Vector2(14f, 19.5f));
        CrearVitrina(root, new Vector2(26f, 19.5f));
        CrearVitrina(root, new Vector2(29.5f, 15f));

        // 4. Cripta Sudoeste: Vitrinas en nichos de piedra de la catacumba
        CrearVitrina(root, new Vector2(-14f, -19.5f));
        CrearVitrina(root, new Vector2(-24f, -19.5f));
        CrearVitrina(root, new Vector2(-14f, -10f));

        // 5. Santuario Sur (Vestíbulo de Entrada): Vitrinas de bienvenida
        CrearVitrina(root, new Vector2(-6f, -19.5f));
        CrearVitrina(root, new Vector2(6f, -19.5f));
        CrearVitrina(root, new Vector2(-9.2f, -12f));
        CrearVitrina(root, new Vector2(9.2f, -12f));

        // 6. Almacén Sudeste: Vitrinas de conservación arqueológica
        CrearVitrina(root, new Vector2(14f, -19.5f));
        CrearVitrina(root, new Vector2(26f, -19.5f));
        CrearVitrina(root, new Vector2(29.5f, -10f));

        // 7. Gran Pasillo Central: Vitrinas de exhibición alineadas a los muros (pasillo central 100% despejado)
        CrearVitrina(root, new Vector2(-28f, 3.6f));
        CrearVitrina(root, new Vector2(-15f, 3.6f));
        CrearVitrina(root, new Vector2(-6f, 3.6f));
        CrearVitrina(root, new Vector2(6f, 3.6f));
        CrearVitrina(root, new Vector2(15f, 3.6f));
        CrearVitrina(root, new Vector2(28f, 3.6f));

        CrearVitrina(root, new Vector2(-28f, -3.6f));
        CrearVitrina(root, new Vector2(-15f, -3.6f));
        CrearVitrina(root, new Vector2(-6f, -3.6f));
        CrearVitrina(root, new Vector2(6f, -3.6f));
        CrearVitrina(root, new Vector2(15f, -3.6f));
        CrearVitrina(root, new Vector2(28f, -3.6f));

        // =========================================================================
        // DETALLES TEMÁTICOS Y DIFERENCIADORES POR SALA
        // =========================================================================
        // A. Cajas apiladas de archivo en Almacén Sudeste
        CrearCajasAlmacen(root, new Vector2(27f, -18f));
        CrearCajasAlmacen(root, new Vector2(15f, -16f));
        CrearCajasAlmacen(root, new Vector2(22f, -17f));

        // B. Cajas y libros en Oficina Noroeste
        CrearCajasAlmacen(root, new Vector2(-28f, 9f));

        // C. Estatuas históricas en nichos de fondo
        CrearEstatua(root, new Vector2(-13f, 19.5f));
        CrearEstatua(root, new Vector2(13f, 19.5f));
        CrearEstatua(root, new Vector2(-8f, -16f));
        CrearEstatua(root, new Vector2(8f, -16f));

        // D. Antorchas perimetrales
        for (float x = -28f; x <= 28f; x += 7f)
        {
            CrearAntorcha(root, new Vector2(x, 20.4f));
            CrearAntorcha(root, new Vector2(x, -20.4f));
        }

        // ==========================================
        // 7. ELEMENTOS DEL SISTEMA DE JUEGO (MISIONES / ESTADOS)
        // ==========================================
        // A. Escritorio con la Pistola (Oficina Noroeste: -20, 18 pegado al muro norte)
        ConfigurarEscritorioOficina(root, new Vector2(-20f, 18f));

        // B. La Llave del Museo (Galería Noreste: 20, 14 sobre pedestal con veladoras)
        ConfigurarLlave(new Vector2(20f, 14f));

        // C. Puerta de Seguridad / Salida (Cripta Sudoeste: -29, -18 en la esquina)
        ConfigurarPuertaSalida(new Vector2(-29f, -18f));

        // D. Spawners de Escarabajos Pequeños en esquinas de salas
        CrearSpawnerEscarabajos(root, "Spawner_Noreste", new Vector2(25f, 16f));
        CrearSpawnerEscarabajos(root, "Spawner_Sudeste", new Vector2(25f, -16f));
        CrearSpawnerEscarabajos(root, "Spawner_Sudoeste", new Vector2(-20f, -16f));
        CrearSpawnerEscarabajos(root, "Spawner_Noroeste", new Vector2(-24f, 16f));
        CrearSpawnerEscarabajos(root, "Spawner_NorteCentral", new Vector2(0f, 14f));
        CrearSpawnerEscarabajos(root, "Spawner_PasilloSur", new Vector2(0f, -6f));

        // Escarabajos iniciales patrullando las salas
        CrearEscarabajoInicial(root, "Escarabajo_Boveda", new Vector2(3f, 13f));
        CrearEscarabajoInicial(root, "Escarabajo_Boveda2", new Vector2(-3f, 14f));
        CrearEscarabajoInicial(root, "Escarabajo_Galeria", new Vector2(18f, 15f));
        CrearEscarabajoInicial(root, "Escarabajo_Galeria2", new Vector2(24f, 11f));
        CrearEscarabajoInicial(root, "Escarabajo_Almacen", new Vector2(18f, -14f));
        CrearEscarabajoInicial(root, "Escarabajo_Almacen2", new Vector2(24f, -13f));
        CrearEscarabajoInicial(root, "Escarabajo_Cripta", new Vector2(-16f, -13f));
        CrearEscarabajoInicial(root, "Escarabajo_Cripta2", new Vector2(-24f, -14f));
        CrearEscarabajoInicial(root, "Escarabajo_PasilloOeste", new Vector2(-12f, 0f));
        CrearEscarabajoInicial(root, "Escarabajo_PasilloEste", new Vector2(12f, 0f));

        // E. Trampas de Brazos de Vitrina (Pegadas a las vitrinas de los muros)
        CrearTrampaBrazos(root, "TrampaBrazos_Oficina", new Vector2(-12f, 18f));
        CrearTrampaBrazos(root, "TrampaBrazos_Galeria", new Vector2(20f, 18f));
        CrearTrampaBrazos(root, "TrampaBrazos_Almacen", new Vector2(16f, -18f));

        // F. Vitrinas Saqueadas (con 60% de munición, pegadas a los muros)
        CrearVitrinaSaqueada(root, "VitrinaSaqueada_1", new Vector2(-27.5f, 3.2f));
        CrearVitrinaSaqueada(root, "VitrinaSaqueada_2", new Vector2(27.5f, 3.2f));
        CrearVitrinaSaqueada(root, "VitrinaSaqueada_3", new Vector2(27.5f, -3.2f));
        CrearVitrinaSaqueada(root, "VitrinaSaqueada_4", new Vector2(-27.5f, -3.2f));

        // G. Altares con Veladoras (Pegados a muros, pasillo central 100% libre)
        CrearAltarVeladoras(root, "Altar_MuroCentralNorte", new Vector2(0f, 3.2f));
        CrearAltarVeladoras(root, "Altar_SantuarioSur", new Vector2(-6f, -16f));

        // H. Fichas de Bronce / Lore pegadas a los muros
        CrearFichaLore(root, "FichaLore_Reglamento", new Vector2(6f, -18f),
            "Reglamento del Museo y Toque de Queda (12:00 AM)",
            "Aviso al guardia nocturno:\nEn punto de la medianoche, todas las salas deben permanecer cerradas. Si escuchas ruidos de vitrinas rotas, dirígete a la oficina y toma tu arma de servicio.");

        CrearFichaLore(root, "FichaLore_Colera", new Vector2(-12f, 3.2f),
            "Crónica de la Epidemia de Cólera (1833)",
            "Durante el brote de cólera morbus de 1833, cientos de cuerpos fueron sepultados en el Panteón de Santa Paula. El clima árido y el nitrato del suelo momificaron sus restos, sumiéndolos en una vigilia eterna.");

        CrearFichaLore(root, "FichaLore_MomiaErrante", new Vector2(12f, 3.2f),
            "Bitácora del Custodio Mayor - La Que Acecha",
            "La que camina en la sombra no puede avanzar si tus ojos están fijos en ella. Mantén tu mirada al frente y nunca retrocedas sin vigilar tu espalda.");

        CrearFichaLore(root, "FichaLore_GranBoveda", new Vector2(0f, 15f),
            "La Vitrina Central de la Bóveda - El Despertar",
            "Aquí reposaba el espécimen más antiguo rescatado del panteón colonial. Al sonar la medianoche, el cristal estalló desde su interior y la entidad despertó buscando a los vivos.");

        CrearFichaLore(root, "FichaLore_Cripta", new Vector2(-20f, -15f),
            "La Compuerta de Escape y los Túneles Coloniales",
            "Bajo las baldosas corre el antiguo túnel de desagüe virreinal hacia el exterior. La compuerta pesada requiere la Llave Maestra Colonial para abrir los cerrojos.");

        CrearFichaLore(root, "FichaLore_Almacen", new Vector2(20f, -15f),
            "Registro de Plagas y Escarabajos de Sepulcro",
            "Las excavaciones perturbaron los nidos de escarabajos de sepulcro (Necrophila). Atacan por los flancos y son atraídos por el calor vivo; el plomo o el fuego los destruye.");
    }

    void CrearPiso(GameObject root, string nombre, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(root.transform);
        go.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = floorSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.sortingOrder = -10;
    }

    void CrearAlfombra(GameObject root, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject("Alfombra_Gala");
        go.transform.SetParent(root.transform);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = carpetSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = tam;
        sr.sortingOrder = -5;
    }

    void CrearMuro(GameObject root, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject("Muro_Museo");
        go.transform.SetParent(root.transform);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = wallSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = tam;
        sr.sortingOrder = 0;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = tam;
    }

    void CrearArcoPuerta(GameObject root, string nombre, Vector2 pos)
    {
        GameObject arco = new GameObject(nombre);
        arco.transform.SetParent(root.transform);
        arco.transform.position = pos;
        arco.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        SpriteRenderer sr = arco.AddComponent<SpriteRenderer>();
        sr.sprite = archSprite;
        sr.sortingOrder = 450;

        // Poste Izquierdo (X mundo = -1.5 a -1.0)
        BoxCollider2D colIzq = arco.AddComponent<BoxCollider2D>();
        colIzq.offset = new Vector2(-0.833f, 0f);
        colIzq.size = new Vector2(0.333f, 2.0f);

        // Poste Derecho (X mundo = +1.0 a +1.5)
        BoxCollider2D colDer = arco.AddComponent<BoxCollider2D>();
        colDer.offset = new Vector2(0.833f, 0f);
        colDer.size = new Vector2(0.333f, 2.0f);

        // Sin colDintel: Vano libre de 2.0 unidades para entrar y salir con total libertad.
    }

    void CrearCuadroOleo(GameObject root, Vector2 pos)
    {
        GameObject cuadro = new GameObject("Cuadro_Oleo");
        cuadro.transform.SetParent(root.transform);
        cuadro.transform.position = pos;
        cuadro.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        SpriteRenderer sr = cuadro.AddComponent<SpriteRenderer>();
        sr.sprite = paintingSprite;
        sr.sortingOrder = 2;
    }

    void CrearLamparaArana(GameObject root, Vector2 pos)
    {
        GameObject lampara = new GameObject("Lampara_Arana");
        lampara.transform.SetParent(root.transform);
        lampara.transform.position = pos;
        lampara.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

        SpriteRenderer sr = lampara.AddComponent<SpriteRenderer>();
        sr.sprite = chandelierSprite;
        sr.sortingOrder = 18; // En el techo por encima de todo
    }

    void CrearBanca(GameObject root, Vector2 pos)
    {
        GameObject banca = new GameObject("Banca_Museo");
        banca.transform.SetParent(root.transform);
        banca.transform.position = pos;
        banca.transform.localScale = new Vector3(1.0f, 1.0f, 1f);

        SpriteRenderer sr = banca.AddComponent<SpriteRenderer>();
        sr.sprite = benchSprite;
        sr.sortingOrder = 3;

        BoxCollider2D col = banca.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.4f, 0.6f);
    }

    void CrearAntorcha(GameObject root, Vector2 pos)
    {
        GameObject go = new GameObject("Antorcha");
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = torchSprite;
        sr.sortingOrder = 3;
    }

    void CrearColumna(GameObject root, Vector2 pos)
    {
        GameObject go = new GameObject("Columna_Marmol");
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pillarSprite;
        sr.sortingOrder = 3;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1.4f);
    }

    void CrearEstatua(GameObject root, Vector2 pos)
    {
        GameObject go = new GameObject("Estatua_Historica");
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = statueSprite;
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.1f, 1.5f);
    }

    void CrearVitrina(GameObject root, Vector2 pos)
    {
        GameObject go = new GameObject("Vitrina_Momia");
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite momiaSprite = Resources.Load<Sprite>("Props/vitrina_momia");
        sr.sprite = momiaSprite != null ? momiaSprite : showcaseSprite;
        sr.sortingOrder = Mathf.RoundToInt(-(pos.y - 0.9f) * 100f);

        // Mueble 100% sólido de cuerpo entero: NADIE puede pasar por encima ni dentro del cristal
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.3f, 2.2f);
        col.offset = new Vector2(0f, 0f);
        col.isTrigger = false;
    }

    void CrearVitrinaRota(GameObject root, Vector2 pos)
    {
        GameObject go = new GameObject("Vitrina_Momia_Rota");
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite brokenSprite = Resources.Load<Sprite>("Props/showcase_broken");
        sr.sprite = brokenSprite != null ? brokenSprite : showcaseSprite;
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.1f, 1.4f);
        col.offset = new Vector2(0f, -0.1f);
    }

    void CrearCajasAlmacen(GameObject root, Vector2 pos)
    {
        GameObject go = new GameObject("Cajas_Almacen");
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = crateSprite;
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.0f, 1.0f);
    }

    void ConfigurarEscritorioOficina(GameObject root, Vector2 pos)
    {
        GameObject desk = new GameObject("Escritorio_Oficina");
        desk.transform.SetParent(root.transform);
        desk.transform.position = pos;
        desk.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

        SpriteRenderer sr = desk.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Props/desk_opened") != null ? Resources.Load<Sprite>("Props/desk_opened") : crateSprite;
        sr.sortingOrder = 4;

        BoxCollider2D col = desk.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.9f, 1.9f);

        InteractableObject io = desk.AddComponent<InteractableObject>();
        io.Configurar(InteractableType.OfficeDesk);
    }

    void ConfigurarLlave(Vector2 pos)
    {
        GameObject llave = GameObject.Find("LlaveMuseo");
        if (llave == null)
        {
            llave = new GameObject("LlaveMuseo");
            llave.AddComponent<LlaveItem>();
        }
        llave.transform.position = pos;
        llave.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

        SpriteRenderer sr = llave.GetComponent<SpriteRenderer>();
        if (sr == null) sr = llave.AddComponent<SpriteRenderer>();
        sr.sprite = keySprite;
        sr.sortingOrder = 15;
        sr.color = new Color(1f, 0.92f, 0.2f, 1f);

        // Altar con veladoras bajo la llave para que resalte muchísimo
        Transform oldPedestal = llave.transform.Find("Pedestal_Llave");
        if (oldPedestal == null)
        {
            GameObject pedestal = new GameObject("Pedestal_Llave");
            pedestal.transform.SetParent(llave.transform);
            pedestal.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            pedestal.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            SpriteRenderer pedSr = pedestal.AddComponent<SpriteRenderer>();
            pedSr.sprite = Resources.Load<Sprite>("Props/altar_lit");
            pedSr.sortingOrder = 12;
        }

        // Siempre activa y visible desde el inicio
        llave.SetActive(true);
    }

    void ConfigurarPuertaSalida(Vector2 pos)
    {
        GameObject puerta = GameObject.Find("PuertaSeguridad");
        if (puerta == null)
        {
            puerta = new GameObject("PuertaSeguridad");
            puerta.AddComponent<PuertaSalida>();
        }
        puerta.transform.position = pos;
        puerta.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

        SpriteRenderer sr = puerta.GetComponent<SpriteRenderer>();
        if (sr == null) sr = puerta.AddComponent<SpriteRenderer>();
        sr.sprite = doorSprite;
        sr.sortingOrder = 4;
        sr.color = Color.white;

        BoxCollider2D col = puerta.GetComponent<BoxCollider2D>();
        if (col == null) col = puerta.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.8f, 1.8f);
    }

    void CrearSpawnerEscarabajos(GameObject root, string nombre, Vector2 pos)
    {
        GameObject spawner = new GameObject(nombre);
        spawner.transform.SetParent(root.transform);
        spawner.transform.position = pos;

        spawner.AddComponent<MinorEnemySpawner>();
        spawner.SetActive(true);

        if (Level1Manager.Instance != null)
        {
            Level1Manager.Instance.RegistrarSpawner(spawner);
        }
    }

    void CrearEscarabajoInicial(GameObject root, string nombre, Vector2 pos)
    {
        GameObject enemy = new GameObject(nombre);
        enemy.transform.SetParent(root.transform);
        enemy.transform.position = pos;
        enemy.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Enemies/beetle_walk_0");
        sr.sortingOrder = 9;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.65f;
        col.isTrigger = true;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        enemy.AddComponent<MinorEnemyAI>();
    }

    void CrearTrampaBrazos(GameObject root, string nombre, Vector2 pos)
    {
        GameObject trampa = new GameObject(nombre);
        trampa.transform.SetParent(root.transform);
        trampa.transform.position = pos;
        trampa.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        SpriteRenderer sr = trampa.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Enemies/showcase_arms_hidden");
        sr.sortingOrder = 5;

        BoxCollider2D col = trampa.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.6f, 1.6f);

        MinorEnemyAI ai = trampa.AddComponent<MinorEnemyAI>();
        ai.ConfigurarTipo(MinorEnemyType.ShowcaseArms);
    }

    void CrearVitrinaSaqueada(GameObject root, string nombre, Vector2 pos)
    {
        GameObject vitrina = new GameObject(nombre);
        vitrina.transform.SetParent(root.transform);
        vitrina.transform.position = pos;
        vitrina.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        SpriteRenderer sr = vitrina.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Props/showcase_broken");
        sr.sortingOrder = 500 + Mathf.RoundToInt(-(pos.y - 0.9f) * 10f);

        // Colisionador 100% SÓLIDO: cubre el mueble y sella la pared trasera para que el jugador nunca quede detrás
        BoxCollider2D col = vitrina.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        float offsetY = (pos.y >= 0) ? 0.75f : -0.75f;
        col.size = new Vector2(1.5f, 3.2f);
        col.offset = new Vector2(0f, offsetY);

        InteractableObject io = vitrina.AddComponent<InteractableObject>();
        io.Configurar(InteractableType.LootedShowcase);
    }

    void CrearAltarVeladoras(GameObject root, string nombre, Vector2 pos)
    {
        GameObject altar = new GameObject(nombre);
        altar.transform.SetParent(root.transform);
        altar.transform.position = pos;
        altar.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

        SpriteRenderer sr = altar.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Props/altar_lit") != null ? Resources.Load<Sprite>("Props/altar_lit") : torchSprite;
        sr.sortingOrder = 4;

        BoxCollider2D col = altar.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.7f, 1.7f);

        InteractableObject io = altar.AddComponent<InteractableObject>();
        io.Configurar(InteractableType.CandleAltar);
    }

    void CrearFichaLore(GameObject root, string nombre, Vector2 pos, string titulo, string desc)
    {
        GameObject ficha = new GameObject(nombre);
        ficha.transform.SetParent(root.transform);
        ficha.transform.position = pos;
        ficha.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

        SpriteRenderer sr = ficha.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Props/lore_plaque");
        sr.sortingOrder = 4;

        BoxCollider2D col = ficha.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 1.5f);

        InteractableObject io = ficha.AddComponent<InteractableObject>();
        io.Configurar(InteractableType.LoreTablet, titulo, desc);
    }
}
