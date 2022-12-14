using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAssets : MonoBehaviour
{
    private static GameAssets instance;

    public static GameAssets Instance { 
        get {
            if (instance == null) instance = Instantiate(Resources.Load<GameAssets>("GameAssets"));
            return instance; 
        } 
    }

    public GameObject popUpTextPrefab;
    public GameObject popUpResourceNumbersPrefab;
    public GameObject cityBorderPrefab;
    public GameObject shoePrintPrefab;
    public GameObject laborNumberPrefab;
    public GameObject cityGrowthProgressPrefab;
    public GameObject cityGrowthProgressPrefab2;
    public GameObject timeProgressPrefab;
    public GameObject resourceBubble;
    public GameObject resourceInfoPanel;
    public GameObject uiTimeProgressPrefab;
}
