using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class YoutubeVideoObjectPrefab : MonoBehaviour, IPointerUpHandler, IDragHandler, IPointerDownHandler
{
	[SerializeField] Youtube youtube;
	[SerializeField] Image thumbnailDisplay;
	[SerializeField] Text videoTitle;
	[SerializeField] Text channel;
	[SerializeField] Text duration;
	[SerializeField] float scrollThreshold;

	YoutubeVideoObject video;
	YoutubePlaylistObject playlist;
	Vector2 startPos;


	public IEnumerator Setup(Youtube yt, YoutubeVideoObject vid)
	{
		WWW www = new WWW(vid.thumbnailUrl);
		yield return www;

		transform.name = vid.name;
		gameObject.SetActive(true);

		this.youtube = yt;
		this.video = vid;
		this.playlist = null;
		this.thumbnailDisplay.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
		this.videoTitle.text = vid.name;
		this.channel.text = vid.channel;

		TimeSpan t = TimeSpan.FromSeconds(vid.duration);
		string ans = "";
		if(t.Hours > 0)
		{
			ans = string.Format("{0}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
		}
		else
		{
			ans = string.Format("{0}:{1:D2}",t.Minutes, t.Seconds);
		}
		this.duration.text = ans;
	}


	public IEnumerator Setup(Youtube yt, YoutubePlaylistObject pl)
	{
		WWW www = new WWW(pl.thumbnailURL);
		yield return www;

		transform.name = pl.name;
		gameObject.SetActive(true);

		this.youtube = yt;
		this.video = null;
		this.playlist = pl;
		this.thumbnailDisplay.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
		this.videoTitle.text = pl.name;
		this.channel.text = pl.channel;
		this.duration.text = "PLAYLIST";
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
			if(video != null)
			{
				youtube.PlayVideo(video);
			}
			else
			if(playlist != null)
			{
				youtube.PlayPlaylist(playlist);
			}
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
