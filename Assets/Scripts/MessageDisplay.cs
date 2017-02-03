using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MessageDisplay : MonoBehaviour
{
	[SerializeField] Text msgDisplay;
	[SerializeField] RectTransform msgField;
	[SerializeField] RectTransform fullScreenRect;
	[SerializeField] float minRectSizePercentage;
	[SerializeField] float maxCharacters;


	public void DisplayMessage(string msg)
	{
		gameObject.SetActive(true);

		msgDisplay.text = msg;
		StartCoroutine(SetSizeDelta(msg));
	}


//	void Start()
//	{
//		string testmsg = "A test with many, many character in order to test this feature properly. Like this is gonna be a long text. And when I say long, I mean long LIKE REAAAAAAAAAAAAAAAAAAAAAALY LONG. I need to test how many characters fit into this box before it starts to look weird";
//		DisplayMessage(testmsg);
//		print(testmsg.Length);
//	}


	IEnumerator SetSizeDelta(string msg)
	{
		yield return null;
		msgField.sizeDelta = new Vector2(msgField.sizeDelta.x, Mathf.Clamp(Mathf.Lerp(0, fullScreenRect.rect.size.y, msg.Length / maxCharacters), minRectSizePercentage * fullScreenRect.rect.size.y, fullScreenRect.rect.size.y));
	}


	public void OnOkayClick()
	{
		gameObject.SetActive(false);
	}
}
