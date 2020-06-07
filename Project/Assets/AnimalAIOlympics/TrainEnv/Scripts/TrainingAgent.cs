using System.Linq;
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using MLAgents;
using PrefabInterface;
using MLAgents.Sensors;
using System.Collections.Generic;

public class TrainingAgent : Agent, IPrefab
{
    public void RandomSize() { }
    public void SetColor(Vector3 color) { }
    public void SetSize(Vector3 scale) { }

    public virtual Vector3 GetPosition(Vector3 position,
                                        Vector3 boundingBox,
                                        float rangeX,
                                        float rangeZ)
    {
        float xBound = boundingBox.x;
        float zBound = boundingBox.z;
        float xOut = position.x < 0 ? Random.Range(xBound, rangeX - xBound)
                                    : Math.Max(0, Math.Min(position.x, rangeX));
        float yOut = Math.Max(position.y, 0) + transform.localScale.y / 2 + 0.01f;
        float zOut = position.z < 0 ? Random.Range(zBound, rangeZ - zBound)
                                    : Math.Max(0, Math.Min(position.z, rangeZ));

        return new Vector3(xOut, yOut, zOut);
    }

    public virtual Vector3 GetRotation(float rotationY)
    {
        return new Vector3(0,
                        rotationY < 0 ? Random.Range(0f, 360f) : rotationY,
                        0);
    }
    public Camera cam;
    public float speed = 30f;
    public float rotationSpeed = 100f;
    public float rotationAngle = 0.25f;
    [HideInInspector]
    public int numberOfGoalsCollected = 0;

    private Rigidbody _rigidBody;
    private bool _isGrounded;
    private ContactPoint _lastContactPoint;
    private TrainingArena _arena;
    private float _rewardPerStep;
    private Color[] _allBlackImage;
    private float _previousScore = 0;
    private float _currentScore = 0;
    List<string> ObjMap = new List<string> {
                                             "Cardbox1",
                                             "Cardbox2",
                                             "CylinderTunnelTransparent",
                                             "CylinderTunnel",
                                             "DeathZone",
                                             "GoodGoalMulti",
                                             "GoodGoal",
                                             "LObject",
                                             "LObject2",
                                             "Ramp",
                                             "UObject",
                                             "WallTransparent",
                                             "Wall",
                                              "BadGoal",
                                              "HotZone"};
    // const int NUM_OBJ_TYPES = (int)ObjMap.WallTransparent;

    public override void Initialize()
    {
        _arena = GetComponentInParent<TrainingArena>();
        _rigidBody = GetComponent<Rigidbody>();
        _rewardPerStep = maxStep > 0 ? -1f / maxStep : 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent attributes
        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.velocity);
        Vector3 localPos = transform.localPosition;
        Vector3 localRot = transform.rotation.eulerAngles;
        sensor.AddObservation(localVel);
        sensor.AddObservation(localPos);
        sensor.AddObservation(localRot);
        // Objects in scene
        List<Vector3> objects = getFovObjects();
        // Debug.Log(objects.Count);
        for (var i = 0; i < 30; i++) { // 10 objects = 10*Vector3 objects. 29 with 0 indexing
            if (i<objects.Count){
                sensor.AddObservation(objects[i]);
            }
            else {
                sensor.AddObservation(new Vector3(-1.0f, -1.0f, -1.0f));
            }
        }


