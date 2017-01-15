using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;
using UnityEngine.UI;
using System.Xml;


public class YoutubeVideoObject
{
	public string name;
	public string channel;
	public string description;
	public string videoID;
	public string thumbnailUrl;
	public int views;
	public float duration;
	public int likes;
	public int dislikes;
	public int commentCount;


	public YoutubeVideoObject(string name, string channel, string description, string videoID, string thumbnailUrl, int views, float duration, int likes, int dislikes, int comments)
	{
		this.name = name;
		this.channel = channel;
		this.description = description;
		this.videoID = videoID;
		this.thumbnailUrl = thumbnailUrl;
		this.views = views;
		this.duration = duration;
		this.likes = likes;
		this.dislikes = dislikes;
		this.commentCount = comments;
	}


	public override string ToString()
	{
		return string.Format("[YoutubeVideoObject] {0} by {1}\n\"{2}\"\nurl: {3}\nthumbnail: {4}", this.name, this.channel, this.description, this.videoID, this.thumbnailUrl);
	}
}


public class YoutubePlaylistObject
{
	public string name;
	public string description;
	public string thumbnailURL;
	public string channel;
	public string playlistID;


	public YoutubePlaylistObject(string name, string desc, string channel, string thumbnail, string playlistID)
	{
		this.name = name;
		this.description = desc;
		this.channel = channel;
		this.thumbnailURL = thumbnail;
		this.playlistID = playlistID;
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
	HISTORY,
	SEARCH
}


public class Youtube : MonoBehaviour
{
	public YoutubeScrollHandler scrollHandler;

	[SerializeField] Manager manager;
	[SerializeField] Transform ytPrefabParent;
	[SerializeField] InputField searchField;
	[SerializeField] YoutubeVideoObjectPrefab[] ytPrefabs;
	[SerializeField] Text currentSearchModeDisplay;
	[SerializeField] string apiKey;
	[SerializeField] string regionCode;

	SearchMode searchMode = SearchMode.VIDEOS;
	Focus focus = Focus.TRENDING;
	List<string> history = new List<string>();
	int searchResults;


	IEnumerator ParseJson(string json)
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
			string videoID = video["id"]["videoId"].ToString();
			WWW www = new WWW("https://www.googleapis.com/youtube/v3/videos?part=statistics%2C+contentDetails&id=" + videoID + "&key=" + apiKey);
			yield return www;
			JObject info = JObject.Parse(www.text);
			JToken videoInfo = info["items"].Children().ToList()[0];
			float duration = (float) XmlConvert.ToTimeSpan(videoInfo["contentDetails"]["duration"].ToString()).TotalSeconds;
			JToken stats = videoInfo["statistics"];
			int views = int.Parse(stats["viewCount"].ToString());
			int likes = int.Parse(stats["likeCount"].ToString());
			int dislikes = int.Parse(stats["dislikeCount"].ToString());
			int comments = int.Parse(stats["commentCount"].ToString());


			string thumbnailUrl = data["thumbnails"]["default"]["url"].ToString();
			videos.Add(new YoutubeVideoObject(title, channel, desc, videoID, thumbnailUrl, views, duration, likes, dislikes, comments));
		}

