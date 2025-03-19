using System;
using System.Collections;
using Characters.Controllers;
using Characters.InputSystems.Interface;
using UnityEngine;

namespace Characters.InputSystems
{
    public class EnemyInputReader : MonoBehaviour, ICharacterInput
    {
        public Action<Vector2> OnMove { get; set; }
        public Action OnPrimarySkillPerform { get; set; }
        public Action OnSecondarySKillPerform { get; set; }
        private Coroutine updateTickCoroutine;
        private float timeTick = 0.2f;

        private void OnEnable()
        {
            updateTickCoroutine = StartCoroutine(UpdateTick());
        }

        private void OnDisable()
        {
            StopCoroutine(updateTickCoroutine);
        }

        private IEnumerator UpdateTick()
        {
            while (true)
            {
                yield return new WaitForSeconds(timeTick);
                OnMove?.Invoke(PlayerController.Instant.transform.position);
            }
        }
    }
}
