using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Artemis.Core;
using Artemis.Core.Modules;
using Artemis.Core.Services;
using Artemis.Plugins.Curators.Curations;
using Artemis.UI.Shared.Utilities;
using Artemis.WebClient.Workshop;
using Artemis.WebClient.Workshop.Handlers.InstallationHandlers;
using Artemis.WebClient.Workshop.Services;
using JetBrains.Annotations;

namespace Artemis.Plugins.Curators;

[PublicAPI]
public class CuratorsModule(
    PluginSettings pluginSettings,
    ProfileEntryInstallationHandler profileInstaller,
    IWorkshopService workshopService,
    IWorkshopClient client
) : Module
{
    private readonly Dictionary<string, List<ProfileDetection>> _processProfiles = new();
    private CancellationTokenSource _cancellationTokenSource = new();

    public override List<IModuleActivationRequirement>? ActivationRequirements => null;

    public override void Enable()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        //TODO read curation list from pluginSettings
        var json = File.ReadAllText("curation.json");
        var curation = JsonSerializer.Deserialize(json, JsonSourceContext.Default.Curation);

        foreach (var profileDetection in EnumerateEntries(curation))
        {
            var entryDetails = profileDetection.Entry;
            if (entryDetails.LatestRelease == null)
            {
                // entry doesn't have release
                continue;
            }

            var installedEntry = workshopService.GetInstalledEntry(entryDetails.Id);
            if (installedEntry != null)
            {
                var installedDate = installedEntry.InstalledAt;
                var latestReleaseDate = entryDetails.LatestRelease.CreatedAt;
                if (installedDate > latestReleaseDate)
                {
                    // no update needed
                    continue;
                }
            }

            var processName = profileDetection.ProcessName;
            if (!_processProfiles.TryGetValue(processName, out var list))
            {
                list = new();
                _processProfiles.Add(processName, list);
            }

            list.Add(profileDetection);
        }

        ProcessMonitor.ProcessStarted += ProcessMonitorOnProcessStarted;
    }

    public override void Disable()
    {
        ProcessMonitor.ProcessStarted -= ProcessMonitorOnProcessStarted;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public override void Update(double deltaTime)
    {
        // unused
    }

    private IEnumerable<ProfileDetection> EnumerateEntries(Curation? curation)
    {
        if (curation == null)
        {
            yield break;
        }

        foreach (var curationProfile in curation.Profiles)
        {
            foreach (var triggerData in curationProfile.ProfileTriggers)
            {
                var getEntryOp = client.GetEntryById.ExecuteAsync(curationProfile.WorkshopId, _cancellationTokenSource.Token).Result;
                var entry = getEntryOp.Data?.Entry;

                if (entry == null)
                {
                    continue;
                }

                yield return new ProfileDetection(triggerData.ProcessName, Trigger, entry);

                //TODO check for title name if triggerData has it
                bool Trigger(string processData) => true;
            }
        }
    }

    private async void ProcessMonitorOnProcessStarted(object? sender, ProcessEventArgs e)
    {
        if (!_processProfiles.TryGetValue(e.ProcessInfo.ProcessName, out var profiles))
        {
            return;
        }

        var profileDetection = profiles.FirstOrDefault(x => x.DetectionFunc(e.ProcessInfo.ProcessName));
        if (profileDetection == null)
        {
            return;
        }

        var entry = profileDetection.Entry;
        if (entry.LatestRelease == null)
        {
            return;
        }

        await profileInstaller.InstallAsync(entry, entry.LatestRelease, new Progress<StreamProgress>(), _cancellationTokenSource.Token);

        profiles.Remove(profileDetection);
    }
}