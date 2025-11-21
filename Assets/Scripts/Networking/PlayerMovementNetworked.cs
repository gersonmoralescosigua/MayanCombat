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
            // Esto debe estar en None para evitar peleas con Fusion (quita el temblor)
            rb.interpolation = RigidbodyInterpolation2D.None; 
            // Gravedad alta para caer rápido
            rb.gravityScale = 3f; 
        }
    }

    public override void Spawned()
    {
        NetGrounded = true;
        NetFacingDirection = 1;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid || Runner == null) return;

        NetGrounded = IsGrounded();

        if (GetInput(out NetworkInputData data))
        {
            // 1. MOVIMIENTO
            rb.linearVelocity = new Vector2(data.Move.x * moveSpeed, rb.linearVelocity.y);

            // 2. SALTO
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

            // 3. ATAQUE
            if (data.AttackPressed && !_wasAttackPressed)
            {
                if (animator != null) animator.SetTrigger("Atacar");
            }
            _wasAttackPressed = data.AttackPressed;

            // 4. DIRECCIÓN (Logica simple: 1 derecha, -1 izquierda)
            if (data.Move.x > 0.1f) NetFacingDirection = 1;
            else if (data.Move.x < -0.1f) NetFacingDirection = -1;
        }

        NetSpeed = Mathf.Abs(rb.linearVelocity.x);
    }

    public override void Render()
    {
        // --- CORRECCIÓN AQUÍ: ELIMINADO EL LERP QUE HACÍA DESAPARECER AL PERSONAJE ---
        // Simplemente asignamos la escala directamente. Si es -1, mira a la izquierda.
        if (NetFacingDirection != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * NetFacingDirection;
            transform.localScale = scale;
        }

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