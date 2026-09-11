using UnityEngine;

/// <summary>
/// 평소엔 단단한 벽이지만, 고체 상태의 슬라임이 일정 높이 이상에서 떨어져 부딪히면 부서지는 블록.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BreakableBlock : MonoBehaviour
{
    [Header("부서지는 조건 (고체 상태로 이 속도 이상 낙하 중 충돌해야 함)")]
    [Tooltip("고체 상태 중력(기본 5배) 기준으로 대략 1~2칸 높이에서 떨어진 속도에 해당하는 값")]
    [SerializeField] private float requiredFallSpeed = 5f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        SlimeStateController slime = collision.collider.GetComponent<SlimeStateController>();
        if (slime == null || slime.CurrentState != SlimeState.Solid) return;

        // 아래로 향하는 충돌 속도(양수)를 구해 요구 속도와 비교
        float downwardImpactSpeed = -collision.relativeVelocity.y;
        if (downwardImpactSpeed >= requiredFallSpeed)
        {
            Destroy(gameObject);
        }
    }
}
