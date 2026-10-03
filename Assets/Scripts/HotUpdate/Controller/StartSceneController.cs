using UnityEngine;

public class StartSceneController : MonoBehaviour
{
    private void Awake()
    {
        UIManager.Instance.OpenPanel<LoginPanel>();
    }
}
