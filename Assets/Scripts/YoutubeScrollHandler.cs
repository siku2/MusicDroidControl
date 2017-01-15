using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class YoutubeScrollHandler : MonoBehaviour, IDragHandler
{
	[SerializeField] RectTransform scrollWindow;
	[SerializeField] RectTransform viewPort;
	[SerializeField] float scrollSpeed;
	[SerializeField] float speedDecayRate;
	[SerializeField] float minSpeed;

	float currentSpeed;
	Vector3[] worldCorners = new Vector3[4];
	Vector3[] worldViewCorners = new Vector3[4];


	void Update()
	{
		if(Mathf.Abs(currentSpeed) > minSpeed)
		{
			currentSpeed *= speedDecayRate;
			ReallyScroll(currentSpeed);
		}
	}


	void ReallyScroll(float deltaY)
	{
		Vector3 newPos = scrollWindow.position + deltaY * Vector3.up;

		scrollWindow.GetWorldCorners(worldCorners);
		viewPort.GetWorldCorners(worldViewCorners);
		if(deltaY < 0 && worldCorners[1].y < worldViewCorners[1].y)
		{
			return;
		}
		if(deltaY > 0 && worldCorners[0].y > worldViewCorners[0].y)
		{
			return;
		}

		scrollWindow.position = newPos;
	}


	public void Scroll(float deltaY)
	{
		ReallyScroll(scrollSpeed * deltaY);
		currentSpeed = scrollSpeed * deltaY;
	}


	public void OnDrag(PointerEventData data)
	{
		Scroll(data.delta.y);
	}
}
