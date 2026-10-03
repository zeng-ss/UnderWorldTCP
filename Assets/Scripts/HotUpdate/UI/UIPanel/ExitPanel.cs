using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ExitPanel : BasePanel
{
    public Button yesBtn;
    public Button noBtn;
    private Vector3 startPos = new(0, 840, 0);

    private void OnEnable()
    {
        transform.localPosition = startPos;
        transform.DOLocalMove(Vector3.zero, 0.5f);
        yesBtn.onClick.AddListener(() =>
        {
            //GameManager.Instance.SaveArchive();
            Application.Quit();
        });
        noBtn.onClick.AddListener(() =>
        {
            transform.DOLocalMove(startPos, 0.5f)
                .OnComplete(() => { UIManager.Instance.ClosePanel<ExitPanel>(); });
        });
    }
    
    private void OnDisable()
    {
        yesBtn.onClick.RemoveAllListeners();
        noBtn.onClick.RemoveAllListeners();
    }
}
