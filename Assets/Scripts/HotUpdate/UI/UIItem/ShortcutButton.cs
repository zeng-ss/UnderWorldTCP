using HotUpdate.Data;
using HotUpdate.RedDot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIItem
{
    // 悬停放大是通用组件 HoverScale，别的按钮想用直接挂同一个组件即可
    public class ShortcutButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject redDot;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI keyTxt, desTxt;

        private RedDotNode _redDotNode;

        private void Awake()
        {
            // 模板上没挂悬停动画就补一个，保证快捷格一定有悬停反馈
            if (!TryGetComponent<HoverScale>(out _)) gameObject.AddComponent<HoverScale>();
        }

        public void Init(ShortcutBtnData data)
        {
            desTxt.text = data.name;
            keyTxt.text = KeyToText(data.key);
            Sprite sprite = Resources.Load<Sprite>(data.spritePath);
            if (sprite is not null)
            {
                icon.sprite = sprite;
                icon.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning($"[ShortcutButton] 加载图标失败：{data.spritePath}");
            }

            if (!AppContext.IsAlive) return;

            // 开关动作与红点路径都来自 PanelShortcut 总表；Config 只决定「长什么样、开哪个面板」
            PanelShortcut shortcut = PanelShortcut.Get(data.panel);
            if (shortcut == null)
            {
                Debug.LogWarning(
                    $"[ShortcutButton] Config 里的「{data.name}」没有对应的 PanelShortcut（PanelId={data.panel}）!");
                return;
            }

            if (shortcut.Key != data.key)
            {
                Debug.LogWarning(
                    $"[ShortcutButton] 「{data.name}」的按键不一致：Config={KeyToText(data.key)}，键盘注册={KeyToText(shortcut.Key)}!");
            }

            button?.onClick.AddListener(() => shortcut.Toggle());
            // 红点交给红点树：只订阅自己那一格对应的节点，数值变了才动显隐
            _redDotNode = AppContext.RedDot.GetOrCreate(shortcut.DotPath);
            _redDotNode.AddWatcher(SetRedDotVisible);
        }

        private static string KeyToText(KeyCode key) => key == KeyCode.Tab ? "Tab" : key.ToString();

        private void OnDestroy() => _redDotNode?.RemoveWatcher(SetRedDotVisible);

        // 红点数 > 0 才亮
        private void SetRedDotVisible(int count)
        {
            if (redDot is null) return;

            bool show = count > 0;
            if (redDot.activeSelf != show) redDot.SetActive(show);
        }
    }
}