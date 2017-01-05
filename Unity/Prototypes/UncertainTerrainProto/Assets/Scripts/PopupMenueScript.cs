using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PopupMenueScript : MonoBehaviour {


	public GameObject ActionMenue;
	public GameObject Player1Health;
	public GameObject Player2Health;
	public GameObject Player1;
	public GameObject Player2;

	// Use this for initialization
	void Start () {
		ActionMenue.gameObject.SetActive (false);
	}
	
	// Update is called once per frame
	void Update () {
		Player1Health.GetComponent<Text> ().text = Player1.GetComponent<PlayerUnit> ().currentHealth.ToString();
		Player2Health.GetComponent<Text> ().text = Player2.GetComponent<PlayerUnit> ().currentHealth.ToString();
	}
}
