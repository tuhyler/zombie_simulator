using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInfoPanelUnit : MonoBehaviour //This script is for populating the panel and switching it off/on
{
    public TMP_Text unitName, level, health, speed, strength;
    public Image strengthImage;
    public Sprite strengthSprite, inventorySprite;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;

        gameObject.SetActive(false);
    }

    public void SetData(string displayName, int level, string name, int currentHealth, int healthMax, float speed, int strength, int cargo)
    {
        unitName.text = displayName;
        this.level.text = "Level " + level + " " + name;
        health.text = currentHealth.ToString() + "/" + healthMax.ToString();
        this.speed.text = Mathf.RoundToInt(speed * 2).ToString();
        if (cargo > 0)
        {
            this.strength.text = cargo.ToString();
            strengthImage.sprite = inventorySprite;
        }
        else
        {
            this.strength.text = strength.ToString();
            strengthImage.sprite = strengthSprite;
        }
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.4f).setEaseOutBack();
            LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -600f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }
    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }
}
