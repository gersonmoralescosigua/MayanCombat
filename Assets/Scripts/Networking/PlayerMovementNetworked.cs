// Assets/Scripts/Gameplay/PlayerMovementNetworked.cs
using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementNetworked : NetworkBehaviour
{
    [Header("Tuning")]
    public float moveSpeed = 5f;
    public float maxSpeed = 8f;
    public float jumpForce = 7f;
    public float interpolation = 12f; // cómo lerpean los remotos
    public float groundCheckDistance = 0.6f;
    public LayerMask groundMask;

    // Attack impulse (opcional)
    public float attackForce = 18f;

    // Networked state (replicado)
    [Networked] public Vector2 NetPosition { get; set; }
    [Networked] public Vector2 NetVelocity { get; set; }
    [Networked] public bool NetGrounded { get; set; }
    [Networked] public PlayerRef OwnerPlayer { get; set; }

    // Local
    Rigidbody2D rb;
    Animator animator;

    // Input local cache (filled from Runner.GetInput in FixedUpdateNetwork)
    private Vector2 inputMove = Vector2.zero;
    private bool wantJump = false;
    private bool wantAttack = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>(true);
        if (rb != null) rb.freezeRotation = true;
    }

    public override void Spawned()
    {
        // Inicializa la posición networked si es primera vez
        NetPosition = (Vector2)transform.position;
        NetVelocity = Vector2.zero;
        NetGrounded = true;
    }

    // Este FixedUpdateNetwork se ejecuta en todos los nodos, pero GetInput sólo true en el cliente con authority
    public override void FixedUpdateNetwork()
    {
        // 1) Leer input (solo clientes con input authority)
        if (GetInput(out NetworkInputData input))
        {
            inputMove = input.Move;
            wantJump = input.JumpPressed;
            wantAttack = input.AttackPressed;
        }
        else
        {
            // si no hay input, limpiar (evita arrastres)
            inputMove = Vector2.zero;
            wantJump = false;
            wantAttack = false;
        }

        // 2) Owner -> aplica movimiento físico local y escribe estado networked
        if (Object.HasInputAuthority)
        {
            // Aplicar horizontal
            Vector2 desired = new Vector2(inputMove.x * moveSpeed, rb.linearVelocity.y);
            // Lerp para suavizar
            float blend = Mathf.Clamp01(30f * (float)Runner.DeltaTime);
            float newVelX = Mathf.Lerp(rb.linearVelocity.x, desired.x, blend);
            rb.linearVelocity = new Vector2(Mathf.Clamp(newVelX, -maxSpeed, maxSpeed), rb.linearVelocity.y);

            // Salto
            if (wantJump && IsGrounded())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }

            // Ataque: ejemplo simple aplica impulso horizontal
            if (wantAttack)
            {
                // aplica un impulso hacia delante según facing
                float dir = Mathf.Sign(rb.linearVelocity.x != 0 ? rb.linearVelocity.x : inputMove.x != 0 ? inputMove.x : 1f);
                rb.AddForce(new Vector2(dir * attackForce, 0f), ForceMode2D.Impulse);
            }

            // Escribir estado networked (propagar a los remotos)
            NetPosition = rb.position;
            NetVelocity = rb.linearVelocity;
            NetGrounded = IsGrounded();
        }
        else
        {
            // 3) Remotos: leen el estado networked y aplican interpolación suave a transform/rigidbody visual
            Vector2 pos = NetPosition;
            Vector2 vel = NetVelocity;

            // Interpolación: mover el transform físico (no ejecutar físicas) para evitar conflictos.
            Vector2 current = rb.position;
            Vector2 target = Vector2.Lerp(current, pos, Mathf.Clamp01(interpolation * (float)Runner.DeltaTime));
            rb.position = target;

            // Opcional: ajustar velocity local para que colisiones reaccionen
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, vel, Mathf.Clamp01(interpolation * (float)Runner.DeltaTime));

            // actualizar grounded (para animaciones)
            NetGrounded = NetGrounded; // ya viene replicado
        }

        // Animations (se puede hacer en otro script)
        if (animator != null)
        {
            float speed = Mathf.Abs(rb.linearVelocity.x);
            animator.SetFloat("Speed", speed);
            animator.SetFloat("Vertical", rb.linearVelocity.y);
            animator.SetBool("Grounded", IsGrounded());
        }
    }

    bool IsGrounded()
    {
        Vector2 origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundMask);
        return hit.collider != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
#endif

    // Helper para que el servidor pueda asignar OwnerPlayer inmediatamente después de spawn
    public void SetOwnerPlayer(PlayerRef p)
    {
        // Esto se hace por el servidor (state authority) justo después de runner.Spawn
        OwnerPlayer = p;
    }
}