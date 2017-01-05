using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class PlayerUnit : MonoBehaviour {

	public int posX = 1;
	public int posY = 1;

	public int moveRadius = 3;
	public GridManager gridManager;
	public GameObject turret;

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
				Ability2Tile ();
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
		Ray ray;
		RaycastHit hit;
		ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		Physics.Raycast (ray, out hit);
		if (hit.collider != null) {
			if(hit.collider.gameObject.tag.Equals("Tile")){
				if (gridManager.attackTiles.Contains (hit.collider.gameObject)) {
					GameObject tempTurret = GameObject.Instantiate (Resources.Load ("Turret") as GameObject);
					tempTurret.transform.position = new Vector3 (hit.collider.gameObject.transform.position.x, hit.collider.gameObject.transform.position.y + 2.5f, hit.collider.gameObject.transform.position.z);
					turret = tempTurret;
					//NetworkServer.Spawn (turret); //Networking - Disabled for now
				}
			}
		}
		gridManager.ShowAbilities(false);
		gridManager.attackTiles.Clear ();
		ability1Active = false;
	}
	public 	void Ability2Button(){
		//Button click: Show moveRadius
		List<GameObject> tile = new List<GameObject> ();
		tile.Add (gridManager.tiles [turret.GetComponent<TurretScript>().posX * gridManager.gridSizeX + turret.GetComponent<TurretScript>().posY]);
		gridManager.attackTiles.Add (tile[0]);
		gridManager.showAttackRadius (1, 5, tile);
		ability2Active = true;
	}

	public void Ability2Tile(){
		//Tile click: move turret to tile
		Ray ray;
		RaycastHit hit;
		ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		Physics.Raycast (ray, out hit);
		if (hit.collider != null) {
			if(hit.collider.gameObject.tag.Equals("Tile")){
				if (gridManager.attackTiles.Contains (hit.collider.gameObject)) {
					turret.transform.position = new Vector3 (hit.collider.gameObject.transform.position.x, hit.collider.gameObject.transform.position.y + 2.5f, hit.collider.gameObject.transform.position.z);
					turret.GetComponent<TurretScript> ().updateGridPosition ();
					//NetworkServer.Spawn (turret); //Networking - Disabled for now
				}
			}
		}
		gridManager.ShowAbilities(false);
		gridManager.attackTiles.Clear ();
		ability1Active = false;
	}

	public void Ability3(GameObject otherPlayer){
		//Button click: raycast from turret to targeted location, if hit deal damage
		RaycastHit hit;
		Vector3 temp = transform.position - otherPlayer.transform.position;
		temp.Normalize ();
		Physics.Raycast (transform.position,temp,out hit, 5.0f);
		bool playerHit = false;
		bool turretHit = false;
		if (hit.collider != null) {
			if (hit.collider.gameObject.tag == "Player") {
				playerHit = true;
			}
		}
		if (turret) {
			turretHit = turret.GetComponent<TurretScript> ().checkEnemy (otherPlayer);
		}
		if (playerHit) {
			otherPlayer.GetComponent<PlayerUnit> ().currentHealth -= 60;
			if (turretHit) {
				otherPlayer.GetComponent<PlayerUnit> ().currentHealth -= 60;
			}
			return;
		}
		if (turretHit) {
			otherPlayer.GetComponent<PlayerUnit> ().currentHealth -= 60;
			if (playerHit) {
				otherPlayer.GetComponent<PlayerUnit> ().currentHealth -= 60;
			}
			return;
		}
	}

	public void Ability4(GameObject otherPlayer){
		//button click: deal damage, check if push is possible
		RaycastHit hit;
		Physics.Raycast (transform.position, Vector3.forward, out hit, 5.1f);
		if (hit.collider != null) {
			if (hit.collider.gameObject.tag == "Player") {
				List<GameObject> path;
				path.Add(gridManager.tiles[otherPlayer.GetComponent<PlayerUnit>().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit>().posY]);
				path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY - 5]);
				gridManager.StartCoroutine (gridManager.MoveOverSeconds (otherPlayer, 3f, path));
			}
		}
		Physics.Raycast (transform.position, Vector3.left, out hit, 5.1f);
		if (hit.collider != null) {
			if (hit.collider.gameObject.tag == "Player") {
				List<GameObject> path;
				path.Add(gridManager.tiles[otherPlayer.GetComponent<PlayerUnit>().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit>().posY]);
				path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX - 5 * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY + 5]);
				gridManager.StartCoroutine (gridManager.MoveOverSeconds (otherPlayer, 3f, path));
			}
		}
		Physics.Raycast (transform.position, Vector3.right, out hit, 5.1f);
		if (hit.collider != null) {
			if (hit.collider.gameObject.tag == "Player") {
				List<GameObject> path;
				path.Add(gridManager.tiles[otherPlayer.GetComponent<PlayerUnit>().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit>().posY]);
				path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX + 5 * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY + 5]);
				gridManager.StartCoroutine (gridManager.MoveOverSeconds (otherPlayer, 3f, path));
			}
		}
		Physics.Raycast (transform.position, Vector3.back, out hit, 5.1f);
		if (hit.collider != null) {
			if (hit.collider.gameObject.tag == "Player") {
				List<GameObject> path;
				path.Add(gridManager.tiles[otherPlayer.GetComponent<PlayerUnit>().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit>().posY]);
				path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY + 5]);
				gridManager.StartCoroutine (gridManager.MoveOverSeconds (otherPlayer, 3f, path));
			}
		}

		if (turret) {
			Physics.Raycast (turret.transform.position, Vector3.forward, out hit, 5.1f);
			if (hit.collider != null) {
				if (hit.collider.gameObject.tag == "Player") {
					List<GameObject> path;
					path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY]);
					path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY - 5]);
					gridManager.StartCoroutine (gridManager.MoveOverSeconds (otherPlayer, 3f, path));
				}
			}
			Physics.Raycast (turret.transform.position, Vector3.left, out hit, 5.1f);
			if (hit.collider != null) {
				if (hit.collider.gameObject.tag == "Player") {
					List<GameObject> path;
					path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY]);
					path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX - 5 * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY + 5]);
					gridManager.StartCoroutine (gridManager.MoveOverSeconds (otherPlayer, 3f, path));
				}
			}
			Physics.Raycast (turret.transform.position, Vector3.right, out hit, 5.1f);
			if (hit.collider != null) {
				if (hit.collider.gameObject.tag == "Player") {
					List<GameObject> path;
					path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY]);
					path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX + 5 * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY + 5]);
					gridManager.StartCoroutine (gridManager.MoveOverSeconds (otherPlayer, 3f, path));
				}
			}
			Physics.Raycast (turret.transform.position, Vector3.back, out hit, 5.1f);
			if (hit.collider != null) {
				if (hit.collider.gameObject.tag == "Player") {
					List<GameObject> path;
					path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY]);
					path.Add (gridManager.tiles [otherPlayer.GetComponent<PlayerUnit> ().posX * gridManager.gridSizeX + otherPlayer.GetComponent<PlayerUnit> ().posY + 5]);
					gridManager.StartCoroutine (gridManager.MoveOverSeconds (otherPlayer, 3f, path));
				}
			}
		}
	}

	public IEnumerator CharacterSetup(){
		yield return new WaitForSeconds (0.5f);
		RaycastHit hit;
		Physics.Raycast (transform.position, Vector3.down, out hit, 2.5f);
		Debug.Log (hit.collider.gameObject.tag);
		posX = hit.collider.gameObject.GetComponent<TileScript> ().posX;
		posY = hit.collider.gameObject.GetComponent<TileScript> ().posY;
		hit.collider.gameObject.GetComponent<TileScript> ().hasUnit = true;
		currentHealth = maxHealth;
		currentEnergy = maxEnergy;
		gridManager = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GridManager>();
	}
}