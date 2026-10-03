using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRoomItem : MonoBehaviour
{
    public TMP_Text playerName;
    public TMP_Text isReadyText;
    public Image playerHeadImage;

    public void Init(PlayerConfig playerConfig)
    {
        playerName.text = playerConfig.name;
        isReadyText.text = playerConfig.isReady ? "已准备" : "未准备";
        playerHeadImage.sprite = Resources.Load<Sprite>("Icon/icon_question");
    }

    public void UpdateRoomItem(PlayerConfig playerConfig)
    {
        playerName.text = playerConfig.name;
        isReadyText.text = playerConfig.isReady ? "已准备" : "未准备";
        // 优先检查本地是否已有该Sprite，避免首次Resources.Load失败
        Sprite headSprite = playerHeadImage.sprite;
        if (headSprite == null || headSprite.name != playerConfig.headImageName)
        {
            try { headSprite = Resources.Load<Sprite>($"Icon/{playerConfig.headImageName}"); }
            catch
            {
                //  fallback：使用默认头像
                headSprite = Resources.Load<Sprite>("Icon/icon_question");
            }
            playerHeadImage.sprite = headSprite;
        }
        DOVirtual.DelayedCall(0.2f, () => { UpdateHeadImage(playerConfig);});
    }

    private void UpdateHeadImage(PlayerConfig playerConfig)
    {
        Sprite headSprite = playerHeadImage.sprite;
        if (headSprite == null || headSprite.name != playerConfig.headImageName)
        {
            try
            {
                headSprite = Resources.Load<Sprite>($"Icon/{playerConfig.headImageName}");
            }
            catch
            {
                //  fallback：使用默认头像
                headSprite = Resources.Load<Sprite>("Icon/icon_question");
            }
            playerHeadImage.sprite = headSprite;
        }
    }
}
