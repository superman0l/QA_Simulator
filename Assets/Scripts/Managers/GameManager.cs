using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections; // 新增：用于协程

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // 游戏状态
    public float remainingMoney = 100f;
    public int currentDay = 1;
    public float currentTime = 600f; // 10:00开始（分钟）
    
    // 统计数据（当日累积）
    public int totalJudgements = 0;
    public int wrongJudgements = 0;
    public float todayExpenses = 0f;
    public float bribeToday = 0f; // 当日收到的贿赂，日终统一结算

    // 全流程统计（跨天）
    public int globalTotalJudgements = 0;
    public int globalWrongJudgements = 0;

    [Header("结算参数（Inspector 可调）")]
    public float initialBalance = 50f;              // 初始余额
    public float perBugReward = 15f;                // 每单单价
    public float fullCorrectBonus = 20f;            // 全正确绩效
    public float dailyLivingCost = 75f;             // 每日生活费
    public List<float> wrongPenaltyLadder = new List<float>{0f, 0f, -20f, -60f, -130f};
    public int firedAtWrongCount = 5;               // 当日错误次数达到该值及以上：被开除
    [Range(0f, 1f)] public float globalAccuracyThreshold = 0.8f; // 结局阈值（预留）

    [Header("系统邮件（延迟发送-游戏分钟）")]
    public float delayedSystemMailMinutes = 20f;    // 默认20游戏分钟

    [Header("结局预留（当前不启用）")]
    public List<string> criticalBugIds = new List<string>(); // 用于“有人被开除”判断的关键ID

    [Header("UI References")]
    public DayEndPanelUI dayEndPanelUI; // 允许在 Inspector 直接拖引用；为空时运行期查找

    [Header("时间设置")]
    [Tooltip("游戏内时间步长（分钟）。例如30表示每次跳30分钟")]
    public int gameTimeStepMinutes = 30;
    [Tooltip("每次时间跳变的UI过渡时长（秒）")]
    public float timeTickAnimDuration = 0.25f;

    // 日终结算事件（UI可订阅显示面板）
    public struct DayEndSummary
    {
        public int totalJudgements;
        public int wrongJudgements;
        public float todayExpenses;
        public float remainingMoney;
        public bool isFiredEnding;      // 达到当日错误上限
        public bool isBankruptEnding;   // 余额不足以维持生活
    }
    public event Action<DayEndSummary> OnDayEnded;
    private bool awaitingNextDay = false;

    // ========== 关卡/时间流逝逻辑 ==========
    public float defaultLevelTimeSeconds = 600f; // 默认一关现实600秒（10分钟），若关卡未配置 LevelTime 时使用
    private float currentLevelTimeSeconds;
    private float minutesPerRealSecond; // 每现实秒流逝的"游戏内分钟"

    private const float StartMinuteNormal = 600f;   // 10:00
    private const float StartMinutePunished = 840f; // 14:00（受罚次日）
    private const float EndMinute = 1560f;          // 次日02:00（26:00）
    private const float MidnightMinute = 1440f;     // 当日24:00（00:00）

    private bool punishNextDayFlag = false; // 当日跨过00:00仍未完成，次日受罚（75%时长，14:00开始）
    private bool midnightChecked = false;
    private bool allBugsCompleted = false;
    private float lastCompletionMinute = -1f;

    // 时间步长累积器（现实秒）
    private float realSecondsPerTimeStep = 0f;
    private float realTimeAccumulator = 0f;
    
    // 时间显示平滑
    private float displayTime = 600f; // UI显示使用的时间（分钟）
    private float tickAnimStartRealTime = -1f;
    private float displayStartTimeOnAnim = 600f;
    private float nextTickRealTime = -1f;

    // 稳态步进（基于绝对时间）
    private double dayStartRealTime = 0; // 当天开始时的真实时间（unscaledTime）
    private int stepsDone = 0;           // 已执行的步数

    // 当日 bug 队列（来自 levels.json）
    private List<string> todaysBugIds = new List<string>();
    private int currentBugIndex = 0;

    // 关卡数量（用于判断最后一天）
    private int maxLevelNum = 1;
    private bool maxLevelComputed = false;
    public bool IsLastDay() { return currentDay >= maxLevelNum; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 初始化余额
            remainingMoney = initialBalance;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 使用更明确的判定完成事件
        if (BugManager.Instance != null)
        {
            BugManager.Instance.OnJudgementConfirmed += OnJudgementConfirmedHandler;
        }
        // 不直接开始一天，等待数据加载与各Manager就绪
        StartCoroutine(WaitForDataThenStartDay());
    }

    private IEnumerator WaitForDataThenStartDay()
    {
        // 等待 DataStoreManager 加载完成
        while (DataStoreManager.Instance == null)
            yield return null;
        while (!DataStoreManager.Instance.IsLoaded)
            yield return null;

        // 等待 EmailManager/BugManager 就绪（避免注入失败）
        while (EmailManager.Instance == null)
            yield return null;
        while (BugManager.Instance == null)
            yield return null;

        Debug.Log("[GameManager] Data & Managers ready. Starting day...");
        StartDay();
    }

    private void OnDestroy()
    {
        if (BugManager.Instance != null)
        {
            BugManager.Instance.OnJudgementConfirmed -= OnJudgementConfirmedHandler;
        }
    }

    private void Update()
    {
        if (awaitingNextDay) return; // 等待玩家确认进入下一天，暂停时间推进
        UpdateGameTime();
    }

    // 将游戏分钟换算为现实秒（供延迟系统邮件等使用）
    public float GameMinutesToRealSeconds(float minutes)
    {
        return minutes / Mathf.Max(0.0001f, minutesPerRealSecond);
    }

    public event System.Action<int> OnTimeTick; // parameter: currentTime in minutes

    private void UpdateGameTime()
    {
        // 稳态步进： derive exact number of steps from absolute time; process all outstanding steps.
        if (realSecondsPerTimeStep <= 0f) return;

        double elapsed = Time.unscaledTime - dayStartRealTime;
        int shouldSteps = Mathf.FloorToInt((float)(elapsed / realSecondsPerTimeStep));

        if (shouldSteps > stepsDone)
        {
            int toProcess = shouldSteps - stepsDone;
            for (int k = 0; k < toProcess; k++)
            {
                // 推进逻辑时间
                currentTime += gameTimeStepMinutes;

                // 同步显示时间 + 触发时间跳变事件
                displayTime = currentTime;
                OnTimeTick?.Invoke((int)currentTime);

                // 午夜检查（设置次日受罚）
                if (!midnightChecked && currentTime >= MidnightMinute)
                {
                    midnightChecked = true;
                    if (!allBugsCompleted)
                    {
                        punishNextDayFlag = true;
                    }
                }

                // 到达日终
                if (currentTime >= EndMinute)
                {
                    currentTime = EndMinute; // 对齐到终点
                    stepsDone = shouldSteps;
                    EndDay();
                    return;
                }
            }

            stepsDone = shouldSteps;
        }
    }

    // 开始新的一天：加载关卡配置、投放邮箱与首个bug、初始化时间与流速
    public void StartDay()
    {
        // 计算最大关卡编号（仅计算一次）
        if (!maxLevelComputed && DataStoreManager.Instance != null)
        {
            maxLevelNum = 1;
            var levels = DataStoreManager.Instance.Levels;
            if (levels != null)
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    if (levels[i] != null && levels[i].LevelNum > maxLevelNum)
                        maxLevelNum = levels[i].LevelNum;
                }
            }
            maxLevelComputed = true;
            Debug.Log($"[GameManager] MaxLevelNum computed: {maxLevelNum}");
        }

        // 获取当前关卡配置
        LevelRawRecord lv = DataStoreManager.Instance != null ? DataStoreManager.Instance.GetLevelByNum(currentDay) : null;
        if (lv == null)
        {
            Debug.LogWarning($"[GameManager] LevelRawRecord not found for day {currentDay}. Emails/Bugs won't be injected.");
        }

        // 关卡时长：优先使用 levels.json 的 LevelTime（秒）；否则使用默认值
        float normalSecs = (lv != null && lv.LevelTime > 0f) ? lv.LevelTime : Mathf.Max(1f, defaultLevelTimeSeconds);
        currentLevelTimeSeconds = punishNextDayFlag ? normalSecs * 0.75f : normalSecs;
        minutesPerRealSecond = 960f / currentLevelTimeSeconds; // 10:00→次日02:00为960分钟

        // 初始化固定步长：每步对应的现实秒
        realSecondsPerTimeStep = (float)gameTimeStepMinutes / Mathf.Max(0.0001f, minutesPerRealSecond);
        realTimeAccumulator = 0f; // 已弃用，可保留

        Debug.Log($"[GameManager] Day {currentDay} LevelTime={normalSecs}s, punished={(punishNextDayFlag ? "yes" : "no")}, minutesPerRealSecond={minutesPerRealSecond:F2}, step={gameTimeStepMinutes}min ~= {realSecondsPerTimeStep:F2}s");

        // 初始化当日开始时间（受罚从14:00开始，否则10:00）
        currentTime = punishNextDayFlag ? StartMinutePunished : StartMinuteNormal;
        // 初始化显示与步进基准
        displayTime = currentTime;
        tickAnimStartRealTime = -1f;
        displayStartTimeOnAnim = displayTime;
        dayStartRealTime = Time.unscaledTime;
        stepsDone = 0;

        // 重置标志
        awaitingNextDay = false;
        midnightChecked = false;
        allBugsCompleted = false;
        lastCompletionMinute = -1f;

        // 投放"开工前邮件"（如果 EmailManager 尚未就绪，等待它就绪再注入）
        if (EmailManager.Instance != null)
        {
            int count = EmailManager.Instance.InjectPreworkEmailsForLevel(currentDay);
            Debug.Log($"[GameManager] Injected prework emails: {count}");
        }
        else
        {
            StartCoroutine(InjectPreworkEmailsWhenReady(currentDay));
        }

        // 清理当日统计（用于UI展示），全局统计不清理
        totalJudgements = 0;
        wrongJudgements = 0;
        todayExpenses = 0f;
        bribeToday = 0f;

        // 设置当日 bug 队列并投放首个
        todaysBugIds.Clear();
        currentBugIndex = 0;
        if (lv != null && lv.BugsForToday != null && lv.BugsForToday.Count > 0)
        {
            todaysBugIds.AddRange(lv.BugsForToday);
            Debug.Log($"[GameManager] Today's bugs count: {todaysBugIds.Count}");
            if (BugManager.Instance != null)
            {
                bool ok = BugManager.Instance.SetCurrentBugById(todaysBugIds[0]);
                Debug.Log($"[GameManager] Set first bug {todaysBugIds[0]} success={ok}");
            }
            else
            {
                StartCoroutine(SetFirstBugWhenReady(todaysBugIds[0]));
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] No bugs configured for today or Level is null.");
        }

        // 次日受罚标志在启动新的一天后清除（已应用）
        punishNextDayFlag = false;

        // 立即同步一次UI显示（例如刚进入新的一天）
        OnTimeTick?.Invoke((int)currentTime);
    }

    private IEnumerator InjectPreworkEmailsWhenReady(int day)
    {
        while (EmailManager.Instance == null)
            yield return null;
        int count = EmailManager.Instance.InjectPreworkEmailsForLevel(day);
        Debug.Log($"[GameManager] Injected prework emails (delayed): {count}");
    }

    private IEnumerator SetFirstBugWhenReady(string bugId)
    {
        while (BugManager.Instance == null)
            yield return null;
        bool ok = BugManager.Instance.SetCurrentBugById(bugId);
        Debug.Log($"[GameManager] Set first bug (delayed) {bugId} success={ok}");
    }

    // 明确的判定完成事件（来自 BugManager）
    private void OnJudgementConfirmedHandler(bool isWrong)
    {
        // 维护全局统计
        globalTotalJudgements++;
        if (isWrong) globalWrongJudgements++;

        if (todaysBugIds.Count == 0) return;

        // 推进到下一条 bug；完成则结束当天
        currentBugIndex++;
        if (currentBugIndex < todaysBugIds.Count)
        {
            if (BugManager.Instance != null)
            {
                BugManager.Instance.SetCurrentBugById(todaysBugIds[currentBugIndex]);
            }
        }
        else
        {
            allBugsCompleted = true;
            lastCompletionMinute = currentTime;
            EndDay();
        }
    }

    public void EndDay()
    {
        // 结算当天数据（使用策划公式）
        CalculateDayEnd();

        // 若当日未完成，或在00:00后才完成，则次日受罚（若未在午夜时刻设置过惩罚，这里兜底）
        if (!allBugsCompleted || lastCompletionMinute >= MidnightMinute)
        {
            punishNextDayFlag = true;
        }

        // 触发日终结算事件（UI显示结算面板）
        var summary = new DayEndSummary
        {
            totalJudgements = totalJudgements,
            wrongJudgements = wrongJudgements,
            todayExpenses = todayExpenses,
            remainingMoney = remainingMoney,
            isFiredEnding = wrongJudgements >= firedAtWrongCount,
            isBankruptEnding = remainingMoney < 0f,
        };

        // 确保结算面板对象处于激活状态（即便它在层级中是Inactive），并直接显示（避免事件订阅时序问题）
        if (dayEndPanelUI == null)
        {
            dayEndPanelUI = UnityEngine.Object.FindObjectOfType<DayEndPanelUI>(true); // 包含未激活对象
        }
        if (dayEndPanelUI != null)
        {
            if (!dayEndPanelUI.gameObject.activeSelf)
                dayEndPanelUI.gameObject.SetActive(true);
            dayEndPanelUI.DisplaySummary(summary);
        }
        else
        {
            // 没有找到面板则退回事件模式（如果有其它监听者）
            OnDayEnded?.Invoke(summary);
        }

        awaitingNextDay = true; // 暂停时间推进，等待玩家点击"进入下一天"
    }

    public void ProceedToNextDay()
    {
        // 清理当日计数，进入下一天
        totalJudgements = 0;
        wrongJudgements = 0;
        todayExpenses = 0f;
        bribeToday = 0f;

        currentDay++;
        StartDay();
    }

    private float GetWrongPenalty(int wrongCount)
    {
        if (wrongPenaltyLadder == null || wrongPenaltyLadder.Count == 0) return 0f;
        int idx = Mathf.Clamp(wrongCount, 0, wrongPenaltyLadder.Count - 1);
        return wrongPenaltyLadder[idx];
    }

    private void CalculateDayEnd()
    {
        // 当日正确数
        int correctCount = Mathf.Max(0, totalJudgements - wrongJudgements);
        float income = correctCount * perBugReward;
        float fullCorrect = (wrongJudgements == 0 && totalJudgements > 0) ? fullCorrectBonus : 0f;
        float wrongPenalty = GetWrongPenalty(wrongJudgements);

        // 今日生活费用于显示
        todayExpenses = dailyLivingCost;

        // 结算：加入当日贿赂，不再即时入账
        remainingMoney = remainingMoney + income + fullCorrect + wrongPenalty + bribeToday - dailyLivingCost;
    }

    public string GetCurrentTimeString()
    {
        // 将显示时间对齐到步长（例如30分钟），避免UI文本出现非整步值
        int step = Mathf.Max(1, gameTimeStepMinutes);
        int snappedMinutes = Mathf.Clamp(((int)(currentTime / step)) * step, 0, (int)EndMinute);
        int hours = snappedMinutes / 60;
        int minutes = snappedMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }
}