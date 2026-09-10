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

    [Header("상태별 시각 색상")]
    [SerializeField] private Color liquidColor = new Color(0.2f, 0.9f, 0.3f, 1f);  // 네온 연두
    [SerializeField] private Color solidColor = new Color(0.4f, 0.8f, 1f, 1f);   // 얼음 하늘색
    [SerializeField] private Color gasColor = new Color(0.9f, 0.9f, 1f, 0.5f);   // 반투명 증기

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
        SetVisual(liquidColor);
    }

    private void ApplySolidState()
    {
        rb.gravityScale = 5f;       // 강한 중력으로 급강하
        rb.mass = 5f;               // 스위치 압박용 무거운 질량
        // 얼어붙어 제자리 점프 불가가 되도록 이동 스크립트 비활성화 (물리 미끄러짐만 유지)
        movementScript.enabled = false;
        SetVisual(solidColor);
    }

    private void ApplyGasState()
    {
        rb.gravityScale = -1.5f;    // 중력 반전으로 위로 부유
        rb.mass = 0.2f;             // 가벼운 질량
        movementScript.enabled = true;
        SetVisual(gasColor);
    }

    private void SetVisual(Color targetColor)
    {
        if (visualRenderer != null)
        {
            visualRenderer.color = targetColor;
        }
    }
}