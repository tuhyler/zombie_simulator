using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResearchTab : MonoBehaviour
{
    [SerializeField]
    public Era era;
    
    [SerializeField]
    private Image background;

    [SerializeField]
    private TMP_Text tabText; 

    [SerializeField]
    private Sprite selectedSprite, unselectedSprite;

    [HideInInspector]
    public int tabLoc;

    private UIResearchTreePanel uiResearchTree;

    public void SelectTabClick()
    {
		if (uiResearchTree.selectedTab == tabLoc)
			return;

		uiResearchTree.world.cityBuilderManager.PlaySelectAudio();
        uiResearchTree.horizontalScroll.value = uiResearchTree.tabThresholds[tabLoc];
        SelectTab();
	}

    public void SelectTab()
    {
        background.sprite = selectedSprite;
        tabText.color = Color.black;

        //uiResearchTree.tabList[uiResearchTree.selectedTab].Unselect();
        uiResearchTree.selectedTab = tabLoc;
        uiResearchTree.SetTab();
        uiResearchTree.titleText.text = tabText.text;
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
