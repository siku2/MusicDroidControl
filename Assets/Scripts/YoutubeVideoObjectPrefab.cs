using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class YoutubeVideoObjectPrefab : MonoBehaviour, IPointerUpHandler, IDragHandler, IPointerDownHandler
{
	[SerializeField] Youtube youtube;
	[SerializeField] Image thumbnailDisplay;
	[SerializeField] Text videoTitle;
	[SerializeField] Text description;
	[SerializeField] float scrollThreshold;

	YoutubeVideoObject video;
	Vector2 startPos;


	public IEnumerator Setup(Youtube yt, YoutubeVideoObject vid)
	{
		WWW www = new WWW(vid.thumbnailUrl);
		yield return www;

		transform.name = vid.name;

		this.youtube = yt;
		this.video = vid;
		this.thumbnailDisplay.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
		this.videoTitle.text = vid.name;
		this.description.text = vid.description;
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
//			print("this is a click");
			youtube.PlayVideo(video);
		}
	}


	public void OnPointerDown(PointerEventData data)
	{
		startPos = data.position;
	}


	public void OnDrag(PointerEventData data)
	{
		youtube.scrollHandler.Scroll(data.delta.y);
	}
}
