using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("맵 데이터 (한 글자 = 칸 하나, 통째로 복사-붙여넣기 가능)")]
    [TextArea(10, 30)]
    [SerializeField]
    private string mapText =
        "####################\n" +
        "#P                 #\n" +
        "#        S         #\n" +
        "#       ###        #\n" +
        "#                  #\n" +
        "#           L      #\n" +
        "####################";

    [Header("칸 크기 및 원점")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 origin = Vector2.zero;

    [Header("프리팹 매핑")]
    [SerializeField] private GameObject groundPrefab; // '#'
    [SerializeField] private GameObject liquidPrefab; // 'L'
    [SerializeField] private GameObject solidPrefab;  // 'S'
    [SerializeField] private GameObject gasPrefab;    // 'G'
    [SerializeField] private GameObject goalPrefab;   // 'X'
    [SerializeField] private GameObject liquidGatePrefab;    // 'W' 액체 상태일 때만 통과 가능
    [SerializeField] private GameObject gasGatePrefab;       // 'V' 기체 상태일 때만 통과 가능
    [SerializeField] private GameObject breakableBlockPrefab; // 'B' 고체 상태로 낙하 충돌해야 부서짐

    [Header("플레이어 (선택, 'P' 위치로 이동시킴)")]
    [SerializeField] private Transform player;

    [Header("카메라 (선택, 맵 생성 후 전체가 보이도록 자동 배치)")]
    [SerializeField] private bool autoFitCameraToMap = true; // 맵이 커서 CameraFollow로 따라다니게 할 거면 꺼두기
    [SerializeField] private Camera targetCamera; // 비워두면 Camera.main 사용
    [SerializeField] private float cameraPadding = 1f; // 맵 가장자리 여유 공간(유닛)

    [Header("바닥 충돌 이음새 병합")]
    [SerializeField] private bool mergeGroundColliders = true; // 바닥 타일 사이 이음새에 걸려 점프가 멈추는 문제 방지

    private void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        // 이전에 생성된 맵 오브젝트 정리
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        string[] mapRows = mapText.Replace("\r\n", "\n").Split('\n');

        for (int row = 0; row < mapRows.Length; row++)
        {
            string line = mapRows[row];
            for (int col = 0; col < line.Length; col++)
            {
                char tile = line[col];
                Vector2 pos = origin + new Vector2(col * cellSize, -row * cellSize);

                switch (tile)
                {
                    case '#':
                        SpawnTile(groundPrefab, pos);
                        break;
                    case 'L':
                        SpawnTile(liquidPrefab, pos);
                        break;
                    case 'S':
                        SpawnTile(solidPrefab, pos);
                        break;
                    case 'G':
                        SpawnTile(gasPrefab, pos);
                        break;
                    case 'X':
                        SpawnTile(goalPrefab, pos);
                        break;
                    case 'W':
                        SpawnTile(liquidGatePrefab, pos);
                        break;
                    case 'V':
                        SpawnTile(gasGatePrefab, pos);
                        break;
                    case 'B':
                        SpawnTile(breakableBlockPrefab, pos);
                        break;
                    case 'P':
                        if (player != null)
                            player.position = pos;
                        break;
                    // 그 외 문자(공백 등)는 빈 칸으로 취급하고 넘어감
                }
            }
        }

        if (mergeGroundColliders)
            SetupCompositeGroundCollider();

        if (autoFitCameraToMap)
            FitCameraToMap(mapRows);
    }

    private void SpawnTile(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);

        // 바닥 타일끼리는 콜라이더를 하나로 합쳐서, 타일 이음새에 캐릭터가 걸리지 않도록 함
        if (mergeGroundColliders && prefab == groundPrefab)
        {
            BoxCollider2D box = instance.GetComponent<BoxCollider2D>();
            if (box != null)
                box.compositeOperation = Collider2D.CompositeOperation.Merge;
        }
    }

    /// <summary>
    /// 이 오브젝트에 Rigidbody2D(Static) + CompositeCollider2D를 준비해서,
    /// compositeOperation이 Merge로 설정된 자식 바닥 타일들의 콜라이더를 이음새 없는 하나의 도형으로 합침
    /// </summary>
    private void SetupCompositeGroundCollider()
    {
        Rigidbody2D groundBody = GetComponent<Rigidbody2D>();
        if (groundBody == null)
            groundBody = gameObject.AddComponent<Rigidbody2D>();
        groundBody.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite = GetComponent<CompositeCollider2D>();
        if (composite == null)
            composite = gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        // 병합된 콜라이더는 물리적으로 이 오브젝트(MapGenerator) 소속이 되므로,
        // 바닥 판정(groundLayer)이 제대로 되도록 이 오브젝트도 바닥 타일과 같은 레이어로 맞춰줌
        if (groundPrefab != null)
            gameObject.layer = groundPrefab.layer;

        // 벽에 붙어 점프할 때 마찰로 걸리는 문제 방지 (플레이어 쪽 무마찰 재질과 짝을 맞춤)
        if (composite.sharedMaterial == null || composite.sharedMaterial.friction != 0f)
        {
            composite.sharedMaterial = new PhysicsMaterial2D("GroundNoFriction") { friction = 0f, bounciness = 0f };
        }

        composite.GenerateGeometry();
    }

    /// <summary>
    /// 생성된 맵 전체가 한 화면에 들어오도록 카메라 위치와 Orthographic Size를 자동으로 맞춤
    /// </summary>
    private void FitCameraToMap(string[] mapRows)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null || !targetCamera.orthographic)
            return;

        int rowCount = mapRows.Length;
        int colCount = 0;
        foreach (string line in mapRows)
            colCount = Mathf.Max(colCount, line.Length);

        if (rowCount == 0 || colCount == 0)
            return;

        float mapWidth = colCount * cellSize;
        float mapHeight = rowCount * cellSize;

        // 맵의 중앙 좌표 (row 0이 맨 위, 아래로 갈수록 -Y)
        Vector2 center = origin + new Vector2(mapWidth / 2f - cellSize / 2f, -(mapHeight / 2f - cellSize / 2f));

        Vector3 camPos = targetCamera.transform.position;
        targetCamera.transform.position = new Vector3(center.x, center.y, camPos.z);

        // 세로 기준, 가로 기준(화면 비율 고려) 둘 다 맵이 다 보이도록 더 큰 쪽으로 맞춤
        float verticalSize = mapHeight / 2f + cameraPadding;
        float horizontalSize = (mapWidth / targetCamera.aspect) / 2f + cameraPadding;
        targetCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }
}
