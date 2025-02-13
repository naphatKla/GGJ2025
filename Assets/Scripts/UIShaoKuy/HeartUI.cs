using System;
using System.Collections;
using Characters;
using DG.Tweening;
using UnityEngine;

public class HeartUI : MonoBehaviour
{
    [SerializeField] private GameObject[] heartsUI;
    [SerializeField] private Animator[] animators;

    private void Start()
    {
        PlayerCharacter.Instance.onHealthChanged.AddListener(UpdateLife);
    }
    
    void UpdateLife()
    {
        for (int i = 0; i < heartsUI.Length; i++)
        {
            bool activeCondition = i < PlayerCharacter.Instance.Life;
            if (activeCondition)
            {
                heartsUI[i].SetActive(true);
                animators[i].Play("Move");
            }
            else
            {
                animators[i].Play("Dead");
                StartCoroutine(CheckIfAnimationFinished(animators[i]));
            }
        }
    }
    private IEnumerator CheckIfAnimationFinished(Animator animator)
    {
        yield return null;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
        {
            yield return null;
        }

        animator.gameObject.SetActive(false);
    }
}
