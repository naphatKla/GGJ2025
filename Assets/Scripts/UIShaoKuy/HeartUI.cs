using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using UnityEngine;
using UnityEngine.Serialization;

public class HeartUI : MonoBehaviour
{
    [SerializeField] private GameObject[] heartsUI;
    [SerializeField] private Animator[] animators;
    [SerializeField] private PlayerCharacter player;

    private void OnEnable()
    {
        player.OnTakeDamage.AddListener(UpdateLife);
    }

    private void OnDisable()
    {
        player.OnTakeDamage.RemoveListener(UpdateLife);
    }

    void UpdateLife()
    {
        if (animators[player.Life])
        {
            animators[player.Life].Play("Dead");
            StartCoroutine(CheckIfAnimationFinished(animators[player.Life]));
        }
    }
    private IEnumerator CheckIfAnimationFinished(Animator animator)
    {
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
        {
            yield return null;
        }

        Destroy(animator.gameObject);
    }
}
