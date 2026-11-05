using UnityEngine;

/// <summary>
/// Controla dinámicamente el orden de renderizado (sortingOrder) en perspectiva Top-Down 2D.
/// Los objetos con menor Y (más cercanos a la cámara abajo) se dibujan por delante
/// de los objetos con mayor Y (más lejanos arriba).
/// Esto evita al 100% que los personajes caminen "por encima" de vitrinas o se dibujen sobre muros.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSortOrder : MonoBehaviour
{
    [Tooltip("Ajuste del punto de apoyo/pies respecto al centro del objeto.")]
    public float yOffset = 0f;

    [Tooltip("Si es estático (vitrina, muro, cuadro) solo calcula el orden una vez.")]
    public bool esEstatico = false;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ActualizarOrden();
    }

    private void LateUpdate()
    {
        if (!esEstatico)
        {
            ActualizarOrden();
        }
    }

    public void ActualizarOrden()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // Base de apoyo en coordenadas de mundo con offset de baseline positivo
        float baseFootY = transform.position.y + yOffset;
        sr.sortingOrder = 500 + Mathf.RoundToInt(-baseFootY * 10f);
    }
}
