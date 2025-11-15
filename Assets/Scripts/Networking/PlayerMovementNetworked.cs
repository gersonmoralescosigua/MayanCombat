using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkTransform))]
public class PlayerMovementNetworked : NetworkBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    [Header("Físicas / Suelo")]
    public float groundCheckDistance = 0.6f;
    public LayerMask groundMask; // asignar en prefab al layer "Suelo"

    [Header("Golpe cuerpo a cuerpo")]
    public float attackForce = 18f;

    [Header("Referencias")]
    public Animator animator; // Arrastra el Animator aquí en el prefab

    // networked props (usadas por animator)
    [Networked] public float NetSpeed { get; set; }
    [Networked] public float NetVertical { get; set; }
    [Networked] public bool NetGrounded { get; set; }
    [Networked] public NetworkBool NetAttacking { get; set; }

    private Rigidbody2D rb;
    private bool wasGrounded = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
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
        // SOLO el dueño controla el movimiento
        if (HasInputAuthority && GetInput(out NetworkInputData data))
        {
            Vector2 desired = new Vector2(data.Move.x * moveSpeed, rb.linearVelocity.y);

            // suavizar velocidad X (blend)
            float blend = Mathf.Clamp01(30f * (float)Runner.DeltaTime);
            float newVelX = Mathf.Lerp(rb.linearVelocity.x, desired.x, blend);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);

            // Salto - solo cuando presiona el botón Y está en suelo
            if (data.Jump && NetGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                
                // Trigger de salto en animator
                if (animator != null)
                    animator.SetTrigger("Saltar");
            }

            // Ataque
            if (data.Attack)
            {
                NetAttacking = true;
                if (animator != null)
                    animator.SetTrigger("Atacar");
                    
                // ejemplo simple: impulse hacia delante para el que ataca
                Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                rb.AddForce(dir * attackForce, ForceMode2D.Impulse);
            }
            else
            {
                NetAttacking = false;
            }

            // Voltear personaje según dirección
            if (data.Move.x > 0.1f)
                transform.localScale = new Vector3(1, 1, 1);
            else if (data.Move.x < -0.1f)
                transform.localScale = new Vector3(-1, 1, 1);
        }

        // siempre actualizar props networked (replicadas)
        NetSpeed = Mathf.Abs(rb.linearVelocity.x);
        NetVertical = rb.linearVelocity.y;
        NetGrounded = IsGrounded();

        // Actualizar animator localmente (por si acaso)
        if (animator != null)
        {
            animator.SetFloat("Velocidad", NetSpeed);
            animator.SetBool("EnSuelo", NetGrounded);
            animator.SetBool("IsWalking", NetSpeed > 0.1f);
        }
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