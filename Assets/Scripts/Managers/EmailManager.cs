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

    // 记录已注入的原始 Email ID，避免重复注入
    private HashSet<string> _addedEmailIds = new HashSet<string>();
    
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

    // ========== 就地映射与注入：从 DataStoreManager 读取邮件配置 ==========

    // 将原始 Email 记录映射为运行时 Email
    public Email MapFromRaw(EmailRawRecord src)
    {
        if (src == null) return null;
        return new Email
        {
            sender = src.Sender,
            subject = src.Title,
            content = src.Text,
            date = System.DateTime.Now.ToString("MM月dd日"), // Excel 未提供日期，这里使用当前日期
            isRead = false,
            hasRuleBook = false,
        };
    }

    // 按 ID 添加单封邮件（避免重复注入同一 ID）
    public bool AddEmailById(string id, bool markRead = false)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (_addedEmailIds.Contains(id)) return false;
        if (DataStoreManager.Instance == null) return false;

        var raw = DataStoreManager.Instance.GetEmailById(id);
        if (raw == null) return false;

        var email = MapFromRaw(raw);
        if (email == null) return false;
        if (markRead) email.isRead = true;

        _addedEmailIds.Add(id);
        AddEmail(email);
        return true;
    }

    // 批量按 ID 列表添加邮件，返回成功添加数量
    public int AddEmailsByIds(IEnumerable<string> ids, bool markRead = false)
    {
        if (ids == null) return 0;
        int count = 0;
        foreach (var id in ids)
        {
            if (AddEmailById(id, markRead)) count++;
        }
        return count;
    }

    // 根据关卡配置注入“开工前邮件”
    public int InjectPreworkEmailsForLevel(int levelNum)
    {
        if (DataStoreManager.Instance == null) return 0;
        var lv = DataStoreManager.Instance.GetLevelByNum(levelNum);
        if (lv == null || lv.EmailBeforeWork == null) return 0;
        return AddEmailsByIds(lv.EmailBeforeWork, markRead: false);
    }

    // 可选：清空收件箱与已注入 ID（用于调试或重新开始关卡）
    public void ClearMailbox()
    {
        emails.Clear();
        _addedEmailIds.Clear();
        OnEmailsUpdated?.Invoke(emails);
    }
}