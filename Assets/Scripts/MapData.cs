using UnityEngine;
using System.Collections;

public class MapData : MonoBehaviour {

	public Vector2 size;
	public Transform[] spawnPoints;

	// Use this for initialization
	void Start () {
		Invoke ("SendData",0.1f);
	}
	
	void SendData () {
		GlobalManager.current.mapSize = size;
		GlobalManager.current.spawnPoints = spawnPoints;
	}

	void OnDrawGizmos () {
		Gizmos.DrawWireCube (Vector3.zero,size);
		if (spawnPoints != null) {
			for (int i=0;i<spawnPoints.Length;i++) {
				Gizmos.DrawSphere (spawnPoints[i].position,0.5f);
			}
		}
	}
}
