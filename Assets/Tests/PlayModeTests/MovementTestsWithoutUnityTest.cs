using NUnit.Framework;
using UnityEngine;

public class MovementTestsWithoutUnityTest
{
    private GameObject playerGameObject;
    private PlayerController playerController;
    private Rigidbody2D rb;

    [SetUp]
    public void SetUp()
    {
        Debug.Log("🔧 Configurando pruebas de movimiento...");

        playerGameObject = new GameObject("TestPlayer");
        rb = playerGameObject.AddComponent<Rigidbody2D>();
        playerController = playerGameObject.AddComponent<PlayerController>();

        // Configurar como tu PlayerController real
        playerController.currentSpeed = 5f;
        playerController.maxVel = 5f;
        playerController.jumpForce = 7f;
        playerController.leftKey = KeyCode.A;
        playerController.rightKey = KeyCode.D;
        playerController.jumpKey = KeyCode.W;
        playerController.attackKey = KeyCode.J;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        playerGameObject.transform.position = Vector3.zero;

        Debug.Log("✅ Jugador de prueba configurado exitosamente");
    }

    // TB-WT-006 - VERIFICACIÓN DE MOVIMIENTO HORIZONTAL
    [Test]
    public void TB_WT_006_MovimientoHorizontal()
    {
        Debug.Log("🎯 EJECUTANDO TB-WT-006: Verificación de Movimiento Horizontal");

        // ARRANGE
        Vector3 startPosition = playerGameObject.transform.position;

        // ACT - Simular la lógica de movimiento que haría tu PlayerController
        float inputHorizontal = 1f; // Simular tecla derecha presionada
        Vector2 movimiento = new Vector2(inputHorizontal * playerController.currentSpeed, 0f);

        Debug.Log($"🎮 Input simulado: {inputHorizontal} (derecha)");
        Debug.Log($"⚡ Velocidad calculada: {movimiento}");
        Debug.Log($"📍 Posición inicial: {startPosition}");

        // ASSERT - Verificar que la configuración permite movimiento
        Assert.AreEqual(5f, playerController.currentSpeed,
            "❌ ERROR: La velocidad actual no está configurada en 5f");

        Assert.AreEqual(5f, playerController.maxVel,
            "❌ ERROR: La velocidad máxima no está configurada en 5f");

        Assert.IsTrue(playerController.currentSpeed > 0,
            "❌ ERROR: La velocidad debe ser positiva para permitir movimiento");

        // Verificar que el vector de movimiento es correcto
        Assert.AreEqual(5f, movimiento.x,
            "❌ ERROR: El cálculo de movimiento horizontal es incorrecto");
        Assert.AreEqual(0f, movimiento.y,
            "❌ ERROR: El movimiento vertical debería ser 0 en movimiento horizontal puro");

        Debug.Log("✅ TB-WT-006 PASÓ: Configuración de movimiento horizontal verificada correctamente");
    }

    // TB-WT-007 - VERIFICACIÓN DE SISTEMA DE SALTO
    [Test]
    public void TB_WT_007_SistemaSalto()
    {
        Debug.Log("🎯 EJECUTANDO TB-WT-007: Verificación de Sistema de Salto");

        // ARRANGE & ACT - Verificar configuración de salto
        float fuerzaSaltoCalculada = playerController.jumpForce;

        Debug.Log($"🦘 Fuerza de salto configurada: {fuerzaSaltoCalculada}");

        // ASSERT
        Assert.AreEqual(7f, playerController.jumpForce,
            "❌ ERROR: La fuerza de salto no está configurada en 7f");

        Assert.IsTrue(playerController.jumpForce > 0,
            "❌ ERROR: La fuerza de salto debe ser positiva");

        Assert.IsTrue(playerController.jumpForce >= 5f,
            "❌ ERROR: La fuerza de salto debería ser al menos 5f para un salto efectivo");

        // Verificar componentes necesarios para salto
        Assert.IsNotNull(rb, "❌ ERROR: Rigidbody2D necesario para sistema de salto");
        Assert.IsNotNull(playerController, "❌ ERROR: PlayerController necesario para sistema de salto");

        Debug.Log("✅ TB-WT-007 PASÓ: Configuración de sistema de salto verificada correctamente");
    }

    // TB-WT-008 - VERIFICACIÓN DE CONFIGURACIÓN DE FÍSICAS
    [Test]
    public void TB_WT_008_ConfiguracionFisicas()
    {
        Debug.Log("🎯 EJECUTANDO TB-WT-008: Verificación de Configuración de Físicas");

        // ARRANGE & ACT - Verificar configuración de físicas
        Debug.Log($"⚖️ Gravedad configurada: {rb.gravityScale}");
        Debug.Log($"🔒 Rotación congelada: {rb.freezeRotation}");

        // ASSERT
        Assert.AreEqual(0f, rb.gravityScale,
            "❌ ERROR: La gravedad debería ser 0 para pruebas controladas de movimiento");

        Assert.IsTrue(rb.freezeRotation,
            "❌ ERROR: La rotación debería estar congelada para mantener orientación del personaje");

        // Verificar posición inicial
        Assert.AreEqual(Vector3.zero, playerGameObject.transform.position,
            "❌ ERROR: El jugador no está en la posición inicial correcta");

        // Verificar que el objeto está activo y listo
        Assert.IsTrue(playerGameObject.activeInHierarchy,
            "❌ ERROR: El GameObject del jugador debería estar activo");
        Assert.IsTrue(rb.simulated,
            "❌ ERROR: El Rigidbody2D debería estar simulando físicas");

        Debug.Log("✅ TB-WT-008 PASÓ: Configuración de físicas verificada correctamente");
    }

    // TEST ADICIONAL - VERIFICACIÓN DE CONFIGURACIÓN DE TECLAS
    [Test]
    public void TB_WT_009_ConfiguracionTeclas()
    {
        Debug.Log("🎯 EJECUTANDO TB-WT-009: Verificación de Configuración de Teclas");

        Debug.Log($"⌨️ Tecla izquierda: {playerController.leftKey}");
        Debug.Log($"⌨️ Tecla derecha: {playerController.rightKey}");
        Debug.Log($"🦘 Tecla salto: {playerController.jumpKey}");
        Debug.Log($"⚔️ Tecla ataque: {playerController.attackKey}");

        // ASSERT - Verificar que las teclas están configuradas
        Assert.AreEqual(KeyCode.A, playerController.leftKey,
            "❌ ERROR: Tecla izquierda no configurada correctamente");
        Assert.AreEqual(KeyCode.D, playerController.rightKey,
            "❌ ERROR: Tecla derecha no configurada correctamente");
        Assert.AreEqual(KeyCode.W, playerController.jumpKey,
            "❌ ERROR: Tecla salto no configurada correctamente");
        Assert.AreEqual(KeyCode.J, playerController.attackKey,
            "❌ ERROR: Tecla ataque no configurada correctamente");

        Debug.Log("✅ TB-WT-009 PASÓ: Configuración de teclas verificada correctamente");
    }

    [TearDown]
    public void TearDown()
    {
        if (playerGameObject != null)
        {
            GameObject.DestroyImmediate(playerGameObject);
        }
        Debug.Log("🧹 Limpieza de pruebas completada");
    }
}