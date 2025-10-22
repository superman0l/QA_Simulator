using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject bugPanel;
    public GameObject emailPanel;
    public GameObject ruleBookPanel;
    
    [Header("Top Bar")]
    public TextMeshProUGUI remainingMoneyText;
    
    [Header("Bottom Bar")]
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;
    
    [Header("Navigation Buttons")]
    public Button emailButton;
    public Button ruleBookButton;
    public Button shutdownButton;
    public GameObject emailNotification;
    
    private void Start()
    {
        // 注册按钮事件
        emailButton.onClick.AddListener(ShowEmailPanel);
        ruleBookButton.onClick.AddListener(ShowRuleBookPanel);
        shutdownButton.onClick.AddListener(OnShutdown);
        
        // 注册事件监听
        EmailManager.Instance.OnEmailsUpdated += UpdateEmailNotification;
        
        // 默认显示BUG面板
        ShowBugPanel();
        
        // 开始更新UI
        InvokeRepeating("UpdateTimeDisplay", 0f, 1f);
    }
    
    private void UpdateTimeDisplay()
    {
        if (GameManager.Instance != null)
        {
            timeText.text = GameManager.Instance.GetCurrentTimeString();
            dayText.text = $"第{GameManager.Instance.currentDay}天";
            remainingMoneyText.text = $"剩余资金: {GameManager.Instance.remainingMoney:F0}";
        }
    }
    
    private void UpdateEmailNotification(System.Collections.Generic.List<Email> emails)
    {
        int unreadCount = EmailManager.Instance.GetUnreadCount();
        emailNotification.SetActive(unreadCount > 0);
    }
    
    public void ShowBugPanel()
    {
        bugPanel.SetActive(true);
        emailPanel.SetActive(false);
        ruleBookPanel.SetActive(false);
    }
    
    public void ShowEmailPanel()
    {
        bugPanel.SetActive(false);
        emailPanel.SetActive(true);
        ruleBookPanel.SetActive(false);
    }
    
    public void ShowRuleBookPanel()
    {
        bugPanel.SetActive(false);
        emailPanel.SetActive(false);
        ruleBookPanel.SetActive(true);
    }
    
    private void OnShutdown()
    {
        // 触发下班结算
        GameManager.Instance.EndDay();
    }
}