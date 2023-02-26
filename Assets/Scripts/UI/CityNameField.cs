using TMPro;
using UnityEngine;

public class CityNameField : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer cityNameField;

    [SerializeField]
    public TMP_Text cityName, cityPop;

    [SerializeField]
    private Material cityNameMaterial;
    private Material originalCityNameMaterial;


    private void Awake()
    {
        originalCityNameMaterial = cityNameField.material;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void ToggleVisibility(bool v)
    {
        if (v)
        {
            gameObject.SetActive(true);
            
            //gameObject.LeanScale() = Vector3.zero;
            //LeanTween.scale(gameObject, Vector3.one, 0.1f);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void EnableHighlight()
    {
        cityNameField.material = cityNameMaterial;
    }

    public void DisableHighlight()
    {
        cityNameField.material = originalCityNameMaterial;
    }

    public void SetCityNameFieldSize(string cityName)
    {
        float wordLength = cityName.Length;
        float width = Mathf.Max(wordLength * 0.18f + 0.7f, 2.7f);
        cityNameField.size = new Vector2(width, 1.0f);
    }

    public void SetCityName(string cityName)
    {
        this.cityName.text = cityName;
    }

    public void SetCityPop(int pop)
    {
        cityPop.text = $"Population: {pop}";
    }
}
