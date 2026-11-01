using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controlador de la linterna de mano del vigilante para el Nivel 2.
/// Ahora incluye la linterna visible en la mano del guardia con rotación direccional.
/// Orienta el cono de luz según la dirección hacia donde mira o camina el jugador,
/// añade oscilación natural de paso y detecta elementos sensibles a la luz (como ratas).
/// NO TOCA NINGÚN ARCHIVO DEL NIVEL 1.
/// </summary>
public class LinternaVigilanteNivel2 : MonoBehaviour
{
    [Header("Configuración del Haz de Luz")]
    public float velocidadGiro = 14f;
    public float distanciaLuz = 8.5f;
    public float anguloCono = 75f;
    public float intensidadBase = 1.6f;

    [Header("Efecto de Oscilación de Pasos")]
    public float bamboleoFrecuencia = 8f;
    public float bamboleoAmplitud = 0.08f;

    private Light2D linternaLight;
    private SpriteRenderer linternaSr;
    private PlayerController playerController;
    private Rigidbody2D playerRb;
    private Vector2 direccionApuntado = Vector2.down;
    private float tiempoPaso = 0f;

    private void Awake()
    {
        // La linterna es EXCLUSIVA del Nivel 2 (Subterráneo oscuro).
        // En Nivel 1 o Menu Principal no debe existir bajo ninguna circunstancia.
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Nivel1" || sceneName == "MenuPrincipal" || !sceneName.Contains("2"))
        {
            Transform v = transform.Find("VisualLinternaMano");
            if (v != null)
            {
                if (Application.isPlaying) Destroy(v.gameObject);
                else DestroyImmediate(v.gameObject);
            }
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
            return;
        }

        linternaLight = GetComponent<Light2D>();
        if (linternaLight == null) linternaLight = gameObject.AddComponent<Light2D>();

        linternaLight.lightType = Light2D.LightType.Point;
        linternaLight.color = new Color(1f, 0.96f, 0.84f, 1f); // Tono haz cálido incandescente
        linternaLight.intensity = intensidadBase;
        linternaLight.pointLightOuterRadius = distanciaLuz;
        linternaLight.pointLightInnerRadius = 0.4f;
        linternaLight.pointLightOuterAngle = anguloCono;
        linternaLight.pointLightInnerAngle = anguloCono * 0.35f;

        Transform padre = transform.parent;
        if (padre != null)
        {
            playerController = padre.GetComponent<PlayerController>();
            playerRb = padre.GetComponent<Rigidbody2D>();
        }

        CrearVisualLinternaEnMano();
    }

    private void CrearVisualLinternaEnMano()
    {
        Transform child = transform.Find("VisualLinternaMano");
        GameObject visualObj;
        if (child == null)
        {
            visualObj = new GameObject("VisualLinternaMano");
            visualObj.transform.SetParent(transform);
        }
        else
        {
            visualObj = child.gameObject;
        }

        linternaSr = visualObj.GetComponent<SpriteRenderer>();
        if (linternaSr == null) linternaSr = visualObj.AddComponent<SpriteRenderer>();

        linternaSr.sprite = GenerarSpriteLinternaTactical();
        linternaSr.sortingOrder = 12; // Por encima del uniforme del vigilante
        visualObj.transform.localPosition = new Vector3(0.08f, 0.12f, 0f);
        visualObj.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        visualObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f); // Dimensiones naturales proporcionales a las manos
    }

    private Sprite GenerarSpriteLinternaTactical()
    {
        int w = 14;
        int h = 6;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // Pixel art nítido
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0, 0, 0, 0);

        Color cuerpoNegro = new Color(0.18f, 0.18f, 0.22f, 1f);
        Color anilloMetal = new Color(0.55f, 0.58f, 0.62f, 1f);
        Color cabezal = new Color(0.32f, 0.34f, 0.38f, 1f);
        Color lenteAmarilla = new Color(1.0f, 0.95f, 0.45f, 1f);

        // Cuerpo cilíndrico de la linterna (X: 1 a 8)
        for (int x = 1; x <= 8; x++)
        {
            for (int y = 1; y <= 4; y++)
            {
                cols[y * w + x] = ((x == 3 || x == 6) ? anilloMetal : cuerpoNegro);
            }
        }
        // Interruptor / tapa posterior (X: 0)
        cols[2 * w + 0] = anilloMetal;
        cols[3 * w + 0] = anilloMetal;

        // Cabezal ampliado (X: 9 a 12)
        for (int x = 9; x <= 12; x++)
        {
            for (int y = 0; y <= 5; y++)
            {
                cols[y * w + x] = (x == 10) ? anilloMetal : cabezal;
            }
        }

        // Lente brillante frontal emisora de luz (X: 13)
        for (int y = 1; y <= 4; y++)
        {
            cols[y * w + 13] = lenteAmarilla;
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }

    private void Update()
    {
        if (playerController == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerController = p.GetComponent<PlayerController>();
                playerRb = p.GetComponent<Rigidbody2D>();
            }
        }

        if (playerController != null)
        {
            Vector2 dirActual = playerController.facingDirection;
            if (dirActual.sqrMagnitude > 0.01f)
            {
                direccionApuntado = dirActual;
            }
        }

        // Rotar suavemente el cono de luz y el sprite de la linterna hacia donde apunta el guardia
        float anguloZ = Mathf.Atan2(direccionApuntado.y, direccionApuntado.x) * Mathf.Rad2Deg - 90f;
        Quaternion rotObjetivo = Quaternion.Euler(0, 0, anguloZ);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotObjetivo, Time.deltaTime * velocidadGiro);

        // Posicionar dinámicamente según hacia dónde mire (en la mano correspondiente con escala ajustada)
        Vector3 offsetMano = new Vector3(0.08f, 0.08f, 0f);
        if (direccionApuntado.x > 0.1f) offsetMano = new Vector3(0.14f, -0.04f, 0f);
        else if (direccionApuntado.x < -0.1f) offsetMano = new Vector3(-0.14f, -0.04f, 0f);
        else if (direccionApuntado.y > 0.1f) offsetMano = new Vector3(0.10f, 0.10f, 0f);
        else if (direccionApuntado.y < -0.1f) offsetMano = new Vector3(-0.10f, -0.12f, 0f);

        // Bamboleo natural al caminar
        bool enMovimiento = playerRb != null && playerRb.linearVelocity.magnitude > 0.2f;
        if (enMovimiento)
        {
            tiempoPaso += Time.deltaTime * bamboleoFrecuencia;
            float offsetBamboleo = Mathf.Sin(tiempoPaso) * bamboleoAmplitud;
            transform.localPosition = offsetMano + new Vector3(offsetBamboleo, 0f, 0f);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, offsetMano, Time.deltaTime * 6f);
        }
    }

    public bool EstaIluminando(Vector3 puntoObjetivo)
    {
        Vector2 haciaPunto = (Vector2)(puntoObjetivo - transform.position);
        float distancia = haciaPunto.magnitude;
        if (distancia > distanciaLuz) return false;

        float angulo = Vector2.Angle(direccionApuntado, haciaPunto);
        return angulo <= (anguloCono * 0.5f);
    }
}
