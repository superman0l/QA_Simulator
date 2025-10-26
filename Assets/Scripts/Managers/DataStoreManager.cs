using UnityEngine;
using System.Collections.Generic;

// JSON 容器类（根键与输出一致）
[System.Serializable]
public class BugsDatabase { public List<BugRawRecord> items; }

[System.Serializable]
public class EmailsDatabase { public List<EmailRawRecord> items; }

[System.Serializable]
public class LevelsDatabase { public List<LevelRawRecord> levels; }

// 与 Excel/JSON 表头一一对应的原始记录类型
[System.Serializable]
public class BugRawRecord
{
    public string ID;
    public string Title;
    public string Name;
    public string Submit_Time;
    public string Version;
    public List<string> Files;
    public string Description;
    public string ScreenShot_ID;
    public bool ShouldApprove;        // 已在转换器做布尔识别
    public string ReportEmail_ID;
    public float BounsIfWrong;        // 若表中为空，将解析为 0
}

[System.Serializable]
public class EmailRawRecord
{
    public string ID;
    public string Sender;
    public string Title;
    public string Text;
}

[System.Serializable]
public class LevelRawRecord
{
    public int LevelNum;                         // 若 Excel 单元格是数值将正确解析
    public List<string> EmailBeforeWork;         // 逗号分隔已由转换器拆分为数组
    public List<string> BugsForToday;            // 同上
    public float LevelTime;                      // 关卡时长（秒），来自 levels.json
}

// 集中数据存储与加载管理器
public class DataStoreManager : MonoBehaviour
{
    public static DataStoreManager Instance { get; private set; }

    // 原始数据列表
    public List<BugRawRecord> Bugs = new List<BugRawRecord>();
    public List<EmailRawRecord> Emails = new List<EmailRawRecord>();
    public List<LevelRawRecord> Levels = new List<LevelRawRecord>();

    // 索引字典，便于按键快速查找
    public Dictionary<string, BugRawRecord> BugsById = new Dictionary<string, BugRawRecord>();
    public Dictionary<string, EmailRawRecord> EmailsById = new Dictionary<string, EmailRawRecord>();
    public Dictionary<int, LevelRawRecord> LevelsByNum = new Dictionary<int, LevelRawRecord>();

    public bool IsLoaded { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 从 Resources/Data 目录加载 JSON
    public void LoadFromResources()
    {
        LoadBugs();
        LoadEmails();
        LoadLevels();
        IsLoaded = true;
        Debug.Log($"[DataStore] 加载完成：bugs={Bugs.Count}, emails={Emails.Count}, levels={Levels.Count}");
    }

    private void LoadBugs()
    {
        var ta = Resources.Load<TextAsset>("Data/bugs");
        if (ta == null)
        {
            Debug.LogWarning("[DataStore] 未找到 Data/bugs.json（Resources）");
            return;
        }
        var db = JsonUtility.FromJson<BugsDatabase>(ta.text);
        if (db != null && db.items != null)
        {
            Bugs = db.items;
            BugsById.Clear();
            foreach (var b in Bugs)
            {
                if (!string.IsNullOrEmpty(b.ID))
                {
                    if (!BugsById.ContainsKey(b.ID))
                        BugsById.Add(b.ID, b);
                    else
                        Debug.LogWarning($"[DataStore] Bug ID 重复：{b.ID}");
                }
            }
        }
    }

    private void LoadEmails()
    {
        var ta = Resources.Load<TextAsset>("Data/emails");
        if (ta == null)
        {
            Debug.LogWarning("[DataStore] 未找到 Data/emails.json（Resources）");
            return;
        }
        var db = JsonUtility.FromJson<EmailsDatabase>(ta.text);
        if (db != null && db.items != null)
        {
            Emails = db.items;
            EmailsById.Clear();
            foreach (var e in Emails)
            {
                if (!string.IsNullOrEmpty(e.ID))
                {
                    if (!EmailsById.ContainsKey(e.ID))
                        EmailsById.Add(e.ID, e);
                    else
                        Debug.LogWarning($"[DataStore] Email ID 重复：{e.ID}");
                }
            }
        }
    }

    private void LoadLevels()
    {
        var ta = Resources.Load<TextAsset>("Data/levels");
        if (ta == null)
        {
            Debug.LogWarning("[DataStore] 未找到 Data/levels.json（Resources）");
            return;
        }
        var db = JsonUtility.FromJson<LevelsDatabase>(ta.text);
        if (db != null && db.levels != null)
        {
            Levels = db.levels;
            LevelsByNum.Clear();
            foreach (var lv in Levels)
            {
                if (!LevelsByNum.ContainsKey(lv.LevelNum))
                    LevelsByNum.Add(lv.LevelNum, lv);
                else
                    Debug.LogWarning($"[DataStore] LevelNum 重复：{lv.LevelNum}");
            }
        }
    }

    // 便捷查询接口（其他 Managers 可使用）
    public BugRawRecord GetBugById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        BugsById.TryGetValue(id, out var rec);
        return rec;
    }

    public EmailRawRecord GetEmailById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EmailsById.TryGetValue(id, out var rec);
        return rec;
    }

    public LevelRawRecord GetLevelByNum(int levelNum)
    {
        LevelsByNum.TryGetValue(levelNum, out var rec);
        return rec;
    }
}