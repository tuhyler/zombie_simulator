using UnityEngine;

public class ImprovementAnimators : MonoBehaviour
{
    //animation
    private Animator improvementAnimator;
    private int isWorkingHash;
    private int isWaitingHash;

    void Awake()
    {
        improvementAnimator = GetComponent<Animator>();
        isWorkingHash = Animator.StringToHash("isWorking");
        isWaitingHash = Animator.StringToHash("isWaiting");
    }

    public void StartAnimation(int seconds)
    {
        improvementAnimator.SetBool(isWorkingHash, false); //stopping first
        improvementAnimator.SetBool(isWorkingHash, true);
        improvementAnimator.SetFloat("speed", 1f/seconds);
    }

    public void StopAnimation(bool waiting)
    {
        if (waiting)
            improvementAnimator.SetBool(isWaitingHash, true);
        else
            improvementAnimator.SetBool(isWorkingHash, false);
    }
}
