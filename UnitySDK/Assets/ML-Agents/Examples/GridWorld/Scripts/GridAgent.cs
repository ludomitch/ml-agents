﻿using System;
using UnityEngine;
using System.Linq;
using MLAgents;

public class GridAgent : Agent
{
    [Header("Specific to GridWorld")]
    private GridAcademy m_Academy;
    public float timeBetweenDecisionsAtInference;
    private float m_TimeSinceDecision;

    [Tooltip("Because we want an observation right before making a decision, we can force " +
        "a camera to render before making a decision. Place the agentCam here if using " +
        "RenderTexture as observations.")]
    public Camera renderCamera;

    [Tooltip("Selecting will turn on action masking. Note that a model trained with action " +
        "masking turned on may not behave optimally when action masking is turned off.")]
    public bool maskActions = true;
    Collider[] m_BlockTest = new Collider[8];

    private const int k_NoAction = 0;  // do nothing!
    private const int k_Up = 1;
    private const int k_Down = 2;
    private const int k_Left = 3;
    private const int k_Right = 4;

    protected override void InitializeAgent()
    {
        m_Academy = FindObjectOfType(typeof(GridAcademy)) as GridAcademy;
    }

    protected override void CollectObservations()
    {
        // There are no numeric observations to collect as this environment uses visual
        // observations.

        // Mask the necessary actions if selected by the user.
        if (maskActions)
        {
            SetMask();
        }
    }

    /// <summary>
    /// Applies the mask for the agents action to disallow unnecessary actions.
    /// </summary>
    private void SetMask()
    {
        // Prevents the agent from picking an action that would make it collide with a wall
        var position = transform.position;
        var positionX = (int)position.x;
        var positionZ = (int)position.z;
        var maxPosition = m_Academy.gridSize - 1;

        if (positionX == 0)
        {
            SetActionMask(k_Left);
        }

        if (positionX == maxPosition)
        {
            SetActionMask(k_Right);
        }

        if (positionZ == 0)
        {
            SetActionMask(k_Down);
        }

        if (positionZ == maxPosition)
        {
            SetActionMask(k_Up);
        }
    }

    // to be implemented by the developer
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        AddReward(-0.01f);
        var action = Mathf.FloorToInt(vectorAction[0]);

        var targetPos = transform.position;
        switch (action)
        {
            case k_NoAction:
                // do nothing
                break;
            case k_Right:
                targetPos = transform.position + new Vector3(1f, 0, 0f);
                break;
            case k_Left:
                targetPos = transform.position + new Vector3(-1f, 0, 0f);
                break;
            case k_Up:
                targetPos = transform.position + new Vector3(0f, 0, 1f);
                break;
            case k_Down:
                targetPos = transform.position + new Vector3(0f, 0, -1f);
                break;
            default:
                throw new ArgumentException("Invalid action value");
        }


        Physics.OverlapBoxNonAlloc(targetPos, new Vector3(0.3f, 0.3f, 0.3f), m_BlockTest);
        if (m_BlockTest.Where(col => col.gameObject.CompareTag("wall")).ToArray().Length == 0)
        {
            transform.position = targetPos;

            if (m_BlockTest.Where(col => col.gameObject.CompareTag("goal")).ToArray().Length == 1)
            {
                Done();
                SetReward(1f);
            }
            if (m_BlockTest.Where(col => col.gameObject.CompareTag("pit")).ToArray().Length == 1)
            {
                Done();
                SetReward(-1f);
            }
        }
    }

    // to be implemented by the developer
    public override void AgentReset()
    {
        m_Academy.AcademyReset();
    }

    public void FixedUpdate()
    {
        WaitTimeInference();
    }

    private void WaitTimeInference()
    {
        if (renderCamera != null)
        {
            renderCamera.Render();
        }

        if (!m_Academy.GetIsInference())
        {
            RequestDecision();
        }
        else
        {
            if (m_TimeSinceDecision >= timeBetweenDecisionsAtInference)
            {
                m_TimeSinceDecision = 0f;
                RequestDecision();
            }
            else
            {
                m_TimeSinceDecision += Time.fixedDeltaTime;
            }
        }
    }
}
