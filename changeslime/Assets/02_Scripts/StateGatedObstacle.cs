using UnityEngine;

/// <summary>
/// 지정된 상태(액체/고체/기체)일 때만 통과할 수 있고, 그 외의 상태에서는 단단한 벽처럼 막는 장애물.
/// 예: 액체일 때만 통과되는 좁은 틈, 기체일 때만 통과되는 환풍구.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StateGatedObstacle : MonoBehaviour
{
    [Header("이 상태일 때만 통과 가능 (그 외 상태에서는 벽)")]
    [SerializeField] private SlimeState passableState;

    /// <summary>이 게이트를 통과할 수 있는 상태. HUD 가이드가 "이 게이트를 지나쳤는지" 판단하는 용도로 읽기 전용 참조.</summary>
    public SlimeState PassableState => passableState;

    [Header("플레이어 참조 (비워두면 자동으로 찾음)")]
    [SerializeField] private SlimeStateController player;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (player == null)
            player = FindAnyObjectByType<SlimeStateController>();
    }

    private void Update()
    {
        if (player == null) return;

        // 통과 가능 여부는 충돌만 바뀌고, 비주얼은 상태와 무관하게 항상 그대로 유지함
        col.isTrigger = player.CurrentState == passableState;
    }
}
