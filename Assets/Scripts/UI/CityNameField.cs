using TMPro;
using UnityEngine;

public class CityNameField : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer cityNameField;

    [SerializeField]
    public TMP_Text cityName, cityPop;

    [SerializeField]
    private SpriteRenderer background;

    [SerializeField]
    private Sprite originalBackground, neutralBackground, enemyBackground;

    //[SerializeField]
    //private Material cityNameMaterial;
    //private Material originalCityNameMaterial;


    private void Awake()
    {
        //originalCityNameMaterial = cityNameField.material;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void ToggleVisibility(bool v)
    {
        gameObject.SetActive(v);
    }

    public void SetCityNameFieldSize(string cityName)
    {
        float wordLength = cityName.Length;
        float width = Mathf.Max(wordLength * 0.18f + 1f, 1f /*3f*/);
        cityNameField.size = new Vector2(width, 1.0f);
    }

    public void SetCityName(string cityName)
    {
        this.cityName.text = cityName;
    }

    public void SetCityPop(int pop)
    {
        cityPop.text = $"Pop: {pop}";
    }

    public void SetOriginalBackground()
    {
        background.sprite = originalBackground;
    }

    public void SetNeutralBackground()
    {
        background.sprite = neutralBackground;
    }

    public void SetEnemyBackGround()
    {
        background.sprite = enemyBackground;
    }
}
