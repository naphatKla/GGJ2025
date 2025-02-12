using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectorButton : MonoBehaviour
{
    [SerializeField] private Sprite toggle;
    [SerializeField] private Sprite normal;
    [SerializeField] private LevelSelector levelSelectorPar;
    [SerializeField] private GameObject NextButton;
    private Image _image;
    void Start()
    {
        _image = GetComponent<Image>();
        levelSelectorPar = FindObjectOfType<LevelSelector>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        levelSelectorPar.SetLevel(this);
        NextButton.SetActive(true);
    }

    public void OnToggle()
    {
        _image.sprite = toggle;
        transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
    }

    public void TurnNormal()
    {
        _image.sprite = normal;
        transform.localScale = new Vector3(1f, 1f, 1f);
    }
}
