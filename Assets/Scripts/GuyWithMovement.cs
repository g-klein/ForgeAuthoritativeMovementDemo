using BeardedManStudios.Forge.Networking.Generated;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuyWithMovement : GuyWithMovementBehavior
{
    private bool IsLocalOwner;

    private void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        IsLocalOwner = networkObject.MyPlayerId == networkObject.inputOwnerId;
    }

    void Update()
    {
        //Server sets position on the network
        if (networkObject.IsServer)
        {
            networkObject.position = transform.position;
        }

        //Authoritative movement / client side prediction
        if (networkObject.IsServer || IsLocalOwner)
        {

        }
    }
}
