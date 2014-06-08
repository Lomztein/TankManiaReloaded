using UnityEngine;
using System.Collections;

public class InvasionGameMode : MonoBehaviour {

	public float gameProgress;
	public GameObject bot;
	public static InvasionGameMode current;
	public float spawnRadius;
	public float[] spawnFrequency;
	public float startFrequency = 200;
	public float timeBetweenTypes = 60;
	public float endFrequency = 10;
	public int weaponIndex;
	public bool[] canSpawn;
	public float gameTimeMultiplier = 1;
	public GameObject fortress;
	public bool purchaseMenuOpen;
	public int enemyTypeAmount;
	
	void Start () {
		current = this;
		spawnFrequency = new float[GlobalManager.current.weapons.Length];
		canSpawn = new bool[spawnFrequency.Length];
		for (int i=0;i<spawnFrequency.Length;i++) {
			spawnFrequency[i] = startFrequency * spawnFrequency.Length;
		}
	}
	
	void FixedUpdate () {
		if (NetworkManager.current.gameStarted && Network.isServer) {
			if (Random.Range (0,(int)spawnFrequency[weaponIndex]) == 1 && gameProgress > weaponIndex * timeBetweenTypes) {
				Vector3 newPos = Random.onUnitSphere * spawnRadius;
				newPos = new Vector3 (newPos.x,newPos.y,0);
				GameObject newBot = (GameObject)Network.Instantiate (bot,newPos,Quaternion.identity,0);
				newBot.GetComponent<InvasionBotController>().newWeaponID = weaponIndex;
			}
			if (gameProgress > weaponIndex * timeBetweenTypes) {
				if (canSpawn[weaponIndex] == false) {
					GlobalManager.current.networkView.RPC ("SendChat",RPCMode.All,"The invaders have gotten their dirty hands on " + GlobalManager.current.weapons[weaponIndex].GetComponent<WeaponScript>().weaponName + "s");
					canSpawn[weaponIndex] = true;
					enemyTypeAmount = weaponIndex;
				}
				if (spawnFrequency[weaponIndex] > endFrequency) {
					spawnFrequency[weaponIndex] -= Time.fixedDeltaTime * enemyTypeAmount;
				}else{
					spawnFrequency[weaponIndex] = endFrequency;
				}
			}
			weaponIndex++;
			if (enemyTypeAmount > 0) {
				weaponIndex = weaponIndex % enemyTypeAmount;
			}else{
				weaponIndex = 0;
			}
			gameProgress += Time.fixedDeltaTime / gameTimeMultiplier;
		}
		if (Vector3.Distance (GlobalManager.current.localPlayer.transform.position,fortress.transform.position) < 5f) {
			purchaseMenuOpen = true;
		}else{
			purchaseMenuOpen = false;
		}
	}

	void OnGUI () {
		GUI.skin = GlobalManager.current.skin;
		if (GlobalManager.current.debugMode) {
			for (int i=0;i<spawnFrequency.Length;i++) {
				GUI.Label (new Rect (Screen.width-300,20 + (i*20),300,20),GlobalManager.current.weapons[i].name + " - " + spawnFrequency[i].ToString() + " - " + canSpawn[i].ToString());
			}
		}
		if (purchaseMenuOpen) {
			Vector3 sp = Camera.main.WorldToScreenPoint (Vector3.zero+Vector3.up*3);
			sp = new Vector3(sp.x,-sp.y+Screen.height);
			for (int i = 0;i < GlobalManager.current.weapons.Length; i++) {
				if (GlobalManager.current.localPlayer.credits >= GlobalManager.current.weaponData[i].cost) {
					if (GUI.Button ( new Rect (sp.x-120, sp.y-(i*20), 240, 20),GlobalManager.current.weaponData[i].weaponName + " - COST: " + GlobalManager.current.weaponData[i].cost)) {
						GlobalManager.current.localPlayer.newWeapon = GlobalManager.current.weapons[i];
					}
				}else{
					GUI.Box ( new Rect (sp.x-120, sp.y-(i*20), 240, 20),GlobalManager.current.weaponData[i].weaponName + " - COST: " + GlobalManager.current.weaponData[i].cost);
				}
			}
		}
	}
}
