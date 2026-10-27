using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidad")]
    public float speed = 4.8f;
    public float runSpeed = 7.5f;

    [Header("Estados de Control")]
    public bool canMove = true;

    private Rigidbody2D rb;
    private PlayerController controller;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();

        if (controller == null)
        {
            controller = gameObject.AddComponent<PlayerController>();
        }
    }

    private void Update()
    {
        // Si canMove es falso (por trampa de brazos de vitrina), frenar al jugador
        if (!canMove && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}