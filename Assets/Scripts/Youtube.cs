using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;


public class YoutubeVideoObject
{
	public string name;
	public string channel;
	public string description;
	public string videoUrl;
	public string thumbnailUrl;


	public YoutubeVideoObject(string name, string channel, string description, string videoUrl, string thumbnailUrl)
	{
		this.name = name;
		this.channel = channel;
		this.description = description;
		this.videoUrl = videoUrl;
		this.thumbnailUrl = thumbnailUrl;
	}


	public override string ToString()
	{
		return string.Format("[YoutubeVideoObject] {0} by {1}\n\"{2}\"\nurl: {3}\nthumbnail: {4}", this.name, this.channel, this.description, this.videoUrl, this.thumbnailUrl);
	}
}


public class CoroutineWithData
{
	public Coroutine coroutine { get; private set; }


	public object result;
	private IEnumerator target;


	public CoroutineWithData(MonoBehaviour owner, IEnumerator target)
	{
		this.target = target;
		this.coroutine = owner.StartCoroutine(Run());
	}


	private IEnumerator Run()
	{
		while(target.MoveNext())
		{
			result = target.Current;
			yield return result;
		}
	}
}


public class Youtube : MonoBehaviour
{
	[SerializeField] string apiKey;


	IList<YoutubeVideoObject> ParseJson(string json)
	{
		JObject result = JObject.Parse(json);
		IList<JToken> videosFound = result["items"].Children().ToList();
		IList<YoutubeVideoObject> videos = new List<YoutubeVideoObject>();
		foreach(JToken video in videosFound)
		{
			JToken data = video["snippet"];
			string title = data["title"].ToString();
			string channel = data["channelTitle"].ToString();
			string desc = data["description"].ToString();
			string videoUrl = "https://www.youtube.com/watch?v=" + video["id"]["videoId"].ToString();
			string thumbnailUrl = data["thumbnails"]["default"]["url"].ToString();
			videos.Add(new YoutubeVideoObject(title, channel, desc, videoUrl, thumbnailUrl));
		}

		return videos;
	}


	IEnumerator GetPopularMusic()
	{
		WWW www = new WWW("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=10&order=relevance&type=video&videoCategoryId=10&key=" + apiKey);
		yield return www;
		IList<YoutubeVideoObject> result = ParseJson(www.text);

		yield return result;
	}


	IEnumerator Start()
	{
		CoroutineWithData cd = new CoroutineWithData(this, GetPopularMusic());
		yield return cd.coroutine;

		foreach(YoutubeVideoObject vid in (IList<YoutubeVideoObject>) cd.result)
		{
//			print(vid.ToString());
		}
	}
}
