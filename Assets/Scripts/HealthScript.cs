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

	// Use this for initialization
	void Start () {
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
			}
		}
	}

	[RPC] void TakeDamage (float damage) {
		if (armor > 0) {
			armor -= damage;
		}else{
			health -= damage;
		}
	}
}
