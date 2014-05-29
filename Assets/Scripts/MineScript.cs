using UnityEngine;
using System.Collections;

public class MineScript : MonoBehaviour {

	public BulletScript bullet;
	public float range;
	public float fadeTime;

	void OnTriggerEnter (Collider other) {
		if (Network.isServer) {
			if (other.tag == "Player") {
				if (other.GetComponent<PlayerController>() != bullet.parent) {
					if (bullet.hitParticle) {
						networkView.RPC ("Hit",RPCMode.All,transform.position);
					}
					other.networkView.RPC ("TakeDamage",RPCMode.All,bullet.damage,bullet.parent.networkView.viewID);
					Network.Destroy (gameObject);
				}
			}
		}
	}
}