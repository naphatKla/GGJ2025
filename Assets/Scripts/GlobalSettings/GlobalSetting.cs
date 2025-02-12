using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GlobalSettings
{
    public abstract class GlobalSetting<T> : SerializedScriptableObject where T : ScriptableObject
    {
        private const string Path = "GlobalSettings"; // Path for Resources folder
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
                _instance = Resources.Load<T>($"{Path}/{typeof(T).Name}");
#if UNITY_EDITOR
                CheckForConflictingFiles();
                if (_instance) return _instance;
                _instance = CreateInstance<T>();
                Debug.LogWarning(
                    $"There is no {typeof(T).Name} in the Resources folder. Creating a new one in Resources/{Path}/{typeof(T).Name}.");
                CreateNestedFolder("Assets/Resources");
                string savePath = $"Assets/Resources/{Path}/{typeof(T).Name}.asset";
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(savePath) ?? string.Empty); // Ensure directory exist
                AssetDatabase.CreateAsset(_instance, savePath);
                AssetDatabase.SaveAssets();
#endif
                return _instance;
            }
        }

        // Create the folder structure if it doesn't exist
        private static void CreateNestedFolder(string path)
        {
            if (Directory.Exists(path)) return;

            string currentPath = "";
            string[] folders = path.Split('/');

            foreach (var folder in folders)
            {
                currentPath = System.IO.Path.Combine(currentPath, folder);
                if (Directory.Exists(currentPath)) continue;
                string parentPath = System.IO.Path.GetDirectoryName(currentPath);
                string folderName = System.IO.Path.GetFileName(currentPath);
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void CheckForConflictingFiles()
        {
            string[] results = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            
            if (results.Length <= 1) return;
            string conflictingFiles = "";
            foreach (var result in results)
            {
                conflictingFiles += AssetDatabase.GUIDToAssetPath(result) + "\n";
            }

            Debug.LogError(
                $"There are more than one {typeof(T).Name} in the Resources folder. Please make sure there is only one.\nConflicting files:\n{conflictingFiles}");
        }

        [Title("")]
        [HorizontalGroup]
        [Button(ButtonSizes.Large)]
        protected void FindInFolder()
        {
            EditorGUIUtility.PingObject(this);
        }

        [Title("")]
        [HorizontalGroup]
        [Button(ButtonSizes.Large)]
        protected void OpenSourceCode()
        {
            MonoScript script = MonoScript.FromScriptableObject(this);
            AssetDatabase.OpenAsset(script);
        }
    }
}