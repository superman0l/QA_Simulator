using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// 邮件项UI组件
public class EmailItemUI : MonoBehaviour
{
    [Header("Email Item Detail")]
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI subjectText;
    //public TextMeshProUGUI dateText;
    public GameObject unreadMark;
    public Button button;

    public System.Action OnClick;

    private void Start()
    {
        button.onClick.AddListener(() => OnClick?.Invoke());
    }

    public void SetupEmail(Email email, int index)
    {
        senderText.text = email.sender;
        subjectText.text = email.subject;
        //dateText.text = email.date;
        unreadMark.SetActive(!email.isRead);
    }
}