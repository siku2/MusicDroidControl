using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class YoutubeScrollHandler : MonoBehaviour, IDragHandler
{
	[SerializeField] RectTransform scrollWindow;
	[SerializeField] Vector2 bounds;
	[SerializeField] float scrollSpeed;
	[SerializeField] float speedDecayRate;

	float currentSpeed;


	void Update()
	{
		currentSpeed *= speedDecayRate;
		ReallyScroll(currentSpeed);
	}


	void ReallyScroll(float deltaY)
	{
		Vector3 newPos = scrollWindow.position;
		newPos += deltaY * Vector3.up;
		newPos.y = Mathf.Clamp(newPos.y, bounds.x, bounds.y);

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
