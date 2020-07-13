using System.Linq;
using System;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;
using MLAgents;
using PrefabInterface;
using MLAgents.Sensors;
using System.Collections.Generic;
using System.Diagnostics;
// using UnityEngine.CoreModule;

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

    // public RenderTexture renderTexture = new RenderTexture(84, 84, 24);
    // public byte[] rawByteData = new byte[84 * 84 * 84];
    // public Texture2D texture2D = new Texture2D(84, 84, TextureFormat.RGB24, false);
    // public Rect rect = new Rect(0, 0, 84, 84);
    private RenderTexture renderTexture;
    public int bytesPerPixel;
    private byte[] rawByteData;
    private Texture2D texture2D;
    private Rect rect;

    private Rigidbody _rigidBody;
    private bool _isGrounded;
    private ContactPoint _lastContactPoint;
    private TrainingArena _arena;
    private float _rewardPerStep;
    private Color[] _allBlackImage;
    private float _previousScore = 0;
    private float _currentScore = 0;


    public override void Initialize()
    {
        _arena = GetComponentInParent<TrainingArena>();
        _rigidBody = GetComponent<Rigidbody>();
        _rewardPerStep = maxStep > 0 ? -1f / maxStep : 0;
        renderTexture = new RenderTexture(84, 84, 24);
        rawByteData = new byte[84 * 84 * bytesPerPixel];
        texture2D = new Texture2D(84, 84, TextureFormat.RGB24, false);
        rect = new Rect(0, 0, 84, 84);
        cam.targetTexture = renderTexture;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent attributes
        // Debug.Log(transform.worldToLocalMatrix);
        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.velocity); //max vel
        float ang_vel = localVel.x/5.81f;
        float normvel = localVel.z/11.6f;
        sensor.AddObservation(ang_vel);
        sensor.AddObservation(normvel);

        // Debug.Log(wtlm);
        // All objects in scene with all necessary info
        // List<float> objects = getFovObjects();
        List<float> bboxes = run_cmd();
        foreach (float b in bboxes){
            sensor.AddObservation(b);
        }

    }

    private List<float> run_cmd()
    {
        // Setup a camera, texture and render texture
        cam.targetTexture = renderTexture;
        cam.Render();

        // Read pixels to texture
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(rect, 0, 0);
        rawByteData = ImageConversion.EncodeToJPG(texture2D);
        string fileName = "/Users/ludo/Desktop/tmp/" + Guid.NewGuid().ToString() + ".jpg";
        // string fileName = "/media/home/ludovico/aai/neuro/tmp/" + Guid.NewGuid().ToString() + ".jpg";
        File.WriteAllBytes(fileName, rawByteData); // Requires System.IO

         ProcessStartInfo start = new ProcessStartInfo();
         // start.FileName = "/media/home/ludovico/venv/bin/python3";
         start.FileName = "/Library/Frameworks/Python.framework/Versions/3.6/bin/python3";
         // start.Arguments = string.Format("{0} {1}", cmd, args);
         // start.Arguments = string.Format(
         //    "/media/home/ludovico/aai/neuro/ctrack_wall.py photo {0}", fileName);
         start.Arguments = string.Format(
            "/Users/ludo/Desktop/animalai/animalai/neuro/ctrack_wall.py photo {0}", fileName);         start.UseShellExecute = false;
         start.RedirectStandardOutput = true;
         start.RedirectStandardError = true;
         string strout;
         string strerr;
         using(Process process = Process.Start(start))
         {
             using(StreamReader reader1 = process.StandardOutput)
             {
                 strout = reader1.ReadToEnd();
                 // UnityEngine.Debug.Log(str);
             }
             using(StreamReader reader2 = process.StandardError)
             {
                 strerr = reader2.ReadToEnd();
                 // UnityEngine.Debug.Log(str);
             }
                process.WaitForExit();

         }

        if (!string.IsNullOrEmpty(strerr)) {
            File.WriteAllText("/Users/ludo/Desktop/err.txt", strerr);
        }
        // if (!string.IsNullOrEmpty(strerr)) {
        //     File.WriteAllText("/media/home/ludovico/aai/neuro/err.txt", strerr);
        // }

         // UnityEngine.Debug.Log(strerr);
        string[] tokens = strout.Split(',');
        List<float> result = tokens.Select(x => float.Parse(x)).ToList();
         // UnityEngine.Debug.Log(String.Join(",",
         //             new List<float>(result)
         //             .ConvertAll(i => i.ToString())
         //             .ToArray()));

        System.IO.File.Delete(fileName);

        return result;


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


