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
    public LayerMask groundMask;

    [Header("Golpe cuerpo a cuerpo")]
    public float attackForce = 18f;

    [Header("Referencias")]
    public Animator animator;

    // Networked properties
    [Networked] public float NetSpeed { get; set; }
    [Networked] public float NetVertical { get; set; }
    [Networked] public bool NetGrounded { get; set; }
    [Networked] public NetworkBool NetAttacking { get; set; }
    [Networked] public int NetFacingDirection { get; set; }

    // Para detección de botones presionados (no mantenidos)
    [Networked] private NetworkBool _wasJumpPressed { get; set; }
    [Networked] private NetworkBool _wasAttackPressed { get; set; }

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rb != null) 
        {
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.None;
        }
    }

    public override void Spawned()
    {
        // DEBUG crítico para verificar autoridad
        Debug.Log($"🎮 [{Object?.Id}] Spawned - InputAuthority: {Object?.InputAuthority}, LocalPlayer: {Runner?.LocalPlayer}, HasInputAuth: {HasInputAuthority}");
        
        NetSpeed = 0f;
        NetVertical = 0f;
        NetGrounded = true;
        NetFacingDirection = 1;
        _wasJumpPressed = false;
        _wasAttackPressed = false;
    }

    public override void FixedUpdateNetwork()
    {
        // Verificación de seguridad
        if (Object == null || !Object.IsValid || Runner == null) return;

        // SOLO procesar input si tenemos autoridad de input
        if (GetInput(out NetworkInputData data))
        {
            ProcessInput(data);
        }

        // ACTUALIZAR PROPIEDADES NETWORKED (todos los clientes)
        UpdateNetworkedProperties();
        
        // APLICAR VOLTEO (todos los clientes)
        ApplyFacingDirection();
        
        // ACTUALIZAR ANIMACIONES (todos los clientes)
        UpdateAnimations();
    }

    private void ProcessInput(NetworkInputData data)
    {
        // 1. MOVIMIENTO HORIZONTAL (solo input authority)
        if (HasInputAuthority)
        {
            Vector2 desiredVelocity = new Vector2(data.Move.x * moveSpeed, rb.linearVelocity.y);
            
            // Suavizado optimizado
            float blend = Mathf.Clamp01(25f * Runner.DeltaTime);
            float newVelX = Mathf.Lerp(rb.linearVelocity.x, desiredVelocity.x, blend);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
        }

        // 2. DETECCIÓN DE BOTONES PRESIONADOS (solo input authority)
        if (HasInputAuthority)
        {
            // SALTO (solo cuando se PRESIONA el botón, no se mantiene)
            bool jumpPressed = data.JumpPressed && !_wasJumpPressed;
            _wasJumpPressed = data.JumpPressed;

            if (jumpPressed && NetGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                if (animator != null) 
                {
                    animator.SetTrigger("Saltar");
                    Debug.Log("🦘 Salto ejecutado");
                }
            }

            // ATAQUE (solo cuando se PRESIONA el botón)
            bool attackPressed = data.AttackPressed && !_wasAttackPressed;
            _wasAttackPressed = data.AttackPressed;

            if (attackPressed)
            {
                NetAttacking = true;
                if (animator != null) 
                {
                    animator.SetTrigger("Atacar");
                    Debug.Log("⚔️ Ataque ejecutado");
                }
                
                Vector2 dir = NetFacingDirection > 0 ? Vector2.right : Vector2.left;
                rb.AddForce(dir * attackForce, ForceMode2D.Impulse);
            }
            else
            {
                NetAttacking = false;
            }
        }

        // 3. ACTUALIZAR DIRECCIÓN (basado en input de cualquier jugador)
        if (data.Move.x > 0.1f)
        {
            NetFacingDirection = 1; // Derecha
        }
        else if (data.Move.x < -0.1f)
        {
            NetFacingDirection = -1; // Izquierda
        }
    }

    private void UpdateNetworkedProperties()
    {
        if (rb != null)
        {
            NetSpeed = Mathf.Abs(rb.linearVelocity.x);
            NetVertical = rb.linearVelocity.y;
            NetGrounded = IsGrounded();
        }
    }

    private void ApplyFacingDirection()
    {
        // SOLUCIÓN PERFECTA PARA VOLTEO SIN DEFORMACIÓN
        Vector3 currentScale = transform.localScale;
        float newXScale = Mathf.Abs(currentScale.x) * (NetFacingDirection >= 0 ? 1f : -1f);
        
        // Solo aplicar si hay cambio para optimizar
        if (Mathf.Abs(newXScale - currentScale.x) > 0.01f)
        {
            transform.localScale = new Vector3(newXScale, currentScale.y, currentScale.z);
        }
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat("Velocidad", NetSpeed);
            animator.SetBool("EnSuelo", NetGrounded);
            animator.SetBool("IsWalking", NetSpeed > 0.1f);
        }
    }

    private bool IsGrounded()
    {
        Vector2 origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundMask);
        bool grounded = hit.collider != null;
        
        // Debug ocasional del estado en suelo
        if (Runner.Tick % 100 == 0 && HasInputAuthority)
        {
            Debug.Log($"🦶 Grounded: {grounded}, Position: {transform.position}");
        }
        
        return grounded;
    }

    // DEBUG para verificar estado en tiempo real
    public override void Render()
    {
        if (Runner.Tick % 150 == 0 && Object != null) // Cada 150 ticks
        {
            Debug.Log($"👀 [{Object.Id}] Render - InputAuth: {Object.InputAuthority}, Speed: {NetSpeed}, Grounded: {NetGrounded}");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}