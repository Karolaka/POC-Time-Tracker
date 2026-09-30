using Timesheet.Domain;

namespace Timesheet.Web;

/// <summary>Small UI-only helpers for consistent, subtle pastel color-coding of organizations
/// across grids and cards (badges, header accents, etc.).</summary>
public static class UiHelpers
{
    /// <summary>Soft badge CSS class for a given organization, used to give grids/cards a light
    /// visual color cue per partner organization without being heavy-handed.</summary>
    public static string GetOrgBadgeClass(OrganizationCode organization) => organization switch
    {
        OrganizationCode.Jacobs => "badge-soft-org-jacobs",
        OrganizationCode.Rumble => "badge-soft-org-rumble",
        OrganizationCode.AECOM => "badge-soft-org-aecom",
        OrganizationCode.Metrolink => "badge-soft-org-metrolink",
        _ => "badge-soft-neutral"
    };

    /// <summary>Subtle pastel accent class applied to program/organization group headers
    /// (e.g. the Approvals/Final Approvals program cards) so each organization is easy to tell apart at a glance.</summary>
    public static string GetOrgAccentClass(OrganizationCode organization) => organization switch
    {
        OrganizationCode.Jacobs => "org-accent-jacobs",
        OrganizationCode.Rumble => "org-accent-rumble",
        OrganizationCode.AECOM => "org-accent-aecom",
        OrganizationCode.Metrolink => "org-accent-metrolink",
        _ => ""
    };
}
