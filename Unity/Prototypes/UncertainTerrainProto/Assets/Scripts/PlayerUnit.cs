using UnityEngine;
using System.Collections;

public class PlayerUnit : MonoBehaviour {

	public int posX = 1;
	public int posY = 1;

	public int moveRadius = 3;


	// Use this for initialization
	void Start () {
		RaycastHit hit;
		Physics.Raycast (transform.position, Vector3.down, out hit);
		posX = hit.collider.gameObject.GetComponent<TileScript> ().posX;
		posY = hit.collider.gameObject.GetComponent<TileScript> ().posY;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
