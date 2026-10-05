using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneHandle = YooAsset.SceneHandle;

public class SceneMgr : MonoBehaviour
{
    private bool _isLoading;
    private static SceneMgr _instance;
    public static SceneMgr Instance => _instance;

    public void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string scenename)
    {
        _isLoading = true;
        UIManager.Instance.OpenPanel<LoadPanel>(_ =>
        {
            Debug.Log("Starting LoadScene: " + scenename);
            StartCoroutine(TrackLoadingProgress(scenename));
        });
    }

    private IEnumerator TrackLoadingProgress(string scenename)
    {
        string path = "Assets/Scenes/" + scenename;
        SceneHandle asyncOperation =
            Global.Instance._YooPackage.LoadSceneAsync(path, LoadSceneMode.Single, LocalPhysicsMode.None, false);
        float displayProgress = 0f;
        float realProgress;
        float minLoadTime = 1.5f;
        float elapsedTime = 0f;

        while (asyncOperation.Progress < 0.9f)
        {
            elapsedTime += Time.deltaTime;
            realProgress = asyncOperation.Progress / 0.9f;
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

    private void OnSceneLoaded(string scenename)
    {
        switch (scenename)
        {
            case "LobbyScene":
                ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/Prefab/LobbyController");
                break;
            case "GameScene":
                // 首次访问 Instance 会自动创建并注册位置同步网络事件；同时清理上一局残留
                RemotePlayerManager.Instance.ResetForNewScene();
                ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/Prefab/Character");
                ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/Prefab/NPC");
                ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/Prefab/GameController");
                AppContext.Events.EventTrigger(GameEvent.GameStart);
                break;
        }

        _isLoading = false;
        UIManager.Instance.ClosePanel<LoadPanel>();
    }

    public void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}