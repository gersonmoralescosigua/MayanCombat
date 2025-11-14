using Fusion;
using UnityEngine;

/// <summary>
/// Player movement networked (FUSION).
/// - Usa Rigidbody2D (no depende de NetworkRigidbody2D).
/// - Expone propiedades networked NetSpeed, NetVertical, NetGrounded
/// - Campos públicos moveSpeed, jumpForce utilizados por tests y animator.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementNetworked : NetworkBehaviour
{
    [Header("Movimiento (editable)")]
    public float moveSpeed = 5f;        // usado por tests
    public float maxSpeed = 8f;
    public float jumpForce = 7f;        // usado por tests

    [Header("Física")]
    public float groundCheckDistance = 0.6f;
    public LayerMask groundMask;

    // Attack (si quieres usar)
    public float attackForce = 18f;

    // --------- Networked properties (usadas por el AnimatorNetwork) ----------
    [Networked] public float NetSpeed { get; set; }
    [Networked] public float NetVertical { get; set; }
    [Networked] public bool NetGrounded { get; set; }

    // -------------------------------------
    private Rigidbody2D rb;
    private Animator animator;

    // Local runtime (no networked)
    private bool wantJump = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        // Seguridad en caso de sueño del rigidbody
        if (rb != null) rb.freezeRotation = true;
    }

    public override void Spawned()
    {
        // inicializa networked vars
        NetSpeed = 0f;
        NetVertical = 0f;
        NetGrounded = true;
    }

    public override void FixedUpdateNetwork()
    {
        // Sólo quien tenga input authority envía inputs
        if (GetInput(out NetworkInputData data))
        {
            // MOVIMIENTO HORIZONTAL
            Vector2 desired = data.Move * moveSpeed;
            Vector2 vel = rb.linearVelocity;

            // Interpolación suave
            float blend = Mathf.Clamp01(30f * (float)Runner.DeltaTime); // ajuste rápido
            float newVelX = Mathf.Lerp(vel.x, desired.x, blend);
            rb.linearVelocity = new Vector2(newVelX, vel.y);

            // SALTO (se detecta en GetInput con botón)
            if (data.JumpPressed)
            {
                // sólo si en suelo
                if (IsGrounded())
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                }
            }

            // Attack simple: se puede implementar similar
            if (data.AttackPressed)
            {
                // ejemplo: aplicar fuerza hacia adelante (opcional)
            }
        }

        // Actualiza props networked (estas serán replicadas a todos)
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

    // Para debugging visual en editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}