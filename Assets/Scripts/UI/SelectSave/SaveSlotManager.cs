using UnityEngine;
using UnityEngine.EventSystems;

public class SaveSlotManager : MonoBehaviour
{
    public static SaveSlotManager Instance;

    private SaveSlotUI currentActiveSlot;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject clickedObject = GetObjectUnderMouse();

            // Reset only if clicked another SaveSlotUI
            if (currentActiveSlot != null &&
                clickedObject != null &&
                clickedObject != currentActiveSlot.gameObject)
            {
                SaveSlotUI clickedSlot = clickedObject.GetComponent<SaveSlotUI>();
                if (clickedSlot != null)
                {
                    ResetActiveSlot();
                }
            }
        }
    }

    
    // Check if the slot is already active
    public void SetActiveSlot(SaveSlotUI slot)
    {
        if (currentActiveSlot != null && currentActiveSlot != slot)
        {
            currentActiveSlot.DeactivateSlot();
        }

        currentActiveSlot = slot;
    }
    
    public void ResetActiveSlot()
    {
        if (currentActiveSlot != null)
        {
            currentActiveSlot.DeactivateSlot();
            currentActiveSlot = null;
        }
    }
    
    private GameObject GetObjectUnderMouse()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        if (raycastResults.Count > 0)
        {
            return raycastResults[0].gameObject;
        }

        return null;
    }
}