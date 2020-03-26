using UnityEngine;
using MLAgents;
using MLAgents.SideChannels;
using System.Text;
using System;

public class ArenasParametersSideChannel : SideChannel
{

    public bool arenasParametersToUpdate = false;
    public String arenasParametersProtoString = "";

    public ArenasParametersSideChannel()
    {
        ChannelId = new Guid("9c36c837-cad5-498a-b675-bc19c9370072");
    }

    public override void OnMessageReceived(IncomingMessage msg)
    {
        arenasParametersProtoString = msg.ReadString();
        arenasParametersToUpdate = true;
    }

    // TODO: maybe add feedback on which items haven't been spawned ??
    
    // public void SendDebugStatementToPython(string logString, string stackTrace, LogType type)
    // {
    //     if (type == LogType.Error)
    //     {
    //         var stringToSend = type.ToString() + ": " + logString + "\n" + stackTrace;
    //         using (var msgOut = new OutgoingMessage())
    //         {
    //             msgOut.WriteString(stringToSend);
    //             QueueMessageToSend(msgOut);
    //         }
    //     }
    // }
}