using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EmailPanelUI : MonoBehaviour
{
    [Header("Email List")]
    public Transform emailListContent;
    public GameObject emailItemPrefab;
    
    [Header("Email Detail")]
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI subjectText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI contentText;
    public GameObject ruleBookButton;
    
    [Header("Close Button")]
    public Button closeButton;
    
    private void Start()
    {
        // 注册事件
        EmailManager.Instance.OnEmailsUpdated += UpdateEmailList;
        EmailManager.Instance.OnEmailSelected += ShowEmailDetail;
        
        // 注册关闭按钮
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        
        // 初始化邮件列表
        UpdateEmailList(EmailManager.Instance.emails);
    }
    
    private void UpdateEmailList(List<Email> emails)
    {
        // 清理现有列表
        foreach (Transform child in emailListContent)
        {
            Destroy(child.gameObject);
        }
        
        // 添加邮件项
        for (int i = 0; i < emails.Count; i++)
        {
            Email email = emails[i];
            GameObject item = Instantiate(emailItemPrefab, emailListContent);
            
            // 设置邮件项UI
            EmailItemUI itemUI = item.GetComponent<EmailItemUI>();
            if (itemUI != null)
            {
                itemUI.SetupEmail(email, i);
                itemUI.OnClick += () => EmailManager.Instance.SelectEmail(i);
            }
            
            // 设置未读标记
            if (item.transform.Find("UnreadMark") != null)
            {
                item.transform.Find("UnreadMark").gameObject.SetActive(!email.isRead);
            }
        }
    }
    
    private void ShowEmailDetail(Email email)
    {
        if (email == null) return;
        
        senderText.text = $"发件人：{email.sender}";
        subjectText.text = email.subject;
        dateText.text = email.date;
        contentText.text = email.content;
        
        // 显示/隐藏规则书按钮
        ruleBookButton.SetActive(email.hasRuleBook);
    }
}

// 邮件项UI组件
public class EmailItemUI : MonoBehaviour
{
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI subjectText;
    public TextMeshProUGUI dateText;
    public GameObject unreadMark;
    public Button button;
    
    public System.Action OnClick;
    
    private void Start()
    {
        button.onClick.AddListener(() => OnClick?.Invoke());
    }
    
    public void SetupEmail(Email email, int index)
    {
        senderText.text = email.sender;
        subjectText.text = email.subject;
        dateText.text = email.date;
        unreadMark.SetActive(!email.isRead);
    }
}