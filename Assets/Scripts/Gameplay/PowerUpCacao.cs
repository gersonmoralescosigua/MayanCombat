using UnityEngine;

public class PowerUpCacao : PowerUpBase
{
    public float speedMultiplier = 1.5f;
    public float attackSpeedMultiplier = 1.5f;
    public float cacaoDuration = 5f;

    void Reset() { powerUpName = "Cacao"; duration = cacaoDuration; }

    public override void ApplyTo(PlayerController player)
    {
        player.StartCoroutine(player.ApplyCacao(speedMultiplier, attackSpeedMultiplier, duration));
    }
}
