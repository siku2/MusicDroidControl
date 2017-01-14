using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class YoutubeWindowController : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
	[SerializeField] RectTransform window;
	[SerializeField] int canvasSize;
	[SerializeField] float posMultiplier;
	[SerializeField] float openPercentage;
	[SerializeField] float closePercentage;
	[SerializeField] float speed;

	bool open = true;
	bool blocking;


	void Update()
	{
		if(!blocking)
		{
			if(open && window.offsetMin.y > 0)
			{
				window.offsetMin = new Vector2(0, Mathf.Clamp(window.offsetMin.y - speed, 0, canvasSize));
			}

			if(!open && window.offsetMin.y < canvasSize)
			{
				window.offsetMin = new Vector2(0, Mathf.Clamp(window.offsetMin.y + speed, 0, canvasSize));
			}

			if(!open && window.offsetMin.y == canvasSize)
			{
				window.gameObject.SetActive(false);
			}
		}
	}


	public void Open()
	{
		open = true;
	}


	public void OnBeginDrag(PointerEventData data)
	{
		blocking = true;
	}


	public void OnDrag(PointerEventData data)
	{
		window.offsetMin = new Vector2(0, Mathf.Clamp(data.position.y * posMultiplier, 0, canvasSize));
	}


	public void OnEndDrag(PointerEventData data)
	{
		float perc = openPercentage;
		if(open)
		{
			perc = closePercentage;
		}

		if(window.offsetMin.y > canvasSize * perc)
		{
//			print("close");
			open = false;
		}
		else
		{
//			print("open");
			open = true;
		}

		blocking = false;
	}
}
