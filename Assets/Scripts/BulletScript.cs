using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour {

	public Vector3 velocity;
	public float damage;
	public GameObject hitParticle;
	public PlayerController parent;
	public SpriteRenderer sprite;
	public bool doNativeDamage = true;
	public bool modifySize;
	public bool piercing;
	public bool homing;
	public float turnSpeed;
	public float maxTime = 20;
	public Transform target;

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
		if (Network.isServer) {
			if (Physics.Raycast (ray, out hit, 1.6f + velocity.magnitude * Time.fixedDeltaTime)) {
				if (hitParticle) { networkView.RPC ("Hit",RPCMode.All, hit.point); }
				PlayerController hitPlayer = hit.collider.GetComponent<PlayerController>();
				if ((hitPlayer.playerTeam == 0 || hitPlayer.playerTeam != parent.playerTeam) && hitPlayer != parent) {
					if (hit.collider.GetComponent<HealthScript>()) {
						hit.collider.networkView.RPC ("TakeDamage",RPCMode.All,damage,parent.networkView.viewID);
					}
					if (!piercing) {
						Network.Destroy (gameObject);
					}
				}
			}
		}
	}

	void FixedUpdate () {
		Ray ray = new Ray (transform.position,velocity);
		if (homing) {
			if (target) {
				ray = new Ray (transform.position,transform.right);
				float angle = Mathf.Atan2(target.position.y-transform.position.y, target.position.x-transform.position.x)*180 / Mathf.PI;
				Quaternion newDir = Quaternion.Euler(0f,0f,angle);
				transform.rotation = Quaternion.RotateTowards (transform.rotation,newDir,turnSpeed * Time.fixedDeltaTime);
				transform.position += transform.right * velocity.magnitude * Time.fixedDeltaTime;
			}else{
				transform.position += velocity * Time.fixedDeltaTime;
			}
		}else{
			transform.position += velocity * Time.fixedDeltaTime;
		}
		if (Network.isServer) {
			maxTime -= Time.fixedDeltaTime;
			if (maxTime < 0) { Network.Destroy (gameObject); }
			RaycastHit hit;
			if (doNativeDamage) {
				if (Physics.Raycast (ray, out hit, velocity.magnitude * Time.fixedDeltaTime)) {
					if (hitParticle) { networkView.RPC ("Hit",RPCMode.All, hit.point); }
					if (hit.collider.tag == "Player" || hit.collider.tag == "Bot") {
						PlayerController hitPlayer = hit.collider.GetComponent<PlayerController>(); 
						if ((hitPlayer.playerTeam == 0 || hitPlayer.playerTeam != parent.playerTeam) && hitPlayer != parent) {
							if (hit.collider.GetComponent<HealthScript>()) {
								hit.collider.networkView.RPC ("TakeDamage",RPCMode.All,damage,parent.networkView.viewID);
							}
						}
					}
					if (!piercing) {
						Network.Destroy (gameObject);
					}else{
						damage -= damage*Time.fixedDeltaTime*20;
					}
				}
			}
		}
	}

	//BOOM BOOM BOOM BOOM
	//I'M GONNA FUCK A BROOM

	[RPC] void Hit (Vector3 position) {
		Instantiate (hitParticle,position+Vector3.back*2,transform.rotation);
	}

	void OnDrawGizmos () {
		Gizmos.DrawRay (new Ray (transform.position,velocity * Time.fixedDeltaTime));
	}
}
