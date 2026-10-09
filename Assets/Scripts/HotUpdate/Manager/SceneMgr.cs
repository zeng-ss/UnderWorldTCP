using System.Collections;
using HotUpdate.Core;
using HotUpdate.Event;
using HotUpdate.UI.UIPanel;
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

        public void LoadScene(string sceneName)
        {
            _isLoading = true;
            AppContext.Ui.OpenPanel<LoadPanel>(_ =>
            {
                Debug.Log("Starting LoadScene: " + sceneName);
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
                AppContext.Events.EventTrigger(GameEvent.LoadProgress, new LoadProgressArgs(displayProgress));
                yield return null;
            }

            float startProgress = displayProgress;
            float smoothElapsed = 0f;
            float smoothDuration = 1f;

            while (smoothElapsed < smoothDuration)
            {
                smoothElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(smoothElapsed / smoothDuration);
                float easeT = t * (2 - t);
                displayProgress = Mathf.Lerp(startProgress, 1f, easeT);
                AppContext.Events.EventTrigger(GameEvent.LoadProgress, new LoadProgressArgs(displayProgress));
                yield return null;
            }

            AppContext.Events.EventTrigger(GameEvent.LoadProgress, new LoadProgressArgs(1f));
            yield return new WaitForSeconds(0.3f);
            handle.Result.ActivateAsync();
            yield return handle;
            yield return new WaitUntil(() => handle.IsDone);

            OnSceneLoaded(SceneManager.GetActiveScene().name);
        }

        private void OnSceneLoaded(string sceneName)
        {
            switch (sceneName)
            {
                case "LobbyScene":
                    AppContext.Res.LoadAndInstantiateAsync("LobbyController");
                    break;
                case "GameScene":
                    // 首次访问会自动创建并注册位置同步网络事件；同时清理上一局残留
                    AppContext.RemotePlayer.ResetForNewScene();
                    AppContext.Res.LoadAndInstantiateAsync("Character");
                    AppContext.Res.LoadAndInstantiateAsync("NPC");
                    AppContext.Res.LoadAndInstantiateAsync("GameController");
                    // 联机时从服务端恢复任务进度（离线调试内部会跳过）
                    AppContext.Task.LoadFromServer();
                    AppContext.Events.EventTrigger(GameEvent.GameStart);
                    break;
            }

            _isLoading = false;
            AppContext.Ui.ClosePanel<LoadPanel>();
        }
    }
}
