// Assets/Scripts/Networking/PlayerMovementNetworked.cs
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementNetworked : NetworkBehaviour
{
    [Networked] public PlayerRef Owner { get; set; }

    [Header("Movimiento")]
    public float moveSpeed = 5f;    // usado en tests y anim
    public float maxSpeed = 8f;
    public float jumpForce = 7f;

    [Header("Físicas / Suelo")]
    public float groundCheckDistance = 0.6f;
    public LayerMask groundMask; // asignar en prefab al layer "Suelo"

    [Header("Golpe cuerpo a cuerpo")]
    public float attackForce = 18f;

    // networked props (usadas por animator)
    [Networked] public float NetSpeed { get; set; }
    [Networked] public float NetVertical { get; set; }
    [Networked] public bool NetGrounded { get; set; }

    Rigidbody2D rb;
    Animator animator;

    // local
    private Vector2 lastMove = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        if (rb != null) rb.freezeRotation = true;
    }

    public override void Spawned()
    {
        // inicializar networked
        NetSpeed = 0f;
        NetVertical = 0f;
        NetGrounded = true;
    }

    public override void FixedUpdateNetwork()
    {
        // Solo quien tiene input authority procesa inputs
        if (GetInput(out NetworkInputData data))
        {
            Vector2 desired = new Vector2(data.Move.x * moveSpeed, rb.linearVelocity.y);

            // suavizar velocidad X (blend)
            float blend = Mathf.Clamp01(30f * (float)Runner.DeltaTime);
            float newVelX = Mathf.Lerp(rb.linearVelocity.x, desired.x, blend);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);

            // salto
            if (data.Jump)
            {
                if (IsGrounded())
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                }
            }

            // ataque (solo local, pero habrá que notificar al host si quieres efecto server-side)
            if (data.Attack)
            {
                // ejemplo simple: impulse hacia delante para el que ataca
                Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                rb.AddForce(dir * attackForce, ForceMode2D.Impulse);
            }

            lastMove = data.Move;
        }

        // siempre actualizar props networked (replicadas)
        NetSpeed = Mathf.Abs(rb.linearVelocity.x);
        NetVertical = rb.linearVelocity.y;
        NetGrounded = IsGrounded();
    }

    bool IsGrounded()
    {
        Vector2 origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundMask);
        return hit.collider != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}