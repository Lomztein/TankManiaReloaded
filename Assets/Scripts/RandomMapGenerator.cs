using UnityEngine;
using System.Collections;

public class RandomMapGenerator : MonoBehaviour {

	public Vector2 size;
	public Transform[] spawnPoints;
	public float perlinOffset;
	public float perlinScale;
	public float tolorance;

	public GameObject block;

	public Vector3[] nearby;

	public bool carve;

	void Start () {
		if (Network.isServer) {
			networkView.RPC ("GenerateMap",RPCMode.All,(int)size.x + (Network.connections.Length*10),(int)size.y + (Network.connections.Length*10),Random.Range (0f,10000f),perlinScale,tolorance);
		}
	}

	// Use this for initialization
	[RPC] void GenerateMap (int width, int height, float seed, float scale, float tol) {
		size = new Vector2 (width,height);
		perlinOffset = seed;
		perlinScale = scale;
		tolorance = tol;
		for (int x=-(int)size.x;x<(int)size.x+1;x+=2) {
			for (int y=-(int)size.y;y<(int)size.y+1;y+=2) {
				if (GetBlock (x,y)) {
					GameObject newBlock = (GameObject)Instantiate (block,new Vector3 (x,y,0),Quaternion.identity);
					newBlock.transform.parent = transform;
				}
			}
		}

		if (carve) {
			foreach (Transform children in transform) {
				bool isOutside = false;
				for (int i=0;i<nearby.Length;i++) {
					if (!GetBlock (children.position.x + nearby[i].x,children.position.y + nearby[i].y)) {
						isOutside = true;
					}
				}
				if (isOutside == false) {
					Destroy (children.gameObject);
				}
			}
		}
		CreateSpawnPoint ();
		Invoke ("ChangeMapSize",0.1f);
	}

	void CreateSpawnPoint () {
		spawnPoints = new Transform[2];
		int index = 0;
		while (spawnPoints[index] == null) {
			Vector3 newPos = new Vector3 (Random.Range (-size.x,size.x),Random.Range (-size.y,size.y),0);
			if (Physics.CheckSphere (newPos,2f) == false) {
				spawnPoints[index] = new GameObject ("Player" + index.ToString() + "Spawn").transform;
				spawnPoints[index].position = newPos;
			}
		}
	}

	void ChangeMapSize () {
		GlobalManager.current.spawnPoints = spawnPoints;
		GlobalManager.current.mapSize = size*2;
	}

	bool GetBlock (float x, float y) {
		if (Mathf.PerlinNoise ((perlinOffset+x)/perlinScale,(perlinOffset+y)/perlinScale) > tolorance) {
			return true;
		}
		return false;
	}

	void ChangeColors (Texture2D tex, int x, int y, int width, int height, Color col) {
		for (int xx=0;xx<width;xx++) {
			for (int yy=0;yy<height;yy++) {
				tex.SetPixel (xx+x,yy+y,col);
			}
		}
	}

	/*void ChangeSprite (GameObject block) {
		Texture2D curSprite = block.transform.FindChild ("Sprite").GetComponent<SpriteRenderer>().sprite.texture;
		Sprite newSprite = new Sprite ();
		Texture2D newTex = new Texture2D (curSprite.width,curSprite.height);
		Color newColor = curSprite.GetPixel (curSprite.width/2,curSprite.height/2);
		Vector3 pos = block.transform.position;
		if (GetBlock (pos.x-2,pos.y)) {
			ChangeColors (newTex,0,2,2,curSprite.height-4,newColor);
		}else{
			Debug.Log ("There is not a block on the left of this one: ",block);
		}
		curSprite.Apply ();
		block.transform.FindChild ("Sprite").GetComponent<SpriteRenderer>().sprite = newTex;
	}*/
}
