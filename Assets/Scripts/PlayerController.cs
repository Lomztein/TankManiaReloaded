using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {

	public enum BonusType { Damage, Speed, Firerate, Health };

	public HealthScript health;
	public SpriteRenderer sprite;
	public string playerName = "Player";

	public float turnSpeed;
	public float speed;

	public GameObject newWeapon;
	public GameObject weapon;
	public Transform weaponPos;
	public WeaponScript weaponScript;

	public CharacterController cc;

	//float damageMultiplier = 1f;
	public float firerateMultiplier = 1f;
	public float speedMultiplier = 1f;
		
	public List<float> timers;
	public List<string> timerNames;
	public Texture2D weaponTex;

	public float reloadTime;
	float maxReloadTime;

	public int kills;
	public int deaths;

	public NetworkPlayer player;

	// Use this for initialization
	void Start () {
		sprite = transform.FindChild ("Sprite").GetComponent<SpriteRenderer>();
		health = GetComponent<HealthScript>();
		cc = GetComponent<CharacterController>();
		name = playerName;
	}
	
	// Update is called once per frame
	void Update () {
		if (networkView.isMine) {
			if (newWeapon) {
				networkView.RPC ("ChangeWeapon",RPCMode.All,Network.AllocateViewID(),newWeapon.GetComponent<WeaponScript>().weaponIndex);
			}
			if (timers.Count > 0) {
				for (int i=0;i<timers.Count;i++) {
					timers[i] -= Time.deltaTime;
				}
			}
			if (reloadTime < maxReloadTime) {
				reloadTime += Time.deltaTime;
			}else{
				reloadTime = maxReloadTime;
			}
		}
	}

	void FixedUpdate () {
		if (networkView.isMine) {
			transform.Rotate (0,0,-Input.GetAxis ("Horizontal") * turnSpeed * speedMultiplier * Time.fixedDeltaTime);
			cc.Move (transform.right * Input.GetAxis ("Vertical") * speed * speedMultiplier * Time.fixedDeltaTime);
			if (Input.GetButton ("Fire1")) {
				weaponScript.Fire ();
			}
			Camera.main.transform.position = transform.position + Vector3.back * 10;
		}
	}

	void AddTimer (string tName) {
		timers.Add (90);
		timerNames.Add (tName);
	}

	[RPC] void ChangeName (string newName) {
		playerName = newName;
	}

	[RPC] void GetPlayer (NetworkPlayer p) {
		player = p;
	}

	[RPC] void GetKill () {
		kills++;
	}

	[RPC] void GetDeath () {
		deaths++;
	}

	[RPC] void ChangeWeapon (NetworkViewID newID, int newW) {
		if (weapon) { Destroy (weapon); }
		weapon = (GameObject)Instantiate (GlobalManager.current.weapons[newW],weaponPos.position,weaponPos.rotation);
		weaponScript = weapon.GetComponent<WeaponScript>();
		weaponScript.parent = this;
		weaponScript.equipped = true;
		weapon.transform.parent = transform;
		weapon.networkView.viewID = newID;
		maxReloadTime = weaponScript.reloadTime;
		weaponTex = weapon.transform.FindChild ("Sprite").GetComponent<SpriteRenderer>().sprite.texture;
		newWeapon = null;

		CancelInvoke ("ResetWeapon");
		if (newW != 0) {
			Invoke ("ResetWeapon",90f);
			AddTimer (weaponScript.weaponName);
		}
	}

	void ResetWeapon () {
		networkView.RPC ("ChangeWeapon",RPCMode.All,Network.AllocateViewID (),0);
	}

	void ChangeSpeed (float factor) {
		speedMultiplier = factor;
		Invoke ("ResetSpeed",90f);
		AddTimer ("Speed Bonus");
	}

	void ResetSpeed () {
		speedMultiplier = 1f;
	}

	void ChangeFirerate (float factor) {
		firerateMultiplier = factor;
		Invoke ("ResetFirerate",90f);
		AddTimer ("Firerate Bonus");
	}

	void OnDestroy () {
		if (networkView.isMine) {
			NetworkManager.current.SpawnPlayer();
		}
	}

	void OnGUI () {
		if (networkView.isMine) {
			int f = 0;
			for (int i=0;i<timers.Count;i++) {
				if (timers[i] > 0) {
					float locValue = (timers[i]/90)*200;
					GUI.Box (new Rect (Screen.width-locValue-10,Screen.height-55-(f*55),locValue,40),timerNames[i]);
					f++;
				}
			}
			GUI.Box (new Rect(0,Screen.height-125,125,125),"");
			if (weaponScript) {
				GUI.DrawTexture (new Rect(10,Screen.height-110,100,100),weaponTex,ScaleMode.ScaleToFit,true,0);
			}
			GUI.Box (new Rect(135,Screen.height-55,reloadTime/maxReloadTime*200,40),"RELOAD");
			GUI.Box (new Rect(135,Screen.height-110,health.health/health.maxHealth*200,40),"HULL");
		}else{
			Vector3 hudPos = Camera.main.WorldToScreenPoint (transform.position);
			hudPos = new Vector3 (hudPos.x,-hudPos.y+Screen.height);
			float barLength = ((playerName + (health.health/health.maxHealth*100).ToString()).Length+4)*8.5f;
			GUI.Box (new Rect (hudPos.x-barLength/2,hudPos.y-40,barLength,20),playerName + " - " + (health.health/health.maxHealth*100).ToString() + "%");
		}
	}
}
