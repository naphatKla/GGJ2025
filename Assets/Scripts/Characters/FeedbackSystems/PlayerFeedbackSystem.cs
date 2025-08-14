using Cameras;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.FeedbackSystems
{
    public class PlayerFeedbackSystem : FeedbackSystem
    {
        private PlayerController _player;
        private PlayerDataSo _playerData;
        
        public override void AssignData(BaseController owner)
        {
            _player = owner as PlayerController;
            _playerData = owner.CharacterData as PlayerDataSo;
            base.AssignData(owner);
        }

        public override void PlayFeedback(string feedbackName)
        {
            if (!feedbackMap.ContainsKey(feedbackName)) return;
            if (ignoreFeedbackList.Contains(feedbackName)) return;

            switch (feedbackName)
            {
                case FeedbackName.Character.AttackHit :
                    if (!IsFeedbackPlaying(FeedbackName.Character.CounterAttack))
                        _player.CameraController.ShakeCamera(_playerData.AttackHitCameraShakeOption);
                    break;
                case FeedbackName.Character.CounterAttack :
                    _player.CameraController.ShakeCamera(_playerData.CounterAttackHitCameraShakeOption);
                    break;
                case FeedbackName.Character.TakeDamage :
                    _player.CameraController.ShakeCamera(_playerData.TakeDamageCameraShakeOption);
                    break;
            }
            
            base.PlayFeedback(feedbackName);
        }
    }
}
