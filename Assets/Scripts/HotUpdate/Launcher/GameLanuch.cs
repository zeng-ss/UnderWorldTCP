using HotUpdate.Core;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HotUpdate.Launcher
{
    public class GameLanuch : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("游戏开始,主流程开始");
            Addressables.LoadSceneAsync("EnterScene").Completed += _ => { AppContext.Ui.OpenPanel<LoginPanel>(); };
        }
    }
}
