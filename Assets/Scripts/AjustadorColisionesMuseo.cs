using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ajusta herméticamente las colisiones, el espaciado ordenado y la profundidad 2.5D
/// de muros, arcos de puertas, vitrinas, cuadros y altares del museo.
/// Garantiza:
/// 1. CADA OBJETO TIENE SU PROPIO ESPACIO (cero objetos encimados).
/// 2. Los vanos de las 6 puertas permiten el paso visible y natural sin que el jugador se esconda en dinteles.
/// 3. La vigilante NUNCA lleva linterna en el Nivel 1 (la linterna es exclusiva del Nivel 2).
/// 4. Las vitrinas son 100% sólidas.
/// </summary>
[DefaultExecutionOrder(-100)]
public class AjustadorColisionesMuseo : MonoBehaviour
{
    private void Awake()
    {
        AjustarTodo();
    }

    private void Start()
    {
        AjustarTodo();
    }

    public static void AjustarTodo()
    {
        GameObject root = GameObject.Find("GranMuseo2D");
        if (root == null)
        {
            root = new GameObject("GranMuseo2D");
        }

        // 0. Quitar cualquier linterna accidental en Nivel 1
        SistemaIluminacionMuseo.LimpiarLinternasEnNivel1();

        // 1. Asegurar pisos ordenados en capa de fondo (-500)
        AsegurarPisosMuseo(root);

        // 2. Configurar Arcos de Puerta: sortingOrder = 450 (detrás del jugador que va en 500+).
        // El jugador es SIEMPRE visible pasando por el vano, NUNCA se oculta ni queda cubierto.
        ConfigurarArcoConPasoLibre(root, "Arco_GranBoveda", new Vector2(0f, 4.8f));
        ConfigurarArcoConPasoLibre(root, "Arco_SantuarioLlave", new Vector2(0f, -4.8f));
        ConfigurarArcoConPasoLibre(root, "Arco_Oficina", new Vector2(-20f, 4.8f));
        ConfigurarArcoConPasoLibre(root, "Arco_CriptaSalida", new Vector2(-20f, -4.8f));
        ConfigurarArcoConPasoLibre(root, "Arco_Galeria", new Vector2(20f, 4.8f));
        ConfigurarArcoConPasoLibre(root, "Arco_Almacen", new Vector2(20f, -4.8f));

        // 3. Muros visuales y su profundidad Top-Down 2.5D
        AjustarMurosVisualesExistentes();

        // 4. Muros de física herméticos
        ConstruirMurosHermeticos(root);

        // 5. Configurar Vitrinas con espacios únicos y separados (cero encimadas)
        AjustarVitrinasMuseo(root);

        // 6. Configurar Cuadros al óleo en espacios libres de pared
        AjustarCuadrosOleo(root);

        // 7. Configurar Altares de veladoras separados
        AjustarAltaresVeladoras(root);

        // 8. Fichas de historia en posiciones separadas
        AjustarFichasLore(root);

        // 9. Gran Puerta de Escape colonial
        AjustarPuertaEscape(root);

        // 10. Eliminar cualquier superposición residual entre props
        EliminarSuperposicionesResiduales();

        Debug.Log("<color=green>[AjustadorColisionesMuseo] Museo calibrado: Sin linterna en N1, arcos visibles y props con espacio propio.</color>");
    }

    private static void AsegurarPisosMuseo(GameObject root)
    {
        Sprite floorSprite = Resources.Load<Sprite>("Tiles/tile_floor");
        if (floorSprite == null) return;

        (string nombre, Vector2 pos, Vector2 size)[] pisos = new (string, Vector2, Vector2)[]
        {
            ("Piso_Troncal_Central", new Vector2(0f, 0f), new Vector2(66f, 10f)),
            ("Piso_Oficina_Noroeste", new Vector2(-20f, 13f), new Vector2(21f, 16f)),
            ("Piso_GranBoveda_Norte", new Vector2(0f, 13f), new Vector2(21f, 16f)),
            ("Piso_Galeria_Noreste", new Vector2(20f, 13f), new Vector2(21f, 16f)),
            ("Piso_Cripta_Sudoeste", new Vector2(-20f, -13f), new Vector2(21f, 16f)),
            ("Piso_Santuario_Sur", new Vector2(0f, -13f), new Vector2(21f, 16f)),
            ("Piso_Almacen_Sudeste", new Vector2(20f, -13f), new Vector2(21f, 16f))
        };

        foreach (var p in pisos)
        {
            GameObject go = GameObject.Find(p.nombre);
            if (go == null)
            {
                go = new GameObject(p.nombre);
                go.transform.SetParent(root.transform);
                go.transform.position = new Vector3(p.pos.x, p.pos.y, 0f);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = floorSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = p.size;
                sr.sortingOrder = -500;
            }
            else
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = -500;
            }
        }

