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
