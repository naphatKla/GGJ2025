using System;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    public class SpaceMineSkillObject : BaseSkillObject
    {
        [SerializeField] private TextMeshPro countDownText;
        [SerializeField] private float countDownDuration = 5f;
        private bool _isTimer = false;
        private BaseController _target;

        private void Update()
        {
            if (!_target) return;
            transform.position = _target.transform.position;
        }

        public void FollowTarget(BaseController target)
        {
            StopFollow();
            if (!target) return;
            _target = target;
            _target.HealthSystem.OnDead += StopFollow;
        }

        public void StopFollow()
        {
            if (_target == null) return;
            _target.HealthSystem.OnDead -= StopFollow;
            _target = null;
        }
        
        public async UniTask WaitPlaceBombAsync()
        {
            Debug.Log("call?");
            if (_isTimer) return;
            
            _isTimer = true;
            float countDownTimer = countDownDuration;
            
            while (countDownTimer > 0)
            {
                countDownTimer -= Time.deltaTime;
                countDownText.text = countDownTimer.ToString("F1");
                Debug.Log(countDownTimer);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            _isTimer = false;    
        }
    }
}
