using UnityEngine;
using System.Collections;

public class ParticleScript : MonoBehaviour {
	void Start () {
		GlobalManager.current.UpdateParticles ();
		if (GlobalManager.current.particleAmount > GlobalManager.current.maxParticles) {
			Destroy(gameObject);
		}
	}

	void OnDestroy () {
		GlobalManager.current.UpdateParticles ();
	}
}
