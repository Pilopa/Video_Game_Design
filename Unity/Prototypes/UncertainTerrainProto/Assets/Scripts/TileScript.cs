using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileScript : MonoBehaviour{

	public List<GameObject> neighbours;

	public bool moveHere = false;
	public bool accessible = true;
	public bool hasUnit = false;

	public int posX;
	public int posY;
	public int posZ;

	public int gScore;
	public int hScore;
	public int fScore;

	public GameObject parent = null;

}
