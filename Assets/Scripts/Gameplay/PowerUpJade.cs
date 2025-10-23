using UnityEngine;

public class PowerUpJade : PowerUpBase
{
    void Reset() { powerUpName = "Jade"; duration = 0f; } // no duraci�n tradicional

    public override void ApplyTo(PlayerController player)
    {
        player.AddJadeStack(1);
        // HUD se actualizar� v�a evento o m�todo
    }
}
