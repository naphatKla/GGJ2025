using UnityEngine;

namespace UI.Leaderboard
{
    public enum ItemPrefabKind { Default = 0, Top1 = 1, Top2 = 2, Top3 = 3 }

    public class LeaderboardCellTag : MonoBehaviour
    {
        public ItemPrefabKind kind = ItemPrefabKind.Default;
    }
}