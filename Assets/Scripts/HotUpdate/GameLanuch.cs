using UnityEngine;

public class GameLanuch : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("游戏开始,主流程开始");
        Global.Instance._YooPackage.LoadSceneAsync("EnterScene");
        Destroy(gameObject);
    }
}
