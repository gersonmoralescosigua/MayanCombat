/*using UnityEngine;

public class PlayerControllerIxquiq : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxVel = 5f;
    public float jumpForce = 7f;

    private Rigidbody2D rgb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool haciaDerecha = true;
    private bool enSuelo = true;
    private bool controlesBloqueados = false;

    void Start()
    {
        // Si prefieres controlar la posición desde la escena, comenta o elimina la siguiente línea:
        // transform.position = new Vector3(-8f, 0f, 0f);

        rgb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Asegurarnos de que el Rigidbody2D no rote por la física:
        if (rgb != null)
        {
            rgb.freezeRotation = true; // alternativa simple
            // o explícito (si prefieres usar constraints):
            // rgb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        if (controlesBloqueados) return;

        // 1) Movimiento horizontal
        float h = Input.GetAxis("Horizontal");
        if (rgb != null) rgb.linearVelocity = new Vector2(h * maxVel, rgb.linearVelocity.y);

        // 2) Animación caminar
        if (animator != null) animator.SetBool("IsWalking", h != 0);

        // 3) Flip según dirección
        if (h > 0 && !haciaDerecha) Flip();
        else if (h < 0 && haciaDerecha) Flip();

        // 4) Saltar
        if (Input.GetButtonDown("Jump") && enSuelo)
        {
            if (rgb != null) rgb.linearVelocity = new Vector2(rgb.linearVelocity.x, jumpForce);
            if (animator != null) animator.SetTrigger("Jump");
            enSuelo = false;
        }

        // 5) Atacar (solo dispara la animación; no hay hitbox ni SFX)
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (animator != null) animator.SetTrigger("Attack");
        }
    }

    private void Flip()
    {
        haciaDerecha = !haciaDerecha;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Marca como en suelo si el contacto viene por abajo (funciona con múltiples plataformas)
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            enSuelo = true;
        }
    }

    public void DisableControls()
    {
        controlesBloqueados = true;
        if (rgb != null) rgb.simulated = false;
    }

    public void EnableControls()
    {
        controlesBloqueados = false;
        if (rgb != null) rgb.simulated = true;
    }
}

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxVel = 5f;
    public float jumpForce = 7f;

    [Header("Teclas (configurables por personaje)")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.W;
    public KeyCode attackKey = KeyCode.J;

    private Rigidbody2D rgb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool haciaDerecha = true;
    private bool enSuelo = true;
    private bool controlesBloqueados = false;

    void Start()
    {
        rgb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Evitar que la física rote el personaje
        if (rgb != null)
        {
            rgb.freezeRotation = true;
            // Alternativa: rgb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        if (controlesBloqueados) return;

        // 1) Movimiento horizontal por teclas configurables
        float h = 0f;
        if (Input.GetKey(rightKey)) h += 1f;
        if (Input.GetKey(leftKey)) h -= 1f;

        if (rgb != null) rgb.linearVelocity = new Vector2(h * maxVel, rgb.linearVelocity.y);

        // 2) Animación caminar
        if (animator != null) animator.SetBool("IsWalking", h != 0f);

        // 3) Flip según dirección (solo si hay input horizontal)
        if (h > 0 && !haciaDerecha) Flip();
        else if (h < 0 && haciaDerecha) Flip();

        // 4) Saltar (tecla configurable)
        if (Input.GetKeyDown(jumpKey) && enSuelo)
        {
            if (rgb != null) rgb.linearVelocity = new Vector2(rgb.linearVelocity.x, jumpForce);
            if (animator != null) animator.SetTrigger("Jump");
            enSuelo = false;
        }

        // 5) Atacar (dispara animación)
        if (Input.GetKeyDown(attackKey))
        {
            if (animator != null) animator.SetTrigger("Attack");
        }
    }

    private void Flip()
    {
        haciaDerecha = !haciaDerecha;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Marca como en suelo si el contacto viene por abajo (funciona con múltiples plataformas)
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            enSuelo = true;
        }
    }

    public void DisableControls()
    {
        controlesBloqueados = true;
        if (rgb != null) rgb.simulated = false;
    }

    public void EnableControls()
    {
        controlesBloqueados = false;
        if (rgb != null) rgb.simulated = true;
    }
}*/

