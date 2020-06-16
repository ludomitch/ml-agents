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
        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.velocity); //max vel
        Vector3 localPos = transform.localPosition;
        // Vector3 localRot = transform.rotation.eulerAngles / 180.0f - Vector3.one;
        float ang_vel = localVel.x/5.81f;
        float normvel = localVel.z/11.6f;
        sensor.AddObservation(ang_vel);
        sensor.AddObservation(normvel);
        sensor.AddObservation(localPos);

        // All objects in scene with all necessary info
        List<float> objects = getFovObjects();
        // Debug.Log(objects.Count);
        for (var i = 0; i < 140; i++) { // 10 objects = 10*Vector3 objects. 29 with 0 indexing
            if (i<objects.Count){
                sensor.AddObservation(objects[i]);
            }
            else {
                sensor.AddObservation(-1.0f);
            }
        }

        // All raycast distances
        List<float> r_hits = RaycastSweep();
        foreach (float r in r_hits){
            sensor.AddObservation(r);
        }
    }

    public bool check_raycasts(List<Vector3> corners, string name){
    // public bool check_raycasts(Vector3 corners, string name){
        // Format name by removing (clone)
        string removeString = "(Clone)";
        int index = name.IndexOf(removeString);
        string cleanName = (index < 0)
            ? name
            : name.Remove(index, removeString.Length);
        // int layer_mask = LayerMask.GetMask("noray");
        foreach (Vector3 corner in corners) {
            RaycastHit hit;
            Vector3 dir = corner - cam.transform.position;

            if(Physics.Raycast(cam.transform.position, dir, out hit, 100)){
                if (hit.collider.gameObject.name.Contains(cleanName)){
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

    public List<float> getFovObjects()
    {
        // string all_objects = "";
        Vector3 localPos = transform.localPosition;
        List<float> obj_vectors = new List<float>();
        Transform parent_arena = transform.parent;
        List<string> blacklist = new List<string> {
                "fence", "WallOut", "Walls", "Ground", "Cam",
                 "Fwd", "Spawn", "Screen", "Light",
                  "Arena", "Image", "Agent", "Ramp(Clone)"};
        float percentage;
        Transform[] allChildren = parent_arena.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren) {
            if(!(blacklist.Any(x => child.name.Contains(x)))) {
            // if((whitelist.Any(x => child.name.Contains(x)))) {
                Vector3 screenPoint = cam.WorldToViewportPoint(child.position);
                bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
                if (onScreen){
                    percentage = CalcScreenPercentage(child);
                    // Debug.Log(percentage + child.name);
                }
                else {
                    percentage = (float)-1.0;
                }
                Vector3 rel_distance = transform.InverseTransformDirection((child.transform.localPosition - localPos));
                float rel_angle = Vector3.SignedAngle(rel_distance, cam.transform.forward, Vector3.up);
                int obj_idx =  ObjMap.FindIndex(s => child.name.Contains(s));
                obj_vectors.Add(child.GetInstanceID());
                obj_vectors.Add(obj_idx);
                obj_vectors.Add(percentage);
                obj_vectors.Add(child.transform.localPosition.x/40.0f);
                obj_vectors.Add(child.transform.localPosition.y/40.0f);
                obj_vectors.Add(child.transform.localPosition.z/40.0f);
                obj_vectors.Add(rel_distance.x/40.0f);
                obj_vectors.Add(rel_distance.y/40.0f);
                obj_vectors.Add(rel_distance.z/40.0f);
                obj_vectors.Add(child.transform.rotation.eulerAngles.y/180.0f);
                obj_vectors.Add(rel_angle/180.0f);
                obj_vectors.Add(child.transform.localScale.x/40.0f);
                obj_vectors.Add(child.transform.localScale.y/40.0f);
                obj_vectors.Add(child.transform.localScale.z/40.0f);

            }
        }
     
        return obj_vectors;
    }


    public List<float> RaycastSweep() 
     {
        float theAngle = 60.0f;
        float segments = 5.0f;
        float max_distance = 20.0f;
        Vector3 startPos = cam.transform.localPosition; // umm, start position !
        Vector3 targetDirStraight = Vector3.zero; // variable for calculated end position
        Vector3 targetDirDown = Vector3.zero; // variable for calculated end position

        int startAngle = Convert.ToInt32(-theAngle * 0.5f); // half the angle to the Left of the forward
        int finishAngle = Convert.ToInt32(theAngle * 0.5f); // half the angle to the Right of the forward
         
         // the gap between each ray (increment)
        int inc = Convert.ToInt32(theAngle / segments);
        RaycastHit hit;
        List<float> r_hits = new List<float>();

        List<string> blacklist = new List<string> {
                "fence", "Ground", "Cam",
                 "Fwd", "Spawn", "Screen", "Light",
                  "Arena", "Image", "Goal"};
        // string all_objects = "";

         // step through and find each target point
         for (int i = startAngle; i < finishAngle; i += inc ) // Angle from forward
         {
             targetDirStraight = (Quaternion.Euler( 0, i, 0 ) * cam.transform.forward).normalized;    
             targetDirDown = (Quaternion.Euler( 0, i, -55 ) * cam.transform.forward).normalized;    
             // Raycast between points
             if ( Physics.Raycast( startPos, targetDirStraight, out hit, max_distance ) )
             {
                if(!(blacklist.Any(x => hit.collider.gameObject.name.Contains(x)))){
                 // Debug.Log( "Hit " + hit.collider.gameObject.name );
                // all_objects += hit.collider.gameObject.name + hit.distance.ToString() +  "-";                    
                    r_hits.Add(hit.distance/max_distance);
                }
                else {r_hits.Add(1.0f);}
             }
            else {r_hits.Add(1.0f);}

             if ( Physics.Raycast( startPos, targetDirDown, out hit, max_distance ) )
             {
                if(!(blacklist.Any(x => hit.collider.gameObject.name.Contains(x)))){
                 // Debug.Log( "Hit " + hit.collider.gameObject.name );
                // all_objects += hit.collider.gameObject.name + hit.distance.ToString() +  "-";                    
                    r_hits.Add(hit.distance/max_distance);
                }
                else {r_hits.Add(1.0f);}

             }
            else {r_hits.Add(1.0f);}

             // to show ray just for testing
             // Debug.DrawLine( startPos, targetPos, Color.green ); 
         }    
        // if (!string.IsNullOrEmpty(all_objects)) {
        //     Debug.Log(all_objects);
        // }
         // foreach (float i in r_hits){
         //    all_objects += i.ToString();
         // }
         // Debug.Log(r_hits.Count);
         return r_hits;
     }

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


