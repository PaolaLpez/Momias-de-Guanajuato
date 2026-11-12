using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Momia del Nivel 2 ("La Que Escucha" - 100% Ciega, orientada únicamente por oído).
/// Confinada exclusivamente a su sección designada del Nivel 4 (Sector Este - Gran Osario).
/// - Permanece dormida/oculta hasta las 04:00 AM, igual que la momia del Nivel 1 en su sector.
/// - Reacciona al sonido de hojas secas, pasos sin sigilo o carrera del jugador en su zona.
/// - Es completamente inmune visualmente al modo sigilo agachado (salvo contacto físico directo <= 0.65m).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MomiaNivel2Seccion : MonoBehaviour
{
    public enum EstadoMomia2
    {
        Dormida,
        Patrulla,
        Escuchando,
        Caza,
        Investigando,
        AturdidaCirio,
        Petrificada
    }

    [Header("Identidad y Sección")]
    public string nombreSeccion = "Sector Este (Gran Osario)";
    public int horaAparicion = 4;
    public int minutoAparicion = 0;

    [Header("Límites de Sección")]
    public Vector2 zonaMin = new Vector2(10f, -14f);
    public Vector2 zonaMax = new Vector2(36f, 15f);

    [Header("Velocidades (Fiel a Nivel 2)")]
    public float velocidadPatrulla = 2.0f;
    public float velocidadInvestigacion = 3.2f;
    public float velocidadCaza = 4.5f;

    [Header("Audición (100% Ciega)")]
    public float rangoAudicionCorrer = 13.5f;
    public float rangoAudicionCaminarPie = 4.8f;
    public float rangoContactoSigilo = 0.65f;
    public float rangoAtaque = 1.35f;
    public float danioZarpazo = 34f;
    public float cooldownAtaque = 1.45f;

    public EstadoMomia2 EstadoActual { get; private set; } = EstadoMomia2.Dormida;
    public bool HaDespertado { get; private set; } = false;
    public bool Petrificada => EstadoActual == EstadoMomia2.Petrificada;

    private SpriteRenderer sr;
    private BoxCollider2D boxCol;
    private AudioSource audioSource;
    private AudioSource fuenteAudio2D;
    private AudioSource audioCaminataMomia;
    private Light2D auraAlerta;

    private Sprite[] spritesWalk;
    private Sprite[] spritesBack;
    private Sprite[] spritesScream;
    private Sprite spriteStalk;

    private AudioClip clipGrito;
    private AudioClip clipZarpazo;
    private AudioClip clipPasos;
    private AudioClip clipCercaniaMomia;
    private AudioClip clipAtaqueN2;
    private AudioClip clipRugidoDeteccion;
    private AudioClip clipCaminataMomia;
    private float timerSonidoMomiaDeVezEnCuando = 3.2f;

    private VigilanteNivel4 jugador;
    private Vector3 posicionObjetivo;
    private Vector3 ultimaPosJugador;
    private List<Vector2> puntosPatrulla = new List<Vector2>();
    private int idxPatrulla = 0;

    private float animTimer = 0f;
    private int animFrame = 0;
    private float timerEstado = 0f;
    private float timerAtaque = 0f;
    private float timerPasos = 0f;
    private float timerEsperaWaypoint = 0f;
    private float timerAtascado = 0f;
    private float timerAlejamientoEscondite = 0f;
    private float timerPermanenciaHabitacion = 0f;
    private Vector2 posEvitarEscondite = Vector2.zero;
    private Vector3 ultimaPosFrame;
    private Vector3 dirRepulsionCirio = Vector3.right;
    private bool apareciendo = false;
    private int mascaraObstaculos;
    private Vector2 dirDesvioActual = Vector2.right;
    private float timerDesvio = 0f;
    private float timerCheckAtasco = 0f;
    private int signoDesvioPreferido = 1;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        boxCol = GetComponent<BoxCollider2D>();
        if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider2D>();
        boxCol.size = new Vector2(0.75f, 1.35f);
        boxCol.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        GameObject audio2DObj = new GameObject("MomiaN2_AudioCercania2D");
        audio2DObj.transform.SetParent(transform, false);
        fuenteAudio2D = audio2DObj.AddComponent<AudioSource>();
        fuenteAudio2D.spatialBlend = 0f;
        fuenteAudio2D.playOnAwake = false;

        GameObject camObj = new GameObject("MomiaN2_Caminata2D");
        camObj.transform.SetParent(transform, false);
        audioCaminataMomia = camObj.AddComponent<AudioSource>();
        audioCaminataMomia.spatialBlend = 0f;
        audioCaminataMomia.loop = true;
        audioCaminataMomia.playOnAwake = false;
        audioCaminataMomia.volume = 0f;

        mascaraObstaculos = ~(LayerMask.GetMask("Ignore Raycast"));
        CargarSpritesNivel2();
        SintetizarAudios();
        CrearAuraAlerta();
        ConstruirPatrullaEste();
    }

    void Start()
    {
        jugador = FindFirstObjectByType<VigilanteNivel4>();
        if (jugador != null) ultimaPosJugador = jugador.transform.position;
        ultimaPosFrame = transform.position;

        // Dormida hasta la hora asignada (04:00 AM por defecto)
        EstadoActual = EstadoMomia2.Dormida;
        HaDespertado = false;
        sr.enabled = false;
        if (boxCol != null) boxCol.enabled = false;
        if (auraAlerta != null) auraAlerta.enabled = false;
    }

    private void CargarSpritesNivel2()
    {
        spritesWalk = new Sprite[6];
        for (int i = 0; i < 6; i++)
            spritesWalk[i] = Resources.Load<Sprite>($"Momia2/momia2_walk_{i}");

        spritesBack = new Sprite[4];
        for (int i = 0; i < 4; i++)
            spritesBack[i] = Resources.Load<Sprite>($"Momia2/momia2_back_{i}");

        spritesScream = new Sprite[4];
        for (int i = 0; i < 4; i++)
            spritesScream[i] = Resources.Load<Sprite>($"Momia2/momia2_scream_{i}");

        spriteStalk = Resources.Load<Sprite>("Momia2/momia2_pose1_stalk");
        if (spriteStalk == null && spritesWalk.Length > 0) spriteStalk = spritesWalk[0];
        if (spriteStalk != null) sr.sprite = spriteStalk;
    }

    private void CrearAuraAlerta()
    {
        GameObject auraGo = new GameObject("AuraLaQueEscucha");
        auraGo.transform.SetParent(transform);
        auraGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        auraAlerta = auraGo.AddComponent<Light2D>();
        auraAlerta.lightType = Light2D.LightType.Point;
        auraAlerta.color = new Color(0.85f, 0.15f, 0.12f, 1f);
        auraAlerta.intensity = 0.75f;
        auraAlerta.pointLightOuterRadius = 3.2f;
        auraAlerta.pointLightInnerRadius = 0.5f;
        auraAlerta.enabled = false;
    }

    private void ConstruirPatrullaEste()
    {
        puntosPatrulla.Clear();
        // Puntos despejados dentro de Sector Este (Gran Osario y tumbas orientales)
        puntosPatrulla.Add(new Vector2(18f, 0f));
        puntosPatrulla.Add(new Vector2(13f, 5f));
        puntosPatrulla.Add(new Vector2(18f, 13f));
        puntosPatrulla.Add(new Vector2(12f, 10f));
        puntosPatrulla.Add(new Vector2(13f, -6f));
        puntosPatrulla.Add(new Vector2(22f, -11f));
        puntosPatrulla.Add(new Vector2(28f, -1f));
        posicionObjetivo = ClampZona(puntosPatrulla[0]);
    }

    void Update()
    {
        if (Nivel4Manager.Instance == null || Nivel4Manager.Instance.JuegoTerminado) return;
        if (EstadoActual == EstadoMomia2.Petrificada) return;

        // Esperar hasta la hora programada (04:00 AM)
        if (!HaDespertado)
        {
            float minutosMeta = horaAparicion * 60 + minutoAparicion;
            if (Nivel4Manager.Instance.TotalMinutosActual >= minutosMeta)
            {
                DespertarEnSeccion();
            }
            return;
        }

        if (apareciendo) return;
        if (jugador == null) jugador = FindFirstObjectByType<VigilanteNivel4>();
        if (timerAtaque > 0f) timerAtaque -= Time.deltaTime;
        if (timerAlejamientoEscondite > 0f) timerAlejamientoEscondite -= Time.deltaTime;

        ActualizarAudioCercaniaMomiaN2();
        ActualizarAudioCaminataMomiaN2();
        EvaluarAudicionJugador();
        ActualizarEstado();
        ActualizarAnimacion();

        if (jugador != null) ultimaPosJugador = jugador.transform.position;
    }

    private void DespertarEnSeccion()
    {
        HaDespertado = true;
        sr.enabled = true;
        if (boxCol != null) boxCol.enabled = true;
        if (auraAlerta != null) auraAlerta.enabled = true;
        EstadoActual = EstadoMomia2.Patrulla;
        posicionObjetivo = ClampZona(puntosPatrulla[0]);

        StartCoroutine(SecuenciaAparicionNatural());
    }

    public void NotificarJugadorOculto(Vector2 posEscondite)
    {
        if (!HaDespertado || EstadoActual == EstadoMomia2.Petrificada || EstadoActual == EstadoMomia2.AturdidaCirio)
            return;

        if (Vector2.Distance(transform.position, posEscondite) <= 16f ||
            EstadoActual == EstadoMomia2.Caza ||
            EstadoActual == EstadoMomia2.Escuchando ||
            EstadoActual == EstadoMomia2.Investigando)
        {
            AlejarDePuntoEnSeccion(posEscondite, 10.5f);
        }
    }

    private void AlejarDePuntoEnSeccion(Vector2 puntoEvitar, float duracion)
    {
        posEvitarEscondite = puntoEvitar;
        timerAlejamientoEscondite = duracion;
        EstadoActual = EstadoMomia2.Patrulla;

        if (puntosPatrulla.Count > 0)
        {
            int mejorFueraHab = -1;
            float maxDFuera = -1f;
            int mejorGeneral = idxPatrulla;
            float maxD = -1f;
            for (int i = 0; i < puntosPatrulla.Count; i++)
            {
                Vector2 p = puntosPatrulla[i];
                float d = Vector2.Distance(p, puntoEvitar);
                if (d > maxD)
                {
                    maxD = d;
                    mejorGeneral = i;
                }
                bool fueraOsario = !(p.x >= 20.5f && p.x <= 35.5f && p.y >= 0.2f && p.y <= 12f);
                if (fueraOsario && d > maxDFuera)
                {
                    maxDFuera = d;
                    mejorFueraHab = i;
                }
            }
            idxPatrulla = (mejorFueraHab >= 0) ? mejorFueraHab : mejorGeneral;
            posicionObjetivo = ClampZona(puntosPatrulla[idxPatrulla]);
        }
    }

    private IEnumerator SecuenciaAparicionNatural()
    {
        apareciendo = true;
        Color c = Color.white;
        c.a = 0f;
        sr.color = c;
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / 1.2f);
            sr.color = c;
            yield return null;
        }
        sr.color = Color.white;
        apareciendo = false;
    }

    private void EvaluarAudicionJugador()
    {
        if (jugador == null || jugador.EstaMuerto || jugador.EstaOculta) return;
        if (EstadoActual == EstadoMomia2.AturdidaCirio || EstadoActual == EstadoMomia2.Petrificada) return;

        Vector3 posJ = jugador.transform.position;
        float dist = Vector2.Distance(transform.position, posJ);

        // Solo escucha activamente si el jugador está dentro o pegado al borde de su sección
        bool jugadorEnSeccion =
            posJ.x >= zonaMin.x - 3f && posJ.x <= zonaMax.x + 2f &&
            posJ.y >= zonaMin.y - 3f && posJ.y <= zonaMax.y + 3f;

        if (!jugadorEnSeccion && EstadoActual != EstadoMomia2.Caza) return;

        float velAprox = (posJ - ultimaPosJugador).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
        bool seMueve = velAprox > 0.25f;

        // 1. Contacto físico directo incluso en sigilo (tropezón)
        if (dist <= rangoContactoSigilo)
        {
            AlertarHacia(posJ, true);
            return;
        }

        // 2. Sigilo agachado: invisible al oído salvo trampa acústica
        if (VigilanteNivel4.ModoSigiloActivo) return;

        // 3. Correr genera ruido a larga distancia en el sector
        if (jugador.EstaCorriendo && seMueve && dist <= rangoAudicionCorrer)
        {
            AlertarHacia(posJ, true);
            return;
        }

        // 4. Caminar erguido cerca sin obstáculos gruesos de por medio
        if (seMueve && dist <= rangoAudicionCaminarPie && !HayParedEntre(transform.position, posJ))
        {
            AlertarHacia(posJ, dist <= 3.6f);
        }
    }

    /// <summary>
    /// Recibe notificación acústica de hojas secas detonadas cerca de la sección Este.
    /// </summary>
    public void NotificarRuido(Vector2 origen, float radioAlerta, float intensidad)
    {
        if (!HaDespertado || EstadoActual == EstadoMomia2.Petrificada || EstadoActual == EstadoMomia2.AturdidaCirio)
            return;

        bool ruidoEnSeccion =
            origen.x >= zonaMin.x - 4f && origen.x <= zonaMax.x + 2f &&
            origen.y >= zonaMin.y - 4f && origen.y <= zonaMax.y + 4f;

        float dist = Vector2.Distance(transform.position, origen);
        if (!ruidoEnSeccion && dist > radioAlerta) return;

        AlertarHacia(origen, dist <= 10.5f || intensidad >= 1f);
    }

    private void AlertarHacia(Vector3 origenRuido, bool cazaInmediata)
    {
        posicionObjetivo = ClampZona(origenRuido);
        if (cazaInmediata)
        {
            if (EstadoActual == EstadoMomia2.Caza)
            {
                timerEstado = 3.2f;
            }
            else if (EstadoActual != EstadoMomia2.Escuchando)
            {
                EstadoActual = EstadoMomia2.Escuchando;
                timerEstado = 0.42f;
                ReproducirClipConDistancia(clipRugidoDeteccion ?? clipGrito, 0.88f, Random.Range(0.92f, 1.06f));
            }
            // Si ya está en Escuchando, no reiniciar timerEstado para que avance sin trabarse hacia Caza
        }
        else if (EstadoActual == EstadoMomia2.Patrulla)
        {
            EstadoActual = EstadoMomia2.Investigando;
            timerEstado = 3.4f;
            ReproducirClipConDistancia(clipRugidoDeteccion ?? clipGrito, 0.65f, Random.Range(0.90f, 1.02f));
        }
    }

    private void ActualizarEstado()
    {
        // Si está rondando dentro del Gran Osario o bloqueando su puerta sin cazar, salir a otro extremo del Sector Este
        bool enOsarioOPuerta = (transform.position.x >= 20.5f && transform.position.x <= 35.5f && transform.position.y >= 0.2f && transform.position.y <= 12f) ||
                               Vector2.Distance(transform.position, new Vector2(28f, 1.5f)) <= 4.2f;
        bool ocultaAhora = jugador != null && jugador.EstaOculta;
        if (enOsarioOPuerta && (EstadoActual == EstadoMomia2.Patrulla || ocultaAhora))
        {
            timerPermanenciaHabitacion += Time.deltaTime;
            if (timerPermanenciaHabitacion >= 2.3f)
            {
                timerPermanenciaHabitacion = 0f;
                AlejarDePuntoEnSeccion(new Vector2(28f, 6f), 8.5f);
            }
        }
        else
        {
            timerPermanenciaHabitacion = 0f;
        }

        switch (EstadoActual)
        {
            case EstadoMomia2.Patrulla:
                float velP = (timerAlejamientoEscondite > 0f && Vector2.Distance(transform.position, posEvitarEscondite) < 10f)
                    ? velocidadInvestigacion
                    : velocidadPatrulla;
                MoverHacia(posicionObjetivo, velP);
                if (Vector2.Distance(transform.position, posicionObjetivo) < 0.7f)
                {
                    timerEsperaWaypoint += Time.deltaTime;
                    if (timerEsperaWaypoint >= 0.85f)
                    {
                        timerEsperaWaypoint = 0f;
                        idxPatrulla = (idxPatrulla + 1) % puntosPatrulla.Count;
                        // Mientras evita un escondite/habitación, saltar puntos cercanos a él
                        if (timerAlejamientoEscondite > 0f && Vector2.Distance(puntosPatrulla[idxPatrulla], posEvitarEscondite) < 9.5f)
                        {
                            AlejarDePuntoEnSeccion(posEvitarEscondite, timerAlejamientoEscondite);
                        }
                        else
                        {
                            posicionObjetivo = ClampZona(puntosPatrulla[idxPatrulla]);
                        }
                    }
                }
                if (auraAlerta != null) auraAlerta.intensity = 0.55f;
                break;

            case EstadoMomia2.Escuchando:
                timerEstado -= Time.deltaTime;
                if (jugador != null) VoltearHacia(posicionObjetivo);
                if (auraAlerta != null) auraAlerta.intensity = 1.45f;
                if (timerEstado <= 0f)
                {
                    EstadoActual = EstadoMomia2.Caza;
                    timerEstado = 4.0f;
                }
                break;

            case EstadoMomia2.Investigando:
                timerEstado -= Time.deltaTime;
                MoverHacia(posicionObjetivo, velocidadInvestigacion);
                if (auraAlerta != null) auraAlerta.intensity = 1.05f;
                if (Vector2.Distance(transform.position, posicionObjetivo) < 0.65f || timerEstado <= 0f)
                {
                    EstadoActual = EstadoMomia2.Patrulla;
                    posicionObjetivo = ClampZona(puntosPatrulla[idxPatrulla]);
                }
                break;

            case EstadoMomia2.Caza:
                timerEstado -= Time.deltaTime;
                if (auraAlerta != null) auraAlerta.intensity = 1.65f;

                if (jugador != null && !jugador.EstaOculta && !VigilanteNivel4.ModoSigiloActivo &&
                    jugador.transform.position.x >= zonaMin.x - 2f && jugador.transform.position.x <= zonaMax.x + 2f)
                {
                    posicionObjetivo = ClampZona(jugador.transform.position);
                    if (jugador.EstaCorriendo) timerEstado = 3.2f;
                }

                float distJ = (jugador != null) ? Vector2.Distance(transform.position, jugador.transform.position) : 999f;
                if (jugador != null && !jugador.EstaOculta && !jugador.EstaMuerto && distJ <= rangoAtaque && timerAtaque <= 0f)
                {
                    EjecutarZarpazo();
                }
                else
                {
                    MoverHacia(posicionObjetivo, velocidadCaza);
                }

                if (Vector2.Distance(transform.position, posicionObjetivo) < 0.65f || timerEstado <= 0f)
                {
                    EstadoActual = EstadoMomia2.Investigando;
                    timerEstado = 2.0f;
                }
                break;

            case EstadoMomia2.AturdidaCirio:
                timerEstado -= Time.deltaTime;
                MoverHacia(transform.position + dirRepulsionCirio * 3f, velocidadInvestigacion * 1.15f);
                if (auraAlerta != null) auraAlerta.intensity = 0.4f;
                if (timerEstado <= 0f)
                {
                    EstadoActual = EstadoMomia2.Patrulla;
                    posicionObjetivo = ClampZona(puntosPatrulla[idxPatrulla]);
                }
                break;
        }

        // Mantener confinada dentro del Sector Este
        Vector3 clamped = ClampZona(transform.position);
        if ((transform.position - clamped).sqrMagnitude > 0.001f)
            transform.position = clamped;
    }

    private void EjecutarZarpazo()
    {
        timerAtaque = cooldownAtaque;
        if (clipAtaqueN2 == null) clipAtaqueN2 = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_ATAQUE_MOMIA_N2);
        if (clipAtaqueN2 != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.06f);
            audioSource.PlayOneShot(clipAtaqueN2, 0.92f);
        }
        ReproducirClip(clipZarpazo, 0.82f, Random.Range(0.95f, 1.08f));
        if (jugador != null && !jugador.EstaOculta && !jugador.EstaMuerto)
        {
            jugador.RecibirDanio(danioZarpazo, "Momia2");
            StartCoroutine(DestelloRojoAtaque());
            if (!jugador.EstaMuerto && Nivel4Manager.Instance != null)
            {
                Nivel4Manager.Instance.MostrarAviso($"¡LA QUE ESCUCHA TE HA HERIDO! (❤ {Mathf.CeilToInt(jugador.vidaActual)})", 2.0f);
                Nivel4Manager.Instance.SacudirPantalla(0.35f, 12f);
            }
        }
    }

    private IEnumerator DestelloRojoAtaque()
    {
        sr.color = new Color(1f, 0.35f, 0.35f, 1f);
        yield return new WaitForSeconds(0.18f);
        if (EstadoActual != EstadoMomia2.Petrificada) sr.color = Color.white;
    }

    private void MoverHacia(Vector3 destino, float velocidad)
    {
        Vector2 pos = transform.position;
        Vector2 delta = (Vector2)destino - pos;
        if (delta.magnitude < 0.05f) return;

        if (timerDesvio > 0f) timerDesvio -= Time.deltaTime;

        Vector2 dirDeseada = delta.normalized;
        float paso = velocidad * Time.deltaTime;
        Vector2 origenCast = (boxCol != null && boxCol.enabled) ? (Vector2)boxCol.bounds.center : pos;

        Vector2 dirFinal = dirDeseada;
        float distLibre = ProbarDistanciaLibre(origenCast, dirDeseada, Mathf.Max(0.85f, paso + 0.5f), out Vector2 normalChoque);
        if (distLibre < 0.80f)
        {
            if (timerDesvio <= 0f)
            {
                dirDesvioActual = ElegirDesvio(origenCast, dirDeseada, normalChoque);
                timerDesvio = 0.55f;
            }
            dirFinal = Vector2.Lerp(dirDeseada, dirDesvioActual, 0.88f).normalized;
        }
        else if (timerDesvio > 0f)
        {
            dirFinal = Vector2.Lerp(dirDesvioActual, dirDeseada, 0.45f).normalized;
        }

        // Verificar que el paso inmediato no choque; si está tapado, usar mejor desvío libre o deslizar
        if (EstaBloqueado(origenCast, dirFinal, Mathf.Max(0.32f, paso + 0.22f)))
        {
            dirDesvioActual = ElegirDesvio(origenCast, dirDeseada, normalChoque);
            timerDesvio = 0.60f;
            dirFinal = dirDesvioActual;

            if (EstaBloqueado(origenCast, dirFinal, Mathf.Max(0.28f, paso + 0.16f)))
            {
                Vector2 soloX = new Vector2(Mathf.Sign(dirFinal.x), 0f);
                Vector2 soloY = new Vector2(0f, Mathf.Sign(dirFinal.y));
                if (Mathf.Abs(dirFinal.x) > 0.1f && !EstaBloqueado(origenCast, soloX, Mathf.Max(0.26f, paso + 0.14f)))
                    dirFinal = soloX;
                else if (Mathf.Abs(dirFinal.y) > 0.1f && !EstaBloqueado(origenCast, soloY, Mathf.Max(0.26f, paso + 0.14f)))
                    dirFinal = soloY;
                else
                    dirFinal = Vector2.zero;
            }
        }

        if (dirFinal.sqrMagnitude > 0.001f)
        {
            Vector3 nuevaPos = ClampZona((Vector3)(pos + dirFinal * paso));
            transform.position = nuevaPos;
            VoltearHacia(transform.position + (Vector3)dirFinal);
        }

        // Anti-atasco real (funciona en Patrulla, Investigación y Caza sin perder el rumbo)
        timerCheckAtasco += Time.deltaTime;
        if (timerCheckAtasco >= 0.25f)
        {
            if ((transform.position - ultimaPosFrame).magnitude < 0.09f)
            {
                timerAtascado += timerCheckAtasco;
                if (timerAtascado >= 0.50f)
                {
                    signoDesvioPreferido = -signoDesvioPreferido;
                    dirDesvioActual = ElegirDesvio(origenCast, dirDeseada, Vector2.zero);
                    timerDesvio = 1.05f;
                    timerAtascado = 0f;

                    if (EstadoActual == EstadoMomia2.Patrulla || EstadoActual == EstadoMomia2.Investigando)
                    {
                        idxPatrulla = (idxPatrulla + 1) % puntosPatrulla.Count;
                        posicionObjetivo = ClampZona(puntosPatrulla[idxPatrulla]);
                        if (EstadoActual == EstadoMomia2.Investigando)
                        {
                            EstadoActual = EstadoMomia2.Patrulla;
                        }
                    }
                }
            }
            else
            {
                timerAtascado = 0f;
            }
            ultimaPosFrame = transform.position;
            timerCheckAtasco = 0f;
        }
    }

    private float ProbarDistanciaLibre(Vector2 origen, Vector2 dirPrueba, float maxDist, out Vector2 normalChoque)
    {
        normalChoque = Vector2.zero;
        if (dirPrueba.sqrMagnitude < 0.001f) return 0f;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origen, 0.24f, dirPrueba.normalized, maxDist, mascaraObstaculos);
        float menorDist = maxDist;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (jugador != null && hit.collider.gameObject == jugador.gameObject) continue;
            if (hit.distance <= 0.02f && Vector2.Dot(dirPrueba, hit.normal) >= -0.05f) continue;
            if (hit.distance < menorDist)
            {
                menorDist = hit.distance;
                normalChoque = hit.normal;
            }
        }
        return menorDist;
    }

    private Vector2 ElegirDesvio(Vector2 origen, Vector2 dirDeseada, Vector2 normalChoque)
    {
        Vector2 perpA = normalChoque != Vector2.zero
            ? new Vector2(-normalChoque.y, normalChoque.x).normalized
            : new Vector2(-dirDeseada.y, dirDeseada.x).normalized;
        Vector2 perpB = -perpA;
        if (Vector2.Dot(perpB, dirDeseada) > Vector2.Dot(perpA, dirDeseada))
        {
            Vector2 tmp = perpA;
            perpA = perpB;
            perpB = tmp;
        }

        Vector2[] candidatas = new Vector2[]
        {
            perpA,
            perpB,
            RotarVector(dirDeseada, 42f * signoDesvioPreferido),
            RotarVector(dirDeseada, -42f * signoDesvioPreferido),
            RotarVector(dirDeseada, 75f * signoDesvioPreferido),
            RotarVector(dirDeseada, -75f * signoDesvioPreferido),
            RotarVector(dirDeseada, 110f * signoDesvioPreferido),
            RotarVector(dirDeseada, -110f * signoDesvioPreferido),
            RotarVector(dirDeseada, 145f * signoDesvioPreferido),
            RotarVector(dirDeseada, -145f * signoDesvioPreferido),
            -dirDeseada
        };

        Vector2 mejor = perpA;
        float mejorPuntaje = -999f;
        for (int i = 0; i < candidatas.Length; i++)
        {
            Vector2 cand = candidatas[i].normalized;
            float libre = ProbarDistanciaLibre(origen, cand, 1.7f, out _);
            float alineacion = Mathf.Max(0f, Vector2.Dot(cand, dirDeseada));
            float puntaje = libre * 1.45f + alineacion * 1.15f;
            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejor = cand;
            }
        }
        if (normalChoque != Vector2.zero)
        {
            mejor = (mejor + normalChoque * 0.20f).normalized;
        }
        return mejor;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!HaDespertado || apareciendo || EstadoActual == EstadoMomia2.Petrificada || EstadoActual == EstadoMomia2.AturdidaCirio)
            return;

        if (other.CompareTag("Player") || other.GetComponent<VigilanteNivel4>() != null)
        {
            if (jugador != null && !jugador.EstaOculta && !jugador.EstaMuerto && timerAtaque <= 0f)
            {
                AlertarHacia(jugador.transform.position, true);
                EjecutarZarpazo();
            }
        }
    }

    private bool EstaBloqueado(Vector2 pos, Vector2 dir, float dist)
    {
        if (dir.sqrMagnitude < 0.001f) return false;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(pos, 0.24f, dir.normalized, dist, mascaraObstaculos);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (jugador != null && hit.collider.gameObject == jugador.gameObject) continue;
            if (hit.distance <= 0.02f && Vector2.Dot(dir.normalized, hit.normal) >= -0.05f) continue;
            return true;
        }
        return false;
    }

    private bool HayParedEntre(Vector2 a, Vector2 b)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(a, b, mascaraObstaculos);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.gameObject == gameObject) continue;
            if (jugador != null && h.collider.gameObject == jugador.gameObject) continue;
            return true;
        }
        return false;
    }

    private Vector2 RotarVector(Vector2 v, float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c).normalized;
    }

    private void VoltearHacia(Vector3 objetivo)
    {
        float dx = objetivo.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.08f)
            sr.flipX = (dx < 0f);
    }

    private Vector3 ClampZona(Vector3 p)
    {
        return new Vector3(
            Mathf.Clamp(p.x, zonaMin.x, zonaMax.x),
            Mathf.Clamp(p.y, zonaMin.y, zonaMax.y),
            0f);
    }

    private void ActualizarAnimacion()
    {
        if (EstadoActual == EstadoMomia2.Escuchando && spritesScream != null && spritesScream.Length > 0)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 0.11f)
            {
                animTimer = 0f;
                animFrame = (animFrame + 1) % spritesScream.Length;
            }
            if (spritesScream[animFrame] != null) sr.sprite = spritesScream[animFrame];
            return;
        }

        float deltaY = posicionObjetivo.y - transform.position.y;
        bool yendoArriba = deltaY > 0.7f && Mathf.Abs(posicionObjetivo.x - transform.position.x) < 1.2f;

        Sprite[] setActual = (yendoArriba && spritesBack != null && spritesBack.Length > 0) ? spritesBack : spritesWalk;
        if (setActual == null || setActual.Length == 0) return;

        float velAnim = (EstadoActual == EstadoMomia2.Caza) ? 0.085f : 0.15f;
        animTimer += Time.deltaTime;
        if (animTimer >= velAnim)
        {
            animTimer = 0f;
            animFrame = (animFrame + 1) % setActual.Length;
        }
        if (setActual[animFrame] != null) sr.sprite = setActual[animFrame];
    }

    /// <summary>
    /// Repele a La Que Escucha al entrar en el radio de un Cirio Bendito encendido.
    /// </summary>
    public void RepelerPorFuegoBendito(Vector3 centroCirio, float radio)
    {
        if (!HaDespertado || EstadoActual == EstadoMomia2.Petrificada) return;

        Vector3 dif = transform.position - centroCirio;
        dif.z = 0f;
        if (dif.sqrMagnitude < 0.01f) dif = Vector3.right;
        dirRepulsionCirio = dif.normalized;

        if (EstadoActual != EstadoMomia2.AturdidaCirio)
        {
            ReproducirClip(clipGrito, 0.65f, 0.85f);
        }
        EstadoActual = EstadoMomia2.AturdidaCirio;
        timerEstado = 2.6f;

        Vector3 empuje = ClampZona(centroCirio + dirRepulsionCirio * (radio + 1.8f));
        posicionObjetivo = empuje;
    }

    /// <summary>
    /// Petrifica permanentemente a la momia al llegar el alba / sonar la campana.
    /// </summary>
    public void Petrificar()
    {
        EstadoActual = EstadoMomia2.Petrificada;
        if (audioCaminataMomia != null && audioCaminataMomia.isPlaying) audioCaminataMomia.Stop();
        if (boxCol != null) boxCol.enabled = false;
        if (auraAlerta != null) auraAlerta.enabled = false;
        sr.color = new Color(0.45f, 0.45f, 0.48f, 0.85f);
    }

    private void ActualizarAudioCercaniaMomiaN2()
    {
        if (Time.timeScale == 0f || !HaDespertado || EstadoActual == EstadoMomia2.Petrificada) return;

        if (clipCercaniaMomia == null)
        {
            clipCercaniaMomia = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_SONIDO_MOMIA_N2);
        }

        timerSonidoMomiaDeVezEnCuando -= Time.deltaTime;
        if (timerSonidoMomiaDeVezEnCuando <= 0f)
        {
            float dist = (jugador != null) ? Vector2.Distance(transform.position, jugador.transform.position) : 30f;
            float tMin = dist < 12f ? 2.5f : 3.8f;
            float tMax = dist < 12f ? 4.2f : 6.5f;
            timerSonidoMomiaDeVezEnCuando = Random.Range(tMin, tMax);

            // Solo emitir si el jugador está en un radio audible alrededor del Sector Este
            if (clipCercaniaMomia != null && dist <= 32f && fuenteAudio2D != null)
            {
                float vol;
                if (dist < 6f) vol = Random.Range(0.85f, 1.0f);
                else if (dist < 15f) vol = Mathf.Lerp(0.85f, 0.48f, (dist - 6f) / 9f);
                else vol = Mathf.Lerp(0.48f, 0.16f, Mathf.Clamp01((dist - 15f) / 17f));

                fuenteAudio2D.pitch = Random.Range(0.92f, 1.06f);
                fuenteAudio2D.PlayOneShot(clipCercaniaMomia, Mathf.Clamp01(vol));
            }
        }
    }

    private void ActualizarAudioCaminataMomiaN2()
    {
        if (audioCaminataMomia == null) return;
        if (clipCaminataMomia == null)
        {
            clipCaminataMomia = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_CAMINATA_MOMIA);
        }

        bool caminando = HaDespertado && !apareciendo &&
                         EstadoActual != EstadoMomia2.Dormida &&
                         EstadoActual != EstadoMomia2.Escuchando &&
                         EstadoActual != EstadoMomia2.Petrificada &&
                         Time.timeScale > 0f &&
                         Nivel4Manager.Instance != null && !Nivel4Manager.Instance.JuegoTerminado &&
                         (transform.position - ultimaPosFrame).magnitude > 0.002f;

        float dist = (jugador != null) ? Vector2.Distance(transform.position, jugador.transform.position) : 30f;

        if (caminando && clipCaminataMomia != null && dist <= 20f)
        {
            if (audioCaminataMomia.clip != clipCaminataMomia) audioCaminataMomia.clip = clipCaminataMomia;
            float prox = Mathf.Pow(Mathf.Clamp01(1f - (dist / 20f)), 1.25f);
            audioCaminataMomia.volume = Mathf.Lerp(audioCaminataMomia.volume, prox * 0.82f, Time.deltaTime * 8f);
            audioCaminataMomia.pitch = (EstadoActual == EstadoMomia2.Caza) ? 1.22f : 0.96f;
            if (!audioCaminataMomia.isPlaying) audioCaminataMomia.Play();
        }
        else
        {
            audioCaminataMomia.volume = Mathf.Lerp(audioCaminataMomia.volume, 0f, Time.deltaTime * 10f);
            if (audioCaminataMomia.volume < 0.01f && audioCaminataMomia.isPlaying) audioCaminataMomia.Stop();
        }
    }

    private void ReproducirClip(AudioClip clip, float vol, float pitch)
    {
        if (clip == null || audioSource == null || !sr.enabled) return;
        float dist = (jugador != null) ? Vector2.Distance(transform.position, jugador.transform.position) : 25f;
        float aten = Mathf.Clamp01(1f - (dist / 24f));
        if (aten <= 0.02f) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, vol * aten);
    }

    private void ReproducirClipConDistancia(AudioClip clip, float volMax, float pitch)
    {
        if (clip == null || audioSource == null || !sr.enabled) return;
        float dist = (jugador != null) ? Vector2.Distance(transform.position, jugador.transform.position) : 25f;
        float aten = Mathf.Clamp01(1f - (dist / 28f));
        float finalVol = Mathf.Max(dist <= 22f ? 0.30f : 0f, volMax * aten);
        if (finalVol <= 0.02f) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(finalVol));
    }

    private void SintetizarAudios()
    {
        clipCercaniaMomia = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_SONIDO_MOMIA_N2);
        clipAtaqueN2 = AudioManagerNivel4.ObtenerClip(AudioManagerNivel4.CLIP_ATAQUE_MOMIA_N2);

        int srHz = 22050;

        // Rugido gutural de detección fiel al Nivel 2 (SintetizarGritoMomia2)
        int lenRug = (int)(44100 * 0.85f);
        float[] rugData = new float[lenRug];
        for (int i = 0; i < lenRug; i++)
        {
            float t = (float)i / 44100;
            float prog = t / 0.85f;
            float fm = Mathf.Sin(2f * Mathf.PI * 16f * t) * 9f;
            float freq = 65f + fm;
            float baseWave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.55f +
                             Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.28f;
            float harmonic = Mathf.Sin(2f * Mathf.PI * 310f * t) *
                             Mathf.Exp(-Mathf.Pow(prog - 0.35f, 2f) * 14f) * 0.18f;
            float env = Mathf.Sin(prog * Mathf.PI) * (1f - prog * 0.25f);
            rugData[i] = Mathf.Clamp((baseWave + harmonic) * env * 0.85f, -0.95f, 0.95f);
        }
        clipRugidoDeteccion = AudioClip.Create("Momia2_RugidoAlerta_N4", lenRug, 1, 44100, false);
        clipRugidoDeteccion.SetData(rugData, 0);

        // Grito agudo gutural de "La Que Escucha"
        int lenGrito = (int)(srHz * 0.55f);
        float[] gData = new float[lenGrito];
        for (int i = 0; i < lenGrito; i++)
        {
            float t = (float)i / srHz;
            float env = Mathf.Sin(Mathf.PI * t / 0.55f);
            float f = 310f + Mathf.Sin(t * 24f) * 45f;
            float w = Mathf.Sin(2f * Mathf.PI * f * t) * 0.55f +
                      Mathf.Sin(2f * Mathf.PI * f * 1.48f * t) * 0.25f +
                      Random.Range(-0.18f, 0.18f);
            gData[i] = Mathf.Clamp(w * env * 0.65f, -1f, 1f);
        }
        clipGrito = AudioClip.Create("Momia2_Grito_N4", lenGrito, 1, srHz, false);
        clipGrito.SetData(gData, 0);

        // Zarpazo desgarrador de garras fiel al Nivel 2
        int lenZarp = (int)(srHz * 0.28f);
        float[] zData = new float[lenZarp];
        for (int i = 0; i < lenZarp; i++)
        {
            float t = (float)i / srHz;
            float prog = t / 0.28f;
            float sweepFreq = Mathf.Lerp(1450f, 380f, prog);
            float slashTone = Mathf.Sin(2f * Mathf.PI * sweepFreq * t) * 0.38f;
            float ruidoDesgarro = Random.Range(-0.62f, 0.62f);
            float env = Mathf.Sin(prog * Mathf.PI) * Mathf.Exp(-prog * 2.2f);
            zData[i] = Mathf.Clamp((slashTone + ruidoDesgarro) * env * 0.9f, -0.95f, 0.95f);
        }
        clipZarpazo = AudioClip.Create("Momia2_Zarpazo_N4", lenZarp, 1, srHz, false);
        clipZarpazo.SetData(zData, 0);

        // Pasos secos arrastrados
        int lenPaso = (int)(srHz * 0.12f);
        float[] pData = new float[lenPaso];
        for (int i = 0; i < lenPaso; i++)
        {
            float t = (float)i / srHz;
            float env = Mathf.Exp(-t * 22f);
            pData[i] = Mathf.Clamp((Mathf.Sin(2f * Mathf.PI * 115f * t) * 0.5f + Random.Range(-0.35f, 0.35f)) * env, -1f, 1f);
        }
        clipPasos = AudioClip.Create("Momia2_Pasos_N4", lenPaso, 1, srHz, false);
        clipPasos.SetData(pData, 0);
    }
}
