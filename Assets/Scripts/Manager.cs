using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;
using dia = System.Diagnostics;


public struct SongInformation
{
	public string artist;
	public string song_name;
	public string videoID;
	public int duration;
	public float progress;
	public string cover_url;
	public bool playing;
	public bool inChannel;
	public bool noEntry;
	public float volume;
}


public struct PlaylistInformation
{
	public string name;
	public string author;
	public int replays;
	public int duration;
	public int entries;


	public string beautifiedName {
		get
		{
			TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;
			return textInfo.ToTitleCase(name.Replace("_", " "));
		}
	}


	public PlaylistInformation(string name, string author, int replays, int duration, int entries)
	{
		this.name = name;
		this.author = author;
		this.replays = replays;
		this.duration = duration;
		this.entries = entries;
	}


	public override string ToString()
	{
		return string.Format("[{0} by {1}] {2} times played, {3} entries and {4} seconds long", beautifiedName, author, replays, entries, duration);
	}
}


public class Manager : MonoBehaviour
{
	[SerializeField] Youtube youtube;
	[SerializeField] MessageDisplay msgDisplay;
	[SerializeField] Transform loading_screen;
	[SerializeField] Transform main_screen;
	[SerializeField] GameObject discord_logo;
	[SerializeField] GameObject noPlayerChannel;
	[SerializeField] GameObject registerScreen;
	[SerializeField] Animator general_anim;
	[SerializeField] Image song_display;
	[SerializeField] Image progress_display;
	[SerializeField] Text tokenDisplay;
	[SerializeField] Text tokenDescription;
	[SerializeField] Text progress_text;
	[SerializeField] Text song_text;
	[Header("Control")]
	[SerializeField] Image play_button;
	[SerializeField] Image skip_button;
	[SerializeField] Text volume_value;
	[SerializeField] Slider volume_slider;
	[Space(10)]
	[SerializeField] Sprite play_pressed;
	[SerializeField] Sprite play_normal;
	[SerializeField] Sprite pause_pressed;
	[SerializeField] Sprite pause_normal;
	[Space(10)]
	[SerializeField] Sprite skip_pressed;
	[SerializeField] Sprite skip_normal;
	[Header("Covers")]
	[SerializeField] Sprite genericCover;
	[Header("Debug")]
	[SerializeField] bool connect;
	[SerializeField] bool local;
	[SerializeField] bool clearPerfs;

	List<PlaylistInformation> playlists = new List<PlaylistInformation>();
	WaitForSeconds connectionInterval = new WaitForSeconds(3);
	WaitForSeconds checkAnswerInterval = new WaitForSeconds(2);
	WaitForSeconds volumeChangeDelay = new WaitForSeconds(.6f);
	WaitForSeconds leftPadPressDelay = new WaitForSeconds(.3f);
	WaitForSeconds pingInterval = new WaitForSeconds(5);
	SongInformation songInformation = new SongInformation();
	dia.Stopwatch progressTimer = new dia.Stopwatch();
	string serverID;
	string authorID;
	bool initializingDone;


	IEnumerator Start()
	{
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		if(clearPerfs)
		{
			PlayerPrefs.DeleteAll();
		}

		if(connect)
		{
			SocketClient.localConnect = local;

			loading_screen.gameObject.SetActive(true);
			main_screen.gameObject.SetActive(false);
			noPlayerChannel.SetActive(false);
			registerScreen.SetActive(false);
			discord_logo.SetActive(true);

			initializingDone = false;

			SocketClient.Init();
			while(SocketClient.socketStatus == SocketStatus.CONNECTING)
			{
				yield return connectionInterval;
			}

			if(SocketClient.socketStatus == SocketStatus.FAILED_TO_CONNECT)
			{
				Debug.LogError("Couldn't connect");
			}

			if(PlayerPrefs.HasKey("user_id") && PlayerPrefs.HasKey("server_id"))
			{
				serverID = PlayerPrefs.GetString("server_id");
				authorID = PlayerPrefs.GetString("user_id");
			}
			else
			{
				string token = Utils.GenerateToken(6).ToUpper();
				discord_logo.SetActive(false);
				registerScreen.SetActive(true);
				tokenDisplay.text = token;
				tokenDescription.text = String.Format(tokenDescription.text, token);
				yield return StartCoroutine(RequestIdentity(token));
				PlayerPrefs.SetString("user_id", authorID);
				PlayerPrefs.SetString("server_id", serverID);
				PlayerPrefs.Save();

				Analytics.CustomEvent("Registered");

				registerScreen.SetActive(false);
				discord_logo.SetActive(true);
			}


			general_anim.SetTrigger("loading_done");
			yield return StartCoroutine(RequestInformation());
			yield return StartCoroutine(UpdateInterface());

			yield return new WaitForSeconds(2);
			loading_screen.gameObject.SetActive(false);
			main_screen.gameObject.SetActive(true);

			initializingDone = true;
			StartCoroutine(AliveCheck());
			Analytics.CustomEvent("Connected");
		}

		youtube.Init();
		RequestPlaylists();
	}


