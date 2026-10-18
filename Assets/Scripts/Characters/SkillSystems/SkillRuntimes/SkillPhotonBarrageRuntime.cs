using System.Threading;
using Characters.Controllers;
using Characters.SkillSystems.SkillObjects;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// ปล่อยกระสุนรอบตัวแล้วจบทันที กระสุนแต่ละนัดบินและคืน pool เอง
    /// </summary>
    public class SkillPhotonBarrageRuntime : BaseSkillRuntime<SkillPhotonBarrageDataSo>
    {
        private readonly PhotonTargetClaims _claims = new();

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            PoolingManager.Instance.Create<PhotonBarrageSkillObject>(
                this.skillData.MissilePrefab.name,
                PoolingGroupName.SkillObject,
                CreatePoolInstance,
                prewarmCount: this.skillData.MissileCount * 2); // cooldown ต่ำ → กระสุนซ้อนหลายชุด
        }

        protected override void OnSkillStart()
        {
            LayerMask enemyLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            string ownerId = owner.CharacterData.CharacterId;

            int count = skillData.MissileCount;
            float baseAngle = Random.value * 360f; // หมุนรูปแบบทุกครั้ง ไม่ซ้ำแพทเทิร์นเดิม

            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle + i * 360f / count) * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));

                var missile = PoolingManager.Instance.Get<PhotonBarrageSkillObject>(skillData.MissilePrefab.name);
                missile.transform.position = owner.transform.position;
                missile.ResetTrail();
                missile.gameObject.SetActive(true);
                missile.Launch(skillData, owner.transform, ownerId, enemyLayer, dir, _claims);
            }
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken) => UniTask.CompletedTask;

        protected override void OnSkillExit() { }

        private PhotonBarrageSkillObject CreatePoolInstance()
        {
            var obj = Instantiate(skillData.MissilePrefab);
            obj.gameObject.SetActive(false);
            obj.transform.position = owner.transform.position;
            return obj;
        }
    }
}