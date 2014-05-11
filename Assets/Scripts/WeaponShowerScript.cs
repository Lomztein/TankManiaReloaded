using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponShowerScript : MonoBehaviour {

	public GameObject manager;
	public GlobalManager weaponStash;
	public GameObject platform;
	public Vector3 startingPos;
	public int columnLength;

	public List<GameObject> platforms;
	public static WeaponShowerScript current;
	public WeaponPlatformScript displayedWeapon;

	// Use this for initialization
	void Start () {
		CreatePlatforms ();
		WeaponShowerScript.current = this;
	}

	void CreatePlatforms () {
		weaponStash = manager.GetComponent<GlobalManager>();
		startingPos = new Vector3 (-(Camera.main.orthographicSize*Camera.main.aspect)+2,Camera.main.orthographicSize-2);
		int index = 0;
		int columnIndex = 0;
		int rowIndex = 0;
		foreach (GameObject wep in weaponStash.weapons) {
			GameObject nPlatform = (GameObject)Instantiate(platform,new Vector3 (startingPos.x+columnIndex*3,startingPos.y-rowIndex*3,startingPos.z),Quaternion.identity);
			nPlatform.transform.FindChild ("WeaponSprite").GetComponent<SpriteRenderer>().sprite = wep.transform.FindChild ("Sprite").GetComponent<SpriteRenderer>().sprite;
			nPlatform.GetComponent<WeaponPlatformScript>().weapon = wep.GetComponent<WeaponScript>();
			platforms.Add (nPlatform);
			index++;
			rowIndex++;
			if (rowIndex > columnLength) {
				columnIndex++;
				rowIndex = 0;
			}
		}
		//Invoke ("AllignWeapons",0.1f);
	}

	void AllignWeapons () {
		foreach (GameObject plat in platforms) {
			plat.transform.FindChild ("WeaponSprite").localPosition -= plat.transform.FindChild ("WeaponSprite").GetComponent<SpriteRenderer>().bounds.center;
		}
	}

	void OnGUI () {
		if (displayedWeapon) {
			WeaponScript weapon = displayedWeapon.weapon;
			int multiplier = 1;
			string info = "Weapon number " + weapon.weaponIndex + " - " + weapon.weaponName +
				"\nDamage: " + weapon.bulletDamage;
			if (weapon.bulletAmount > 1) {
				multiplier *= weapon.bulletAmount;
				info = info + " * " + weapon.bulletAmount;
			}
			if (weapon.fireInSequence) {
				multiplier *= (weapon.transform.childCount-1);
				info = info + " * " + (weapon.transform.childCount-1);
			}

					info = info + "\nFirerate: " + 1/weapon.reloadTime + 
					"\nDPS: " + (weapon.bulletDamage*multiplier) * (1/weapon.reloadTime) + 
					"\nTurn speed: " + weapon.turnSpeed;
			GUI.Label (new Rect (Screen.width-400,0,300,Screen.height),info);
		}
	}
}
