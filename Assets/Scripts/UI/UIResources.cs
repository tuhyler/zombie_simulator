using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIResources : MonoBehaviour
{
    [SerializeField]
    private TMP_Text resourceAmount;//, resourceGenerationAmount;
    [SerializeField]
    private ResourceType resourceType;

    private int resourceValue;

    public ResourceType ResourceType { get => resourceType; }

    private void Start()
    {
        //resourceLimitAmount.color = Color.red; //need to change text color for all to white in order to see this
        if (resourceType == ResourceType.None)
            throw new System.ArgumentException("Resource type can't be none! in " + gameObject.name);
        //gameObject.SetActive(false);
    }

    public void SetActiveStatus(bool v)
    {
        gameObject.SetActive(v);
    }

    public void CheckVisibility()
    {
        if (resourceValue > 0) //convert string to integer
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void SetValue(int val)
    {
        resourceValue = val;
        resourceAmount.text = val.ToString();
    }

    public void SetGeneration(int val)
    {
        //if (val > 0)
        //{
        //    //resourceGenerationAmount.text = $"+{val}";
        //}
        //if (val == 0)
        //{
        //    //resourceGenerationAmount.text = "-";
        //}
        //if (val < 0)
        //{
        //    //resourceGenerationAmount.text = val.ToString();
        //}
    }
}
