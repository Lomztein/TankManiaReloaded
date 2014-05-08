using UnityEngine;
using System.Collections;

public class PeanutButter : MonoBehaviour {

	void OnTriggerEnter (Collider other) {
		if (other.networkView.isMine) {
			other.SendMessage ("ChangeSpeed",3f,SendMessageOptions.DontRequireReceiver);
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
