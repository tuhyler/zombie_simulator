using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NPCData
{
    public bool somethingToSay, onQuest, hasMetKoa;
    public List<string> conversationTopics;
    public int currentQuest, timeWaited, purchasedAmount;
}
