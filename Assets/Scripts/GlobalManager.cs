using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlobalManager : MonoBehaviour {

	public enum GameMode : int {Deathmatch, GunGame, SuddenDeath, InstaGib, Berserk};

	public static GlobalManager current;
	public int particleAmount;
	public int maxParticles;
	public GameObject[] weapons;
	public Transform[] spawnPoints;
	public float weaponSpawnChance = 1;
	public Vector2 mapSize;

	public int mapIndex;
	public GameMode gameMode;
	public GameObject[] maps;
	public MapData[] mapData;
	public string[] killNouns;
	public int teams;
	public int[] teamAmount;

	public GameObject curMap;

	public PlayerController localPlayer;
	public List<PlayerController> players;

	public bool spawnWeapons = true;
	public bool spawnBonuses = false;

	public GameObject instaGibCannon;
	public GameObject playerSquare;
	public bool isPVE;

	public GUISkin skin;
	public bool debugMode;

	// Use this for initialization
	void Start () {
		current = this;
		for (int i=0;i<weapons.Length;i++) {
			weapons[i].GetComponent<WeaponScript>().weaponIndex = i;
		}
		mapData = new MapData[maps.Length];
		for (int i=0;i<maps.Length;i++) {
			mapData[i] = maps[i].GetComponent<MapData>();
		}
	}

	public void UpdateParticles () {
		particleAmount = GameObject.FindGameObjectsWithTag ("Particle").Length;
	}

	void FixedUpdate () {
		if (Network.isServer && NetworkManager.current.gameStarted && spawnWeapons) {
			SpawnWeapon ();
		}
	}

	[RPC] void LoadMap (int index, int gmIndex) {
		mapIndex = index;
		curMap = (GameObject)Instantiate (maps[mapIndex]);
		gameMode = (GameMode)gmIndex;
		if (gameMode == GameMode.GunGame || gameMode == GameMode.InstaGib) {
			spawnWeapons = false;
		}
		teamAmount = new int[Mathf.Max (1,teams)];
	}

	[RPC] void GetPlayer (NetworkViewID playerID) {
		players.Add (NetworkView.Find (playerID).GetComponent<PlayerController>());
	}

	[RPC] void ShortenPlayerList () {
		players.TrimExcess ();
	}

	void SpawnWeapon () {
		if (Network.isServer) {
			for (int i = 1;i < weapons.Length; i++) {
				if (Random.Range (0f,100f) < weaponSpawnChance/((float)weapons.Length-1f)) {
					Vector3 newPos = new Vector3 (Random.Range (-mapSize.x/2,mapSize.x/2),Random.Range (-mapSize.y/2,mapSize.y/2),0);
					if (!Physics.CheckSphere (newPos,1)) {
						Network.Instantiate (weapons[i],newPos,Quaternion.identity,0);
					}
				}
			}
		}
	}

	void OnDrawGizmos () {
		Gizmos.DrawWireCube (Vector3.zero,new Vector3 (mapSize.x,mapSize.y,1));
	}

	void OnGUI () {
		GUI.skin = skin;
		if (Network.isServer && NetworkManager.current.gameStarted == false) {
			if (mapData[mapIndex].maxPlayers < Network.connections.Length+1) {
				mapIndex = 0;
			}
			for (int i=0;i<maps.Length;i++) {
				if (i != mapIndex && mapData[i].maxPlayers >= Network.connections.Length+1) {
					if (GUI.Button (new Rect (10,70 + (i*30),100,20),mapData[i].mapName)) {
						mapIndex = i;
					}
				}else{
					GUI.Box (new Rect (10,70 + (i*30),100,20),maps[i].name);
				}
			}
			for (int i=0; i<System.Enum.GetValues(typeof(GameMode)).Length;i++) {
				if ((int)gameMode != i) {
					if (GUI.Button (new Rect(10,90+(20*maps.Length)+20+(i*30),100,20),((GameMode)i).ToString())) {
						gameMode = (GameMode)i;
					}
				}else{
					GUI.Box (new Rect(10,90+(20*maps.Length)+20+(i*30),100,20),((GameMode)i).ToString());
				}
			}
		}
		if (Input.GetButton ("Tab") && NetworkManager.current.gameStarted) {
			int[] teamIndex = new int[teams];
			int i = 0;
			int a = 0;
			do {
				string text = "";
				if (teams == 0) {
					text = "Free for all";
				}else{
					text = "Team " + (i+1).ToString();
				}
				Vector2 distance = new Vector2 ((Screen.width-100-10*(teams-1))/(Mathf.Max (teams,1)),10);
				GUI.Box (new Rect (50 + ((distance.x + distance.y) * i),50,distance.x,20),text);
				int b = 0;
				for (int j = 0;j<teamAmount[i];j++) {
					if (players[a]) {
						GUI.Box (new Rect (50 + ((distance.x + distance.y) * i), 80 + (20*b),distance.x,20),players[a].playerName + " - K/D: " + players[a].kills + " / " + players[a].deaths);
						b++;
					}
					a++;
				}
				i++;
			}while (i<teams);
		}
	}
}
