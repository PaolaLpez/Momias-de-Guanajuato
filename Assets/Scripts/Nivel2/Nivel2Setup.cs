using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Configurador inicial del Nivel 2.
/// Prepara la iluminación tenebrosa del subsuelo, la cámara, el generador del escenario y al vigilante con su linterna.
/// NO TOCA NINGÚN ARCHIVO DEL NIVEL 1.
/// </summary>
[ExecuteAlways]
public class Nivel2Setup : MonoBehaviour
{
    private void Awake()
    {
        EjecutarConfiguracion();
    }

    private void OnEnable()
    {
        EjecutarConfiguracion();
    }

    private void EjecutarConfiguracion()
    {
        ConfigurarGeneradorEscenario();
        ConfigurarIluminacionSubterranea();
        ConfigurarJugadorYLinterna();
        ConfigurarCamara();
        ConfigurarPlanoSubterraneo();
    }

    private void ConfigurarPlanoSubterraneo()
    {
        if (Application.isPlaying && FindFirstObjectByType<PlanoSubterraneoNivel2>() == null)
        {
            gameObject.AddComponent<PlanoSubterraneoNivel2>();
        }
    }

    private void ConfigurarIluminacionSubterranea()
    {
        // Buscar la luz global de la escena o crearla si no existe
        Light2D luzGlobal = null;
        GameObject lgObj = GameObject.Find("Global Light 2D");
        if (lgObj != null) luzGlobal = lgObj.GetComponent<Light2D>();

        if (luzGlobal == null)
        {
            var luces = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            foreach (var l in luces)
            {
                if (l.lightType == Light2D.LightType.Global)
                {
                    luzGlobal = l;
                    break;
                }
            }
        }

        if (luzGlobal != null)
        {
            // Oscuridad casi absoluta: luz de subsuelo lúgubre
            luzGlobal.color = new Color(0.12f, 0.14f, 0.20f, 1f);
            luzGlobal.intensity = 0.05f;
        }

        // Conectar la luz ambiental al Generador Principal
        GeneradorPrincipalNivel2 gen = FindFirstObjectByType<GeneradorPrincipalNivel2>();
        if (gen != null && luzGlobal != null)
        {
            gen.luzAmbientalSubterraneo = luzGlobal;
        }
    }

    private void ConfigurarGeneradorEscenario()
    {
        if (FindFirstObjectByType<GeneradorEscenarioNivel2>() == null)
        {
            GameObject genGo = new GameObject("SistemaEscenarioNivel2");
            genGo.AddComponent<GeneradorEscenarioNivel2>();
        }
    }

    private void ConfigurarJugadorYLinterna()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = new GameObject("Vigilante");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, -14f, 0f); // Entrada sur de las criptas
            player.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Vigilante/vigilante_idle");
            sr.sortingOrder = 10;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.55f, 0.85f);
            col.offset = new Vector2(0f, -0.1f);

            // Reutilizar el controlador del vigilante
            player.AddComponent<PlayerController>();
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.museoMinX = -25.5f;
            pc.museoMaxX = 25.5f;
            pc.museoMinY = -19.5f;
            pc.museoMaxY = 21.8f;
        }

        // Configurar Linterna URP 2D en el Vigilante
        Transform linternaTransform = player.transform.Find("LinternaSubterraneo");
        GameObject linternaObj;
        if (linternaTransform == null)
        {
            linternaObj = new GameObject("LinternaSubterraneo");
            linternaObj.transform.SetParent(player.transform);
            linternaObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        }
        else
        {
            linternaObj = linternaTransform.gameObject;
        }

        if (linternaObj.GetComponent<LinternaVigilanteNivel2>() == null)
        {
            linternaObj.AddComponent<LinternaVigilanteNivel2>();
        }

        // Configurar Pasos de Carrera Audibles para el Nivel 2
        if (player.GetComponent<PasosJugadorNivel2>() == null)
        {
            player.AddComponent<PasosJugadorNivel2>();
        }
    }

    private void ConfigurarCamara()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f); // Fondo de abismo oscuro
        CameraFollow cf = cam.GetComponent<CameraFollow>();
        if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            cf.objetivo = player.transform;
            cf.zoomCercano = 4.0f;
            cf.zoomMapaCompleto = 21f;
            cf.permitirTeclaM = false; // Eliminar función de tecla M en Nivel 2
            cf.minX = -26f;
            cf.maxX = 26f;
            cf.minY = -19.5f;
            cf.maxY = 21.5f;
        }
    }
}
