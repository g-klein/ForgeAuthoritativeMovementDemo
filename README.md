# ForgeAuthoritativeMovementDemo
## What is this?
This is an example of forge running an authoritative server with clients providing authoritative input.  
It features client-side prediction / reconciliation based on the principles described here: http://www.gabrielgambetta.com/fpm1.html .
The code is intended to supply a bare-bones and easy to understand framework that can be built out into a larger server-authoritative model.

_note: this uses versions 5.6.1f of Unity and version 23.02 of Forge Networking Remastered.  This project is not guaranteed to work with other versions of Forge / Unity.  Get Forge Networking Remastered here: https://github.com/BeardedManStudios/ForgeNetworkingRemastered_

## How to run it?
Simply clone the project and open it unity.  Build your own .exe through unity.  (Make sure that you have Run in background enabled in player settings during build).  
  
Run two instances of the build -- in one press host, in the other hit connect.  You should see a cube that you can move around with WASD keys. 
Press "X" in the server to teleport the cube around.  Press "X" in the client to try to teleport.  It is not seen as a valid move by the server, so you can see reconciliation kick in and move the cube back to the expected position.
  
## How it works
There are three important network objects in this scene.  
* The ```InputListener``` collects authoritative input from the client, and sends it to the server.   
* The ```GuyWithMovement``` is the script that controls the cube on the screen. This is the script performing client-side prediction, server-side authoritative movement, and reconciliation.
* The ```GameStateManager``` is the script that creates ```InputListener``` and ```GuyWithMovement``` and associates them when players connect.

I will dive into these objects in more detail in the next section.

## Deeper dive into what's happening
When the server starts it registers a method to create a ```GuyWithMovement``` whenever a player connects.  On the client side, an ```InputListener``` is spawned accross the network. These two are associated using the network ID of the connecting player.  
  
    
In the ```FixedUpdate``` of the ```InputListener```, client inputs are collected and added to the lists ```FramesToPlay``` and ```FramesToSendToServer```.  The ```FramesToPlay``` are stored to be used for client-side prediction (more on this later).  The ```FramesToSendToServer``` are sent to the server every 5 ```FixedUpdate``` loops by sending them as a ```ByteArray``` to the ```SyncInputs``` RPC.  When the server receives this RPC, it adds the recieved inputs to its own ```FramesToPlay```. 
  
  
Meanwhile, in the ```GuyWithMovement```, some work is done to find the correct ```InputListener```.  In the ```FixedUpdate``` on both servers and clients, the frames are read from ```FramesToPlay``` one at a time and go into the ```PerformMovement``` method.  The client and server keep their own local copy of the results of the movement in ```LocalMovementHistory```. The server version of this is the "authoritative" one, while the client is the holding a history of its own predictions.  
  
  
Every 5 ```FixeUpdate``` loops, the server will send its authoritative ```LocalMovementHistory``` to the client via the ```SyncMovementHistory``` RPC.  On the client, this gets stored into ```AuthoritativeMovementHistory```.  In the ```FixedUpdate``` on the client, ```PerformMovementReconciliation``` is called.  If there are any frames in ```AuthoritativeMovementHistory``` the client will perform reconciliation.  
  
  
So how does reconciliation work?  The client compares its predicted movements from ```LocalMovementHistory``` with the authoritative history from ```AuthoritativeMovementHistory```.  If the position from the authoritative history is far enough away from the client's position, it goes into a correction loop.  The client sets its position to the server's authoritative position from the history, and then replays its inputs to get a new position, thereby "reconciling" the predicted position with the server's authoritative position.

## Optomizations / limitations / considerations
This code was created for demo purposes.  While it is certainly functional, and could be the good basis to build a game off of, there are some things that were left out / unoptomized for the sake of readability.  Certain linq statements should probably be rewritten, and some code abstracted into smaller files.  
  
The most important thing to note if you are adding to this script, you need to be careful what you put in the ```PerformMovement``` method, since it may be called MANY times during reconciliation.  For example, lets say you add an ability to perform a Dash, but it costs mana.  If you do this during ```PerformMovement```, and then the inputs get replayed several times during reconciliation, the Dash ability could deplete your mana entirely while trying to reconcile position.  You would probably want to pass in a reference to amount of Mana you have,  and store it in history items.  When reconciling, you would want to pass in the Mana from movement history so as not to deplete the actual mana on your character.  This is just an example of how reconciliation can lead to unexpected game states.  Always keep this in mind when writing your movement scripts!


## Known issues  
There is an issue where the inputs to play can become backed up.  If one of the connected players has a lag spike, the inputs will be delayed when reaching the server.  Since the server plays the inputs back at a fixed rate, it will never catch up with the spike in inputs.  To get around this, the server can check if the number of inputs to play is greater than the number of inputs that come in from a single ```SyncInputs``` RPC, and if so, it can play all the extra inputs at once in order to "catch up".  This is not implemented in this example project!
