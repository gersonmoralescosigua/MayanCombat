using System.Collections;
using UnityEngine;

public class PlayerController1 : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxVel = 5f;
    public float jumpForce = 7f;

    [Header("Teclas (configurables por personaje)")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.W;
    public KeyCode attackKey = KeyCode.J;

    [Header("Estados y Powerups")]
    public int jadeStacks = 0;
    public float baseSpeed = 5f;
    public float currentSpeed;
    public float basePushForce = 5f;
    public bool isKnocked = false;

    // Ventajas
    public bool hasJade = false;

    // Desventajas
    public bool canMove = true;
    public bool isConfused = false;
    public bool isSlowed = false;

    private Rigidbody2D rgb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool haciaDerecha = true;
    private bool enSuelo = true;
    private bool controlesBloqueados = false;

    void Awake() { currentSpeed = baseSpeed; }

    void Start()
    {
        rgb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // ✅ SOLUCIÓN: Fuerza el estado inicial correcto
        enSuelo = true;
        if (animator != null)
        {
            animator.SetBool("EnSuelo", true);
            animator.SetFloat("Velocidad", 0f);
            animator.ResetTrigger("Saltar");
            animator.ResetTrigger("Atacar");
        }

        if (rgb != null) rgb.freezeRotation = true;

        // ✅ Verificación extra después de un frame
        StartCoroutine(VerificarSueloInicial());
    }

    IEnumerator VerificarSueloInicial()
    {
        yield return new WaitForSeconds(0.1f);
        VerificarSuelo();
        if (animator != null)
        {
            animator.SetBool("EnSuelo", enSuelo);
        }
    }

    void Update()
    {
        if (controlesBloqueados || isKnocked || !canMove) return;

        float movimientoHorizontal = 0f;
        if (Input.GetKey(leftKey)) movimientoHorizontal = isConfused ? 1f : -1f;
        else if (Input.GetKey(rightKey)) movimientoHorizontal = isConfused ? -1f : 1f;

        Vector2 velocidadActual = rgb.linearVelocity;
        rgb.linearVelocity = new Vector2(movimientoHorizontal * currentSpeed, velocidadActual.y);

        if (Input.GetKeyDown(jumpKey) && enSuelo) Saltar();
        if (Input.GetKeyDown(attackKey)) Atacar();

        if (movimientoHorizontal > 0 && !haciaDerecha) Voltear();
        else if (movimientoHorizontal < 0 && haciaDerecha) Voltear();

        ActualizarAnimaciones();
    }

    void FixedUpdate() { VerificarSuelo(); }

    void Saltar()
    {
        rgb.linearVelocity = new Vector2(rgb.linearVelocity.x, 0f);
        rgb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        enSuelo = false;
        animator?.SetTrigger("Saltar");
    }

    void Atacar() { animator?.SetTrigger("Atacar"); }

    void Voltear()
    {
        haciaDerecha = !haciaDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    void VerificarSuelo()
    {
        // ✅ Mejor detección con LayerMask
        LayerMask sueloMask = LayerMask.GetMask("Suelo", "Default");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, sueloMask);
        enSuelo = hit.collider != null;
    }

    void ActualizarAnimaciones()
    {
        if (animator == null) return;
        animator.SetFloat("Velocidad", Mathf.Abs(rgb.linearVelocity.x));
        animator.SetBool("EnSuelo", enSuelo);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Suelo"))
        {
            enSuelo = true;
            // ✅ Actualiza inmediatamente el Animator
            if (animator != null)
                animator.SetBool("EnSuelo", true);
        }
    }

    // ---- Interacción con pickups ----
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pickup"))
        {
            Pickup pickup = other.GetComponent<Pickup>();
            if (pickup != null)
            {
                CollectPickup(pickup.type);
                pickup.gameObject.SetActive(false);
            }
        }
    }

    public void CollectPickup(PickupType type)
    {
        switch (type)
        {
            case PickupType.Maize:
                StartCoroutine(ApplyMaize(1.5f, 5f));
                break;
            case PickupType.Jade:
                hasJade = true;
                break;
            case PickupType.Cacao:
                StartCoroutine(ApplyCacao(3.5f, 3.5f, 10f));
                break;
            case PickupType.Jaguar:
                StartCoroutine(ApplyStun(8f));
                break;
            case PickupType.Lava:
                StartCoroutine(ApplySlow(10f));
                break;
            case PickupType.Serpiente:
                StartCoroutine(ApplyConfusion(10f));
                break;
        }
    }

    // ---- Nuevos efectos ----
    IEnumerator ApplyStun(float duration)
    {
        canMove = false;
        yield return new WaitForSeconds(duration);
        canMove = true;
    }

    IEnumerator ApplySlow(float duration)
    {
        float oldSpeed = currentSpeed;
        currentSpeed *= 0.7f;
        isSlowed = true;
        yield return new WaitForSeconds(duration);
        currentSpeed = oldSpeed;
        isSlowed = false;
    }

    IEnumerator ApplyConfusion(float duration)
    {
        isConfused = true;
        yield return new WaitForSeconds(duration);
        isConfused = false;
    }

    // ---- Métodos ya existentes de powerups ----
    public void AddJadeStack(int n)
    {
        jadeStacks += n;
        HUDManager.Instance?.UpdateJadeCount(jadeStacks);
    }

    public IEnumerator ApplyMaize(float pushMultiplier, float duration)
    {
        float originalPush = basePushForce;
        basePushForce *= pushMultiplier;
        HUDManager.Instance?.ShowPowerupIcon("Maiz", duration);
        yield return new WaitForSeconds(duration);
        basePushForce = originalPush;
        HUDManager.Instance?.HidePowerupIcon("Maiz");
    }

    public IEnumerator ApplyCacao(float speedMul, float attackMul, float duration)
    {
        float oldSpeed = currentSpeed;
        currentSpeed *= speedMul;
        HUDManager.Instance?.ShowPowerupIcon("Cacao", duration);
        yield return new WaitForSeconds(duration);
        currentSpeed = oldSpeed;
        HUDManager.Instance?.HidePowerupIcon("Cacao");
    }

    // ---- Fall to void ----
    public void OnFallToVoid()
    {
        if (jadeStacks > 0)
        {
            jadeStacks--;
            HUDManager.Instance?.UpdateJadeCount(jadeStacks);
            RespawnAtNearestPlatformEdge();
            StartCoroutine(TemporaryInvulnerability(1.5f));
        }
        else HandleElimination();
    }

    IEnumerator TemporaryInvulnerability(float t) { yield return new WaitForSeconds(t); }

    void RespawnAtNearestPlatformEdge() { }
    void HandleElimination() { }
}