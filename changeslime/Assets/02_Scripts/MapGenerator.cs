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

    [Header("플레이어 (선택, 'P' 위치로 이동시킴)")]
    [SerializeField] private Transform player;

    [Header("카메라 (선택, 맵 생성 후 전체가 보이도록 자동 배치)")]
    [SerializeField] private Camera targetCamera; // 비워두면 Camera.main 사용
    [SerializeField] private float cameraPadding = 1f; // 맵 가장자리 여유 공간(유닛)

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
                    case 'P':
                        if (player != null)
                            player.position = pos;
                        break;
                    // 그 외 문자(공백 등)는 빈 칸으로 취급하고 넘어감
                }
            }
        }

        FitCameraToMap(mapRows);
    }

    private void SpawnTile(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return;
        Instantiate(prefab, position, Quaternion.identity, transform);
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
