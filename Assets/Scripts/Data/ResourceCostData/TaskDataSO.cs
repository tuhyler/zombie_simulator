using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Task Data", menuName = "EconomyData/TaskCostData")]
public class TaskDataSO : ScriptableObject
{
    public GameObject prefab;
    public List<ResourceValue> taskCost;
    public List<ResourceValue> producedResources;
    public ResourceType resourceType;
    public TaskType taskType;
    public int maxLabor;
}

public enum TaskType
{
    Worker,
    Infantry,
    Ranged,
    Cavalry,
    Seige
}


