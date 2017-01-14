using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;
using UnityEngine.UI;


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


public enum SearchMode
{
	VIDEOS,
	PLAYLIST
}


public enum Focus
{
	TRENDING,
	HISTORY
}


public class Youtube : MonoBehaviour
{
	public YoutubeScrollHandler scrollHandler;

	[SerializeField] Manager manager;
	[SerializeField] RectTransform youtubeRect;
	[SerializeField] Transform ytPrefabParent;
	[SerializeField] InputField searchField;
	[SerializeField] YoutubeVideoObjectPrefab ytPrefab;
	[SerializeField] Text currentSearchModeDisplay;
	[SerializeField] string apiKey;
	[SerializeField] int searchResults;
	[SerializeField] string regionCode;

	SearchMode searchMode = SearchMode.VIDEOS;
	Focus focus = Focus.TRENDING;


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


	IEnumerator DisplayVideos(IList<YoutubeVideoObject> youtubeObjects)
	{
		for(int i = 0; i < ytPrefabParent.childCount; i++)
		{
			Destroy(ytPrefabParent.GetChild(i).gameObject);
		}
		IList<Coroutine> coroutines = new List<Coroutine>();
		foreach(YoutubeVideoObject vid in youtubeObjects)
		{
			coroutines.Add(StartCoroutine(YoutubeObjectCreator(vid)));
		}

		foreach(Coroutine c in coroutines)
		{
			yield return c;
		}
	}


	IEnumerator YoutubeObjectCreator(YoutubeVideoObject vid)
	{
		YoutubeVideoObjectPrefab newPrefab = Instantiate(ytPrefab, ytPrefabParent, false) as YoutubeVideoObjectPrefab;
		yield return StartCoroutine(newPrefab.Setup(this, vid));
	}


	IEnumerator SearchForVideos(string query)
	{
		WWW www = new WWW("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=" + searchResults + "&order=relevance&q=" + WWW.EscapeURL(query) + "&regionCode=" + regionCode + "&type=video&videoCategoryId=10&key=" + apiKey);
		yield return www;
		IList<YoutubeVideoObject> result = ParseJson(www.text);

		yield return result;
	}


	IEnumerator GetPopularMusic()
	{
		WWW www = new WWW("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=" + searchResults + "&order=relevance&regionCode=" + regionCode + "&type=video&videoCategoryId=10&key=" + apiKey);
		yield return www;
		IList<YoutubeVideoObject> result = ParseJson(www.text);

		yield return result;
	}


	public void PlayVideo(YoutubeVideoObject vid)
	{
		manager.VideoPlayCommand(vid);
	}


	public IEnumerator ShowHistory()
	{
		print("not yet properly implemented");
		yield break;
	}


	public IEnumerator ShowTrending()
	{
		CoroutineWithData cd = new CoroutineWithData(this, GetPopularMusic());
		yield return cd.coroutine;

		StartCoroutine(DisplayVideos((IList<YoutubeVideoObject>) cd.result));
	}


	public IEnumerator SearchEnter()
	{
		string currentText = searchField.text;
		if(currentText.Trim() == "")
		{
			yield break;
		}

		if(searchMode == SearchMode.VIDEOS)
		{
			CoroutineWithData cd = new CoroutineWithData(this, SearchForVideos(currentText.Trim()));
			yield return cd.coroutine;
			StartCoroutine(DisplayVideos(cd.result as IList<YoutubeVideoObject>));
		}
	}


	public void OnEndEditSearchQuery()
	{
		StartCoroutine(SearchEnter());
	}


	public void SwitchToTrending()
	{
		if(focus == Focus.TRENDING)
		{
			return;
		}

		focus = Focus.TRENDING;
		StartCoroutine(ShowTrending());
	}


	public void SwitchToHistory()
	{
		if(focus == Focus.HISTORY)
		{
			return;
		}

		focus = Focus.HISTORY;
		StartCoroutine(ShowHistory());
	}


	public void SwitchSearchMode()
	{
		if(searchMode == SearchMode.VIDEOS)
		{
			searchMode = SearchMode.PLAYLIST;
			currentSearchModeDisplay.text = "Playlist";
		}
		else
		if(searchMode == SearchMode.PLAYLIST)
		{
			searchMode = SearchMode.VIDEOS;
			currentSearchModeDisplay.text = "Song";
		}

		//update the current Focus
		if(focus == Focus.TRENDING)
		{
			StartCoroutine(ShowTrending());
		}
		else
		if(focus == Focus.HISTORY)
		{
			StartCoroutine(ShowHistory());
		}
	}


	void Start()
	{
		StartCoroutine(ShowTrending());
	}
}
