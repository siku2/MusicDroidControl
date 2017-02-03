using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class RadioStationObject : MonoBehaviour, IPointerUpHandler, IDragHandler, IPointerDownHandler, IEndDragHandler, IBeginDragHandler
{
	[SerializeField] Text nameDisplay;
	[SerializeField] Text languageDisplay;
	[SerializeField] Image logoDisplay;
	[SerializeField] GameObject overlay;
	[SerializeField] float scrollThreshold;
	[SerializeField] float slideThreshold;

	RadioPanel panel;
	int index;
	Vector2 startPos;


	public void Setup(RadioPanel panel, int index, string name, string language, Sprite image)
	{
		gameObject.SetActive(true);
		transform.name = name;

		this.panel = panel;
		this.index = index;

		nameDisplay.text = name;
		languageDisplay.text = language;
		logoDisplay.sprite = image;
	}


	public void OnPointerUp(PointerEventData data)
	{
		if(Mathf.Abs(data.position.y - startPos.y) > scrollThreshold)
		{
			//			print("this was a scroll");
			//we don't want scrolls to be registered as clicks.
		}
		else
		{
			panel.OnStationClick(index);
		}

		overlay.SetActive(false);
	}


	public void OnPointerDown(PointerEventData data)
	{
		startPos = data.position;
		overlay.SetActive(true);
	}


	public void OnBeginDrag(PointerEventData data)
	{
		panel.blocking = true;
	}


	public void OnDrag(PointerEventData data)
	{
		if(Mathf.Abs(data.delta.x) > slideThreshold && Mathf.Abs(data.delta.y) < scrollThreshold)
		{
			panel.OnDrag(data);
		}

		if(panel.open || !panel.blocking)
		{
			panel.scrollHandler.Scroll(data.delta.y);
		}
	}


	public void OnEndDrag(PointerEventData data)
	{
		panel.OnEndDrag(data);
	}
}
