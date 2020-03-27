// using System.Collections;
using System.Collections.Generic;
// using System;
using UnityEngine;
using MLAgents;
using ArenaBuilders;
using UnityEngineExtensions;
using ArenasParameters;
using System.Linq;
using Holders;

public class TrainingArena : MonoBehaviour
{

    public ListOfPrefabs prefabs = new ListOfPrefabs();
    public GameObject spawnedObjectsHolder;
    public int maxSpawnAttemptsForAgent = 100;
    public int maxSpawnAttemptsForPrefabs = 20;
    public ListOfBlackScreens blackScreens = new ListOfBlackScreens();
    [HideInInspector]
    public int arenaID = -1;
    [HideInInspector]
    public Agent agent;

    private ArenaBuilder _builder;
    private ArenaConfiguration _arenaConfiguration = new ArenaConfiguration();
    private EnvironmentManager _environmentManager;
    private int _agentDecisionInterval;
    private List<Fade> _fades = new List<Fade>();
    private bool _lightStatus = true;

    internal void Awake()
    {
        _builder = new ArenaBuilder(gameObject,
                                    spawnedObjectsHolder,
                                    maxSpawnAttemptsForPrefabs,
                                    maxSpawnAttemptsForAgent);
        EnvironmentManager _environmentManager = GameObject.FindObjectOfType<EnvironmentManager>();
        if (!_environmentManager.GetConfiguration(arenaID, out _arenaConfiguration))
        {
            // Debug.Log("configuration missing for arena " + arenaID);
            _arenaConfiguration = new ArenaConfiguration(prefabs);
            _environmentManager.AddConfiguration(arenaID, _arenaConfiguration);
        }
        
        agent = transform.FindChildWithTag("agent").GetComponent<Agent>();
        _agentDecisionInterval =transform.FindChildWithTag("agent").GetComponent<DecisionRequester>().DecitionPeriod;
        _fades = blackScreens.GetFades();
    }

    public void ResetArena()
    {
        DestroyImmediate(transform.FindChildWithTag("spawnedObjects"));

        ArenaConfiguration newConfiguration;
        if (_environmentManager.GetConfiguration(arenaID, out newConfiguration))
        {
            _arenaConfiguration = newConfiguration;
            if (_arenaConfiguration.toUpdate)
            {
                _arenaConfiguration.SetGameObject(prefabs.GetList());
                _builder.Spawnables = _arenaConfiguration.spawnables;
                _arenaConfiguration.toUpdate = false;
                agent.maxStep = _arenaConfiguration.T * _agentDecisionInterval;
            }
        }
        _builder.Build();
        _arenaConfiguration.lightsSwitch.Reset();
    }

    public void UpdateLigthStatus()
    {
        int stepCount = agent.StepCount;
        bool newLight = _arenaConfiguration.lightsSwitch.LightStatus(stepCount, _agentDecisionInterval);
        if (newLight != _lightStatus)
        {
            _lightStatus = newLight;
            foreach (Fade fade in _fades)
            {
                fade.StartFade();
            }
        }
    }

    public void SetResolution(int resolutionWidth, int resolutionHeight)
    {
        // TODO: set the camera resolution
    }

}
