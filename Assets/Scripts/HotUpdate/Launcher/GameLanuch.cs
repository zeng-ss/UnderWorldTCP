using HotUpdate.Core;
using HotUpdate.Manager;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HotUpdate.Launcher
{
    public class GameLanuch : MonoBehaviour
    {
        private void Awake()
        {
            new GameObject(nameof(GameManager), typeof(GameManager));
            Debug.Log("游戏开始,主流程开始");
            AppContext.Ui.Transition(0.5f, TransitionPanel.ScrollBgType.Normal,
                () =>
                {
                    Addressables.LoadSceneAsync("EnterScene").Completed += _ =>
                    {
                        AppContext.Ui.OpenPanel<LoginPanel>();
                        AppContext.Ui.TransitionDone();
                    };
                });
        }
    }
}