/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace UI.MapSelection
{
    class MapSelectionViewHolder : FancyCell<MapSelectionItemModel, MapSelectionContext>
    {
        [SerializeField] Animator animator = default;
        [Title("Map Selection")]
        [SerializeField] Button button = default;
        [SerializeField] private Image backgroundSelection;
        [SerializeField] private TMP_Text selectionText;
        
        //Preview
        [Title("Map Preview")]
        [SerializeField] private Image backgroundPreview;
        [SerializeField] private Image mapImage;
        [SerializeField] private TMP_Text mapNameText;
        

        static class AnimatorHash
        {
            public static readonly int Scroll = Animator.StringToHash("scroll");
        }

        void Start()
        {
            button.onClick.AddListener(() => Context.OnCellClicked?.Invoke(Index));
        }

        public override void UpdateContent(MapSelectionItemModel itemData)
        {
            selectionText.text = itemData.MapName;
            mapNameText.text = "Map " + itemData.MapName;
            mapImage.sprite = itemData.MapImage;

            //messageLarge.text = Index.ToString();

            var selected = Context.SelectedIndex == Index;
            backgroundPreview.color = backgroundSelection.color = selected
                ? new Color32(0, 255, 255, 100)
                : new Color32(255, 255, 255, 77);
        }

        public override void UpdatePosition(float position)
        {
            currentPosition = position;

            if (animator.isActiveAndEnabled)
            {
                animator.Play(AnimatorHash.Scroll, -1, position);
            }

            animator.speed = 0;
        }

        // GameObject が非アクティブになると Animator がリセットされてしまうため
        // 現在位置を保持しておいて OnEnable のタイミングで現在位置を再設定します
        float currentPosition = 0;

        void OnEnable() => UpdatePosition(currentPosition);
    }
}
