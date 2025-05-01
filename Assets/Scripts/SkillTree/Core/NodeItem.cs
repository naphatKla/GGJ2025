using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector;
#endif

public class NodeItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [ColorUsage(false, true)] public Color colorNotUnlock = new(0.5f, 0.5f, 0.5f, 1f);
    [ColorUsage(false, true)] public Color colorUnlock = new(0f, 1f, 0f, 1f);

    [SerializeField] private SkillNodeSO skillNode;
    private Button button;
    private Image image;
    private TMP_Text nodeText;
    private SkillTreeManager manager;
    private RectTransform rectTransform;
    private Tween hoverTween;

    public SkillNodeSO SkillNode => skillNode;
    public Vector2 Position => rectTransform.anchoredPosition;

    private void Awake()
    {
        CacheComponents();
        InitializeComponents();
    }

    private void CacheComponents()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        nodeText = GetComponentInChildren<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void InitializeComponents()
    {
        if (image != null) image.raycastTarget = true;
        if (nodeText != null) nodeText.raycastTarget = false;
    }

    public void Initialize(SkillTreeManager skillTreeManager)
    {
        manager = skillTreeManager;

        if (skillNode != null)
        {
            UpdateNodeText();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => manager.TryUnlockNode(skillNode.Id));
            }

            UpdateVisual(skillNode.IsStartingNode);
        }
    }


    private void UpdateNodeText()
    {
        if (nodeText != null) nodeText.text = $"{skillNode.NodeName}\n{GetNodeEffectText()}";
    }

    public void UpdateVisual(bool isUnlocked)
    {
        if (image != null)
        {
            var color = isUnlocked ? colorUnlock : colorNotUnlock;
            if (color.a < 0.01f)
                color.a = 1f;
            image.color = color;
        }
    }


    private string GetNodeEffectText()
    {
        var operation = skillNode.operationType switch
        {
            SkillNodeSO.OperationType.Add => "+",
            SkillNodeSO.OperationType.Multiply => "x",
            SkillNodeSO.OperationType.AddPercent => "+%",
            _ => ""
        };
        var eventText = skillNode.ActionEvent ? "\n+Custom Event" : "";
        return $"{skillNode.statType} {operation}{skillNode.Value}{eventText}";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        manager?.ShowTooltip(skillNode, rectTransform);

        if (hoverTween != null && hoverTween.IsActive())
            hoverTween.Kill();

        hoverTween = DOTween.Sequence()
            .Append(transform.DOScale(1.1f, 0.3f).SetEase(Ease.InOutSine))
            .Join(transform.DORotate(new Vector3(0, 0, 5), 0.3f).SetEase(Ease.InOutSine))
            .Append(transform.DOScale(1f, 0.3f).SetEase(Ease.InOutSine))
            .Join(transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.InOutSine))
            .SetLoops(-1);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        manager?.HideTooltip();

        if (hoverTween != null && hoverTween.IsActive())
        {
            hoverTween.Kill();
            hoverTween = null;
        }

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
    }


#if UNITY_EDITOR
    [Button("Create Connected Node")]
    private void CreateConnectedNode()
    {
        var manager = Application.isPlaying
            ? SkillTreeManager.Instance
            : FindObjectOfType<SkillTreeManager>();
        if (skillNode == null) return;
        if (manager == null) return;

        // Create new SkillNodeSO
        var newNode = ScriptableObject.CreateInstance<SkillNodeSO>();
        newNode.SetId(GUID.Generate().ToString());
        newNode.NodeName = "New Skill";
        newNode.Description = "Auto-created node.";
        newNode.statType = skillNode.statType;
        newNode.operationType = skillNode.operationType;
        newNode.Value = skillNode.Value;

        // Connect back to this node
        newNode.Connections = new List<SkillNodeSO> { skillNode };

        // Save as asset
        var folderPath = "Assets/GameData/SkillNodes";
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets", "SkillNodes");

        var assetPath = $"{folderPath}/{newNode.NodeName}_{newNode.Id}.asset";
        AssetDatabase.CreateAsset(newNode, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //Add to SkillTreeManager
        manager.AddNode(newNode);

        //Instantiate NodeItem prefab
        var prefab = manager.NodePrefab;
        if (prefab == null) return;

        var content = manager.Content;
        var newNodeGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, content);
        newNodeGO.name = $"Node_{newNode.NodeName}";
        var newRect = newNodeGO.GetComponent<RectTransform>();
        var currentRect = GetComponent<RectTransform>();

        //Set position near current node
        newRect.anchoredPosition = currentRect.anchoredPosition + new Vector2(200f, 0f);

        // Assign SkillNodeSO and initialize
        var newItem = newNodeGO.GetComponent<NodeItem>();
        var soField = typeof(NodeItem).GetField("skillNode", BindingFlags.NonPublic | BindingFlags.Instance);
        soField?.SetValue(newItem, newNode);
        newItem.Initialize(manager);
        newItem.colorUnlock = colorUnlock;
        newItem.colorNotUnlock = colorNotUnlock;

        manager.RefreshAll();
    }

    [Button("Delete This Node")]
    private void DeleteThisNode()
    {
        if (EditorUtility.DisplayDialog("Delete Node", $"Delete node: {skillNode.NodeName}?", "Yes", "Cancel"))
        {
            var manager = Application.isPlaying
                ? SkillTreeManager.Instance
                : FindObjectOfType<SkillTreeManager>();

            if (manager != null && skillNode != null)
            {
                manager.RemoveNode(skillNode);

                var path = AssetDatabase.GetAssetPath(skillNode);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            DestroyImmediate(gameObject);
        }
    }

#endif

    private void OnDrawGizmos()
    {
        if (skillNode == null || skillNode.Connections == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var allNodes = FindObjectsOfType<NodeItem>();
        var startPos = rectTransform.anchoredPosition;

        foreach (var connection in skillNode.Connections)
        {
            var endNode = allNodes.FirstOrDefault(n => n.SkillNode == connection);
            if (endNode != null)
            {
                if (endNode.rectTransform == null)
                    endNode.rectTransform = endNode.GetComponent<RectTransform>();

                var endPos = endNode.rectTransform.anchoredPosition;
                var startWorldPos = canvas.transform.TransformPoint(startPos);
                var endWorldPos = canvas.transform.TransformPoint(endPos);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(startWorldPos, endWorldPos);
            }
        }
    }
}