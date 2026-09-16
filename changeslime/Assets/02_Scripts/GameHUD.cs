using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전 레벨 공통 HUD(현재 상태 / 별 개수 / 재시작 / 전체 맵 보기 안내)와, 특정 레벨에서만 한 번씩 표시되는
/// 이동·점프 조작 안내 및 액체·고체·기체 상태별 특성 설명을 담당함. 기존 상태 전환·별 수집·레벨 전환
/// 로직은 읽기만 하고 전혀 바꾸지 않음 — 화면에 그리는 것만 담당하는 순수 UI 레이어.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("이동/점프 안내 + 액체 특성 설명을 보여줄 레벨 인덱스 (게임 내 표시 기준 1레벨)")]
    [SerializeField] private int moveJumpGuideLevelIndex = 0;

    [Header("변환 패드 안내 + 고체 특성 설명을 보여줄 레벨 인덱스 (게임 내 표시 기준 2레벨, 첫 변환 블록 등장)")]
    [SerializeField] private int padGuideLevelIndex = 1;

    [Header("조작 가이드 감지 설정")]
    [SerializeField] private float moveGuideDistance = 1.5f;    // 이 거리 이상 좌우로 움직이면 이동 안내를 숨김
    [SerializeField] private float jumpAheadCheckDistance = 1f; // 오른쪽으로 이 거리만큼 바닥이 있는지 확인
    [SerializeField] private float padDetectRadius = 2.2f;      // 이 반경 안에 변환 패드가 있으면 패드 안내 표시
    [SerializeField] private float guideFadeSpeed = 6f;         // 안내 문구 페이드 인/아웃 속도

    [Header("상태별 특성 설명 (처음 소개되는 레벨에서 한 번만, 일정 시간 표시 후 자동으로 사라짐)")]
    [SerializeField] private float infoGuideDuration = 4f;

    private enum GuideKind { None, Move, Jump, Pad, LiquidInfo, SolidInfo, GasInfo }

    private static readonly Color LiquidColor = new Color(0.35f, 0.65f, 1f);   // 파란색 계열
    private static readonly Color SolidColor = new Color(0.75f, 0.78f, 0.8f);  // 회색 계열
    private static readonly Color GasColor = new Color(0.75f, 0.95f, 1f);      // 밝은 하늘색 계열

    private const string LiquidInfoMessage = "액체: 점프할 수 있고 액체 통로를 통과합니다.\n전류 배관에 닿으면 재시작됩니다.";
    private const string SolidInfoMessage = "고체: 느리고 점프할 수 없지만 전류에 안전합니다.\n높은 곳에서 떨어지면 파괴 블록을 부술 수 있습니다.";
    private const string GasInfoMessage = "기체: 계속 위로 떠오르며 스스로 내려올 수 없습니다.\n기체 통로를 통과하지만 독성 안개에 닿으면 재시작됩니다.";

    private LevelManager levelManager;
    private CameraFollow cameraFollow;
    private SlimeStateController playerState;
    private SlimeMovement playerMovement;
    private PlayerRespawn playerRespawn;
    private Transform playerTransform;

    private Text stateText;
    private Text starText;
    private Text restartText;
    private Text mapViewText;
    private Text guideText;
    private CanvasGroup guideGroup;

    private int lastGenerationId = int.MinValue;
    private float levelStartX;
    private bool movedEnough;
    private bool jumpConditionSeen;
    private SlimeState lastObservedState;
    private bool suppressPadGuide;
    private bool liquidInfoShown;
    private bool solidInfoPending;
    private bool solidInfoShown;
    private bool gasInfoPending;
    private bool gasInfoShown;
    private int gasGuideLevelIndex = int.MinValue; // 아직 계산 안 함(캐시 전) 표시값
    private GuideKind displayedGuide = GuideKind.None;
    private float infoGuideTimer;

    private void Awake()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        cameraFollow = FindAnyObjectByType<CameraFollow>();
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
                color = SolidColor;
                break;
            case SlimeState.Gas:
                label = "기체";
                color = GasColor;
                break;
            default:
                label = "액체";
                color = LiquidColor;
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
        if (gasGuideLevelIndex == int.MinValue)
            gasGuideLevelIndex = levelManager != null ? levelManager.FirstLevelIndexContaining('G') : -1;

        int currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : int.MinValue;
        int generationId = StarManager.Instance != null ? StarManager.Instance.GenerationId : int.MinValue;

        // 레벨이 (재)생성되면 — 다음 레벨로 넘어갈 때뿐 아니라 같은 레벨을 리스폰으로 다시 시작할 때도 —
        // 조작 가이드 판단 기준값과 상태별 설명 표시 여부를 전부 다시 잡음
        if (generationId != lastGenerationId)
        {
            lastGenerationId = generationId;
            movedEnough = false;
            jumpConditionSeen = false;
            suppressPadGuide = false;
            liquidInfoShown = false;
            solidInfoPending = false;
            solidInfoShown = false;
            gasInfoPending = false;
            gasInfoShown = false;
            levelStartX = playerTransform != null ? playerTransform.position.x : 0f;
            if (playerState != null)
                lastObservedState = playerState.CurrentState;
        }

        // 상태가 실제로 바뀐 순간을 감지 (기존 패드 안내를 끄는 것 + 상태별 설명을 예약하는 것 둘 다에 사용)
        if (playerState.CurrentState != lastObservedState)
        {
            SlimeState newState = playerState.CurrentState;

            if (newState == SlimeState.Solid && currentLevelIndex == padGuideLevelIndex && !solidInfoShown)
                solidInfoPending = true;
            if (newState == SlimeState.Gas && gasGuideLevelIndex >= 0 && currentLevelIndex == gasGuideLevelIndex && !gasInfoShown)
                gasInfoPending = true;

            suppressPadGuide = true;
            lastObservedState = newState;
        }

        bool moveJumpEnabled = currentLevelIndex == moveJumpGuideLevelIndex && playerTransform != null && playerMovement != null;
        bool padGuideEnabled = currentLevelIndex == padGuideLevelIndex && playerTransform != null;
        bool gasGuideEnabled = gasGuideLevelIndex >= 0 && currentLevelIndex == gasGuideLevelIndex;

        GuideKind desired = GuideKind.None;
        string desiredText = null;
        Color desiredColor = Color.white;

        if (moveJumpEnabled)
        {
            if (!movedEnough && Mathf.Abs(playerTransform.position.x - levelStartX) > moveGuideDistance)
                movedEnough = true;

            bool needsJump = playerMovement.IsGrounded && playerMovement.IsGroundAheadMissing(jumpAheadCheckDistance);
            if (needsJump) jumpConditionSeen = true;

            if (!movedEnough)
            {
                desired = GuideKind.Move;
                desiredText = "A/D 또는 방향키로 이동";
            }
            else if (needsJump)
            {
                desired = GuideKind.Jump;
                desiredText = "Space로 점프";
            }
            else if (!liquidInfoShown && jumpConditionSeen)
            {
                // 이동·점프 안내를 이미 한 번씩 겪은 뒤, 지금 당장 점프가 필요한 상황이 아닐 때 이어서 설명
                desired = GuideKind.LiquidInfo;
                desiredText = LiquidInfoMessage;
                desiredColor = LiquidColor;
            }
        }

        if (desired == GuideKind.None && padGuideEnabled)
        {
            bool nearPad = IsNearEnvironmentTrigger();
            if (suppressPadGuide && !nearPad)
                suppressPadGuide = false;

            if (nearPad && !suppressPadGuide)
            {
                desired = GuideKind.Pad;
                desiredText = "패드에 닿으면 상태가 변합니다";
            }
            else if (solidInfoPending && !solidInfoShown)
            {
                desired = GuideKind.SolidInfo;
                desiredText = SolidInfoMessage;
                desiredColor = SolidColor;
            }
        }

        if (desired == GuideKind.None && gasGuideEnabled && gasInfoPending && !gasInfoShown)
        {
            desired = GuideKind.GasInfo;
            desiredText = GasInfoMessage;
            desiredColor = GasColor;
        }

        // 상태별 설명은 한 번 표시되기 시작하면(페이드 인 도중 포함), 정해진 시간이 다 지나기 전까지는
        // 위 판단과 무관하게 계속 붙잡아둠. 다 보인 뒤부터만 시간을 차감해서 "완전히 보이는 시간"이 약 infoGuideDuration초가 되게 함
        bool displayingInfoGuide = displayedGuide == GuideKind.LiquidInfo || displayedGuide == GuideKind.SolidInfo || displayedGuide == GuideKind.GasInfo;
        if (displayingInfoGuide && infoGuideTimer > 0f)
        {
            desired = displayedGuide;
            if (guideGroup.alpha >= 0.98f)
                infoGuideTimer -= Time.deltaTime;
        }

        bool shouldShow = desired != GuideKind.None;

        // 다른 안내로 바뀔 때는 완전히 사라진 다음에만 문구를 바꿔치기해서 겹쳐 보이지 않게 함
        if (desired != displayedGuide)
        {
            if (guideGroup.alpha <= 0.02f)
            {
                displayedGuide = desired;
                guideText.text = desiredText ?? string.Empty;
                guideText.color = desiredColor;

                switch (desired)
                {
                    case GuideKind.LiquidInfo:
                        liquidInfoShown = true;
                        infoGuideTimer = infoGuideDuration;
                        break;
                    case GuideKind.SolidInfo:
                        solidInfoShown = true;
                        solidInfoPending = false;
                        infoGuideTimer = infoGuideDuration;
                        break;
                    case GuideKind.GasInfo:
                        gasInfoShown = true;
                        gasInfoPending = false;
                        infoGuideTimer = infoGuideDuration;
                        break;
                }
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

        // 유니티 내장 폰트(LegacyRuntime.ttf)는 한글 글리프가 없어 WebGL에서 한글이 안 보이므로,
        // 한글을 지원하는 폰트를 우선 사용하고 못 찾으면 내장 폰트로 대체함
        Font font = Resources.Load<Font>("Fonts/NanumGothic-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 좌측 상단: 현재 상태 (LevelManager의 디버그용 레벨 라벨이 이미 좌측 상단 맨 위를 쓰고 있어 그 아래에 배치)
        stateText = CreateHudText(canvasObj.transform, font, "StateText",
            anchor: new Vector2(0f, 1f), pivot: new Vector2(0f, 1f),
            anchoredPosition: new Vector2(24f, -76f), sizeDelta: new Vector2(440f, 52f),
            alignment: TextAnchor.UpperLeft, fontSize: 34, color: Color.white);

        // 중앙 상단: 별 개수
        starText = CreateHudText(canvasObj.transform, font, "StarText",
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPosition: new Vector2(0f, -26f), sizeDelta: new Vector2(320f, 52f),
            alignment: TextAnchor.UpperCenter, fontSize: 36, color: Color.white);

        // 우측 상단: 재시작 안내 (다른 HUD보다 작고 흐리게, 낮은 우선순위)
        restartText = CreateHudText(canvasObj.transform, font, "RestartHintText",
            anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            anchoredPosition: new Vector2(-20f, -20f), sizeDelta: new Vector2(200f, 38f),
            alignment: TextAnchor.UpperRight, fontSize: 23, color: new Color(1f, 1f, 1f, 0.6f));

        string restartKey = playerRespawn != null ? playerRespawn.RespawnKey.ToString() : "R";
        restartText.text = $"{restartKey} 재시작";

        // 재시작 안내 바로 아래에 같은 우선순위로 배치
        mapViewText = CreateHudText(canvasObj.transform, font, "MapViewHintText",
            anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            anchoredPosition: new Vector2(-20f, -58f), sizeDelta: new Vector2(200f, 38f),
            alignment: TextAnchor.UpperRight, fontSize: 23, color: new Color(1f, 1f, 1f, 0.6f));

        string mapViewKey = cameraFollow != null ? cameraFollow.FullMapViewKey.ToString() : "Tab";
        mapViewText.text = $"{mapViewKey} 전체 맵 보기";

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
        guideText.verticalOverflow = VerticalWrapMode.Overflow; // 상태별 설명은 두 줄이라 잘리지 않도록 함

        Outline outline = guideObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        RectTransform guideRect = guideText.rectTransform;
        guideRect.anchorMin = new Vector2(0.5f, 0f);
        guideRect.anchorMax = new Vector2(0.5f, 0f);
        guideRect.pivot = new Vector2(0.5f, 0f);
        guideRect.anchoredPosition = new Vector2(0f, 90f);
        guideRect.sizeDelta = new Vector2(950f, 120f);
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
