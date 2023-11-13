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
    private UIUnitTurnHandler turnHandler;

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

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
            StoreName();
    }

    public void ToggleVisibility(bool v, City city = null)
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
                placeHolderText = city.cityName;
                placeHolder.text = city.cityName;
                placeHolder.alpha = 0.5f;
                inputField.Select();
            }
        }
        else
        {
            activeStatus = false;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
        tempCity = city;
    }

    public void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void StoreName() //method for 'confirm' button
    {
        tempCity.world.cityBuilderManager.PlaySelectAudio();   
        
        string tempText = inputField.text;

        if (tempText.Length < 1 || tempText == placeHolderText)
        {
            tempText = placeHolderText;
            ToggleVisibility(false);
            return;
        }

        if (tempCity.CheckCityName(tempText))
        {
            StartCoroutine(Shake(.25f, 10));

            Debug.Log("Name already used.");
            return;
        }

        tempCity.RemoveCityName();
        tempCity.UpdateCityName(tempText);
        uiInfoPanelCity.UpdateCityName(tempText);
        ToggleVisibility(false);
    }


    IEnumerator Shake (float duration, float magnitude)
    {
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(x, y, originalPosition.z);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}
