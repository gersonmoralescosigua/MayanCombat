using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
public class PlayerNetwork : NetworkBehaviour
{
    [Networked] public int CharacterId { get; set; }

    public void SetCharacterId_Server(int id)
    {
        if (Object.HasStateAuthority)
            CharacterId = id;
    }
}
