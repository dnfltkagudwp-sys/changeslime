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

    [Header("플레이어 참조 (비워두면 자동으로 찾음)")]
    [SerializeField] private SlimeStateController player;

    [Header("통과 가능할 때의 시각 피드백 (선택)")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private float passableAlpha = 0.35f;

    private Collider2D col;
    private float fullAlpha = 1f;

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (player == null)
            player = FindFirstObjectByType<SlimeStateController>();

        if (visualRenderer == null)
            visualRenderer = GetComponent<SpriteRenderer>();

        if (visualRenderer != null)
            fullAlpha = visualRenderer.color.a;
    }

    private void Update()
    {
        if (player == null) return;

        bool isPassable = player.CurrentState == passableState;
        col.isTrigger = isPassable;

        if (visualRenderer != null)
        {
            Color c = visualRenderer.color;
            c.a = isPassable ? passableAlpha : fullAlpha;
            visualRenderer.color = c;
        }
    }
}
