using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMarketResourcePanel : MonoBehaviour
{
    [HideInInspector]
    public string resourceName;
    [HideInInspector]
    public int price, amount;
    [HideInInspector]
    public bool sell;

    [SerializeField]
    public Image resourceImage;

    [SerializeField]
    public TMP_Text cityPrice, cityAmount;

    [SerializeField]
    public Toggle sellToggle;

    [SerializeField]
    public TMP_InputField minimumAmount;

    
}
