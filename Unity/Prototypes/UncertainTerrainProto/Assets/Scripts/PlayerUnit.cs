using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class PlayerUnit : NetworkBehaviour {

	public int posX = 1;
	public int posY = 1;

	public int moveRadius = 3;
	public GridManager gridManager;
	//public TurretScript turret;

	public bool ability1Active;
	public bool ability2Active;
	public bool ability3Active;
	public bool ability4Active;

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
		if (Input.GetMouseButtonDown (0)) {
			if (ability1Active) {
				Ability1Tile ();
			}

			if (ability2Active) {

			}

			if (ability3Active) {
				
			}

			if (ability4Active) {

			}
		}
	}

	public void Ability1Button(){
		List<GameObject> tile = new List<GameObject> ();
		tile.Add (gridManager.tiles [posX * gridManager.gridSizeX + posY]);
		gridManager.attackTiles.Add (tile[0]);
		gridManager.showAttackRadius (1, 5, tile);
		ability1Active = true;
	}

	void Ability1Tile(){
		//Tile click: Spawn Turret on location
		gridManager.ShowAbilities(false);
		gridManager.attackTiles.Clear ();
		ability1Active = false;
	}
	public 	void Ability2(){
		//Button click: Show moveRadius
		//Tile click: move turret to tile
	}

	public void Ability3(){
		//Button click: raycast from turret to targeted location, if hit deal damage
	}

	public void Ability4(){
		//button click: deal damage, check if push is possible
	}

	IEnumerator CharacterSetup(){
		yield return new WaitForSeconds (0.5f);
		RaycastHit hit;
		Physics.Raycast (transform.position, Vector3.down, out hit, 2.5f);
		posX = hit.collider.gameObject.GetComponent<TileScript> ().posX;
		posY = hit.collider.gameObject.GetComponent<TileScript> ().posY;
		hit.collider.gameObject.GetComponent<TileScript> ().hasUnit = true;
		currentHealth = maxHealth;
		currentEnergy = maxEnergy;
		gridManager = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GridManager>();
	}
}