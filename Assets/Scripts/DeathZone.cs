using UnityEngine;
using Fusion;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo el Servidor decide quien muere
        if (NetworkRunnerHandler.Instance == null || !NetworkRunnerHandler.Instance.Runner.IsServer) return;

        // Verificamos si lo que cayó es un jugador
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            Debug.Log($"💀 Jugador cayó al vacío: {other.name}");
            // Llamamos a la función en el Handler para terminar la partida
            NetworkRunnerHandler.Instance.OnPlayerFellToDeath(other.gameObject);
        }
    }
}