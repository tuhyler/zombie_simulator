using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAmbush
{
    public Vector3Int loc;
    public List<Unit> attackingUnits = new();
    public List<Unit> attackedUnits = new();
}
