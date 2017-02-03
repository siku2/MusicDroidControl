using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[System.Serializable]
public struct RadioStation
{
	public Sprite logo;
	public string name;
	public string language;
}


public class RadioPanel : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
	public YoutubeScrollHandler scrollHandler;
	public bool blocking;

	[SerializeField] Manager manager;
	[SerializeField] RadioStation[] stations;
	[SerializeField] RectTransform rectTransform;
	[SerializeField] RadioStationObject stationObjectPrefab;
	[SerializeField] Transform stationObjectParent;
	[SerializeField] Image background;
	[SerializeField] float slideThreshold;
	[SerializeField] float posMultiplier;
	[SerializeField] float openPercentage;
	[SerializeField] float closePercentage;
	[SerializeField] float speed;

	List<RadioStationObject> existingObjects = new List<RadioStationObject>();
	bool open;


	public void OnStationClick(int index)
	{
		manager.RadioPlayCommand(stations[index].name);
	}


	public void Show()
	{
		DisplayRadioStations();
		gameObject.SetActive(true);
		open = true;
		scrollHandler.GotoTop();
	}


	void Update()
	{
		if(!blocking)
		{
			if(open && rectTransform.anchoredPosition.x < 0)
			{
				rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(rectTransform.anchoredPosition.x + speed, -rectTransform.rect.size.x, 0), rectTransform.anchoredPosition.y);
			}

			if(!open && rectTransform.anchoredPosition.x > -rectTransform.rect.size.x)
			{
				rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(rectTransform.anchoredPosition.x - speed, -rectTransform.rect.size.x, 0), rectTransform.anchoredPosition.y);
			}

			background.color = new Color(background.color.r, background.color.g, background.color.b, Mathf.InverseLerp(-rectTransform.rect.size.x, 0, rectTransform.anchoredPosition.x) * .3f);

			if(!open && rectTransform.anchoredPosition.x == -rectTransform.rect.size.x)
			{
				gameObject.SetActive(false);
			}
		}
	}


	public void OnBeginDrag(PointerEventData data)
	{
		blocking = true;
	}


	public void OnDrag(PointerEventData data)
	{
		float xSlide = data.delta.x;
		if(Mathf.Abs(xSlide) > slideThreshold)
		{
			rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(rectTransform.anchoredPosition.x + xSlide, -rectTransform.rect.size.x, 0), rectTransform.anchoredPosition.y);
			background.color = new Color(background.color.r, background.color.g, background.color.b, Mathf.InverseLerp(-rectTransform.rect.size.x, 0, rectTransform.anchoredPosition.x) * .3f);
		}
	}


	public void OnEndDrag(PointerEventData data)
	{
		float perc = openPercentage;
		if(open)
		{
			perc = closePercentage;
		}

		if(rectTransform.anchoredPosition.x < -rectTransform.rect.size.x * perc)
		{
			open = false;
		}
		else
		{
			open = true;
		}

		blocking = false;
	}


	void EnsureEnoughObjects(int amountNeeded)
	{
		int diff = amountNeeded - existingObjects.Count;
		if(diff > 0)
		{
			for(int i = 0; i < diff; i++)
			{
				existingObjects.Add(Instantiate(stationObjectPrefab, stationObjectParent, false) as RadioStationObject);
			}
		}
		else
		if(diff < 0)
		{
			for(int i = existingObjects.Count - 1; i >= existingObjects.Count + diff; i--)
			{
				existingObjects[i].gameObject.SetActive(false);
			}
		}
	}


	void DisplayRadioStations()
	{
		EnsureEnoughObjects(stations.Length);

		for(int i = 0; i < stations.Length; i++)
		{
			RadioStation station = stations[i];
			existingObjects[i].Setup(this, i, station.name, station.language, station.logo);
		}

		scrollHandler.GotoTop();
	}
}
