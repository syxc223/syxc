using TMPro;

using UnityEngine;
public class EquipmentStatCompareRow : MonoBehaviour
{
    [SerializeField] private TMP_Text currentValueText;
    [SerializeField] private TMP_Text modifyValueText;

    [Header("Colors")]
    [SerializeField] private Color increaseColor;
    [SerializeField] private Color decreaseColor;

    public void SetRaw(int currentValue, int previewValue, bool isInPreviewMode = true)
{
    currentValueText.text = currentValue.ToString();

    if (!isInPreviewMode || previewValue == currentValue)
    {
        modifyValueText.text = "";
        return;
    }

    modifyValueText.text = previewValue.ToString();

    // Set color
    if (previewValue > currentValue)
    {
        modifyValueText.color = increaseColor;
    }
    else
    {
        modifyValueText.color = decreaseColor;
    }
}
}