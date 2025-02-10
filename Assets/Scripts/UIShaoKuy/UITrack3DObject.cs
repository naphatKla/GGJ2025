using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class UITrack3DObject : MonoBehaviour
{
    [SerializeField] private Transform objectInScene;
    [SerializeField] private GameObject UiObj;

    private Camera cam;
    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 screenPos = cam.WorldToScreenPoint(objectInScene.position);
        UiObj.transform.position = new Vector3(screenPos.x, screenPos.y);
    }
}
