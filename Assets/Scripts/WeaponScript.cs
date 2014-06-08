using UnityEngine;
using System.Collections;

public class WeaponScript : MonoBehaviour {

	public SpriteRenderer sprite;
	public int weaponIndex;
	public string weaponName;
	public string killNoun;
	public float turnSpeed = 10;
	public PlayerController parent;

	public Vector3 targetPos;
	public Transform[] muzzles;
	public GameObject bulletType;
	public float bulletSpeed;
	public float bulletDamage;
	public float bulletInaccuracy;
	public int bulletAmount;
	public int muzzleIndex;
	public GameObject fireParticle;
	public float reloadTime;
	public bool reloaded;

	public bool equipped;

	public bool fireInSequence;
	public float sequenceTime;

	public bool findTarget;
	public float homingSphereSize;
	public Transform target;
	public GameObject targetSprite;

	public int cost;

	// Use this for initialization
	void Start () {
		if (findTarget) {
			if (equipped) {
				if (parent.networkView.isMine) {
					targetSprite = (GameObject)Instantiate (targetSprite,transform.position,Quaternion.identity);
					targetSprite.transform.parent = transform;
					targetSprite.renderer.material.color = Color.clear;
				}
			}
		}
		sprite = transform.FindChild ("Sprite").GetComponent<SpriteRenderer>();
		sprite.transform.position += Vector3.back;
		if (!equipped) {
			Quaternion newRot = Quaternion.Euler (0,0,Random.Range (0f,360f));
			networkView.observed = null;
			networkView.stateSynchronization = NetworkStateSynchronization.Off;
			transform.rotation = newRot;
		}else{
			Destroy (collider);
		}
		if (GlobalManager.current.gameMode == GlobalManager.GameMode.InstaGib) {
			turnSpeed *= 3;
		}
	}

	void Update () {
		if (transform.parent) {
			if (transform.parent.networkView.isMine) {
				Vector3 mousePos = targetPos;
				float directionToMouse = Mathf.Atan2(mousePos.y-transform.position.y, mousePos.x-transform.position.x)*180 / Mathf.PI;
				Quaternion dq = Quaternion.Euler(new Vector3(0,0,directionToMouse));
				transform.rotation = Quaternion.RotateTowards(transform.rotation,dq,turnSpeed*Time.deltaTime);
			}
		}
	}

	void FixedUpdate () {
		if (findTarget) {
			if (equipped) {
				if (parent.networkView.isMine) {
					RaycastHit hit;
					if (Physics.SphereCast (new Ray(transform.position,transform.right),homingSphereSize,out hit,Mathf.Infinity)) {
						if (hit.collider.tag == "Player") {
							target = hit.collider.transform;
							targetSprite.transform.position = target.position + Vector3.back;
							targetSprite.renderer.material.color = Color.white;
						}else{
							target = null;
							targetSprite.renderer.material.color = Color.clear;
						}
					}
				}
			}
		}
	}

	public void Fire () {
		if (reloaded) {
			NetworkFire (muzzleIndex);
			Invoke ("Reload",reloadTime * parent.firerateMultiplier);
			reloaded = false;
			muzzleIndex++;
			muzzleIndex = muzzleIndex % muzzles.Length;
			parent.reloadTime = 0;
		}
	}

	void Reload () {
		reloaded = true;
	}

	void OnTriggerEnter (Collider other) {
		if (other.tag == "Player" && other.networkView.isMine) {
			WeaponScript ow = other.GetComponent<PlayerController>().weaponScript;
			if (ow.weaponIndex == 0) {
				other.GetComponent<PlayerController>().newWeapon = GlobalManager.current.weapons[weaponIndex];
				Network.Destroy (gameObject);
			}
		}
	}

	void FireSequence () {
		NetworkFire (muzzleIndex);
		muzzleIndex++;
		muzzleIndex = muzzleIndex % muzzles.Length;
	}

	[RPC] void FeedBulletData (NetworkViewID id, Vector3 velocity) {
		BulletScript bullet = NetworkView.Find (id).GetComponent<BulletScript>();
		bullet.velocity = velocity;
		bullet.damage = bulletDamage;
		bullet.parent = parent;
	}

	[RPC] void FeedBulletTarget (NetworkViewID id, NetworkViewID targetID) {
		BulletScript bullet = NetworkView.Find (id).GetComponent<BulletScript>();
		bullet.target = NetworkView.Find (targetID).transform;
	}

	[RPC] void NetworkFire (int index) {
		if (fireParticle) { networkView.RPC ("CreateFireParticle",RPCMode.All, index); }
		for (int i=0;i<bulletAmount;i++) {
			GameObject newBullet = (GameObject)Network.Instantiate(bulletType,muzzles[index].position,muzzles[index].rotation,0);
			networkView.RPC ("FeedBulletData",RPCMode.All,newBullet.networkView.viewID,(muzzles[index].right * Random.Range (0.9f,1.1f)) * bulletSpeed + muzzles[index].up * Random.Range (-bulletInaccuracy,bulletInaccuracy));
			if (target) {
				if (target.networkView) {
					networkView.RPC ("FeedBulletTarget",RPCMode.All,newBullet.networkView.viewID,target.networkView.viewID);
				}
			}
		}
		if (fireInSequence) {
			if (index < muzzles.Length-1) {
				Invoke ("FireSequence",sequenceTime);
			}
		}
	}

	[RPC] void CreateFireParticle (int index) {
		Instantiate (fireParticle,muzzles[index].position,muzzles[index].rotation);
	}
}
