using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 move;   // X para izquierda/derecha
    public NetworkBool jumpPressed;
    public NetworkBool attackPressed;
}
