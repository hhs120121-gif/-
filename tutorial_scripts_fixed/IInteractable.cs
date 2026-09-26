/// <summary>
/// 의뢰서, 철제 선반, 제작대 등 상호작용 가능한 모든 오브젝트가 구현하는 인터페이스.
/// </summary>
public interface IInteractable
{
    /// <summary>플레이어가 상호작용 키(Z/Enter)를 눌렀을 때 호출됩니다.</summary>
    void OnInteract();

    /// <summary>현재 이 오브젝트와 상호작용이 가능한 상태인지 (예: 인벤토리 만석, 재료 부족 등).</summary>
    bool CanInteract();
}
