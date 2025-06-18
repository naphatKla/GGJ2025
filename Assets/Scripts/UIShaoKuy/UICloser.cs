using UnityEngine;
using UnityEngine.EventSystems;

public class UICloser : MonoBehaviour
{
    public GameObject targetPanel;

    public void ClosePanel()
    {
        if (targetPanel != null)
            targetPanel.SetActive(false);

        // Reset focus เพื่อให้คลิกครั้งต่อไปไม่พลาด
        EventSystem.current.SetSelectedGameObject(null);
    }
}