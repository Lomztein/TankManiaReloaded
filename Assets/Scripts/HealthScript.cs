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
				if (debris) { Instantiate (debris,transform.position,Quaternion.identity); }
				if (returnToStart) {
					transform.position = startingPos;
					health = maxHealth;
				}else{
					Destroy (gameObject);
				}
				if (networkView.isMine) {
					if (lastHit && displayDeathMessage) {
						networkView.RPC ("GetDeath",RPCMode.All);
						lastHit.networkView.RPC ("GetKill",RPCMode.All);
						NetworkManager.current.networkView.RPC ("SendChat",RPCMode.All,parent.playerName + " was " + GlobalManager.current.killNouns[Random.Range (0,GlobalManager.current.killNouns.Length)] + " by " + lastHit.playerName + " using a " + lastHit.weaponScript.weaponName);
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
