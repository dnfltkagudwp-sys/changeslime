using UnityEngine;

/// <summary>
/// 슈퍼마리오 식 카메라: 플레이어가 화면 중앙 근처의 "데드존" 안에 있을 때는 카메라가 가만히 있다가,
/// 데드존 경계를 벗어나려고 하면 그만큼만 부드럽게 따라와서 플레이어를 다시 데드존 안에 둠.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("따라갈 대상")]
    [SerializeField] private Transform target; // 보통 Player

    [Header("데드존 (카메라가 안 움직이는 중앙 여유 구간, 가로/세로 전체 크기, 월드 유닛)")]
    [SerializeField] private Vector2 deadZoneSize = new Vector2(3f, 2f);

    [Header("따라오는 부드러움 (작을수록 빠르게 따라붙음)")]
    [SerializeField] private float smoothTime = 0.2f;

    [Header("맵 경계 (선택, 카메라가 맵 바깥을 비추지 않도록 제한)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    private Vector3 currentVelocity = Vector3.zero; // SmoothDamp 내부 계산용

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 camPos = transform.position;
        Vector3 targetPos = target.position;

        float halfWidth = deadZoneSize.x * 0.5f;
        float halfHeight = deadZoneSize.y * 0.5f;

        // 데드존을 벗어난 만큼만 카메라의 목표 위치를 밀어줌
        float desiredX = camPos.x;
        float dx = targetPos.x - camPos.x;
        if (dx > halfWidth) desiredX = targetPos.x - halfWidth;
        else if (dx < -halfWidth) desiredX = targetPos.x + halfWidth;

        float desiredY = camPos.y;
        float dy = targetPos.y - camPos.y;
        if (dy > halfHeight) desiredY = targetPos.y - halfHeight;
        else if (dy < -halfHeight) desiredY = targetPos.y + halfHeight;

        Vector3 desiredPos = new Vector3(desiredX, desiredY, camPos.z);

        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, minBounds.x, maxBounds.x);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, smoothTime);
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 데드존 범위를 눈으로 확인하기 위한 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneSize.x, deadZoneSize.y, 0f));
    }
}
