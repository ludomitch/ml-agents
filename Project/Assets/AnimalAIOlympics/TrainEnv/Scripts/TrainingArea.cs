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

public class TrainingArena : Area
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
    // private ArenasConfigurations _arenasConfigurations;
    private int _agentDecisionInterval;
    private List<Fade> _fades = new List<Fade>();
    private bool _lightStatus = true;

    public void Awake()
    {
        _builder = new ArenaBuilder(gameObject,
                                    spawnedObjectsHolder,
                                    maxSpawnAttemptsForPrefabs,
                                    maxSpawnAttemptsForAgent);
        _arenasConfigurations = GameObject.FindObjectOfType<Academy>().arenasConfigurations;
        if (!_arenasConfigurations.configurations.TryGetValue(arenaID, out _arenaConfiguration))
        {
            // Debug.Log("configuration missing for arena " + arenaID);
            _arenaConfiguration = new ArenaConfiguration(prefabs);
            _arenasConfigurations.configurations.Add(arenaID, _arenaConfiguration);
        }
        agent = transform.FindChildWithTag("agent").GetComponent<Agent>();
        _agentDecisionInterval = agent.agentParameters.numberOfActionsBetweenDecisions;
        _fades = blackScreens.GetFades();
    }

    public override void ResetArena()
    {
        DestroyImmediate(transform.FindChildWithTag("spawnedObjects"));

        ArenaConfiguration newConfiguration;
        if (_arenasConfigurations.configurations.TryGetValue(arenaID, out newConfiguration))
        {
            _arenaConfiguration = newConfiguration;
            if (_arenaConfiguration.toUpdate)
            {
                _arenaConfiguration.SetGameObject(prefabs.GetList());
                _builder.Spawnables = _arenaConfiguration.spawnables;
                _arenaConfiguration.toUpdate = false;
                agent.agentParameters.maxStep = _arenaConfiguration.T * _agentDecisionInterval;
            }
        }
        _builder.Build();
        _arenaConfiguration.lightsSwitch.Reset();
    }

    public void UpdateLigthStatus()
    {
        int stepCount = agent.GetStepCount();
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
