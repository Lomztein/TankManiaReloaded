using UnityEngine;
using System.Collections;

public class GranadeScript : MonoBehaviour {

	public BulletScript bullet;
	public int fragments;
	public float range;
	public bool isArmed;

	// Use this for initialization
	void Start () {
		if (Network.isServer) {
			bullet = GetComponent<BulletScript>();
			Invoke ("Arm",0.2f);
		}
	}

	void Arm () {
		isArmed = true;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (Network.isServer) {
			if (isArmed) {
				if (Physics.CheckSphere (transform.position,range/1.5f)) {
					networkView.RPC ("Hit",RPCMode.All, transform.position);
					Network.Destroy (gameObject);
				}
			}
		}
	}

	void OnDestroy () {
		if (Network.isServer) {
			for (int i=0;i<fragments;i++) {
				Vector3 randomDir = Random.insideUnitCircle * range * 1.5f;
				Ray newRay = new Ray (transform.position,randomDir);
				RaycastHit hit;
				if (Physics.Raycast (newRay,out hit,range)) {
					if (hit.collider.tag == "Player") {
						PlayerController hitPlayer = hit.collider.GetComponent<PlayerController>(); 
						if ((hitPlayer.playerTeam == 0 || hitPlayer.playerTeam != bullet.parent.playerTeam) && hitPlayer != bullet.parent) {
							hit.collider.networkView.RPC ("TakeDamage",RPCMode.All,bullet.damage,bullet.parent.networkView.viewID);
						}
					}
				}
			}
		}
	}
}
