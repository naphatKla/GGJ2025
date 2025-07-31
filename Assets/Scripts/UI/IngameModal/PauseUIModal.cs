using System;
using System.Collections;
using System.Collections.Generic;
using GameControl.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PauseUIModal : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingButton;
        [SerializeField] private Button backButton;

        private void Start()
        {
            restartButton?.onClick.RemoveAllListeners();
            restartButton?.onClick.AddListener(RestartClick);
            
            continueButton?.onClick.RemoveAllListeners();
            continueButton?.onClick.AddListener(ContinueClick);
            
            backButton?.onClick.RemoveAllListeners();
            backButton?.onClick.AddListener(BackClick);
        }
        
        private void BackClick()
        {
            UIManager.Instance.BackMenu();
            UIManager.Instance.CloseAllPanels();
        }

        private void RestartClick()
        {
            GameStateController.Instance.RestartMap();
            UIManager.Instance.CloseAllPanels();
        }

        private void ContinueClick()
        {
            UIManager.Instance.OpenPausePanel();
        }
    }
}
