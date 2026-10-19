using System.Collections;
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
            GameObject mgr = new GameObject(nameof(GameManager), typeof(GameManager));
            //StartCoroutine(EnterGame());
            Debug.Log("游戏开始,主流程开始");
            AppContext.Ui.Transition(0.5f,
                () =>
                {
                    Addressables.LoadSceneAsync("EnterScene").Completed += _ =>
                    {
                        AppContext.Ui.OpenPanel<LoginPanel>();
                        AppContext.Ui.TransitionDone();
                    };
                });
        }

        private IEnumerator EnterGame()
        {
            var handle = Addressables.InstantiateAsync("GameManager");
            yield return handle;
        }
    }
}