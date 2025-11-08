using UnityEngine;
using Fusion;

/// <summary>
/// Pickup minimal: cuando Host detecta colisión con Player (Networked), notifica al PlayerNetwork.
/// Este script debe estar en el prefab del pickup que el spawner instancia (host authoritative).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    public PickupType type;
    public float respawnTime = 0f;
    [System.NonSerialized] public Transform spawnPointUsed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // solo host debería ejecutar lógica (si el pickup fue spawn por Runner en host)
        // pero OnTrigger puede ejecutarse en cliente; la lógica de authority se comprueba abajo.
        var pn = other.GetComponent<PlayerNetwork>();
        if (pn == null) return;

        // Ejecutar sólo si la instancia local del pickup tiene StateAuthority (host)
        var netObj = pn.Object;
        // Si este GameObject tiene NetworkObject (es pickup), comprobamos su authority:
        var thisNet = GetComponent<NetworkObject>();
        if (thisNet != null && !thisNet.HasStateAuthority)
        {
            // no somos host; abortar. (el host está a cargo)
            return;
        }

        // En host: llamar al PlayerNetwork para que aplique
        switch (type)
        {
            case PickupType.Maize:
                pn.ApplyMaize_Server(netObj.InputAuthority, 1.5f, 5f); // ejemplo
                break;
            case PickupType.Jade:
                pn.AddJadeStack_Server(netObj.InputAuthority, 1);
                break;
            case PickupType.Cacao:
                pn.ApplyCacao_Server(netObj.InputAuthority, 1.5f, 1.2f, 5f);
                break;
                // otros casos...
        }

        // si hay spawner, notificar
        var spawner = FindObjectOfType<PickupsSpawner>();
        if (spawner != null)
        {
            // Pass NetworkObject if you want to despawn via runner
            var obj = GetComponent<NetworkObject>();
            spawner.OnPickupCollected(obj, spawnPointUsed);
        }

        // desactivar / despawn
        if (thisNet != null && thisNet.HasStateAuthority)
        {
            // si spawned por runner, despawn por runner o destruir aquí
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}