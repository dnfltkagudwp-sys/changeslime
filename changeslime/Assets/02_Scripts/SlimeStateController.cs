using UnityEngine;

public enum SlimeState
{
    Liquid, // 기본 액체: 표준 이동 및 점프
    Solid,  // 고체(얼음): 무거움, 점프 불가, 미끄러짐
    Gas     // 기체(증기): 중력 반전, 부유 비행
}

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeStateController : MonoBehaviour
{
    [Header("현재 상태")]
    [SerializeField] private SlimeState currentState = SlimeState.Liquid;

    [Header("연결 컴포넌트")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private SlimeMovement movementScript;

    [Header("상태별 시각 색상 (아래 스프라이트가 비어있을 때의 대체용)")]
    [SerializeField] private Color liquidColor = new Color(0.2f, 0.5f, 0.95f, 1f); // 물 느낌의 진한 파랑
    [SerializeField] private Color solidColor = new Color(0.6f, 0.85f, 1f, 1f);    // 얼음 느낌의 옅은 하늘색
    [SerializeField] private Color gasColor = new Color(0.4f, 0.9f, 0.5f, 0.5f);   // 반투명 초록빛 증기

    [Header("상태별 전용 이미지 (비워두면 위 색상으로 대체)")]
    [SerializeField] private Sprite liquidSprite;
    [SerializeField] private Sprite solidSprite;
    [SerializeField] private Sprite gasSprite;

    [Header("상태별 이동/부유 튜닝")]
    [SerializeField] private float liquidMoveSpeedMultiplier = 0.8f; // 액체 상태 이동 속도 배율 (살짝 느려짐)
    [SerializeField] private float solidMoveSpeedMultiplier = 0.25f; // 고체 상태 이동 속도 배율 (아주 느리게 움직임, 점프는 불가)
    [SerializeField] private float gasFloatSpeed = 1.5f;             // 기체 상태로 전환 시 위로 떠오르는 등속 속도

    private Rigidbody2D rb;

    public SlimeState CurrentState => currentState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (movementScript == null)
            movementScript = GetComponent<SlimeMovement>();

        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // 시작 상태 적용
        ChangeState(currentState);
    }

    /// <summary>
    /// 외부 환경 트리거와 닿았을 때 호출되는 상태 강제 변환 메서드
    /// </summary>
    public void ChangeState(SlimeState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case SlimeState.Liquid:
                ApplyLiquidState();
                break;
            case SlimeState.Solid:
                ApplySolidState();
                break;
            case SlimeState.Gas:
                ApplyGasState();
                break;
        }
    }

    private void ApplyLiquidState()
    {
        rb.gravityScale = 3f;       // 기본 중력
        rb.mass = 1f;               // 기본 질량
        movementScript.enabled = true;
        movementScript.SetStateModifiers(liquidMoveSpeedMultiplier, true); // 살짝 느려진 이동, 점프 가능
        movementScript.SetSquashStyle(1f, false); // 말랑말랑한 액체 느낌 (기존 스쿼시&스트레치 그대로)
        movementScript.SetVerticalVelocityOverride(false, 0f); // 중력에 의한 정상 낙하로 복귀
        SetVisual(liquidSprite, liquidColor);
    }

    private void ApplySolidState()
    {
        rb.gravityScale = 5f;       // 강한 중력으로 급강하
        rb.mass = 5f;               // 스위치 압박용 무거운 질량
        movementScript.enabled = true;
        movementScript.SetStateModifiers(solidMoveSpeedMultiplier, false); // 아주 느리게만 이동, 점프 불가
        movementScript.SetSquashStyle(0.1f, false); // 거의 변형되지 않아 단단한 느낌
        movementScript.SetVerticalVelocityOverride(false, 0f); // 중력에 의한 정상 낙하로 복귀
        SetVisual(solidSprite, solidColor);
    }

    private void ApplyGasState()
    {
        rb.gravityScale = 0f;       // 중력 제거: 등속으로 서서히 떠오르게 함 (음수 중력은 계속 가속되어 하늘로 치솟는 문제가 있었음)
        rb.mass = 0.2f;             // 가벼운 질량
        movementScript.enabled = true;
        movementScript.SetStateModifiers(1f, true); // 좌우 이동은 정상 속도 유지
        movementScript.SetSquashStyle(0f, true); // 둥실둥실 떠다니는 펄스 연출로 전환
        movementScript.SetVerticalVelocityOverride(true, gasFloatSpeed); // 매 프레임 계속 상승 속도를 강제 유지 (중간에 멈추지 않도록)
        SetVisual(gasSprite, gasColor);
    }

    /// <summary>
    /// 상태별 전용 이미지가 있으면 그 이미지로 교체(원래 색 그대로 보이도록 흰색 처리)하고,
    /// 없으면 기존 플레이스홀더 모양에 색만 입혀서 대체함.
    /// </summary>
    private void SetVisual(Sprite targetSprite, Color fallbackColor)
    {
        if (visualRenderer == null) return;

        if (targetSprite != null)
        {
            visualRenderer.sprite = targetSprite;
            visualRenderer.color = Color.white;
        }
        else
        {
            visualRenderer.color = fallbackColor;
        }

        // 스프라이트가 바뀌었으니, 발밑이 콜라이더 바닥과 다시 맞도록 보정
        if (movementScript != null)
            movementScript.SnapVisualToGroundBounds();
    }
}