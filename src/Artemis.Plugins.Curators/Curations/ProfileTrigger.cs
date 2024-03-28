namespace Artemis.Plugins.Curators.Curations;

public class ProfileTrigger
{
    public string ProcessName { get; set; } = string.Empty;
    public string? WindowTitle { get; set; }    // for games like minecraft that needs title processing
}