using System.Globalization;
using CoolWSL.Core.Helpers;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.App.Models;

public sealed record DashboardState(
    string AvailabilityLabel,
    string Summary,
    string WslVersion,
    string KernelVersion,
    string DefaultWslVersion,
    string DistroSectionSummary,
    string EmptyStateTitle,
    string EmptyStateMessage,
    string RefreshStatus,
    string? WarningText,
    string? SuggestedNextStep,
    string? DefaultDistroName,
    DateTimeOffset? LastUpdatedAt,
    bool IsLoading,
    bool HasLoaded,
    IReadOnlyList<DashboardDistroRow> Distros)
{
    private const string MissingValueLabel = "Not reported";
    private const string UnavailableValueLabel = "Unavailable";

    public static DashboardState Initial { get; } = new(
        "Loading",
        "Refresh the dashboard to inspect WSL availability and distro inventory.",
        MissingValueLabel,
        MissingValueLabel,
        MissingValueLabel,
        "The distro inventory will appear after the first refresh completes.",
        "Dashboard not loaded",
        "Refresh the dashboard to load the current WSL environment status and distro inventory.",
        "Dashboard has not been loaded yet.",
        null,
        null,
        null,
        null,
        false,
        false,
        Array.Empty<DashboardDistroRow>());

    public bool CanRefresh => !IsLoading;

    public bool HasDistroRows => Distros.Count > 0;

    public bool ShowEmptyState => !HasDistroRows;

    public bool HasSuggestedNextStep => !string.IsNullOrWhiteSpace(SuggestedNextStep);

    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningText);

    public bool HasDefaultDistro => !string.IsNullOrWhiteSpace(DefaultDistroName);

    public bool CanOpenDefaultDistro => HasDefaultDistro && HasLoaded;

    public bool CanShutdownAll => Distros.Any(static distro => distro.IsRunning);

    public static DashboardState Create(DashboardStatusSnapshot snapshot, DateTimeOffset refreshedAt)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var environmentStatus = snapshot.EnvironmentStatus;
        var distroInventory = snapshot.DistroInventory;
        var distros = distroInventory.Distros.Select(MapDistro).ToArray();
        var warningText = StringHelpers.CombineDistinct(environmentStatus.DegradedReason, distroInventory.DegradedReason);
        var suggestedNextStep = StringHelpers.CombineDistinct(environmentStatus.SuggestedNextStep, distroInventory.SuggestedNextStep);
        var (emptyStateTitle, emptyStateMessage) = BuildEmptyState(environmentStatus, distroInventory, distros.Length);

        return new(
            FormatAvailability(environmentStatus.Availability),
            BuildSummary(environmentStatus, distroInventory),
            FormatEnvironmentValue(environmentStatus.WslVersion, environmentStatus.Availability),
            FormatEnvironmentValue(environmentStatus.KernelVersion, environmentStatus.Availability),
            FormatEnvironmentValue(environmentStatus.DefaultWslVersion, environmentStatus.Availability),
            BuildDistroSectionSummary(environmentStatus, distroInventory, distros),
            emptyStateTitle,
            emptyStateMessage,
            BuildRefreshStatus(refreshedAt),
            warningText,
            suggestedNextStep,
            environmentStatus.DefaultDistroName,
            refreshedAt,
            false,
            true,
            distros);
    }

    public DashboardState WithLoading()
    {
        return this with
        {
            IsLoading = true,
            RefreshStatus = HasLoaded ? "Refreshing dashboard..." : "Loading dashboard...",
        };
    }

    public DashboardState WithRefreshFailure(string failureMessage)
    {
        var message = string.IsNullOrWhiteSpace(failureMessage)
            ? "Dashboard refresh failed."
            : failureMessage.Trim();

        return this with
        {
            AvailabilityLabel = HasLoaded ? AvailabilityLabel : "Unavailable",
            Summary = HasLoaded ? Summary : "The dashboard could not load WSL status.",
            DistroSectionSummary = HasLoaded ? DistroSectionSummary : "The distro inventory is unavailable until a refresh succeeds.",
            EmptyStateTitle = HasLoaded ? EmptyStateTitle : "Dashboard unavailable",
            EmptyStateMessage = HasLoaded ? EmptyStateMessage : message,
            RefreshStatus = "Refresh failed. Review the warning text and try again.",
            WarningText = StringHelpers.CombineDistinct(WarningText, message),
            IsLoading = false,
            HasLoaded = true,
        };
    }

    private static string BuildDistroSectionSummary(
        WslEnvironmentStatus environmentStatus,
        WslDistroInventory distroInventory,
        IReadOnlyList<DashboardDistroRow> distros)
    {
        if (distros.Count > 0)
        {
            var runningCount = distroInventory.Distros.Count(static distro => distro.IsRunning);
            var distroLabel = distros.Count == 1 ? "distro" : "distros";

            return string.Create(
                CultureInfo.InvariantCulture,
                $"{distros.Count} registered {distroLabel}. {runningCount} running.");
        }

        if (environmentStatus.Availability == WslAvailability.NotInstalled ||
            distroInventory.Availability == WslAvailability.NotInstalled)
        {
            return "WSL is not installed, so there are no distros to display.";
        }

        if (environmentStatus.Availability == WslAvailability.Unavailable ||
            distroInventory.Availability == WslAvailability.Unavailable ||
            distroInventory.IsDegraded)
        {
            return StringHelpers.FirstNonEmpty(distroInventory.Summary, environmentStatus.Summary, "The distro inventory is unavailable.");
        }

        return "No Linux distributions are installed.";
    }

    private static (string Title, string Message) BuildEmptyState(
        WslEnvironmentStatus environmentStatus,
        WslDistroInventory distroInventory,
        int distroCount)
    {
        if (distroCount > 0)
        {
            return (string.Empty, string.Empty);
        }

        if (environmentStatus.Availability == WslAvailability.NotInstalled ||
            distroInventory.Availability == WslAvailability.NotInstalled)
        {
            return (
                "WSL is not installed",
                StringHelpers.FirstNonEmpty(environmentStatus.Summary, distroInventory.Summary, "Install WSL to populate the dashboard inventory."));
        }

        if (environmentStatus.Availability == WslAvailability.Unavailable ||
            distroInventory.Availability == WslAvailability.Unavailable)
        {
            return (
                "WSL inventory unavailable",
                StringHelpers.FirstNonEmpty(distroInventory.Summary, environmentStatus.Summary, "WSL inventory data is unavailable."));
        }

        if (distroInventory.IsDegraded)
        {
            return (
                "Distro inventory is limited",
                StringHelpers.FirstNonEmpty(distroInventory.Summary, "WSL inventory details are only partially available."));
        }

        return (
            "No distros installed",
            "Install a Linux distribution to populate the dashboard inventory table.");
    }

    private static string BuildRefreshStatus(DateTimeOffset refreshedAt)
    {
        return $"Last refreshed {refreshedAt.LocalDateTime:g}.";
    }

    private static string BuildSummary(WslEnvironmentStatus environmentStatus, WslDistroInventory distroInventory)
    {
        var includeInventorySummary =
            distroInventory.Availability != WslAvailability.Available ||
            distroInventory.IsDegraded ||
            distroInventory.Distros.Count == 0;

        return StringHelpers.FirstNonEmpty(
            StringHelpers.CombineDistinct(environmentStatus.Summary, includeInventorySummary ? distroInventory.Summary : null),
            "WSL dashboard data loaded.");
    }

    private static string FormatAvailability(WslAvailability availability)
    {
        return availability switch
        {
            WslAvailability.Available => "Installed",
            WslAvailability.NotInstalled => "Not installed",
            _ => "Unavailable",
        };
    }

    private static string FormatEnvironmentValue(string? value, WslAvailability availability)
    {
        if (availability != WslAvailability.Available)
        {
            return UnavailableValueLabel;
        }

        return string.IsNullOrWhiteSpace(value) ? MissingValueLabel : value.Trim();
    }

    private static string FormatEnvironmentValue(int? value, WslAvailability availability)
    {
        if (availability != WslAvailability.Available)
        {
            return UnavailableValueLabel;
        }

        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : MissingValueLabel;
    }

    private static DashboardDistroRow MapDistro(WslDistro distro)
    {
        return new(
            distro.Name,
            distro.StateLabel,
            distro.WslVersion?.ToString(CultureInfo.InvariantCulture) ?? MissingValueLabel,
            distro.IsDefault ? "Default" : string.Empty,
            distro.IsSystemManaged ? "System-managed" : string.Empty,
            BuildCapabilityMessage(distro),
            distro.IsRunning,
            distro.IsDefault,
            distro.IsSystemManaged);
    }

    private static string BuildCapabilityMessage(WslDistro distro)
        => DistroCapabilityHelper.BuildCapabilityMessage(distro);
}

public sealed record DashboardDistroRow(
    string Name,
    string State,
    string WslVersion,
    string DefaultLabel,
    string ManagementLabel,
    string CapabilityMessage,
    bool IsRunning,
    bool IsDefault,
    bool IsSystemManaged)
{
    public bool CanOpen => true;

    public bool CanStart => !IsRunning;

    public bool CanTerminate => IsRunning && !IsSystemManaged;

    public bool CanSetDefault => !IsDefault && !IsSystemManaged;

    public bool HasManagementLabel => !string.IsNullOrWhiteSpace(ManagementLabel);

    public string OpenAutomationName => $"Open {Name}";

    public string StartAutomationName => $"Start {Name}";

    public string TerminateAutomationName => $"Terminate {Name}";

    public string SetDefaultAutomationName => $"Set {Name} as default";
}