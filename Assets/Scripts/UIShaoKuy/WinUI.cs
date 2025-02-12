using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using Unity.VisualScripting;
using UnityEngine;

public class WinUI : MonoBehaviour
{
    [SerializeField] private GameObject[] items;
    private int _count = 0;
    private float firstTimeOpenTime = 0.01f;

    private Coroutine dd;
    // Update is called once per frame
    private void Start()
    {
        dd = StartCoroutine(PlayGameObject());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StopCoroutine(dd);
            if (items[_count-1].TryGetComponent<AddNumberLoop>(out AddNumberLoop anl))
            {
                anl.SetToTargetNumber();
                anl.StopAllCoroutines();
            }
            
            if (_count < items.Length)
            {
                ShowGameObject();
                StartCoroutine(PlayGameObject());
            }
        }
    }


    private IEnumerator PlayGameObject()
    {
        yield return new WaitForSeconds(firstTimeOpenTime);
        ShowGameObject();
        if (_count >= items.Length)
        {
            yield break;
        }
        firstTimeOpenTime = 1;
        StartCoroutine(PlayGameObject());
    }

    private void ShowGameObject()
    {
        items[_count].SetActive(true);
        _count++;
    }
}
