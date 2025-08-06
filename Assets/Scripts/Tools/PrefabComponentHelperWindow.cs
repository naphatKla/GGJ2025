using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tools
{
    /// <summary>
    /// Simple ScriptableObject to store a selected string value.
    /// Used to bind selection from searchable dropdown.
    /// </summary>
    [Serializable]
    public class SearchableStringSO : ScriptableObject
    {
        public string selectedValue;
    }

    public class PrefabComponentHelperWindow : EditorWindow
    {
        #region Menu

        [MenuItem("Tools/Prefab Component Helper")]
        public static void OpenWindow()
        {
            GetWindow<PrefabComponentHelperWindow>("Prefab Component Helper");
        }

        #endregion

        #region Fields

        private string searchPath = "Assets/";
        private List<string> ignorePaths = new();

        private List<Type> filterTypes = new();
        private bool useOrCondition = false;
        private bool includeDerivedTypes = true;

        private bool doNotAddIfAlreadyExists = true;
        private List<Type> addComponentTypes = new();

        private Vector2 scrollPos;
        private List<SearchResult> searchResults = new();
        private List<Type> allComponentTypes;

        private SearchableStringSO filterSearchSO;
        private SearchableStringSO addComponentSearchSO;

        private string lastClickedPrefabPath;
        private string lastClickedObjectPath;

        private Dictionary<string, List<string>> prefabParentMap = new();
        private Dictionary<string, string> prefabChildToParent = new();

        private List<DuplicateComponentInfo> duplicateResults = new();

        #endregion

        #region Data Class

        /// <summary>
        /// Classification of search result type
        /// </summary>
        private enum PrefabSearchResultType
        {
            PrefabRoot,
            PrefabRootVariant,
            PrefabObjectNested,
            ObjectInPrefab
        }

        /// <summary>
        /// Data for a single search result
        /// </summary>
        private class SearchResult
        {
            public string assetPath;
            public string prefabPath;
            public List<string> matchedComponentNames;
            public bool isVariant;
            public bool isNested;
            public PrefabSearchResultType resultType;
        }

        public class DuplicateComponentInfo
        {
            public string assetPath;
            public string prefabPath;
            public List<string> duplicateComponentNames;
        }

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            LoadAllComponentTypes();

            filterSearchSO = CreateInstance<SearchableStringSO>();
            addComponentSearchSO = CreateInstance<SearchableStringSO>();
        }
        
        private void OnDisable()
        {
            if (filterSearchSO != null)
                DestroyImmediate(filterSearchSO);

            if (addComponentSearchSO != null)
                DestroyImmediate(addComponentSearchSO);
        }

        private void OnGUI()
        {
            GUILayout.Label("Search Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawSearchPathField();
            DrawIgnorePathsField();
            DrawFilterTypesListOnlyUI();

            useOrCondition = EditorGUILayout.ToggleLeft("Use OR Condition", useOrCondition);
            includeDerivedTypes = EditorGUILayout.ToggleLeft("Include Derived Types", includeDerivedTypes);

            DrawFilterSearchField();

            DrawColoredButton(
                label: "Search Prefabs",
                color: new Color(0.4f, 1f, 0.4f, 1f),
                height: 30,
                action: PerformSearch
            );

            EditorGUILayout.EndVertical();

            DrawSeparator();

            GUILayout.Label("Add Component to Results", EditorStyles.boldLabel);

            doNotAddIfAlreadyExists =
                EditorGUILayout.ToggleLeft("Do Not Add If Already Exists", doNotAddIfAlreadyExists);

            DrawAddComponentTypesUI();

            EditorGUILayout.BeginHorizontal();

            DrawColoredButton(
                label: "Add Component(s) to Results",
                color: new Color(1f, 1f, 0.6f, 1f),
                height: 30,
                action: PerformAddComponents,
                expandWidth: true
            );

            DrawColoredButton(
                label: "Check For Duplicate Components",
                color: new Color(1f, 0.6f, 0.6f, 1f),
                height: 30,
                action: CheckForDuplicateComponents,
                expandWidth: true
            );

            EditorGUILayout.EndHorizontal();

            DrawSeparator();

            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.Label($"Search Results ({searchResults.Count})", EditorStyles.boldLabel);
            DrawSearchResults();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region UI Drawing

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 1));
            EditorGUILayout.Space(15);
        }

        private void DrawSearchPathField()
        {
            EditorGUILayout.BeginHorizontal();

            searchPath = EditorGUILayout.TextField("Search Path", searchPath);

            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                    {
                        searchPath = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        Debug.LogWarning("Selected path must be under Assets/");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawIgnorePathsField()
        {
            EditorGUILayout.LabelField("Ignore Path(s)", EditorStyles.boldLabel);

            for (int i = 0; i < ignorePaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(ignorePaths[i]);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    ignorePaths.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Browse Ignore Folder", GUILayout.Width(150)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Ignore Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                    {
                        var path = "Assets" + selected.Substring(Application.dataPath.Length);
                        if (!ignorePaths.Contains(path))
                        {
                            ignorePaths.Add(path);
                        }

                        Repaint();
                    }
                    else
                    {
                        Debug.LogWarning("Selected path must be under Assets/");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        private void DrawFilterTypesListOnlyUI()
        {
            EditorGUILayout.LabelField("Filter Component Type(s)", EditorStyles.boldLabel);

            for (int i = 0; i < filterTypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(filterTypes[i].FullName);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    filterTypes.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
        }

        private void DrawFilterSearchField()
        {
            var rect = EditorGUILayout.GetControlRect();
            string label = string.IsNullOrEmpty(filterSearchSO.selectedValue)
                ? "Select Type..."
                : filterSearchSO.selectedValue;

            if (GUI.Button(rect, label, EditorStyles.popup))
            {
                if (!filterSearchSO)
                    filterSearchSO = CreateInstance<SearchableStringSO>();
                
                var serialized = new SerializedObject(filterSearchSO);
                var property = serialized.FindProperty("selectedValue");

                var options = allComponentTypes
                    .Select(t => t.FullName.Replace(".", "/"))
                    .ToList();

                Khami.SearchableDropdown.Editor.SearchablePopup.Show(rect, property, options);
            }

            if (!string.IsNullOrEmpty(filterSearchSO.selectedValue))
            {
                var typeName = filterSearchSO.selectedValue.Replace("/", ".");
                var selectedType = allComponentTypes.FirstOrDefault(
                    t => t.FullName == typeName);

                if (selectedType != null && !filterTypes.Contains(selectedType))
                {
                    filterTypes.Add(selectedType);
                    filterSearchSO.selectedValue = "";
                    Repaint();
                }
            }
        }

        private void DrawAddComponentTypesUI()
        {
            EditorGUILayout.LabelField("Add Component Type(s)", EditorStyles.boldLabel);

            for (int i = 0; i < addComponentTypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(addComponentTypes[i].FullName);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    addComponentTypes.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            DrawAddComponentSearchField();
        }

        private void DrawAddComponentSearchField()
        {
            var rect = EditorGUILayout.GetControlRect();
            string label = string.IsNullOrEmpty(addComponentSearchSO.selectedValue)
                ? "Select Type..."
                : addComponentSearchSO.selectedValue;

            if (GUI.Button(rect, label, EditorStyles.popup))
            {
                if (!addComponentSearchSO)
                    addComponentSearchSO = CreateInstance<SearchableStringSO>();
                
                var serialized = new SerializedObject(addComponentSearchSO);
                var property = serialized.FindProperty("selectedValue");

                var options = allComponentTypes
                    .Select(t => t.FullName.Replace(".", "/"))
                    .ToList();

                Khami.SearchableDropdown.Editor.SearchablePopup.Show(rect, property, options);
            }

            if (!string.IsNullOrEmpty(addComponentSearchSO.selectedValue))
            {
                var typeName = addComponentSearchSO.selectedValue.Replace("/", ".");
                var selectedType = allComponentTypes.FirstOrDefault(
                    t => t.FullName == typeName);

                if (selectedType != null && !addComponentTypes.Contains(selectedType))
                {
                    addComponentTypes.Add(selectedType);
                    addComponentSearchSO.selectedValue = "";
                    Repaint();
                }
            }
        }

        private void DrawSearchResults()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

            foreach (var result in searchResults)
            {
                bool isLastClicked =
                    result.assetPath == lastClickedPrefabPath &&
                    result.prefabPath == lastClickedObjectPath;

                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = isLastClicked ? Color.yellow : Color.white;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.Space(5);

                var stylePrefab = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    normal = { textColor = new Color(1.0f, 0.6f, 0.1f) }
                };
                EditorGUILayout.LabelField($"PREFAB: {result.assetPath}", stylePrefab);

                var pathNodes = result.prefabPath.Split('/');
                var highlightedPath = string.Join("/", pathNodes.Select((node, i) =>
                    i == pathNodes.Length - 1 ? $"<color=#00ffff>{node}</color>" : node
                ));

                var richStyle = new GUIStyle(EditorStyles.label) { richText = true };
                EditorGUILayout.LabelField(highlightedPath, richStyle);

                EditorGUILayout.LabelField("Type: " + result.resultType.ToString());

                var greenStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
                foreach (var compName in result.matchedComponentNames)
                {
                    EditorGUILayout.LabelField("- " + compName, greenStyle);
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Ping"))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(result.assetPath);
                    EditorGUIUtility.PingObject(obj);

                    lastClickedPrefabPath = result.assetPath;
                    lastClickedObjectPath = result.prefabPath;
                    Repaint();
                }

                if (GUILayout.Button("Open Prefab"))
                {
                    OpenPrefabAndSelect(result);

                    lastClickedPrefabPath = result.assetPath;
                    lastClickedObjectPath = result.prefabPath;
                    Repaint();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = oldColor;
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        private void DrawColoredButton(
            string label,
            Color color,
            float height,
            Action action,
            bool expandWidth = false
        )
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = height,
                fontSize = 14
            };

            if (expandWidth)
                style.stretchWidth = true;

            if (GUILayout.Button(label, style))
            {
                action?.Invoke();
            }

            GUI.backgroundColor = oldColor;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Load all Component types available in loaded assemblies
        /// </summary>
        private void LoadAllComponentTypes()
        {
            allComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => typeof(Component).IsAssignableFrom(t))
                .OrderBy(t => t.FullName)
                .ToList();
        }

        /// <summary>
        /// Search all prefabs under the searchPath and find matching components.
        /// Ignores any prefabs under ignorePaths.
        /// </summary>
        private void PerformSearch()
        {
            searchResults.Clear();
            prefabParentMap.Clear();
            prefabChildToParent.Clear();

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Check if asset path is under any ignore path
                bool ignored = ignorePaths.Any(ip =>
                    assetPath.StartsWith(ip + "/") || assetPath == ip);

                if (ignored)
                {
                    continue;
                }

                var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

                var parentObj = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
                bool isVariant = parentObj != null;

                if (isVariant)
                {
                    var parentPath = AssetDatabase.GetAssetPath(parentObj);
                    if (!prefabParentMap.ContainsKey(parentPath))
                        prefabParentMap[parentPath] = new List<string>();
                    prefabParentMap[parentPath].Add(assetPath);
                    prefabChildToParent[assetPath] = parentPath;
                }

                var allTransforms = prefabRoot.GetComponentsInChildren<Transform>(true);

                foreach (var tr in allTransforms)
                {
                    var obj = tr.gameObject;
                    var matched = GetMatchingComponents(obj, filterTypes, useOrCondition, includeDerivedTypes);

                    if (matched.Count > 0)
                    {
                        var prefabPath = GetPath(tr);

                        var assetType = PrefabUtility.GetPrefabAssetType(obj);

                        PrefabSearchResultType resultType;

                        if (!prefabPath.Contains("/"))
                        {
                            resultType = isVariant
                                ? PrefabSearchResultType.PrefabRootVariant
                                : PrefabSearchResultType.PrefabRoot;
                        }
                        else
                        {
                            resultType = (assetType == PrefabAssetType.Regular ||
                                          assetType == PrefabAssetType.Variant)
                                ? PrefabSearchResultType.PrefabObjectNested
                                : PrefabSearchResultType.ObjectInPrefab;
                        }

                        var result = new SearchResult
                        {
                            assetPath = assetPath,
                            prefabPath = prefabPath,
                            matchedComponentNames = matched.Select(c => c.GetType().Name).ToList(),
                            isVariant = isVariant,
                            isNested = resultType == PrefabSearchResultType.PrefabObjectNested,
                            resultType = resultType
                        };

                        searchResults.Add(result);
                    }
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            // Sort results
            searchResults = searchResults
                .OrderBy(r => (int)r.resultType)
                .ThenBy(r => r.assetPath)
                .ThenBy(r => r.prefabPath)
                .ToList();

            Debug.Log($"Found {searchResults.Count} matching result(s).");
        }

        /// <summary>
        /// Adds component(s) to all matching prefabs/objects found in search results
        /// </summary>
        private void PerformAddComponents()
        {
            var addTypes = addComponentTypes
                .Where(t => t != null &&
                            typeof(Component).IsAssignableFrom(t) &&
                            !t.IsAbstract &&
                            !t.IsGenericTypeDefinition &&
                            !t.IsInterface)
                .Distinct()
                .ToList();

            if (addTypes.Count == 0)
            {
                Debug.LogWarning("No valid add component types specified.");
                return;
            }

            var grouped = searchResults
                .GroupBy(r => new { r.assetPath, r.prefabPath })
                .ToList();

            HashSet<string> processedPrefabs = new();

            foreach (var group in grouped)
            {
                var assetPath = group.Key.assetPath;
                var prefabPath = group.Key.prefabPath;

                string uniqueKey = assetPath + "|" + prefabPath;
                if (processedPrefabs.Contains(uniqueKey))
                    continue;

                var prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
                var target = FindTransformByPath(prefabContents.transform, prefabPath);

                if (target != null)
                {
                    foreach (var type in addTypes)
                    {
                        bool parentHas = false;
                        if (prefabChildToParent.TryGetValue(assetPath, out var parentAssetPath))
                        {
                            parentHas = parentHasComponentInPrefab(parentAssetPath, prefabPath, type);
                        }

                        if (parentHas)
                            continue;

                        var existing = target.GetComponent(type);
                        if (existing)
                        {
                            if (doNotAddIfAlreadyExists)
                                continue;
                        }

                        Undo.RegisterFullObjectHierarchyUndo(prefabContents, "Add Component");
                        target.gameObject.AddComponent(type);
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }

                processedPrefabs.Add(uniqueKey);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Component(s) added successfully.");
        }

        /// <summary>
        /// Checks if the parent prefab already has a specific component on the same object path
        /// </summary>
        private bool parentHasComponentInPrefab(string parentAssetPath, string prefabPath, Type type)
        {
            var parentContents = PrefabUtility.LoadPrefabContents(parentAssetPath);
            var target = FindTransformByPath(parentContents.transform, prefabPath);
            if (target != null)
            {
                var existing = target.GetComponent(type);
                if (existing)
                {
                    PrefabUtility.UnloadPrefabContents(parentContents);
                    return true;
                }
            }

            PrefabUtility.UnloadPrefabContents(parentContents);
            return false;
        }

        /// <summary>
        /// Finds all matching components on a GameObject
        /// </summary>
        private List<Component> GetMatchingComponents(GameObject obj, List<Type> filterTypes, bool orMode,
            bool includeDerived)
        {
            var matched = new List<Component>();
            var components = obj.GetComponents<Component>();

            foreach (var comp in components)
            {
                if (comp == null) continue;

                int matchCount = 0;

                foreach (var type in filterTypes)
                {
                    if (type == null || !typeof(Component).IsAssignableFrom(type))
                        continue;

                    bool isMatch = includeDerived
                        ? IsAssignableFromGeneric(type, comp.GetType())
                        : type == comp.GetType();

                    if (isMatch)
                        matchCount++;
                }

                if ((orMode && matchCount > 0) || (!orMode && matchCount == filterTypes.Count))
                {
                    matched.Add(comp);
                }
            }

            return matched;
        }

        private void CheckForDuplicateComponents()
        {
            duplicateResults.Clear();

            foreach (var result in searchResults)
            {
                var duplicates = new List<string>();

                var prefabContents = PrefabUtility.LoadPrefabContents(result.assetPath);
                var target = FindTransformByPath(prefabContents.transform, result.prefabPath);

                if (target == null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                    continue;
                }

                var components = target.GetComponents<Component>();
                var duplicateGroups = components
                    .Where(c => c != null)
                    .GroupBy(c => c.GetType())
                    .Where(g => g.Count() > 1);

                foreach (var g in duplicateGroups)
                {
                    duplicates.Add($"{g.Key.Name} (Count: {g.Count()})");
                }

                if (duplicates.Count > 0)
                {
                    duplicateResults.Add(new DuplicateComponentInfo
                    {
                        assetPath = result.assetPath,
                        prefabPath = result.prefabPath,
                        duplicateComponentNames = duplicates
                    });
                }

                PrefabUtility.UnloadPrefabContents(prefabContents);
            }

            if (duplicateResults.Count == 0)
            {
                Debug.Log("No duplicate components found.");
            }
            else
            {
                Debug.LogWarning($"Found {duplicateResults.Count} objects with duplicate components.");
                DuplicateComponentResultWindow.ShowWindow(duplicateResults);
            }
        }

        /// <summary>
        /// Checks if derivedType is assignable to baseType (supports generic base types)
        /// </summary>
        private bool IsAssignableFromGeneric(Type baseType, Type derivedType)
        {
            if (baseType == null || derivedType == null) return false;
            if (baseType == derivedType) return true;

            if (baseType.IsGenericTypeDefinition)
            {
                Type current = derivedType;
                while (current != null && current != typeof(object))
                {
                    if (current.IsGenericType &&
                        current.GetGenericTypeDefinition() == baseType)
                        return true;

                    current = current.BaseType;
                }

                return false;
            }

            return baseType.IsAssignableFrom(derivedType);
        }

        /// <summary>
        /// Returns the transform path string of a transform (e.g. Canvas/Button/Label)
        /// </summary>
        private static string GetPath(Transform transform)
        {
            var path = new List<string>();

            while (transform != null)
            {
                path.Insert(0, transform.name);
                transform = transform.parent;
            }

            return string.Join("/", path);
        }

        /// <summary>
        /// Finds a Transform in a hierarchy by path (e.g. Canvas/Button/Label)
        /// </summary>
        public static Transform FindTransformByPath(Transform root, string path)
        {
            var parts = path.Split('/');
            var current = root;

            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null)
                    return null;
            }

            return current;
        }

        /// <summary>
        /// Opens the prefab stage and selects a target object by path
        /// </summary>
        private void OpenPrefabAndSelect(SearchResult result)
        {
            var stage = PrefabStageUtility.OpenPrefab(result.assetPath);
            EditorApplication.delayCall += () =>
            {
                if (stage == null) return;

                var root = stage.prefabContentsRoot.transform;
                var target = FindTransformByPath(root, result.prefabPath);
                if (target != null)
                {
                    Selection.activeObject = target.gameObject;
                    EditorGUIUtility.PingObject(target.gameObject);
                }
                else
                {
                    Debug.LogWarning("Cannot find transform: " + result.prefabPath);
                }
            };
        }

        #endregion
    }

    public class DuplicateComponentResultWindow : EditorWindow
    {
        private List<PrefabComponentHelperWindow.DuplicateComponentInfo> duplicates;
        private Vector2 scroll;

        public static void ShowWindow(List<PrefabComponentHelperWindow.DuplicateComponentInfo> results)
        {
            var window = GetWindow<DuplicateComponentResultWindow>("Duplicate Components");
            window.duplicates = results;
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            if (duplicates == null || duplicates.Count == 0)
            {
                EditorGUILayout.LabelField("No duplicate components found.");
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var dup in duplicates)
            {
                EditorGUILayout.BeginVertical("box");

                var stylePrefab = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    normal = { textColor = Color.red }
                };

                EditorGUILayout.LabelField($"Prefab: {dup.assetPath}", stylePrefab);

                var pathNodes = dup.prefabPath.Split('/');
                var highlightedPath = string.Join("/", pathNodes.Select((node, i) =>
                    i == pathNodes.Length - 1 ? $"<color=#00ffff>{node}</color>" : node
                ));

                var richStyle = new GUIStyle(EditorStyles.label) { richText = true };
                EditorGUILayout.LabelField(highlightedPath, richStyle);

                EditorGUILayout.Space(5);

                foreach (var comp in dup.duplicateComponentNames)
                {
                    EditorGUILayout.LabelField($"- {comp}", new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = Color.red }
                    });
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Ping"))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dup.assetPath);
                    EditorGUIUtility.PingObject(obj);
                }

                if (GUILayout.Button("Open Prefab"))
                {
                    var stage = PrefabStageUtility.OpenPrefab(dup.assetPath);
                    EditorApplication.delayCall += () =>
                    {
                        if (stage == null) return;
                        var root = stage.prefabContentsRoot.transform;
                        var target = PrefabComponentHelperWindow.FindTransformByPath(root, dup.prefabPath);
                        if (target != null)
                        {
                            Selection.activeObject = target.gameObject;
                            EditorGUIUtility.PingObject(target.gameObject);
                        }
                    };
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}