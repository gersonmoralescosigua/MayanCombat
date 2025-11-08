using UnityEngine;
using Fusion;

/// <summary>
/// Lee propiedades networked del PlayerMovementNetworked y actualiza el Animator.
/// - Ejecuta en todos los clientes; usa las variables networked para sincronizar animaciones.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimatorNetwork : NetworkBehaviour
{
    public Animator animator;
    PlayerMovementNetworked movement;
    private int hSpeed = Animator.StringToHash("Speed");
    private int hGrounded = Animator.StringToHash("Grounded");
    private int hVertical = Animator.StringToHash("Vertical");

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        movement = GetComponent<PlayerMovementNetworked>();
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null || movement == null) return;

        // leer props networked (estos están sincronizados por movement)
        float speed = movement.NetSpeed;
        float vert = movement.NetVertical;
        bool grounded = movement.NetGrounded;

        animator.SetFloat(hSpeed, speed);
        animator.SetFloat(hVertical, vert);
        animator.SetBool(hGrounded, grounded);
    }

    // trigger de ataque local (por ejemplo)
    public void PlayAttack()
    {
        animator?.SetTrigger("Attack");
    }
}
