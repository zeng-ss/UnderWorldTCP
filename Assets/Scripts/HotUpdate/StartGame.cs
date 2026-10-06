using UnityEngine;
using UnityEngine.AddressableAssets;

public class StartGame : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("Change Scene to Start Game");
        Addressables.LoadSceneAsync("HotUpdateScene");
    }
}
