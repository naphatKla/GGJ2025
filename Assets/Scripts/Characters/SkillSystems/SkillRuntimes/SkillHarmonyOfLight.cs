using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.SkillSystems.SkillObjects;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillHarmonyOfLight : BaseSkillRuntime<SkillHarmonyOfLightDataSo>
    {
        private List<HarmonyOfLightSkillObject> _skillObjects = new();
        private Vector3 startPos;
        private float startYScale;

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            PoolingManager.Instance.Create<HarmonyOfLightSkillObject>(this.skillData.LightPrefab.name,
                PoolingGroupName.SkillObject,
                CreatePoolInstance);
        }

        protected override void OnSkillStart()
        {
            _skillObjects.Clear();

            float angleStep = 360f / (skillData.LightAmount * 2);

            for (int i = 0; i < skillData.LightAmount; i++)
            {
                var skillInstance = PoolingManager.Instance.Get<HarmonyOfLightSkillObject>(skillData.LightPrefab.name);
                skillInstance.transform.position = owner.transform.position;
                startYScale = skillInstance.transform.localScale.y;
                
                float angle = angleStep * i;
                skillInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                skillInstance.gameObject.SetActive(true);
                _skillObjects.Add(skillInstance);
            }

            startPos = owner.transform.position;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            float elapsed = 0f;
            float spinSpeed = 360f * skillData.SpinRatePerSec;
            
            while (elapsed < skillData.SpinDuration)
            {
                if (cancelToken.IsCancellationRequested) break;

                float delta = Time.deltaTime;
                elapsed += delta;

                var center = startPos;
                foreach (var obj in _skillObjects)
                {
                    obj.transform.RotateAround(center, Vector3.forward, -spinSpeed * delta); // clockwise
                }

                await UniTask.Yield();
            }
        }

        protected override void OnSkillExit()
        {
            foreach (var obj in _skillObjects)
            {
                obj.gameObject.SetActive(false);
                PoolingManager.Instance.Release(skillData.LightPrefab.name, obj);
            }
        }

        private HarmonyOfLightSkillObject CreatePoolInstance()
        {
            HarmonyOfLightSkillObject skillObj = Instantiate(skillData.LightPrefab);
            skillObj.gameObject.SetActive(false);
            skillObj.transform.position = owner.transform.position;
            return skillObj;
        }
    }
}