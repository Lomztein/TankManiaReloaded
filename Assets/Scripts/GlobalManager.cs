using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlobalManager : MonoBehaviour {

	public static GlobalManager current;
	public int particleAmount;
	public int maxParticles;
	public GameObject[] weapons;
	public Transform[] spawnPoints;
	public float weaponSpawnChance = 1;
	public Vector2 mapSize;

	public int mapIndex;
	public GameObject[] maps;
	public string[] killNouns;

	public GameObject curMap;

	public PlayerController localPlayer;
	public List<PlayerController> players;

	// Use this for initialization
	void Start () {
		current = this;
		for (int i=0;i<weapons.Length;i++) {
			weapons[i].GetComponent<WeaponScript>().weaponIndex = i;
		}
	}

	public void UpdateParticles () {
		particleAmount = GameObject.FindGameObjectsWithTag ("Particle").Length;
	}

	void FixedUpdate () {
		if (Network.isServer && NetworkManager.current.gameStarted) {
			SpawnWeapon ();
		}
	}

	[RPC] void LoadMap (int index) {
		curMap = (GameObject)Instantiate (maps[index]);
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
		if (Network.isServer && NetworkManager.current.gameStarted == false) {
			for (int i=0;i<maps.Length;i++) {
				if (i != mapIndex) {
					if (GUI.Button (new Rect (10,70 + (i*30),100,20),maps[i].name)) {
						mapIndex = i;
					}
				}else{
					GUI.Box (new Rect (10,70 + (i*30),100,20),maps[i].name);
				}
			}
		}
		if (Input.GetButton ("Tab") && NetworkManager.current.gameStarted) {
			int n = 0;
			for (int i=0;i<players.Count;i++) {
				if (players[i]) {
					n++;
					string text = players[i].playerName + " - K/D: " + players[i].kills + " / " + players[i].deaths;
					if (players[i] == localPlayer) {
						text = "You -- " + text;
					}
					GUI.Box (new Rect (50,50 + (n*20), Screen.width-100,20),text);
					if (Network.isServer && players[i].player != Network.player) {
						if (GUI.Button (new Rect (Screen.width-150,50 + (n*20), 50, 20),"Kick")) {
							Network.CloseConnection (players[i].player,true);
						}
					}
				}
			}
		}
	}
}
