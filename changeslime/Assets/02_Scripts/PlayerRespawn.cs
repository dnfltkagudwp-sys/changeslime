using UnityEngine;

/// <summary>
/// R키(또는 나중에 추가될 UI 리스폰 버튼)를 누르면 플레이어를 스폰 지점으로 되돌리고 상태를 초기화함.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private KeyCode respawnKey = KeyCode.R;

    private Rigidbody2D rb;
    private SlimeStateController stateController;
    private Vector3 spawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stateController = GetComponent<SlimeStateController>();
        spawnPosition = transform.position;
    }

    private void Update()
    {
        if (Input.GetKeyDown(respawnKey))
            Respawn();
    }

    /// <summary>
    /// MapGenerator가 'P' 위치에 플레이어를 배치할 때 호출해서 스폰 지점을 갱신함.
    /// </summary>
    public void SetSpawnPoint(Vector3 position)
    {
        spawnPosition = position;
    }

    /// <summary>
    /// UI 리스폰 버튼 등 외부에서도 호출할 수 있도록 공개 메서드로 둠.
    /// </summary>
    public void Respawn()
    {
        transform.position = spawnPosition;
        rb.position = spawnPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.WakeUp();

        if (stateController != null)
            stateController.ChangeState(SlimeState.Liquid);
    }
}
