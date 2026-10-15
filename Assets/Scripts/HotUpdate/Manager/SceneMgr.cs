using System.Collections;
using HotUpdate.Controller;
using HotUpdate.Core;
using HotUpdate.Player;
using HotUpdate.UI.UIItem;
using HotUpdate.UI.UIPanel;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace HotUpdate.Manager
{
    public class SceneMgr
    {
        private bool _isLoading;
        private LoadPanel _loadPanel;

        public void LoadScene(string sceneName)
        {
            _isLoading = true;
            AppContext.Ui.OpenPanel<LoadPanel>(panel =>
            {
                Debug.Log("Starting LoadScene: " + sceneName);
                _loadPanel = panel;
                MonoManager.Instance.StartCoroutine(TrackLoadingProgress(sceneName));
            });
        }

        private IEnumerator TrackLoadingProgress(string sceneName)
        {
            // 地址即场景资源名（如 EnterScene / LobbyScene / GameScene），在 Addressables 分组里配置
            // activateOnLoad=false：加载到 90% 暂停，由展示进度走完后再手动激活（与旧实现行为一致）
            AsyncOperationHandle<SceneInstance> handle =
                Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single, false);

            float displayProgress = 0f;
            float minLoadTime = 1.5f;
            float elapsedTime = 0f;

            while (!handle.IsDone && handle.PercentComplete < 0.9f)
            {
                elapsedTime += Time.deltaTime;
                var realProgress = handle.PercentComplete / 0.9f;
                float timeProgress = Mathf.Clamp01(elapsedTime / minLoadTime);
                displayProgress = Mathf.Min(realProgress, timeProgress);
                _loadPanel.UpdateProgress(displayProgress);
                yield return null;
            }

            float startProgress = displayProgress;
            float smoothElapsed = 0f;
            float smoothDuration = 1f;
            if (sceneName == "GameScene") Preload();

            while (smoothElapsed < smoothDuration)
            {
                smoothElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(smoothElapsed / smoothDuration);
                float easeT = t * (2 - t);
                displayProgress = Mathf.Lerp(startProgress, 1f, easeT);
                _loadPanel.UpdateProgress(displayProgress);
                yield return null;
            }

            _loadPanel.UpdateProgress(1f);
            yield return new WaitForSeconds(0.3f);
            handle.Result.ActivateAsync();
            yield return handle;
            yield return new WaitUntil(() => handle.IsDone);

            OnSceneLoaded(SceneManager.GetActiveScene().name);
        }

        private void Preload()
        {
            AppContext.UILoader.LoadAsync(PanelPathResolver.Resolve<PlayerProPanel>(), null);
            AppContext.Res.LoadAssetAsync<GameObject>("Character", null);
            AppContext.Res.LoadAssetAsync<GameObject>("NPC", null);
            AppContext.Res.LoadAssetAsync<GameObject>("GameController", null);
            AppContext.Res.LoadAssetAsync<GameObject>("HUDSlotItem", null);
        }

        private void OnSceneLoaded(string sceneName)
        {
            switch (sceneName)
            {
                case "EnterScene":
                    // 主界面：房间面板「返回主界面」跳回来时重新打开开始面板
                    AppContext.Ui.OpenPanel<StartPanel>();
                    break;
                case "LobbyScene":
                    // 大厅 = 房间面板（由 UIManager 统一打开，无需场景里摆预制体）
                    AppContext.Ui.ClosePanel<PlayerProPanel>(); // 离开战斗场景时收掉 HUD
                    AppContext.Ui.OpenPanel<RoomPanel>();
                    break;
                case "GameScene":
                    // 首次访问会自动创建并注册位置同步网络事件；同时清理上一局残留
                    AppContext.RemotePlayer.ResetForNewScene();
                    AppContext.Res.LoadAndInstantiateAsync("Character");
                    AppContext.Res.LoadAndInstantiateAsync("NPC");
                    AppContext.Res.LoadAndInstantiateAsync("GameController");
                    // 联机时从服务端恢复任务进度（离线调试内部会跳过）
                    AppContext.Task.LoadFromServer();
                    // 战斗 HUD：头像 / 血条 + 底部面板快捷栏
                    AppContext.Ui.OpenPanel<PlayerProPanel>();
                    break;
            }

            _isLoading = false;
            AppContext.Ui.ClosePanel<LoadPanel>();
        }
    }
}