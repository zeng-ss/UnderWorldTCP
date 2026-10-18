using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Manager;
using HotUpdate.UI.UIItem;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("AvatarPanel")]
    public class AvatarPanel : BasePanel
    {
        [FormerlySerializedAs("_saveBtn")] [SerializeField]
        private Button saveBtn;

        [FormerlySerializedAs("_slots")] [SerializeField]
        private AvatarSlot[] slots;

        private int _selected;
        private bool _saving;

        protected override void Awake()
        {
            base.Awake();
            saveBtn.onClick.AddListener(OnSaveClicked);

            for (int i = 0; i < slots.Length; i++)
            {
                int avatar = i;
                slots[i].Init(AvatarConfig.Get(avatar), () => Select(avatar));
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
            for (int i = 0; i < slots.Length; i++) slots[i].SetSelected(i == avatar);
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
                // 开始面板还开着，让它按新的 Session.Avatar 重画
                AppContext.Ui.GetPanel<StartPanel>()?.RefreshAvatar();
                AppContext.Ui.ClosePanel(this);
            });
        }
    }
}