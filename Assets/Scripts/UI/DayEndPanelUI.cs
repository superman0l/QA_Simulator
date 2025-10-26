using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 当日结算面板 UI：订阅 GameManager.OnDayEnded，显示统计并允许进入下一天
public class DayEndPanelUI : MonoBehaviour
{
    [Header("面板根节点（显示/隐藏）")]
    public GameObject panelRoot;

    [Header("统计文本绑定")]
    public TextMeshProUGUI judgementCountText;      // 单子判定数量：8
    public TextMeshProUGUI wrongJudgementCountText; // 单子判定失误数量：3
    public TextMeshProUGUI todayExpensesText;       // 今日生活费消耗：75
    public TextMeshProUGUI remainingMoneyText;      // 剩余资金：20

    [Header("按钮")]
    public Button nextDayButton;                    // 进入下一天

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false); // 初始隐藏
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayEnded += HandleDayEnded;
        }
        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(OnNextDayClicked);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayEnded -= HandleDayEnded;
        }
        if (nextDayButton != null)
        {
            nextDayButton.onClick.RemoveListener(OnNextDayClicked);
        }
    }

    private void HandleDayEnded(GameManager.DayEndSummary summary)
    {
        DisplaySummary(summary);
    }

    // 直接显示结算（供 GameManager 主动调用，避免事件订阅时序问题）
    public void DisplaySummary(GameManager.DayEndSummary summary)
    {
        // 填充统计数据
        if (judgementCountText != null)
            judgementCountText.text = $"单子判定数量：{summary.totalJudgements}";

        if (wrongJudgementCountText != null)
            wrongJudgementCountText.text = $"单子判定失误数量：{summary.wrongJudgements}";

        if (todayExpensesText != null)
            todayExpensesText.text = $"今日生活费消耗：{summary.todayExpenses:F0}";

        if (remainingMoneyText != null)
            remainingMoneyText.text = $"剩余资金：{summary.remainingMoney:F0}";

        // 显示面板
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    private void OnNextDayClicked()
    {
        // 进入下一天并隐藏面板
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ProceedToNextDay();
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}