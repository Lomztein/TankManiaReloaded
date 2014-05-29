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
	
	void Start () {
		current = this;
		spawnFrequency = new float[GlobalManager.current.weapons.Length];
		canSpawn = new bool[spawnFrequency.Length];
		for (int i=0;i<spawnFrequency.Length;i++) {
			spawnFrequency[i] = startFrequency;
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
				}
				if (spawnFrequency[weaponIndex] > endFrequency) {
					spawnFrequency[weaponIndex] -= Time.fixedDeltaTime * spawnFrequency.Length;
				}else{
					spawnFrequency[weaponIndex] = endFrequency;
				}
			}
			weaponIndex++;
			weaponIndex = weaponIndex % spawnFrequency.Length;
			gameProgress += Time.fixedDeltaTime / gameTimeMultiplier;
		}
	}

	void OnGUI () {
		if (GlobalManager.current.debugMode) {
			for (int i=0;i<spawnFrequency.Length;i++) {
				GUI.Label (new Rect (Screen.width-300,20 + (i*20),300,20),GlobalManager.current.weapons[i].name + " - " + spawnFrequency[i].ToString() + " - " + canSpawn[i].ToString());
			}
		}
	}
}
