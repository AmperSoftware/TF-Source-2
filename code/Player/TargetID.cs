namespace TFS2;

partial class TFPlayer : ITargetID
{
	string ITargetID.Name => Client.Name;
	string ITargetID.Avatar => $"avatar:{Client.SteamId}";
}
