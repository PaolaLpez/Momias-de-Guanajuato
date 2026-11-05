using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de animación dinámica y efectos visuales para el Fondo del Menú Principal
/// de "Momias de Guanajuato: La Noche del Museo".
/// Incluye:
/// - Movimiento cinemático flotante continuo (sway & breathing zoom).
/// - Parallax suave con el cursor del mouse.
/// - Faroles de época coloniales parpadeantes con luz viva cálida.
/// - Capas de niebla nocturna desplazándose por la calle empedrada.
/// - Polvo de tumba / cenizas nocturnas flotando hacia el cielo.
/// - Resplandor tenue de relámpago / luz de luna ocasional.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FondoMenuAnimado : MonoBehaviour
{
    [Header("Parámetros de Movimiento y Parallax")]
    [SerializeField] private float intensidadMovimiento = 16f;
    [SerializeField] private float velocidadFlotacion = 0.35f;
    [SerializeField] private float escalaBase = 1.06f;
    [SerializeField] private float amplitudRespiracion = 0.025f;
    [SerializeField] private float fuerzaParallaxMouse = 22f;

    [Header("Efectos Atmosféricos")]
    [SerializeField] private bool activarFarolesVivos = true;
    [SerializeField] private bool activarNieblaMovimiento = true;
    [SerializeField] private bool activarPolvoTumba = true;
    [SerializeField] private bool activarResplandorLunar = true;

    private RectTransform rectTransform;
    private Vector2 posicionInicial;
    private Vector2 offsetMouseActual = Vector2.zero;

    // Elementos dinámicos generados
    private List<Image> farolesGlow = new List<Image>();
    private List<float> farolesSemillas = new List<float>();
    private RectTransform[] capasNiebla;
    private float[] velocidadesNiebla;
    private RectTransform[] motasPolvo;
    private Vector2[] velocidadesPolvo;
    private Image resplandorTormentaImg;
    private float proximoRelampagoTimer = 15f;
    private float relampagoDuracion = 0f;

    private static Sprite glowSpriteCompartido;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            posicionInicial = rectTransform.anchoredPosition;
            rectTransform.localScale = new Vector3(escalaBase, escalaBase, 1f);
        }

        CrearSpriteGlow();
        InicializarEfectos();
    }

    private void Update()
    {
        float t = Time.time;

        // 1. Movimiento cinemático de flotación suave y respiración
        if (rectTransform != null)
        {
            float offsetX = Mathf.Sin(t * velocidadFlotacion) * intensidadMovimiento +
                           Mathf.Cos(t * velocidadFlotacion * 0.7f) * (intensidadMovimiento * 0.5f);
            float offsetY = Mathf.Cos(t * velocidadFlotacion * 0.85f) * (intensidadMovimiento * 0.6f) +
                           Mathf.Sin(t * velocidadFlotacion * 0.45f) * (intensidadMovimiento * 0.3f);

            // Parallax con el cursor del mouse
            Vector2 mousePos = Input.mousePosition;
            float normX = (mousePos.x / Mathf.Max(1, Screen.width)) - 0.5f;
            float normY = (mousePos.y / Mathf.Max(1, Screen.height)) - 0.5f;
            Vector2 targetParallax = new Vector2(-normX * fuerzaParallaxMouse, -normY * fuerzaParallaxMouse);
            offsetMouseActual = Vector2.Lerp(offsetMouseActual, targetParallax, Time.deltaTime * 3.5f);

            rectTransform.anchoredPosition = posicionInicial + new Vector2(offsetX, offsetY) + offsetMouseActual;

            // Respiración de zoom sutil (breathing scale)
            float escalaDinamica = escalaBase + Mathf.Sin(t * 0.28f) * amplitudRespiracion;
            rectTransform.localScale = new Vector3(escalaDinamica, escalaDinamica, 1f);
        }

        // 2. Parpadeo orgánico de los faroles coloniales
        if (activarFarolesVivos && farolesGlow.Count > 0)
        {
            for (int i = 0; i < farolesGlow.Count; i++)
            {
                if (farolesGlow[i] == null) continue;
                float seed = farolesSemillas[i];
                float noise = Mathf.PerlinNoise(t * 5f, seed);
                float subNoise = Mathf.PerlinNoise(t * 14f, seed * 1.5f);
                float flicker = (noise * 0.7f + subNoise * 0.3f);

                float alpha = Mathf.Clamp(0.35f + (flicker - 0.5f) * 0.35f, 0.18f, 0.75f);
                farolesGlow[i].color = new Color(1f, 0.74f, 0.32f, alpha);

                float escalaFarol = 1f + (flicker - 0.5f) * 0.18f;
                farolesGlow[i].rectTransform.localScale = new Vector3(escalaFarol, escalaFarol, 1f);
            }
        }

        // 3. Movimiento de las capas de niebla
        if (activarNieblaMovimiento && capasNiebla != null)
        {
            for (int i = 0; i < capasNiebla.Length; i++)
            {
                if (capasNiebla[i] == null) continue;
                capasNiebla[i].anchoredPosition += new Vector2(velocidadesNiebla[i] * Time.deltaTime, 0f);

                // Reinicio continuo cuando se sale del ancho de pantalla
                float anchoRef = Screen.width > 0 ? Screen.width : 1920f;
                if (capasNiebla[i].anchoredPosition.x < -anchoRef * 1.2f)
                {
                    capasNiebla[i].anchoredPosition = new Vector2(anchoRef * 1.2f, capasNiebla[i].anchoredPosition.y);
                }
            }
        }

        // 4. Polvo de tumba flotante hacia el cielo nocturno
        if (activarPolvoTumba && motasPolvo != null)
        {
            float altoRef = Screen.height > 0 ? Screen.height : 1080f;
            float anchoRef = Screen.width > 0 ? Screen.width : 1920f;

            for (int i = 0; i < motasPolvo.Length; i++)
            {
                if (motasPolvo[i] == null) continue;
                Vector2 pos = motasPolvo[i].anchoredPosition;
                pos += velocidadesPolvo[i] * Time.deltaTime;
                pos.x += Mathf.Sin(t * 1.2f + i) * 12f * Time.deltaTime;

                if (pos.y > altoRef * 0.55f)
                {
                    pos.y = -altoRef * 0.55f;
                    pos.x = Random.Range(-anchoRef * 0.5f, anchoRef * 0.5f);
                }
                motasPolvo[i].anchoredPosition = pos;
            }
        }

        // 5. Destello tenue de tormenta / luna de medianoche
        if (activarResplandorLunar && resplandorTormentaImg != null)
        {
            proximoRelampagoTimer -= Time.deltaTime;
            if (proximoRelampagoTimer <= 0f)
            {
                relampagoDuracion = 0.45f;
                proximoRelampagoTimer = Random.Range(18f, 32f);
            }

            if (relampagoDuracion > 0f)
            {
                relampagoDuracion -= Time.deltaTime;
                float progreso = 1f - (relampagoDuracion / 0.45f);
                float flash = Mathf.Sin(progreso * Mathf.PI);
                resplandorTormentaImg.color = new Color(0.7f, 0.78f, 1f, flash * 0.16f);
            }
            else
            {
                resplandorTormentaImg.color = Color.clear;
            }
        }
    }

    private void InicializarEfectos()
    {
        // 1. Faroles coloniales parpadeantes ubicados estratégicamente sobre los faroles del museo
        if (activarFarolesVivos)
        {
            // Farol Izquierdo de la puerta principal
            CrearPuntoLuzFarol("Farol_Puerta_Izq", new Vector2(-0.37f, -0.08f), new Vector2(170f, 170f), 12.3f);
            // Farol Derecho de la puerta principal
            CrearPuntoLuzFarol("Farol_Puerta_Der", new Vector2(-0.20f, -0.12f), new Vector2(170f, 170f), 45.6f);
            // Farol Poste Colonial en la calle derecha
            CrearPuntoLuzFarol("Farol_Poste_Der", new Vector2(0.468f, 0.05f), new Vector2(210f, 210f), 78.9f);
        }

        // 2. Capas de niebla horizontal flotando en la calle y base del museo
        if (activarNieblaMovimiento)
        {
            capasNiebla = new RectTransform[2];
            velocidadesNiebla = new float[] { -28f, -16f };

            capasNiebla[0] = CrearCapaNiebla("Niebla_Calle_Baja", -0.38f, 220f, new Color(0.25f, 0.32f, 0.45f, 0.18f));
            capasNiebla[1] = CrearCapaNiebla("Niebla_Calle_Media", -0.22f, 280f, new Color(0.20f, 0.26f, 0.40f, 0.12f));
        }

        // 3. Polvo espectral flotando
        if (activarPolvoTumba)
        {
            int cantidadMotas = 10;
            motasPolvo = new RectTransform[cantidadMotas];
            velocidadesPolvo = new Vector2[cantidadMotas];

            for (int i = 0; i < cantidadMotas; i++)
            {
                GameObject mota = new GameObject($"MotaPolvo_{i}");
                mota.transform.SetParent(transform, false);

                RectTransform rt = mota.AddComponent<RectTransform>();
                float tamaño = Random.Range(3.5f, 6.5f);
                rt.sizeDelta = new Vector2(tamaño, tamaño);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(Random.Range(-800f, 800f), Random.Range(-500f, 500f));

                Image img = mota.AddComponent<Image>();
                img.sprite = glowSpriteCompartido;
                img.color = new Color(1f, 0.85f, 0.6f, Random.Range(0.25f, 0.60f));
                img.raycastTarget = false;

                motasPolvo[i] = rt;
                velocidadesPolvo[i] = new Vector2(Random.Range(-5f, 5f), Random.Range(22f, 48f));
            }
        }

        // 4. Capa de resplandor tormentoso/lunar
        if (activarResplandorLunar)
        {
            GameObject resplandor = new GameObject("ResplandorTormentaLunar");
            resplandor.transform.SetParent(transform, false);

            RectTransform rt = resplandor.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            resplandorTormentaImg = resplandor.AddComponent<Image>();
            resplandorTormentaImg.color = Color.clear;
            resplandorTormentaImg.raycastTarget = false;
        }
    }

    private void CrearPuntoLuzFarol(string nombre, Vector2 factorPosicion, Vector2 tamano, float semilla)
    {
        GameObject farolObj = new GameObject(nombre);
        farolObj.transform.SetParent(transform, false);

        RectTransform rt = farolObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Posición normalizada respecto al tamaño del fondo
        float anchoRef = 1920f;
        float altoRef = 1080f;
        rt.anchoredPosition = new Vector2(factorPosicion.x * anchoRef, factorPosicion.y * altoRef);
        rt.sizeDelta = tamano;

        Image img = farolObj.AddComponent<Image>();
        img.sprite = glowSpriteCompartido;
        img.color = new Color(1f, 0.74f, 0.32f, 0.45f);
        img.raycastTarget = false;

        farolesGlow.Add(img);
        farolesSemillas.Add(semilla);
    }

    private RectTransform CrearCapaNiebla(string nombre, float factorY, float alto, Color colorNiebla)
    {
        GameObject nieblaObj = new GameObject(nombre);
        nieblaObj.transform.SetParent(transform, false);

        RectTransform rt = nieblaObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float altoRef = 1080f;
        rt.anchoredPosition = new Vector2(0f, factorY * altoRef);
        rt.sizeDelta = new Vector2(3000f, alto);

        Image img = nieblaObj.AddComponent<Image>();
        img.sprite = glowSpriteCompartido;
        img.color = colorNiebla;
        img.raycastTarget = false;

        return rt;
    }

    private static void CrearSpriteGlow()
    {
        if (glowSpriteCompartido != null) return;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDist = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(dist / maxDist);
                // Gradiente suave exponencial (soft radial glow)
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - t), 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        glowSpriteCompartido = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
