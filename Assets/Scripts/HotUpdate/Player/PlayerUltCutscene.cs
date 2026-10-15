using DG.Tweening;
using HotUpdate.Manager;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Player
{
    /// <summary>
    /// 大招过场：角色身上的过场相机（PlayerCtrl.ultCutsceneCamera）把角色渲染进一张
    /// RenderTexture，交给 <see cref="UltPanel"/> 全屏显示。
    /// </summary>
    public class PlayerUltCutscene
    {
        private const int TextureWidth = 1920;

        // 面板黑边滑出动画播完才收相机，否则角色会在黑边退场时从画面里消失
        private const float PanelExitDelay = 0.4f;

        private Camera _camera;
        private RenderTexture _texture;
        private bool _playing;

        public void Init(Camera ultCutsceneCamera)
        {
            _camera = ultCutsceneCamera;
            _camera.enabled = false;
            // 预热面板资源，第一次放大招才不会被异步加载拖半拍
            AppContext.UILoader.LoadAsync(PanelPathResolver.Resolve<UltPanel>(), null);
        }

        public void Play()
        {
            if (_playing || _camera is null) return;
            _playing = true;

            // RT 高度跟屏幕比例走，全屏显示时才不会被拉伸
            int height = Mathf.Clamp(Mathf.RoundToInt(TextureWidth * (float)Screen.height / Screen.width), 480, 1600);
            _texture = new RenderTexture(TextureWidth, height, 24);

            _camera.gameObject.SetActive(true);
            _camera.aspect = (float)TextureWidth / height;
            _camera.targetTexture = _texture;
            _camera.enabled = true;

            AppContext.Ui.OpenPanel<UltPanel>(panel => panel?.Play(_texture));
        }

        public void Stop()
        {
            if (!_playing) return;
            _playing = false;

            AppContext.Ui.ClosePanel<UltPanel>();
            DOVirtual.DelayedCall(PanelExitDelay, Release);
        }

        private void Release()
        {
            _camera.enabled = false;
            _camera.targetTexture = null;
            Object.Destroy(_texture);
            _texture = null;
            _camera.gameObject.SetActive(false);
        }
    }
}
