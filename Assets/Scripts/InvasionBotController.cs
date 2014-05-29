using UnityEngine;
using System.Collections;

public class InvasionBotController : MonoBehaviour {

	public PlayerController con;
	public HealthScript health;
	public Transform target;
	public CharacterController targetChar;
	public Vector3 targetPos;
	public float angleToTarget;
	public float distanceToTarget;
	public float range;
	public int newWeaponID;
	public float verticalMove;
	public float accSpeed;
	public bool leftHanded;

	// Use this for initialization
	void Start () {
		networkView.RPC ("ChangeWeapon",RPCMode.All,Network.AllocateViewID (),newWeaponID);
		con = GetComponent<PlayerController>();
		health = GetComponent<HealthScript>();
		health.maxHealth = Mathf.Max (InvasionGameMode.current.gameProgress/3,20);
		health.health = health.maxHealth;
		range = con.weaponScript.bulletSpeed;
		name = con.weaponScript.weaponName + " Tank";
		con.playerName = name;
		if (Random.Range (0,11) == 1) {
			leftHanded = true;
		}
	}

	void GetTarget () {
		Transform nearest = null;
		float distance = float.MaxValue;
		GameObject[] targets = GameObject.FindGameObjectsWithTag ("Player");
		for (int i=0; i < targets.Length ; i++) {
			float nd = Vector3.Distance (transform.position, targets[i].transform.position);
			if (nd < distance) {
				nearest = targets[i].transform;
				distance = nd;
			}
		}
		target = nearest;
		targetChar = nearest.GetComponent<CharacterController>();
	}
	
	void FixedUpdate () {
		if (Network.isServer) {
			if (!target) {
				GetTarget ();
			}

			distanceToTarget = Vector3.Distance (transform.position,targetPos);
			targetPos = target.position + ( targetChar.velocity * ( distanceToTarget/con.weaponScript.bulletSpeed) );
			con.weaponScript.targetPos = targetPos;

			if (distanceToTarget < range) {
				RaycastHit hit;
				if (Physics.Raycast (new Ray (transform.position,target.position-transform.position),out hit, (target.position-transform.position).magnitude)) {
					if (hit.collider.tag == "Player") {
						con.weaponScript.Fire ();
						Decellarate ();
					}else{
						float angleMod = 90;
						if (leftHanded) {
							angleMod = -angleMod;
						}
						angleToTarget = (Mathf.Atan2(targetPos.y-transform.position.y, targetPos.x-transform.position.x)*180 / Mathf.PI) - angleMod;
						Accelerate ();
					}
				}
			}else{
				angleToTarget = Mathf.Atan2(targetPos.y-transform.position.y, targetPos.x-transform.position.x)*180 / Mathf.PI;
				Accelerate ();
			}
			Quaternion dq = Quaternion.Euler(new Vector3(0,0,angleToTarget));
			transform.rotation = Quaternion.RotateTowards(transform.rotation,dq,con.turnSpeed*Time.deltaTime);
			con.cc.Move (transform.right * verticalMove * con.speed * con.speedMultiplier * Time.fixedDeltaTime);
		}
	}

	void Accelerate () {
		if (verticalMove < 1) {
			verticalMove += accSpeed * Time.fixedDeltaTime;
		}else{
			verticalMove = 1;
		}
	}

	void Decellarate () {
		if (verticalMove > 0) {
			verticalMove -= accSpeed * Time.fixedDeltaTime;
		}else{
			verticalMove = 0;
		}
	}

	void OnDestroy () {
		if (Network.isServer) {
			if (Random.Range (0,4) == 3) {
				Network.Instantiate (GlobalManager.current.weapons[Random.Range (0,GlobalManager.current.weapons.Length)],transform.position,Quaternion.identity,0);
			}
		}
	}
}
