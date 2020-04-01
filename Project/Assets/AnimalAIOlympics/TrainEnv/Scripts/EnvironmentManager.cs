using System;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.Sensors;
using ArenasParameters;
using UnityEngineExtensions;
using MLAgents.SideChannels;
using Random = UnityEngine.Random;

public class EnvironmentManager : MonoBehaviour
{

    public GameObject arenaTraining; // we need two prefabs as the Heuristic/Training attribute of Agents
    public GameObject arenaHeuristic; // is private and therefore can't be modified
    public int maximumResolution = 512;
    public int minimumResolution = 4;
    public int defaultResolution = 84;
    public GameObject playerControls;
    [HideInInspector]
    public GameObject arena;

    // private FloatPropertiesChannel _ResetParameters;
    private TrainingArena[] _arenas;
    private Agent _agent;
    private bool _firstReset = true;
    private ArenasConfigurations _arenasConfigurations;
    private ArenasParametersSideChannel _arenasParametersSideChannel;

    public void Awake()
    {
        _arenasParametersSideChannel = new ArenasParametersSideChannel();
        _arenasConfigurations = new ArenasConfigurations();

        _arenasParametersSideChannel.NewArenasParametersReceived += _arenasConfigurations.UpdateWithConfigurationsReceived;

        Academy.Instance.RegisterSideChannel(_arenasParametersSideChannel);
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
    }

    public void EnvironmentReset()
    {
        if (_firstReset)
        {
            Dictionary<string, int> environmentParameters = RetrieveEnvironmentParameters();

            int paramValue;
            bool playerMode = (environmentParameters.TryGetValue("playerMode", out paramValue) ?  paramValue : 1) > 0;
            bool inferenceMode = (environmentParameters.TryGetValue("inferenceMode", out paramValue) ?  paramValue : 0) > 0;
            bool receiveConfiguration = (environmentParameters.TryGetValue("receiveConfiguration", out paramValue) ?  paramValue : 0) > 0;
            int numberOfArenas = environmentParameters.TryGetValue("numberOfArenas", out paramValue) ?  paramValue : 1;
            int resolutionWidth = environmentParameters.TryGetValue("resolutionWidth", out paramValue) ?  paramValue : defaultResolution;
            int resolutionHeight = environmentParameters.TryGetValue("resolutionHeight", out paramValue) ?  paramValue : defaultResolution;

            resolutionWidth = Math.Max(minimumResolution, Math.Min(maximumResolution, resolutionWidth));
            resolutionHeight = Math.Max(minimumResolution, Math.Min(maximumResolution, resolutionHeight));
            numberOfArenas = playerMode ? 1 : numberOfArenas;
            arena = playerMode ? arenaHeuristic : arenaTraining;

            _arenasConfigurations.numberOfArenas = numberOfArenas;
            _arenas = new TrainingArena[numberOfArenas];
            ChangeResolution(resolutionWidth, resolutionHeight);
            InstantiateArenas(numberOfArenas);
            ConfigureIfPlayer(playerMode, inferenceMode, receiveConfiguration);
            _firstReset = false;
        }
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
            _arenas[i] = arenaInst.GetComponent<TrainingArena>();
            _arenas[i].arenaID = i;
        }

        GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition =
            new Vector3(n * width / 2, 50 * (float)n, (float)n * height / 2);
    }

    private void ChangeResolution(int resolutionWidth, int resolutionHeight)
    {
        CameraSensorComponent cameraSensor = arena.transform.FindChildWithTag("agent").GetComponent<CameraSensorComponent>();
        cameraSensor.width = resolutionWidth;
        cameraSensor.height = resolutionHeight;
    }

    public bool GetConfiguration(int arenaID, out ArenaConfiguration arenaConfiguration)
    {
        return _arenasConfigurations.configurations.TryGetValue(arenaID, out arenaConfiguration);
    }

    public void AddConfiguration(int arenaID, ArenaConfiguration arenaConfiguration)
    {
        _arenasConfigurations.configurations.Add(arenaID, arenaConfiguration);
    }

    private void ConfigureIfPlayer(bool playerMode, bool inferenceMode, bool receiveConfiguration)
    {

        GameObject.FindGameObjectWithTag("score").SetActive(playerMode);
        if (playerMode)
        {
            _agent = GameObject.FindObjectOfType<Agent>();
            playerControls.SetActive(true);
        }

        // if (playerMode)
        // {
        // arena.transform.FindChildWithTag("agent").GetComponent<Agent>().
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

    private Dictionary<string, int> RetrieveEnvironmentParameters()
    {
        Dictionary<string, int> environmentParameters = new Dictionary<string, int>();

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--playerMode":
                    environmentParameters.Add("playerMode", 1);
                    break;
                case "--inferenceMode":
                    environmentParameters.Add("inferenceMode", 0);
                    break;
                case "--receiveConfiguration":
                    environmentParameters.Add("receiveConfiguration", 0);
                    break;
                case "--numberOfArenas":
                    int nArenas = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : 1;
                    environmentParameters.Add("numberOfArenas", nArenas);
                    break;
                case "--resolutionWidth":
                    int resW = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : defaultResolution;
                    environmentParameters.Add("resolutionWidth", resW);
                    break;
                case "--resolutionHeight":
                    int resH = (i < args.Length - 1) ? Int32.Parse(args[i + 1]) : defaultResolution;
                    environmentParameters.Add("resolutionHeight", resH);
                    break;
            }
        }
        return environmentParameters;
    }

        // private Dictionary<string,int> RetrieveEnvironmentParameters()
        // {
        //     // Version using the FloatProperties side channel
        //     Dictionary<string, int> environmentParameters = new Dictionary<string, int>();

        //     FloatPropertiesChannel _ResetParameters = Academy.Instance.FloatProperties;
        //     environmentParameters.Add("playerMode",(int)_ResetParameters.GetPropertyWithDefault("playerMode", 1f));
        //     environmentParameters.Add("inferenceMode",(int)_ResetParameters.GetPropertyWithDefault("inferenceMode", 0f));
        //     environmentParameters.Add("receiveConfiguration",(int)_ResetParameters.GetPropertyWithDefault("receiveConfiguration", 0f));
        //     environmentParameters.Add("numberOfArenas",(int)(_ResetParameters.GetPropertyWithDefault("numberOfArenas", 1f)));
        //     environmentParameters.Add("resolutionWidth",(int)(_ResetParameters.GetPropertyWithDefault("resolutionWidth", 84f)));
        //     environmentParameters.Add("resolutionHeight",(int)(_ResetParameters.GetPropertyWithDefault("resolutionHeight", 84f));

        //     return environmentParameters;
        // }




        public void OnDestroy()
        {
            if (Academy.IsInitialized)
            {
                Academy.Instance.UnregisterSideChannel(_arenasParametersSideChannel);
            }
        }
    }
