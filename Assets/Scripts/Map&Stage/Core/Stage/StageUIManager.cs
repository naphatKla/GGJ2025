using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class StageUIManager : MonoBehaviour
{
    [FoldoutGroup("Stage UI")] [SerializeField]
    private TMP_Text stageText;
    
    [FoldoutGroup("Stage UI")] [SerializeField]
    private TMP_Text killquotaText;
    
    [SerializeField] private StageManager _stageManager;
    
    private void Awake()
    {
        _stageManager = GetComponent<StageManager>();
        _stageManager.OnStageUpdated.AddListener(UpdateStageText);
        _stageManager.OnKillQuotaUpdated.AddListener(UpdateKillQuotaText);
    }
    
    /// <summary>
    /// Update stage index
    /// </summary>
    public void UpdateStageText(int currentStage, int totalStages)
    {
        if (stageText == null) return;
        stageText.text = "Stage: " + (currentStage + 1) + " / " + totalStages;
    }
    
    /// <summary>
    /// Update kill quota
    /// </summary>
    public void UpdateKillQuotaText(float currentKills, float quota)
    {
        if (killquotaText == null) return;
        killquotaText.text = "Kill: " + currentKills + "/" + quota;
    }
}
