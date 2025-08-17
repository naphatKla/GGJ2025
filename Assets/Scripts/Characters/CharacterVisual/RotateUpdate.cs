using UnityEngine;

namespace Characters.CharacterVisual
{
    public class RotateUpdate : MonoBehaviour
    {
        [Header("Target")] [Tooltip("ถ้าเว้นว่าง จะใช้ transform ของตัวเอง")] [SerializeField]
        private Transform target;

        [Header("Rotation")] [Tooltip("ความเร็วหมุน (องศาต่อวินาที)")] [Min(0f)] [SerializeField]
        private float speed = 180f;

        [Header("Direction")] [Tooltip("true = ตามเข็ม (บนแกน Z คือมุมติดลบ), false = ทวนเข็ม")] [SerializeField]
        private bool clockwise = true;

        [Tooltip("สุ่มทิศทางตอนเริ่ม (จะ override ค่า clockwise)")] [SerializeField]
        private bool randomizeDirectionOnStart = false;

        [Header("Random Start Angle")] [Tooltip("สุ่มมุมเริ่มต้นบนแกน Z")] [SerializeField]
        private bool randomizeStartAngle = false;

        [SerializeField] private Vector2 randomStartAngleRange = new Vector2(0f, 360f);

        [Header("Run Options")] [Tooltip("ใช้ UnscaledDeltaTime แทน DeltaTime")] [SerializeField]
        private bool useUnscaledTime = false;

        [Tooltip("เริ่มหมุนอัตโนมัติเมื่อ Enable")] [SerializeField]
        private bool autoStart = true;

        private bool _running;

        private void Awake()
        {
            if (target == null) target = transform;
        }

        private void OnEnable()
        {
            if (randomizeDirectionOnStart)
                clockwise = Random.value < 0.5f;

            if (randomizeStartAngle)
                ApplyRandomStartAngle();

            _running = autoStart;
        }

        private void Update()
        {
            if (!_running || !target) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float sign = clockwise ? -1f : 1f;
            float deltaDegrees = sign * speed * dt;

            target.Rotate(0f, 0f, deltaDegrees, Space.Self);
        }

        // ==== Public API ====
        public void StartRotate() => _running = true;
        public void StopRotate() => _running = false;
        public void SetDirectionClockwise(bool isClockwise) => clockwise = isClockwise;
        public void RandomizeDirection() => clockwise = Random.value < 0.5f;
        public void SetSpeed(float degPerSec) => speed = Mathf.Max(0f, degPerSec);

        public void ApplyRandomStartAngle()
        {
            if (target == null) return;
            float ang = Random.Range(randomStartAngleRange.x, randomStartAngleRange.y);
            var e = target.localEulerAngles;
            e.z = ang;
            target.localEulerAngles = e;
        }
    }
}