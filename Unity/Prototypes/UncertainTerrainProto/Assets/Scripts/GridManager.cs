using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class GridManager : NetworkBehaviour {

	public GameObject tile;
	public GameObject grid;

	GameObject selectedUnit;

	public int gridSizeX;
	public int gridSizeY;

	Dictionary<KeyValuePair<int, int>, GameObject> gridTiles = new Dictionary<KeyValuePair<int, int>, GameObject>();
	Dictionary<KeyValuePair<int, int>, GameObject> moveRadius = new Dictionary<KeyValuePair<int, int>, GameObject>();

	public bool movingUnit = false;
	public bool moveRadiusShown = false;

	Ray ray;
	RaycastHit hit;

	//Camera
	public GameObject cameraMover;
	public float timeTakenDuringLerp = 1f;


	// Menues, Popups and Tooltips
	public Canvas PopupMenues;
	public GameObject ActionMenue;
	bool actionMenueActive = false;

	bool isActive = false;

	//Server stuff
	int playersDone = 0;

	// Use this for initialization
	void Start () {
		if (isServer) {
			GameObject lastChild = null;
			GameObject[] lastRow = new GameObject[gridSizeX];

			// create the graph while generating the grid
			for (int x = 0; x < gridSizeX; x++) {
				lastChild = null;
				for (int y = 0; y < gridSizeY; y++) {
					GameObject tmp = Instantiate (tile, new Vector3 (x, 0, y), Quaternion.identity) as GameObject;
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

					gridTiles.Add (new KeyValuePair<int, int> (x, y), tmp);
				}

			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (!isActive){
			if (Input.GetMouseButtonDown (0)) {
				ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				Physics.Raycast (ray, out hit);

				if (hit.collider != null) {
					if (hit.collider.tag == "Player" && !actionMenueActive) {
						unitClick (hit.collider.gameObject);
						selectedUnit = hit.collider.gameObject;
					} else {
						if (!movingUnit && !actionMenueActive) {
							if (hit.collider.gameObject.GetComponent<TileScript> ().moveHere) {
								gridTiles [new KeyValuePair<int,int> (selectedUnit.GetComponent<PlayerUnit> ().posX, selectedUnit.GetComponent<PlayerUnit> ().posY)].GetComponent<TileScript> ().hasUnit = false;
								calculatePath (hit.collider.gameObject);
							}
						}
					}
				}
			}

		}
		if (isServer) {
			if (playersDone == 2) {



				playersDone = 0;
			}
		}
	}


	public void unitClick (GameObject unit){
		if (unit.GetComponent<NetworkIdentity> ().localPlayerAuthority) {
			int moveRadius = unit.GetComponent<PlayerUnit> ().moveRadius;

			if (!movingUnit && moveRadiusShown) {
				isActive = true;
				// Open menü: Attack, or wait
				hideMoveRadius();
				showActionMenue ( unit);
			} 
			else if (!moveRadiusShown) {
				showMoveRadius (moveRadius, unit.GetComponent<PlayerUnit> ().posX, unit.GetComponent<PlayerUnit> ().posY);
			}
		}
	}





	public void showMoveRadius(int mr, int x, int y){
		if (!movingUnit) {
			moveRadiusShown = true;
			if (mr > 0) {
				mr -= 1;
				if (gridTiles.ContainsKey (new KeyValuePair<int, int> (x + 1, y + 0))) {
					KeyValuePair<int, int> tmp = new KeyValuePair<int, int> (x + 1, y + 0);
					if (gridTiles [tmp].GetComponent<TileScript> ().accessible && !gridTiles [tmp].GetComponent<TileScript> ().hasUnit) {
						gridTiles [tmp].GetComponent<Renderer> ().material.color = Color.cyan;
						gridTiles [tmp].GetComponent<TileScript> ().moveHere = true;
						if (!moveRadius.ContainsKey (tmp)) {
							moveRadius.Add (tmp, gridTiles [tmp]);
						}
						showMoveRadius (mr, x + 1, y + 0);
					}
				}

				if (gridTiles.ContainsKey (new KeyValuePair<int, int> (x + 0, y + 1))) {
					KeyValuePair<int, int> tmp = new KeyValuePair<int, int> (x + 0, y + 1);
					if (gridTiles [tmp].GetComponent<TileScript> ().accessible && !gridTiles [tmp].GetComponent<TileScript> ().hasUnit) {
						gridTiles [tmp].GetComponent<Renderer> ().material.color = Color.cyan;
						gridTiles [tmp].GetComponent<TileScript> ().moveHere = true;
						if (!moveRadius.ContainsKey (tmp)) {
							moveRadius.Add (tmp, gridTiles [tmp]);
						}
						showMoveRadius (mr, x + 0, y + 1);
					}
				}

				if (gridTiles.ContainsKey (new KeyValuePair<int, int> (x - 1, y + 0))) {
					KeyValuePair<int, int> tmp = new KeyValuePair<int, int> (x - 1, y + 0);
					if (gridTiles [tmp].GetComponent<TileScript> ().accessible && !gridTiles [tmp].GetComponent<TileScript> ().hasUnit) {
						gridTiles [tmp].GetComponent<Renderer> ().material.color = Color.cyan;
						gridTiles [tmp].GetComponent<TileScript> ().moveHere = true;
						showMoveRadius (mr, x - 1, y + 0);
						if (!moveRadius.ContainsKey (tmp)) {
							moveRadius.Add (tmp, gridTiles [tmp]);
						}
					}
				}

				if (gridTiles.ContainsKey (new KeyValuePair<int, int> (x + 0, y - 1))) {
					KeyValuePair<int, int> tmp = new KeyValuePair<int, int> (x + 0, y - 1);
					if (gridTiles [tmp].GetComponent<TileScript> ().accessible && !gridTiles [tmp].GetComponent<TileScript> ().hasUnit) {
						gridTiles [tmp].GetComponent<Renderer> ().material.color = Color.cyan;
						gridTiles [tmp].GetComponent<TileScript> ().moveHere = true;
						showMoveRadius (mr, x + 0, y - 1);
						if (!moveRadius.ContainsKey (tmp)) {
							moveRadius.Add (tmp, gridTiles [tmp]);
						}
					}
				}
		
			}
		}
		else
		{
			hideMoveRadius ();

		}
	}


	public void calculatePath(GameObject destination){

		List<GameObject> openList = new List<GameObject>();
		List<GameObject> closedList = new List<GameObject>();
		List<GameObject> adjTiles =  new List<GameObject>();
		List<GameObject> path =  new List<GameObject>();
		GameObject parent = null; 

		int endX = destination.GetComponent<TileScript> ().posX;
		int endY = destination.GetComponent<TileScript> ().posY;

		int startX = selectedUnit.GetComponent<PlayerUnit> ().posX;
		int startY = selectedUnit.GetComponent<PlayerUnit> ().posY;

		// Try to give the player a reference to the tile he is standing on so that we can dump those retarded dictionaries
		GameObject selectedTile = gridTiles [new KeyValuePair<int, int> (selectedUnit.GetComponent<PlayerUnit> ().posX, selectedUnit.GetComponent<PlayerUnit> ().posY)];
		int tmpFScore = 0;
		openList.Add (selectedTile);


		// Try to do it with a graph.  Each Tile has each adjacent tile as a child. instead of going in the direction just go through the children


		//Schleife
		do {
			for(int i = 0; i < openList.Count; i++)
			{
				if(i == 0){
					tmpFScore = openList[i].GetComponent<TileScript>().fScore;
					selectedTile = openList[i];
				}
				if(openList[i].GetComponent<TileScript>().fScore < tmpFScore){
					tmpFScore = openList[i].GetComponent<TileScript>().fScore;
					selectedTile = openList[i];
				}
			}
				

			closedList.Add (selectedTile);
			openList.Remove (selectedTile);


			//If Destination found
			if(closedList.Contains(destination)){
				parent = destination;
				//Debug.Log(destination + " " + destination.GetComponent<TileScript>().parent);
				selectedUnit.GetComponent<PlayerUnit>().posX = endX;
				selectedUnit.GetComponent<PlayerUnit>().posY = endY;
				//try and build a path by going from the destination backwards and add the partent to the list
				int i = selectedUnit.GetComponent<PlayerUnit>().moveRadius;
				do{
					//i--;
					path.Add(parent);
					parent = parent.GetComponent<TileScript>().parent;
					Debug.LogError(path.Count);
					/*
					if(i<=0){
						Debug.LogError("Path too long!");
						break;
					}
					*/
				}while(parent != null);
				break;
			}

			//------------------------------------------- GRAPH VERSION START --------------------------------------


			foreach (GameObject tile in selectedTile.GetComponent<TileScript> ().neighbours)
			{
				if(!tile.GetComponent<TileScript> ().hasUnit && tile.GetComponent<TileScript> ().accessible &&  moveRadius.ContainsKey (new KeyValuePair<int, int> (tile.GetComponent<TileScript> ().posX, tile.GetComponent<TileScript> ().posY)))
				{
					Debug.Log("TileCheck" + tile.transform.name);
					tile.GetComponent<TileScript> ().gScore = Mathf.Abs(tile.GetComponent<TileScript> ().posX - startX) + Mathf.Abs(tile.GetComponent<TileScript> ().posY - startY);
					tile.GetComponent<TileScript> ().hScore = Mathf.Abs(tile.GetComponent<TileScript> ().posX - endX) + Mathf.Abs(tile.GetComponent<TileScript> ().posY - endY);

					tile.GetComponent<TileScript> ().fScore = tile.GetComponent<TileScript> ().gScore + tile.GetComponent<TileScript> ().hScore;

					if(!closedList.Contains (tile) && !openList.Contains (tile))// if it is on the closed list ignore it, we don t need it it has already been checked
					{
						openList.Add (tile);									// if it is not on the open list add it to the open list
						tile.GetComponent<TileScript> ().parent = selectedTile;
					}
				}
			}


			//------------------------------------------- GRAPH VERSION END ----------------------------------------


		} while(openList.Count > 0);

		movingUnit = true;
		StartCoroutine (MoveOverSeconds( selectedUnit, timeTakenDuringLerp, path));
	}




	public IEnumerator MoveOverSeconds (GameObject objectToMove, float seconds, List<GameObject> path)
	{
		if (path.Count == 0) {
			Debug.Log ("Path Empty");
			yield break;
		}
		float elapsedTime = 0;
		Vector3 startingPos = objectToMove.transform.position;
		Vector3 end = new Vector3 (path [path.Count - 1].transform.position.x, 1 ,path [path.Count - 1].transform.position.z); 			//path.Count - 1
		path [path.Count - 1].GetComponent<TileScript> ().parent = null;
		path.RemoveAt (path.Count - 1);
		while (elapsedTime < seconds)
		{
			selectedUnit.transform.position = Vector3.Lerp(startingPos, end, (elapsedTime / seconds));
			elapsedTime += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		selectedUnit.transform.position = end;
		if (path.Count > 0) {
			StartCoroutine (MoveOverSeconds (selectedUnit, timeTakenDuringLerp, path));
		} else {
			gridTiles [new KeyValuePair<int, int> ((int)end.x, (int)end.z)].GetComponent<TileScript> ().hasUnit = true;
			movingUnit = false;
			showActionMenue (selectedUnit);
		}
	}
		
	public void showActionMenue(GameObject unit){
		
		Vector2 playerPosition=Camera.main.WorldToViewportPoint(unit.transform.position);
		//Debug.Log (playerPosition);
		Vector2 actualPosition = new Vector2(
			((playerPosition.x*PopupMenues.GetComponent<RectTransform> ().sizeDelta.x)-(PopupMenues.GetComponent<RectTransform> ().sizeDelta.x*0.5f)),
			((playerPosition.y*PopupMenues.GetComponent<RectTransform> ().sizeDelta.y)-(PopupMenues.GetComponent<RectTransform> ().sizeDelta.y*0.5f)));

		ActionMenue.GetComponent<RectTransform> ().anchoredPosition = actualPosition;
		ActionMenue.SetActive(true);
		actionMenueActive = true;
		hideMoveRadius ();
	}

	public void hideMoveRadius(){
		moveRadiusShown = false;
		List<GameObject> tmpGO = new List<GameObject>(gridTiles.Values);
		foreach (GameObject go in tmpGO) {
			go.GetComponent<Renderer> ().material.color = Color.white;
			go.GetComponent<TileScript> ().moveHere = false;
		}
		moveRadius.Clear ();
	}

	public void setMovingUnit(bool b){
		movingUnit = b;
	}

	public void setActionMenueActive(bool b){
		actionMenueActive = b;
	}

	[ClientRpc]
	public void RpcPlayerDone(){
		playersDone++;
	}

	public void setIsActive(){
		isActive = false;
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