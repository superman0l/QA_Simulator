using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Email
{
    public string sender;
    public string subject;
    public string content;
    public string date;
    public bool isRead;
    public bool hasRuleBook;
}

public class EmailManager : MonoBehaviour
{
    public static EmailManager Instance { get; private set; }
    
    public List<Email> emails = new List<Email>();
    public event Action<List<Email>> OnEmailsUpdated;
    public event Action<Email> OnEmailSelected;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddEmail(Email email)
    {
        emails.Add(email);
        OnEmailsUpdated?.Invoke(emails);
    }

    public void SelectEmail(int index)
    {
        if (index >= 0 && index < emails.Count)
        {
            Email email = emails[index];
            email.isRead = true;
            OnEmailSelected?.Invoke(email);
            OnEmailsUpdated?.Invoke(emails);
        }
    }

    public int GetUnreadCount()
    {
        return emails.Where(e => !e.isRead).Count();
    }

    // 生成测试邮件
    public void GenerateTestEmail()
    {
        Email email = new Email
        {
            sender = "测试部门",
            subject = "新的测试任务",
            content = "请查看附件中的测试用例并进行验证。",
            date = System.DateTime.Now.ToString("MM月dd日"),
            isRead = false,
            hasRuleBook = UnityEngine.Random.value > 0.5f
        };
        
        AddEmail(email);
    }
}