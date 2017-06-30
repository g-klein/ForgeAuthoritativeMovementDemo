using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;
using UnityEngine;

namespace BeardedManStudios.Forge.Networking.Generated
{
	[GeneratedRPC("{\"types\":[]")]
	[GeneratedRPCVariableNames("{\"types\":[]")]
	public abstract partial class GameStateManagerBehavior : NetworkBehavior
	{
		public GameStateManagerNetworkObject networkObject = null;

		public override void Initialize(NetworkObject obj)
		{
			// We have already initialized this object
			if (networkObject != null && networkObject.AttachedBehavior != null)
				return;
			
			networkObject = (GameStateManagerNetworkObject)obj;
			networkObject.AttachedBehavior = this;

			base.SetupHelperRpcs(networkObject);
			networkObject.RegistrationComplete();

			MainThreadManager.Run(NetworkStart);

			networkObject.onDestroy += DestroyGameObject;

			if (!obj.IsOwner)
			{
				if (!skipAttachIds.ContainsKey(obj.NetworkId))
					ProcessOthers(gameObject.transform, obj.NetworkId + 1);
				else
					skipAttachIds.Remove(obj.NetworkId);
			}
		}

		public override void Initialize(NetWorker networker)
		{
			Initialize(new GameStateManagerNetworkObject(networker, createCode: TempAttachCode));
		}

		private void DestroyGameObject()
		{
			MainThreadManager.Run(() => { Destroy(gameObject); });
			networkObject.onDestroy -= DestroyGameObject;
		}

		public override NetworkObject CreateNetworkObject(NetWorker networker, int createCode)
		{
			return new GameStateManagerNetworkObject(networker, this, createCode);
		}


		// DO NOT TOUCH, THIS GETS GENERATED PLEASE EXTEND THIS CLASS IF YOU WISH TO HAVE CUSTOM CODE ADDITIONS
	}
}