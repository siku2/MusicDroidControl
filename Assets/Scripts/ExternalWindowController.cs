using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class ExternalWindowController : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
	[SerializeField] YoutubeWindowController originalController;


	public void OnBeginDrag(PointerEventData data)
	{
		originalController.OnBeginDrag(data);
	}


	public void OnDrag(PointerEventData data)
	{
		originalController.OnDrag(data);
	}


	public void OnEndDrag(PointerEventData data)
	{
		originalController.OnEndDrag(data);
	}
}
