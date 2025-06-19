using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class SaveSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    public RectTransform slotTransform;
    public TextMeshProUGUI labelTitle;
    public TMP_InputField inputName;
    public GameObject nextButton;

    [Header("Meta Info")]
    public GameObject nameStateObj;
    public GameObject numStateObj;
    public GameObject timeObj;

    [Header("Scale Settings")]
    public float hoverScale = 1.05f;
    public float clickScale = 1.1f;
    public float tweenDuration = 0.2f;

    private bool isClicked = false;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = slotTransform.localScale;

        inputName.gameObject.SetActive(false);
        nextButton.SetActive(false);

        // ซ่อน meta info ถ้ายังไม่ได้พิมพ์ชื่อ
        bool hasName = !string.IsNullOrEmpty(inputName.text);
        nameStateObj.SetActive(hasName);
        numStateObj.SetActive(hasName);
        timeObj.SetActive(hasName);

        inputName.onValueChanged.AddListener(OnInputChanged);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isClicked)
        {
            slotTransform.DOScale(originalScale * hoverScale, tweenDuration).SetEase(Ease.OutBack);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isClicked)
        {
            slotTransform.DOScale(originalScale, tweenDuration).SetEase(Ease.OutQuad);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isClicked) return;

        // บอก Manager ว่าอันนี้คืออันที่ถูกคลิก
        SaveSlotManager.Instance.SetActiveSlot(this);

        isClicked = true;

        slotTransform.DOScale(originalScale * clickScale, tweenDuration).SetEase(Ease.OutBack);

        labelTitle.gameObject.SetActive(false);
        inputName.gameObject.SetActive(true);
        inputName.ActivateInputField();

        nameStateObj.SetActive(false);
        numStateObj.SetActive(false);
        timeObj.SetActive(false);
    }


    private void OnInputChanged(string value)
    {
        bool hasName = !string.IsNullOrEmpty(value);
        nextButton.SetActive(hasName);
        nameStateObj.SetActive(hasName);
        numStateObj.SetActive(hasName);
        timeObj.SetActive(hasName);
    }
    
    public void DeactivateSlot()
    {
        isClicked = false;
        slotTransform.DOScale(originalScale, tweenDuration).SetEase(Ease.OutQuad);

        labelTitle.gameObject.SetActive(true);
        inputName.gameObject.SetActive(false);
        nextButton.SetActive(false);

        nameStateObj.SetActive(false);
        numStateObj.SetActive(false);
        timeObj.SetActive(false);
    }
}
