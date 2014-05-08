using UnityEngine;
using System.Collections;

public class NetworkCharacter : MonoBehaviour {

	PlayerController player;

	public Vector3 expectedPos;
	public Vector3 expectedVel;
	public Quaternion expectedTankAngle;
	public Quaternion expectedWeaponAngle;

	float timeSinceUpdate;

	void Start () {
		player = GetComponent<PlayerController>();
		expectedPos = transform.position;
		expectedTankAngle = transform.rotation;
	}

	void Update () {

		if (networkView.isMine == false) {
			timeSinceUpdate += Time.deltaTime;
			transform.position = expectedPos + expectedVel * timeSinceUpdate;
			transform.rotation = Quaternion.Lerp (transform.rotation,expectedTankAngle,0.1f);
			if (player.weapon) {
				player.weapon.transform.rotation = Quaternion.Lerp (player.weapon.transform.rotation,expectedWeaponAngle,0.1f);
			}
		}
	}
		
	void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo info) {

		Vector3 nPos = Vector3.zero;
		Vector3 nVel = Vector3.zero;
		Quaternion nTAngle = Quaternion.identity;
		Quaternion nWAngle = Quaternion.identity;

		if (stream.isWriting) {

			nPos = transform.position;
			nVel = player.cc.velocity;
			nTAngle = transform.rotation;
			if (player.weapon) {
				nWAngle = player.weapon.transform.rotation;
			}

			stream.Serialize (ref nPos);
			stream.Serialize (ref nVel);
			stream.Serialize (ref nTAngle);
			stream.Serialize (ref nWAngle);

		}else{

			stream.Serialize (ref nPos);
			stream.Serialize (ref nVel);
			stream.Serialize (ref nTAngle);
			stream.Serialize (ref nWAngle);

			expectedPos = nPos;
			expectedVel = nVel;
			expectedTankAngle = nTAngle;
			expectedWeaponAngle = nWAngle;
			timeSinceUpdate = 0f;
		}
	}
}
