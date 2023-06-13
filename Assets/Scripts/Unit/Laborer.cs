using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laborer : Unit
{
    //animations
    private int celebrateTime = 15;
    private int isCelebratingHash;
    private int isJumpingHash;
    [HideInInspector]
    public Coroutine co;

    private void Awake()
    {
        AwakeMethods();
        isLaborer = true;
        isCelebratingHash = Animator.StringToHash("isCelebrating");
        isJumpingHash = Animator.StringToHash("isJumping");
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
    }

    public IEnumerator Celebrate()
    {
        unitAnimator.SetBool(isCelebratingHash, true);
        int randomWait = Random.Range(1, 4);
        int currentWait = 0;
        int totalWait = 0;

        while (totalWait < celebrateTime)
        {
            yield return new WaitForSeconds(1);
            totalWait++;
            currentWait++;
            if (currentWait == randomWait)
            {
                unitAnimator.SetBool(isJumpingHash, true);
            }
            else if (currentWait > randomWait)
            {
                unitAnimator.SetBool(isJumpingHash, false);
                currentWait = 0;
                randomWait = Random.Range(0, 5);
            }
        }

        StopLaborAnimations();
    }

    public void StartLaborAnimations()
    {
        co = StartCoroutine(Celebrate());
    }

    public void StopLaborAnimations()
    {
        unitAnimator.SetBool(isCelebratingHash, false);
        unitAnimator.SetBool(isJumpingHash, false);
    }
}
