using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MessageDisplay : MonoBehaviour
{
	[SerializeField] Text msgDisplay;
	[SerializeField] RectTransform msgField;


	public void DisplayMessage(string msg)
	{
		gameObject.SetActive(true);

		msgDisplay.text = msg;
		StartCoroutine(SetSizeDelta());
	}


	IEnumerator SetSizeDelta()
	{
		yield return null;
		msgField.sizeDelta = new Vector2(msgField.sizeDelta.x, msgDisplay.rectTransform.sizeDelta.y * 1.1f);
	}


	public void OnOkayClick()
	{
		gameObject.SetActive(false);
	}
}
