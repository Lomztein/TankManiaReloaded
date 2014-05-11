using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {
		
	public Rect bs;
	public float buttonDistance;
	public string gameType = "TankMania II";
	public int maxConnections = 1;
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
	public string currentMessage;
	public List<string> chat;
	public List<float> chatTime;
	public int maxChatLength;
	public float maxChatTime;

	void Start () {
		NetworkManager.current = this;
		localName = PlayerPrefs.GetString ("PlayerName","Player" + Random.Range (0,9999));
	}
	
	void StartServer () {
		Network.InitializeServer(maxConnections,8008,!Network.HavePublicAddress());
		MasterServer.RegisterHost(gameType,"SERVER_" + Random.Range (0,1000).ToString ());
	}
	
	void RefreshHostList () {
		hostData = new HostData[0];
		MasterServer.RequestHostList(gameType);
		refreshing = true;
	}

	[RPC] void RequestPlayerData (NetworkMessageInfo info) {
		for (int i=0;i<names.Count;i++) {
			networkView.RPC ("GetPlayerData",info.sender,names[i]);
		}
	}

	[RPC] void GetPlayerData (string newName) {
		names.Add (newName);
	}
	
	void Update () {
		if (refreshing) {
			if (MasterServer.PollHostList().Length != 0) {
				refreshing = false;
				hostData = MasterServer.PollHostList ();
			}
		}
		if (Input.GetButtonDown ("Enter")) {
			if (chatOpen) {
				if (currentMessage != "") {
					networkView.RPC ("SendChat",RPCMode.All,"[" + localName + "] - " + currentMessage);
				}
				currentMessage = "";
				chatOpen = false;
			}else{
				chatOpen = true;
			}
		}
		if (chatOpen) {
			string input = Input.inputString;
			foreach (char c in input) {
				if (c == "\b"[0]) {
					if (currentMessage.Length != 0) {
						currentMessage = currentMessage.Substring (0,currentMessage.Length-1);
					}
				}else{
					currentMessage += c.ToString();
				}
			}
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
		networkView.RPC ("RequestPlayerData",RPCMode.Server);
		networkView.RPC ("AddPlayer",RPCMode.All,localName);
		PlayerPrefs.SetString ("PlayerName",localName);
		PlayerPrefs.Save();
	}

	void OnFailedToConnect (NetworkConnectionError error) {
		status = "Connection failed: " + error.ToString ();
	}

	[RPC] void AddPlayer (string newName, NetworkMessageInfo info) {
		names.Add (newName);
	}

	[RPC] void RemovePlayer (int index) {
		names.RemoveAt (index);
	}

	[RPC] void GetIndex (int index) {
		localID = index;
	}

	public void SpawnPlayer () {
		Vector3 newPos = Vector3.zero;
		if (GlobalManager.current.mapIndex != 0) {
			if (Network.isServer) {
				newPos = GlobalManager.current.spawnPoints[0].position;
			}else{
				newPos = GlobalManager.current.spawnPoints[1].position;
			}
		}else{
			newPos = GlobalManager.current.spawnPoints[0].position;
		}
		GameObject newP = (GameObject)Network.Instantiate (player,newPos,Quaternion.identity,0);
		newP.networkView.RPC ("ChangeName",RPCMode.All,localName);
		newP.networkView.RPC ("GetPlayer",RPCMode.All,Network.player);
		GlobalManager.current.networkView.RPC ("GetPlayer",RPCMode.All,newP.networkView.viewID);
		GlobalManager.current.localPlayer = newP.GetComponent<PlayerController>();
	}

	[RPC] void StartGame () {
		gameStarted = true;
		Invoke ("SpawnPlayer",0.5f);
	}

	[RPC] void SendPlayerData (int index, string newName) {
		names.Add (newName);
	}

	[RPC] void SendChat (string newChat) {
		chat.Add (newChat);
		chatTime.Add (maxChatTime);
		if (chat.Count > maxChatLength) {
			chat.RemoveAt (0);
		}
	}

	void OnDisconnectedFromServer () {
		Destroy(GlobalManager.current.curMap);
		foreach (PlayerController p in GlobalManager.current.players) {
			Destroy (p.gameObject);
		}
		Camera.main.transform.position = Vector3.back * 10;
	}

	void OnPlayerDisconnected (NetworkPlayer p) {
		Network.RemoveRPCs (p);
		Network.DestroyPlayerObjects(p);
		networkView.RPC ("ShortenPlayerList",RPCMode.All);																															
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
				if (GUI.Button (new Rect (buttonDistance,Screen.height-bs.height-buttonDistance,bs.width,bs.height),"QUIT")) {
					Network.Disconnect ();
				}
			}
			if (chatOpen) {
				currentMessage = GUI.TextField (new Rect(0,Screen.height-20,Screen.width,20),currentMessage);
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