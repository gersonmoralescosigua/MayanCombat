// Assets/Scripts/Gameplay/PlayerAnimatorNetwork.cs
using UnityEngine;
using Fusion;

/// <summary>
/// Sincroniza el Animator usando las variables networked del PlayerMovementNetworked.
/// Funciona en todos los clientes; lee NetVelocity / NetGrounded del movement.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimatorNetwork : NetworkBehaviour
{
    public Animator animator;
    private PlayerMovementNetworked movement;

    // Hashes EXACTOS (ajusta si tu Animator usa otros nombres)
    private readonly int hSpeed = Animator.StringToHash("Speed");
    private readonly int hVertical = Animator.StringToHash("Vertical");
    private readonly int hGrounded = Animator.StringToHash("Grounded");
    private readonly int hAttack = Animator.StringToHash("Attack"); // si usas trigger de attack

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        movement = GetComponent<PlayerMovementNetworked>();
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null || movement == null) return;

        // Leemos las propiedades networked del movement
        Vector2 netVel = movement.NetVelocity;    // Vector2 (x = horizontal speed, y = vertical)
        bool grounded = movement.NetGrounded;

        float speed = Mathf.Abs(netVel.x);
        float vertical = netVel.y;

        animator.SetFloat(hSpeed, speed);
        animator.SetFloat(hVertical, vertical);
        animator.SetBool(hGrounded, grounded);
    }

    // Llamado localmente para reproducir ataque (opcional)
    public void PlayAttack()
    {
        animator?.SetTrigger(hAttack);
    }
}