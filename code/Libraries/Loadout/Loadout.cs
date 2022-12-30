using Sandbox;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TFS2;

public partial class Loadout : BaseNetworkable
{
	public static Loadout LocalLoadout => ForClient( Game.LocalClient );
	static Dictionary<IClient, Loadout> All { get; set; } = new();

	public IClient Client { get; set; }
	public LoadoutState State { get; set; }

	LoadoutData Data { get; set; }
	TimeSince TimeSinceDataUpdated { get; set; }

	public enum LoadoutState
	{
		/// <summary>
		/// Loadout is invalid, we can't use it, need to request data ASAP.
		/// </summary>
		Invalid,
		/// <summary>
		/// We are currently waiting from the client for input.
		/// </summary>
		Loading,
		/// <summary>
		/// We have failed to load inventory data, we can't use it and we need to try to reload it sometimes in the future.
		/// </summary>
		Failed,
		/// <summary>
		/// Loadout is loaded, we can use it.
		/// </summary>
		Loaded,
		/// <summary>
		/// Loadout is loaded and we can use it, but it might be outdated, so better request again next time.
		/// </summary>
		Outdated,
		/// <summary>
		/// Loadout is not available for this client. (Probably a Bot)
		/// </summary>
		Unavailable
	}

	/// <summary>
	/// Retrieve loadout data for a client, both on server and client.
	/// </summary>
	/// <param name="client"></param>
	/// <returns></returns>
	public static Loadout ForClient( IClient client )
	{
		if ( client == null )
			return null;

		if ( All.TryGetValue( client, out var loadout ) )
			return loadout;

		var el = new Loadout( client );
		All[client] = el;

		return el;
	}

	private Loadout( IClient client )
	{
		State = LoadoutState.Invalid;
		Client = client;
	}

	// Invalidates the loadout, we will request it again next time we need it.
	public void Invalidate()
	{
		State = LoadoutState.Invalid;
		Data = null;
	}

	/// <summary>
	/// Load the loadout from the appropriate source if not yet loaded.
	/// </summary>
	/// <returns></returns>
	public async Task Load()
	{
		State = LoadoutState.Loaded;
		if ( !NeedsReload() )
			return;

		State = LoadoutState.Loading;

		if ( Client.IsBot )
		{
			// This client is a bot, don't bother requesting loadout...
			State = LoadoutState.Unavailable;
		}
		else
		{
			if ( Game.IsClient )
			{
				// if we're on the client, load data from disk.
				LoadDataFromDisk();
			}
			else
			{
				// if we're on server, request data from client.
				await RequestDataFromClientAsync();
			}

			if ( Data == null ) State = LoadoutState.Failed;
			else State = LoadoutState.Loaded;
		}
	}

	public bool NeedsReload()
	{
		if ( State == LoadoutState.Loading )
			return false;

		// This loadout belongs to a bot, dont need to reload it.
		if ( State == LoadoutState.Unavailable )
			return false;

		return !IsDataValid();
	}

	public bool IsDataValid()
	{
		if ( Data == null )
			return false;

		if ( State == LoadoutState.Loaded )
			return true;

		if ( State == LoadoutState.Outdated )
			return TimeSinceDataUpdated < mp_loadout_max_outdated_time;

		return false;
	}

	/// <summary>
	/// Called when loadout is available.
	/// </summary>
	public virtual void OnAvailable()
	{

	}

	/// <summary>
	/// Called when loadout is available.
	/// </summary>
	public virtual void OnUpdated()
	{
		TimeSinceDataUpdated = 0;
	}

	public async Task<WeaponData> GetLoadoutItem( PlayerClass pclass, TFWeaponSlot slot )
	{
		if ( pclass == null )
			return null;

		var defaultItem = pclass.GetDefaultWeaponForSlot( slot );

		// If there is no default item, dont even bother checking for loadout data.
		if ( defaultItem == null )
			return null;

		// find the item from loadout
		await Load();

		// Loadout data was unavailable, use stock.
		if ( !IsDataValid() )
			return defaultItem;

		if ( Data.Classes == null )
			Data.Classes = new();

		// loadout data doesn't contain anything for this class.
		if ( !Data.Classes.TryGetValue( pclass.ResourceName, out var classData ) )
			return defaultItem;

		// loadout doesn't contain anything in this slot.
		if ( !classData.TryGetValue( (int)slot, out var weaponname ) )
			return defaultItem;

		var weapondata = WeaponData.All.Find( x => x.ResourceName == weaponname );

		// No data for current weapon.
		if ( weapondata == null )
			return defaultItem;

		// Check if whatever we have in the loadout is something we can actually wear.
		if ( !weapondata.CanBeOwnedByPlayerClass( pclass ) )
			return defaultItem;

		return weapondata;
	}

	/// <summary>
	/// Amount of time that server can trust outdated data.
	/// </summary>
	[ConVar.Replicated] public static float mp_loadout_max_outdated_time { get; set; } = 5;
}

public class LoadoutData
{
	public Dictionary<string, Dictionary<int, string>> Classes { get; set; }
}
