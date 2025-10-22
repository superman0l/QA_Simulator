using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RuleBookPanelUI : MonoBehaviour
{
    [Header("Content")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    
    [Header("Navigation")]
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;
    public Button collapseButton;
    
    [Header("Notes")]
    public Transform notesContent;
    public GameObject noteItemPrefab;
    public Button addNoteButton;
    public TMP_InputField noteInput;
    
    private bool isCollapsed = false;
    
    private void Start()
    {
        // 注册按钮事件
        prevButton.onClick.AddListener(OnPrevClick);
        nextButton.onClick.AddListener(OnNextClick);
        closeButton.onClick.AddListener(OnCloseClick);
        collapseButton.onClick.AddListener(OnCollapseClick);
        addNoteButton.onClick.AddListener(OnAddNoteClick);
        
        // 注册规则书事件
        RuleBookManager.Instance.OnPageChanged += UpdatePage;
        
        // 显示初始页面
        UpdatePage(RuleBookManager.Instance.GetCurrentPage());
    }
    
    private void UpdatePage(RuleBookPage page)
    {
        if (page == null) return;
        
        titleText.text = page.title;
        contentText.text = page.content;
        
        // 更新笔记列表
        UpdateNotes(page.notes);
        
        // 更新导航按钮状态
        UpdateNavigationButtons();
    }
    
    private void UpdateNotes(List<string> notes)
    {
        // 清理现有笔记
        foreach (Transform child in notesContent)
        {
            Destroy(child.gameObject);
        }
        
        // 添加笔记项
        foreach (string note in notes)
        {
            GameObject item = Instantiate(noteItemPrefab, notesContent);
            item.GetComponentInChildren<TextMeshProUGUI>().text = note;
        }
    }
    
    private void UpdateNavigationButtons()
    {
        RuleBookManager manager = RuleBookManager.Instance;
        prevButton.interactable = manager.currentPageIndex > 0;
        nextButton.interactable = manager.currentPageIndex < manager.pages.Count - 1;
    }
    
    private void OnPrevClick()
    {
        RuleBookManager.Instance.PreviousPage();
    }
    
    private void OnNextClick()
    {
        RuleBookManager.Instance.NextPage();
    }
    
    private void OnCloseClick()
    {
        gameObject.SetActive(false);
    }
    
    private void OnCollapseClick()
    {
        isCollapsed = !isCollapsed;
        
        // 实现收起/展开动画
        // 这里可以添加动画效果
        contentText.gameObject.SetActive(!isCollapsed);
        notesContent.gameObject.SetActive(!isCollapsed);
    }
    
    private void OnAddNoteClick()
    {
        if (string.IsNullOrEmpty(noteInput.text)) return;
        
        RuleBookManager.Instance.AddNote(noteInput.text);
        noteInput.text = "";
    }
}