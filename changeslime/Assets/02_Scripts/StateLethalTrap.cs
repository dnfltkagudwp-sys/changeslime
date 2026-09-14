using UnityEngine;

/// <summary>
/// 특정 상태일 때만 닿으면 리스폰시키는 함정 (예: 액체일 때만 위험한 전류 배관, 기체일 때만 위험한 흡입 환풍구).
/// 지정한 상태가 아니면 아무 효과 없이 안전하게 통과됨.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StateLethalTrap : MonoBehaviour
{
    [SerializeField] private SlimeState lethalState = SlimeState.Liquid;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        SlimeStateController state = collision.GetComponent<SlimeStateController>();
        if (state == null || state.CurrentState != lethalState) return;

        PlayerRespawn respawn = collision.GetComponent<PlayerRespawn>();
        if (respawn != null)
            respawn.Respawn();
    }
}
