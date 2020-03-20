using MLAgents;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControls : MonoBehaviour
{

    private Camera _cameraAbove;
    private Camera _cameraAgent;
    // private Camera _cameraBlack;
    private Camera _cameraFollow;
    private Agent _agent;
    private Text _score;
    private int _numActive = 0;
    private Dictionary<int, Camera> _cameras;
    public bool activate = false;
    public float prevScore = 0;

    void Start()
    {
        _agent = GameObject.FindGameObjectWithTag("agent").GetComponent<Agent>();
        _cameraAbove = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        _cameraAgent = _agent.transform.Find("AgentCamMid").GetComponent<Camera>();
        _cameraFollow = GameObject.FindGameObjectWithTag("camBase").GetComponent<Camera>();
        // _cameraBlack = GameObject.FindGameObjectWithTag("camBlack").GetComponent<Camera>();
        _score = GameObject.FindObjectOfType<Text>();
        
        _cameraAbove.enabled = true;
        _cameraAgent.enabled = false;
        // _cameraBlack.enabled = false;
        _cameraFollow.enabled = false;

        _cameras = new Dictionary<int, Camera>();
        _cameras.Add(0, _cameraAbove);
        _cameras.Add(1, _cameraAgent);
        _cameras.Add(2, _cameraFollow);
        _numActive = 0;
        

    }

    void FixedUpdate()
    {
        if (activate)
        {
            bool cDown = Input.GetKeyDown(KeyCode.C);
            if (cDown)
            {
                _cameras[_numActive].enabled = false;
                _numActive = (_numActive + 1) %3;
                _cameras[_numActive].enabled = true;
            }
            // else if (!_cameraAbove.enabled)
            // {
            //     if (!_agent.LightStatus() && _cameras[_numActive].enabled)
            //     {
            //         _cameras[_numActive].enabled = false;
            //         // _cameraBlack.enabled = true;
            //     }
            //     if (_agent.LightStatus() && _cameras[_numActive].enabled)
            //     {
            //         _cameras[_numActive].enabled = true;
            //         // _cameraBlack.enabled = false;
            //     }
            // }
            if (Input.GetKeyDown(KeyCode.R))
            {
                GameObject.FindObjectOfType<Agent>().Done();
            }
            _score.text = "Prev reward: "+ prevScore.ToString("0.000")+ "\n"
                            + "Reward: "+ GameObject.FindObjectOfType<Agent>()
                                                .GetCumulativeReward().ToString("0.000");
        }
    }
}
