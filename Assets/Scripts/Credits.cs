using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Video; // หากใช้ TextMeshPro

public class Credits : MonoBehaviour
{ 
    public RectTransform creditsText;
    public float scrollSpeed = 50f; 
    public float endPositionY = 1000f;
    private Vector3 startPosition;
    public VideoPlayer bubble;

    void Start()
    {
        startPosition = creditsText.localPosition; 
        /*bubble.isLooping = true;
        bubble.Play();*/
    }

    void Update()
    {
        creditsText.localPosition += Vector3.up * scrollSpeed * Time.deltaTime;
        
        if (creditsText.localPosition.y >= endPositionY)
        {
            SceneManager.LoadScene("MainMenu");
        }

        if (Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}