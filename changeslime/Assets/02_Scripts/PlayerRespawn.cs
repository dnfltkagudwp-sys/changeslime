using UnityEngine;

/// <summary>
/// R키(또는 나중에 추가될 UI 리스폰 버튼)를 누르면 플레이어를 스폰 지점으로 되돌리고 상태를 초기화함.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private KeyCode respawnKey = KeyCode.R;

    /// <summary>리스폰 단축키. HUD 안내 문구가 실제 설정값과 어긋나지 않도록 외부에서 읽기 전용으로 참조.</summary>
    public KeyCode RespawnKey => respawnKey;

    [Header("맵 이탈사 (맵 위/아래 경계보다 이만큼 더 벗어나면 리스폰 — 추락은 물론, 기체로 계속 떠올라 천장 밖으로 나가는 경우도 포함)")]
    [SerializeField] private float outOfBoundsMargin = 3f;

    private Rigidbody2D rb;
    private SlimeStateController stateController;
    private MapGenerator mapGenerator;
    private Vector3 spawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stateController = GetComponent<SlimeStateController>();
        mapGenerator = FindAnyObjectByType<MapGenerator>();
        spawnPosition = transform.position;
    }

    private void Update()
    {
        if (Input.GetKeyDown(respawnKey))
            Respawn();

        // 맵 위/아래 경계를 한참 벗어나면 자동으로 리스폰시킴
        // (구덩이를 잘못 건너뛰어 추락하는 경우 + 기체 상태로 계속 떠올라 천장 뚫린 곳으로 빠져나가는 경우 둘 다 커버)
        if (mapGenerator != null)
        {
            float halfHeight = mapGenerator.MapWorldSize.y / 2f;
            float bottomY = mapGenerator.MapCenter.y - halfHeight - outOfBoundsMargin;
            float topY = mapGenerator.MapCenter.y + halfHeight + outOfBoundsMargin;

            if (transform.position.y < bottomY || transform.position.y > topY)
                Respawn();
        }
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
        // 부서진 블록/모은 별 등 맵 상태 전체를 처음 그대로 다시 생성함
        if (mapGenerator != null)
            mapGenerator.GenerateMap();

        transform.position = spawnPosition;
        rb.position = spawnPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.WakeUp();

        if (stateController != null)
            stateController.ChangeState(SlimeState.Liquid);
    }
}
