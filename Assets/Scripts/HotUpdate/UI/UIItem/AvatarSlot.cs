using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    /// <summary>
    /// 头像面板里的一个格子（Slot{i}）。引用在预制体里连好，代码不按名字找。
    /// </summary>
    public class AvatarSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [FormerlySerializedAs("_button")] [SerializeField]
        private Button button;

        [FormerlySerializedAs("_head")] [SerializeField]
        private Image head;

        [FormerlySerializedAs("_selected")] [SerializeField]
        private Image selected;

        [SerializeField] private Image hoverBg;

        public void Init(Sprite sprite, UnityAction onClick)
        {
            head.sprite = sprite;
            selected.enabled = false;
            hoverBg.enabled = false;
            button.onClick.AddListener(onClick);
        }

        public void SetSelected(bool value) => selected.enabled = value;

        public void OnPointerEnter(PointerEventData eventData)
        {
            hoverBg.enabled = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hoverBg.enabled = false;
        }
    }
}