	void Update()
	{
		if(initializingDone)
		{
			if(SocketClient.has_new_messages)
			{
				string message = SocketClient.GetLastMessage();
				string[] elements = message.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
				if(elements[1].StartsWith("INFORMATION")) //"xx==INFORMATION;AVICII;FOR A BETTER DAY;PLAYING;https://i.scdn.co/image/1e95e13d082e43c547dadd93808dceeb99f589cf;290;352;0.15" 
				{
					string oldUrl = songInformation.cover_url;
					UpdateSongInformation(elements[1].Substring(0, int.Parse(elements[0])));
					StartCoroutine(UpdateInterface(oldUrl != songInformation.cover_url));
				}
				else
				if(elements[1].StartsWith("MESSAGE")) //xx==MESSAGE;HELLO WORLD!
				{
					try
					{
						string[] messageParts = elements[1].Substring(0, int.Parse(elements[0])).Split(new String[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
						string msg = messageParts[1];
						
						msgDisplay.DisplayMessage(msg);
					}
					catch
					{
						Debug.LogWarning("Received malformed MESSAGE");
					}
				}
			}
		}
			
		if(songInformation.progress < songInformation.duration || songInformation.duration == 0)
		{
			float realProgress = (float) songInformation.progress + (((float) progressTimer.ElapsedMilliseconds) / 1000);
			if(songInformation.duration > 0)
			{
				progress_display.fillAmount = ((float) realProgress) / ((float) songInformation.duration);
			}
			progress_text.text = string.Format("{0}:{1}", Mathf.Floor(realProgress / 60).ToString("00"), Mathf.Floor(realProgress % 60).ToString("00"));
		}

		if(Input.GetKeyDown(KeyCode.Escape) && YoutubeWindowController.open)
		{
			YoutubeWindowController.open = false;
		}
	}


	void OnApplicationQuit()
	{
		Analytics.CustomEvent("Application Quit");
		SocketClient.Shutdown();
	}


	IEnumerator AliveCheck()
	{
		bool needsReconnect = false;

		while(true)
		{
			yield return pingInterval;
			if(SocketClient.Send("ping") != SocketSendResponse.SUCCESSFUL)
			{
				needsReconnect = true;
				break;
			}
		}

		if(needsReconnect)
		{
			Analytics.CustomEvent("Reconnecting through pinging...");
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
		}
	}


	IEnumerator UpdateInterface(bool downloadCover = true)
	{
		noPlayerChannel.SetActive(!songInformation.inChannel);

		song_text.text = songInformation.artist + "\n" + songInformation.song_name;
		volume_slider.value = songInformation.volume;
		if(songInformation.playing)
		{
			play_button.sprite = pause_normal;
		}
		else
		{
			play_button.sprite = play_normal;
		}

		if(downloadCover)
		{
			if(songInformation.noEntry)
			{
				song_display.sprite = genericCover;
			}
			else
			{
				yield return StartCoroutine(DisplayCover(songInformation.cover_url));
			}
		}
	}


	IEnumerator RequestIdentity(string token)
	{
		SocketClient.Send("REQUEST;USER_IDENTIFICATION;" + token);
		while(true)
		{
			if(SocketClient.has_new_messages)
			{
				string message = SocketClient.GetLastMessage();
//				print(message);
				string[] elements = message.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
				if(elements[1].StartsWith("USERINFORMATION")) //"xx==USERINFORMATION;203304535949574154;203302899277627392" 
				{
					if(UpdateUserInformation(elements[1].Substring(0, int.Parse(elements[0]))))
					{
						break;
					}
				}

			}
			else
			{
				Debug.Log("waiting for user information!");
				yield return checkAnswerInterval;
			}
		}

		Debug.Log("MusicBot sent user-data");
	}


	IEnumerator RequestInformation()
	{
		SocketClient.Send("REQUEST;" + serverID + ";" + authorID + ";SEND_INFORMATION");
		while(true)
		{
			if(SocketClient.has_new_messages)
			{
				string message = SocketClient.GetLastMessage();
				string[] elements = message.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
				if(elements[1].StartsWith("INFORMATION")) //"xx==INFORMATION;AVICII;FOR A BETTER DAY;PLAYING;https://i.scdn.co/image/1e95e13d082e43c547dadd93808dceeb99f589cf;290;352;0.15" 
				{
					if(UpdateSongInformation(elements[1].Substring(0, int.Parse(elements[0]))))
					{
						break;
					}
				}

			}
			else
			{
				Debug.Log("MusicBot hasn't answered yet!");
				yield return checkAnswerInterval;
			}
		}

		Debug.Log("MusicBot sent data");
	}


	void RequestPlaylists()
	{
		SocketClient.Send("REQUEST;" + serverID + ";" + authorID + ";SEND_PLAYLISTS");
	}


	bool UpdateSongInformation(string msg)
	{

		string[] elements = msg.Split(new char[] { ';' });

		if(elements[1] == "PLAYLISTS")
		{
			return UpdatePlaylists(msg);
		}

		if(elements[0] == "INFORMATION")
		{
			try
			{
				songInformation.artist = elements[1];
				songInformation.song_name = elements[2];

				string oldVideoID = songInformation.videoID;
				songInformation.videoID = elements[3];

				if(oldVideoID != null && oldVideoID != songInformation.videoID)
				{
					youtube.AddVideoToHistory(oldVideoID);
				}

				songInformation.playing = elements[4] == "PLAYING";
				songInformation.inChannel = elements[4] != "UNCONNECTED";
				songInformation.noEntry = elements[4] == "STOPPED";
				songInformation.cover_url = elements[5];
				songInformation.progress = float.Parse(elements[6]);
				songInformation.duration = int.Parse(elements[7]);
				songInformation.volume = float.Parse(elements[8]);
				progressTimer.Reset();
				if(songInformation.playing)
				{
					progressTimer.Start();
				}
				else
				{
					progressTimer.Stop();
				}
				return true;
			}
			catch
			{
				Debug.LogWarning("Malformatted information code: " + msg);
				Analytics.CustomEvent("Received malformed data");
				return false;
			}
		}

		return false;
	}


	bool UpdatePlaylists(string msg)
	{
		string[] elements = msg.Split(new char[] { ';' });
		if(elements[0] == "INFORMATION" && elements[1] == "PLAYLISTS")
		{
			try
			{
				int playlistCount = int.Parse(elements[2]);

				for(int i = 0; i < playlistCount; i++)
				{
					int indexOffset = 3 + i * 5;
					string name = elements[indexOffset + 0];
					string author = elements[indexOffset + 1];
					int replayCount = int.Parse(elements[indexOffset + 2]);
					int entryCount = int.Parse(elements[indexOffset + 3]);
					int duration = int.Parse(elements[indexOffset + 4]);

					PlaylistInformation playlist = new PlaylistInformation(name, author, replayCount, duration, entryCount);
					if(!playlists.Exists(new Predicate<PlaylistInformation>(x => x.name == playlist.name)))
					{
						playlists.Add(playlist);
					}
				}
//				playlists.ForEach(new Action<PlaylistInformation>(x => print(x)));
				return true;
			}
			catch
			{
				Debug.LogWarning("Malformatted playlists code: " + msg);
				Analytics.CustomEvent("Received malformed pl data");
				return false;
			}
		}

		return false;
	}


	bool UpdateUserInformation(string msg)
	{

		string[] elements = msg.Split(new char[] { ';' });
		if(elements[0] == "USERINFORMATION")
		{
			try
			{
				serverID = elements[1];
				authorID = elements[2];
				return true;
			}
			catch
			{
				Debug.LogWarning("Malformatted userdata code: " + msg);
				Analytics.CustomEvent("Received malformed userdata");
				return false;
			}
		}

		return false;
	}


	IEnumerator DisplayCover(string url)
	{
		WWW www = new WWW(url);
		yield return www;

		try
		{
			song_display.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
		}
		catch
		{
			song_display.sprite = genericCover;
		}
	}


	public void VideoPlayCommand(YoutubeVideoObject vid)
	{
		Analytics.CustomEvent("Playing", new Dictionary<string, object>() { { "name", vid.name }, { "channel", vid.channel }, { "id", vid.videoID } });
		if(SocketClient.Send("COMMAND;" + serverID + ";" + authorID + ";PLAY;" + "https://www.youtube.com/watch?v=" + vid.videoID) == SocketSendResponse.NOT_CONNECTED)
		{
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
		}
	}


	public void YoutubePlaylistPlayCommand(YoutubePlaylistObject pl)
	{
		Analytics.CustomEvent("Playing Playlist", new Dictionary<string, object>() { { "name", pl.name }, { "channel", pl.channel }, { "id", pl.playlistID } });
		if(SocketClient.Send("COMMAND;" + serverID + ";" + authorID + ";PLAY;" + "https://www.youtube.com/playlist?list=" + pl.playlistID) == SocketSendResponse.NOT_CONNECTED)
		{
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
		}
	}


	public void RadioPlayCommand(string name)
	{
		Analytics.CustomEvent("Playing Radio", new Dictionary<string, object>() { { "name", name } });
		if(SocketClient.Send("COMMAND;" + serverID + ";" + authorID + ";RADIO;" + name) == SocketSendResponse.NOT_CONNECTED)
		{
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
		}
//		print("Sent radio command");
	}


	public void PlaylistPlayCommand(string name)
	{
		Analytics.CustomEvent("Playing Playlist", new Dictionary<string, object>() { { "name", name } });
		if(SocketClient.Send("COMMAND;" + serverID + ";" + authorID + ";PLAYLIST;" + name) == SocketSendResponse.NOT_CONNECTED)
		{
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
		}
	}


	public void RightPadPress()
	{
		Analytics.CustomEvent("Skipping", new Dictionary<string, object>() { { "song", songInformation.song_name }, { "artist", songInformation.artist } });
		if(SocketClient.Send("COMMAND;" + serverID + ";" + authorID + ";SKIP") == SocketSendResponse.NOT_CONNECTED)
		{
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
		}
	}


	public void RightPadDown()
	{
		skip_button.sprite = skip_pressed;
	}


	public void RightPadUp()
	{
		skip_button.sprite = skip_normal;
	}


	public void LeftPadPress()
	{
		if(SocketClient.Send("COMMAND;" + serverID + ";" + authorID + ";PLAY_PAUSE") == SocketSendResponse.NOT_CONNECTED)
		{
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
			return;
		}
		songInformation.playing = !songInformation.playing;
		StartCoroutine(LeftPadPressDelayed());
	}


	IEnumerator LeftPadPressDelayed()
	{
		yield return leftPadPressDelay;
		yield return UpdateInterface(false);
	}


	public void LeftPadDown()
	{
		if(songInformation.playing)
		{
			play_button.sprite = pause_pressed;
		}
		else
		{
			play_button.sprite = play_pressed;
		}
	}


	public void LeftPadUp()
	{
		if(songInformation.playing)
		{
			play_button.sprite = pause_normal;
		}
		else
		{
			play_button.sprite = play_normal;
		}
	}


	public void OnVolumeChange()
	{
		volume_value.text = String.Format("{0}%", Mathf.RoundToInt(volume_slider.value * 100));
		if(volume_slider.value != songInformation.volume)
		{
			StartCoroutine(ChangeVolume(volume_slider.value));
		}
	}


	IEnumerator ChangeVolume(float vol)
	{
		yield return volumeChangeDelay;

		if(volume_slider.value == vol)
		{
			Debug.Log("Sending new volume");
			Analytics.CustomEvent("Changing Volume", new Dictionary<string, object>() { { "old volume", songInformation.volume }, { "new volume", Math.Round(vol, 2) } });
			if(SocketClient.Send("COMMAND;" + serverID + ";" + authorID + ";VOLUMECHANGE;" + Math.Round(vol, 2).ToString()) == SocketSendResponse.NOT_CONNECTED)
			{
				general_anim.SetTrigger("switch_to_loading");
				StartCoroutine(Start());
			}
		}
	}


	public void OnSummonPress()
	{
		if(SocketClient.Send("COMMAND;" + serverID + ";" + authorID + ";SUMMON") == SocketSendResponse.NOT_CONNECTED)
		{
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
		}
	}
}
