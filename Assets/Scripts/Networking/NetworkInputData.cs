using Fusion;
using UnityEngine; // ¡ESTA LÍNEA FALTABA!

public struct NetworkInputData : INetworkInput
{
    public Vector2 Move;
    public NetworkBool JumpPressed;
    public NetworkBool AttackPressed;

    // helpers para lectura más cómoda
    public bool Jump => JumpPressed;
    public bool Attack => AttackPressed;
}

/*using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    // movimiento normalizado (-1..1) en X (y opcional Y)
    public Vector2 Move;
    // acciones
    public NetworkBool JumpPressed;
    public NetworkBool AttackPressed;

    // helpers para lectura m�s c�moda en scripts
    public bool Jump => JumpPressed;
    public bool Attack => AttackPressed;
}*/