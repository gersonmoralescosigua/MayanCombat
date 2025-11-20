using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkTransform))]
public class PlayerMovementNetworked : NetworkBehaviour
{
    [Header("Configuración de Agilidad")]
    public float moveSpeed = 9f;     
    public float jumpForce = 16f;    

    [Header("Referencias")]
    public Animator animator;

    // Estado de Red
    [Networked] public float NetSpeed { get; set; }
    [Networked] public bool NetGrounded { get; set; }
    [Networked] public int NetFacingDirection { get; set; }
    [Networked] private NetworkBool _wasJumpPressed { get; set; }
    [Networked] private NetworkBool _wasAttackPressed { get; set; }

    private Rigidbody2D rb;
    public float groundCheckDistance = 0.6f;
    public LayerMask groundMask;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        if (rb != null) 
        {
            rb.freezeRotation = true;
            // CRUCIAL: Desactivamos la interpolación de Unity para que no pelee con NetworkTransform (adiós temblor)
            rb.interpolation = RigidbodyInterpolation2D.None; 
            // Forzamos gravedad alta para que caiga rápido
            rb.gravityScale = 3f; 
        }
    }

    public override void Spawned()
    {
        NetGrounded = true;
        NetFacingDirection = 1;
    }

    // FÍSICA Y LÓGICA (Solo aquí para evitar desincronización)
    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid || Runner == null) return;

        // Detección de suelo física
        NetGrounded = IsGrounded();

        if (GetInput(out NetworkInputData data))
        {
            // 1. MOVIMIENTO HORIZONTAL DIRECTO
            rb.linearVelocity = new Vector2(data.Move.x * moveSpeed, rb.linearVelocity.y);

            // 2. SALTO
            if (data.JumpPressed && !_wasJumpPressed)
            {
                if (NetGrounded)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Reset Y
                    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                    if (animator != null) animator.SetTrigger("Saltar");
                }
            }
            _wasJumpPressed = data.JumpPressed;

            // 3. ATAQUE
            if (data.AttackPressed && !_wasAttackPressed)
            {
                if (animator != null) animator.SetTrigger("Atacar");
            }
            _wasAttackPressed = data.AttackPressed;

            // 4. DIRECCIÓN
            if (data.Move.x > 0.1f) NetFacingDirection = 1;
            else if (data.Move.x < -0.1f) NetFacingDirection = -1;
        }

        // Actualizar variable para animaciones
        NetSpeed = Mathf.Abs(rb.linearVelocity.x);
    }

    // SOLO LÓGICA VISUAL (Suavizado)
    public override void Render()
    {
        // Interpolación visual del volteo (Flip)
        Vector3 currentScale = transform.localScale;
        float targetX = Mathf.Abs(currentScale.x) * (NetFacingDirection >= 0 ? 1f : -1f);
        // Lerp suave solo para el flip visual
        transform.localScale = Vector3.Lerp(currentScale, new Vector3(targetX, currentScale.y, currentScale.z), Time.deltaTime * 20f);

        // Animaciones
        if (animator != null)
        {
            animator.SetFloat("Velocidad", NetSpeed);
            animator.SetBool("EnSuelo", NetGrounded);
            animator.SetBool("IsWalking", NetSpeed > 0.1f);
        }
    }

    private bool IsGrounded()
    {
        Vector2 origin = (Vector2)rb.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundMask);
        return hit.collider != null;
    }
}


/*using Fusion;
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

// En PlayerMovementNetworked.cs

public override void FixedUpdateNetwork()
{
    // Verificación básica
    if (Object == null || !Object.IsValid || Runner == null) return;

    // 1. Obtener Input. 
    // GetInput devuelve TRUE si:
    // a) Soy el Cliente y es mi input.
    // b) Soy el Servidor y recibí el input del cliente.
    // c) Soy el Host y es mi propio input.
    if (GetInput(out NetworkInputData data))
    {
        ProcessInput(data);
    }
    
    // Mantenemos esto fuera para interpolación visual correcta
    UpdateNetworkedProperties();
    ApplyFacingDirection();
    UpdateAnimations();
}

private void ProcessInput(NetworkInputData data)
{
    // 1. MOVIMIENTO HORIZONTAL
    // Calculamos la velocidad deseada basada en el input
    Vector2 desiredVelocity = new Vector2(data.Move.x * moveSpeed, rb.linearVelocity.y);

    // IMPORTANTE: Asignación directa. 
    // Eliminamos el Lerp para que la predicción de Fusion (NetworkTransform) haga el suavizado visual.
    rb.linearVelocity = desiredVelocity;

    // 2. ACCIONES (Salto y Ataque)
    
    // SALTO
    if (data.JumpPressed && !_wasJumpPressed)
    {
        if (NetGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            if (animator != null) animator.SetTrigger("Saltar");
        }
    }
    _wasJumpPressed = data.JumpPressed;

    // ATAQUE
    if (data.AttackPressed && !_wasAttackPressed)
    {
        NetAttacking = true;
        if (animator != null) animator.SetTrigger("Atacar");
        
        Vector2 dir = NetFacingDirection > 0 ? Vector2.right : Vector2.left;
        rb.AddForce(dir * attackForce, ForceMode2D.Impulse);
    }
    else
    {
        NetAttacking = false;
    }
    _wasAttackPressed = data.AttackPressed;

    // 3. DIRECCIÓN
    if (data.Move.x > 0.1f) NetFacingDirection = 1;
    else if (data.Move.x < -0.1f) NetFacingDirection = -1;
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
*/