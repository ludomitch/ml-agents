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
    // enum ObjMap {Agent,
    //              Cardbox1,
    //              Cardbox2,
    //              CylinderTunnel,
    //              CylinderTunnelTransparent,
    //              DeathZone,
    //              GoodGoal,
    //              GoodGoalMulti,
    //              LObject,
    //              LObject2,
    //              Ramp,
    //              UObject,
    //              Wall,
    //              WallTransparent};
    List<string> ObjMap = new List<string> {"Agent",
                                             "Cardbox1",
                                             "Cardbox2",
                                             "CylinderTunnel",
                                             "CylinderTunnelTransparent",
                                             "DeathZone",
                                             "GoodGoal",
                                             "GoodGoalMulti",
                                             "LObject",
                                             "LObject2",
                                             "Ramp",
                                             "UObject",
                                             "Wall",
                                             "WallTransparent"};
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
        float localRot = transform.rotation.eulerAngles.y;
        sensor.AddObservation(localVel);
        sensor.AddObservation(localPos);
        sensor.AddObservation(localRot);
        // Objects in scene
        List<Vector3> objects = getFovObjects();
        // Debug.Log(objects.Count);
        for (var i = 0; i < 30; i++) {
            if (i<objects.Count){
                sensor.AddObservation(objects[i]);
            }
            else {
                sensor.AddObservation(new Vector3(-1.0f, -1.0f, -1.0f));
            }
        }

        // TODO
        // 1) Need to get all FOV objects in some format, list of array
        // 2) Get the number of objects retrieved: do len of list
        // 3) Do a add observation in batches of 9 for each 10 objects
        // 4) If num objects under 10 then loop through adding vector size 9 of 0s, use
        // -1 for class if enum is 0 indexed
        // 5) Total observations should be 97, 7 for agent + 9*10 objects

    }

    public List<Vector3> getFovObjects()
    {

        List<Vector3> obj_vectors = new List<Vector3>();

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("arena"))
        {
            Transform[] allChildren = go.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren) {
                Vector3 screenPoint = cam.WorldToViewportPoint(child.position);
                bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

                if(!(new[] {
                    "fence", "Wall", "Ground", "Cam",
                     "Fwd", "Spawn", "Screen", "Light",
                      "Arena", "Image", "Agent" }.Any(x => child.name.Contains(x))) && onScreen) {
                    // int obj_idx =  (int)Enum.Parse(typeof(ObjMap), child.name);
                    int obj_idx =  ObjMap.FindIndex(s => child.name.Contains(s));
                    obj_vectors.Add(new Vector3(obj_idx, child.transform.rotation.eulerAngles.y, 0));
                    obj_vectors.Add(child.transform.localPosition);
                    obj_vectors.Add(child.transform.localScale);

                }
            }
        }
        return obj_vectors;
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
