namespace TFS2;

public class DeflectingContext
{
	public bool ShouldChangeTeam { get; set; } = true;
	public bool ShouldApplyBoost { get; set; } = true;

	public TFWeaponBase Weapon { get; set; }
	public TFPlayer Who { get; set; }
}
