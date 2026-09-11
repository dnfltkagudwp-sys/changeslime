using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 슬라임이 닿으면 클리어 문구를 띄우고, 지정된 다음 씬으로 넘어가는 골인 지점(깃발).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GoalFlag : MonoBehaviour
{
    [Header("클리어 문구")]
    [SerializeField] private string clearMessage = "GAME CLEAR!";
    [SerializeField] private float messageDuration = 2f; // 문구를 보여주고 다음 씬으로 넘어가기까지의 시간

    [Header("다음 씬 이름 (비워두면 전환 없이 문구만 표시)")]
    [SerializeField] private string nextSceneName;

    private bool cleared = false;
    private Text clearText;

    private void Awake()
    {
        // 슬라임이 통과하며 감지되도록 트리거로 강제 설정
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (cleared) return;

        // 슬라임(SlimeStateController가 붙은 오브젝트)인지 확인
        if (collision.GetComponent<SlimeStateController>() == null) return;

        cleared = true;
        StartCoroutine(ClearRoutine());
    }

    private IEnumerator ClearRoutine()
    {
        ShowClearMessage();
        yield return new WaitForSeconds(messageDuration);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void ShowClearMessage()
    {
        if (clearText == null)
            clearText = CreateClearMessageUI();

        clearText.text = clearMessage;
        clearText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 씬에 미리 Canvas/Text를 만들어두지 않아도 되도록, 필요한 순간에 화면 중앙에 꽉 차는 텍스트를 직접 생성함.
    /// </summary>
    private Text CreateClearMessageUI()
    {
        GameObject canvasObj = new GameObject("ClearMessageCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("ClearText");
        textObj.transform.SetParent(canvasObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 64;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }
}
