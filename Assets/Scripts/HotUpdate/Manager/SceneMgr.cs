using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneHandle = YooAsset.SceneHandle;

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
        string path = "Assets/Scenes/" + sceneName;
        SceneHandle asyncOperation =
            Global.Instance._YooPackage.LoadSceneAsync(path, LoadSceneMode.Single, LocalPhysicsMode.None, false);
        float displayProgress = 0f;
        float minLoadTime = 1.5f;
        float elapsedTime = 0f;

        while (asyncOperation.Progress < 0.9f)
        {
            elapsedTime += Time.deltaTime;
            var realProgress = asyncOperation.Progress / 0.9f;
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
        asyncOperation.AllowSceneActivation();
        yield return asyncOperation;
        yield return new WaitUntil(() => asyncOperation.IsDone);

        OnSceneLoaded(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(string sceneName)
    {
        switch (sceneName)
        {
            case "LobbyScene":
                AppContext.Res.LoadAndInstantiateAsync("Assets/Res/Prefab/LobbyController");
                break;
            case "GameScene":
                // 首次访问 Instance 会自动创建并注册位置同步网络事件；同时清理上一局残留
                AppContext.RemotePlayer.ResetForNewScene();
                AppContext.Res.LoadAndInstantiateAsync("Assets/Res/Prefab/Character");
                AppContext.Res.LoadAndInstantiateAsync("Assets/Res/Prefab/NPC");
                AppContext.Res.LoadAndInstantiateAsync("Assets/Res/Prefab/GameController");
                AppContext.Events.EventTrigger(GameEvent.GameStart);
                break;
        }

        _isLoading = false;
        AppContext.Ui.ClosePanel<LoadPanel>();
    }
}