		yield return videos;
	}


	IList<YoutubePlaylistObject> ParseJsonPlaylist(string json)
	{
		IList<YoutubePlaylistObject> pls = new List<YoutubePlaylistObject>();

		JObject response = JObject.Parse(json);
		List<JToken> playlists = response["items"].Children().ToList();

		foreach(JToken pl in playlists)
		{
			JToken snippet = pl["snippet"];
			string title = snippet["title"].ToString();
			string desc = snippet["description"].ToString();
			string thumbnail = snippet["thumbnails"]["default"]["url"].ToString();
			string channel = snippet["channelTitle"].ToString();
			string id = pl["id"]["playlistId"].ToString();

			pls.Add(new YoutubePlaylistObject(title, desc, channel, thumbnail, id));
		}

		return pls;
	}


	IEnumerator GetVideo(string videoID)
	{
		WWW www = new WWW("https://www.googleapis.com/youtube/v3/videos?part=statistics%2C+contentDetails%2C+snippet&id=" + videoID + "&key=" + apiKey);
		yield return www;
		JObject info = JObject.Parse(www.text);
		JToken videoInfo = info["items"].Children().ToList()[0];

		JToken data = videoInfo["snippet"];
		string title = data["title"].ToString();
		string channel = data["channelTitle"].ToString();
		string desc = data["description"].ToString();
		float duration = (float) XmlConvert.ToTimeSpan(videoInfo["contentDetails"]["duration"].ToString()).TotalSeconds;
		JToken stats = videoInfo["statistics"];
		int views = int.Parse(stats["viewCount"].ToString());
		int likes = int.Parse(stats["likeCount"].ToString());
		int dislikes = int.Parse(stats["dislikeCount"].ToString());
		int comments = int.Parse(stats["commentCount"].ToString());

		string thumbnailUrl = data["thumbnails"]["default"]["url"].ToString();


		yield return new YoutubeVideoObject(title, channel, desc, videoID, thumbnailUrl, views, duration, likes, dislikes, comments);
	}


	IEnumerator DisplayVideos(IList<YoutubeVideoObject> youtubeObjects)
	{
		IList<Coroutine> coroutines = new List<Coroutine>();
		for(int i = 0; i < searchResults; i++)
		{
			if(i >= youtubeObjects.Count)
			{
				ytPrefabs[i].gameObject.SetActive(false);
			}
			else
			{
				coroutines.Add(StartCoroutine(ytPrefabs[i].Setup(this, youtubeObjects[i])));
			}
		}

		foreach(Coroutine c in coroutines)
		{
			yield return c;
		}
	}


	IEnumerator DisplayVideos(IList<YoutubePlaylistObject> youtubePlaylistObjects)
	{
		IList<Coroutine> coroutines = new List<Coroutine>();
		for(int i = 0; i < searchResults; i++)
		{
			coroutines.Add(StartCoroutine(ytPrefabs[i].Setup(this, youtubePlaylistObjects[i])));
		}

		foreach(Coroutine c in coroutines)
		{
			yield return c;
		}
	}


	IEnumerator SearchForVideos(string query)
	{
		if(searchMode == SearchMode.VIDEOS)
		{
			WWW www = new WWW("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=" + searchResults + "&order=relevance&q=" + WWW.EscapeURL(query) + "&regionCode=" + regionCode + "&type=video&videoCategoryId=10&key=" + apiKey);
			yield return www;
			CoroutineWithData cd = new CoroutineWithData(this, ParseJson(www.text));
			yield return cd.coroutine;

			yield return (IList<YoutubeVideoObject>) cd.result;
		}
		else
		{
//			                   https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=                     &order=relevance&q=                            &regionCode=                  &type=playlist&key=
			WWW www = new WWW("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=" + searchResults + "&order=relevance&q=" + WWW.EscapeURL(query) + "&regionCode=" + regionCode + "&type=playlist&key=" + apiKey);
			yield return www;

			yield return ParseJsonPlaylist(www.text);
		}
	}


	IEnumerator GetPopularMusic()
	{
		if(searchMode == SearchMode.VIDEOS)
		{
			WWW www = new WWW("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=" + searchResults + "&order=relevance&regionCode=" + regionCode + "&type=video&videoCategoryId=10&key=" + apiKey);
			yield return www;
			CoroutineWithData cd = new CoroutineWithData(this, ParseJson(www.text));
			yield return cd.coroutine;

			yield return (IList<YoutubeVideoObject>) cd.result;
		}
		else
		{
			WWW www = new WWW("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=" + searchResults + "&order=relevance&regionCode=" + regionCode + "&type=playlist&key=" + apiKey);
			yield return www;

			yield return ParseJsonPlaylist(www.text);
		}
	}


	public void PlayVideo(YoutubeVideoObject vid)
	{
		manager.VideoPlayCommand(vid);
	}


	public void PlayPlaylist(YoutubePlaylistObject pl)
	{
		manager.PlaylistPlayCommand(pl);
	}


	public IEnumerator ShowHistory()
	{
		IList<YoutubeVideoObject> videos = new List<YoutubeVideoObject>();
		foreach(string videoID in history)
		{
			CoroutineWithData cd = new CoroutineWithData(this, GetVideo(videoID));
			yield return cd.coroutine;
			videos.Add((YoutubeVideoObject) cd.result);
		}

		if(videos.Count > 0)
		{
			StartCoroutine(DisplayVideos(videos));
		}
	}


	public IEnumerator ShowTrending()
	{
		CoroutineWithData cd = new CoroutineWithData(this, GetPopularMusic());
		yield return cd.coroutine;

		if(searchMode == SearchMode.VIDEOS)
		{
			StartCoroutine(DisplayVideos((IList<YoutubeVideoObject>) cd.result));
		}
		else
		{
			StartCoroutine(DisplayVideos((IList<YoutubePlaylistObject>) cd.result));
		}
	}


	public IEnumerator SearchEnter()
	{
		string currentText = searchField.text;
		if(currentText.Trim() == "")
		{
			yield break;
		}

		CoroutineWithData cd = new CoroutineWithData(this, SearchForVideos(currentText.Trim()));
		yield return cd.coroutine;

		if(searchMode == SearchMode.VIDEOS)
		{
			StartCoroutine(DisplayVideos(cd.result as IList<YoutubeVideoObject>));
		}
		else
		{
			StartCoroutine(DisplayVideos(cd.result as IList<YoutubePlaylistObject>));
		}
	}


	public void OnEndEditSearchQuery()
	{
		focus = Focus.SEARCH;
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
		if(history.Count > 0)
		{
			StartCoroutine(ShowHistory());
		}
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
		else
		if(focus == Focus.SEARCH)
		{
			StartCoroutine(SearchEnter());
		}
	}


	public void AddVideoToHistory(string videoID)
	{
		history.Insert(0, videoID);
		if(focus == Focus.HISTORY)
		{
			StartCoroutine(ShowHistory());
		}
	}


	void Awake()
	{
		searchResults = ytPrefabs.Length;
	}


	public void Init()
	{
		StartCoroutine(ShowTrending());
	}
}
