using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BugSubmission
{
    public string submitter;
    public string submitTime;
    public string version;
    public List<string> files;
    public string description;
    public bool isAutomatedTestRunning;
    public float testProgress;
    public bool isApproved;
}

public class BugManager : MonoBehaviour
{
    public static BugManager Instance { get; private set; }
    
    public BugSubmission currentBug;
    public event Action<BugSubmission> OnBugChanged;
    public event Action<float> OnTestProgressUpdated;
    
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

    public void StartAutomatedTest()
    {
        // 自动化跑测功能暂未启用，保留接口但不执行任何逻辑
        UnityEngine.Debug.Log("[BugManager] 自动化跑测暂未启用。");
        return;
    }

    private IEnumerator RunAutomatedTest()
    {
        // 自动化跑测功能暂未启用
        yield break;
    }

    public void ApproveBug()
    {
        if (currentBug != null)
        {
            currentBug.isApproved = true;
            GameManager.Instance.totalJudgements++;
            bool isWrong = false;
            if (TryGetCurrentBugExtras(out var extras))
            {
                isWrong = (true != extras.ShouldApprove);
                if (isWrong)
                {
                    GameManager.Instance.wrongJudgements++;
                    // 贿赂改为当日累计，结算时统一加
                    GameManager.Instance.bribeToday += Mathf.Max(0f, extras.BounsIfWrong);
                    // 系统邮件改为延迟发送
                    TryScheduleReportEmail(extras.ReportEmail_ID);
                }
            }
            OnBugChanged?.Invoke(currentBug);
            OnJudgementConfirmed?.Invoke(isWrong); // 明确的判定完成事件
        }
    }

    public void RejectBug()
    {
        if (currentBug != null)
        {
            currentBug.isApproved = false;
            GameManager.Instance.totalJudgements++;
            bool isWrong = false;
            if (TryGetCurrentBugExtras(out var extras))
            {
                isWrong = (false != extras.ShouldApprove);
                if (isWrong)
                {
                    GameManager.Instance.wrongJudgements++;
                    GameManager.Instance.bribeToday += Mathf.Max(0f, extras.BounsIfWrong);
                    TryScheduleReportEmail(extras.ReportEmail_ID);
                }
            }
            OnBugChanged?.Invoke(currentBug);
            OnJudgementConfirmed?.Invoke(isWrong); // 明确的判定完成事件
        }
    }

    // 明确的判定完成事件（参数为是否判错）
    public event System.Action<bool> OnJudgementConfirmed;
    
    // ========== 映射工具：从 DataStoreManager 的原始记录生成 BugSubmission，并保留额外字段 ==========

    // 旁路存储：保存与运行时类未对齐但业务需要的字段，按 Bug ID 记录
    [System.Serializable]
    public class BugExtraInfo
    {
        public string ID;
        public string Title;
        public string ScreenShot_ID;
        public bool ShouldApprove;
        public string ReportEmail_ID;
        public float BounsIfWrong;
    }

    private readonly Dictionary<string, BugExtraInfo> _extrasById = new Dictionary<string, BugExtraInfo>();
    private string _currentBugId = null;

    // 将原始记录映射为运行时 BugSubmission（不改变现有字段结构）
    public BugSubmission MapFromRaw(BugRawRecord src)
    {
        if (src == null) return null;

        // 保存额外信息（供后续判断与邮件关联使用）
        if (!string.IsNullOrEmpty(src.ID))
        {
            _extrasById[src.ID] = new BugExtraInfo
            {
                ID = src.ID,
                Title = src.Title,
                ScreenShot_ID = src.ScreenShot_ID,
                ShouldApprove = src.ShouldApprove,
                ReportEmail_ID = src.ReportEmail_ID,
                BounsIfWrong = src.BounsIfWrong,
            };
        }

        return new BugSubmission
        {
            submitter = src.Name,
            submitTime = src.Submit_Time,
            version = src.Version,
            description = src.Description,
            files = src.Files != null ? new List<string>(src.Files) : new List<string>(),
            isAutomatedTestRunning = false,
            testProgress = 0f,
            isApproved = false,
        };
    }

    // 便捷：按 ID 从 DataStoreManager 取原始记录并设为 currentBug
    public bool SetCurrentBugById(string id)
    {
        var raw = DataStoreManager.Instance != null ? DataStoreManager.Instance.GetBugById(id) : null;
        if (raw == null) return false;
        currentBug = MapFromRaw(raw);
        _currentBugId = raw.ID;
        OnBugChanged?.Invoke(currentBug);
        return true;
    }

    // 获取当前 Bug 的额外信息（若存在）
    public bool TryGetCurrentBugExtras(out BugExtraInfo extras)
    {
        extras = null;
        if (currentBug == null || string.IsNullOrEmpty(_currentBugId)) return false;
        return _extrasById.TryGetValue(_currentBugId, out extras);
    }

    // 提供 Title 获取（供 UI 使用）
    public string GetCurrentBugTitle()
    {
        return TryGetCurrentBugExtras(out var ex) ? (ex.Title ?? string.Empty) : string.Empty;
    }

    // 提供 Screenshot Sprite 获取（Resources/Screenshots/ScreenShot_ID）
    public Sprite GetCurrentBugScreenshotSprite()
    {
        if (TryGetCurrentBugExtras(out var ex) && !string.IsNullOrEmpty(ex.ScreenShot_ID))
        {
            return Resources.Load<Sprite>("Screenshots/" + ex.ScreenShot_ID);
        }
        return null;
    }

    // 延迟发送系统邮件：根据 ReportEmail_ID 从 DataStore 取原始记录并注入到 EmailManager
    private void TryScheduleReportEmail(string emailId)
    {
        if (string.IsNullOrEmpty(emailId)) return;
        if (DataStoreManager.Instance == null) return;
        var raw = DataStoreManager.Instance.GetEmailById(emailId);
        if (raw == null) return;
        StartCoroutine(SendReportEmailDelayed(raw));
    }

    private IEnumerator SendReportEmailDelayed(EmailRawRecord raw)
    {
        if (GameManager.Instance == null)
            yield break;
        float realDelay = GameManager.Instance.GameMinutesToRealSeconds(GameManager.Instance.delayedSystemMailMinutes);
        yield return new WaitForSeconds(realDelay);

        var email = MapEmailFromRaw(raw);
        if (EmailManager.Instance != null)
        {
            EmailManager.Instance.AddEmail(email);
        }
    }

    // 将原始 Email 记录映射为运行时 Email（就地工具函数）
    private Email MapEmailFromRaw(EmailRawRecord src)
    {
        if (src == null) return null;
        return new Email
        {
            sender = src.Sender,
            subject = src.Title,
            content = src.Text,
            date = System.DateTime.Now.ToString("MM月dd日"),
            isRead = false,
            hasRuleBook = false,
        };
    }
}