using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BugPanelUI : MonoBehaviour
{
    [Header("Bug Info")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI submitterText;
    public TextMeshProUGUI submitTimeText;
    public TextMeshProUGUI versionText;
    
    [Header("File List")]
    public Transform fileListContent;
    public GameObject fileItemPrefab;
    
    [Header("Description")]
    public TextMeshProUGUI descriptionText;
    
    [Header("Test Progress")]
    public GameObject progressBar;
    public Image progressFill;
    public TextMeshProUGUI progressText;
    
    [Header("Buttons")]
    public Button autoTestButton;
    public Button rejectButton;
    public Button approveButton;
    
    private void Start()
    {
        // 注册按钮事件
        autoTestButton.onClick.AddListener(OnAutoTestClick);
        rejectButton.onClick.AddListener(OnRejectClick);
        approveButton.onClick.AddListener(OnApproveClick);
        
        // 注册BUG管理器事件
        BugManager.Instance.OnBugChanged += UpdateBugInfo;
        BugManager.Instance.OnTestProgressUpdated += UpdateTestProgress;
    }
    
    private void UpdateBugInfo(BugSubmission bug)
    {
        if (bug == null) return;
        
        titleText.text = "标题最多十个字十个字";
        submitterText.text = $"提交人：{bug.submitter}";
        submitTimeText.text = $"提交时间：{bug.submitTime}";
        versionText.text = $"提交版本：{bug.version}";
        
        // 更新文件列表
        UpdateFileList(bug.files);
        
        // 更新描述
        descriptionText.text = bug.description;
        
        // 更新按钮状态
        UpdateButtonStates(bug);
    }
    
    private void UpdateFileList(List<string> files)
    {
        // 清理现有列表
        foreach (Transform child in fileListContent)
        {
            Destroy(child.gameObject);
        }
        
        // 添加新文件项
        foreach (string file in files)
        {
            GameObject item = Instantiate(fileItemPrefab, fileListContent);
            item.GetComponentInChildren<TextMeshProUGUI>().text = file;
        }
    }
    
    private void UpdateTestProgress(float progress)
    {
        progressBar.SetActive(true);
        progressFill.fillAmount = progress;
        progressText.text = $"自动化测试中，预计用时剩余 {(1 - progress) * 100:F0} 秒";
    }
    
    private void UpdateButtonStates(BugSubmission bug)
    {
        bool testCompleted = !bug.isAutomatedTestRunning && bug.testProgress >= 1f;
        
        autoTestButton.interactable = !bug.isAutomatedTestRunning;
        rejectButton.interactable = testCompleted;
        approveButton.interactable = testCompleted;
    }
    
    private void OnAutoTestClick()
    {
        BugManager.Instance.StartAutomatedTest();
    }
    
    private void OnRejectClick()
    {
        BugManager.Instance.RejectBug();
    }
    
    private void OnApproveClick()
    {
        BugManager.Instance.ApproveBug();
    }
}