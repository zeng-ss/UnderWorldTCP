using UnityEngine;
using YooAsset;

public class StartGame : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("Change Scene to Start Game");
        Global.Instance._YooPackage.LoadSceneAsync("HotUpdateScene");
    }
}
