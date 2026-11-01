using UnityEngine;

/// <summary>
/// Bootstrap automático para garantizar la inicialización del Nivel 3.
/// Se ejecuta automáticamente al cargar la escena "Nivel 3".
/// </summary>
[DefaultExecutionOrder(-200)]
public class Nivel3Setup : MonoBehaviour
{
    private void Awake()
    {
        Time.timeScale = 1f;

        // Procesar audio del escorpión y cortar sprites
        PreparadorAssetsNivel3.ProcesarAssets();

        // 1. Garantizar Nivel3Manager
        if (FindFirstObjectByType<Nivel3Manager>() == null)
        {
            GameObject mgr = new GameObject("Nivel3Manager");
            mgr.AddComponent<Nivel3Manager>();
        }

        // 2. Garantizar GeneradorEscenarioNivel3
        if (FindFirstObjectByType<GeneradorEscenarioNivel3>() == null)
        {
            GameObject gen = new GameObject("GeneradorEscenarioNivel3");
            gen.AddComponent<GeneradorEscenarioNivel3>();
        }

        // 3. Configurar Cámara Principal si existe
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
            cam.orthographicSize = 3.6f;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();
            cf.zoomCercano = 3.6f;
            cf.suavizado = 7f;
            cf.permitirTeclaM = false;
            cf.minX = -20f;
            cf.maxX = 20f;
            cf.minY = -12f;
            cf.maxY = 10f;
        }
    }
}
