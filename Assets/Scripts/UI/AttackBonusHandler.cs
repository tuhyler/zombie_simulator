using TMPro;
using UnityEngine;

public class AttackBonusHandler : MonoBehaviour
{
	[SerializeField]
	public TMP_Text text;
	
	private void Awake()
	{
		text = GetComponent<TMP_Text>();
		text.outlineWidth = 0.35f;
		text.outlineColor = Color.black;
	}

	void LateUpdate()
    {
		transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
	}
}
