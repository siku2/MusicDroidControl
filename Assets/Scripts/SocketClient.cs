using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using System.Threading;
using UnityEngine;


public enum SocketStatus
{
	FAILED_TO_CONNECT,
	CONNECTED,
	CONNECTING
}


public enum SocketSendResponse
{
	SUCCESSFUL,
	SEND_FAILED,
	NOT_CONNECTED,
	STILL_CONNECTING,
}



public static class SocketClient
{
	public static bool localConnect;
	public static List<string> received_messages = new List<string>();
	public static bool has_new_messages;
	public static SocketStatus socketStatus;

	static Socket sending_socket;
	static Thread receive_thread;
	static bool initialized;
	static bool forceStop;
	static bool initializing;


	public static void Init()
	{
		initializing = true;
		sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		IPAddress send_to_address = IPAddress.Parse((localConnect) ? "127.0.0.1" : "35.165.215.126");
		IPEndPoint sending_end_point = new IPEndPoint(send_to_address, 5005);

		socketStatus = SocketStatus.CONNECTING;
		sending_socket.BeginConnect(sending_end_point, new AsyncCallback(ConnectCallback), sending_socket);
	}


	public static void ConnectCallback(IAsyncResult ar)
	{
		try
		{
			Socket client = (Socket) ar.AsyncState;

			client.EndConnect(ar);
			receive_thread = new Thread(new ThreadStart(Receiver));
			receive_thread.Start();

			initializing = false;
			initialized = true;
			socketStatus = SocketStatus.CONNECTED;

		}
		catch/*(Exception e)*/
		{
			if(!forceStop)
			{
				Debug.LogWarning("Couldn't connect to the musicbot... retrying!");
				Init();
			}
			//socketStatus = SocketStatus.FAILED_TO_CONNECT;
			//Debug.LogError(e.ToString());
		}
	}


	public static void Receiver()
	{
		byte[] bytes = new byte[1024];
		while(!forceStop && sending_socket.Connected)
		{
			sending_socket.Receive(bytes);
			string msg = Encoding.UTF8.GetString(bytes);
//			Debug.Log(msg);
			if(msg == "sdown" || msg == "exit" || msg == "")
			{
				Debug.LogWarning("MusicBot ShutDown!");
				Shutdown();
			}
//			Debug.Log("Received message: " + msg);
			received_messages.Add(msg);
			has_new_messages = true;
		}
	}


	public static void Shutdown()
	{
		if(sending_socket.Connected)
		{
			Send("sdown");
			sending_socket.Shutdown(SocketShutdown.Both);
		}
		forceStop = true;
		if(initialized)
		{
			sending_socket.Close();
		}
	}


	public static SocketSendResponse Send(string msg)
	{
		if(!initialized || !sending_socket.Connected)
		{
			if(initializing)
			{
				return SocketSendResponse.STILL_CONNECTING;
			}

			return SocketSendResponse.NOT_CONNECTED;
		}

		byte[] send_buffer = Encoding.UTF8.GetBytes(msg);

		try
		{
			sending_socket.Send(send_buffer);
			return SocketSendResponse.SUCCESSFUL;
		}
		catch(Exception send_exception)
		{
			Console.WriteLine("Exception {0}", send_exception.Message);
			return SocketSendResponse.SEND_FAILED;
		}
	}


	public static string GetLastMessage(bool also_delete = true)
	{
		string msg = received_messages[received_messages.Count - 1];
		if(also_delete)
		{
			received_messages.RemoveAt(received_messages.Count - 1);
		}
		has_new_messages = false;
		return msg;
	}
}
