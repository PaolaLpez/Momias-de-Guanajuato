using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Bootstrap automático e inicializador del Nivel 4 (Panteón de Santa Paula).
/// Se ejecuta automáticamente al cargar la escena "Nivel 4".
/// Prepara la cámara, la iluminación lúgubre de medianoche, el generador del panteón y a la vigilante.
/// CERO dependencias o modificaciones con los niveles anteriores.
/// </summary>
[DefaultExecutionOrder(-200)]
public class Nivel4Setup : MonoBehaviour
{
    private void Awake()
    {
        Time.timeScale = 1f;

        // 1. Garantizar AudioManagerNivel4
        if (FindFirstObjectByType<AudioManagerNivel4>() == null)
        {
            GameObject aMgr = new GameObject("AudioManagerNivel4");
            aMgr.AddComponent<AudioManagerNivel4>();
        }

        // 2. Garantizar Nivel4Manager
        if (FindFirstObjectByType<Nivel4Manager>() == null)
        {
            GameObject mgr = new GameObject("Nivel4Manager");
            mgr.AddComponent<Nivel4Manager>();
        }

        // 3. Garantizar GeneradorPanteonNivel4
        if (FindFirstObjectByType<GeneradorPanteonNivel4>() == null)
        {
            GameObject gen = new GameObject("GeneradorPanteonNivel4");
            gen.AddComponent<GeneradorPanteonNivel4>();
        }

        // 4. Configurar Cámara Principal y Seguimiento
        ConfigurarCamara();

        // 5. Configurar Iluminación Global
        ConfigurarIluminacionGlobal();

        // 6. Configurar Vigilante (Personaje principal de Nivel 2)
        ConfigurarVigilante();
    }

    private void ConfigurarCamara()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
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
            cf.zoomMapaCompleto = 44f; // Zoom panorámico amplio para abarcar el gran laberinto del panteón
            cf.centroVistaPanoramica = new Vector3(0f, 4f, -10f); // Centro del panteón para la tecla M
            cf.suavizado = 8f;
            cf.permitirTeclaM = false; // El mapa con [M] se desbloquea tras encontrar 2 reliquias
            cf.respetarLimitesExternos = true;
            cf.minX = -44f;
            cf.maxX = 44f;
            cf.minY = -34f;
            cf.maxY = 42f;
        }
    }

    private void ConfigurarIluminacionGlobal()
    {
        Light2D luzGlobal = null;
        Light2D[] luces = FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        string escenaActual = gameObject.scene.name;

        // Priorizar la "Global Light 2D" propia de la escena actual para evitar duplicados en URP 2D
        foreach (var l in luces)
        {
            if (l != null && l.lightType == Light2D.LightType.Global && l.gameObject.scene.name == escenaActual)
            {
                if (luzGlobal == null || l.gameObject.name == "Global Light 2D")
                {
                    luzGlobal = l;
                }
            }
        }

        // Si no había ninguna en la escena actual, reutilizar alguna existente
        if (luzGlobal == null)
        {
            foreach (var l in luces)
            {
                if (l != null && l.lightType == Light2D.LightType.Global)
                {
                    luzGlobal = l;
                    break;
                }
            }
        }

        // Desactivar y destruir cualquier Global Light 2D adicional que cause el error de Light2D:OnEnable
        foreach (var l in luces)
        {
            if (l != null && l != luzGlobal && l.lightType == Light2D.LightType.Global)
            {
                l.enabled = false;
                Destroy(l.gameObject);
            }
        }

        if (luzGlobal == null)
        {
            GameObject lgObj = new GameObject("LuzGlobalPanteon");
            luzGlobal = lgObj.AddComponent<Light2D>();
            luzGlobal.lightType = Light2D.LightType.Global;
        }
        else
        {
            if (!luzGlobal.gameObject.activeSelf) luzGlobal.gameObject.SetActive(true);
            if (!luzGlobal.enabled) luzGlobal.enabled = true;
        }

        luzGlobal.color = new Color(0.08f, 0.10f, 0.18f, 1f); // Azul frío gélido de noche en Guanajuato
        luzGlobal.intensity = 0.15f;
    }

    private void ConfigurarVigilante()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = new GameObject("Vigilante");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, -28f, 0f); // Entrada sur frente al gran portón monumental en el atrio
            player.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Vigilante/vigilante_idle");
            if (sr.sprite == null)
            {
                Sprite[] sps = Resources.LoadAll<Sprite>("Vigilante");
                if (sps != null && sps.Length > 0) sr.sprite = sps[0];
            }
            sr.sortingOrder = 10;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = player.AddComponent<BoxCollider2D>();
            // Colisionador ajustado a los pies para navegación 2D top-down suave sin atascos
            col.size = new Vector2(0.45f, 0.35f);
            col.offset = new Vector2(0f, -0.35f);
        }

        // ELIMINAR PlayerController del museo si existía en el GameObject para evitar bloqueos por límites del museo
        PlayerController pcViejo = player.GetComponent<PlayerController>();
        if (pcViejo != null)
        {
            if (Application.isPlaying) Destroy(pcViejo);
            else DestroyImmediate(pcViejo);
        }

        // Asegurar que el controlador exclusivo del Nivel 4 esté presente y activo
        VigilanteNivel4 vig = player.GetComponent<VigilanteNivel4>();
        if (vig == null)
        {
            vig = player.AddComponent<VigilanteNivel4>();
        }
        vig.velocidadCaminar = 4.2f;
        vig.velocidadCorrer = 6.2f;
        vig.velocidadSigilo = 2.2f;
        vig.EstaOculta = false;

        // Asegurar colisionador en los pies si ya existía
        BoxCollider2D bCol = player.GetComponent<BoxCollider2D>();
        if (bCol != null)
        {
            bCol.size = new Vector2(0.45f, 0.35f);
            bCol.offset = new Vector2(0f, -0.35f);
        }

        // Asignar objetivo a la cámara
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf != null)
            {
                cf.objetivo = player.transform;
                cf.respetarLimitesExternos = true;
                cf.minY = -34f;
            }
            cam.transform.position = new Vector3(
                Mathf.Clamp(player.transform.position.x, -44f, 44f),
                Mathf.Clamp(player.transform.position.y, -34f, 42f),
                -10f);
        }
    }
}
