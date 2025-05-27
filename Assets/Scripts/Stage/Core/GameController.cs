using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    [SerializeField] private string sceneLoad;
    
    [ReadOnly] public int selectedMapIndex;
    [ReadOnly] public MapDataSO selectedMapData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void PlaySelected()
    {
        await SceneManager.LoadSceneAsync(sceneLoad).ToUniTask();
        await UniTask.Yield();
    }
}