using UnityEngine;
using Fusion;

/// <summary>
/// Sincroniza animaciones usando variables networked del PlayerMovementNetworked.
/// Funciona en todos los clientes.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimatorNetwork : NetworkBehaviour
{
    public Animator animator;
    private PlayerMovementNetworked movement;

    // Hashes EXACTOS para tu Animator
    private int hSpeed = Animator.StringToHash("Speed");
    private int hVertical = Animator.StringToHash("Vertical");
    private int hGrounded = Animator.StringToHash("Grounded");
    private int hAttack = Animator.StringToHash("Attack");

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        movement = GetComponent<PlayerMovementNetworked>();
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null || movement == null)
            return;

        animator.SetFloat(hSpeed, movement.NetSpeed);
        animator.SetFloat(hVertical, movement.NetVertical);
        animator.SetBool(hGrounded, movement.NetGrounded);
    }

    // Llamado desde input local al presionar J
    public void PlayAttack()
    {
        if (animator != null)
            animator.SetTrigger(hAttack);
    }
}