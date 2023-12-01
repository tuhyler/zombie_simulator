using System.Collections;
using TMPro;
using UnityEngine;

public class SpeechBubbleHandler : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer bubble;
    
    [SerializeField]
    private TMP_Text speech;

    public float wordPause = 0.1f;
    public float sentencePause = 0.4f;
    private WaitForSeconds wordWait, sentenceWait;

    private Coroutine co;

	private void Awake()
	{
        wordWait = new WaitForSeconds(wordPause);
        sentenceWait = new WaitForSeconds(sentencePause);
	}

	void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void SetText(Vector3 location, string text)
    {
        location.y += 1.85f;
        location.z += .2f;
        transform.position = location;
        gameObject.SetActive(true);

        string[] textArray = text.Split(' ');
        //SetSpriteSize(textArray.Length);
        co = StartCoroutine(ShowSpeech(textArray));
    }

    private IEnumerator ShowSpeech(string[] textArray)
    {
        speech.text = textArray[0];
        for (int i = 1; i < textArray.Length; i++)
        {
            if (speech.text.EndsWith('.'))
                yield return sentenceWait;
            else
                yield return wordWait;

            speech.text += " " + textArray[i];
        }
    }

    private void SetSpriteSize(int wordCount)
    {
        Vector3 scale = new Vector3(2, 2, 2);
        bubble.transform.localScale = scale; 
    }

    public void CancelText()
    {
        gameObject.SetActive(false);
        
        if (co != null)
            StopCoroutine(co);
    }
}
