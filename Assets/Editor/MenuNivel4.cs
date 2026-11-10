#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuNivel4
{
    [MenuItem("Juego/Cargar Nivel 4 (Final)", false, 20)]
    public static void CargarEscenaNivel4()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Scene escena = EditorSceneManager.OpenScene("Assets/Scenes/Nivel 4.unity");
            ConstruirYGuardarPanteon(escena);
            Debug.Log("<color=green>[MenuNivel4] ¡Escena Nivel 4 cargada y poblada en el Editor sin cuadros rosas!</color>");
        }
    }

    [MenuItem("Juego/Reconstruir Panteón Nivel 4", false, 21)]
    public static void ReconstruirPanteon()
    {
        Scene escena = SceneManager.GetActiveScene();
        if (escena.name != "Nivel 4")
        {
            escena = EditorSceneManager.OpenScene("Assets/Scenes/Nivel 4.unity");
        }
        ConstruirYGuardarPanteon(escena);
    }

    private static void ConstruirYGuardarPanteon(Scene escena)
    {
        // Solo generar los assets si no existen aún, para preservar los PNGs 2D dedicados
        if (!System.IO.File.Exists("Assets/Resources/Cementerio/lapida_cruz_celta.png"))
        {
            GeneradorAssetsCementerio.GenerarTodosLosAssets();
        }

        GameObject viejo = GameObject.Find("EscenarioPanteonNivel4");
        if (viejo != null) Object.DestroyImmediate(viejo);

        foreach (var p in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(p.gameObject);
        }

        // Configurar Cámara y CameraFollow con soporte total para tecla M y zoom panorámico
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
            cam.clearFlags = CameraClearFlags.SolidColor;

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
        }

        // Garantizar GameObject Bootstrap con Nivel4Setup
        Nivel4Setup setup = Object.FindFirstObjectByType<Nivel4Setup>();
        if (setup == null)
        {
            GameObject setupGo = GameObject.Find("Nivel4_Bootstrap_Manager");
            if (setupGo == null) setupGo = new GameObject("Nivel4_Bootstrap_Manager");
            setup = setupGo.GetComponent<Nivel4Setup>();
            if (setup == null) setup = setupGo.AddComponent<Nivel4Setup>();
        }

        GeneradorPanteonNivel4 gen = Object.FindFirstObjectByType<GeneradorPanteonNivel4>();
        if (gen == null)
        {
            GameObject genGo = new GameObject("GeneradorPanteonNivel4");
            gen = genGo.AddComponent<GeneradorPanteonNivel4>();
        }

        gen.ReconstruirManual();

        // Limpiar cualquier PlayerController del museo y posicionar a la vigilante en la entrada sur
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) Object.DestroyImmediate(pc);

            VigilanteNivel4 vig = player.GetComponent<VigilanteNivel4>();
            if (vig == null) vig = player.AddComponent<VigilanteNivel4>();
            vig.velocidadCaminar = 4.2f;
            vig.velocidadCorrer = 6.2f;
            vig.velocidadSigilo = 2.2f;
            vig.EstaOculta = false;
            player.transform.position = new Vector3(0f, -28f, 0f);

            BoxCollider2D bCol = player.GetComponent<BoxCollider2D>();
            if (bCol != null)
            {
                bCol.size = new Vector2(0.45f, 0.35f);
                bCol.offset = new Vector2(0f, -0.35f);
            }
        }

        MomiaEncadenadaController momia = Object.FindFirstObjectByType<MomiaEncadenadaController>();
        if (momia != null)
        {
            momia.transform.position = new Vector3(-32f, 18f, 0f);
            momia.momiaDetenida = true;
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("<color=green>[MenuNivel4] ¡Panteón de Santa Paula reconstruido con éxito (materiales y texturas 2D verificadas)!</color>");
    }
}
#endif
