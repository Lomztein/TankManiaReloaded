using UnityEngine;
using System.Collections;

public class HealthScript : MonoBehaviour {

	public float health;
	public float maxHealth;
	public float armor;
	public float maxArmor;
	public float regenSpeed;
	public float maxRegen;

	public bool invincible;
	public bool returnToStart;
	public GameObject debris;

	public Vector3 startingPos;

	public PlayerController parent;
	public PlayerController lastHit;
	public bool displayDeathMessage = true;

	// Use this for initialization
	void Start () {
		parent = GetComponent<PlayerController>();
		startingPos = transform.position;
		if (health == 0) {
			health = maxHealth;
		}

		if (GlobalManager.current.gameMode == GlobalManager.GameMode.SuddenDeath) {
			maxHealth = 10;
			health = 10;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (health <= maxRegen) {
			health += regenSpeed * Time.deltaTime;
		}
		if (invincible) {
			health = maxHealth;
		}else{
			if (health <= 0) {
				if (returnToStart) {
					transform.position = startingPos;
					health = maxHealth;
				}
				if (debris) { Instantiate (debris,transform.position,Quaternion.identity); }
				if (Network.isServer) {
					if (!returnToStart) {
						Network.Destroy (gameObject);
					}
					networkView.RPC ("GetDeath",RPCMode.All);
					if (lastHit) {
						lastHit.networkView.RPC ("GetKill",RPCMode.All,maxHealth + maxArmor);
					}
					if (lastHit && displayDeathMessage) {
						if (lastHit.weaponScript.killNoun != "") {
							NetworkManager.current.networkView.RPC ("SendChat",RPCMode.All,parent.playerName + " was " + lastHit.weaponScript.killNoun + " by " + lastHit.playerName + " using a " + lastHit.weaponScript.weaponName);
						}else{
							NetworkManager.current.networkView.RPC ("SendChat",RPCMode.All,parent.playerName + " was " + GlobalManager.current.killNouns[Random.Range (0,GlobalManager.current.killNouns.Length)] + " by " + lastHit.playerName + " using a " + lastHit.weaponScript.weaponName);
						}
					}
				}
			}
		}
	}

	[RPC] void TakeDamage (float damage, NetworkViewID shooterID) {
		lastHit = NetworkView.Find (shooterID).GetComponent<PlayerController>();
		if (armor > 0) {
			armor -= damage;
		}else{
			health -= damage;
		}
	}
}
