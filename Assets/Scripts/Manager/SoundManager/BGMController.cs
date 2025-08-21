using Cysharp.Threading.Tasks;
using GameControl;
using GameControl.Controller;
using GameControl.GameState;
using UnityEngine;

namespace Manager.SoundManager
{
    public class BGMController : MonoBehaviour
    {
        void Start()
        {
            PlayNewBGMAfter().Forget();
        }
        
        private async UniTaskVoid PlayNewBGMAfter()
        {
            await UniTask.WaitForSeconds(1f);
            SoundManager.Instance.PlayBGM(SoundName.BGM.GamePlayPhase1, fadeIn: 2f);
            await UniTask.WaitUntil(() => GameStateController.Instance.CurrentState is StartState, cancellationToken: destroyCancellationToken);
            await UniTask.WaitUntil(() => GameTimer.Instance.GlobalTimer < 450,  cancellationToken: destroyCancellationToken);
            SoundManager.Instance.PlayBGM(SoundName.BGM.GamePlayPhase2, fadeOut:2.5f, fadeIn:2.5f);
        }
    }
}
