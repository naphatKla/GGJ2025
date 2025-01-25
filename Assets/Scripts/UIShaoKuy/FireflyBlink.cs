using UnityEngine;

public class FireflyBlink : MonoBehaviour
{
    private Light pointLight;
    private float blinkSpeed;

    void Start()
    {
        pointLight = GetComponent<Light>();
        blinkSpeed = Random.Range(0.5f, 2f);
    }

    void Update()
    {
        pointLight.intensity = Mathf.PingPong(Time.time * blinkSpeed, 1f);
    }
}