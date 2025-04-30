// SkillTreeManager.cs

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    [SerializeField] private List<SkillNodeSO> allNodes;
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private Vector2 tooltipOffset = new(50, -50);
    [SerializeField] private ConnectionRenderer connectionRenderer;
    [SerializeField] private GameObject nodePrefab;

    private readonly Dictionary<string, SkillNodeSO> nodeDict = new();
    private readonly Dictionary<string, bool> nodeUnlockStatus = new();
    private readonly List<NodeItem> nodeItems = new();
    private PlayerStats playerStats;
    private CanvasGroup tooltipCanvasGroup;
    private RectTransform tooltipRect;
    private float lastContentScale = 1f;
    private bool isTooltipActive;
    
    public GameObject NodePrefab => nodePrefab;
    public RectTransform Content => content;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    ///     Called on start; initializes nodes and draws initial connections.
    /// </summary>
    private void Start()
    {
        Initialize();
        UpdateConnections();
    }

    /// <summary>
    ///     Initializes all components and node-related data for the skill tree.
    /// </summary>
    private void Initialize()
    {
        CacheComponents();
        SetupTooltip();
        InitializeNodes();
        InitializeNodeItems();
    }

    /// <summary>
    ///     Caches required components like PlayerStats.
    /// </summary>
    private void CacheComponents()
    {
        playerStats = FindObjectOfType<PlayerStats>();
    }

    /// <summary>
    ///     Prepares the tooltip UI panel and sets default behavior.
    /// </summary>
    private void SetupTooltip()
    {
        if (tooltipPanel == null) return;

        tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>() ?? tooltipPanel.AddComponent<CanvasGroup>();
        tooltipCanvasGroup.blocksRaycasts = false;
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipPanel.SetActive(false);
    }

    /// <summary>
    ///     Initializes the node dictionary and unlock statuses from the list of all nodes.
    /// </summary>
    private void InitializeNodes()
    {
        foreach (var node in allNodes)
        {
            nodeDict[node.Id] = node;
            nodeUnlockStatus[node.Id] = node.IsStartingNode;
        }
    }

    /// <summary>
    ///     Finds all NodeItem components and initializes them.
    /// </summary>
    private void InitializeNodeItems()
    {
        nodeItems.AddRange(content.GetComponentsInChildren<NodeItem>());
        foreach (var nodeItem in nodeItems) nodeItem.Initialize(this);
    }

    /// <summary>
    ///     Updates node and line scales when content scale changes, and moves tooltip with mouse.
    /// </summary>
    private void Update()
    {
        if (!Mathf.Approximately(content.localScale.x, lastContentScale))
        {
            UpdateNodeAndLineScales();
            lastContentScale = content.localScale.x;
        }

        if (isTooltipActive && tooltipRect != null) tooltipRect.position = (Vector2)Input.mousePosition + tooltipOffset;
    }

    /// <summary>
    ///     Displays the tooltip for a given skill node near the mouse pointer.
    /// </summary>
    public void ShowTooltip(SkillNodeSO node, RectTransform nodeRect)
    {
        if (tooltipPanel == null || tooltipText == null || isTooltipActive) return;

        tooltipText.text = $"Name: {node.NodeName}\n" +
                           $"Effect: {node.statType} {GetOperationText(node)}{node.Value}{(node.ActionEvent ? "\nCustom Event: Enabled" : "")}\n" +
                           $"Description: {node.Description}";

        tooltipPanel.SetActive(true);
        isTooltipActive = true;
        tooltipRect.position = (Vector2)Input.mousePosition + tooltipOffset;
    }

    /// <summary>
    ///     Builds the operation text (e.g., +, x, +%) based on the node's operation type.
    /// </summary>
    private string GetOperationText(SkillNodeSO node)
    {
        return node.operationType switch
        {
            SkillNodeSO.OperationType.Add => "+",
            SkillNodeSO.OperationType.Multiply => "x",
            SkillNodeSO.OperationType.AddPercent => "+%",
            _ => ""
        };
    }

    /// <summary>
    ///     Hides the tooltip panel and disables its active state.
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            isTooltipActive = false;
        }
    }

    /// <summary>
    ///     Attempts to unlock the specified skill node if conditions are met.
    /// </summary>
    public void TryUnlockNode(string nodeId)
    {
        if (!nodeDict.TryGetValue(nodeId, out var node)) return;

        var canUnlock = nodeUnlockStatus[nodeId] || node.Connections.Any(c => nodeUnlockStatus[c.Id]);
        if (canUnlock && !nodeUnlockStatus[nodeId])
        {
            nodeUnlockStatus[nodeId] = true;
            node.ApplyEffect(playerStats);
            UpdateVisuals();
            UpdateConnections();
            Debug.Log($"Unlocked: {node.NodeName}");
        }
        else
        {
            Debug.Log("Cannot unlock this node yet!");
        }
    }

    /// <summary>
    ///     Updates visual appearance of all skill nodes based on their unlock status.
    /// </summary>
    private void UpdateVisuals()
    {
        foreach (var nodeItem in nodeItems)
            if (nodeDict.TryGetValue(nodeItem.SkillNode.Id, out var node))
                nodeItem.UpdateVisual(nodeUnlockStatus[node.Id]);
    }

    /// <summary>
    ///     Draws or updates all connection lines between skill nodes.
    /// </summary>
    private void UpdateConnections()
    {
        connectionRenderer.ClearLines();

        foreach (var node in allNodes)
        {
            var startNode = nodeItems.Find(n => n.SkillNode == node);
            if (startNode == null) continue;

            foreach (var connection in node.Connections)
            {
                var endNode = nodeItems.Find(n => n.SkillNode == connection);
                if (endNode != null)
                {
                    var lineColor = nodeUnlockStatus[node.Id] && nodeUnlockStatus[connection.Id]
                        ? Color.green
                        : Color.red;
                    connectionRenderer.DrawLine(
                        startNode.GetComponent<RectTransform>(),
                        endNode.GetComponent<RectTransform>(),
                        lineColor
                    );
                }
            }
        }
    }

    /// <summary>
    ///     Adjusts the scale of skill node UI and connection lines according to the current zoom level.
    /// </summary>
    private void UpdateNodeAndLineScales()
    {
        var inverseScale = 1f / content.localScale.x;
        foreach (var nodeItem in nodeItems)
        {
            var nodeRect = nodeItem.GetComponent<RectTransform>();
            nodeRect.localScale = new Vector3(inverseScale, inverseScale, 1f);
        }

        connectionRenderer.UpdateLineScales();
    }
    
    public void RemoveNode(SkillNodeSO node)
    {
        if (allNodes.Contains(node))
        {
            allNodes.Remove(node);
            nodeDict.Remove(node.Id);
            nodeUnlockStatus.Remove(node.Id);
            RefreshAll();
        }
    }
    
    public void AddNode(SkillNodeSO node)
    {
        if (!allNodes.Contains(node))
        {
            allNodes.Add(node);
            nodeDict[node.Id] = node;
            nodeUnlockStatus[node.Id] = node.IsStartingNode;
        }
    }

    public void RefreshAll()
    {
        nodeItems.Clear();
        nodeItems.AddRange(content.GetComponentsInChildren<NodeItem>());
        foreach (var nodeItem in nodeItems)
            nodeItem.Initialize(this);
    }
}