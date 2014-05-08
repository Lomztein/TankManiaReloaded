using UnityEngine;
using System.Collections;

public class DestroyAfterTimeScript : MonoBehaviour {

	public float life = 10;

	// Use this for initialization
	void Start () {
		Destroy (gameObject,life);
	}
}
