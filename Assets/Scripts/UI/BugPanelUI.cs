using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Screenshot")]
    public Image screenshotImage;
    
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
        // 自动化跑测停用，按钮不绑定
        rejectButton.onClick.AddListener(OnRejectClick);
        approveButton.onClick.AddListener(OnApproveClick);
        
        // 注册BUG管理器事件
        BugManager.Instance.OnBugChanged += UpdateBugInfo;
        BugManager.Instance.OnTestProgressUpdated += UpdateTestProgress;
    }
    
    private void UpdateBugInfo(BugSubmission bug)
    {
        if (bug == null) return;
        
        titleText.text = BugManager.Instance != null ? BugManager.Instance.GetCurrentBugTitle() : string.Empty;
        submitterText.text = $"提交人：{bug.submitter}";
        submitTimeText.text = $"提交时间：{bug.submitTime}";
        // 版本号保留 1 位小数（若无法解析则原样显示）
        string formattedVersion = bug.version;
        if (!string.IsNullOrEmpty(bug.version) && float.TryParse(bug.version, NumberStyles.Float, CultureInfo.InvariantCulture, out var verFloat))
        {
            formattedVersion = verFloat.ToString("F1", CultureInfo.InvariantCulture);
        }
        versionText.text = $"提交版本：{formattedVersion}";

        // 更新文件列表
        UpdateFileList(bug.files);
        
        // 更新描述
        descriptionText.text = bug.description;

        // 更新截图
        if (screenshotImage != null)
        {
            var sprite = BugManager.Instance != null ? BugManager.Instance.GetCurrentBugScreenshotSprite() : null;
            if (sprite != null)
            {
                screenshotImage.sprite = sprite;
                screenshotImage.gameObject.SetActive(true);
                var c = screenshotImage.color; c.a = 1f; screenshotImage.color = c;
            }
            else
            {
                screenshotImage.sprite = null;
                screenshotImage.gameObject.SetActive(false);
            }
        }
        
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
        // 自动化跑测停用：隐藏进度条（如需显示，可恢复为原逻辑）
        if (progressBar != null) progressBar.SetActive(false);
    }
    
    private void UpdateButtonStates(BugSubmission bug)
    {
        // 自动化跑测停用，判定按钮根据是否有当前bug决定是否可交互
        bool hasBug = bug != null;
        rejectButton.interactable = hasBug;
        approveButton.interactable = hasBug;
        if (autoTestButton != null) autoTestButton.interactable = false;
    }
    
    private void OnAutoTestClick()
    {
        // 自动化跑测已停用
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