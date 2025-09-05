using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Characters.Controllers;
using GameControl.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameViewholder
{
    public class ResultUIViewholder : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text middleText;
        [SerializeField] private TMP_Text summaryText;

        private void OnEnable()
        {
            UpdateUIText();
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
            UIManager.Instance.CloseAllPanels();
            GameStateController.Instance.RestartMap();
        }
        
        private void BackClick()
        {
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.BackMenu();
        }

        private void UpdateUIText()
        {
            summaryText.text = GroupStatus().ToString();
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

        private StringBuilder GroupStatus()
        {
            var dataStatus = PlayerController.Instance.GetSummaryStatsOnStateEnd();
            var sb = new System.Text.StringBuilder(256);

            sb.AppendLine($"<color=#aeb0af>Total score :</color> <color=yellow>{dataStatus.totalScore}</color>");
            sb.AppendLine($"<color=#aeb0af>Current level :</color> <color=#00FF00>{dataStatus.currentLevel}</color>");
            sb.AppendLine($"<color=#aeb0af>Highest Rank :</color> <color=#00FFFF>{(string.IsNullOrEmpty(dataStatus.highestRank) ? "-" : dataStatus.highestRank)}</color>");
            sb.AppendLine($"<color=#aeb0af>Highest Streak Count :</color> <color=#FFA500>{dataStatus.highestStreakCount}</color>");
            sb.AppendLine($"<color=#aeb0af>Average Exp Multiplier :</color> <color=#FF69B4>{dataStatus.averageExpMultiplier:0.###}</color>");
            sb.AppendLine($"<color=#aeb0af>Total Enemies Eliminated :</color> <color=#FF0000>{dataStatus.totalEnemiesEliminated}</color>");
            sb.AppendLine($"<color=#aeb0af>Total Damage Deal :</color> <color=#FF4500>{dataStatus.totalDamageDeal}</color>");
            sb.AppendLine($"<color=#aeb0af>Critical Count :</color> <color=#FFD700>{dataStatus.criticalCount}</color>");
            sb.AppendLine($"<color=#aeb0af>Total Counter Dash Count :</color> <color=#ADFF2F>{dataStatus.totalCounterDashCount}</color>");
            sb.AppendLine($"<color=#aeb0af>Total Primary Skill Used :</color> <color=#00CED1>{dataStatus.totalPrimarySkillUsed}</color>");
            sb.AppendLine($"<color=#aeb0af>Total Secondary Skill Used :</color> <color=#1E90FF>{dataStatus.totalSecondarySkillUsed}</color>");
            sb.AppendLine($"<color=#aeb0af>Total Auto Skill Used :</color> <color=#BA55D3>{dataStatus.totalAutoSkillUsed}</color>");
            sb.AppendLine($"<color=#aeb0af>Total Damage Taken :</color> <color=#DC143C>{dataStatus.totalDamageTaken}</color>");
            sb.AppendLine($"<color=#aeb0af>Total Heal :</color> <color=#32CD32>{dataStatus.totalHeal}</color>");

            return sb;
        }

    }
}