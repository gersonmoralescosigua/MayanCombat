using UnityEngine;
using Fusion;

public class PlayerNetwork : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    private Rigidbody rb;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 2, -4);
            Camera.main.transform.localRotation = Quaternion.identity;
        }

        rb = GetComponent<Rigidbody>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 move = new Vector3(data.direction.x, 0, data.direction.y);
            transform.position += move * moveSpeed * Runner.DeltaTime;

            if (data.jump && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
