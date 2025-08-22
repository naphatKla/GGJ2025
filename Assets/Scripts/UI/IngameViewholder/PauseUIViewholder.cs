using System;
using System.Collections;
using System.Collections.Generic;
using GameControl.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameViewholder
{
    public class PauseUIViewholder : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            restartButton?.onClick.RemoveAllListeners();
            restartButton?.onClick.AddListener(RestartClick);
            
            continueButton?.onClick.RemoveAllListeners();
            continueButton?.onClick.AddListener(ContinueClick);
            
            backButton?.onClick.RemoveAllListeners();
            backButton?.onClick.AddListener(BackClick);
            
            quitButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.AddListener(QuitClick);
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
        
        private void QuitClick()
        {
            UIManager.Instance.QuitGame();
        }
    }
}
