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
    
    // SOLUCIÓN PERFECTA PARA EL VOLTEO SIN DEFORMACIÓN
    [Networked] private NetworkButtons _previousButtons { get; set; }
    private float _currentFacingDirection = 1f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rb != null) 
        {
            rb.freezeRotation = true;
            // QUITAMOS la interpolación del Rigidbody para evitar conflicto con NetworkTransform
            rb.interpolation = RigidbodyInterpolation2D.None;
        }
    }

    public override void Spawned()
    {
        NetSpeed = 0f;
        NetVertical = 0f;
        NetGrounded = true;
        _currentFacingDirection = 1f;
    }

    public override void FixedUpdateNetwork()
    {
        // CRÍTICO: Todos procesan el input, pero solo el dueño aplica física
        if (GetInput(out NetworkInputData data))
        {
            // Solo el dueño mueve el Rigidbody
            if (HasInputAuthority)
            {
                ProcessMovement(data);
                ProcessJump(data);
                ProcessAttack(data);
            }
            
            // Todos actualizan la dirección basada en el input
            UpdateFacingDirection(data);
        }

        // Todos actualizan propiedades networked
        UpdateNetworkedProperties();
        
        // Todos aplican el volteo (esto es seguro porque no afecta la física)
        ApplyFacingDirection();
        
        // Todos actualizan animaciones
        UpdateAnimations();
    }

    private void ProcessMovement(NetworkInputData data)
    {
        Vector2 desired = new Vector2(data.Move.x * moveSpeed, rb.linearVelocity.y);

        // Suavizado original que funcionaba bien
        float blend = Mathf.Clamp01(30f * (float)Runner.DeltaTime);
        float newVelX = Mathf.Lerp(rb.linearVelocity.x, desired.x, blend);
        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
    }

    private void ProcessJump(NetworkInputData data)
    {
        // Detectar cuando se PRESIONA el botón, no cuando está mantenido
        var pressed = data.GetButtonPressed(_previousButtons);
        _previousButtons = data.Buttons;

        if (pressed.Jump && NetGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            
            if (animator != null)
                animator.SetTrigger("Saltar");
        }
    }

    private void ProcessAttack(NetworkInputData data)
    {
        // Detectar cuando se PRESIONA el botón
        var pressed = data.GetButtonPressed(_previousButtons);

        if (pressed.Attack)
        {
            NetAttacking = true;
            if (animator != null)
                animator.SetTrigger("Atacar");
                    
            Vector2 dir = _currentFacingDirection > 0 ? Vector2.right : Vector2.left;
            rb.AddForce(dir * attackForce, ForceMode2D.Impulse);
        }
        else
        {
            NetAttacking = false;
        }
    }

    private void UpdateFacingDirection(NetworkInputData data)
    {
        // SOLUCIÓN PERFECTA: Solo cambiamos una variable local, no el transform
        if (data.Move.x > 0.1f)
        {
            _currentFacingDirection = 1f;
        }
        else if (data.Move.x < -0.1f)
        {
            _currentFacingDirection = -1f;
        }
    }

    private void UpdateNetworkedProperties()
    {
        NetSpeed = Mathf.Abs(rb.linearVelocity.x);
        NetVertical = rb.linearVelocity.y;
        NetGrounded = IsGrounded();
    }

    private void ApplyFacingDirection()
    {
        // SOLUCIÓN PERFECTA: Scale matemáticamente seguro
        // Esto se ejecuta en TODOS los clientes de forma idéntica
        Vector3 currentScale = transform.localScale;
        
        // Preservamos la escala Y y Z original, solo modificamos X con valor absoluto
        float newXScale = Mathf.Abs(currentScale.x) * _currentFacingDirection;
        
        transform.localScale = new Vector3(newXScale, currentScale.y, currentScale.z);
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