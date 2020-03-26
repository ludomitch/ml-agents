using System;
using System.Linq;
using UnityEngine;
using MLAgents;
using ArenasParameters;
using UnityEngineExtensions;
using MLAgents.SideChannels;

public class EnvironmentManager : MonoBehaviour
{

    public GameObject arena;
    public int maximumResolution = 512;
    public int minimumResolution = 4;

    private FloatPropertiesChannel _ResetParameters;
    private TrainingArea[] _areas;
    private Agent _agent;
    private bool _firstReset = true;
    private ArenasConfigurations _arenasConfigurations = new ArenasConfigurations();
    private ArenasParametersSideChannel _arenasParametersSideChannel;

    public void Awake()
    {
        _arenasParametersSideChannel = new ArenasParametersSideChannel();
        Academy.Instance.RegisterSideChannel(_arenasParametersSideChannel);
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        
    }

    public void EnvironmentReset()
    {
        // TODO: check the side channel for the initial configuration containing:
        //  - whether to train or play
        //  - if play set the behavior to heuristic only
        //  - if train get all the parameters (number of agents, resolution, others?)
        if (_firstReset)
        {
            _ResetParameters = Academy.Instance.FloatProperties;
            bool playerMode = (_ResetParameters.GetPropertyWithDefault("playerMode", 1f)) > 0;
            bool inferenceMode = (_ResetParameters.GetPropertyWithDefault("inferenceMode", 0f)) > 0;
            bool receiveConfiguration = (_ResetParameters.GetPropertyWithDefault("receiveConfiguration", 0f)) > 0;
            int numberOfArenas = (int)(_ResetParameters.GetPropertyWithDefault("numberOfArenas", 1f));
            int resolutionWidth = (int)(_ResetParameters.GetPropertyWithDefault("resolutionWidth", 84f));
            int resolutionHeight = (int)(_ResetParameters.GetPropertyWithDefault("resolutionHeight", 84f));

            resolutionWidth = Math.Max(minimumResolution, Math.Min(maximumResolution, resolutionWidth));
            resolutionHeight = Math.Max(minimumResolution, Math.Min(maximumResolution, resolutionHeight));
            numberOfArenas = playerMode ? 1 : numberOfArenas;

            _areas = new TrainingArea[numberOfArenas];
            InstantiateArenas(numberOfArenas);
            ConfigureIfPlayer(playerMode, inferenceMode, receiveConfiguration);
            ChangeResolution(resolutionWidth, resolutionHeight);
            _firstReset = false;
        }

        // bool receiveConfiguration = false;
        // int numberOfArenas = -1;
        // int resolution = 84;
        // ParseArguments(ref receiveConfiguration, ref numberOfArenas, ref resolution);

        // if (numberOfArenas == -1 || externalInferenceMode)
        // {
        //     playerMode = true;
        //     numberOfArenas = 1;
        // }

        // if (Application.isEditor)
        // {
        //     // playerMode = true;
        //     playerMode = false;
        //     externalInferenceMode = true;
        //     numberOfArenas = 4;
        //     receiveConfiguration = true;
        //     // resolution = 126;
        // }

        // ChangeResolution(resolution);

        // _arenas = new TrainingArea[numberOfArenas];
        // arenasConfigurations.numberOfArenas = numberOfArenas;
        // InstantiateArenas(numberOfArenas);
        // ConfigureIfPlayer(receiveConfiguration);
    }

    private void InstantiateArenas(int numberOfArenas)
    {
        // We organize the arenas in a grid and position the main camera at the center, high enough
        // to see all arenas at once

        Vector3 boundingBox = arena.GetBoundsWithChildren().extents;
        float width = 2 * boundingBox.x + 5f;
        float height = 2 * boundingBox.z + 5f;
        int n = (int)Math.Round(Math.Sqrt(numberOfArenas));

        for (int i = 0; i < numberOfArenas; i++)
        {
            float x = (i % n) * width;
            float y = (i / n) * height;
            GameObject arenaInst = Instantiate(arena, new Vector3(x, 0f, y), Quaternion.identity);
            _areas[i] = arenaInst.GetComponent<TrainingArea>();
            _areas[i].arenaID = i;
        }

        GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition =
            new Vector3(n * width / 2, 50 * (float)n, (float)n * height / 2);
    }

    private void ChangeResolution(int resolutionWidth, int resolutionHeight)
    {
        foreach (TrainingArea area in _areas)
        {
            area.SetResolution(resolutionWidth, resolutionHeight);
        }
        // var controlledBrains = broadcastHub.broadcastingBrains.Where(
        //         x => x != null && x is LearningBrain && broadcastHub.IsControlled(x));
        // foreach (LearningBrain brain in controlledBrains)
        // {
        //     if (brain.brainParameters.cameraResolutions.Length>0)
        //     {
        //         brain.brainParameters.cameraResolutions[0].height = resolution;
        //         brain.brainParameters.cameraResolutions[0].width = resolution;
        //     }
        // }
    }

    private void ConfigureIfPlayer(bool playerMode, bool inferenceMode, bool receiveConfiguration)
    {

        // TODO

        // if (playerMode)
        // {
        //     if (!receiveConfiguration) // && !Application.isEditor)
        //     {
        //         this.broadcastHub.Clear();
        //     }
        //     if (!externalInferenceMode)
        //     {
        //         _arenas[0].gameObject.GetComponentInChildren<Agent>().brain = playerBrain;
        //     }
        //     GameObject.FindObjectOfType<PlayerControls>().activate = true;
        //     _agent = GameObject.FindObjectOfType<Agent>();
        // }
        // else
        // {
        //     GameObject.FindGameObjectWithTag("agent")
        //                 .transform.Find("AgentCamMid")
        //                 .GetComponent<Camera>()
        //                 .enabled = false;
        //     GameObject.FindGameObjectWithTag("score").SetActive(false);
        // }
    }

    public void OnDestroy()
    {
        if (Academy.IsInitialized){
            Academy.Instance.UnregisterSideChannel(_arenasParametersSideChannel);
        }
    }


    // public override void AcademyReset()
    // {
    // }

    // TODO: move lights on/off in the arena

    // public override void AcademyStep()
    // {
    //     foreach (TrainingArea arena in _arenas)
    //     {
    //         arena.UpdateLigthStatus();
    //     }
    // }
}
