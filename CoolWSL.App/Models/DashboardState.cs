using System.Globalization;
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
        false,
        false,
        Array.Empty<DashboardDistroRow>());

    public bool CanRefresh => !IsLoading;

    public bool HasDistroRows => Distros.Count > 0;

    public bool ShowEmptyState => !HasDistroRows;

    public bool HasSuggestedNextStep => !string.IsNullOrWhiteSpace(SuggestedNextStep);

    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningText);

    public static DashboardState Create(DashboardStatusSnapshot snapshot, DateTimeOffset refreshedAt)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var environmentStatus = snapshot.EnvironmentStatus;
        var distroInventory = snapshot.DistroInventory;
        var distros = distroInventory.Distros.Select(MapDistro).ToArray();
        var warningText = CombineDistinct(environmentStatus.DegradedReason, distroInventory.DegradedReason);
        var suggestedNextStep = CombineDistinct(environmentStatus.SuggestedNextStep, distroInventory.SuggestedNextStep);
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
            WarningText = CombineDistinct(WarningText, message),
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
            return FirstNonEmpty(distroInventory.Summary, environmentStatus.Summary, "The distro inventory is unavailable.");
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
                FirstNonEmpty(environmentStatus.Summary, distroInventory.Summary, "Install WSL to populate the dashboard inventory."));
        }

        if (environmentStatus.Availability == WslAvailability.Unavailable ||
            distroInventory.Availability == WslAvailability.Unavailable)
        {
            return (
                "WSL inventory unavailable",
                FirstNonEmpty(distroInventory.Summary, environmentStatus.Summary, "WSL inventory data is unavailable."));
        }

        if (distroInventory.IsDegraded)
        {
            return (
                "Distro inventory is limited",
                FirstNonEmpty(distroInventory.Summary, "WSL inventory details are only partially available."));
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

        return FirstNonEmpty(
            CombineDistinct(environmentStatus.Summary, includeInventorySummary ? distroInventory.Summary : null),
            "WSL dashboard data loaded.");
    }

    private static string CombineDistinct(params string?[] values)
    {
        var parts = new List<string>();

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmedValue = value.Trim();
            if (parts.Contains(trimmedValue, StringComparer.Ordinal))
            {
                continue;
            }

            parts.Add(trimmedValue);
        }

        return string.Join(" ", parts);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
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
            distro.IsDefault ? "Default" : string.Empty);
    }
}

public sealed record DashboardDistroRow(string Name, string State, string WslVersion, string DefaultLabel);