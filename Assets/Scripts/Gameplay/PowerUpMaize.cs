using UnityEngine;

public class PowerUpMaize : PowerUpBase
{
    public float pushMultiplier = 1.5f;
    public float resistanceDuration = 5f;

    void Reset() { powerUpName = "Maiz"; duration = resistanceDuration; }

    public override void ApplyTo(PlayerController player)
    {
        player.StartCoroutine(player.ApplyMaize(pushMultiplier, duration));
    }
}
