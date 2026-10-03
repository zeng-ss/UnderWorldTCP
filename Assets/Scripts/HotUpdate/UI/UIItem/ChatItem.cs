using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatItem : MonoBehaviour
{
    public Image headImage;
    public TMP_Text nameText;
    public TMP_Text mesText;
    
    public void UpdateDate(MessageData data)
    {
        nameText.text = $"{data.name}——{data.sendTime.ToString(CultureInfo.InvariantCulture)}";
        mesText.text = data.message;
    }
    
}
