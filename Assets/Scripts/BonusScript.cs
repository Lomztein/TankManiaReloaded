using UnityEngine;
using System.Collections;

public class BonusScript : MonoBehaviour {

	public enum Type {Speed,Firerate,Damage,Health};
	public Type type;

	void OnTriggerEnter (Collider other) {
		if (other.networkView.isMine) {
			if (type == Type.Speed) {
				other.SendMessage ("ChangeSpeed",3f,SendMessageOptions.DontRequireReceiver);
			}
			if (type == Type.Firerate) {
				other.SendMessage ("ChangeFirerate",0.5f,SendMessageOptions.DontRequireReceiver);
			}
			if (type == Type.Damage) {
				other.SendMessage ("ChangeDamage",0.5f,SendMessageOptions.DontRequireReceiver);
			}
			if (type == Type.Health) {
				other.SendMessage ("ChangeHealth",0.5f,SendMessageOptions.DontRequireReceiver);
			}
			networkView.RPC ("ActiveOnNetwork",RPCMode.All,false);
		}
	}

	[RPC] void ActiveOnNetwork (bool isActive) {
		gameObject.SetActive (isActive);
		if (Network.isServer) {
			Invoke ("Activate",90f);
		}
	}

	void Activate () {
		networkView.RPC ("ActiveOnNetwork",RPCMode.All,true);
	}
}
