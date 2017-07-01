# ForgeAuthoritativeMovementDemo
## What is this?
This is an example of forge running an authoritative server with clients providing authoritative input.  
It features client-side prediction / reconciliation based on the principles described here: http://www.gabrielgambetta.com/fpm1.html .
The code is intended to supply a bare-bones and easy to understand framework that can be built out into a larger server-authoritative model.

## How to run it?
Simply clone the project and open it unity.  Build your own .exe through unity.  (Make sure that you have Run in background enabled in player settings during build).  
  
Run two instances of the build -- in one press host, in the other hit connect.  You should see a cube that you can move around with WASD keys. 
Press "X" in the server to teleport the cube around.  Press "X" in the client to try to teleport.  It is not seen as a valid move by the server, so you can see reconciliation kick in and move the cube back to the expected position.
  
## How it works
There are three important network objects in this scene.  
* The ```InputListener``` collects authoritative input from the client, and sends it to the server.   
* The ```GuyWithMovement``` is the script that controls the cube on the screen. This is the script performing client-side interpolation, server-side authoritative movement, and reconciliation.
* The ```GameStateManager``` is the script that creates ```InputListener``` and ```GuyWithMovement``` and associates them when players connect.

I will dive into these objects in more detail in the next section.

## Deeper dive into what's happening
When the server starts it registers a method to create a ```GuyWithMovement``` whenever a player connects.  On the client side, an ```InputListener``` is spawned accross the network. These two are associated using the network ID of the connecting player.  
  
    
In the ```FixedUpdate``` of the ```InputListener```, client inputs are collected and added to the lists ```FramesToPlay``` and ```FramesToSendToServer```.  The ```FramesToPlay``` are stored to be used for client-side prediction (more on this later).  The ```FramesToSendToServer``` are sent to the server every 5 ```FixedUpdate``` loops by sending them as a ```ByteArray``` to the ```SyncInputs``` RPC.  When the server receives this RPC, it adds the recieved inputs to its own ```FramesToPlay```. 
  
  
Meanwhile, in the ```GuyWithMovement```, some work is done to find the correct ```InputListener```.  In the ```FixedUpdate``` on both servers and clients, the frames are read from ```FramesToPlay``` one at a time and go into the ```PerformMovement``` method.  The client and server keep their own local copy of the results of the movement in ```LocalMovementHistory```. The server version of this is the "authoritative" one, while the client is the holding a history of its own predictions.  
  
  
Every 5 ```FixeUpdate``` loops, the server will send its authoritative ```LocalMovementHistory``` to the client via the ```SyncMovementHistory``` RPC.  On the client, this gets stored into ```AuthoritativeMovementHistory```.  In the ```FixedUpdate``` on the client, ```PerformMovementReconciliation``` is called.  If there are any frames in ```AuthoritativeMovementHistory``` the client will perform reconciliation.  
  
  
So how does reconciliation work?  The client compares its predicted movements from ```LocalMovementHistory``` with the authoritative history from ```AuthoritativeMovementHistory```.  If the position from the authoritative history is far enough away from the client's position, it goes into a correction loop.  The client sets its position to the server's authoritative position from the history, and then replays its inputs to get a new position, thereby "reconciling" the predicted position with the server's authoritative position.
