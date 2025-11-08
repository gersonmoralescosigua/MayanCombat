using UnityEngine;

public class PowerUpJade : PowerUpBase
{
    void Reset() { powerUpName = "Jade"; duration = 0f; }

    public override void ApplyTo(PlayerNetwork player)
    {
        if (player.Object.HasStateAuthority)
        {
            player.AddJadeStack_Server(player.Runner.LocalPlayer, 1);
        }
        else
        {
            Debug.LogWarning("[PowerUpJade] ApplyTo ejecutado en cliente no-host.");
        }
    }
}
