using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISaveGame : MonoBehaviour
{
    [SerializeField]
    private TitleScreen titleScreen;
    
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    public Transform saveHolder;

    [SerializeField]
    private TMP_InputField saveField;

    [SerializeField]
    private UIWarning uiWarning;

    [SerializeField]
    public Image screenshot;

    [SerializeField]
    private TMP_Text titleText, playTime, version, saveLoadText, seed;

    [SerializeField]
    private GameObject playTimeTitle, versionTitle, seedTitle;

    [SerializeField]
    private GameObject uiSaveItemGO, screenshotParent, deleteButton;
    private UISaveItem selectedSaveItem;
    //List<UISaveItem> saveItemList = new();

    [HideInInspector]
    public List<string> currentSaves = new();
    private bool newItem, load/*, populated*/;
    [HideInInspector]
    public bool activeStatus;

	private void Start()
	{
        //if (!populated)
        //    PopulateSaveItems();
        gameObject.SetActive(false);

		saveField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return AlphaNumericSpaceCheck(addedChar); };
	}

	private void Update()
	{
        if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
        {
            if (uiWarning.activeStatus)
                ConfirmWarning();
            else
			    SaveGameCheck();
        }
	}

	private char AlphaNumericSpaceCheck(char c)
	{
		if (!Char.IsWhiteSpace(c) && !Char.IsLetter(c) && !Char.IsDigit(c))
			c = '\0';

		return c;
	}

	public void PopulateSaveItems()
    {
        List<UISaveItem> saveItems = new();
        
        foreach (string fullSavedName in Directory.GetFiles(Application.persistentDataPath, "*.save"))
		{
            string[] saveBreaks = fullSavedName.Split("\\");
			string fileName = "/" + saveBreaks[saveBreaks.Length-1];
            string saveName = fileName.Substring(1, fileName.Length - 6);

			currentSaves.Add(saveName);
            GameData gameData /*= GameManager.Instance.GetLoadInfo(savedName)*/;

            if (GameLoader.Instance == null)
                gameData = GameManager.Instance.GetLoadInfo(fileName);
            else
                gameData = GameLoader.Instance.gamePersist.LoadData(fileName, false);

            if (gameData == null)
            {
                //File.Delete(Application.persistentDataPath + savedName);
                continue;
            }

            GameObject item = Instantiate(uiSaveItemGO);
			UISaveItem uiSaveItem = item.GetComponent<UISaveItem>();
			uiSaveItem.SetSaveGameMenu(this);
            //uiSaveItem.loaded = true;
            uiSaveItem.fileName = fileName;
			uiSaveItem.saveName = saveName;
            uiSaveItem.saveItemText.text = saveName;
            uiSaveItem.playTime = gameData.savePlayTime;
            uiSaveItem.version = gameData.saveVersion;
            uiSaveItem.seed = gameData.seed.ToString();
            //Texture2D texture = new Texture2D(1200, 900, TextureFormat.ARGB32, false);
            //Rect rect = new Rect(0, 0, texture.width, texture.height);
            //texture.LoadImage(Convert.FromBase64String(gameData.saveScreenshot));
            //uiSaveItem.screenshot = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
            //uiSaveItem.screenshot = Resources.Load<Sprite>("Assets/Resources/SaveScreens" + saveName + ".png");
            uiSaveItem.dateTime = Convert.ToDateTime(gameData.saveDate);
            saveItems.Add(uiSaveItem);
		}

        List<UISaveItem> newSaveItems = saveItems.OrderByDescending(s => s.dateTime).ToList();

        for (int i = 0; i < newSaveItems.Count; i++)
        {
            newSaveItems[i].transform.SetParent(saveHolder, false);
            //saveItemList.Add(newSaveItems[i]);
        }

        //populated = true;
        Resources.UnloadUnusedAssets();
	}

	public void ToggleVisibility(bool v, bool load = false)
    {
        if (activeStatus == v)
            return;

        if (v)
        {
            //if (world != null)
            //    PopulateSaveNames();
            this.load = load;
            playTime.gameObject.SetActive(false);
			version.gameObject.SetActive(false);
            seed.gameObject.SetActive(false);
			screenshotParent.SetActive(false);
            playTimeTitle.SetActive(false);
            versionTitle.SetActive(false);
            seedTitle.SetActive(false);

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

            //if (world != null)
            //    ClearSaveItems();

            //saveItemList.Clear();
        }
    }

 //   public void ClearSaveItems()
 //   {
	//	for (int i = 0; i < saveItemList.Count; i++)
 //       {
	//		Destroy(saveItemList[i].screenshot);
	//		Destroy(saveItemList[i].gameObject);
 //       }

	//	Resources.UnloadUnusedAssets();
	//}

    public void CloseSaveGameButton()
    {
		if (world != null)
			world.cityBuilderManager.PlayCloseAudio();
		else
			titleScreen.PlayCloseAudio();

		ToggleVisibility(false);
    }

    public void SelectItem(UISaveItem uiSaveItem)
    {
        if (selectedSaveItem != null)
            selectedSaveItem.UnselectItem();

        playTimeTitle.SetActive(true);
        playTime.gameObject.SetActive(true);
        playTime.text = ConvertPlayTime(uiSaveItem.playTime);
        versionTitle.SetActive(true);
        version.gameObject.SetActive(true);
        version.text = uiSaveItem.version;
        seedTitle.SetActive(true);
        seed.gameObject.SetActive(true);
        seed.text = uiSaveItem.seed;
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
            uiWarning.SetWarningMessages("Overwrite?","Yup!","Nope");
        }
        uiWarning.ToggleVisibilty(true);
	}

    public void ConfirmWarning()
    {
		if (world != null)
			world.cityBuilderManager.PlaySelectAudio();
		else
			titleScreen.PlaySelectAudio();

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
        seed.gameObject.SetActive(false);
        playTimeTitle.SetActive(false);
        versionTitle.SetActive(false);
        seedTitle.SetActive(false);
		screenshotParent.SetActive(false);

        File.Delete(Application.persistentDataPath + "/" + selectedSaveItem.saveName + "Screen.png");
		File.Delete(path);
	}

	public void SaveGameCheck()
    {
        if (world != null)
            world.cityBuilderManager.PlaySelectAudio();
        else
            titleScreen.PlaySelectAudio();
        
        if (load)
        {
            if (selectedSaveItem == null)
                return;

            Cursor.visible = false;

            if (world != null)
            {
                foreach (GameObject go in GameLoader.Instance.textList)
                    Destroy(go);
            }

            Cursor.visible = false;
            selectedSaveItem.screenshot = null;
            screenshot.sprite = null;
            if (GameLoader.Instance == null)
            {
				string loadName = selectedSaveItem.saveName;
				ToggleVisibility(false);

                //have to do this here, others are in "MapWorld.ClearMap()"
        //        if (world != null)
        //        {
        //            foreach (Transform go in world.unitHolder)
        //            {
        //                Unit unit = go.GetComponent<Unit>();
        //                //if (unit.marker != null)
        //                //    unit.marker.gameObject.SetActive(false);
				    //}
        //        }

                //ClearSaveItems();
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

    public void UpdateSaveItems(string saveName, float savePlayTime, string saveVersion, int seed/*, Texture2D saveScreenshot*/)
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
            uiSaveItem.seed = seed.ToString();
			//Rect rect = new Rect(0, 0, saveScreenshot.width, saveScreenshot.height);
			//uiSaveItem.screenshot = Sprite.Create(saveScreenshot, rect, new Vector2(0.5f, 0.5f));
		}
        else
        {
            selectedSaveItem.playTime = savePlayTime;
            selectedSaveItem.version = saveVersion;
            selectedSaveItem.seed = seed.ToString();
			//Rect rect = new Rect(0, 0, saveScreenshot.width, saveScreenshot.height);
			//selectedSaveItem.screenshot = Sprite.Create(saveScreenshot, rect, new Vector2(0.5f, 0.5f));
		}
	}

    public void ConfirmOverWrite()
    {
        selectedSaveItem.transform.SetAsFirstSibling();
        world.StartSaveProcess(selectedSaveItem.saveName);
	}

    public string GetNameOfLatestSave()
    {
        return saveHolder.GetChild(0).GetComponent<UISaveItem>().saveName;
    }
}
