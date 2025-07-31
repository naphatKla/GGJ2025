using System;
using System.Collections;
using System.Collections.Generic;
using GameControl.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ResultUIModal : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text middleText;

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
            GameStateController.Instance.RestartMap();
            UIManager.Instance.CloseAllPanels();
        }
        
        private void BackClick()
        {
            UIManager.Instance.BackMenu();
            UIManager.Instance.CloseAllPanels();
        }

        private void UpdateUIText()
        {
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
    }
}