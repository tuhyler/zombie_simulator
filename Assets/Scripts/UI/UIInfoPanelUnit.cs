using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInfoPanelUnit : MonoBehaviour //This script is for populating the panel and switching it off/on
{
    public TMP_Text unitName, level, health, speed, strength;
    public Image strengthImage;
    public Sprite strengthSprite, inventorySprite;
    public GameObject renamerButton, goToNextButton;

    private UITooltipTrigger tooltipTrigger;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;

        List<UITooltipTrigger> allTooltips = new();
        foreach (UITooltipTrigger trigger in GetComponentsInChildren<UITooltipTrigger>())
            allTooltips.Add(trigger);

        tooltipTrigger = allTooltips[allTooltips.Count - 1]; 

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
            tooltipTrigger.SetMessage("Cargo Space");
        }
        else
        {
            this.strength.text = strength.ToString();
            strengthImage.sprite = strengthSprite;
			tooltipTrigger.SetMessage("Strength");
		}
    }

    public void SetHealth(int currentHealth, int maxHealth)
    {
        health.text = currentHealth.ToString() + '/' + maxHealth.ToString();
    }

    public void ToggleVisibility(bool v, bool isTrader = false, bool isLaborer = false)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

            if (isTrader)
            {
                renamerButton.SetActive(true);
				goToNextButton.SetActive(true);
			}
            else if (isLaborer)
            {
				renamerButton.SetActive(false);
				goToNextButton.SetActive(true);
            }
            else
            {
				renamerButton.SetActive(false);
				goToNextButton.SetActive(false);
			}

			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.4f).setEaseOutBack();
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
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
