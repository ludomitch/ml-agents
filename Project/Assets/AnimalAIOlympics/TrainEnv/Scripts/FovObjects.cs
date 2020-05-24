using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.Sensors;
using UnityEngineExtensions;
using System.Linq;
public class FovObjects : MonoBehaviour
{
    public Camera cam;

// Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
   //      string allObjects = "";
   //      foreach (GameObject go in GameObject.FindGameObjectsWithTag("arena"))
   //      {
   //          Transform[] allChildren = go.GetComponentsInChildren<Transform>();
   //          foreach (Transform child in allChildren) {
			// 	Vector3 screenPoint = cam.WorldToViewportPoint(child.position);
			// 	bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

			// 	if(!(new[] {
			// 		"fence", "Wall", "Ground", "Cam",
			// 		 "Fwd", "Spawn", "Screen", "Light",
			// 		  "Arena", "Image", "Agent" }.Any(x => child.name.Contains(x))) && onScreen) {
			// 		allObjects += child.name + "-";
			// 	}
   //          }
			// Debug.Log(allObjects);
        // }
    }
}
