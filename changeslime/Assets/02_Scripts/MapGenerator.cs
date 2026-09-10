using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("맵 데이터 (한 글자 = 칸 하나, 배열의 첫 줄이 맵 맨 위)")]
    [TextArea(5, 20)]
    [SerializeField]
    private string[] mapRows = new string[]
    {
        "####################",
        "#P                 #",
        "#      ###     G   #",
        "#  S               #",
        "#     ###   L      #",
        "####################",
    };

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
    }

    private void SpawnTile(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return;
        Instantiate(prefab, position, Quaternion.identity, transform);
    }
}
