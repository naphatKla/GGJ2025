using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    public class SpaceMineSkillObject : BaseSkillObject
    {
        [SerializeField] private TextMeshPro countDownText;
        [SerializeField] private float countDownDuration = 5f;
        private bool isTimer;
        
        public async UniTask WaitPlaceBombAsync()
        {
            if (isTimer) return;

            isTimer = true;
            float countDownTimer = countDownDuration;
            
            while (countDownTimer <= 0)
            {
                countDownTimer -= Time.deltaTime;
                countDownText.text = countDownTimer.ToString("F1");
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            isTimer = false;    
        }
    }
}
