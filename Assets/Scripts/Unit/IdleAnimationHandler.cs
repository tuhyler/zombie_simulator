using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleAnimationHandler : StateMachineBehaviour
{
    [SerializeField]
    private float totalTime = 10;
    [SerializeField]
    private int animationCount = 0;
    private bool isBored;
    private float idleTime;
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        isBored = false;
        idleTime = 0;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!isBored)
        {
            idleTime += Time.deltaTime;

            if (idleTime >= totalTime)
            {
                isBored = true;
                animator.SetInteger("IdleAnimsIndex", Random.Range(0, animationCount + 1));
                animator.SetTrigger("IdleAnimations");
            }
        }
    }
}
