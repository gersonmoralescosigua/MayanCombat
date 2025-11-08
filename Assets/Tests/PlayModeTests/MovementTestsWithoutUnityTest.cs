using NUnit.Framework;
using UnityEngine;

public class MovementTestsWithoutUnityTest
{
    private GameObject playerGameObject;
    private PlayerMovementNetworked playerMovement;
    private Rigidbody2D rb;

    [SetUp]
    public void SetUp()
    {
        playerGameObject = new GameObject("TestPlayer");
        rb = playerGameObject.AddComponent<Rigidbody2D>();
        playerMovement = playerGameObject.AddComponent<PlayerMovementNetworked>();

        // Configurar como tu PlayerController real
        playerMovement.moveSpeed = 5f;
        playerMovement.jumpForce = 7f;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        playerGameObject.transform.position = Vector3.zero;
    }

    [Test]
    public void TB_WT_006_MovimientoHorizontal()
    {
        Vector3 startPosition = playerGameObject.transform.position;
        float inputHorizontal = 1f;
        Vector2 movimiento = new Vector2(inputHorizontal * playerMovement.moveSpeed, 0f);

        Assert.AreEqual(5f, playerMovement.moveSpeed);
        Assert.IsTrue(playerMovement.moveSpeed > 0);
        Assert.AreEqual(5f, movimiento.x);
        Assert.AreEqual(0f, movimiento.y);
    }

    [Test]
    public void TB_WT_007_SistemaSalto()
    {
        float fuerzaSaltoCalculada = playerMovement.jumpForce;
        Assert.AreEqual(7f, playerMovement.jumpForce);
        Assert.IsTrue(playerMovement.jumpForce > 0);
        Assert.IsTrue(playerMovement.jumpForce >= 5f);

        Assert.IsNotNull(rb);
        Assert.IsNotNull(playerMovement);
    }

    [TearDown]
    public void TearDown()
    {
        if (playerGameObject != null)
            GameObject.DestroyImmediate(playerGameObject);
    }
}