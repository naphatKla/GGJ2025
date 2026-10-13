using Characters.Controllers;
using UnityEngine;

namespace GameControl.EventMap
{
    /// <summary>
    /// Special Interaction for a single Border piece (Design Ver 0.0.20): "When a piece of Border enters
    /// the (Devourer) damage radius those pieces disappear." No explosion, no damage - it is simply eaten.
    /// <para/>
    /// Added at runtime by <see cref="BorderMapEvent"/> to every piece it pools, so border enemy prefabs
    /// don't have to carry a component that only matters while they belong to a border.
    /// </summary>
    [DisallowMultipleComponent]
    public class BorderPieceSpecialInteraction : MonoBehaviour, ISpecialInteractionTarget
    {
        private BorderMapEvent _border;
        private EnemyController _piece;

        public void Bind(BorderMapEvent border, EnemyController piece)
        {
            _border = border;
            _piece = piece;
        }

        public Transform Transform => transform;

        public bool CanTriggerSpecialInteraction => _border && _piece && gameObject.activeInHierarchy;

        public void TriggerSpecialInteraction(SpecialInteractionContext context)
        {
            if (!CanTriggerSpecialInteraction) return;

            SpecialInteractionRegistry.Unregister(this);
            _border.RemovePiece(_piece);
        }

        // Border pieces are pooled, so these run on every spawn / recycle.
        private void OnEnable() => SpecialInteractionRegistry.Register(this);
        private void OnDisable() => SpecialInteractionRegistry.Unregister(this);
    }
}
