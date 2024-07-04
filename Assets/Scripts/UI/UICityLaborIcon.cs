using TMPro;
using UnityEngine;

public class UICityLaborIcon : MonoBehaviour
{
    [SerializeField]
    private TMP_Text numberText;

    public int size;

    public bool infinite;

    [HideInInspector]
    public bool isActive;

    private void Start()
    {
        numberText.outlineColor = Color.black;
        numberText.outlineWidth = 0.2f;
    }

    public void ToggleVisibility(bool v)
    {
        gameObject.SetActive(v);
        isActive = v;
    }

    public void SetNumber(int number)
    {
        numberText.text = number.ToString();
    }

    public void HideNumber()
    {
        numberText.gameObject.SetActive(false);
    }
}
