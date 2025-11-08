using UnityEngine;

public class PowerUpCacao : PowerUpBase
{
    public float speedMultiplier = 1.5f;
    public float attackSpeedMultiplier = 1.5f;
    public float cacaoDuration = 5f;

    void Reset() { powerUpName = "Cacao"; duration = cacaoDuration; }

    public override void ApplyTo(PlayerNetwork player)
    {
        if (player.Object.HasStateAuthority)
        {
            player.ApplyCacao_Server(player.Runner.LocalPlayer, speedMultiplier, attackSpeedMultiplier, duration);
        }
        else
        {
            Debug.LogWarning("[PowerUpCacao] ApplyTo ejecutado en cliente no-host.");
        }
    }
}
