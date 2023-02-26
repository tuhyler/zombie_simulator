using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIImprovementBuildPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    [SerializeField]
    private Image image;

    [SerializeField]
    private RectTransform imageBackground;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    //private Vector3 originalLoc;

    private void Awake()
    {
        gameObject.SetActive(false);
        imageBackground.gameObject.SetActive(false);
        image.gameObject.SetActive(false);
    }

    public void SetText(string text)
    {
        this.text.text = text;
    }

    public void SetImage(Sprite image)
    {
        imageBackground.gameObject.SetActive(true);
        this.image.gameObject.SetActive(true);
        this.image.sprite = image;
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(true);

            activeStatus = true;

            //allContents.anchoredPosition3D = originalLoc + new Vector3(0, 100f, 0);

            LeanTween.scale(allContents, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutSine);
            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 100f, 0.3f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            imageBackground.gameObject.SetActive(false);
            image.gameObject.SetActive(false);
            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 100f, 0.2f).setOnComplete(SetActiveStatusFalse);
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }
}
