using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISaveGame : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    private Transform saveHolder;

    [SerializeField]
    private TMP_InputField saveField;

    [SerializeField]
    private UIWarning uiWarning;

    [SerializeField]
    private Image screenshot;

    [SerializeField]
    private TMP_Text titleText, playTime, version, saveLoadText; 
    
    [SerializeField]
    private GameObject uiSaveItemGO, screenshotParent, deleteButton;
    private UISaveItem selectedSaveItem;

    private List<string> currentSaves = new();
    private bool newItem, load, populated;
    [HideInInspector]
    public bool activeStatus;

	private void Start()
	{
        if (!populated)
            PopulateSaveItems();
        gameObject.SetActive(false);
	}

	public void PopulateSaveItems()
    {
        foreach (string fullSavedName in Directory.GetFiles(Application.persistentDataPath, "*.save"))
		{
            string[] saveBreaks = fullSavedName.Split("\\");
			string savedName = "/" + saveBreaks[saveBreaks.Length-1];

            currentSaves.Add(savedName.Substring(1,savedName.Length-6));
            GameData gameData /*= GameManager.Instance.GetLoadInfo(savedName)*/;

            if (GameLoader.Instance == null)
                gameData = GameManager.Instance.GetLoadInfo(savedName);
            else
                gameData = GameLoader.Instance.gamePersist.LoadData(savedName, false);

            if (gameData == null)
            {
                //File.Delete(Application.persistentDataPath + savedName);
                continue;
            }

			GameObject item = Instantiate(uiSaveItemGO);
			item.transform.SetParent(saveHolder, false);
			UISaveItem uiSaveItem = item.GetComponent<UISaveItem>();
			uiSaveItem.SetSaveGameMenu(this);

			string nameTrunc = gameData.saveName.Substring(1, gameData.saveName.Length - 6);
			uiSaveItem.saveName = nameTrunc;
            uiSaveItem.saveItemText.text = nameTrunc;
            uiSaveItem.playTime = gameData.savePlayTime;
            uiSaveItem.version = gameData.saveVersion;
            Texture2D texture = new Texture2D(1200, 900, TextureFormat.ARGB32, false);
            Rect rect = new Rect(0, 0, texture.width, texture.height);    
            texture.LoadImage(Convert.FromBase64String(gameData.saveScreenshot));
            uiSaveItem.screenshot = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
		}

        populated = true;
        Resources.UnloadUnusedAssets();
	}

	public void ToggleVisibility(bool v, bool load = false)
    {
        if (activeStatus == v)
            return;

        if (v)
        {
            this.load = load;
            playTime.gameObject.SetActive(false);
			version.gameObject.SetActive(false);
			screenshotParent.SetActive(false);

            if (load)
            {
                saveField.gameObject.SetActive(false);
				deleteButton.SetActive(true);
                saveLoadText.text = "Load Game";
                titleText.text = "Load Game";
			}
			else
            {
				saveField.gameObject.SetActive(true);
                deleteButton.SetActive(false);
				saveLoadText.text = "Save Game";
				titleText.text = "Save Game";
			}

			saveField.text = "";
            gameObject.SetActive(true);
            activeStatus = true;
        }
        else
        {
			gameObject.SetActive(false);
			activeStatus = false;
            if (selectedSaveItem != null)
            {
                selectedSaveItem.UnselectItem();
                selectedSaveItem = null;
            }
        }
    }

    public void CloseSaveGameButton()
    {
        ToggleVisibility(false);
    }

    public void SelectItem(UISaveItem uiSaveItem)
    {
        if (selectedSaveItem != null)
            selectedSaveItem.UnselectItem();

        playTime.gameObject.SetActive(true);
        playTime.text = ConvertPlayTime(uiSaveItem.playTime);
        version.gameObject.SetActive(true);
        version.text = uiSaveItem.version;
        screenshotParent.SetActive(true);
        screenshot.sprite = uiSaveItem.screenshot;
        saveField.text = uiSaveItem.saveName;
        selectedSaveItem = uiSaveItem;
    }

    private string ConvertPlayTime(float time)
    {
        return Math.Round(time * 0.0002777f, 1).ToString() + " hrs";
    }

    public void ShowWarning()
    {
        if (load)
        {
            if (selectedSaveItem == null)
                return;

            uiWarning.SetWarningMessages("Deleting, you sure?","Absolutely I am","No, not really");
        }
        else
        {
            uiWarning.SetWarningMessages("Overwrite?","Yup!","Nah");
        }
        uiWarning.ToggleVisibilty(true);
	}

    public void ConfirmWarning()
    {
        if (load)
            DeleteGame();
        else
            ConfirmOverWrite();

        uiWarning.CloseWarning();
    }

    public void DeleteGame()
    {
		string path = Application.persistentDataPath + "/" + selectedSaveItem.saveName + ".save";
		Destroy(selectedSaveItem.gameObject);
		playTime.gameObject.SetActive(false);
		version.gameObject.SetActive(false);
		screenshotParent.SetActive(false);

		File.Delete(path);
	}

	public void SaveGameCheck()
    {
        if (load)
        {
            if (selectedSaveItem == null)
                return;
            
            if (GameLoader.Instance == null)
            {
				string loadName = selectedSaveItem.saveName;
				ToggleVisibility(false);
                GameManager.Instance.LoadGame(loadName);
                return;
			}
            else
            {
                string loadName = selectedSaveItem.saveName;
			    ToggleVisibility(false);
                world.uiMainMenu.ToggleVisibility(false);
                GameLoader.Instance.LoadDataGame(loadName);
                return;
            }
        }
        
        if (saveField.text.Length == 0)
            return;

        if (world == null)
            Debug.Log("");
        else
		    world.cityBuilderManager.PlaySelectAudio();
		string saveName = saveField.text;
        for (int i = 0; i < currentSaves.Count; i++)
        {
            if (saveName == currentSaves[i])
            {
                newItem = false;
                ShowWarning();
                return;
            }
        }

        newItem = true;
        world.StartSaveProcess(saveName);
    }

    public void UpdateSaveItems(string saveName, float savePlayTime, string saveVersion, Texture2D saveScreenshot)
    {
        if (newItem)
        {
			GameObject item = Instantiate(uiSaveItemGO);
			item.transform.SetParent(saveHolder, false);
            item.transform.SetAsFirstSibling();
			UISaveItem uiSaveItem = item.GetComponent<UISaveItem>();
			uiSaveItem.SetSaveGameMenu(this);

			uiSaveItem.saveName = saveName;
			uiSaveItem.saveItemText.text = saveName;
			uiSaveItem.playTime = savePlayTime;
			uiSaveItem.version = saveVersion;
			Rect rect = new Rect(0, 0, saveScreenshot.width, saveScreenshot.height);
			uiSaveItem.screenshot = Sprite.Create(saveScreenshot, rect, new Vector2(0.5f, 0.5f));
		}
        else
        {
            selectedSaveItem.playTime = savePlayTime;
            selectedSaveItem.version = saveVersion;
			Rect rect = new Rect(0, 0, saveScreenshot.width, saveScreenshot.height);
			selectedSaveItem.screenshot = Sprite.Create(saveScreenshot, rect, new Vector2(0.5f, 0.5f));
		}
	}

    public void ConfirmOverWrite()
    {
        world.StartSaveProcess(selectedSaveItem.saveName);
	}
}
