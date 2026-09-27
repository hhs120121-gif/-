using UnityEngine;

/// <summary>
/// 게임 전역 상태를 관리하는 싱글턴.
/// 컷씬 재생 중 플레이어 조작 잠금, 상호작용 중 이동 잠금 등
/// "예외 상황 및 비정상 행동 처리" 요구사항의 기반이 되는 클래스입니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("상태 플래그")]
    [SerializeField] private bool isCutscenePlaying = false;
    [SerializeField] private bool isInteracting = false;

    public bool IsCutscenePlaying => isCutscenePlaying;
    public bool IsInteracting => isInteracting;

    /// <summary>
    /// 플레이어 조작(이동/인벤토리 등)이 가능한 상태인지 여부.
    /// 컷씬 재생 중이거나 상호작용 UI가 열려 있으면 false.
    /// </summary>
    public bool CanControlPlayer => !isCutscenePlaying && !isInteracting;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        // 씬 전환이나 테스트 중 오브젝트가 제거된 뒤에도 이전 인스턴스가
        // 남아 조작을 막는 문제를 방지합니다.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>컷씬 시작. 카메라 추적/플레이어 조작이 비활성화됩니다.</summary>
    public void StartCutscene()
    {
        isCutscenePlaying = true;
    }

    /// <summary>컷씬 종료. 카메라 추적/플레이어 조작이 다시 활성화됩니다.</summary>
    public void EndCutscene()
    {
        isCutscenePlaying = false;
    }

    /// <summary>상호작용(대화창, 제작 UI 등) 시작. 이동이 잠깁니다.</summary>
    public void StartInteraction()
    {
        isInteracting = true;
    }

    /// <summary>상호작용 종료.</summary>
    public void EndInteraction()
    {
        isInteracting = false;
    }

    /// <summary>튜토리얼을 처음부터 다시 시작할 때 상태 플래그를 초기화합니다.</summary>
    public void ResetState()
    {
        isCutscenePlaying = false;
        isInteracting = false;
    }
}
