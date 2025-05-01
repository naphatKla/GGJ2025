using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Skill Node", menuName = "Skill Tree/Skill Node")]
public class SkillNodeSO : ScriptableObject
{
    public string Id { get; private set; }
    public string NodeName;
    [TextArea] public string Description;
    public List<SkillNodeSO> Connections;
    public bool IsStartingNode;

    public enum StatType { Damage, Health, Speed }
    public StatType statType;

    public enum OperationType { Add, Multiply, AddPercent }
    public OperationType operationType;

    public float Value;

    public bool ActionEvent;
    [SerializeField] public UnityEvent OnClick;
    
    public void SetId(string id)
    {
        Id = id;
    }

    public void ApplyEffect(PlayerStats playerStats)
    {
        switch (statType)
        {
            case StatType.Damage:
                break;
            case StatType.Health:
                break;
            case StatType.Speed:
                break;
        }

        if (ActionEvent)
        {
            OnClick?.Invoke();
            Debug.Log($"Triggered UnityEvent for {NodeName}");
        }
    }
    
    private void ApplyOperation(ref float stat, float value, OperationType op, string statName)
    {
        switch (op)
        {
            case OperationType.Add:
                stat += value;
                Debug.Log($"Applied {NodeName}: {statName} +{value}");
                break;
            case OperationType.Multiply:
                stat *= value;
                Debug.Log($"Applied {NodeName}: {statName} *{value}");
                break;
            case OperationType.AddPercent:
                stat += stat * (value / 100f);
                Debug.Log($"Applied {NodeName}: {statName} +{value}%");
                break;
        }
    }
}