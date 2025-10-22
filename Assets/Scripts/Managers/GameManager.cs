using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // 游戏状态
    public float remainingMoney = 100f;
    public int currentDay = 1;
    public float currentTime = 600f; // 10:00开始
    
    // 统计数据
    public int totalJudgements = 0;
    public int wrongJudgements = 0;
    public float todayExpenses = 0f;

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

    private void Update()
    {
        UpdateGameTime();
    }

    private void UpdateGameTime()
    {
        currentTime += Time.deltaTime;
        // 检查是否需要结束当天
        if (currentTime >= 1080f) // 18:00
        {
            EndDay();
        }
    }

    public void EndDay()
    {
        currentDay++;
        currentTime = 600f; // 重置到10:00
        // 结算当天数据
        CalculateDayEnd();
    }

    private void CalculateDayEnd()
    {
        remainingMoney -= todayExpenses;
        todayExpenses = 0f;
        // 触发UI更新等
    }

    public string GetCurrentTimeString()
    {
        int hours = Mathf.FloorToInt(currentTime / 60f);
        int minutes = Mathf.FloorToInt(currentTime % 60f);
        return $"{hours:00}:{minutes:00}";
    }
}