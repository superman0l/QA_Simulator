using UnityEngine;
using System;
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
        if (currentBug != null && !currentBug.isAutomatedTestRunning)
        {
            currentBug.isAutomatedTestRunning = true;
            currentBug.testProgress = 0f;
            // 开始协程进行测试
            StartCoroutine(RunAutomatedTest());
        }
    }

    private System.Collections.IEnumerator RunAutomatedTest()
    {
        float testDuration = UnityEngine.Random.Range(3f, 8f);
        float elapsedTime = 0f;

        while (elapsedTime < testDuration)
        {
            elapsedTime += Time.deltaTime;
            currentBug.testProgress = elapsedTime / testDuration;
            OnTestProgressUpdated?.Invoke(currentBug.testProgress);
            yield return null;
        }

        currentBug.isAutomatedTestRunning = false;
        currentBug.testProgress = 1f;
        OnTestProgressUpdated?.Invoke(1f);
    }

    public void ApproveBug()
    {
        if (currentBug != null)
        {
            currentBug.isApproved = true;
            GameManager.Instance.totalJudgements++;
            // 这里可以添加判断是否正确的逻辑
            OnBugChanged?.Invoke(currentBug);
        }
    }

    public void RejectBug()
    {
        if (currentBug != null)
        {
            currentBug.isApproved = false;
            GameManager.Instance.totalJudgements++;
            // 这里可以添加判断是否正确的逻辑
            OnBugChanged?.Invoke(currentBug);
        }
    }
}