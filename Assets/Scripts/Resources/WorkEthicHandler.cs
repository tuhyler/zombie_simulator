using UnityEngine;

public class WorkEthicHandler : MonoBehaviour
{
    private ImprovementDataSO myImprovementData;
    public ImprovementDataSO GetImprovementData { get { return myImprovementData; } }
    private int currentLabor;
    public int GetSetCurrentLabor { get { return currentLabor; } set { currentLabor = value; } }

    private float currentWorkEthicChange;

    public void InitializeImprovementData(ImprovementDataSO data)
    {
        myImprovementData = data;
    }

    public float GetWorkEthicChange(int labor)
    {
        float prevWorkEthicChange = currentWorkEthicChange;
        currentWorkEthicChange = myImprovementData.workEthicChange * labor;

        return currentWorkEthicChange - prevWorkEthicChange;
    }
}
