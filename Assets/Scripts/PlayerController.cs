using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {

	public enum BonusType { Damage, Speed, Firerate, Health };

	public HealthScript health;
	public SpriteRenderer sprite;
	public string playerName = "Player";
	public int playerTeam;

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
	public Transform colorSquare;

	public bool isBot;

	public int level;
	public float experience;
	public float expNeeded = 50;
	public bool upgradeMenuOpen;

	// Use this for initialization
	void Start () {
		sprite = transform.FindChild ("Sprite").GetComponent<SpriteRenderer>();
		health = GetComponent<HealthScript>();
		cc = GetComponent<CharacterController>();
		name = playerName;
		if (!isBot) {
			if (GlobalManager.current.gameMode == GlobalManager.GameMode.InstaGib) {
				speed *= 3;
				turnSpeed *= 3;
			}
			if (GlobalManager.current.gameMode == GlobalManager.GameMode.InstaGib && networkView.isMine) {
				newWeapon = GlobalManager.current.instaGibCannon;
				networkView.RPC ("SpawnInstaGibWeapon",RPCMode.All,Network.AllocateViewID ());
			}
			if (GlobalManager.current.gameMode == GlobalManager.GameMode.Berserk) {
				firerateMultiplier = 0.5f;
			}
		}
	}

	[RPC] void LevelUp () {
		level++;
		float excess = expNeeded - experience;
		experience = excess;
		expNeeded *= 1.5f;
	}
	
	// Update is called once per frame
	void Update () {
		if (networkView.isMine && !isBot) {
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

		if (Network.isServer) {
			if (experience > expNeeded) {
				networkView.RPC ("LevelUp",RPCMode.All);
			}
		}
	}

	void LateUpdate () {
		if (colorSquare) {
			colorSquare.rotation = Quaternion.identity;
			colorSquare.position = transform.position;
			colorSquare.localScale = sprite.bounds.extents/2;
		}
	}

	void FixedUpdate () {
		if (networkView.isMine && !isBot) {
			transform.Rotate (0,0,-Input.GetAxis ("Horizontal") * turnSpeed * speedMultiplier * Time.fixedDeltaTime);
			cc.Move (transform.right * Input.GetAxis ("Vertical") * speed * speedMultiplier * Time.fixedDeltaTime);
			weaponScript.targetPos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			if (Input.GetButton ("Fire1")) {
				weaponScript.Fire ();
			}
			Vector3 newPos = (transform.position + ((Camera.main.ScreenToWorldPoint (Input.mousePosition)-transform.position) / 3));
			Camera.main.transform.position = new Vector3 (newPos.x,newPos.y,-10);
		}
	}

	void AddTimer (string tName) {
		timers.Add (90);
		timerNames.Add (tName);
	}

	[RPC] void ChangeName (string newName, int newTeam) {
		playerName = newName;
		playerTeam = newTeam;
		if (playerTeam != 0) {
			GlobalManager.current.teamAmount[playerTeam-1]++;
		}else{
			GlobalManager.current.teamAmount[0]++;
		}
		if (!networkView.isMine) {
			GameObject cs = (GameObject)Instantiate (GlobalManager.current.playerSquare,transform.position,Quaternion.identity);
			colorSquare = cs.transform;
			if (GlobalManager.current.localPlayer.playerTeam == playerTeam) {
				cs.renderer.material.color = Color.green;
				if (playerTeam == 0) {
					cs.renderer.material.color = Color.clear;
				}
			}else{
				cs.renderer.material.color = Color.red;
			}
		}
	}

	[RPC] void GetPlayer (NetworkPlayer p) {
		player = p;
	}

	[RPC] void GetKill (float exp) {
		kills++;
		if (GlobalManager.current.gameMode == GlobalManager.GameMode.GunGame) {
			newWeapon = GlobalManager.current.weapons[weaponScript.weaponIndex + 1];
		}
		if (GlobalManager.current.isPVE) {
			experience += exp;
		}
	}

	[RPC] void GetDeath () {
		deaths++;
	}

	[RPC] void SpawnInstaGibWeapon (NetworkViewID newID) {
		if (weapon) { Destroy (weapon); }
		weapon = (GameObject)Instantiate (GlobalManager.current.instaGibCannon,weaponPos.position,weaponPos.rotation);
		weaponScript = weapon.GetComponent<WeaponScript>();
		weaponScript.parent = this;
		weaponScript.equipped = true;
		weapon.transform.parent = transform;
		weapon.networkView.viewID = newID;
		maxReloadTime = weaponScript.reloadTime;
		weaponTex = weapon.transform.FindChild ("Sprite").GetComponent<SpriteRenderer>().sprite.texture;
		newWeapon = null;
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
		if (newW != 0 && networkView.isMine && !isBot) {
			NetworkManager.current.networkView.RPC ("SendChat",RPCMode.All,playerName + " got their hands on a " + weaponScript.weaponName);
		}
		if (GlobalManager.current.gameMode != GlobalManager.GameMode.GunGame || GlobalManager.current.gameMode != GlobalManager.GameMode.InstaGib) {
			CancelInvoke ("ResetWeapon");
			if (newW != 0) {
				Invoke ("ResetWeapon",90f);
				AddTimer (weaponScript.weaponName);
			}
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
			if (!isBot) {
				NetworkManager.current.SpawnPlayer();
			}
		}
	}

	void OnGUI () {
		GUI.skin = GlobalManager.current.skin;
		if (networkView.isMine && !isBot) {
			int f = 0;
			for (int i=0;i<timers.Count;i++) {
				if (timers[i] > 0) {
					float locValue = (timers[i]/90)*200;
					GUI.Box (new Rect (Screen.width-locValue-10,Screen.height-55-(f*55),locValue,40),timerNames[i]);
					f++;
				}
			}
			GUI.Box (new Rect(0,Screen.height-125,125,125),"",GUI.skin.customStyles[1]);
			if (weaponScript) {
				GUI.DrawTexture (new Rect(10,Screen.height-110,100,100),weaponTex,ScaleMode.ScaleToFit,true,0);
			}
			GUI.Box (new Rect(135,Screen.height-55,reloadTime/maxReloadTime*200,40),"RELOAD");
			GUI.Box (new Rect(135,Screen.height-110,health.health/100*200,40),"HULL");
		}else{
			Vector3 hudPos = Camera.main.WorldToScreenPoint (transform.position);
			hudPos = new Vector3 (hudPos.x,-hudPos.y+Screen.height);
			float barLength = ((playerName + (health.health/health.maxHealth*100).ToString()).Length+4)*8.5f;
			GUI.Box (new Rect (hudPos.x-barLength/2,hudPos.y-40,barLength,20),playerName + " - " + (health.health/health.maxHealth*100).ToString() + "%");
		}
	}
}