        GameObject[] todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var a in todos)
        {
            if (a.name.StartsWith("Alfombra") || a.name.Contains("Carpet"))
            {
                SpriteRenderer sr = a.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = -400;
            }
        }
    }

    private static void ConfigurarArcoConPasoLibre(GameObject root, string nombreArco, Vector2 posBase)
    {
        GameObject arco = GameObject.Find(nombreArco);
        if (arco == null)
        {
            arco = new GameObject(nombreArco);
            arco.transform.SetParent(root.transform);
            arco.transform.position = posBase;
            arco.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            SpriteRenderer sr = arco.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Props/prop_arch");
        }

        SpriteRenderer arcoSr = arco.GetComponent<SpriteRenderer>();
        if (arcoSr != null)
        {
            // El dintel de la puerta queda por encima de la cabeza del personaje.
            // Al calcular el orden con la base inferior de los postes (posBase.y - 1.4f),
            // el jugador pasa naturalmente POR DEBAJO de las puertas y nunca por encima.
            float basePostesY = posBase.y - 1.4f;
            arcoSr.sortingOrder = 500 + Mathf.RoundToInt(-basePostesY * 10f);
        }

        BoxCollider2D[] viejosCol = arco.GetComponents<BoxCollider2D>();
        foreach (var c in viejosCol)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        // Colisionadores en los postes laterales para que el jugador cruce exactamente por el vano central
        BoxCollider2D colIzq = arco.AddComponent<BoxCollider2D>();
        colIzq.offset = new Vector2(-0.833f, 0f);
        colIzq.size = new Vector2(0.333f, 2.0f);
        colIzq.isTrigger = false;

        BoxCollider2D colDer = arco.AddComponent<BoxCollider2D>();
        colDer.offset = new Vector2(0.833f, 0f);
        colDer.size = new Vector2(0.333f, 2.0f);
        colDer.isTrigger = false;
    }

    private static void AjustarMurosVisualesExistentes()
    {
        GameObject[] todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject go in todos)
        {
            if (go.name == "Muro_Museo")
            {
                Vector3 p = go.transform.position;
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

                // Muros horizontales del Pasillo Norte (Y ≈ 4.8)
                if (Mathf.Abs(p.y - 4.8f) < 0.6f)
                {
                    if (p.x < -18f)
                    {
                        go.transform.position = new Vector3(-26.75f, 4.8f, 0f);
                        if (sr != null) sr.size = new Vector2(11.5f, 1.6f);
                    }
                    else if (p.x < 0f)
                    {
                        go.transform.position = new Vector3(-10.0f, 4.8f, 0f);
                        if (sr != null) sr.size = new Vector2(18.0f, 1.6f);
                    }
                    else if (p.x < 18f)
                    {
                        go.transform.position = new Vector3(10.0f, 4.8f, 0f);
                        if (sr != null) sr.size = new Vector2(18.0f, 1.6f);
                    }
                    else
                    {
                        go.transform.position = new Vector3(26.75f, 4.8f, 0f);
                        if (sr != null) sr.size = new Vector2(11.5f, 1.6f);
                    }

                    if (sr != null) sr.sortingOrder = 440;

                    BoxCollider2D col = go.GetComponent<BoxCollider2D>();
                    if (col != null)
                    {
                        if (Application.isPlaying) Destroy(col);
                        else DestroyImmediate(col);
                    }
                }
                // Muros horizontales del Pasillo Sur (Y ≈ -4.8)
                else if (Mathf.Abs(p.y - (-4.8f)) < 0.6f)
                {
                    if (p.x < -18f)
                    {
                        go.transform.position = new Vector3(-26.75f, -4.8f, 0f);
                        if (sr != null) sr.size = new Vector2(11.5f, 1.6f);
                    }
                    else if (p.x < 0f)
                    {
                        go.transform.position = new Vector3(-10.0f, -4.8f, 0f);
                        if (sr != null) sr.size = new Vector2(18.0f, 1.6f);
                    }
                    else if (p.x < 18f)
                    {
                        go.transform.position = new Vector3(10.0f, -4.8f, 0f);
                        if (sr != null) sr.size = new Vector2(18.0f, 1.6f);
                    }
                    else
                    {
                        go.transform.position = new Vector3(26.75f, -4.8f, 0f);
                        if (sr != null) sr.size = new Vector2(11.5f, 1.6f);
                    }

                    if (sr != null) sr.sortingOrder = 556;

                    BoxCollider2D col = go.GetComponent<BoxCollider2D>();
                    if (col != null)
                    {
                        if (Application.isPlaying) Destroy(col);
                        else DestroyImmediate(col);
                    }
                }
                else
                {
                    if (sr != null)
                    {
                        float footY = p.y - 0.8f;
                        sr.sortingOrder = 500 + Mathf.RoundToInt(-footY * 10f);
                    }
                }
            }
        }
    }

    private static void ConstruirMurosHermeticos(GameObject root)
    {
        Transform contenedorMuros = root.transform.Find("Muros_Hermeticos_Fisica");
        if (contenedorMuros != null)
        {
            if (Application.isPlaying) Destroy(contenedorMuros.gameObject);
            else DestroyImmediate(contenedorMuros.gameObject);
        }

        GameObject contenedor = new GameObject("Muros_Hermeticos_Fisica");
        contenedor.transform.SetParent(root.transform);
        contenedor.transform.position = Vector3.zero;

        // Muros Norte (Y = 4.8) - Vanos en X = -20, 0, 20
        CrearBloqueMuroSolido(contenedor, "MuroNorte_Oeste", new Vector2(-26.65f, 4.8f), new Vector2(10.7f, 1.8f));
        CrearBloqueMuroSolido(contenedor, "MuroNorte_CentroOeste", new Vector2(-10.0f, 4.8f), new Vector2(17.4f, 1.8f));
        CrearBloqueMuroSolido(contenedor, "MuroNorte_CentroEste", new Vector2(10.0f, 4.8f), new Vector2(17.4f, 1.8f));
        CrearBloqueMuroSolido(contenedor, "MuroNorte_Este", new Vector2(26.65f, 4.8f), new Vector2(10.7f, 1.8f));

        // Muros Sur (Y = -4.8) - Vanos en X = -20, 0, 20
        CrearBloqueMuroSolido(contenedor, "MuroSur_Oeste", new Vector2(-26.65f, -4.8f), new Vector2(10.7f, 1.8f));
        CrearBloqueMuroSolido(contenedor, "MuroSur_CentroOeste", new Vector2(-10.0f, -4.8f), new Vector2(17.4f, 1.8f));
        CrearBloqueMuroSolido(contenedor, "MuroSur_CentroEste", new Vector2(10.0f, -4.8f), new Vector2(17.4f, 1.8f));
        CrearBloqueMuroSolido(contenedor, "MuroSur_Este", new Vector2(26.65f, -4.8f), new Vector2(10.7f, 1.8f));

        // Divisorios verticales
        CrearBloqueMuroSolido(contenedor, "Divisorio_Oficina_Boveda", new Vector2(-10.25f, 13.2f), new Vector2(1.8f, 16.5f));
        CrearBloqueMuroSolido(contenedor, "Divisorio_Boveda_Galeria", new Vector2(10.25f, 13.2f), new Vector2(1.8f, 16.5f));
        CrearBloqueMuroSolido(contenedor, "Divisorio_Cripta_Santuario", new Vector2(-10.25f, -13.2f), new Vector2(1.8f, 16.5f));
        CrearBloqueMuroSolido(contenedor, "Divisorio_Santuario_Almacen", new Vector2(10.25f, -13.2f), new Vector2(1.8f, 16.5f));

        // Perímetros exteriores
        CrearBloqueMuroSolido(contenedor, "Perimetro_Norte", new Vector2(0f, 21.2f), new Vector2(66f, 2.0f));
        CrearBloqueMuroSolido(contenedor, "Perimetro_Sur", new Vector2(0f, -21.8f), new Vector2(66f, 2.0f));
        CrearBloqueMuroSolido(contenedor, "Perimetro_Oeste", new Vector2(-30.6f, 0f), new Vector2(2.0f, 44f));
        CrearBloqueMuroSolido(contenedor, "Perimetro_Este", new Vector2(30.6f, 0f), new Vector2(2.0f, 44f));
    }

    private static void CrearBloqueMuroSolido(GameObject parent, string nombre, Vector2 pos, Vector2 tam)
    {
        GameObject bloque = new GameObject(nombre);
        bloque.transform.SetParent(parent.transform);
        bloque.transform.position = pos;

        BoxCollider2D col = bloque.AddComponent<BoxCollider2D>();
        col.size = tam;
        col.isTrigger = false;
    }

    private static void AjustarVitrinasMuseo(GameObject root)
    {
        Sprite vitrinaSprite = Resources.Load<Sprite>("Props/vitrina_momia");
        if (vitrinaSprite == null) vitrinaSprite = Resources.Load<Sprite>("Tiles/prop_showcase");

        GameObject[] todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> vitrinas = new List<GameObject>();
        foreach (var go in todos)
        {
            if (go.name.StartsWith("Vitrina_Momia") || go.name.StartsWith("VitrinaSaqueada") || go.name.Contains("Vitrina"))
            {
                vitrinas.Add(go);
            }
        }

        // Posiciones exactas y no solapadas de vitrinas en el pasillo central
        Vector2[] posPasillo = new Vector2[]
        {
            new Vector2(-26.0f, 3.65f),
            new Vector2(-16.0f, 3.65f),
            new Vector2(8.0f, 3.65f),
            new Vector2(26.0f, 3.65f),

            new Vector2(-25.5f, -3.65f),
            new Vector2(-15.5f, -3.65f),
            new Vector2(15.5f, -3.65f),
            new Vector2(25.5f, -3.65f)
        };

        // Asignar o crear vitrinas para las posiciones del pasillo
        int pIdx = 0;
        foreach (var v in vitrinas)
        {
            if (v == null || v.name.Contains("Rota") || v.name.Contains("Saqueada")) continue;

            // Si está en el pasillo (entre Y -5 y +5)
            if (Mathf.Abs(v.transform.position.y) < 5.0f && pIdx < posPasillo.Length)
            {
                v.transform.position = posPasillo[pIdx++];
            }

            SpriteRenderer sr = v.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = vitrinaSprite;
                sr.sortingOrder = 500 + Mathf.RoundToInt(-(v.transform.position.y - 0.9f) * 10f);
            }

            BoxCollider2D col = v.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = false;
                float offsetY = (v.transform.position.y >= 0) ? 0.75f : -0.75f;
                col.size = new Vector2(1.5f, 3.2f);
                col.offset = new Vector2(0f, offsetY);
            }
        }
    }

    private static void AjustarCuadrosOleo(GameObject root)
    {
        // Ubicar cuadros en espacios de pared completamente libres y limpios
        Vector2[] posCuadros = new Vector2[]
        {
            new Vector2(-23.0f, 3.8f),   // Pasillo norte oeste
            new Vector2(-8.0f, 3.8f),    // Pasillo norte centro-oeste
            new Vector2(4.0f, 3.8f),     // Pasillo norte centro-este
            new Vector2(23.0f, 3.8f),    // Pasillo norte este

            new Vector2(-10.0f, -3.8f),  // Pasillo sur centro-oeste
            new Vector2(10.0f, -3.8f),   // Pasillo sur centro-este

            new Vector2(-20.0f, 20.4f),  // Oficina norte
            new Vector2(20.0f, 20.4f)    // Galería norte
        };

        GameObject[] todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> cuadros = new List<GameObject>();
        foreach (var go in todos)
        {
            if (go.name.StartsWith("Cuadro_Oleo") || go.name.Contains("Cuadro"))
            {
                cuadros.Add(go);
            }
        }

        for (int i = 0; i < cuadros.Count; i++)
        {
            if (i < posCuadros.Length)
            {
                cuadros[i].transform.position = posCuadros[i];
                SpriteRenderer sr = cuadros[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = (posCuadros[i].y > 0) ? 445 : 557;
                }
            }
            else
            {
                // Cuadros sobrantes: destruir para evitar duplicados encimados
                if (Application.isPlaying) Destroy(cuadros[i]);
                else DestroyImmediate(cuadros[i]);
            }
        }
    }

    private static void AjustarAltaresVeladoras(GameObject root)
    {
        // Altar Muro Central Norte: ubicado en X = -4.0, Y = 3.2 (separado 4m del cuadro en -8 y 12m de vitrina en -16)
        GameObject altarCentral = GameObject.Find("Altar_MuroCentralNorte");
        if (altarCentral != null)
        {
            altarCentral.transform.position = new Vector3(-4.0f, 3.2f, 0f);
            SpriteRenderer sr = altarCentral.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 455;
        }

        // Altar Santuario Sur: X = -6.0, Y = -15.5 (separado 12m de la ficha de lore en +6)
        GameObject altarSantuario = GameObject.Find("Altar_SantuarioSur");
        if (altarSantuario != null)
        {
            altarSantuario.transform.position = new Vector3(-6.0f, -15.5f, 0f);
            SpriteRenderer sr = altarSantuario.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 555;
        }

        // Altar Bóveda: X = 7.0, Y = 13.5
        GameObject altarBoveda = GameObject.Find("Altar_GranBoveda");
        if (altarBoveda != null)
        {
            altarBoveda.transform.position = new Vector3(7.0f, 13.5f, 0f);
        }

        // Altar Cripta: X = -13.5, Y = -13.5
        GameObject altarCripta = GameObject.Find("Altar_Cripta");
        if (altarCripta != null)
        {
            altarCripta.transform.position = new Vector3(-13.5f, -13.5f, 0f);
        }
    }

    private static void AjustarFichasLore(GameObject root)
    {
        if (LoreUIController.Instance == null)
        {
            GameObject uiGo = GameObject.Find("LoreUIController_Singleton");
            if (uiGo == null)
            {
                uiGo = new GameObject("LoreUIController_Singleton");
                uiGo.AddComponent<LoreUIController>();
            }
        }

        // Fichas de historia en posiciones completamente despejadas
        (string nombre, Vector2 pos, string titulo, string desc)[] fichas = new[]
        {
            ("FichaLore_Reglamento", new Vector2(6.0f, -15.5f),
                "Reglamento del Museo y Toque de Queda (12:00 AM)",
                "Aviso al guardia nocturno:\nEn punto de la medianoche, todas las salas deben permanecer cerradas. Si escuchas ruidos de vitrinas rotas, dirígete a la oficina y toma tu arma de servicio."),

            ("FichaLore_Colera", new Vector2(-12.0f, 3.2f),
                "La Epidemia de Cólera Morbus (1833)",
                "Durante el brote de cólera morbus de 1833, cientos de cuerpos fueron sepultados en el Panteón de Santa Paula. El clima árido y el nitrato del suelo momificaron sus restos, sumiéndolos en una vigilia eterna."),

            ("FichaLore_MomiaErrante", new Vector2(12.0f, 3.2f),
                "Bitácora del Custodio Mayor - La Que Acecha",
                "La que camina en la sombra no puede avanzar si tus ojos están fijos en ella. Mantén tu mirada al frente y nunca retrocedas sin vigilar tu espalda."),

            ("FichaLore_GranBoveda", new Vector2(-7.0f, 13.5f),
                "La Vitrina Central de la Bóveda - El Despertar",
                "Aquí reposaba el espécimen más antiguo rescatado del panteón colonial. Al sonar la medianoche, el cristal estalló desde su interior y la entidad despertó buscando a los vivos."),

            ("FichaLore_Cripta", new Vector2(-26.5f, -13.5f),
                "La Compuerta de Escape y los Túneles Coloniales",
                "Bajo las baldosas corre el antiguo túnel de desagüe virreinal hacia el exterior. La compuerta pesada requiere la Llave Maestra Colonial para abrir los cerrojos."),

            ("FichaLore_Almacen", new Vector2(20.0f, -14.5f),
                "Registro de Plagas y Escarabajos de Sepulcro",
                "Las excavaciones perturbaron los nidos de escarabajos de sepulcro (Necrophila). Atacan por los flancos y son atraídos por el calor vivo; el plomo o el fuego los destruye.")
        };

        foreach (var f in fichas)
        {
            CrearOReubicarFicha(root, f.nombre, f.pos, f.titulo, f.desc);
        }
    }

    private static void CrearOReubicarFicha(GameObject root, string nombre, Vector2 pos, string titulo, string desc)
    {
        Sprite noteSprite = Resources.Load<Sprite>("Props/lore_note");
        if (noteSprite == null) noteSprite = Resources.Load<Sprite>("Props/lore_plaque");

        GameObject ficha = GameObject.Find(nombre);
        if (ficha == null)
        {
            ficha = new GameObject(nombre);
            ficha.transform.SetParent(root.transform);
        }

        ficha.transform.position = pos;
        ficha.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        SpriteRenderer sr = ficha.GetComponent<SpriteRenderer>();
        if (sr == null) sr = ficha.AddComponent<SpriteRenderer>();
        if (noteSprite != null) sr.sprite = noteSprite;
        sr.sortingOrder = 460;

        BoxCollider2D col = ficha.GetComponent<BoxCollider2D>();
        if (col == null) col = ficha.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.8f, 1.8f);

        InteractableObject io = ficha.GetComponent<InteractableObject>();
        if (io == null) io = ficha.AddComponent<InteractableObject>();
        io.Configurar(InteractableType.LoreTablet, titulo, desc);
    }

    private static void AjustarPuertaEscape(GameObject root)
    {
        GameObject puerta = GameObject.Find("PuertaSeguridad");
        if (puerta == null)
        {
            puerta = new GameObject("PuertaSeguridad");
            puerta.transform.SetParent(root.transform);
        }

        puerta.transform.position = new Vector3(-20.0f, -17.0f, 0f);
        puerta.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        SpriteRenderer sr = puerta.GetComponent<SpriteRenderer>();
        if (sr == null) sr = puerta.AddComponent<SpriteRenderer>();
        Sprite spriteCerrada = Resources.Load<Sprite>("Props/puerta_escape_closed");
        if (spriteCerrada != null && (sr.sprite == null || sr.sprite.name != "puerta_escape_opened"))
        {
            sr.sprite = spriteCerrada;
        }
        sr.sortingOrder = 580;

        PuertaSalida script = puerta.GetComponent<PuertaSalida>();
        if (script == null) script = puerta.AddComponent<PuertaSalida>();
        script.distanciaInteraccion = 3.6f;

        BoxCollider2D[] cols = puerta.GetComponents<BoxCollider2D>();
        foreach (var c in cols)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        BoxCollider2D colSolido = puerta.AddComponent<BoxCollider2D>();
        colSolido.isTrigger = false;
        colSolido.size = new Vector2(3.8f, 4.4f);

        BoxCollider2D colTrigger = puerta.AddComponent<BoxCollider2D>();
        colTrigger.isTrigger = true;
        colTrigger.size = new Vector2(4.8f, 5.0f);
    }

    private static void EliminarSuperposicionesResiduales()
    {
        // Escanea y elimina cualquier prop secundario que haya quedado a menos de 1.8m de otro
        GameObject[] todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> props = new List<GameObject>();
        foreach (var go in todos)
        {
            if (go == null) continue;
            string n = go.name;
            if (n.Contains("Vitrina") || n.Contains("Altar") || n.Contains("Cuadro") || n.Contains("Ficha") || n.Contains("Estatua"))
            {
                props.Add(go);
            }
        }

        HashSet<GameObject> aDestruir = new HashSet<GameObject>();
        for (int i = 0; i < props.Count; i++)
        {
            if (props[i] == null || aDestruir.Contains(props[i])) continue;
            for (int j = i + 1; j < props.Count; j++)
            {
                if (props[j] == null || aDestruir.Contains(props[j])) continue;

                float dist = Vector2.Distance(props[i].transform.position, props[j].transform.position);
                if (dist < 1.8f)
                {
                    // Priorizar conservar vitrina o ficha de lore sobre cuadros/duplicados
                    if (props[j].name.Contains("Cuadro") || props[j].name.Contains("Altar"))
                    {
                        aDestruir.Add(props[j]);
                    }
                    else if (props[i].name.Contains("Cuadro"))
                    {
                        aDestruir.Add(props[i]);
                    }
                    else
                    {
                        aDestruir.Add(props[j]);
                    }
                }
            }
        }

        foreach (var go in aDestruir)
        {
            if (go != null)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }
    }
}
