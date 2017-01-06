using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TurretScript : MonoBehaviour {

	public int damage;

	public int posX;
	public int posY;
	public int posZ;

	public int attackrange = 5;

	// Use this for initialization
	void Start () {
		RaycastHit hit;

		Physics.Raycast (transform.position, Vector3.down, out hit, 1.5f);
		if (hit.collider.gameObject.tag == "Tile") {
			posX = hit.collider.gameObject.GetComponent<TileScript> ().posX;
			posY = hit.collider.gameObject.GetComponent<TileScript> ().posY;
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void updateGridPosition(){
		RaycastHit hit;

		Physics.Raycast (transform.position, Vector3.down, out hit, 1.5f);
		if (hit.collider.gameObject.tag == "Tile") {
			posX = hit.collider.gameObject.GetComponent<TileScript> ().posX;
			posY = hit.collider.gameObject.GetComponent<TileScript> ().posY;
		}
	}

	public bool checkEnemy(GameObject enemy){
		RaycastHit hit;
		Vector3 dir =  transform.position - enemy.transform.position;
		dir.Normalize ();
		Physics.Raycast (transform.position,dir,out hit ,attackrange);
		if (hit.collider != null) {
			if (hit.collider.gameObject.tag == "Player") {
				return true;
			}
		} else {
			return false;
		}
		return false;
	}
}
