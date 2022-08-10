using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityPopulation : MonoBehaviour
{
    private int currentPop;
    public int GetPop { get { return currentPop; } }

    private int unusedLabor = 2;
    public int GetSetUnusedLabor { get { return unusedLabor; } set { unusedLabor = value; } }

    private int usedLabor;
    public int GetSetUsedLabor { get { return usedLabor; } set { usedLabor = value; } }

    private int fieldLaborers;
    public int GetSetFieldLaborers { get { return fieldLaborers; } set { fieldLaborers = value; } }

    private int cityLaborers;
    public int GetSetCityLaborers { get { return cityLaborers; } set { cityLaborers = value; } }

    public void IncreasePopulation()
    {
        currentPop++;
    }

    public void DecreasePopulation()
    {
        currentPop--;
    }

    public void IncreaseUnusedLabor()
    {
        unusedLabor++;
    }

    public void DecreaseUnusedLabor()
    {
        unusedLabor--;
    }

    public void IncreaseUsedLabor()
    {
        usedLabor++;
    }

    public void DecreaseUsedLabor()
    {
        usedLabor--;
    }

    public void IncreasePopulationAndLabor()
    {
        currentPop++;
        unusedLabor++;
    }
}
