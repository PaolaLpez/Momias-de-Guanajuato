using System.Collections;
using UnityEngine;

/// <summary>
/// Controla la presencia física de las momias gemelas acostadas en las camillas quirúrgicas
/// y su secuencia de despertar progresivo (una por una) en el Nivel 3.
/// </summary>
public class ControladorDespertarMomiasNivel3 : MonoBehaviour
{
    public static ControladorDespertarMomiasNivel3 Instance { get; private set; }

    [Header("Objetos de Camillas")]
    public GameObject camilla1Obj;
    public GameObject camilla2Obj;

    [Header("Sprites de Estado")]
    public Sprite spriteMomia1Acostada;
    public Sprite spriteMomia2Acostada;
    public Sprite spriteMomia2CabezaGirada;
    public Sprite spriteCamillaVacia;

    [Header("Enemigos Móviles")]
    public MomiaAcechadoraNivel3 gemelaAcechadora;
    public MomiaCiegaNivel3 gemelaCiega;

    [Header("Estado de Despertar")]
    public bool gemela1Despierta = false;
    public bool gemela2Despierta = false;

    private SpriteRenderer srCamilla1;
    private SpriteRenderer srCamilla2;

    private float timerInicio = 0f;
    private bool secuenciaIniciada = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InicializarCamillas();
    }

    public void InicializarCamillas()
    {
        if (camilla1Obj != null) srCamilla1 = camilla1Obj.GetComponent<SpriteRenderer>();
        if (camilla2Obj != null) srCamilla2 = camilla2Obj.GetComponent<SpriteRenderer>();

        if (gemelaAcechadora == null) gemelaAcechadora = FindFirstObjectByType<MomiaAcechadoraNivel3>(FindObjectsInactive.Include);
        if (gemelaCiega == null) gemelaCiega = FindFirstObjectByType<MomiaCiegaNivel3>(FindObjectsInactive.Include);

        // Las momias comienzan ACOSTADAS en sus camillas quirúrgicas.
        // Despiertan estrictamente por reloj: Gemela 1 a las 02:05 AM y Gemela 2 + Escorpiones a las 02:10 AM.
        if (gemelaAcechadora != null)
        {
            gemelaAcechadora.gameObject.SetActive(false);
            gemelaAcechadora.activa = false;
            gemelaAcechadora.transform.position = new Vector3(-2.8f, -0.5f, 0f);
            SpriteRenderer sr1 = gemelaAcechadora.GetComponent<SpriteRenderer>();
            if (sr1 != null)
            {
                sr1.enabled = true;
                sr1.sortingOrder = 350;
                sr1.sprite = HojasSpritesNivel3.MomiaFrame(0, 0);
            }
        }
        if (gemelaCiega != null)
        {
            gemelaCiega.gameObject.SetActive(false);
            gemelaCiega.activa = false;
            gemelaCiega.transform.position = new Vector3(2.8f, -0.5f, 0f);
            SpriteRenderer sr2 = gemelaCiega.GetComponent<SpriteRenderer>();
            if (sr2 != null)
            {
                sr2.enabled = true;
                sr2.sortingOrder = 350;
                sr2.sprite = HojasSpritesNivel3.MomiaFrame(0, 0);
            }
        }

        gemela1Despierta = false;
        gemela2Despierta = false;

        // Los escorpiones emergen ÚNICAMENTE a las 02:10 AM junto a la segunda momia
        EscorpionNivel3[] escorpiones = FindObjectsByType<EscorpionNivel3>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < escorpiones.Length; i++)
        {
            if (escorpiones[i] != null)
            {
                escorpiones[i].gameObject.SetActive(false);
            }
        }

        // Camillas quirúrgicas con las dos momias acostadas físicamente antes de despertar
        if (srCamilla1 != null)
        {
            srCamilla1.sprite = (spriteMomia1Acostada != null) ? spriteMomia1Acostada : HojasSpritesNivel3.MomiaFrame(0, 0);
        }
        if (srCamilla2 != null)
        {
            srCamilla2.sprite = (spriteMomia2Acostada != null) ? spriteMomia2Acostada : HojasSpritesNivel3.MomiaFrame(0, 0);
        }
    }

    private void Update()
    {
        if (Nivel3Manager.Instance != null && Nivel3Manager.Instance.JuegoPausadoOTerminado) return;

        timerInicio += Time.deltaTime;

        // Minuto sincronizado exactamente con el reloj del HUD (02:00 AM -> 02:60 AM)
        int minutoActual = Nivel3Manager.Instance != null 
            ? Nivel3Manager.Instance.ObtenerMinutoActual() 
            : Mathf.FloorToInt((timerInicio / 210f) * 60f);

        // Evento 02:05 AM — Despierta la PRIMERA momia (Gemela Acechadora)
        if (!gemela1Despierta && minutoActual >= 5)
        {
            DespertarGemela1();
        }

        // Evento 02:10 AM — Despierta la SEGUNDA momia (Gemela Ciega) y emergen los escorpiones
        if (gemela1Despierta && !gemela2Despierta && minutoActual >= 10)
        {
            DespertarGemela2();
        }
    }

    public void DespertarGemela1()
    {
        if (gemela1Despierta) return;
        gemela1Despierta = true;

        // Camilla 1 queda vacía con sábanas y vendas
        if (srCamilla1 != null)
        {
            srCamilla1.sprite = (spriteCamillaVacia != null) ? spriteCamillaVacia : srCamilla1.sprite;
        }

        if (gemelaAcechadora == null) gemelaAcechadora = FindFirstObjectByType<MomiaAcechadoraNivel3>(FindObjectsInactive.Include);
        if (gemelaAcechadora != null)
        {
            gemelaAcechadora.gameObject.SetActive(true);
            gemelaAcechadora.activa = true;
            gemelaAcechadora.transform.position = new Vector3(-2.8f, -0.5f, 0f);
            SpriteRenderer sr = gemelaAcechadora.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                sr.sortingOrder = 350;
                sr.sprite = HojasSpritesNivel3.MomiaFrame(0, 0);
            }

            gemelaAcechadora.puntosPatrulla = new Vector2[]
            {
                new Vector2(0f, 4.5f),     // Frente a Caseta
                new Vector2(2.5f, 3.8f),   // Entrada este
                new Vector2(6.5f, 1.0f),   // Pasillo Este hacia Morgue
                new Vector2(13.0f, -2.5f), // Morgue Fría
                new Vector2(0f, -4.5f),    // Quirófano sur
                new Vector2(-12.0f, -2.5f),// Lab Químico
                new Vector2(-2.8f, 1.0f),  // Quirófano salida norte
                new Vector2(0f, 4.5f)      // Regreso frente a Caseta
            };
        }

        if (Nivel3Manager.Instance != null)
        {
            Nivel3Manager.Instance.MostrarAvisoRuido("⏰ 02:05 AM — ¡La Gemela Acechadora despertó y acecha los pasillos!");
        }
    }

    public void DespertarGemela2()
    {
        if (gemela2Despierta) return;
        gemela2Despierta = true;

        // Camilla 2 queda vacía
        if (srCamilla2 != null)
        {
            srCamilla2.sprite = (spriteCamillaVacia != null) ? spriteCamillaVacia : srCamilla2.sprite;
        }

        if (gemelaCiega == null) gemelaCiega = FindFirstObjectByType<MomiaCiegaNivel3>(FindObjectsInactive.Include);
        if (gemelaCiega != null)
        {
            gemelaCiega.gameObject.SetActive(true);
            gemelaCiega.activa = true;
            gemelaCiega.transform.position = new Vector3(2.8f, -0.5f, 0f);
            SpriteRenderer sr = gemelaCiega.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                sr.sortingOrder = 350;
                sr.sprite = HojasSpritesNivel3.MomiaFrame(0, 0);
            }

            gemelaCiega.puntosPatrulla = new Vector2[]
            {
                new Vector2(-2.2f, 3.8f),   // Umbral visible de la Caseta
                new Vector2(0f, 1.0f),      // Pasillo central
                new Vector2(-6.0f, 0f),     // Pasaje a Laboratorio
                new Vector2(-13.5f, -2f),   // Mesón de reactivos
                new Vector2(0f, -4f),       // Quirófano sur
                new Vector2(13.5f, -2f),    // Morgue fría
                new Vector2(0f, 2.5f),      // Pasillo central
                new Vector2(-2.2f, 3.8f)    // Regreso a Caseta
            };

            gemelaCiega.AlertarRuido(gemelaCiega.transform.position, "¡Alarido en el taller! La Gemela Ciega inicia su cacería");
        }

        // ¡A las 02:10 AM emergen todos los escorpiones por el museo!
        EscorpionNivel3[] escorpiones = FindObjectsByType<EscorpionNivel3>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < escorpiones.Length; i++)
        {
            if (escorpiones[i] != null)
            {
                escorpiones[i].gameObject.SetActive(true);
                SpriteRenderer eSr = escorpiones[i].GetComponent<SpriteRenderer>();
                if (eSr != null) eSr.enabled = true;
            }
        }

        if (Nivel3Manager.Instance != null)
        {
            Nivel3Manager.Instance.MostrarAvisoRuido("⏰ 02:10 AM — ¡Ambas momias gemelas y escorpiones andan sueltos!");
            Nivel3Manager.Instance.MostrarLetreroTenebrosoDespertar();
        }
    }
}
