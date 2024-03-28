using System;
using Artemis.WebClient.Workshop;

namespace Artemis.Plugins.Curators;

public class ProfileDetection(string processName, Func<string, bool> detectionFunc, IEntryDetails entry)
{
    public string ProcessName { get; } = processName;
    //TODO needs more information like title name if needed. Lazy Process usage would be nice
    public Func<string, bool> DetectionFunc { get; } = detectionFunc;
    public IEntryDetails Entry { get; } = entry;
}