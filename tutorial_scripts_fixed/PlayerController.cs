using UnityEngine;

/// <summary>
/// 2D 탑뷰 플레이어 이동 컨트롤러.
/// - 4방향 기본 이동 + 대각선 이동(속도 정규화)
/// - 걷기 5.0 / 달리기 8.0
/// - 이동 중 상호작용 불가, 컷씬/상호작용 중 조작 불가
/// - 마지막으로 바라본 방향(FacingDirection)을 외부(InteractionDetector)에 제공
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 속도")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float runSpeed = 8.0f;

    [Header("참조")]
    [SerializeField] private Animator animator; // 대기/이동 모션 전환용 (선택)

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down; // 기본값: 아래를 바라봄

    /// <summary>현재 이동 중인지 여부. 상호작용 판정(3.1 제한 사항)에 사용됩니다.</summary>
    public bool IsMoving { get; private set; }

    /// <summary>플레이어가 마지막으로 바라본 방향 (상하좌우 단위벡터).</summary>
    public Vector2 FacingDirection => facingDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // 탑뷰이므로 중력 제거
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // 컷씬 재생 중이거나 상호작용(UI) 중에는 입력을 받지 않음
        if (GameManager.Instance != null && !GameManager.Instance.CanControlPlayer)
        {
            moveInput = Vector2.zero;
            IsMoving = false;
            rb.velocity = Vector2.zero;
            UpdateAnimator();
            return;
        }

        ReadInput();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanControlPlayer)
        {
            return;
        }

        // Input Manager에 Run 축이 아직 등록되지 않은 초기 씬에서도
        // Shift 키로 달리기가 동작하도록 직접 키 입력을 함께 지원합니다.
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                         || Input.GetKey(KeyCode.X);
        float speed = isRunning ? runSpeed : walkSpeed;

        Vector2 nextPosition = rb.position + moveInput * speed * Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);
    }

    private void ReadInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(x, y);

        // 대각선 이동 시 속도 정규화 (3.1 요구사항)
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        IsMoving = moveInput.sqrMagnitude > 0.0001f;

        if (IsMoving)
        {
            facingDirection = SnapToFourDirections(moveInput);
        }

        UpdateAnimator();
    }

    /// <summary>
    /// 입력 벡터를 상/하/좌/우 중 가장 가까운 방향으로 스냅합니다.
    /// (대각선 이동은 허용하되, "바라보는 방향"은 상호작용 판정을 위해 4방향으로 단순화)
    /// </summary>
    private Vector2 SnapToFourDirections(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return input.x > 0 ? Vector2.right : Vector2.left;
        }
        return input.y > 0 ? Vector2.up : Vector2.down;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool("IsMoving", IsMoving);
        animator.SetFloat("FacingX", facingDirection.x);
        animator.SetFloat("FacingY", facingDirection.y);
    }
}
