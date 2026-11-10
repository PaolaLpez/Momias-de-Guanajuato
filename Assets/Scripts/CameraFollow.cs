using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform objetivo;
    public float suavizado = 6f;
    public Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Zoom")]
    public float zoomCercano = 3.6f;     // Zoom de seguimiento cercano de la vigilante
    public float zoomMapaCompleto = 16.5f; // Zoom panorámico de todo el museo
    public Vector3 centroVistaPanoramica = new Vector3(0f, 0f, -10f);
    public float velocidadZoom = 6f;

    // Límites de cámara que permiten recorrer libremente todo el Colosal Museo sin perder al vigilante
    public float minX = -24.8f;
    public float maxX = 24.8f;
    public float minY = -18.8f;
    public float maxY = 17.5f;

    [Header("Control de Tecla M")]
    public bool permitirTeclaM = true;
    public bool respetarLimitesExternos = false;

    private Camera cam;
    private bool viendoMapaCompleto = false;

    void Start()
    {
        if (!respetarLimitesExternos)
        {
            minY = -18.8f;
        }
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = zoomCercano;
        }

        BuscarJugador();
    }

    void BuscarJugador()
    {
        if (objetivo == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) objetivo = p.transform;
        }
    }

    void LateUpdate()
    {
        BuscarJugador();

        if (permitirTeclaM && Input.GetKeyDown(KeyCode.M))
        {
            AlternarMapaCompleto();
        }
        else if (!permitirTeclaM)
        {
            viendoMapaCompleto = false;
        }

        float targetZoom = viendoMapaCompleto ? zoomMapaCompleto : zoomCercano;
        if (cam != null)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * velocidadZoom);
        }

        if (viendoMapaCompleto)
        {
            // Vista general centrada en todo el complejo
            transform.position = Vector3.Lerp(transform.position, centroVistaPanoramica, Time.deltaTime * suavizado);
        }
        else
        {
            // SIEMPRE sigue con total precisión a la vigilante en tiempo real
            if (objetivo != null)
            {
                Vector3 destino = objetivo.position + offset;
                destino.x = Mathf.Clamp(destino.x, minX, maxX);
                destino.y = Mathf.Clamp(destino.y, minY, maxY);
                transform.position = Vector3.Lerp(transform.position, destino, Time.deltaTime * suavizado);
            }
        }
    }

    public void AlternarMapaCompleto()
    {
        viendoMapaCompleto = !viendoMapaCompleto;
    }

    public bool EstaViendoMapaCompleto()
    {
        return viendoMapaCompleto;
    }
}
