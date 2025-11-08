using UnityEngine;
using Fusion;
using System.Collections;

[RequireComponent(typeof(NetworkObject))]
public class PlayerNetwork : NetworkBehaviour
{
    [Networked] public int CharacterId { get; set; } = -1;

    // Ejemplo de estados networked simples (visibles para todos)
    [Networked] public NetworkBool HasMaize { get; set; }
    [Networked] public NetworkBool HasCacao { get; set; }
    [Networked] public int JadeStacks { get; set; }

    // --------------------
    // Métodos que llama el host cuando detecta pickup
    // Host/StateAuthority ejecuta el RPC y el cliente objetivo aplica efectos locales
    // --------------------

    // Llamar desde el host para aplicar Maize (empuje aumentado) en el cliente objetivo
    public void ApplyMaize_Server(PlayerRef target, float multiplier, float duration)
    {
        if (!Object.HasStateAuthority) return;
        RPC_ApplyMaize(target, multiplier, duration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ApplyMaize(PlayerRef target, float multiplier, float duration, RpcInfo info = default)
    {
        // ejecuta solo en el cliente que tiene input authority (propietario del objeto)
        if (!Object.HasInputAuthority) return;
        StartCoroutine(ApplyMaizeCoroutine(multiplier, duration));
    }

    IEnumerator ApplyMaizeCoroutine(float multiplier, float duration)
    {
        HasMaize = true;
        // ejemplo: comunica a PlayerMovementNetworked via HUD/flags si es necesario
        yield return new WaitForSeconds(duration);
        HasMaize = false;
    }

    // Cacao: velocidad temporal
    public void ApplyCacao_Server(PlayerRef target, float speedMult, float attackMult, float duration)
    {
        if (!Object.HasStateAuthority) return;
        RPC_ApplyCacao(target, speedMult, attackMult, duration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ApplyCacao(PlayerRef target, float speedMult, float attackMult, float duration, RpcInfo info = default)
    {
        if (!Object.HasInputAuthority) return;
        StartCoroutine(ApplyCacaoCoroutine(speedMult, attackMult, duration));
    }

    IEnumerator ApplyCacaoCoroutine(float speedMult, float attackMult, float duration)
    {
        HasCacao = true;
        // el cambio de velocidad puede aplicarse en el PlayerMovementNetworked leyendo HasCacao
        yield return new WaitForSeconds(duration);
        HasCacao = false;
    }

    // Jade: stacks
    public void AddJadeStack_Server(PlayerRef target, int amount)
    {
        if (!Object.HasStateAuthority) return;
        RPC_AddJade(target, amount);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_AddJade(PlayerRef target, int amount, RpcInfo info = default)
    {
        if (!Object.HasInputAuthority) return;
        JadeStacks += amount;
        // puedes actualizar HUD local aquí
    }

    // Métodos auxiliares para corutinas de efectos si quieres exponerlos
    // (por ahora los dejamos simples)
}