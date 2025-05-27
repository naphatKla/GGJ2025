/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using DG.Tweening;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample02
{
    class Cell : FancyCell<ItemData, Context>
    {
        [SerializeField] Animator animator = default;
        [SerializeField] Text message = default;
        [SerializeField] Image image = default;
        [SerializeField] Button button = default;

        static class AnimatorHash
        {
            public static readonly int Scroll = Animator.StringToHash("scroll");
        }

        public override void Initialize()
        {
            button.onClick.AddListener(() => Context.OnCellClicked?.Invoke(Index));
        }

        public override void UpdateContent(ItemData itemData)
        {
            var selected = Context.SelectedIndex == Index;
            
            if (message != null)
                message.text = itemData.Message;

            if (image != null && itemData.Image != null)
            {
                image.sprite = itemData.Image;

                image.color = selected
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 100);
            }
            
            transform.DOKill();

            if (selected)
            {
                transform
                    .DOScale(1.1f, 1f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else { transform.DOScale(1f, 0.2f).SetEase(Ease.OutSine); }
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
        
        float currentPosition = 0;

        void OnEnable() => UpdatePosition(currentPosition);
    }
}
