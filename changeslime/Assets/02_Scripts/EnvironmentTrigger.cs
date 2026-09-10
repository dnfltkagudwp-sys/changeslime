using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnvironmentTrigger : MonoBehaviour
{
    [Header("충돌 시 변경할 타겟 상태")]
    [SerializeField] private SlimeState targetState;

    private void Awake()
    {
        // 충돌 시 밀려나지 않고 통과하도록 Trigger 강제 설정
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 부모 또는 본체에서 상태 컨트롤러 탐색
        SlimeStateController stateController = collision.GetComponent<SlimeStateController>();
        if (stateController != null)
        {
            stateController.ChangeState(targetState);
        }
    }
}