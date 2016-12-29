using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PlayerUnit : NetworkBehaviour {

	public int posX = 1;
	public int posY = 1;

	public int moveRadius = 3;

	//Health
	public int maxHealth;
	public int currentHealth;
	public int healthRegPerRound;

	//Energy
	public int maxEnergy;
	public int currentEnergy;
	public int energyRegPerRound;

	public int energyCostPerTile;

	//AbilityCost
	public int ability1Cost;
	public int ability2Cost;
	public int ability3Cost;
	public int ability4Cost;

	// Use this for initialization
	void Start () {
		StartCoroutine (CharacterSetup());
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void Ability1(){
		//Button click: Show spawn radius
		//Tile click: Spawn Turret on location
	}

	void Ability2(){
		//Button click: Show moveRadius
		//Tile click: move turret to tile
	}

	void Ability3(){
		//Button click: raycast from turret to targeted location, if hit deal damage
	}

	void Ability4(){
		//button click: deal damage, check if push is possible
	}

	IEnumerator CharacterSetup(){
		yield return new WaitForSeconds (0.5f);
		RaycastHit hit;
		Physics.Raycast (transform.position, Vector3.down, out hit, 2.5f);
		posX = hit.collider.gameObject.GetComponent<TileScript> ().posX;
		posY = hit.collider.gameObject.GetComponent<TileScript> ().posY;
		currentHealth = maxHealth;
		currentEnergy = maxEnergy;
	}
}