using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
public class PlayerNetwork : NetworkBehaviour
{
    [Networked] public int CharacterId { get; private set; }   // networked
    [Networked] public int Team { get; private set; } = -1;    // optionally store team

    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;

    // asigna sprites en inspector (index 0 => IXQUIC, 1 => BEATRIZ, etc)
    public Sprite[] availableSprites;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Si ya hay CharacterId definido por host, aplica
        ApplyCharacterSprite(CharacterId);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 move = new Vector3(data.direction.x, 0, data.direction.y);
            transform.position += move * moveSpeed * Runner.DeltaTime;

            if (data.jump && rb != null && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    // Este m�todo es llamado por el host justo despu�s de spawnear al jugador
    public void SetCharacterId_Server(int id)
    {
        if (!Runner.IsServer) return;
        CharacterId = id;
    }

    public override void Render()
    {
        base.Render();
        // Cuando CharacterId cambia (networked), aseg�rate de actualizar sprite
        ApplyCharacterSprite(CharacterId);
    }

    private void ApplyCharacterSprite(int id)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;

        if (availableSprites != null && id >= 0 && id < availableSprites.Length)
        {
            spriteRenderer.sprite = availableSprites[id];
        }
    }
}
