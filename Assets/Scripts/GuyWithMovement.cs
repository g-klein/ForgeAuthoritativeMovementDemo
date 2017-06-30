using BeardedManStudios.Forge.Networking.Generated;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BeardedManStudios.Forge.Networking;
using System;
using Assets.Scripts.Models;
using Assets.Scripts.Utilities;

public class GuyWithMovement : GuyWithMovementBehavior
{
    private bool IsLocalOwner;
    private InputListener inputListener;
    public float speed = 35f;
    public List<MovementHistoryItem> LocalMovementHistory;
    public List<MovementHistoryItem> AuthoritativeMovementHistory;
    public uint currentFrame = 0;

    [Tooltip("FREQUENCY OF SEND HISTORY FROM THE SERVER, IN TERMS OF FIXED UPDATE LOOPS")]
    public int FrameSyncRate = 5;

    public void Start()
    {
        LocalMovementHistory = new List<MovementHistoryItem>();
    }

    void FixedUpdate()
    {
        currentFrame++;

        IsLocalOwner = networkObject.MyPlayerId == networkObject.inputOwnerId;
        //find the input listener that corresponds with this guy
        if (inputListener == null) {
            inputListener = FindObjectsOfType<InputListener>()
                .FirstOrDefault(x => x.networkObject.Owner.NetworkId == this.networkObject.inputOwnerId);
        }

        //Server sets position on the network
        if (!networkObject.IsServer && !IsLocalOwner)
        {
            transform.position = networkObject.position;
        }

        //Authoritative movement / client side prediction
        if (networkObject.IsServer || IsLocalOwner)
        {
            if (inputListener != null && inputListener.FramesToPlay.Count() > 0)
            {
                var frame = inputListener.FramesToPlay[0];
                PerformMovement(frame);
                LocalMovementHistory.Add(GetMovementHistoryItem(frame));
                inputListener.FramesToPlay.RemoveAt(0);
            }
        }

        if (IsLocalOwner)
        {
            PerformMovementReconciliation();
        }

        if (networkObject.IsServer)
        {
            networkObject.position = transform.position;
            if (currentFrame % FrameSyncRate == 0)
            {
                var bytes = ByteArrayUtils.ObjectToByteArray(LocalMovementHistory);
                networkObject.SendRpc(RPC_SYNC_MOVEMENT_HISTORY, Receivers.All, new object[] { bytes });
                LocalMovementHistory.Clear();
            }
        }

        //hack to allow you to teleport on local client only
        if (Input.GetKeyDown("x"))
        {
            var amount = UnityEngine.Random.Range(-10, -1);
            transform.Translate(new Vector3(UnityEngine.Random.Range(-5, -1), 0, UnityEngine.Random.Range(1, 5)));
        }
    }

    private void PerformMovementReconciliation()
    {
        //loop through history items and do the reconciliation
        while (AuthoritativeMovementHistory.Count() > 0)
        {
            var serverItem = AuthoritativeMovementHistory[0];
            try
            {
                //check the distance for this frame between client history and server history
                var localItem = LocalMovementHistory.FirstOrDefault(x => x.frame == serverItem.frame);
                var distance = GetHistoryDistance(serverItem, localItem);

                //if the distance is too far, we need to reconcile
                if (distance > 0.6f)
                {
                    transform.position = new Vector3(serverItem.xPosition, serverItem.yPosition, serverItem.zPosition);
                    var itemsToReplay = LocalMovementHistory.Where(x => x.frame >= serverItem.frame);
                    foreach (var historyItemToReconcile in itemsToReplay)
                    {
                        PerformMovement(historyItemToReconcile.inputFrame);
                    }
                }

                LocalMovementHistory.Remove(localItem);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                AuthoritativeMovementHistory.Remove(serverItem);
            }

            AuthoritativeMovementHistory.Remove(serverItem);
        }
    }

    private float GetHistoryDistance(MovementHistoryItem serverItem, MovementHistoryItem localItem)
    {
        var serverPosition = new Vector3(serverItem.xPosition, serverItem.yPosition, serverItem.zPosition);
        var localPosition = new Vector3(localItem.xPosition, localItem.yPosition, localItem.zPosition);
        return Vector3.Distance(localPosition, serverPosition);
    }

    private void PerformMovement(InputFrame frame)
    {
        if (frame != null)
        {
            transform.Translate(
                new Vector3(frame.horizontalInput * Time.fixedDeltaTime * speed,
                0,
                frame.verticalInput * Time.fixedDeltaTime * speed));
        }
    }

    private MovementHistoryItem GetMovementHistoryItem(InputFrame inputFrame)
    {
        MovementHistoryItem movementHistoryItem = new MovementHistoryItem()
        {
            xPosition = transform.position.x,
            yPosition = transform.position.y,
            zPosition = transform.position.z,
            frame = inputFrame.frame,
            inputFrame = inputFrame
        };

        return movementHistoryItem;
    }

    private void OnGUI()
    {
        var GuiString = "Am I the local owner of the guy? " + IsLocalOwner + "\r\n";
        GuiString += "Cube ID: " + networkObject.NetworkId + "\r\n";
        GuiString += "My cube position: " + transform.position + "\r\n";
        GuiString += "Press X to try to teleport!";
        GUI.Label(new Rect(250 * networkObject.inputOwnerId, 5, 800, 800), GuiString);
    }

    public override void SyncMovementHistory(RpcArgs args)
    {
        var bytes = args.GetNext<Byte[]>();
        List<MovementHistoryItem> historyFrames = (List<MovementHistoryItem>)ByteArrayUtils.ByteArrayToObject(bytes);
        if (IsLocalOwner)
        {
            AuthoritativeMovementHistory.AddRange(historyFrames);
        }
    }
}
