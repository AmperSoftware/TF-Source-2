using Sandbox;
using Editor;

namespace TFS2;

/// <summary>
/// Extends trigger_multiple to add support for custom trigger filter entities. 
/// </summary>
[Library( "tf_trigger_multiple_filtered" )]
[Title("Trigger Multiple - Filtered")]
[Category("Filters")]
[Icon("done_all")]
[HammerEntity]
partial class TriggerMultipleFiltered : TriggerMultiple
{
	[Property( "filter_name", Title = "Filter Name" ), FGDType( "target_destination" )]
	public string FilterName { get; set; }
	public Filter Filter { get; set; }


	[Event.Entity.PostSpawn]
	public void OnLevelCreated()
	{
		Filter = FindByName( FilterName ) as Filter;

		if ( Filter == null ) 
			Log.Error( $"Filter entity is not configured for tf_trigger_multiple_filtered ({this})." );
	}

	public override bool PassesTriggerFilters( Entity other )
	{
		if ( !base.PassesTriggerFilters( other ) ) 
			return false;

		if ( Filter == null ) 
			return true;

		return Filter.Test( other );
	}
}
