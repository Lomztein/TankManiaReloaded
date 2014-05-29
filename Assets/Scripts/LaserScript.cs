using UnityEngine;
using System.Collections;
[RequireComponent(typeof(LineRenderer))]

public class LaserScript : MonoBehaviour {
	
	BulletScript bullet;
	LineRenderer line;
	
	public float width;
	public Vector3 end;
	public float time;
	public float fadeSpeed;
	
	// Use this for initialization
	void Start () {
		fadeSpeed = 1/time;
		bullet = GetComponent<BulletScript>();
		line = GetComponent<LineRenderer>();
		Destroy(gameObject,time);
		if (Network.isServer) {
			Ray ray = new Ray(transform.position,bullet.velocity.normalized);
			RaycastHit hit;
			if (Physics.Raycast (ray,out hit,Mathf.Infinity)) {
				end = hit.point;
				if (hit.collider.tag == "Player") {
					PlayerController hitPlayer = hit.collider.GetComponent<PlayerController>(); 
					if ((hitPlayer.playerTeam == 0 || hitPlayer.playerTeam != bullet.parent.playerTeam) && hitPlayer != bullet.parent) {
						if (hit.collider.GetComponent<HealthScript>()) {
							hit.collider.networkView.RPC ("TakeDamage",RPCMode.All,bullet.damage,bullet.parent.networkView.viewID);
						}
					}
				}
			}
			networkView.RPC ("SetPoints",RPCMode.All,end);
		}
	}

	[RPC] void SetPoints (Vector3 locEnd) {
		line = GetComponent<LineRenderer>();
		line.SetWidth(width,width);
		line.SetPosition(0,transform.position);
		line.SetPosition(1,locEnd);
		Instantiate ( bullet.hitParticle, locEnd, Quaternion.identity );
	}
	
	void Update () {
		line.material.color -= new Color (0,0,0,fadeSpeed * Time.deltaTime);
	}
}