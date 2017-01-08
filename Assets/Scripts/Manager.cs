﻿using System.Collections;
using System.Collections.Generic;
using System;
using dia = System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Analytics;


public struct SongInformation
{
	public string artist;
	public string song_name;
	public int duration;
	public float progress;
	public string cover_url;
	public bool playing;
	public bool inChannel;
	public bool noEntry;
	public float volume;
}



public class Manager : MonoBehaviour
{
	[SerializeField] Transform loading_screen;
	[SerializeField] Transform main_screen;
	[SerializeField] GameObject discord_logo;
	[SerializeField] GameObject noPlayerChannel;
	[SerializeField] Animator general_anim;
	[SerializeField] Image song_display;
	[SerializeField] Image progress_display;
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
	[SerializeField] string serverId;
	[SerializeField] bool connect;

	WaitForSeconds connectionInterval = new WaitForSeconds(3);
	WaitForSeconds checkAnswerInterval = new WaitForSeconds(2);
	WaitForSeconds volumeChangeDelay = new WaitForSeconds(.6f);
	WaitForSeconds leftPadPressDelay = new WaitForSeconds(.3f);
	WaitForSeconds pingInterval = new WaitForSeconds(10);
	SongInformation songInformation = new SongInformation();
	dia.Stopwatch progressTimer = new dia.Stopwatch();
	bool initializingDone;


	IEnumerator Start()
	{
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		if(connect)
		{
			loading_screen.gameObject.SetActive(true);
			main_screen.gameObject.SetActive(false);
			noPlayerChannel.SetActive(false);
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
			}
		}
			
		if(songInformation.progress < songInformation.duration)
		{
			float realProgress = (float) songInformation.progress + (((float) progressTimer.ElapsedMilliseconds) / 1000);
			if(songInformation.duration > 0)
			{
				progress_display.fillAmount = ((float) realProgress) / ((float) songInformation.duration);
			}
			progress_text.text = string.Format("{0}:{1}", Mathf.Floor(realProgress / 60).ToString("00"), Mathf.Floor(realProgress % 60).ToString("00"));
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


	IEnumerator RequestInformation()
	{
		SocketClient.Send("REQUEST;" + serverId + ";SEND_INFORMATION");
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


	bool UpdateSongInformation(string msg)
	{

		string[] elements = msg.Split(new char[] { ';' });
		if(elements[0] == "INFORMATION")
		{
			try
			{
				songInformation.artist = elements[1];
				songInformation.song_name = elements[2];
				songInformation.playing = elements[3] == "PLAYING";
				songInformation.inChannel = elements[3] != "UNCONNECTED";
				songInformation.noEntry = elements[3] == "STOPPED";
				songInformation.cover_url = elements[4];
				songInformation.progress = float.Parse(elements[5]);
				songInformation.duration = int.Parse(elements[6]);
				songInformation.volume = float.Parse(elements[7]);
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


	public void RightPadPress()
	{
		Analytics.CustomEvent("Skipping", new Dictionary<string, object>() { { "song", songInformation.song_name }, { "artist", songInformation.artist } });
		if(SocketClient.Send("COMMAND;" + serverId + ";SKIP") == SocketSendResponse.NOT_CONNECTED)
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
		if(SocketClient.Send("COMMAND;" + serverId + ";PLAY_PAUSE") == SocketSendResponse.NOT_CONNECTED)
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
			Analytics.CustomEvent("Changing Volume", new Dictionary<string, object>() { { "old volume", songInformation.volume }, { "new volume", vol } });
			if(SocketClient.Send("COMMAND;" + serverId + ";VOLUMECHANGE;" + Math.Round(vol, 2).ToString()) == SocketSendResponse.NOT_CONNECTED)
			{
				general_anim.SetTrigger("switch_to_loading");
				StartCoroutine(Start());
			}
		}
	}


	public void OnSummonPress()
	{
		if(SocketClient.Send("COMMAND;" + serverId + ";SUMMON") == SocketSendResponse.NOT_CONNECTED)
		{
			general_anim.SetTrigger("switch_to_loading");
			StartCoroutine(Start());
		}
	}
}
