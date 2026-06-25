using System.Threading;
using Characters.Controllers;
using Characters.SkillSystems.SkillObjects;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillNovaCutter : BaseSkillRuntime<SkillNovaCutterDataSo>
    {
        private NovaCutterSkillObject _cutterObj;
        
        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
           _cutterObj = Instantiate(base.skillData.CutterObj, owner.Body.transform);
           _cutterObj.transform.localPosition += (Vector3)base.skillData.CutterOffset;
           _cutterObj.gameObject.SetActive(false);
        }

        protected override void OnSkillStart()
        {
            _cutterObj.gameObject.SetActive(true);
            _cutterObj.DamageOnTouch.EnableDamage(owner.gameObject,owner.CharacterData.CharacterId ,this, skillData.DamageHitPerSec, skillData.BaseDamagePerHit, skillData.DamageMultiplier);
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.Delay((int)(skillData.Duration * 1000), cancellationToken: cancelToken);
        }

        protected override void OnSkillExit()
        {
            _cutterObj.DamageOnTouch.DisableDamage(this);
            _cutterObj.gameObject.SetActive(false);
        }

        protected void OnDestroy()
        {
            if (!_cutterObj) return;
            Destroy(_cutterObj);
        }
    }
}
