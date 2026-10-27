using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Nivel1Setup : MonoBehaviour
{
    void Awake()
    {
        // 1. Eliminar cualquier mapa residual viejo
        GameObject mapaViejo = GameObject.Find("MapaNivel1");
        if (mapaViejo != null)
        {
            mapaViejo.SetActive(false);
            Destroy(mapaViejo);
        }

        // 2. Instanciar Sistema de Iluminación
        if (FindFirstObjectByType<SistemaIluminacionMuseo>() == null)
        {
            GameObject ilumObj = new GameObject("SistemaIluminacionMuseo");
            ilumObj.AddComponent<SistemaIluminacionMuseo>();
        }

        // 3. Instanciar / Conectar Level1Manager
        Level1Manager l1m = FindFirstObjectByType<Level1Manager>();
        if (l1m == null)
        {
            GameObject mgr = new GameObject("Level1Manager");
            l1m = mgr.AddComponent<Level1Manager>();
        }

        // Conectar UI de Lore si no existe
        if (FindFirstObjectByType<LoreUIController>() == null)
        {
            GameObject loreObj = new GameObject("LoreUIController");
            loreObj.AddComponent<LoreUIController>();
        }

        // Conectar Nivel1Manager (HUD de Salud y Game Over) si no existe
        if (FindFirstObjectByType<Nivel1Manager>() == null)
        {
            GameObject n1mObj = new GameObject("Nivel1Manager");
            n1mObj.AddComponent<Nivel1Manager>();
        }

        // 4. Construir el Museo Colosal y sus salas
        if (FindFirstObjectByType<GeneradorEscenarioMuseo>() == null)
        {
            GameObject genObj = new GameObject("SistemaEscenarioMuseo");
            genObj.AddComponent<GeneradorEscenarioMuseo>();
        }

        // 5. Configurar al Jugador (Vigilante)
        tag = "Player";
        gameObject.name = "Vigilante";
        transform.position = new Vector3(0f, -15.5f, 0f); // Entrada inferior central
        transform.localScale = new Vector3(1.55f, 1.55f, 1f);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.size = new Vector2(0.55f, 0.85f);
        col.offset = new Vector2(0f, -0.1f);

        PlayerController pc = GetComponent<PlayerController>();
        if (pc == null) pc = gameObject.AddComponent<PlayerController>();

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm == null) pm = gameObject.AddComponent<PlayerMovement>();

        // Integrar PistolController al Jugador
        PistolController pistol = GetComponent<PistolController>();
        if (pistol == null) pistol = gameObject.AddComponent<PistolController>();

        // 6. Configurar la Cámara
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.SetParent(null);
            cam.backgroundColor = new Color(0.18f, 0.17f, 0.22f, 1f); // Tono cálido colonial
            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();
            cf.objetivo = transform;
            cf.zoomCercano = 3.8f;
            cf.zoomMapaCompleto = 16.5f;
        }

        // 7. Configurar la Momia Principal
        MomiaController momiaExistente = FindFirstObjectByType<MomiaController>();
        GameObject momiaObj = (momiaExistente != null) ? momiaExistente.gameObject : new GameObject("Momia1_LaQueCamina");
        momiaObj.name = "Momia1_LaQueCamina";
        try { momiaObj.tag = "MainMummy"; } catch { }
        momiaObj.transform.position = new Vector3(0f, 14f, 0f); // Bóveda norte lejana
        momiaObj.transform.localScale = new Vector3(1.55f, 1.55f, 1f);

        SpriteRenderer momiaSr = momiaObj.GetComponent<SpriteRenderer>();
        if (momiaSr == null) momiaSr = momiaObj.AddComponent<SpriteRenderer>();
        momiaSr.sortingOrder = 10;
        if (momiaSr.sprite == null) momiaSr.sprite = Resources.Load<Sprite>("Momia/momia_idle");

        BoxCollider2D momiaCol = momiaObj.GetComponent<BoxCollider2D>();
        if (momiaCol == null) momiaCol = momiaObj.AddComponent<BoxCollider2D>();
        momiaCol.size = new Vector2(0.6f, 1.0f);

        Rigidbody2D momiaRb = momiaObj.GetComponent<Rigidbody2D>();
        if (momiaRb == null) momiaRb = momiaObj.AddComponent<Rigidbody2D>();
        momiaRb.gravityScale = 0f;
        momiaRb.freezeRotation = true;
        momiaRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        momiaRb.mass = 50000f; // Masa inamovible colosal
        momiaRb.constraints = RigidbodyConstraints2D.FreezeAll;

        MomiaController mc = momiaObj.GetComponent<MomiaController>();
        if (mc == null) mc = momiaObj.AddComponent<MomiaController>();

        // En fase SafeExploration inicia visible y petrificada
        momiaObj.SetActive(true);
        if (mc != null) mc.enabled = false;

        // Conectar momia y referencias al Level1Manager
        if (l1m != null)
        {
            l1m.ConfigurarMomia(momiaObj);
            l1m.ResolverReferencias();
        }

        Destroy(this);
    }
}
