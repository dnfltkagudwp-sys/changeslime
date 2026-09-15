using UnityEngine;

/// <summary>
/// 배경 스프라이트(Tiled 모드)를 카메라가 실제로 비추는 영역에 맞춰 매 프레임 늘리고 위치를 맞춤.
/// 맵 크기가 아니라 "카메라가 지금 보는 범위" 기준이라, 플레이어가 맵 가장자리에 붙어서
/// 카메라가 맵 경계 밖을 보여주는 경우나 전체 맵 보기(Tab)로 줌아웃한 경우까지 전부 커버됨.
/// parallaxFactor를 1보다 살짝 낮게 주면 배경이 카메라보다 조금 느리게 따라가서 원근감이 생김
/// (1 = 화면에 완전히 고정, 0 = 배경이 전혀 안 움직임).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera; // 비워두면 Camera.main 사용
    [SerializeField] private float padding = 2f; // 카메라가 보는 범위보다 여유를 두는 정도(유닛)
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.9f; // 1이면 기존처럼 완전 고정, 낮을수록 배경이 더 느리게 따라옴

    private SpriteRenderer sr;
    private Vector3 referenceCamPos;
    private bool hasReference;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.orthographic) return;

        Vector3 camPos = targetCamera.transform.position;

        if (!hasReference)
        {
            referenceCamPos = camPos;
            hasReference = true;
        }

        // 카메라가 기준점에서 움직인 만큼의 일부(parallaxFactor)만 배경에 반영함
        Vector3 delta = camPos - referenceCamPos;
        Vector3 lag = delta * (1f - parallaxFactor); // 배경이 카메라를 못 따라간 만큼(뒤처짐)
        Vector3 bgPos = referenceCamPos + delta * parallaxFactor;
        transform.position = new Vector3(bgPos.x, bgPos.y, transform.position.z);

        // 뒤처진 만큼 가장자리가 비지 않도록 사이즈를 그만큼 더 여유 있게 키움
        float viewHeight = targetCamera.orthographicSize * 2f + padding * 2f + Mathf.Abs(lag.y) * 2f;
        float viewWidth = viewHeight * targetCamera.aspect + Mathf.Abs(lag.x) * 2f;
        sr.size = new Vector2(viewWidth, viewHeight);
    }
}
