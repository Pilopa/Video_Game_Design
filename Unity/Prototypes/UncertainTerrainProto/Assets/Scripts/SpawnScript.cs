using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnScript : MonoBehaviour {

	public GameObject Player1Spawn;
	public GameObject Player2Spawn;

	public GameObject Player1;
	public GameObject Player2;

	// Use this for initialization
	void Start () {
		GameObject.Instantiate (Player1,Player1Spawn.transform.position, Quaternion.Euler(-90,0,0));
		GameObject.Instantiate (Player2,Player2Spawn.transform.position, Quaternion.Euler(-90,0,0));

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
