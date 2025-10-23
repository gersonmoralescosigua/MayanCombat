using UnityEngine;

public class PowerUpJade : PowerUpBase
{
    void Reset() { powerUpName = "Jade"; duration = 0f; } // no duración tradicional

    public override void ApplyTo(PlayerController player)
    {
        player.AddJadeStack(1);
        // HUD se actualizará vía evento o método
    }
}
