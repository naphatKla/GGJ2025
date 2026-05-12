using System;
using System.Threading;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    public class SpaceMineSkillObject : BaseSkillObject
    {
        [SerializeField] private TextMeshPro countDownText;

        private BaseController _target;

        /// <summary>
        /// The controller this bomb is currently attached to. 
        /// Null if not following anyone.
        /// </summary>
        public BaseController Target => _target;

        private void Update()
        {
            if (!_target) return;
            transform.position = _target.transform.position;
        }

        public void FollowTarget(BaseController target)
        {
            StopFollow();
            _target = target;
            if (_target != null)
                _target.HealthSystem.OnDead += StopFollow;
        }

        public void StopFollow()
        {
            if (_target == null) return;
            _target.HealthSystem.OnDead -= StopFollow;
            _target = null;
        }

        /// <summary>
        /// Countdown timer. Updates the text display each frame.
        /// Returns when countdown reaches zero.
        /// </summary>
        public async UniTask WaitCountdownAsync(float duration, CancellationToken ct)
        {
            float remaining = duration;

            while (remaining > 0f)
            {
                if (ct.IsCancellationRequested) return;

                remaining -= Time.deltaTime;
                if (countDownText)
                    countDownText.text = Mathf.Max(0f, remaining).ToString("F1");

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (countDownText)
                countDownText.text = "";
        }

        /// <summary>
        /// Full reset before returning to pool.
        /// </summary>
        public void ResetForPool()
        {
            StopFollow();
            DamageOnTouch.DisableDamage(null);
            if (countDownText) countDownText.text = "";
        }
    }
}