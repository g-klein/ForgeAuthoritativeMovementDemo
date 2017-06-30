using BeardedManStudios.Forge.Networking.Generated;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking;
using System;
using Assets.Scripts.Utilities;

public class InputListener : InputListenerBehavior {
    private InputFrame nextInputFrame;
    public List<InputFrame> FramesToSendToServer;
    public List<InputFrame> FramesToPlay;
    public uint FrameNumber;

    [Tooltip("FREQUENCY OF SENDING INPUTS TO SERVER, IN TERMS OF FIXED UPDATE LOOPS")]
    public int FrameSyncRate = 5;

    private void Start()
    {
        FramesToSendToServer = new List<InputFrame>();
        FramesToPlay = new List<InputFrame>();
    }

    void Update () {
        //collect the next input to send
        nextInputFrame = new InputFrame() {
            horizontalInput = Input.GetAxis("Horizontal"),
            verticalInput = Input.GetAxis("Vertical")
        };
    }

    //during fixed update, do things
    private void FixedUpdate()
    {
        FrameNumber++;
        nextInputFrame.frame = FrameNumber;

        if (networkObject.IsOwner)
        {
            FramesToSendToServer.Add(nextInputFrame);
            FramesToPlay.Add(nextInputFrame);
        }

        if(FrameNumber % FrameSyncRate == 0)
        {
            var bytes = ByteArrayUtils.ObjectToByteArray(FramesToSendToServer);
            networkObject.SendRpc("SyncInputs", Receivers.Server, new object[] { bytes });
            FramesToSendToServer.Clear();
        }
    }

    public override void SyncInputs(RpcArgs args)
    {
        var bytes = args.GetNext<Byte[]>();
        List<InputFrame> networkInputFrames = (List<InputFrame>)ByteArrayUtils.ByteArrayToObject(bytes);

        if (networkObject.IsServer)
        {
            FramesToPlay.AddRange(networkInputFrames);
        }
    }
}
