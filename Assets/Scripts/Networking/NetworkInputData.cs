using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    // movimiento normalizado (-1..1) en X (y opcional Y)
    public Vector2 Move;
    // acciones
    public NetworkBool JumpPressed;
    public NetworkBool AttackPressed;

    // helpers para lectura más cómoda en scripts
    public bool Jump => JumpPressed;
    public bool Attack => AttackPressed;
}