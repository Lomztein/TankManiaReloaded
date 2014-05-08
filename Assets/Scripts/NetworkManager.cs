using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {
	
	public Rect bs;
	public float buttonDistance;
	public string gameType = "TankMania II";
	public bool refreshing = false;
	public HostData[] hostData;
	public GameObject player;
	public bool gameStarted;
	public static NetworkManager current;
	public string status = "On Standby";
	public int statusPos = 0;

	public string localName = "Player";
	public int localID;
	public List<string> names;

	public bool chatOpen;
	public List<string> chat;
	public List<float> chatTime;
	public int maxChatLength;
	public float maxChatTime;

	void Start () {
		NetworkManager.current = this;
		localName = "Player" + Random.Range (0,9999);
	}
	
	void StartServer () {
		Network.InitializeServer(2,8008,!Network.HavePublicAddress());
		MasterServer.RegisterHost(gameType,"SERVER_" + Random.Range (0,1000).ToString ());
	}
	
	void RefreshHostList () {
		hostData = new HostData[0];
		MasterServer.RequestHostList(gameType);
		refreshing = true;
	}
	
	void Update () {
		if (refreshing) {
			if (MasterServer.PollHostList().Length != 0) {
				refreshing = false;
				hostData = MasterServer.PollHostList ();
			}
		}
		if (Input.GetButtonDown ("Jump")) {
			networkView.RPC ("SendChat",RPCMode.All,localName,"Hi! My name is " + localName);
		}
		for (int i=0;i<chat.Count;i++) {
			chatTime[i] -= Time.deltaTime;
			if (chatTime[i] <= 0) {
				chat.RemoveAt (0);
				chatTime.RemoveAt (i);
			}
		}
	}
	
	void OnServerInitialized () {
		status = ("Server initialized!");
		networkView.RPC ("AddPlayer",RPCMode.All,localName);
	}
	
	void OnMasterServerEvent (MasterServerEvent mse) {
		status = ("Master Server event: " + mse.ToString ());
	}
	
	void OnConnectedToServer () {
		status = ("Connected to server!");
		networkView.RPC ("AddPlayer",RPCMode.AllBuffered,localName);
	}

	void OnFailedToConnect (NetworkConnectionError error) {
		status = "Connection failed: " + error.ToString ();
	}

	[RPC] void AddPlayer (string newName, NetworkMessageInfo info) {
		names.Add (newName);
		networkView.RPC ("GetIndex",info.sender,names.Count-1);
	}

	[RPC] void RemovePlayer (int index) {
		names.RemoveAt (index);
	}

	[RPC] void GetIndex (int index) {
		localID = index;
	}

	public void SpawnPlayer () {
		Vector3 newPos = Vector3.zero;
		if (Network.isServer) {
			newPos = GlobalManager.current.spawnPoints[0].position;
		}else{
			newPos = GlobalManager.current.spawnPoints[1].position;
		}
		GameObject newP = (GameObject)Network.Instantiate (player,newPos,Quaternion.identity,0);
		newP.networkView.RPC ("ChangeName",RPCMode.All,localName);
	}

	[RPC] void StartGame () {
		SpawnPlayer ();
	}

	[RPC] void SendPlayerData (int index, string newName) {
		names.Add (newName);
	}

	[RPC] void SendChat (string playerName, string newChat) {
		chat.Add ("[" + playerName + "] - " + newChat);
		chatTime.Add (maxChatTime);
		if (chat.Count > maxChatLength) {
			chat.RemoveAt (0);
		}
	}
	void OnGUI () {
		if (gameStarted == false) {
			GUI.Box (new Rect(bs.x + buttonDistance + bs.width,bs.y + ( buttonDistance + bs.height/2 ) * statusPos+1,Screen.width - bs.width - buttonDistance - bs.x*2,bs.height/2),status);
		}
		if (!Network.isServer && !Network.isClient) {
			if (GUI.Button (new Rect(bs.x,bs.y,bs.width,bs.height),"START SERVER")) {
				StartServer ();
			}
			if (GUI.Button (new Rect(bs.x,bs.y + buttonDistance + bs.height ,bs.width,bs.height),"REFRESH")) {
				RefreshHostList ();
			}
			if (GUI.Button (new Rect(bs.x,bs.y + (buttonDistance + bs.height)*2 ,bs.width,bs.height),"OFFLINE")) {
				Network.InitializeServer (0,8008,!Network.HavePublicAddress ());
			}
			if (GUI.Button (new Rect(bs.x,bs.y + (buttonDistance + bs.height)*3 ,bs.width,bs.height),"QUIT")) {
				Application.Quit ();
			}
			localName = GUI.TextField (new Rect (bs.x,bs.y + (buttonDistance + bs.height) * 4 , bs.width, 20),localName);
			if (hostData != null) {
				statusPos = hostData.Length;
				for (int i=0;i<hostData.Length;i++) {
					string text = hostData[i].gameName + " -- " + hostData[i].connectedPlayers + " / " + hostData[i].playerLimit + " PLAYERS";
					if (hostData[i].passwordProtected) {
						text = text + " - PASSWORD PROTECTED";
					}
					if (GUI.Button (new Rect(bs.x + buttonDistance + bs.width,bs.y + ( buttonDistance + bs.height/2 ) * i,Screen.width - bs.width - buttonDistance - bs.x*2,bs.height/2),text)) {
						status = "Connecting to " + hostData[i].ip[0];
						Network.Connect (hostData [i]);
					}
				}
			}
		}else{
			if (gameStarted == false) {
				for (int i=0;i<names.Count;i++) {
					GUI.Box (new Rect (bs.width + buttonDistance*2,bs.y + (i*(bs.height/2 + buttonDistance)),Screen.width-buttonDistance*3-bs.width,bs.height/2),names[i]);
					statusPos = names.Count;
				}
			}
		}
		for (int i=0;i<chat.Count;i++) {
			GUI.Label (new Rect (10,10 + (i*20),Screen.width-10,20),chat[i]);
		}
		if (Network.isServer && gameStarted == false) {
			if (Network.connections.Length != 0) {
				if (GUI.Button (new Rect(bs.x,bs.y,bs.width,bs.height),"START GAME")) {
					networkView.RPC ("StartGame",RPCMode.All);
					MasterServer.UnregisterHost ();
					GlobalManager.current.networkView.RPC ("LoadMap",RPCMode.All,GlobalManager.current.mapIndex);
					gameStarted = true;
				}
			}else{
				if (GUI.Button (new Rect(bs.x,bs.y,bs.width,bs.height),"WAITING\nSTOP")) {
					Network.RemoveRPCsInGroup (0);
					Network.Disconnect ();
					gameStarted = false;
				}
			}
		}
	}
}