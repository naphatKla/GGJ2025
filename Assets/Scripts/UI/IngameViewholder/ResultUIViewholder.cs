using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Characters.Controllers;
using Characters.Data;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl.Controller;
using GameControl.SO;
using Player;
using TMPro;
using UI.MapSelection;
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
        [SerializeField] private Button nextButton;
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

            nextButton?.onClick.RemoveAllListeners();
            nextButton?.onClick.AddListener(NextClick);
        }

        private void NextClick()
        {
            var map = GameStateController.Instance.CurrentMap;
            var profile = ActiveProfileService.Instance?.CurrentProfile;

            // ไม่มี Milestone ถัดไปให้เล่น -> เลือกด่านถัดไปไว้ให้เลย
            if (profile != null && !HasNextMilestoneUnlocked(profile, map))
            {
                var nextMap = GetNextMapUnlocked(profile, map);
                if (nextMap != null) profile.selectedMapIds = nextMap.mapId;
            }

            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.NextToMapSelection(map != null && map.endlessMode);
        }

        /// <summary>
        /// โชว์ Next เมื่อชนะ หรือ (แม้แพ้) Milestone ถัดไปของด่านนี้ปลดแล้ว หรือด่านถัดไปปลดแล้ว.
        /// รอ 1 เฟรม เพราะ SummaryState เปิดหน้านี้ก่อนจะเซฟผล / ปลด Achievement (ปลดด่าน)
        /// </summary>
        private async UniTaskVoid RefreshNextButton()
        {
            if (!nextButton) return;
            nextButton.gameObject.SetActive(false);

            await UniTask.NextFrame(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

            var map = GameStateController.Instance.CurrentMap;
            var profile = ActiveProfileService.Instance?.CurrentProfile;
            bool show = GameStateController.Instance.gameResult == EndResult.Completed ||
                        (profile != null && map != null &&
                         (HasNextMilestoneUnlocked(profile, map) || GetNextMapUnlocked(profile, map) != null));
            if (nextButton) nextButton.gameObject.SetActive(show);
        }

        private static bool HasNextMilestoneUnlocked(PlayerData profile, MapDataSO map)
        {
            if (map == null || !profile.MapStats.TryGetValue(map.mapId, out var stat)) return false;
            int played = stat.SelectedLevelMilestone;
            return played < map.MaxMilestoneLevel && stat.MaxLevelUnlockMilestone > played;
        }

        private static MapDataSO GetNextMapUnlocked(PlayerData profile, MapDataSO map)
        {
            var list = MapSelectionSender.Instance.currentmapSelectionDataContainer?.mapSelectionList;
            if (map == null || list == null) return null;

            int index = list.FindIndex(m => m != null && m.mapId == map.mapId);
            if (index < 0 || index + 1 >= list.Count) return null;

            var next = list[index + 1];
            return next != null && profile.UnlockedMaps.Contains(next.mapId) ? next : null;
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

            RefreshNextButton().Forget();

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
            var svc = ActiveProfileService.Instance;
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
            
            //10% of Currency
            var currency = (dataStatus.totalScore / 100) * 1;
            sb.AppendLine($"<color=#6ea9e4>Nano Coin :</color> <color=#52d137>{svc.CurrentProfile.nanoCoin}</color> + <color=#ffed48>{currency}</color>");
            
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