using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	//ADD A PEANUT BUTTER POWERUP AT SOME POINT IN DEVELOPMENT

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
	//float firerateMultiplier = 1f;
	float speedMultiplier = 1f;
		
	public float[] timers;
	public string[] timerNames;
	public Texture2D weaponTex;

	public float reloadTime;
	float maxReloadTime;

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
			if (timers.Length > 0) {
				for (int i=0;i<timers.Length;i++) {
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
			if (Input.GetAxis ("Vertical") > 0) {
				transform.Rotate (0,0,-Input.GetAxis ("Horizontal") * turnSpeed * speedMultiplier * Time.fixedDeltaTime);
			}else{
				transform.Rotate (0,0,-Input.GetAxis ("Horizontal") * turnSpeed * speedMultiplier * Time.fixedDeltaTime);
			}
			cc.Move (transform.right * Input.GetAxis ("Vertical") * speed * speedMultiplier * Time.fixedDeltaTime);
			if (Input.GetButton ("Fire1")) {
				weaponScript.Fire ();
			}
			Camera.main.transform.position = transform.position + Vector3.back * 10;
		}
	}

	[RPC] void ChangeName (string newName) {
		playerName = newName;
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
			timers[0] = 90;
		}
	}

	void ResetWeapon () {
		networkView.RPC ("ChangeWeapon",RPCMode.All,Network.AllocateViewID (),0);
	}

	void ChangeSpeed (float factor) {
		speedMultiplier = factor;
		Invoke ("ResetSpeed",90f);
		timers[1] = 90;
	}

	void ResetSpeed () {
		speedMultiplier = 1f;
	}

	void OnDestroy () {
		if (networkView.isMine) {
			NetworkManager.current.SpawnPlayer();
		}
	}

	void OnGUI () {
		if (networkView.isMine) {
			int f = 0;
			for (int i=0;i<timers.Length;i++) {
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
		}
		Vector3 hudPos = Camera.main.WorldToScreenPoint (transform.position);
		float barLength = playerName.Length*8.5f;
		GUI.Box (new Rect (hudPos.x-barLength/2,hudPos.y-40,barLength,20),playerName);
	}
}
