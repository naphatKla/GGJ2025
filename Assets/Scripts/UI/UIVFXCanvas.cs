using System.Collections;
using System.Collections.Generic;
using ProjectExtensions;
using UnityEngine;

public class UIVFXCanvas : NonAutoCreateSingleton<UIVFXCanvas>
{
    [SerializeField] private RectTransform gradeMainTransform;
    [SerializeField] private RectTransform gradeVFXHolder;
    [SerializeField] private Vector3 gradeVFXHolderOffset;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gradeVFXHolder.transform.localScale = gradeMainTransform.transform.localScale;
        gradeVFXHolder.transform.position = gradeMainTransform.transform.position ;
    }

    public void GradeVFXUpdate(GameObject GradeVFXprefab)
    {
        for (int i = gradeVFXHolder.childCount - 1; i >= 0; i--)
        {
            Destroy(gradeVFXHolder.GetChild(i).gameObject);
        }
        Instantiate(GradeVFXprefab, gradeVFXHolder);
    }
}
