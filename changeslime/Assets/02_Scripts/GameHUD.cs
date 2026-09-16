using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전 레벨 공통 HUD(현재 상태 / 별 개수 / 재시작 안내)와, 1레벨(===LEVEL1=== 이후)에서만 표시되는
/// 이동·점프·변환 패드 조작 가이드를 담당함. 기존 상태 전환·별 수집·레벨 전환 로직은 읽기만 하고
/// 전혀 바꾸지 않음 — 화면에 그리는 것만 담당하는 순수 UI 레이어.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("이동/점프 안내를 보여줄 레벨 인덱스 (게임 내 표시 기준 1레벨)")]
    [SerializeField] private int moveJumpGuideLevelIndex = 0;

    [Header("변환 패드 안내를 보여줄 레벨 인덱스 (게임 내 표시 기준 2레벨, 첫 변환 블록 등장)")]
    [SerializeField] private int padGuideLevelIndex = 1;

    [Header("조작 가이드 감지 설정")]
    [SerializeField] private float moveGuideDistance = 1.5f;    // 이 거리 이상 좌우로 움직이면 이동 안내를 숨김
    [SerializeField] private float jumpAheadCheckDistance = 1f; // 오른쪽으로 이 거리만큼 바닥이 있는지 확인
    [SerializeField] private float padDetectRadius = 2.2f;      // 이 반경 안에 변환 패드가 있으면 패드 안내 표시
    [SerializeField] private float guideFadeSpeed = 6f;         // 안내 문구 페이드 인/아웃 속도

    private enum GuideKind { None, Move, Jump, Pad }

    private LevelManager levelManager;
    private SlimeStateController playerState;
    private SlimeMovement playerMovement;
    private PlayerRespawn playerRespawn;
    private Transform playerTransform;

    private Text stateText;
    private Text starText;
    private Text restartText;
    private Text guideText;
    private CanvasGroup guideGroup;

    private int lastLevelIndex = int.MinValue;
    private float levelStartX;
    private bool movedEnough;
    private SlimeState lastObservedState;
    private bool suppressPadGuide;
    private GuideKind displayedGuide = GuideKind.None;

    private void Awake()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        CachePlayerReferences();
        BuildUI();
    }

    private void CachePlayerReferences()
    {
        playerState = FindAnyObjectByType<SlimeStateController>();
        if (playerState == null) return;

        playerMovement = playerState.GetComponent<SlimeMovement>();
        playerRespawn = playerState.GetComponent<PlayerRespawn>();
        playerTransform = playerState.transform;
        lastObservedState = playerState.CurrentState;
    }

    private void Update()
    {
        if (playerState == null)
        {
            CachePlayerReferences();
            if (playerState == null) return;
        }

        UpdateStateText();
        UpdateStarText();
        UpdateGuide();
    }

    private void UpdateStateText()
    {
        SlimeState state = playerState.CurrentState;
        string label;
        Color color;

        switch (state)
        {
            case SlimeState.Solid:
                label = "고체";
                color = new Color(0.75f, 0.78f, 0.8f); // 회색 계열
                break;
            case SlimeState.Gas:
                label = "기체";
                color = new Color(0.75f, 0.95f, 1f); // 밝은 하늘색 계열
                break;
            default:
                label = "액체";
                color = new Color(0.35f, 0.65f, 1f); // 파란색 계열
                break;
        }

        stateText.text = $"현재 상태: {label}";
        stateText.color = color;
    }

    private void UpdateStarText()
    {
        if (StarManager.Instance == null) return;
        starText.text = $"★ {StarManager.Instance.CollectedStars} / {StarManager.Instance.TotalStars}";
    }

    private void UpdateGuide()
    {
        int currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : int.MinValue;

        // 레벨이 바뀌면 이동/패드 안내 판단 기준값(시작 위치, 마지막으로 관찰한 상태)을 다시 잡음
        if (currentLevelIndex != lastLevelIndex)
        {
            lastLevelIndex = currentLevelIndex;
            movedEnough = false;
            suppressPadGuide = false;
            levelStartX = playerTransform != null ? playerTransform.position.x : 0f;
            if (playerState != null)
                lastObservedState = playerState.CurrentState;
        }

        bool moveJumpEnabled = currentLevelIndex == moveJumpGuideLevelIndex && playerTransform != null && playerMovement != null;
        bool padGuideEnabled = currentLevelIndex == padGuideLevelIndex && playerTransform != null;

        GuideKind desired = GuideKind.None;
        string desiredText = null;

        if (moveJumpEnabled)
        {
            if (!movedEnough && Mathf.Abs(playerTransform.position.x - levelStartX) > moveGuideDistance)
                movedEnough = true;

            if (!movedEnough)
            {
                desired = GuideKind.Move;
                desiredText = "A/D 또는 방향키로 이동";
            }
            else if (playerMovement.IsGrounded && playerMovement.IsGroundAheadMissing(jumpAheadCheckDistance))
            {
                desired = GuideKind.Jump;
                desiredText = "Space로 점프";
            }
        }

        if (desired == GuideKind.None && padGuideEnabled)
        {
            // 상태가 실제로 바뀐 순간 패드 안내를 즉시 끔 (패드에서 벗어나야 다음 패드를 위해 다시 허용)
            if (playerState.CurrentState != lastObservedState)
            {
                suppressPadGuide = true;
                lastObservedState = playerState.CurrentState;
            }

            bool nearPad = IsNearEnvironmentTrigger();
            if (suppressPadGuide && !nearPad)
                suppressPadGuide = false;

            if (nearPad && !suppressPadGuide)
            {
                desired = GuideKind.Pad;
                desiredText = "패드에 닿으면 상태가 변합니다";
            }
        }

        bool shouldShow = desired != GuideKind.None;

        // 다른 안내로 바뀔 때는 완전히 사라진 다음에만 문구를 바꿔치기해서 겹쳐 보이지 않게 함
        if (desired != displayedGuide)
        {
            if (guideGroup.alpha <= 0.02f)
            {
                displayedGuide = desired;
                guideText.text = desiredText ?? string.Empty;
            }
            else
            {
                shouldShow = false;
            }
        }

        float targetAlpha = shouldShow ? 1f : 0f;
        guideGroup.alpha = Mathf.MoveTowards(guideGroup.alpha, targetAlpha, guideFadeSpeed * Time.deltaTime);
    }

    private bool IsNearEnvironmentTrigger()
    {
        if (playerTransform == null) return false;

        EnvironmentTrigger[] triggers = FindObjectsByType<EnvironmentTrigger>(FindObjectsSortMode.None);
        if (triggers.Length == 0) return false;

        float sqrRadius = padDetectRadius * padDetectRadius;
        foreach (EnvironmentTrigger trigger in triggers)
        {
            if (trigger == null) continue;
            if ((trigger.transform.position - playerTransform.position).sqrMagnitude <= sqrRadius)
                return true;
        }

        return false;
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("GameHUDCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // 레벨 클리어 UI(50)보다는 아래, 게임 화면보다는 위

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 좌측 상단: 현재 상태 (LevelManager의 디버그용 레벨 라벨이 이미 좌측 상단 맨 위를 쓰고 있어 그 아래에 배치)
        stateText = CreateHudText(canvasObj.transform, font, "StateText",
            anchor: new Vector2(0f, 1f), pivot: new Vector2(0f, 1f),
            anchoredPosition: new Vector2(24f, -74f), sizeDelta: new Vector2(420f, 46f),
            alignment: TextAnchor.UpperLeft, fontSize: 30, color: Color.white);

        // 중앙 상단: 별 개수
        starText = CreateHudText(canvasObj.transform, font, "StarText",
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPosition: new Vector2(0f, -24f), sizeDelta: new Vector2(300f, 46f),
            alignment: TextAnchor.UpperCenter, fontSize: 32, color: Color.white);

        // 우측 상단: 재시작 안내 (다른 HUD보다 작고 흐리게, 낮은 우선순위)
        restartText = CreateHudText(canvasObj.transform, font, "RestartHintText",
            anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            anchoredPosition: new Vector2(-20f, -18f), sizeDelta: new Vector2(180f, 34f),
            alignment: TextAnchor.UpperRight, fontSize: 20, color: new Color(1f, 1f, 1f, 0.6f));

        string restartKey = playerRespawn != null ? playerRespawn.RespawnKey.ToString() : "R";
        restartText.text = $"{restartKey} 재시작";

        // 하단 중앙: 1레벨 전용 조작 가이드 (평소엔 투명, 필요할 때만 페이드 인)
        GameObject guideObj = new GameObject("GuideText");
        guideObj.transform.SetParent(canvasObj.transform, false);

        guideGroup = guideObj.AddComponent<CanvasGroup>();
        guideGroup.alpha = 0f;
        guideGroup.blocksRaycasts = false;
        guideGroup.interactable = false;

        guideText = guideObj.AddComponent<Text>();
        guideText.font = font;
        guideText.fontSize = 44;
        guideText.fontStyle = FontStyle.Bold;
        guideText.alignment = TextAnchor.MiddleCenter;
        guideText.color = Color.white;
        guideText.horizontalOverflow = HorizontalWrapMode.Overflow;

        Outline outline = guideObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        RectTransform guideRect = guideText.rectTransform;
        guideRect.anchorMin = new Vector2(0.5f, 0f);
        guideRect.anchorMax = new Vector2(0.5f, 0f);
        guideRect.pivot = new Vector2(0.5f, 0f);
        guideRect.anchoredPosition = new Vector2(0f, 90f);
        guideRect.sizeDelta = new Vector2(950f, 76f);
    }

    private Text CreateHudText(Transform parent, Font font, string name, Vector2 anchor, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor alignment, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        return text;
    }
}
