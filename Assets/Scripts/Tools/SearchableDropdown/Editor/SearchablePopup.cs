using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Khami.SearchableDropdown.Editor
{
    public class SearchablePopup : EditorWindow
    {
        private class MenuNode
        {
            public string Name;
            public string FullPath;
            public int Depth;
            public bool IsExpanded;
            public bool HasChildren => Children.Count > 0;
            public List<MenuNode> Children = new();
        }

        private List<MenuNode> _visibleNodes = new();
        private MenuNode _rootNode;
        private bool IsSearching => !string.IsNullOrEmpty(_search);

        private SerializedProperty _targetProperty;
        private List<string> _localOptions;
        private string _search = "";
        private Vector2 _scrollPosition;

        private int _selectedIndex = -1;
        private bool _shouldScrollToSelected = true;
        
        private const float IndentWidth = 14f;
        private const float ChevronSize = 16f;
        private const float ItemHeight = 20f;

        private static Color selectedColor;
        private static Color hoverColor;

        private static GUIStyle highlightStyle;

        public static void Show(Rect buttonRect, SerializedProperty property, IEnumerable<string> options)
        {
            var window = CreateInstance<SearchablePopup>();
            window._targetProperty = property.Copy();
            window._localOptions = options.Distinct().ToList();
            window.InitializeStyle();
            
            window.BuildHierarchy(window._localOptions);
            window.ExpandToSelection(property.stringValue);
            window.FlattenHierarchy(window._rootNode, 0);
            
            // Find initial selected index
            window._selectedIndex = window._localOptions.IndexOf(property.stringValue);

            var screenPos = GUIUtility.GUIToScreenPoint(buttonRect.position);
            window.position = new Rect(screenPos.x, screenPos.y + buttonRect.height,
                Mathf.Max(buttonRect.width, 200), 200);
            window.ShowPopup();
        }

        private void InitializeStyle()
        {
            selectedColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.44f, 0.88f, 0.3f) // Dark theme blue
                : new Color(0.22f, 0.44f, 0.88f, 0.6f); // Light theme gray

            hoverColor = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.3f, 0.3f, 0.4f) // Dark theme gray
                : new Color(0.6f, 0.6f, 0.6f, 0.6f); // Light theme gray

            highlightStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(5, 5, 2, 2)
            };
        }

        private void BuildHierarchy(IEnumerable<string> options)
        {
            _rootNode = new MenuNode { Name = "Root", Depth = -1 };

            foreach (var path in options.Distinct().OrderBy(p => p))
            {
                var parts = path.Split('/');
                var current = _rootNode;

                for (var i = 0; i < parts.Length; i++)
                {
                    var existing = current.Children.FirstOrDefault(c => c.Name == parts[i]);
                    if (existing == null)
                    {
                        var newNode = new MenuNode
                        {
                            Name = parts[i],
                            FullPath = string.Join("/", parts.Take(i + 1)),
                            Depth = current.Depth + 1
                        };
                        current.Children.Add(newNode);
                        current = newNode;
                    }
                    else
                    {
                        current = existing;
                    }
                }
            }

            FlattenHierarchy(_rootNode, 0);
        }

        private void FlattenHierarchy(MenuNode node, int depth, bool forceExpand = false)
        {
            foreach (var child in node.Children.OrderBy(c => c.Name))
            {
                _visibleNodes.Add(child);

                var shouldExpand = forceExpand ||
                                   (IsSearching && child.FullPath.ToLower().Contains(_search.ToLower())) ||
                                   child.IsExpanded;

                if (shouldExpand || IsSearching)
                {
                    FlattenHierarchy(child, depth + 1, forceExpand);
                }
            }
        }
        
        private void ExpandToSelection(string selectedPath)
        {
            if (string.IsNullOrEmpty(selectedPath)) return;

            var pathParts = selectedPath.Split('/');
            var current = _rootNode;
        
            foreach (var part in pathParts)
            {
                var next = current.Children.FirstOrDefault(c => c.Name == part);
                if (next == null) break;
            
                next.IsExpanded = true;
                current = next;
            }
        }

        private void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(_targetProperty == null);

            HandleKeyboard();
            DrawSearchField();
            DrawOptionsList();
            HandleInitialScroll();

            EditorGUI.EndDisabledGroup();
        }


        private void HandleKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                    Close();
            }
        }

        private void DrawSearchField()
        {
            EditorGUI.BeginChangeCheck();
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
                Repaint();
        }

        private void DrawOptionsList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            _visibleNodes.Clear();
            FlattenHierarchy(_rootNode, 0, IsSearching);

            var filtered = _visibleNodes
                .Where(n => IsSearching || IsParentExpanded(n))
                .Where(n => !IsSearching || n.FullPath.ToLower().Contains(_search.ToLower()));

            foreach (var node in filtered)
            {
                var rect = GUILayoutUtility.GetRect(
                    new GUIContent(node.Name),
                    highlightStyle,
                    GUILayout.ExpandWidth(true)
                );

                // Chevron interaction area
                var chevronRect = new Rect(
                    rect.x + (node.Depth * IndentWidth),
                    rect.y + (rect.height - ChevronSize) / 2,
                    ChevronSize,
                    ChevronSize
                );

                // Draw background
                if (Event.current.type == EventType.Repaint)
                {
                    var isSelected = node.FullPath == _targetProperty.stringValue;
                    var isHover = rect.Contains(Event.current.mousePosition);

                    if (isSelected)
                    {
                        EditorGUI.DrawRect(rect, selectedColor);
                    }
                    else if (isHover)
                    {
                        EditorGUI.DrawRect(rect, hoverColor);
                    }
                }

                // Draw chevron for parent items
                if (node.HasChildren)
                {
                    EditorGUI.LabelField(chevronRect, node.IsExpanded ? "▼" : "▶");

                    // Toggle expansion state on chevron click
                    if (Event.current.type == EventType.MouseDown && chevronRect.Contains(Event.current.mousePosition))
                    {
                        node.IsExpanded = !node.IsExpanded;
                        Event.current.Use();
                        GUI.changed = true;
                    }
                }

                // Draw label with indentation
                var labelRect = new Rect(
                    rect.x + (node.Depth * IndentWidth) + (node.HasChildren ? ChevronSize : 0),
                    rect.y,
                    rect.width - (node.Depth * IndentWidth),
                    rect.height
                );

                EditorGUI.LabelField(labelRect, node.Name, highlightStyle);

                // Handle item click
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    if (!node.HasChildren)
                    {
                        SetValueAndClose(node.FullPath);
                        GUIUtility.ExitGUI();
                    }
                    else if (!IsSearching)
                    {
                        node.IsExpanded = !node.IsExpanded;
                        GUI.changed = true;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
        
        private bool IsParentExpanded(MenuNode node)
        {
            var current = node;
            while (current.Depth > 0)
            {
                current = FindParent(current);
                if (current != null && !current.IsExpanded && !IsSearching)
                    return false;
            }
            return true;
        }

        private MenuNode FindParent(MenuNode node)
        {
            var pathParts = node.FullPath.Split('/').ToList();
            if (pathParts.Count < 2) return null;
            pathParts.RemoveAt(pathParts.Count - 1);
            return FindNodeByPath(string.Join("/", pathParts));
        }
        
        private MenuNode FindNodeByPath(string path)
        {
            return _visibleNodes.FirstOrDefault(n => n.FullPath == path);
        }
        
        private void HandleInitialScroll()
        {
            if (_shouldScrollToSelected)
            {
                ScrollToSelected();
                _shouldScrollToSelected = false;
            }
        }

        private void ScrollToSelected()
        {
            if (_selectedIndex == -1) return;

            var visibleHeight = position.height - 40;
            var targetY = _selectedIndex * ItemHeight - visibleHeight / 2;
        
            _scrollPosition.y = Mathf.Clamp(
                targetY, 
                0, 
                Mathf.Max(0, _visibleNodes.Count * ItemHeight - visibleHeight)
            );
        
            Repaint();
        }

        private void SetValueAndClose(string value)
        {
            if (_targetProperty != null)
            {
                _targetProperty.stringValue = value;
                _targetProperty.serializedObject.ApplyModifiedProperties();
            }

            Close();
        }

        private void OnLostFocus() => Close();

        //Close before reload to prevent unexpected error
        private void OnEnable() => AssemblyReloadEvents.beforeAssemblyReload += Close;

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            _targetProperty = null;
        }

        private void OnDestroy() => AssemblyReloadEvents.beforeAssemblyReload -= Close;
    }
}