        // RaycastSweep();
        // TODO
        // 1) Need to get all FOV objects in some format, list of array
        // 2) Get the number of objects retrieved: do len of list
        // 3) Do a add observation in batches of 9 for each 10 objects
        // 4) If num objects under 10 then loop through adding vector size 9 of 0s, use
        // -1 for class if enum is 0 indexed
        // 5) Total observations should be 97, 7 for agent + 9*10 objects

    }

    public bool check_raycasts(List<Vector3> corners, string name){
    // public bool check_raycasts(Vector3 corners, string name){
        // Format name by removing (clone)
        string removeString = "(Clone)";
        int index = name.IndexOf(removeString);
        string cleanName = (index < 0)
            ? name
            : name.Remove(index, removeString.Length);
        int layer_mask = LayerMask.GetMask("noray");
        // Debug.Log(layer_mask);
        foreach (Vector3 corner in corners) {
            RaycastHit hit;
            Vector3 dir = corner - cam.transform.position;
            // Debug.Log(corners);
            // Vector3 forward = cam.transform.TransformDirection(Vector3.forward) * 10;
            // Debug.DrawRay(cam.transform.position, forward, Color.green);
            // Debug.Log(cleanName);
            if(Physics.Raycast(cam.transform.position, dir, out hit, 100, 1)){
                // Debug.Log(hit.collider.gameObject.name+"-"+cleanName);
                if (hit.collider.gameObject.name.Contains(cleanName)){
                    // Debug.Log("HITTT" + cleanName);
                    return true;
                }
            }
        }
            return false;
    }

    public float CalcScreenPercentage(Transform obj) {

        var minX = Mathf.Infinity;
        var minY = Mathf.Infinity;
        var maxX = -Mathf.Infinity;
        var maxY = -Mathf.Infinity;

        var bounds = obj.GetComponent<Renderer>().bounds; 
        var v3Center = bounds.center;
        var v3Extents = bounds.extents;

        List<Vector3> corners = new List<Vector3>(new Vector3[9]);

        corners[0]  = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
        corners[1]  = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
        corners[2]  = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
        corners[3]  = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
        corners[4]  = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
        corners[5]  = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
        corners[6]  = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
        corners[7]  = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner
        corners[8] = v3Center;

        if (!check_raycasts(corners, obj.name)){
            return (float)-1.0;
        }
        // string corner_st = "";
        for (var i = 0; i < corners.Count-1; i++) {
            var corner = cam.WorldToScreenPoint(corners[i]);
            if (corner.x > maxX) maxX = corner.x;
            if (corner.x < minX) minX = corner.x;
            if (corner.y > maxY) maxY = corner.y;
            if (corner.y < minY) minY = corner.y;
            minX = Mathf.Clamp(minX, 0, Screen.width);
            maxX = Mathf.Clamp(maxX, 0, Screen.width);
            minY = Mathf.Clamp(minY, 0, Screen.height);
            maxY = Mathf.Clamp(maxY, 0, Screen.height);
            // corner_st += corner.x.ToString("N1")+"–";
            }

        // Debug.Log(obj.name+corner_st);
        var width = maxX - minX;
        var height = maxY - minY;
        var area = width * height;
        var screenArea = Screen.width * Screen.height;
        // Debug.Log(area.ToString("N1")+"-" + screenArea.ToString("N1")+obj.name);
        var percentage = area / screenArea * 100.0;
        // Debug.Log(width.ToString("N1")+"-" + height.ToString("N1") + obj.name + percentage.ToString("N1"));

        return (float)percentage;
    }

    public List<Vector3> getFovObjects()
    {
        string all_objects = "";
        List<Vector3> obj_vectors = new List<Vector3>();
        Transform parent_arena = transform.parent;
        List<string> blacklist = new List<string> {
                "fence", "WallOut", "Walls", "Ground", "Cam",
                 "Fwd", "Spawn", "Screen", "Light",
                  "Arena", "Image", "Agent", "Ramp(Clone)"};
        float percentage;
        Transform[] allChildren = parent_arena.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren) {
            if(!(blacklist.Any(x => child.name.Contains(x)))) {
                Vector3 screenPoint = cam.WorldToViewportPoint(child.position);
                bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
                // Vector3 dir = child.transform.position - cam.transform.position;
                // ray = cam.transform.position;
                // if(Physics.Raycast(cam.transform.position, dir, out hit)&&onScreen){
                //     if (hit.collider.gameObject.name==child.name){

                        // Debug.Log("HIT:" + hit.collider.gameObject.name);
                if (onScreen){
                    percentage = CalcScreenPercentage(child);
                    // Debug.Log(percentage + child.name);
                }
                else {
                    percentage = (float)-1.0;
                }
                if (percentage !=-1.0){
                    int obj_idx =  ObjMap.FindIndex(s => child.name.Contains(s));
                    obj_vectors.Add(new Vector3(obj_idx, child.transform.rotation.eulerAngles.y, percentage));
                    obj_vectors.Add(child.transform.localPosition);
                    obj_vectors.Add(child.transform.localScale);
                    all_objects += child.name;// + percentage.ToString("N2");                    
                    // }
                }
            }
        }
        if (!string.IsNullOrEmpty(all_objects)) {
            Debug.Log(all_objects);
        }

        // Vector3 forward = cam.transform.TransformDirection(Vector3.forward) * 10;
        // RaycastHit hit;
        // // Vector3 forward = cam.transform.TransformDirection(Vector3.forward) * 10;
        // // Debug.DrawRay(cam.transform.position, forward, Color.green);

        // if(Physics.Raycast(cam.transform.position, forward, out hit)){
        //     Debug.Log(hit.collider.gameObject.name);
        //     }
     
        return obj_vectors;
    }

    public void RaycastSweep() 
     {
        float distance = 40.0f;
        float theAngle = 25.0f;
        float segments = 10.0f;
        Vector3 startPos = cam.transform.position; // umm, start position !
        Vector3 targetPos = Vector3.zero; // variable for calculated end position
         
        int startAngle = Convert.ToInt32(-theAngle * 0.5f); // half the angle to the Left of the forward
        int finishAngle = Convert.ToInt32(theAngle * 0.5f); // half the angle to the Right of the forward
         
         // the gap between each ray (increment)
        int inc = Convert.ToInt32(theAngle / segments);
        RaycastHit hit;

        List<string> blacklist = new List<string> {
                "fence", "Wall", "Ground", "Cam",
                 "Fwd", "Spawn", "Screen", "Light",
                  "Arena", "Image"};
        string all_objects = "";
         // step through and find each target point
         for (int i = startAngle; i < finishAngle; i += inc ) // Angle from forward
         {
             targetPos = (Quaternion.Euler( 0, i, 0 ) * transform.forward).normalized * distance;    
             
             // linecast between points
             if ( Physics.Linecast( startPos, targetPos, out hit ) )
             {
                if(!(blacklist.Any(x => hit.collider.gameObject.name.Contains(x)))){
                 // Debug.Log( "Hit " + hit.collider.gameObject.name );
                all_objects += hit.collider.gameObject.name + "-";                    

                }
             }    
             
             // to show ray just for testing
             Debug.DrawLine( startPos, targetPos, Color.green ); 
             Debug.Log(all_objects);   
         }        
     }
    // {

    //     List<Vector3> obj_vectors = new List<Vector3>();
    //     Transform parent_arena = transform.parent;
    //     string all_objects = "";
    //     List<string> blacklist = new List<string> {
    //             "fence", "Wall", "Ground", "Cam",
    //              "Fwd", "Spawn", "Screen", "Light",
    //               "Arena", "Image", "Agent", "Cube" };
    //     Transform[] allChildren = parent_arena.GetComponentsInChildren<Transform>();
    //     foreach (Transform child in allChildren) {
    //         Vector3 screenPoint = cam.WorldToViewportPoint(child.position);
    //         bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

    //         if(!(blacklist.Any(x => child.name.Contains(x))) && onScreen) {
    //             // int obj_idx =  (int)Enum.Parse(typeof(ObjMap), child.name);
    //             // Debug.Log(child.name);
    //             int obj_idx =  ObjMap.FindIndex(s => child.name.Contains(s));
    //             obj_vectors.Add(new Vector3(obj_idx, child.transform.rotation.eulerAngles.y, 0));
    //             obj_vectors.Add(child.transform.localPosition);
    //             obj_vectors.Add(child.transform.localScale);
    //             all_objects += child.name + obj_idx + "-";
    //         }
    //     }
    //     Debug.Log(all_objects);
    //     // }
    //     return obj_vectors;
    // }

    // public List<Vector3> getFovObjects()
    // {
    //     RaycastHit hit;
    //     string all_objects = "";
    //     List<Vector3> obj_vectors = new List<Vector3>();
    //     Transform parent_arena = transform.parent;
    //     List<string> blacklist = new List<string> {
    //             "fence", "Wall", "Ground", "Cam",
    //              "Fwd", "Spawn", "Screen", "Light",
    //               "Arena", "Image", "Agent", "Cube" };
    //     Transform[] allChildren = parent_arena.GetComponentsInChildren<Transform>();
    //     foreach (Transform child in allChildren) {
    //         if(!(blacklist.Any(x => child.name.Contains(x)))) {
    //             Vector3 screenPoint = cam.WorldToViewportPoint(child.position);
    //             bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    //             Vector3 dir = child.transform.position - cam.transform.position;
    //             // ray = cam.transform.position;
    //             if(Physics.Raycast(cam.transform.position, dir, out hit)&&onScreen){
    //                 if (hit.collider.gameObject.name==child.name){
    //                     Debug.Log("HIT:" + hit.collider.gameObject.name);
    //                     int obj_idx =  ObjMap.FindIndex(s => child.name.Contains(s));
    //                     obj_vectors.Add(new Vector3(obj_idx, child.transform.rotation.eulerAngles.y, 0));
    //                     obj_vectors.Add(child.transform.localPosition);
    //                     obj_vectors.Add(child.transform.localScale);
    //                     all_objects += child.name + obj_idx + "-";                    
    //                 }
    //             }
    //         }
    //     }
    //     if (!string.IsNullOrEmpty(all_objects)) {
    //         Debug.Log(all_objects);
    //     }

     
    //     return obj_vectors;
    // }

    public override void OnActionReceived(float[] vectorAction)
    {
        int actionForward = Mathf.FloorToInt(vectorAction[0]);
        int actionRotate = Mathf.FloorToInt(vectorAction[1]);

        moveAgent(actionForward, actionRotate);

        AddReward(_rewardPerStep);
        _currentScore = GetCumulativeReward();
    }

    private void moveAgent(int actionForward, int actionRotate)
    {
        Vector3 directionToGo = Vector3.zero;
        Vector3 rotateDirection = Vector3.zero;

        if (_isGrounded)
        {
            switch (actionForward)
            {
                case 1:
                    directionToGo = transform.forward * 1f;
                    break;
                case 2:
                    directionToGo = transform.forward * -1f;
                    break;
            }
        }
        switch (actionRotate)
        {
            case 1:
                rotateDirection = transform.up * 1f;
                break;
            case 2:
                rotateDirection = transform.up * -1f;
                break;
        }

        transform.Rotate(rotateDirection, Time.fixedDeltaTime * rotationSpeed);
        _rigidBody.AddForce(directionToGo * speed * Time.fixedDeltaTime, ForceMode.VelocityChange);
    }

    public override float[] Heuristic()
    {
        var action = new float[2];
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            action[0] = 1f;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            action[0] = 2f;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            action[1] = 1f;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            action[1] = 2f;
        }
        return action;
    }

    public override void OnEpisodeBegin()
    {
        _previousScore = _currentScore;
        numberOfGoalsCollected = 0;
        _arena.ResetArena();
        _rewardPerStep = maxStep > 0 ? -1f / maxStep : 0;
        _isGrounded = false;
    }


    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0)
            {
                _isGrounded = true;
            }
        }
        _lastContactPoint = collision.contacts.Last();
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0)
            {
                _isGrounded = true;
            }
        }
        _lastContactPoint = collision.contacts.Last();
    }

    void OnCollisionExit(Collision collision)
    {
        if (_lastContactPoint.normal.y > 0)
        {
            _isGrounded = false;
        }
    }

    public void AgentDeath(float reward)
    {
        // Debug.Log("Agent death called");
        AddReward(reward);
        _currentScore = GetCumulativeReward();
        EndEpisode();
    }

    public void AddExtraReward(float rewardFactor)
    {
        AddReward(Math.Min(rewardFactor * _rewardPerStep,-0.00001f));
    }

    public float GetPreviousScore()
    {
        return _previousScore;
    }
}


