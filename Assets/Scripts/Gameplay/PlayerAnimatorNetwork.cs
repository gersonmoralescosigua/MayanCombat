using UnityEngine;
using Fusion;

[DisallowMultipleComponent]
public class PlayerAnimatorNetwork : NetworkBehaviour
{
    public Animator animator;
    private PlayerMovementNetworked movement;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        movement = GetComponent<PlayerMovementNetworked>();
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null || movement == null) return;

        // USA LOS NOMBRES EXACTOS DE TUS PARÁMETROS
        animator.SetFloat("Velocidad", movement.NetSpeed);           // ← Velocidad horizontal
        animator.SetBool("EnSuelo", movement.NetGrounded);          // ← Si está en el suelo
        animator.SetBool("IsWalking", movement.NetSpeed > 0.1f);    // ← Si está caminando
    }

    // llamado localmente cuando presionen J (opcional)
    public void PlayAttack()
    {
        animator?.SetTrigger("Atacar");
    }
    
    public void PlayJump()
    {
        animator?.SetTrigger("Saltar");
    }
}

/*// Assets/Scripts/Gameplay/PlayerAnimatorNetwork.cs
using UnityEngine;
using Fusion;

[DisallowMultipleComponent]
public class PlayerAnimatorNetwork : NetworkBehaviour
{
    public Animator animator;
    private PlayerMovementNetworked movement;

    private int hSpeed = Animator.StringToHash("Speed");
    private int hVertical = Animator.StringToHash("Vertical");
    private int hGrounded = Animator.StringToHash("Grounded");
    private int hAttack = Animator.StringToHash("Attack");

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        movement = GetComponent<PlayerMovementNetworked>();
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null || movement == null) return;

        animator.SetFloat(hSpeed, movement.NetSpeed);
        animator.SetFloat(hVertical, movement.NetVertical);
        animator.SetBool(hGrounded, movement.NetGrounded);
    }

    // llamado localmente cuando presionen J (opcional)
    public void PlayAttack()
    {
        animator?.SetTrigger(hAttack);
    }
}*/