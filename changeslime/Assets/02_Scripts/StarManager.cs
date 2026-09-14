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

    [Header("클리어 문구")]
    [SerializeField] private string clearMessage = "GAME CLEAR!";

    [Header("다음 레벨 버튼")]
    [SerializeField] private string nextLevelButtonLabel = "다음 레벨";

    [Header("다음 씬 이름 (비워두면 버튼을 눌러도 전환하지 않음)")]
    [SerializeField] private string nextSceneName;

    private int totalStars;
    private int collectedStars;
    private bool cleared = false;
    private GameObject clearUIRoot;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// MapGenerator가 맵을 생성하면서 배치한 별 개수를 알려줌. 맵이 새로 생성될 때마다 호출됨.
    /// </summary>
    public void SetTotalStars(int count)
    {
        totalStars = count;
        collectedStars = 0;
        cleared = false;
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
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void ShowClearUI()
    {
        if (clearUIRoot == null)
            clearUIRoot = CreateClearUI();

        clearUIRoot.SetActive(true);
    }

    private GameObject CreateClearUI()
    {
        EnsureEventSystemExists();

        GameObject canvasObj = new GameObject("ClearMessageCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject textObj = new GameObject("ClearText");
        textObj.transform.SetParent(canvasObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.font = font;
        text.text = clearMessage;
        text.fontSize = 64;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

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
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
    }
}
