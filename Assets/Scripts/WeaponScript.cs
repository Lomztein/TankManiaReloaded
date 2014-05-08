using UnityEngine;
using System.Collections;

public class WeaponScript : MonoBehaviour {

	public SpriteRenderer sprite;
	public int weaponIndex;
	public float turnSpeed = 10;
	public PlayerController parent;

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

	// Use this for initialization
	void Start () {
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
	}

	void Update () {
		if (transform.parent) {
			if (transform.parent.networkView.isMine) {
				Vector3 mousePos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
				float directionToMouse = Mathf.Atan2(mousePos.y-transform.position.y, mousePos.x-transform.position.x)*180 / Mathf.PI;
				Quaternion dq = Quaternion.Euler(new Vector3(0,0,directionToMouse));
				transform.rotation = Quaternion.RotateTowards(transform.rotation,dq,turnSpeed*Time.deltaTime);
			}
		}
	}

	public void Fire () {
		if (reloaded) {
			NetworkFire (muzzleIndex);
			Invoke ("Reload",reloadTime);
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
			other.GetComponent<PlayerController>().newWeapon = GlobalManager.current.weapons[weaponIndex];
			Network.Destroy (gameObject);
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

	[RPC] void NetworkFire (int index) {
		if (fireParticle) { networkView.RPC ("CreateFireParticle",RPCMode.All, index); }
		for (int i=0;i<bulletAmount;i++) {
			GameObject newBullet = (GameObject)Network.Instantiate(bulletType,muzzles[index].position,muzzles[index].rotation,0);
			networkView.RPC ("FeedBulletData",RPCMode.All,newBullet.networkView.viewID,(muzzles[index].right * Random.Range (0.9f,1.1f)) * bulletSpeed + muzzles[index].up * Random.Range (-bulletInaccuracy,bulletInaccuracy));
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
