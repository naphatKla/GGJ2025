using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float Damage { get; set; } = 10f;
    public float Health { get; set; } = 100f;
    public float Speed { get; set; } = 5f;

    void Start()
    {
        Debug.Log($"Initial Stats: Damage={Damage}, Health={Health}, Speed={Speed}");
    }
}