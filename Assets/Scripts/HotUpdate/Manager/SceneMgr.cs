using System;
using System.Collections;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Manager
{
    public class SceneMgr
    {
        public void LoadScene(string sceneName, float holdDuration, Action onCovered = null)
        {
            // 用转场遮罩代替进度条：Addressables 的 PercentComplete 在加载阶段就会冲到 90% 左右
            // 然后停住等激活，做成进度条反而会「一来就 90%、卡一会儿再进游戏」。
            // 遮罩没有进度概念，加载多久都只是多转几圈。
            AppContext.Ui.Transition(holdDuration, () =>
            {
                Debug.Log("Starting LoadScene: " + sceneName);
                onCovered?.Invoke();
                MonoManager.Instance.StartCoroutine(LoadRoutine(sceneName));
            });
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(sceneName);
            if (sceneName == "GameScene") Preload();
            yield return handle;

            // 面板真正打开之后才揭幕，避免遮罩淡出时目标界面还没加载出来
            OnSceneLoaded(sceneName, AppContext.Ui.TransitionDone);
        }

        private void Preload()
        {
            // 只预热 HUD 预制体（揭幕前要等它加载完）。
            // Character / NPC / GameController 交给 OnSceneLoaded 里的 LoadAndInstantiateAsync，
            // 跟场景加载并行只会抢主线程和带宽，把过渡拖得更卡。
            AppContext.UILoader.LoadAsync(PanelPathResolver.Resolve<PlayerProPanel>(), null);
        }

        private void OnSceneLoaded(string sceneName, Action onReady)
        {
            switch (sceneName)
            {
                case "EnterScene":
                case "LobbyScene":
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    onReady.Invoke();
                    break;
                case "GameScene":
                    // 首次访问会自动创建并注册位置同步网络事件；同时清理上一局残留
                    AppContext.RemotePlayer.ResetForNewScene();
                    AppContext.Res.LoadAndInstantiateAsync("Character", null, _ => onReady.Invoke());
                    AppContext.Res.LoadAndInstantiateAsync("NPC");
                    AppContext.Res.LoadAndInstantiateAsync("GameController");
                    // 联机时从服务端恢复任务进度
                    AppContext.Task.LoadFromServer();
                    break;
                default:
                    onReady();
                    break;
            }
        }
    }
}