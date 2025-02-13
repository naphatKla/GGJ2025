using System.Collections;
using Characters;
using UnityEngine;

public class HeartUI : MonoBehaviour
{
    [SerializeField] private GameObject[] heartsUI;
    [SerializeField] private Animator[] animators;

    private void OnEnable()
    {
        
        PlayerCharacter.Instance.onHitWithDamage.AddListener(UpdateLife);
    }

    private void OnDisable()
    {
        PlayerCharacter.Instance.onHitWithDamage.RemoveListener(UpdateLife);
    }

    void UpdateLife()
    {
        if (!animators[PlayerCharacter.Instance.Life]) return;
        animators[PlayerCharacter.Instance.Life].Play("Dead");
        StartCoroutine(CheckIfAnimationFinished(animators[PlayerCharacter.Instance.Life]));
    }
    private IEnumerator CheckIfAnimationFinished(Animator animator)
    {
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
        {
            yield return null;
        }

        Destroy(animator.gameObject);
        yield break;
    }
}
