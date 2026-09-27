using UnityEngine;

/// <summary>
/// 플레이어가 바라보는 방향으로 1타일(Grid) 이내의 IInteractable 오브젝트를 탐지하고,
/// Z/Enter 입력 시 상호작용을 실행합니다.
/// - 이동 중에는 상호작용 불가 (3.1 제한 사항)
/// - 컷씬/이미 상호작용 중일 때는 입력 무시
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class InteractionDetector : MonoBehaviour
{
    [Header("탐지 설정")]
    [SerializeField] private float tileSize = 1.0f; // 그리드 한 칸 크기 (프로젝트 타일 크기에 맞게 조정)
    [SerializeField] private float detectRadius = 0.4f; // 대상 오브젝트 판정 반경
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI 참조 (선택)")]
    [SerializeField] private GameObject interactionTooltip; // 화살표 마커 / 툴팁 오브젝트

    private PlayerController playerController;
    private IInteractable currentTarget;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanControlPlayer)
        {
            currentTarget = null;
            SetTooltip(null);
            return;
        }

        // 이동 중에는 상호작용 판정 자체를 하지 않음
        if (playerController.IsMoving)
        {
            currentTarget = null;
            SetTooltip(null);
            return;
        }

        DetectInteractable();

        // Input Manager 설정 누락으로 인한 예외를 피하기 위해 튜토리얼 기본 키를 직접 사용합니다.
        if (currentTarget != null && IsInteractPressed())
        {
            TryInteract(currentTarget);
        }
    }

    private static bool IsInteractPressed()
    {
        return Input.GetKeyDown(KeyCode.Z)
               || Input.GetKeyDown(KeyCode.Return)
               || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    private void DetectInteractable()
    {
        Vector2 checkPosition = (Vector2)transform.position + playerController.FacingDirection * tileSize;
        Collider2D hit = Physics2D.OverlapCircle(checkPosition, detectRadius, interactableLayer);

        // 상호작용 스크립트는 오브젝트 루트에, Collider는 자식에 둘 수 있으므로
        // 부모까지 탐색합니다.
        IInteractable found = hit != null ? hit.GetComponentInParent<IInteractable>() : null;

        if (found != currentTarget)
        {
            currentTarget = found;
            SetTooltip(currentTarget);
        }
    }

    private void TryInteract(IInteractable target)
    {
        if (!target.CanInteract())
        {
            // 예외 상황 처리(5장): 대사 출력은 각 오브젝트 구현부에서 담당
            return;
        }

        target.OnInteract();
    }

    private void SetTooltip(IInteractable target)
    {
        if (interactionTooltip == null) return;
        interactionTooltip.SetActive(target != null);
    }

    private void OnDrawGizmosSelected()
    {
        PlayerController controller = playerController != null ? playerController : GetComponent<PlayerController>();
        if (controller == null) return;
        Vector2 checkPosition = (Vector2)transform.position + controller.FacingDirection * tileSize;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPosition, detectRadius);
    }
}
