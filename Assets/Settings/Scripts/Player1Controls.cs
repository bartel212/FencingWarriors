using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player1Controls : MonoBehaviour
{
    public InputActionReference player1Left;
    public InputActionReference player1Right;
    public InputActionReference player1Sprint;
    public InputActionReference player1Attack;
    public InputActionReference player1Parry;
    public InputActionReference player1Lunge;
    public InputActionReference player1Feint;

    public Vector2 startPos;
    public float moveSpeed = 1.5f;
    public float sprintSpeed = 3f;
    public float lungeSpeed = 8f;
    public float lungeDuration = 0.15f;

    public GameObject p1Hitbox;
    public GameObject p2;

    public AudioSource footstepSource;
    public AudioSource sfxSource;
    public AudioClip walkSound;
    public AudioClip sprintSound;
    public AudioClip attackSound;
    public AudioClip lungeSound;
    public float walkStepInterval = 0.4f;
    public float sprintStepInterval = 0.25f;

    [SerializeField] private float arenaLeftEdge = -6.8f;
    [SerializeField] private float arenaRightEdge = 6.8f;
    [SerializeField] private float minimumSpacing = 1.15f;
    [SerializeField] private float pokeWindup = 0.18f;
    [SerializeField] private float pokeActiveTime = 0.08f;
    [SerializeField] private float pokeRecovery = 0.14f;
    [SerializeField] private Vector2 pokeHitboxSize = new Vector2(1.45f, 0.42f);
    [SerializeField] private float pokeHitboxForwardOffset = 0.78f;
    [SerializeField] private float pokeHitboxVerticalOffset = 0.12f;
    [SerializeField] private Vector2 lungeHitboxSize = new Vector2(1.85f, 0.48f);
    [SerializeField] private float lungeHitboxForwardOffset = 1.02f;
    [SerializeField] private Vector2 feintHitboxSize = new Vector2(2f, 0.56f);
    [SerializeField] private float feintHitboxForwardOffset = 1.05f;
    [SerializeField] private float feintWindup = 0.1f;
    [SerializeField] private float feintActiveTime = 0.22f;
    [SerializeField] private float lungeReturnDuration = 0.15f;
    [SerializeField] private float feintDuration = 0.6f;
    [SerializeField] private float parryStartup = 0.04f;
    [SerializeField] private float parryActiveTime = 0.2f;
    [SerializeField] private float parryRecovery = 0.14f;
    [SerializeField] private float stunDuration = 0.4f;

    private const float PixelSnapFactor = 100f;

    private bool isAttacking;
    private bool isParrying;
    private bool isMobile;
    private SpriteRenderer sr;
    private Animator animator;
    private Coroutine attackCoroutine;
    private Coroutine parryCoroutine;
    private Coroutine footstepCoroutine;
    private bool lastSprinting;
    private int pokeCount;
    private int lungeCount;
    private int parryCount;

    public int PokeCount => pokeCount;
    public int LungeCount => lungeCount;
    public int ParryCount => parryCount;

    void Awake()
    {
        if (p1Hitbox != null)
            p1Hitbox.SetActive(false);
    }

    void Start()
    {
        EnableActions();

        isAttacking = false;
        isParrying = false;
        isMobile = true;
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (footstepSource != null)
            footstepSource.playOnAwake = false;

        if (sfxSource != null)
            sfxSource.playOnAwake = false;

        Restart();
    }

    void OnDisable()
    {
        DisableActions();
    }

    void Update()
    {
        if (UIController.Instance != null && UIController.Instance.IsRoundLocked)
        {
            UpdateAnimator(false, false, false);
            StopFootsteps();
            return;
        }

        bool movingLeft = player1Left.action.IsPressed() && isMobile;
        bool movingRight = player1Right.action.IsPressed() && isMobile;
        bool sprinting = player1Sprint.action.IsPressed() && isMobile && (movingLeft || movingRight);

        UpdateAnimator(movingLeft, movingRight, sprinting);

        float direction = 0f;
        if (movingLeft)
            direction -= 1f;
        if (movingRight)
            direction += 1f;

        if (Mathf.Abs(direction) > 0f)
        {
            float speed = sprinting ? sprintSpeed : moveSpeed;
            MoveHorizontally(direction * speed * Time.deltaTime);
        }

        HandleFootsteps(Mathf.Abs(direction) > 0f, sprinting);

        if (player1Attack.action.WasPressedThisFrame() && CanStartOffensiveAction())
        {
            pokeCount++;
            SetAttackAnimation();
            attackCoroutine = StartCoroutine(Attack());
        }

        if (player1Parry.action.WasPressedThisFrame() && CanStartDefensiveAction())
        {
            parryCount++;
            SetParryAnimation();
            parryCoroutine = StartCoroutine(Parry());
        }

        if (player1Lunge.action.WasPressedThisFrame() && CanStartOffensiveAction())
        {
            lungeCount++;
            SetLungeAnimation();
            attackCoroutine = StartCoroutine(Lunge());
        }

        if (player1Feint.action.WasPressedThisFrame() && CanStartOffensiveAction())
        {
            SetFeintAnimation();
            attackCoroutine = StartCoroutine(Feint());
        }

        SnapToPixelGrid();
    }

    public void ReceiveHitFromPlayer2(Player2Controls attacker)
    {
        if (attacker == null || UIController.Instance == null || UIController.Instance.IsRoundLocked)
            return;

        if (isParrying)
        {
            StartCoroutine(attacker.Stun());
            return;
        }

        UIController.Instance.ResolveRound(2, this, attacker);
    }

    public void Restart()
    {
        ResetState(Color.white, true);
        transform.position = startPos;
        SnapToPixelGrid();
    }

    public void PrepareForRoundReset()
    {
        ResetState(Color.white, true);
    }

    public IEnumerator Stun()
    {
        ResetState(Color.red, false);
        yield return new WaitForSeconds(stunDuration);
        sr.color = Color.white;
        isMobile = true;
        PlayIdleAnimation();
        SnapToPixelGrid();
    }

    private void EnableActions()
    {
        Enable(player1Left);
        Enable(player1Right);
        Enable(player1Sprint);
        Enable(player1Attack);
        Enable(player1Parry);
        Enable(player1Lunge);
        Enable(player1Feint);
    }

    private void DisableActions()
    {
        Disable(player1Left);
        Disable(player1Right);
        Disable(player1Sprint);
        Disable(player1Attack);
        Disable(player1Parry);
        Disable(player1Lunge);
        Disable(player1Feint);
    }

    private static void Enable(InputActionReference actionReference)
    {
        actionReference?.action?.Enable();
    }

    private static void Disable(InputActionReference actionReference)
    {
        actionReference?.action?.Disable();
    }

    private bool CanStartOffensiveAction()
    {
        return isMobile && !isAttacking && !isParrying;
    }

    private bool CanStartDefensiveAction()
    {
        return isMobile && !isAttacking && !isParrying;
    }

    private void MoveHorizontally(float delta)
    {
        Vector3 current = transform.position;
        float nextX = current.x + delta;
        float rightLimit = arenaRightEdge;

        if (p2 != null)
            rightLimit = Mathf.Min(rightLimit, p2.transform.position.x - minimumSpacing);

        current.x = Mathf.Clamp(nextX, arenaLeftEdge, rightLimit);
        transform.position = current;
    }

    private void MoveTowardX(float targetX, float maxDelta)
    {
        Vector3 current = transform.position;
        float rightLimit = arenaRightEdge;

        if (p2 != null)
            rightLimit = Mathf.Min(rightLimit, p2.transform.position.x - minimumSpacing);

        current.x = Mathf.Clamp(Mathf.MoveTowards(current.x, targetX, maxDelta), arenaLeftEdge, rightLimit);
        transform.position = current;
    }

    private void HandleFootsteps(bool moving, bool sprinting)
    {
        if (footstepSource == null)
            return;

        if (moving)
        {
            if (footstepCoroutine == null || lastSprinting != sprinting)
            {
                if (footstepCoroutine != null)
                    StopCoroutine(footstepCoroutine);

                lastSprinting = sprinting;
                footstepCoroutine = StartCoroutine(FootstepLoop(sprinting));
            }
        }
        else
        {
            StopFootsteps();
        }
    }

    private void StopFootsteps()
    {
        if (footstepCoroutine != null)
        {
            StopCoroutine(footstepCoroutine);
            footstepCoroutine = null;
        }

        if (footstepSource != null)
            footstepSource.Stop();
    }

    private IEnumerator FootstepLoop(bool sprinting)
    {
        while (true)
        {
            AudioClip clip = sprinting ? sprintSound : walkSound;
            float interval = sprinting ? sprintStepInterval : walkStepInterval;
            float vol = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;

            if (clip != null)
                footstepSource.PlayOneShot(clip, vol);

            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator Attack()
    {
        BeginAction();
        PlaySfx(attackSound);
        ConfigureAttackHitbox(pokeHitboxSize, pokeHitboxForwardOffset, pokeHitboxVerticalOffset);

        yield return new WaitForSeconds(pokeWindup);
        SetHitboxActive(true);

        yield return new WaitForSeconds(pokeActiveTime);
        SetHitboxActive(false);

        yield return new WaitForSeconds(pokeRecovery);
        EndAction();
    }

    private IEnumerator Lunge()
    {
        BeginAction();
        PlaySfx(lungeSound);
        float startX = transform.position.x;
        ConfigureAttackHitbox(lungeHitboxSize, lungeHitboxForwardOffset);

        yield return new WaitForSeconds(0.08f);
        SetHitboxActive(true);

        float elapsed = 0f;
        while (elapsed < lungeDuration)
        {
            MoveHorizontally(lungeSpeed * Time.deltaTime);
            SnapToPixelGrid();
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetHitboxActive(false);

        elapsed = 0f;
        while (elapsed < lungeReturnDuration)
        {
            MoveTowardX(startX, lungeSpeed * Time.deltaTime);
            SnapToPixelGrid();
            elapsed += Time.deltaTime;
            yield return null;
        }

        EndAction();
    }

    private IEnumerator Feint()
    {
        BeginAction();
        PlaySfx(attackSound);
        ConfigureAttackHitbox(feintHitboxSize, feintHitboxForwardOffset);

        yield return new WaitForSeconds(feintWindup);
        SetHitboxActive(true);

        yield return new WaitForSeconds(feintActiveTime);
        SetHitboxActive(false);

        float remainingDuration = Mathf.Max(0f, feintDuration - feintWindup - feintActiveTime);
        yield return new WaitForSeconds(remainingDuration);
        EndAction();
    }

    private IEnumerator Parry()
    {
        StopFootsteps();
        isMobile = false;

        yield return new WaitForSeconds(parryStartup);
        isParrying = true;

        if (sr != null)
            sr.color = Color.blue;

        yield return new WaitForSeconds(parryActiveTime);
        isParrying = false;

        if (sr != null)
            sr.color = Color.white;

        yield return new WaitForSeconds(parryRecovery);
        isMobile = true;
        parryCoroutine = null;
        PlayIdleAnimation();
    }

    private void BeginAction()
    {
        isAttacking = true;
        isMobile = false;
        StopFootsteps();
    }

    private void EndAction()
    {
        SetHitboxActive(false);
        isAttacking = false;
        isMobile = true;
        attackCoroutine = null;
        PlayIdleAnimation();
    }

    private void ResetState(Color color, bool mobileAfterReset)
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (parryCoroutine != null)
        {
            StopCoroutine(parryCoroutine);
            parryCoroutine = null;
        }

        StopFootsteps();
        SetHitboxActive(false);
        isAttacking = false;
        isParrying = false;
        isMobile = mobileAfterReset;

        if (sr != null)
            sr.color = color;

        PlayIdleAnimation();
    }

    private void SetHitboxActive(bool active)
    {
        if (p1Hitbox == null)
            return;

        p1Hitbox.SetActive(active);

        if (active)
            p1Hitbox.GetComponent<Player1Attack>()?.CheckCurrentOverlap();
    }

    private void ConfigureAttackHitbox(Vector2 size, float forwardOffset, float verticalOffset = 0f)
    {
        if (p1Hitbox == null)
            return;

        p1Hitbox.transform.localPosition = new Vector3(forwardOffset, verticalOffset, 0f);
        p1Hitbox.transform.localScale = Vector3.one;

        BoxCollider2D hitboxCollider = p1Hitbox.GetComponent<BoxCollider2D>();
        if (hitboxCollider != null)
        {
            hitboxCollider.offset = Vector2.zero;
            hitboxCollider.size = size;
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        float vol = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        sfxSource.PlayOneShot(clip, vol);
    }

    private void UpdateAnimator(bool movingLeft, bool movingRight, bool sprinting)
    {
        if (animator == null)
            return;

        animator.SetBool("moving_backward", movingLeft);
        animator.SetBool("moving_forward", movingRight);
        animator.SetBool("sprint", sprinting);
    }

    private void SetAttackAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("parry");
        animator.SetTrigger("attack");
    }

    private void SetLungeAnimation()
    {
        PlayActionAnimation("p1_lunge");
    }

    private void SetFeintAnimation()
    {
        PlayActionAnimation("p1_feint");
    }

    private void SetParryAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("attack");
        animator.SetTrigger("parry");
    }

    private void PlayActionAnimation(string stateName)
    {
        if (animator == null)
            return;

        animator.ResetTrigger("attack");
        animator.ResetTrigger("parry");
        animator.Play(stateName, 0, 0f);
    }

    private void PlayIdleAnimation()
    {
        UpdateAnimator(false, false, false);

        if (animator != null)
            animator.Play("p1_idle", 0, 0f);
    }

    private void SnapToPixelGrid()
    {
        Vector3 position = transform.position;
        position.x = Mathf.Round(position.x * PixelSnapFactor) / PixelSnapFactor;
        position.y = Mathf.Round(position.y * PixelSnapFactor) / PixelSnapFactor;
        transform.position = position;
    }
}
