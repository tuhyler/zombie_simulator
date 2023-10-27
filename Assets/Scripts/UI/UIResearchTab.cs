using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResearchTab : MonoBehaviour
{
    [SerializeField]
    private Image background;

    [SerializeField]
    private TMP_Text tabText; 

    [SerializeField]
    private Sprite selectedSprite, unselectedSprite;

    [HideInInspector]
    public int tabLoc;

    private UIResearchTreePanel uiResearchTree;

    public void SelectTab()
    {
        background.sprite = selectedSprite;
        tabText.color = Color.black;
        uiResearchTree.selectedTab = tabLoc;
        uiResearchTree.SetTab();
        uiResearchTree.titleText.text = tabText.text;
        uiResearchTree.world.cityBuilderManager.PlaySelectAudio();
    }

    public void SetResearchTree(UIResearchTreePanel uiResearchTree)
    {
        this.uiResearchTree = uiResearchTree;
    }

    public void Unselect()
    {
        background.sprite = unselectedSprite;
        tabText.color = new Color(.6f, .6f, .6f);
    }
}
