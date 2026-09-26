using UnityEngine;

/// <summary>
/// 플레이어를 화면 중앙에서 추적하는 2D 카메라.
/// - 맵(공방) 타일 범위를 벗어나 검은 여백이 보이지 않도록 Clamp
/// - 컷씬 진행 중에는 추적을 멈추고 지정된 위치(예: 침대)에 고정
/// 해상도 1920x1080(16:9), Pixel Perfect는 별도의 Pixel Perfect Camera 컴포넌트와 함께 사용하세요.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField] private Transform target; // 플레이어 Transform

    [Header("맵 경계 (World 좌표 기준, BoxCollider2D 등으로 대체 가능)")]
    [SerializeField] private Vector2 mapMin;
    [SerializeField] private Vector2 mapMax;

    [Header("카메라 설정")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float cameraHalfHeight = 5.4f; // Camera가 없을 때의 예비값
    [SerializeField] private float cameraHalfWidth = 9.6f;  // Camera가 없을 때의 예비값

    private Vector3 velocity = Vector3.zero;
    private bool isFixed = false;
    private Vector3 fixedPosition;
    private Camera cachedCamera;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (isFixed)
        {
            // 컷씬 중: 추적하지 않고 고정 위치 유지
            transform.position = fixedPosition;
            return;
        }

        if (target == null) return;

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 clamped = ClampToMapBounds(desiredPosition);

        transform.position = Vector3.SmoothDamp(transform.position, clamped, ref velocity, smoothTime);
    }

    /// <summary>맵 경계를 벗어나지 않도록 카메라 목표 위치를 보정합니다.</summary>
    private Vector3 ClampToMapBounds(Vector3 desired)
    {
        GetCameraHalfExtents(out float halfWidth, out float halfHeight);
        float minX = mapMin.x + halfWidth;
        float maxX = mapMax.x - halfWidth;
        float minY = mapMin.y + halfHeight;
        float maxY = mapMax.y - halfHeight;

        // 맵이 카메라보다 작을 경우(예외) 중앙값으로 고정
        float x = (minX <= maxX) ? Mathf.Clamp(desired.x, minX, maxX) : (mapMin.x + mapMax.x) * 0.5f;
        float y = (minY <= maxY) ? Mathf.Clamp(desired.y, minY, maxY) : (mapMin.y + mapMax.y) * 0.5f;

        return new Vector3(x, y, desired.z);
    }

    private void GetCameraHalfExtents(out float halfWidth, out float halfHeight)
    {
        // orthographicSize나 화면 비율이 바뀌어도 Clamp 범위가 자동으로 맞습니다.
        if (cachedCamera != null && cachedCamera.orthographic)
        {
            halfHeight = cachedCamera.orthographicSize;
            halfWidth = halfHeight * cachedCamera.aspect;
            return;
        }

        halfWidth = cameraHalfWidth;
        halfHeight = cameraHalfHeight;
    }

    /// <summary>오프닝/기상 컷씬 등에서 카메라를 특정 위치(예: 침대)에 고정합니다.</summary>
    public void SetFixedPosition(Vector3 worldPosition)
    {
        isFixed = true;
        fixedPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
    }

    /// <summary>고정을 해제하고 다시 플레이어를 추적합니다.</summary>
    public void ReleaseFixedPosition()
    {
        isFixed = false;
        velocity = Vector3.zero;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
