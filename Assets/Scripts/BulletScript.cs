using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour {

	public Vector3 velocity;
	public float damage;
	public GameObject hitParticle;
	public PlayerController parent;
	public SpriteRenderer sprite;

	public bool modifySize;

	void Start () {
		Transform spriteT = transform.FindChild ("Sprite");
		if (spriteT) {
			sprite = spriteT.GetComponent<SpriteRenderer>();
		}
		if (sprite) {
			if (modifySize) {
				sprite.transform.localScale = new Vector3 (0.45f,velocity.magnitude * 2 * Time.fixedDeltaTime,1);
				sprite.transform.localPosition += new Vector3(sprite.bounds.extents.x,0,0);
			}
		}
		Ray ray = new Ray (parent.weaponPos.position,velocity);
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, 0.8f + velocity.magnitude * Time.fixedDeltaTime)) {
			if (hitParticle) { networkView.RPC ("Hit",RPCMode.All, hit.point); }
			if (hit.collider.GetComponent<HealthScript>()) {
				if (hit.collider.networkView) { hit.collider.networkView.RPC ("TakeDamage",RPCMode.All,damage); }
			}
			Network.Destroy (gameObject);
		}
	}

	void FixedUpdate () {

		transform.position += velocity * Time.fixedDeltaTime;
		if (Network.isServer) {
			Ray ray = new Ray (transform.position,velocity);
			RaycastHit hit;
			if (Physics.Raycast (ray, out hit, velocity.magnitude * Time.fixedDeltaTime)) {
				if (hitParticle) { networkView.RPC ("Hit",RPCMode.All, hit.point); }
				if (hit.collider.GetComponent<HealthScript>()) {
					if (hit.collider.networkView) { hit.collider.networkView.RPC ("TakeDamage",RPCMode.All,damage); }
				}
				Network.Destroy (gameObject);
			}
		}
	}

	[RPC] void Hit (Vector3 position) {
		Instantiate (hitParticle,position+Vector3.back*2,transform.rotation);
	}

	void OnDrawGizmos () {
		Gizmos.DrawRay (new Ray (transform.position,velocity * Time.fixedDeltaTime));
	}
}
