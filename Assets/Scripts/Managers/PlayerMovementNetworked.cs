using UnityEngine;
using Fusion;

/// <summary>
/// Movimiento en 2D usando Rigidbody2D local + NetworkTransform para sincronizar transform.
/// - El cliente con InputAuthority controla su Rigidbody2D (simulated = true).
/// - Los remotos quedan con Rigidbody2D.simulated = false y la posición viene por NetworkTransform.
/// - Exporta estados networked para animador.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerMovementNetworked : NetworkBehaviour
{
    [Header("Stats")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float airControl = 0.8f;

    [Header("Tweak")]
    public float accel = 40f;

    Rigidbody2D rb;

    // Networked states para animaciones / lectura remota
    [Networked] public float NetSpeed { get; set; }
    [Networked] public float NetVertical { get; set; }
    [Networked] public NetworkBool NetGrounded { get; set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    public override void FixedUpdateNetwork()
    {
        // si no hay runner aún, salir (protección)
        if (Runner == null) return;

        // Asegura que solo el cliente con authority simule física localmente
        if (Object.HasInputAuthority)
        {
            // aseguramos que el rigidbody esté activo para este cliente
            rb.simulated = true;

            // obtener input del runner (lo envía NetworkRunnerHandler.OnInput)
            NetworkInputData input;
            if (GetInput(out input))
            {
                // horizontal movimiento
                float targetX = input.move.x * moveSpeed;
                float curX = rb.linearVelocity.x;
                float newX = Mathf.MoveTowards(curX, targetX, accel * Runner.DeltaTime);
                rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

                // salto (simple)
                if (input.jumpPressed && NetGrounded)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                    NetGrounded = false;
                }

                // actualizar networked para animador
                NetSpeed = Mathf.Abs(rb.linearVelocity.x);
                NetVertical = rb.linearVelocity.y;
                // NetGrounded se establece desde colisiones también
            }
        }
        else
        {
            // remotos: no simulamos física en remotos (dejar tras NetworkTransform)
            rb.simulated = false;
            // aún podemos leer NetSpeed/NetVertical desde network para animaciones via PlayerAnimatorNetwork
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!Object.HasInputAuthority) return; // sólo owner actualiza grounded
        foreach (var c in col.contacts)
        {
            if (c.normal.y > 0.5f)
            {
                NetGrounded = true;
                break;
            }
        }
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (!Object.HasInputAuthority) return;
        foreach (var c in col.contacts)
        {
            if (c.normal.y > 0.5f)
            {
                NetGrounded = true;
                return;
            }
        }
        NetGrounded = false;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (!Object.HasInputAuthority) return;
        NetGrounded = false;
    }
}