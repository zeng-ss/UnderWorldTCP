using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    /// <summary>
    /// 头像面板里的一个格子（Slot{i}）。引用在预制体里连好，代码不按名字找。
    /// </summary>
    public class AvatarSlot : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _head;
        [SerializeField] private Image _selected;

        public void Init(Sprite sprite, UnityAction onClick)
        {
            _head.sprite = sprite;
            _selected.enabled = false;
            _button.onClick.AddListener(onClick);
        }

        public void SetSelected(bool value) => _selected.enabled = value;
    }
}
