using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Manager;
using HotUpdate.UI.UIItem;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 头像选择面板：Grid 下按顺序摆好 Slot0..SlotN（每个挂 AvatarSlot），
    /// 点一下只做本地选中，点「保存」才提交服务端落库。
    /// 格子数组下标 = 头像编号，长度须与 AvatarConfig.Count 一致；
    /// 引用全部在预制体里连（见 Tools/UI/头像面板 生成器），代码不按名字找。
    /// </summary>
    [PanelPath("AvatarPanel")]
    public class AvatarPanel : BasePanel
    {
        [SerializeField] private Button _saveBtn;
        [SerializeField] private AvatarSlot[] _slots;

        private int _selected;
        private bool _saving;

        protected override void Awake()
        {
            base.Awake();
            _saveBtn.onClick.AddListener(OnSaveClicked);

            for (int i = 0; i < _slots.Length; i++)
            {
                int avatar = i;
                _slots[i].Init(AvatarConfig.Get(avatar), () => Select(avatar));
            }
        }

        public override void Show()
        {
            base.Show();
            Select(AppContext.Session.Avatar);
        }

        private void Select(int avatar)
        {
            _selected = avatar;
            for (int i = 0; i < _slots.Length; i++) _slots[i].SetSelected(i == avatar);
        }

        private void OnSaveClicked()
        {
            if (_saving) return;
            _saving = true;

            AppContext.Proto.RequestSetAvatar(_selected, ret =>
            {
                _saving = false;
                if (ret.CmdCode != CmdCode.Succeed)
                {
                    AppContext.Ui.ShowTip("头像保存失败");
                    return;
                }

                AppContext.Ui.ShowTip("头像已更新");
                // 开始面板还开着（头像面板是叠在它上面），让它按新的 Session.Avatar 重画
                AppContext.Ui.GetPanel<StartPanel>()?.RefreshAvatar();
                AppContext.Ui.ClosePanel(this);
            });
        }
    }
}
