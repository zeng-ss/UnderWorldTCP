using UnityEngine;

public class GameLanuch : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("游戏开始,主流程开始");
        new GameObject("GameManager").AddComponent<GameManager>();
        Global.Instance._YooPackage.LoadSceneAsync("EnterScene").Completed += sceneHandle =>
        {
            AppContext.Ui.OpenPanel<LoginPanel>();
            sceneHandle.Release();
            Destroy(gameObject);
        };
    }
}