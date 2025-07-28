using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class SaveSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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
    public float clickScale = 1.1f;
    public float hoverScale = 1.05f;
    public float tweenDuration = 0.2f;

    private bool isClicked = false;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = slotTransform.localScale;

        bool hasSaved = HasSavedName();

        labelTitle.gameObject.SetActive(true);
        labelTitle.text = hasSaved ? inputName.text : "New Game";

        inputName.gameObject.SetActive(false);
        nextButton.SetActive(false);

        nameStateObj.SetActive(false);
        numStateObj.SetActive(false);
        timeObj.SetActive(false);

        inputName.onValueChanged.AddListener(OnInputChanged);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isClicked) return;

        isClicked = true;

        SaveSlotManager.Instance.SetActiveSlot(this);

        slotTransform.DOScale(originalScale * clickScale, tweenDuration).SetEase(Ease.OutBack);

        labelTitle.gameObject.SetActive(false);

        // ✅ แสดง inputField เสมอ
        inputName.gameObject.SetActive(true);
        inputName.ActivateInputField();

        // ✅ แสดง nextButton และข้อมูลทันที
        nextButton.SetActive(true);
        nameStateObj.SetActive(true);
        numStateObj.SetActive(true);
        timeObj.SetActive(true);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isClicked) return;
        slotTransform.DOScale(originalScale * hoverScale, tweenDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isClicked) return;
        slotTransform.DOScale(originalScale, tweenDuration).SetEase(Ease.OutQuad);
    }

    private void OnInputChanged(string value)
    {
        bool hasName = !string.IsNullOrEmpty(value);

        labelTitle.text = hasName ? value : "New Game";
        nextButton.SetActive(hasName);
        nameStateObj.SetActive(hasName);
        numStateObj.SetActive(hasName);
        timeObj.SetActive(hasName);
    }

    public void DeactivateSlot()
    {
        isClicked = false;

        slotTransform.DOScale(originalScale, tweenDuration).SetEase(Ease.OutQuad);

        bool hasSaved = HasSavedName();

        labelTitle.gameObject.SetActive(true);
        labelTitle.text = hasSaved ? inputName.text : "New Game";

        inputName.gameObject.SetActive(false);
        nextButton.SetActive(false);
        nameStateObj.SetActive(false);
        numStateObj.SetActive(false);
        timeObj.SetActive(false);
    }

    public bool HasSavedName()
    {
        return !string.IsNullOrEmpty(inputName.text);
    }

    public string GetSavedName()
    {
        return inputName.text;
    }
}
