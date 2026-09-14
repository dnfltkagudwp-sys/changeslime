using UnityEngine;

/// <summary>
/// 맵에 배치되는 별. 슬라임이 닿으면 사라지고 StarManager에 수집을 알림.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StarCollectible : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<SlimeStateController>() == null) return;

        if (StarManager.Instance != null)
            StarManager.Instance.CollectStar();

        Destroy(gameObject);
    }
}
