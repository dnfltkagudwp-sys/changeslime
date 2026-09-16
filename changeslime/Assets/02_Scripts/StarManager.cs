using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 맵에 배치된 별을 모두 모으면 클리어 문구와 "다음 레벨" 버튼을 띄움.
/// (기존 깃발/골인 지점을 대체하는 클리어 조건)
/// </summary>
public class StarManager : MonoBehaviour
{
    public static StarManager Instance { get; private set; }

    [Header("클리어 문구 (중간 레벨)")]
    [SerializeField] private string clearMessage = "GAME CLEAR!";

    [Header("다음 레벨 버튼 (중간 레벨)")]
    [SerializeField] private string nextLevelButtonLabel = "다음 레벨";

    [Header("클리어 문구 (마지막 레벨 — LevelManager 기준 레벨이 나중에 추가돼도 자동으로 마지막 레벨에 적용됨)")]
    [SerializeField] private string finalClearMessage = "ALL CLEAR!";

    [Header("재시작 버튼 (마지막 레벨)")]
    [SerializeField] private string restartButtonLabel = "처음부터 다시";

    [Header("다음 레벨 (LevelManager가 있으면 씬 전환 없이 이걸 우선 사용)")]
    [SerializeField] private LevelManager levelManager; // 비워두면 씬에서 자동으로 찾음

    [Header("다음 씬 이름 (LevelManager가 없을 때만 사용, 비워두면 전환 안 함)")]
    [SerializeField] private string nextSceneName;

    private int totalStars;
    private int collectedStars;
    private bool cleared = false;
    private GameObject clearUIRoot;
    private Text clearMessageText;
    private Text clearButtonText;

    /// <summary>현재 레벨의 전체 별 개수. HUD 등 외부에서 읽기 전용으로 참조.</summary>
    public int TotalStars => totalStars;

    /// <summary>현재 레벨에서 모은 별 개수. HUD 등 외부에서 읽기 전용으로 참조.</summary>
    public int CollectedStars => collectedStars;

    /// <summary>
    /// 맵이 (재)생성될 때마다 하나씩 증가함 — 다음 레벨로 넘어갈 때뿐 아니라 같은 레벨을 리스폰으로
    /// 다시 시작할 때도 늘어나므로, GameHUD가 "레벨이 새로 시작됐다"를 감지하는 용도로 참조.
    /// </summary>
    public int GenerationId { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (levelManager == null)
            levelManager = FindAnyObjectByType<LevelManager>();
    }

    /// <summary>
    /// MapGenerator가 맵을 생성하면서 배치한 별 개수를 알려줌. 맵이 새로 생성될 때마다 호출됨.
    /// </summary>
    public void SetTotalStars(int count)
    {
        totalStars = count;
        collectedStars = 0;
        cleared = false;
        GenerationId++;
    }

    public void CollectStar()
    {
        collectedStars++;

        if (!cleared && totalStars > 0 && collectedStars >= totalStars)
        {
            cleared = true;
            ShowClearUI();
        }
    }

    private void OnNextLevelClicked()
    {
        // 클리어 UI는 다음 레벨로 넘어갈 때 가려야 함 (씬 전환 없이 그대로 재사용되므로)
        if (clearUIRoot != null)
            clearUIRoot.SetActive(false);

        if (levelManager != null)
        {
            levelManager.LoadNextLevel();
        }
        else if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void ShowClearUI()
    {
        if (clearUIRoot == null)
            clearUIRoot = CreateClearUI();

        // 지금이 마지막 레벨인지에 따라 문구/버튼을 다르게 보여줌.
        // 레벨이 나중에 추가/삭제돼도 LevelManager.IsLastLevel이 그때그때 다시 계산되므로 손볼 필요 없음.
        bool isLastLevel = levelManager != null && levelManager.IsLastLevel;
        clearMessageText.text = isLastLevel ? finalClearMessage : clearMessage;
        clearButtonText.text = isLastLevel ? restartButtonLabel : nextLevelButtonLabel;

        clearUIRoot.SetActive(true);
    }

    private GameObject CreateClearUI()
    {
        EnsureEventSystemExists();

        GameObject canvasObj = new GameObject("ClearMessageCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // 상시 표시되는 HUD보다 위에 그려지도록 함
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 유니티 내장 폰트(LegacyRuntime.ttf)는 한글 글리프가 없어 WebGL에서 한글이 안 보이므로,
        // 한글을 지원하는 폰트를 우선 사용하고 못 찾으면 내장 폰트로 대체함
        Font font = Resources.Load<Font>("Fonts/NanumGothic-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject textObj = new GameObject("ClearText");
        textObj.transform.SetParent(canvasObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.font = font;
        text.text = clearMessage;
        text.fontSize = 64;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        clearMessageText = text;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0.55f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        GameObject buttonObj = new GameObject("NextLevelButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(OnNextLevelClicked);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(280, 80);
        buttonRect.anchoredPosition = new Vector2(0, -60);

        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);

        Text buttonText = buttonTextObj.AddComponent<Text>();
        buttonText.font = font;
        buttonText.text = nextLevelButtonLabel;
        buttonText.fontSize = 32;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.black;
        clearButtonText = buttonText;

        RectTransform buttonTextRect = buttonText.rectTransform;
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        return canvasObj;
    }

    /// <summary>
    /// 버튼 클릭을 처리하려면 씬에 EventSystem이 있어야 하는데, 지금 씬엔 없어서 직접 만들어줌.
    /// </summary>
    private void EnsureEventSystemExists()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
    }
}
