using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GlobalSettings
{
    public abstract class GlobalSetting<T> : SerializedScriptableObject where T : ScriptableObject
    {
        private const string Path = "Assets/GameData/GlobalSettings";
        private static T _instance;
        
        /// <summary>
        /// This Instance is scriptable object + singleton.
        /// It will have an only one instance in the project like singleton pattern.
        /// You can access this instance from everywhere in the project.
        /// If there is no instance exits in the project, it will create a new one.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance) return _instance;
                
                string[] results = AssetDatabase.FindAssets("t:" + typeof(T).Name);
                switch (results.Length)
                {
                    case > 1:
                        string conflictingFiles = "";
                        foreach (var result in results)
                            conflictingFiles += AssetDatabase.GUIDToAssetPath(result) + "\n";
                        Debug.LogError("There are more than one " + typeof(T).Name + " in the project. Please make sure there is only one.\n"
                                       + "Instance will use the first one found. The conflicting files are:\n" + conflictingFiles);
                        return null;
                    case 0:
                        Debug.LogWarning($"There is no " + typeof(T).Name + $" in the project. Created file at {Path}");
                        CreateNestedFolder(Path);
                        _instance = CreateInstance<T>();
                        AssetDatabase.CreateAsset(_instance, $"{Path}/{typeof(T).Name}.asset");
                        AssetDatabase.SaveAssets();
                        return _instance;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(results[0]);
                _instance = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                return _instance;
            }
        }
        
        public static void CreateNestedFolder(string path)
        {
            if (Directory.Exists(path)) return;
            string currentPath = "";
            string[] folders = Path.Split('/');
    
            foreach (var folder in folders)
            {
                currentPath = System.IO.Path.Combine(currentPath, folder);
                if (Directory.Exists(currentPath)) continue;
                string parentPath = System.IO.Path.GetDirectoryName(currentPath);
                string folderName = System.IO.Path.GetFileName(currentPath);
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        [Title("")] [HorizontalGroup] [Button(ButtonSizes.Large)]
        protected void FindInFolder()
        {
            EditorGUIUtility.PingObject(this as T);
        }
        
        [Title("")] [HorizontalGroup] [Button(ButtonSizes.Large)]
        protected void OpenSourceCode()
        {
            MonoScript script = MonoScript.FromScriptableObject(this as T);
            AssetDatabase.OpenAsset(script);
        }
    }
}
