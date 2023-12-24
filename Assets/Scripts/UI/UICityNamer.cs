using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class UICityNamer : MonoBehaviour
{
    [SerializeField]
    private TMP_Text placeHolder;

    [SerializeField]
    private TMP_InputField inputField;

    //[SerializeField]
    //private CityDataSO cityDataSO;

    [SerializeField]
    private HandlePlayerInput playerInput;

    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private UICityBuildTabHandler uiCityBuildTabHandler;

    [SerializeField]
    private UILaborAssignment uiLaborAssignment;

    [SerializeField]
    private UIInfoPanelCity uiInfoPanelCity;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;

    private string placeHolderText;

    private City tempCity;
    private Trader tempTrader;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

	private void Start()
	{
		inputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return AlphaNumericSpaceCheck(addedChar); };
	}

	private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
            StoreName();
    }

	private char AlphaNumericSpaceCheck(char c)
    {
        if (!Char.IsWhiteSpace(c) && !Char.IsLetter(c) && !Char.IsDigit(c))
            c = '\0';

        return c;
    }

	public void ToggleVisibility(bool v, City city = null, Trader trader = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            activeStatus = true;

            allContents.localScale = Vector3.zero;
            LeanTween.scale(allContents, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutBack);

            if (city != null)
            {
                city.world.cameraController.paused = true;
                placeHolderText = city.cityName;
                placeHolder.text = city.cityName;
                tempCity = city;
            }
            else if (trader != null)
            {
				trader.world.cameraController.paused = true;
				placeHolderText = trader.name;
                placeHolder.text = trader.name;
                tempTrader = trader;
            }

            placeHolder.alpha = 0.5f;
            inputField.Select();
        }
        else
        {
            if (tempCity)
                tempCity.world.cameraController.paused = false;
            else
                tempTrader.world.cameraController.paused = false;

			activeStatus = false;
            tempCity = null;
            tempTrader = null;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }

    }

    public void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void StoreName() //method for 'confirm' button
    {
        if (tempCity != null)
            tempCity.world.cityBuilderManager.PlaySelectAudio();
        else
            tempTrader.world.cityBuilderManager.PlaySelectAudio();

        string tempText = inputField.text;

        if (tempText.Length < 1 || tempText == placeHolderText)
        {
            tempText = placeHolderText;
            ToggleVisibility(false);
            return;
        }

        if (tempCity != null)
        {
            if (tempCity.CheckCityName(tempText))
            {
                StartCoroutine(Shake(.25f, 10));
			    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already taken", true);
			    return;
            }

            tempCity.RemoveCityName();
            tempCity.UpdateCityName(tempText);
            tempCity.isNamed = true;

            uiInfoPanelCity.UpdateCityName(tempText);
        }
        else if (tempTrader != null)
        {
            tempTrader.name = tempText;
            tempTrader.world.unitMovement.infoManager.UpdateName(tempText);
        }

        ToggleVisibility(false);
    }


    IEnumerator Shake (float duration, float magnitude)
    {
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(x, y, originalPosition.z);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}
