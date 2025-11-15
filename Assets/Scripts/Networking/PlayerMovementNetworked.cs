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

    private Rigidbody2D rb;
    private NetworkTransform networkTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        networkTransform = GetComponent<NetworkTransform>();
        
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rb != null) 
        {
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    public override void Spawned()
    {
        NetSpeed = 0f;
        NetVertical = 0f;
        NetGrounded = true;
        NetFacingDirection = 1;

        // CONFIGURACIÓN SIMPLIFICADA - compatible con tu versión de Fusion
        if (networkTransform != null)
        {
            // En versiones más recientes de Fusion, la interpolación se configura automáticamente
            // No necesitamos configurar manualmente InterpolationDataSource
        }
    }

    public override void FixedUpdateNetwork()
    {
        // SOLO el dueño procesa input y aplica fuerzas
        if (GetInput(out NetworkInputData data))
        {
            ProcessMovement(data);
            ProcessJump(data);
            ProcessAttack(data);
            UpdateFacingDirection(data);
        }

        // TODOS actualizan propiedades y animaciones
        UpdateNetworkedProperties();
        ApplyFacingDirection();
        UpdateAnimations();
    }

    private void ProcessMovement(NetworkInputData data)
    {
        if (!HasInputAuthority) return;

        Vector2 desiredVelocity = new Vector2(data.Move.x * moveSpeed, rb.linearVelocity.y);
        
        // Suavizado mejorado
        float blend = 0.5f; // Valor balanceado para buen rendimiento
        float newVelX = Mathf.Lerp(rb.linearVelocity.x, desiredVelocity.x, blend);
        
        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
    }

    private void ProcessJump(NetworkInputData data)
    {
        if (!HasInputAuthority) return;

        if (data.Jump && NetGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            
            if (animator != null)
                animator.SetTrigger("Saltar");
        }
    }

    private void ProcessAttack(NetworkInputData data)
    {
        if (!HasInputAuthority) return;

        if (data.Attack)
        {
            NetAttacking = true;
            if (animator != null)
                animator.SetTrigger("Atacar");
                    
            Vector2 dir = NetFacingDirection > 0 ? Vector2.right : Vector2.left;
            rb.AddForce(dir * attackForce, ForceMode2D.Impulse);
        }
        else
        {
            NetAttacking = false;
        }
    }

    private void UpdateFacingDirection(NetworkInputData data)
    {
        if (!HasInputAuthority) return;

        if (data.Move.x > 0.1f)
        {
            NetFacingDirection = 1;
        }
        else if (data.Move.x < -0.1f)
        {
            NetFacingDirection = -1;
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
        Vector3 newScale = transform.localScale;
        
        if (NetFacingDirection > 0)
        {
            newScale.x = Mathf.Abs(newScale.x);
        }
        else if (NetFacingDirection < 0)
        {
            newScale.x = -Mathf.Abs(newScale.x);
        }
        
        transform.localScale = newScale;
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

    // Método importante para suavizar objetos remotos
    public override void Render()
    {
        // Fusion automáticamente interpola los Networked properties entre FixedUpdateNetwork
        // Este método se ejecuta en cada frame de renderizado, permitiendo animaciones suaves
        
        // Para objetos remotos, podemos agregar suavizado adicional visual si es necesario
        if (!HasInputAuthority)
        {
            // Esto asegura que las animaciones se actualicen suavemente para otros jugadores
            UpdateAnimations();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}