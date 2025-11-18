using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Characters.Controllers;
using Characters.Data;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl.Controller;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameViewholder
{
    [Serializable]
    public struct GradeCombo
    {
        public string gradeId;
        public Sprite gradeImage;
    }
    
    public class ResultUIViewholder : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text middleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text newRecordText;
        [SerializeField] private Image gradeImage;
        public List<GradeCombo> gradeComboResult;
        Tween _countTween;

        private void OnEnable()
        {
            UpdateUIText();
        }
        
        private void OnDisable()
        {
            if (scoreText) DOTween.Kill(scoreText, complete: false);
            if (gradeImage) DOTween.Kill(gradeImage, complete: false);
            
            scoreText?.transform.DOKill();
            gradeImage?.transform.DOKill();
            newRecordText?.transform.DOKill();
            transform.DOKill();
        }


        private void Start()
        {
            restartButton?.onClick.RemoveAllListeners();
            restartButton?.onClick.AddListener(RestartClick);
            
            backButton?.onClick.RemoveAllListeners();
            backButton?.onClick.AddListener(BackClick);
        }

        private void RestartClick()
        {
            UIManager.Instance.ShowConfirmButton(
                "Restart",
                onYes: () =>
                {
                    UIManager.Instance.CloseAllPanels();
                    GameStateController.Instance.RestartMap();
                },
                onNo: null,
                durationSec: 8f
            ).Forget();
        }
        
        private void BackClick()
        {
            UIManager.Instance.ShowConfirmButton(
                "Leave",
                onYes: () => 
                { 
                    UIManager.Instance.BackMenu();
                    UIManager.Instance.CloseAllPanels();
                },
                onNo: null,
                durationSec: 8f
            ).Forget();
        }

        private void UpdateUIText()
        {
            var dataStatus = PlayerController.Instance.GetSummaryStatsOnStateEnd();
            summaryText.text = GroupStatus(dataStatus).ToString();
            newRecordText.gameObject.SetActive(false);
            ShowResultFeedback(dataStatus).Forget();
            
            switch (GameStateController.Instance.gameResult)
            {
                case EndResult.Completed:
                    middleText.text = "COMPLETE";
                    middleText.color = Color.yellow;
                    break;
                case EndResult.Failed:
                    middleText.text = "FAILED";
                    middleText.color = Color.red;
                    break;
                case EndResult.None:
                    middleText.text = "COMPLETE";
                    middleText.color = Color.yellow;
                    break;
            }
        }

        private async UniTask ShowResultFeedback(PlayerSummaryStats dataStatus)
        {
            try
            {
                await UpdateGradeResult(dataStatus.currentRank);
                await PlayCountUpAsync(dataStatus.totalScore, 2);
                CheckHighestScore(dataStatus);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        
        public async UniTask UpdateGradeResult(string grade)
        {
            foreach (var g in gradeComboResult)
                if (g.gradeId.ToLower() == grade.ToLower())
                {
                    gradeImage.sprite = g.gradeImage;
                    await GradeFeedback(gradeImage);
                    break;
                }
        }

        private void CheckHighestScore(PlayerSummaryStats dataStatus)
        {
            var svc = ActiveProfileService.Instance;
            if (svc?.CurrentProfile == null) return;
            int newScore = Mathf.Max(0, dataStatus.totalScore);
            if (newScore == svc?.CurrentProfile.HighestScore) NewRecordFeedback(newRecordText.transform).Forget();
        }
        
        private async UniTask NewRecordFeedback(Transform tf)
        {
            if (!tf) return;
            tf.DOKill();
            tf.gameObject.SetActive(true);
            var seq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(tf.gameObject, LinkBehaviour.KillOnDestroy)
                .SetTarget(tf);

            await seq
                .Append(tf.DOScale(8.0f, 0.0f).SetUpdate(true).SetLink(tf.gameObject, LinkBehaviour.KillOnDestroy))
                .Append(tf.DOScale(5.5f, 0.5f).SetEase(Ease.InExpo).SetUpdate(true)
                    .SetLink(tf.gameObject, LinkBehaviour.KillOnDestroy))
                .AsyncWaitForCompletion();
        }
        
        private async UniTask GradeFeedback(Image obj)
        {
            if (!obj) return;
            var tf = obj.transform;
            var seq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(obj.gameObject, LinkBehaviour.KillOnDestroy)
                .SetTarget(obj);

            if (tf) tf.localScale = Vector3.one;

            await seq
                .Append(tf.DOScale(1.6f, 0.0f).SetUpdate(true).SetLink(obj.gameObject, LinkBehaviour.KillOnDestroy))
                .Append(tf.DOScale(1.0f, 0.5f).SetEase(Ease.InExpo).SetUpdate(true)
                    .SetLink(obj.gameObject, LinkBehaviour.KillOnDestroy))
                .AsyncWaitForCompletion();
        }
        
        private StringBuilder GroupStatus(PlayerSummaryStats dataStatus)
        {
            var sb = new StringBuilder(256);
            
            sb.AppendLine($"<color=#aeb0af>Level :</color> <color=#00FF00>{dataStatus.currentLevel}</color>");
            sb.AppendLine($"<color=#aeb0af>Enemies Eliminated :</color> <color=#FF0000>{dataStatus.totalEnemiesEliminated}</color>");
            sb.AppendLine($"<color=#aeb0af>Damage Deal :</color> <color=#FF4500>{dataStatus.totalDamageDeal}</color>");
            sb.AppendLine($"<color=#aeb0af>Critical Count :</color> <color=#FFD700>{dataStatus.criticalCount}</color>");
            sb.AppendLine($"<color=#aeb0af>Counter Dash Count :</color> <color=#ADFF2F>{dataStatus.totalCounterDashCount}</color>");
            sb.AppendLine($"<color=#aeb0af>Primary Skill Used :</color> <color=#00CED1>{dataStatus.totalPrimarySkillUsed}</color>");
            sb.AppendLine($"<color=#aeb0af>Secondary Skill Used :</color> <color=#1E90FF>{dataStatus.totalSecondarySkillUsed}</color>");
            sb.AppendLine($"<color=#aeb0af>Auto Skill Used :</color> <color=#BA55D3>{dataStatus.totalAutoSkillUsed}</color>");
            sb.AppendLine($"<color=#aeb0af>Damage Taken :</color> <color=#DC143C>{dataStatus.totalDamageTaken}</color>");
            sb.AppendLine($"<color=#aeb0af>Heal :</color> <color=#32CD32>{dataStatus.totalHeal}</color>");

            return sb;
        }

        public async UniTask PlayCountUpAsync(int targetScore, float duration = 3f, bool ignoreTimeScale = true)
        {
            if (!scoreText) return;

            _countTween?.Kill();

            int current = 0;
            _countTween = DOTween
                .To(() => current, x =>
                {
                    current = x;
                    if (scoreText) scoreText.text = current.ToString("#,0");
                }, targetScore, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(ignoreTimeScale)
                .SetLink(scoreText.gameObject, LinkBehaviour.KillOnDestroy)
                .SetTarget(scoreText);

            var ct = this.GetCancellationTokenOnDestroy();

            try
            {
                await UniTask.WhenAny(
                        _countTween.AsyncWaitForCompletion().AsUniTask(),
                        _countTween.AsyncWaitForKill().AsUniTask()
                    ).AttachExternalCancellation(ct)
                    .SuppressCancellationThrow();
            }
            catch (OperationCanceledException) { }
        }

    }
}