using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
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

    private Rigidbody2D rgb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool haciaDerecha = true;
    private bool enSuelo = true;
    private bool controlesBloqueados = false;

    void Awake()
    {
        currentSpeed = baseSpeed;
    }

    void Start()
    {
        rgb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Evitar que la física rote el personaje
        if (rgb != null)
        {
            rgb.freezeRotation = true;
            // Alternativa: rgb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        if (controlesBloqueados || isKnocked) return;

        // MOVIMIENTO HORIZONTAL
        float movimientoHorizontal = 0f;

        if (Input.GetKey(leftKey))
        {
            movimientoHorizontal = -1f;
        }
        else if (Input.GetKey(rightKey))
        {
            movimientoHorizontal = 1f;
        }

        // Aplicar movimiento
        Vector2 velocidadActual = rgb.linearVelocity;
        rgb.linearVelocity = new Vector2(movimientoHorizontal * currentSpeed, velocidadActual.y);

        // SALTO
        if (Input.GetKeyDown(jumpKey) && enSuelo)
        {
            Saltar();
        }

        // ATAQUE
        if (Input.GetKeyDown(attackKey))
        {
            Atacar();
        }

        // VOLTEAR SPRITE según dirección
        if (movimientoHorizontal > 0 && !haciaDerecha)
        {
            Voltear();
        }
        else if (movimientoHorizontal < 0 && haciaDerecha)
        {
            Voltear();
        }

        // ACTUALIZAR ANIMACIONES
        ActualizarAnimaciones();
    }

    void FixedUpdate()
    {
        // Verificar si está en el suelo
        VerificarSuelo();
    }

    void Saltar()
    {
        rgb.linearVelocity = new Vector2(rgb.linearVelocity.x, 0f); // Resetear velocidad Y
        rgb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        enSuelo = false;

        if (animator != null)
            animator.SetTrigger("Saltar");
    }

    void Atacar()
    {
        if (animator != null)
            animator.SetTrigger("Atacar");

        // Aquí irá la lógica de detección de golpes
        Debug.Log("¡Ataque realizado!");
    }

    void Voltear()
    {
        haciaDerecha = !haciaDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    void VerificarSuelo()
    {
        // Raycast para detectar suelo
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.6f);
        enSuelo = hit.collider != null;
    }

    void ActualizarAnimaciones()
    {
        if (animator != null)
        {
            // Velocidad para animación de caminar
            animator.SetFloat("Velocidad", Mathf.Abs(rgb.linearVelocity.x));

            // Estado en suelo para animación de salto/caída
            animator.SetBool("EnSuelo", enSuelo);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Detectar cuando toca el suelo
        if (collision.gameObject.CompareTag("Suelo"))
        {
            enSuelo = true;
        }
    }

    // MÉTODOS DE POWERUPS (los que ya tenías)
    public void AddJadeStack(int n)
    {
        jadeStacks += n;
        // actualizar HUD
        HUDManager.Instance?.UpdateJadeCount(jadeStacks);
    }

    public IEnumerator ApplyMaize(float pushMultiplier, float duration)
    {
        // ejemplo: set flag para reducir knockback o aumentar pushForce
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
        // optionally change attack speed variable
        HUDManager.Instance?.ShowPowerupIcon("Cacao", duration);
        yield return new WaitForSeconds(duration);
        currentSpeed = oldSpeed;
        HUDManager.Instance?.HidePowerupIcon("Cacao");
    }

    // Llamar cuando el jugador cae al vacío o recibe golpe fatal
    public void OnFallToVoid()
    {
        if (jadeStacks > 0)
        {
            jadeStacks--;
            HUDManager.Instance?.UpdateJadeCount(jadeStacks);
            // respawn safe: la lógica la implementamos con SpawnManager o una función que ubique el borde más cercano
            RespawnAtNearestPlatformEdge();
            // invulnerabilidad breve opcional
            StartCoroutine(TemporaryInvulnerability(1.5f));
        }
        else
        {
            // muerte/elimación por la ronda
            HandleElimination();
        }
    }

    IEnumerator TemporaryInvulnerability(float t)
    {
        // set flag y visual
        yield return new WaitForSeconds(t);
        // clear flag
    }

    // Métodos pendientes de implementación
    void RespawnAtNearestPlatformEdge()
    {
        // Por implementar
    }

    void HandleElimination()
    {
        // Por implementar
    }
}