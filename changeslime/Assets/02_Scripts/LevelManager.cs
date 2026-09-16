using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 여러 레벨을 씬 전환 없이 하나의 MapGenerator로 순서대로 불러오는 매니저.
/// 레벨 텍스트들을 구분자(===LEVEL===)로 이어붙여서 인스펙터에 한 번에 넣어두면,
/// 다음 레벨로 넘어갈 때 씬을 새로 로드하지 않고 mapText만 바꿔서 다시 생성함.
/// </summary>
public class LevelManager : MonoBehaviour
{
    // "===LEVEL===" 뒤에 숫자를 붙여도(예: "===LEVEL 3===") 구분자로 인식됨 —
    // 텍스트가 길어지면 어디가 몇 레벨인지 알아보기 힘드므로, 구분자에 다음 레벨 번호를 적어두는 걸 권장
    private static readonly Regex LevelSeparatorPattern = new Regex(@"===LEVEL\s*\d*===");

    [SerializeField] private MapGenerator mapGenerator; // 비워두면 씬에서 자동으로 찾음

    [Header("레벨 텍스트를 ===LEVEL N=== 한 줄로 구분해서 순서대로 이어붙이기 (N은 안 적어도 됨, 알아보기용)")]
    [TextArea(10, 100)]
    [SerializeField] private string allLevelsText;

    [Header("테스트용: 시작할 레벨 번호 (0부터 시작)")]
    [SerializeField] private int startLevelIndex = 0;

    private string[] levelTexts;
    private int currentLevelIndex;
    private SlimeStateController playerState;
    private Text levelLabel;

    /// <summary>현재 로드된 레벨의 내부 인덱스(0부터 시작). 플레이어에게 보여줄 때는 +1한 값을 사용함. HUD 등 외부에서 읽기 전용으로 참조.</summary>
    public int CurrentLevelIndex => currentLevelIndex;

    /// <summary>
    /// 지금이 마지막 레벨인지 여부. allLevelsText에 레벨이 나중에 추가/삭제되어도
    /// levelTexts.Length 기준으로 자동으로 맞으므로, 최종 레벨 번호를 따로 하드코딩할 필요 없음.
    /// </summary>
    public bool IsLastLevel => levelTexts != null && levelTexts.Length > 0 && currentLevelIndex >= levelTexts.Length - 1;

    private void Awake()
    {
        levelTexts = LevelSeparatorPattern.Split(allLevelsText);

        if (mapGenerator == null)
            mapGenerator = FindAnyObjectByType<MapGenerator>();

        playerState = FindAnyObjectByType<SlimeStateController>();
        levelLabel = CreateLevelLabelUI();
    }

    private void Start()
    {
        LoadLevel(startLevelIndex);
    }

    public void LoadLevel(int index)
    {
        if (levelTexts == null || levelTexts.Length == 0 || mapGenerator == null) return;

        currentLevelIndex = Mathf.Clamp(index, 0, levelTexts.Length - 1);
        mapGenerator.MapText = levelTexts[currentLevelIndex].Trim();
        mapGenerator.GenerateMap();

        // GenerateMap()은 위치/속도만 리셋하고 상태(액체/고체/기체)는 안 건드리므로,
        // 레벨이 바뀔 때 이전 레벨의 마지막 상태가 그대로 넘어오지 않도록 여기서 초기화함
        if (playerState != null)
            playerState.ChangeState(SlimeState.Liquid);

        // 플레이어에게는 내부 인덱스를 노출하지 않음: 0번 인덱스는 "튜토리얼", 그 외에는 1부터 시작하는
        // 레벨 번호 / 전체 레벨 수(튜토리얼 제외, 하드코딩 없이 levelTexts에서 계산)로 표시
        if (levelLabel != null)
        {
            levelLabel.text = currentLevelIndex == 0
                ? "튜토리얼"
                : $"레벨 {currentLevelIndex} / {levelTexts.Length - 1}";
        }
    }

    /// <summary>
    /// levelTexts 중 특정 타일 문자가 처음 등장하는 레벨의 인덱스를 찾음 (없으면 -1).
    /// 어느 레벨에 특정 오브젝트가 처음 나오는지를 맵 텍스트를 하드코딩하지 않고 그때그때 확인하기 위한 용도.
    /// </summary>
    public int FirstLevelIndexContaining(char tile)
    {
        if (levelTexts == null) return -1;

        for (int i = 0; i < levelTexts.Length; i++)
        {
            if (levelTexts[i].IndexOf(tile) >= 0)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 테스트 중에 지금 몇 번째 레벨인지 한눈에 보이도록 화면 좌상단에 작은 라벨을 띄움.
    /// </summary>
    private Text CreateLevelLabelUI()
    {
        GameObject canvasObj = new GameObject("LevelLabelCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("LevelLabel");
        textObj.transform.SetParent(canvasObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        // 유니티 내장 폰트(LegacyRuntime.ttf)는 한글 글리프가 없어 WebGL에서 한글이 안 보이므로,
        // 한글을 지원하는 폰트를 우선 사용하고 못 찾으면 내장 폰트로 대체함
        text.font = Resources.Load<Font>("Fonts/NanumGothic-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -12f);
        rect.sizeDelta = new Vector2(300f, 50f);

        return text;
    }

    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (levelTexts != null && nextIndex >= levelTexts.Length)
        {
            Debug.Log("[LevelManager] 모든 레벨을 클리어했습니다. 처음 레벨로 돌아갑니다.");
            nextIndex = 0;
        }

        LoadLevel(nextIndex);
    }
}
