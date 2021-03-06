﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{

	public GameObject tile;
	public GameObject grid;

	GameObject selectedUnit;

	public int gridSizeX;
	public int gridSizeY;

	public bool movingUnit = false;
	public bool moveRadiusShowing = false;
	public bool attackRadiusShowing = false;

	Ray ray;
	RaycastHit hit;

	//Lists
	public List<GameObject> tiles = new List<GameObject> ();
	public List<GameObject> moveTiles = new List<GameObject> ();
	public List<GameObject> attackTiles = new List<GameObject> ();
	public List<GameObject> notAttackTiles = new List<GameObject> ();

	//Camera
	public GameObject cameraMover;
	public float timeTakenDuringLerp = 1f;


	// Menues, Popups and Tooltips
	public Canvas PopupMenues;
	public GameObject ActionMenue;
	public GameObject AbilityMenue;
	bool actionMenueActive = false;

	bool moveRadiusIsActive = false;

	//Server stuff
	int playersDone = 0;

	// Use this for initialization
	void Start ()
	{
		//if (isServer) { //Networking - Disabled for now
			Debug.Log ("ServerSide");
			GameObject lastChild = null;
			GameObject[] lastRow = new GameObject[gridSizeX];

			// create the graph while generating the grid
			for (int x = 0; x < gridSizeX; x++) {
				lastChild = null;
				for (int y = 0; y < gridSizeY; y++) {
					GameObject tmp = GameObject.Instantiate (tile);
					tmp.transform.position = new Vector3 (x, -1.5f, y);
					tmp.transform.rotation = Quaternion.identity;
					tmp.transform.SetParent (grid.transform);
					tmp.transform.name = "x" + x + "/y" + y;
					tmp.gameObject.GetComponent<TileScript> ().posX = x;
					tmp.gameObject.GetComponent<TileScript> ().posY = y;
					if (lastChild != null) {
						tmp.gameObject.GetComponent<TileScript> ().neighbours.Add (lastChild);
						lastChild.gameObject.GetComponent<TileScript> ().neighbours.Add (tmp);
					}
					if (x > 0) {
						tmp.gameObject.GetComponent<TileScript> ().neighbours.Add (lastRow [y]);
						lastRow [y].gameObject.GetComponent<TileScript> ().neighbours.Add (tmp);
					}
					lastChild = tmp;
					lastRow [y] = tmp;
					tiles.Add (tmp);
				//NetworkServer.Spawn (tmp); //Networking - Disabled for now
				}
			}
		//}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (!moveRadiusIsActive) {
			if (Input.GetMouseButtonDown (0)) {
				ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				Physics.Raycast (ray, out hit);
				if (hit.collider != null) {
					if (hit.collider.tag == "Player" && !actionMenueActive) {
						if (!movingUnit) {
							unitClick (hit.collider.gameObject);
							selectedUnit = hit.collider.gameObject;
						}
					} else {
						if (!movingUnit && !actionMenueActive) {
							if (hit.collider.gameObject.GetComponent<TileScript> ().moveHere) {
								tiles [selectedUnit.GetComponent<PlayerUnit> ().posX * gridSizeX + selectedUnit.GetComponent<PlayerUnit> ().posY].GetComponent<TileScript> ().hasUnit = false;
								calculatePath (hit.collider.gameObject);
							}
						}
					}
				}
			}

		}
		//if (isServer) {
			if (playersDone == 2) {
				shiftTile ();
				playersDone = 0;
			}
		//}
	}

	public void shiftTile ()
	{
		foreach (GameObject g in tiles) {
			//Debug.Log (Mathf.PerlinNoise ((float)g.GetComponent<TileScript> ().posX/(float)gridSizeX,(float) g.GetComponent<TileScript> ().posY/(float)gridSizeY));
			float randomX = Random.Range (1f, 35f);
			float randomY = Random.Range (1f, 35f);
			//Debug.Log (Mathf.PerlinNoise (randomX * (float)g.GetComponent<TileScript> ().posX / (float)gridSizeX, randomY * (float)g.GetComponent<TileScript> ().posY / (float)gridSizeY));
			g.transform.position = new Vector3 (g.transform.position.x, -1.5f + (2f * (Mathf.PerlinNoise (randomX * (float)g.GetComponent<TileScript> ().posX / (float)gridSizeX, randomY * (float)g.GetComponent<TileScript> ().posY / (float)gridSizeY))), g.transform.position.z);
			if (g.GetComponent<TileScript> ().hasUnit) {
				RaycastHit output;
				Physics.Raycast (g.transform.position, Vector3.up, out output, 3.0f);
				output.collider.gameObject.transform.position = new Vector3 (output.collider.gameObject.transform.position.x, g.transform.position.y + 2f, output.collider.gameObject.transform.position.z);
			}
		}
	}

	public void unitClick (GameObject unit)
	{
			int moveRadius = unit.GetComponent<PlayerUnit> ().moveRadius;

			if (!movingUnit && moveRadiusShowing) {
				moveRadiusIsActive = true;
				// Open menü: Attack, or wait
				hideMoveRadius ();
				showActionMenue (unit);
			} else if (!moveRadiusShowing) {
				showMoveRadius (moveRadius, tiles [unit.GetComponent<PlayerUnit> ().posX * gridSizeX + unit.GetComponent<PlayerUnit> ().posY]);
			}

	}





	public void showMoveRadius (int mr, GameObject startTile)
	{
		if (!movingUnit) {
			moveRadiusShowing = true;
			if (mr > 0) {
				mr -= 1;
				foreach (GameObject tile in startTile.GetComponent<TileScript> ().neighbours) {
					if (tile.GetComponent<TileScript> ().accessible && !tile.GetComponent<TileScript> ().hasUnit) {		
						if (!moveTiles.Contains (tile)) {
							tile.GetComponent<Renderer> ().material.color = Color.cyan;
							tile.GetComponent<TileScript> ().moveHere = true;
							moveTiles.Add (tile);
						}
						showMoveRadius (mr, tile);
					}
				}
			}
		} else {
			hideMoveRadius ();
		}
	}

	public void showAttackRadius(int min, int max, List<GameObject> startTile){
		Debug.Log (startTile[0].GetComponent<TileScript>().posX + " TilePosX");
		attackRadiusShowing = true;
		List<GameObject> tmp = new List<GameObject> (startTile);
		notAttackTiles.Clear ();
		if (max > 0) {
			max -= 1;
			min -= 1;
			foreach (GameObject tile in tmp) {
				foreach (GameObject t in tile.GetComponent<TileScript> ().neighbours) {
					if (t.GetComponent<TileScript> ().accessible) {
						if (!attackTiles.Contains (t)) {
							if (min <= 0) {
								t.GetComponent<Renderer> ().material.color = Color.red;
							}
							notAttackTiles.Add (t);  // used for the next iteration
							attackTiles.Add (t);
						}
					}
				} 
			}
			showAttackRadius(min, max, notAttackTiles);
		}
	}

	public void calculatePath (GameObject destination)
	{

		List<GameObject> openList = new List<GameObject> ();
		List<GameObject> closedList = new List<GameObject> ();
		List<GameObject> adjTiles = new List<GameObject> ();
		List<GameObject> path = new List<GameObject> ();
		GameObject parent = null; 

		int endX = destination.GetComponent<TileScript> ().posX;
		int endY = destination.GetComponent<TileScript> ().posY;

		int startX = selectedUnit.GetComponent<PlayerUnit> ().posX;
		int startY = selectedUnit.GetComponent<PlayerUnit> ().posY;

		// Try to give the player a reference to the tile he is standing on so that we can dump those retarded dictionaries
		GameObject selectedTile = tiles [selectedUnit.GetComponent<PlayerUnit> ().posX * gridSizeX + selectedUnit.GetComponent<PlayerUnit> ().posY];
		int tmpFScore = 0;
		openList.Add (selectedTile);


		// Try to do it with a graph.  Each Tile has each adjacent tile as a child. instead of going in the direction just go through the children


		//Schleife
		do {
			for (int i = 0; i < openList.Count; i++) {
				if (i == 0) {
					tmpFScore = openList [i].GetComponent<TileScript> ().fScore;
					selectedTile = openList [i];
				}
				if (openList [i].GetComponent<TileScript> ().fScore < tmpFScore) {
					tmpFScore = openList [i].GetComponent<TileScript> ().fScore;
					selectedTile = openList [i];
				}
			}
				

			closedList.Add (selectedTile);
			openList.Remove (selectedTile);


			//If Destination found
			if (closedList.Contains (destination)) {
				parent = destination;
				//Debug.Log(destination + " " + destination.GetComponent<TileScript>().parent);
				selectedUnit.GetComponent<PlayerUnit> ().posX = endX;
				selectedUnit.GetComponent<PlayerUnit> ().posY = endY;
				//try and build a path by going from the destination backwards and add the partent to the list
				int i = selectedUnit.GetComponent<PlayerUnit> ().moveRadius;
				do {
					i--;
					path.Add (parent);
					parent = parent.GetComponent<TileScript> ().parent;
					Debug.LogError (path.Count);
					if (i <= 0) {
						Debug.LogError ("Path too long!");
						break;
					}
				} while(parent != null);
				break;
			}

			//------------------------------------------- GRAPH VERSION START --------------------------------------


			foreach (GameObject tile in selectedTile.GetComponent<TileScript> ().neighbours) {
				if (!tile.GetComponent<TileScript> ().hasUnit && tile.GetComponent<TileScript> ().accessible && moveTiles.Contains (tile)) {
					Debug.Log ("TileCheck" + tile.transform.name);
					tile.GetComponent<TileScript> ().gScore = Mathf.Abs (tile.GetComponent<TileScript> ().posX - startX) + Mathf.Abs (tile.GetComponent<TileScript> ().posY - startY);
					tile.GetComponent<TileScript> ().hScore = Mathf.Abs (tile.GetComponent<TileScript> ().posX - endX) + Mathf.Abs (tile.GetComponent<TileScript> ().posY - endY);

					tile.GetComponent<TileScript> ().fScore = tile.GetComponent<TileScript> ().gScore + tile.GetComponent<TileScript> ().hScore;

					if (!closedList.Contains (tile) && !openList.Contains (tile)) {// if it is on the closed list ignore it, we don t need it it has already been checked
						openList.Add (tile);									// if it is not on the open list add it to the open list
						tile.GetComponent<TileScript> ().parent = selectedTile;
					}
				}
			}


			//------------------------------------------- GRAPH VERSION END ----------------------------------------


		} while(openList.Count > 0);

		movingUnit = true;
		StartCoroutine (MoveOverSeconds (selectedUnit, timeTakenDuringLerp, path));
	}




	public IEnumerator MoveOverSeconds (GameObject objectToMove, float seconds, List<GameObject> path)
	{
		
		if (path.Count == 0) {
			Debug.Log ("Path Empty");
			yield break;
		}

		float elapsedTime = 0;
		Vector3 startingPos = objectToMove.transform.position;
		Vector3 end = new Vector3 (path [path.Count - 1].transform.position.x, path [path.Count - 1].transform.position.y + 2f, path [path.Count - 1].transform.position.z);
		path [path.Count - 1].GetComponent<TileScript> ().parent = null;
		path.RemoveAt (path.Count - 1);
		while (elapsedTime < seconds) {
			selectedUnit.transform.position = Vector3.Lerp (startingPos, end, (elapsedTime / seconds));
			elapsedTime += Time.deltaTime;
			yield return new WaitForEndOfFrame ();
		}
		selectedUnit.transform.position = end;
		if (path.Count > 0) {
			StartCoroutine (MoveOverSeconds (selectedUnit, timeTakenDuringLerp, path));
		} else {
			tiles [(int)end.x * gridSizeX + (int)end.z].GetComponent<TileScript> ().hasUnit = true;
			movingUnit = false;
			showActionMenue (selectedUnit);
			moveRadiusShowing = false;
		}
	}

	public void showActionMenue (GameObject unit)
	{
		
		Vector2 playerPosition = Camera.main.WorldToViewportPoint (unit.transform.position);
		//Debug.Log (playerPosition);
		Vector2 actualPosition = new Vector2 (
			                         ((playerPosition.x * PopupMenues.GetComponent<RectTransform> ().sizeDelta.x) - (PopupMenues.GetComponent<RectTransform> ().sizeDelta.x * 0.5f)),
			                         ((playerPosition.y * PopupMenues.GetComponent<RectTransform> ().sizeDelta.y) - (PopupMenues.GetComponent<RectTransform> ().sizeDelta.y * 0.5f)));

		ActionMenue.GetComponent<RectTransform> ().anchoredPosition = actualPosition;
		ActionMenue.SetActive (true);
		actionMenueActive = true;
		hideMoveRadius ();
	}


	public void ShowAbilities(bool value){
		Debug.Log ("Attack! " + value);
		AbilityMenue.SetActive (value);
	}

	public void hideMoveRadius ()
	{
		moveRadiusShowing = false;
		foreach (GameObject go in tiles) {
			go.GetComponent<Renderer> ().material.color = Color.white;
			go.GetComponent<TileScript> ().moveHere = false;
		}
		moveTiles.Clear ();
	}

	public void setMovingUnit (bool b)
	{
		movingUnit = b;
	}

	public void setActionMenueActive (bool b)
	{
		actionMenueActive = b;
	}

	public void PlayerDone ()
	{
		playersDone++;
	}

	public void setIsActive ()
	{
		moveRadiusIsActive = false;
	}

	public void Ability1(){
		selectedUnit.GetComponent<PlayerUnit> ().Ability1Button ();
	}

	public void Ability2(){
		selectedUnit.GetComponent<PlayerUnit> ().Ability2Button ();
	}

	public void Ability3(){
		GameObject[] players = GameObject.FindGameObjectsWithTag ("Player");
		if (players [1].Equals (selectedUnit)) {
			selectedUnit.GetComponent<PlayerUnit> ().Ability3 (players[0]);
		}
		if (players [0].Equals (selectedUnit)) {
			selectedUnit.GetComponent<PlayerUnit> ().Ability3 (players[1]);
		}
	}

	public void Ability4(){
		GameObject[] players = GameObject.FindGameObjectsWithTag ("Player");
		if (players [1].Equals (selectedUnit)) {
			selectedUnit.GetComponent<PlayerUnit> ().Ability3 (players[0]);
		}
		if (players [0].Equals (selectedUnit)) {
			selectedUnit.GetComponent<PlayerUnit> ().Ability3 (players[1]);
		}
	}
}










//public IEnumerator MoveOverSeconds (GameObject objectToMove, Vector3 end, float seconds, List<GameObject> path)
//{
//	float elapsedTime = 0;
//	Vector3 startingPos = objectToMove.transform.position;
//	while (elapsedTime < seconds)
//	{
//		selectedUnit.transform.position = Vector3.Lerp(startingPos, end, (elapsedTime / seconds));
//		elapsedTime += Time.deltaTime;
//		yield return new WaitForEndOfFrame();
//	}
//	selectedUnit.transform.position = end;
//
//}