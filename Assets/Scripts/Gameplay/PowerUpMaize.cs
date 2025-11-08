using UnityEngine;

public class PowerUpMaize : PowerUpBase
{
    public float pushMultiplier = 1.5f;
    public float resistanceDuration = 5f;

    void Reset() { powerUpName = "Maiz"; duration = resistanceDuration; }

    public override void ApplyTo(PlayerNetwork player)
    {
        // Invoca la rutina a través del host (server) -> PlayerNetwork.ApplyMaize_Server
        // Si este código corre en el host, llama ApplyMaize_Server; si no, debe hacerse por host.
        if (player.Object.HasStateAuthority)
        {
            player.ApplyMaize_Server(player.Runner.LocalPlayer, pushMultiplier, duration);
        }
        else
        {
            // Si no estamos en authority (raro), intentar buscar el runner/host para que lo ejecute.
            // Normalmente pickups están en host.
            Debug.LogWarning("[PowerUpMaize] ApplyTo ejecutado en cliente no-host. Asegura que pickups spawnen en host.");
        }
    }
}
