using UnityEngine;

/// <summary>
/// Simple Bob chevron indicator - วางเป็น child ของ Player prefab
/// จะตามตำแหน่ง parent อัตโนมัติ และคงทิศแนวตั้งแม้ player หมุน
/// </summary>
public class PlayerChevronIndicator : MonoBehaviour
{
    [Header("Position")]
    [Tooltip("ระยะห่างเหนือ player (หน่วย Unity)")]
    [SerializeField] private float heightAbove = 1.0f;

    [Header("Bob Animation")]
    [Tooltip("ระยะเด้งขึ้น-ลง")]
    [SerializeField] private float bobAmplitude = 0.1f;

    [Tooltip("ความถี่การเด้ง (รอบต่อวินาที)")]
    [SerializeField] private float bobFrequency = 1.5f;

    [Header("Rotation")]
    [Tooltip("ให้ chevron คงทิศแนวตั้ง ไม่หมุนตาม player")]
    [SerializeField] private bool keepUpright = true;

    private Transform parentTransform;

    private void Awake()
    {
        parentTransform = transform.parent;
        if (parentTransform == null)
        {
            Debug.LogWarning("[PlayerChevronIndicator] ไม่พบ parent — ควรวางเป็น child ของ Player");
        }
    }

    private void LateUpdate()
    {
        if (parentTransform == null) return;

        // เด้งขึ้น-ลงด้วย sine wave
        float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;

        // วาง chevron เหนือ parent ใน world space (ไม่ขึ้นกับการหมุนของ player)
        transform.position = parentTransform.position + new Vector3(0f, heightAbove + bob, 0f);

        // กัน chevron ไม่ให้หมุนตามยาน
        if (keepUpright)
        {
            transform.rotation = Quaternion.identity;
        }
    }
}