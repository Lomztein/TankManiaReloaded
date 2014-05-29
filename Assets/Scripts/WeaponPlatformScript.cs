using UnityEngine;
using System.Collections;

public class WeaponPlatformScript : MonoBehaviour {

	public WeaponScript weapon;
	public Texture2D sprite;

	void Start () {
		sprite = weapon.transform.FindChild ("Sprite").GetComponent<SpriteRenderer>().sprite.texture;
	}

	void OnMouseOver () {
		WeaponShowerScript.current.displayedWeapon = this;
	}
}
