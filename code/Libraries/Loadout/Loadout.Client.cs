using Sandbox;
using Amper.FPS;
using System.Text.Json;

namespace TFS2;

partial class Loadout
{
	/// <summary>
	/// Server requested us to send them loadout.
	/// </summary>
	[ClientRpc]
	public static void OnServerRequest()
	{
		Game.AssertClient();
		LocalLoadout.SendDataToServer();
	}

	public async void SendDataToServer()
	{
		Game.AssertClient();

		// Make sure our loadout is loaded before we access it.
		await Load();

		if ( IsDataValid() )
		{
			// Serialize our data
			var json = JsonSerializer.Serialize( Data );

			// compress it
			json = json.Compress();

			// Send to server.
			OnClientTransmit( json );
			return;
		}

		// our data is not valid, so send an empty response
		// because we still need to send something.
		OnClientTransmit( "" );
	}

	public bool LoadDataFromDisk()
	{
		Game.AssertClient();

		// Get data from disk.
		Data = GetDataFromDisk();

		if ( Data == null ) State = LoadoutState.Failed;
		else State = LoadoutState.Loaded;

		return Data != null;
	}

	public LoadoutData GetDataFromDisk()
	{
		Game.AssertClient();

		if ( FileSystem.Data.FileExists( "loadout.json" ) )
		{
			try
			{
				// Try to read from file storage.
				return FileSystem.Data.ReadJson<LoadoutData>( "loadout.json" );
			}
			catch { }
		}

		// If there is an error, or file doesn't exist, create a new one and return it.
		var data = new LoadoutData();
		FileSystem.Data.WriteJson( "loadout.json", data );
		return data;
	}

	public bool SetLoadoutItem( PlayerClass pclass, TFWeaponSlot slot, WeaponData weapon )
	{
		Game.AssertClient();

		if ( !IsDataValid() )
			return false;

		if ( weapon == null )
			return false;

		if ( pclass == null )
			return false;

		// Check if whatever we have in the loadout is something we can actually wear.
		if ( !weapon.CanBeOwnedByPlayerClass( pclass ) )
			return false;

		// loadout data doesn't contain anything for this class.
		if ( !Data.Classes.TryGetValue( pclass.ResourceName, out var classData ) )
		{
			classData = new();
			Data.Classes.Add( pclass.ResourceName, classData );
		}

		classData[(int)slot] = weapon.ResourceName;

		WriteDataToDisk();
		SendDataToServer();
		return true;
	}

	public void WriteDataToDisk()
	{
		Game.AssertClient();

		if ( Data == null )
			return;

		var json = JsonSerializer.Serialize( Data );
		FileSystem.Data.WriteAllText( "loadout.json", json );
	}
}
