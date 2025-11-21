using System.Threading;
using Characters.Controllers;
using Characters.SkillSystems.SkillObjects;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using Manager;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillSpaceMineRuntime : BaseSkillRuntime<SkillSpaceMineDataSo>
    {
        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            PoolingManager.Instance.Create<SpaceMineSkillObject>(this.skillData.SpaceMineSkillObject.name, PoolingGroupName.SkillObject, CreatePoolInstance);
        }

        protected override void OnSkillStart()
        {
            PlaceBomb().Forget();
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.CompletedTask;
        }

        protected override void OnSkillExit()
        {
       
        }

        private SpaceMineSkillObject CreatePoolInstance()
        {
            SpaceMineSkillObject skillObj = Instantiate(skillData.SpaceMineSkillObject);
            skillObj.gameObject.SetActive(false);
            skillObj.transform.position = owner.transform.position;
            return skillObj;
        }

        private async UniTask PlaceBomb()
        {
            var bomb = PoolingManager.Instance.Get<SpaceMineSkillObject>(skillData.SpaceMineSkillObject.name);
            bomb.transform.position = owner.transform.position;
            bomb.gameObject.SetActive(true);
            await bomb.WaitPlaceBombAsync();
            bomb.DamageOnTouch.EnableDamage(owner.gameObject,owner.CharacterData.CharacterId, this, 1, skillData.BaseDamage, skillData.DamageMultiplier);
            await UniTask.Yield();
            bomb.DamageOnTouch.DisableDamage(this);
            bomb.gameObject.SetActive(false);
            PoolingManager.Current?.Release(skillData.SpaceMineSkillObject.name, bomb);
        }
    }
}
