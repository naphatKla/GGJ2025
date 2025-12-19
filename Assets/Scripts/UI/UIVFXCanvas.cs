using System.Collections;
using System.Collections.Generic;
using ProjectExtensions;
using UnityEngine;

public class UIVFXCanvas : NonAutoCreateSingleton<UIVFXCanvas>
{
    [Header("Rank UI")]
    [SerializeField] private RectTransform gradeMainTransform;
    [SerializeField] private RectTransform gradeVFXHolder;
    [SerializeField] private Vector3 gradeVFXHolderOffset;

    [Header("Soft Skill UI")]
    [SerializeField] private GameObject levelUpUI;

    [Header("Skill Bar UI")]
    [SerializeField] private RectTransform primarySkillHolder;
    [SerializeField] private RectTransform secondarySkillHolder;
    [SerializeField] private RectTransform skillsPanel;
    [SerializeField] private RectTransform expBar;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gradeVFXHolder.transform.localScale = gradeMainTransform.transform.localScale;
        gradeVFXHolder.transform.position = gradeMainTransform.transform.position + gradeVFXHolderOffset ;
    }

    public void GradeVFXUpdate(GameObject GradeVFXprefab)
    {
        if (gradeVFXHolder.childCount >= 1)
        {
            for (int i = gradeVFXHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(gradeVFXHolder.GetChild(i).gameObject);
            }
        }
        Instantiate(GradeVFXprefab, gradeVFXHolder);
    }

    public void GradeVFXHolderSetActive(bool isActive)
    {
        gradeVFXHolder.gameObject.SetActive(isActive);
    }

    public void SetActiveLevelUpUI(bool isActive)
    {
        levelUpUI.SetActive(isActive);
    }
}
