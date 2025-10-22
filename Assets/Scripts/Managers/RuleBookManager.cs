using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class RuleBookPage
{
    public string title;
    public string content;
    public List<string> notes;
}

public class RuleBookManager : MonoBehaviour
{
    public static RuleBookManager Instance { get; private set; }
    
    public List<RuleBookPage> pages = new List<RuleBookPage>();
    public int currentPageIndex = 0;
    public event Action<RuleBookPage> OnPageChanged;
    
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
        
        InitializeRuleBook();
    }

    private void InitializeRuleBook()
    {
        // 添加一些示例规则
        pages.Add(new RuleBookPage
        {
            title = "基本规则",
            content = "1. 所有提交必须包含完整的测试用例\n2. 版本号必须符合语义化版本规范\n3. 提交说明必须清晰明确",
            notes = new List<string>()
        });
        
        pages.Add(new RuleBookPage
        {
            title = "测试规范",
            content = "1. 单元测试覆盖率不低于80%\n2. 必须包含集成测试\n3. 性能测试达标",
            notes = new List<string>()
        });
    }

    public void NextPage()
    {
        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;
            OnPageChanged?.Invoke(pages[currentPageIndex]);
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            OnPageChanged?.Invoke(pages[currentPageIndex]);
        }
    }

    public void AddNote(string note)
    {
        if (currentPageIndex >= 0 && currentPageIndex < pages.Count)
        {
            pages[currentPageIndex].notes.Add(note);
            OnPageChanged?.Invoke(pages[currentPageIndex]);
        }
    }

    public RuleBookPage GetCurrentPage()
    {
        if (currentPageIndex >= 0 && currentPageIndex < pages.Count)
        {
            return pages[currentPageIndex];
        }
        return null;
    }
}