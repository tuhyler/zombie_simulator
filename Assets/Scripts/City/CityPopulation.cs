using UnityEngine;

public class CityPopulation : MonoBehaviour
{
    private int currentPop = 0;
    public int CurrentPop { get { return currentPop; } set { currentPop = value; } }

    private int unusedLabor = 0;
    public int UnusedLabor { get { return unusedLabor; } set { unusedLabor = value; } }

    private int usedLabor;
    public int UsedLabor { get { return usedLabor; } set { usedLabor = value; } }

    //public void IncreasePopulationAndLabor(int amount)
    //{
    //    currentPop += amount;
    //    unusedLabor += amount;
    //}

    //private int fieldLaborers;
    //public int GetSetFieldLaborers { get { return fieldLaborers; } set { fieldLaborers = value; } }

    //private int cityLaborers;
    //public int GetSetCityLaborers { get { return cityLaborers; } set { cityLaborers = value; } }

    //public void IncreasePopulation()
    //{
    //    currentPop++;
    //}

    //public void DecreasePopulation()
    //{
    //    currentPop--;
    //}

    //public void IncreaseUnusedLabor()
    //{
    //    unusedLabor++;
    //}

    //public void DecreaseUnusedLabor()
    //{
    //    unusedLabor--;
    //}

    //public void IncreaseUsedLabor()
    //{
    //    usedLabor++;
    //}

    //public void DecreaseUsedLabor()
    //{
    //    usedLabor--;
    //}
}
