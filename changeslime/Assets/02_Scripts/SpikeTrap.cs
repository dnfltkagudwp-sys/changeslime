using UnityEngine;

/// <summary>
/// 닿으면 즉시 플레이어를 리스폰시키는 즉사 함정. 리스폰 시스템(PlayerRespawn)이 있어야 안전하게 사용 가능.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpikeTrap : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerRespawn respawn = collision.GetComponent<PlayerRespawn>();
        if (respawn != null)
            respawn.Respawn();
    }
}
