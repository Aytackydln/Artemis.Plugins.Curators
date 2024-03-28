namespace Artemis.Plugins.Curators.Curations;

public class Profile
{
    public int WorkshopId { get; set; }
    public ProfileTrigger[] ProfileTriggers { get; set; } = [];
}