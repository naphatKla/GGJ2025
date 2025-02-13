using MoreMountains.Feedbacks;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    private LevelSelectorButton currentSelectedLevel;
    public int level;
    [SerializeField] private MMF_Player changeScene;
    private static LevelSelector instance;
    public int[] levelScores = new int[6]{0,0,0,0,0,0};

private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // ถ้ามี instance อยู่แล้ว ให้ทำลายตัวใหม่
        }
    }

    public void SetLevel(LevelSelectorButton button)
    {
        if (currentSelectedLevel == button) return;
        if (currentSelectedLevel != null )
        {
            currentSelectedLevel.TurnNormal();
        }
        currentSelectedLevel = button;
        currentSelectedLevel.OnToggle();
        level = int.Parse(currentSelectedLevel.gameObject.name);
    }

    public void SetScoreOnLevel(int level,int score)
    {
        levelScores[level] = score;
    }
}
