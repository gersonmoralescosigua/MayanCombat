using Fusion;
using UnityEngine;

public class PlayerDataNetworked : NetworkBehaviour
{
    // 1. Quitamos el (OnChanged = ...) para evitar el error CS0246
    [Networked]
    public int TeamID { get; set; } = -1;

    [Networked]
    public int CharacterID { get; set; } = -1;

    // Variable local para recordar el último valor y detectar cambios
    private int _lastTeamID = -1;

    public override void Spawned()
    {
        // Al nacer, si ya tengo datos, los aplico
        if (Object.HasInputAuthority)
        {
            CheckForChanges();
        }
    }

    // Usamos Render (que corre cada frame) para vigilar si el dato cambió.
    // Esto reemplaza al sistema de "OnChanged" que te daba error.
    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            CheckForChanges();
        }
    }

    private void CheckForChanges()
    {
        // Si el valor en red es diferente al último que recuerdo...
        if (TeamID != _lastTeamID)
        {
            // ... significa que hubo una actualización.
            _lastTeamID = TeamID; // Actualizo mi memoria
            
            if (TeamID != -1)
            {
                // Aplico los datos al juego
                SessionManager.Instance?.SetTeam(TeamID);
                PlayerPrefs.SetInt("AssignedCharacter", CharacterID);
                Debug.Log($"✅ [Cliente] Datos recibidos y sincronizados: Team {TeamID}, Char {CharacterID}");
            }
        }
    }
}