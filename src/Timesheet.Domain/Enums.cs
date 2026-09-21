namespace Timesheet.Domain;

/// <summary>
/// Delivery partner organisation. Jacobs uses internal Entra ID; the others use external Entra ID (B2B/guest).
/// </summary>
public enum OrganizationCode
{
    Jacobs,
    Rumble,
    AECOM,
    Metrolink
}

/// <summary>
/// Identity provider used to authenticate a given organization.
/// </summary>
public enum IdentityProviderType
{
    EntraIdInternal,
    EntraIdExternal
}

/// <summary>
/// Application-level roles. A user can hold more than one role (e.g. Approver + TimeEntry).
/// </summary>
public enum RoleType
{
    /// <summary>Can create and submit their own timesheets only.</summary>
    TimeEntry,
    /// <summary>Can view and approve/reject timesheets for people in their scope, across partner organizations.</summary>
    Approver,
    /// <summary>Full administration: users, roles, projects, tasks, calendars, absence codes.</summary>
    Administrator,
    /// <summary>Read-only visibility across the whole program (e.g. PMO / finance).</summary>
    ProgramViewer,
    /// <summary>Second-stage / final sign-off approver over timesheets already approved by a first-stage program approver.</summary>
    FinalApprover
}

public enum TimesheetStatus
{
    Draft,
    ReadyToSubmit,
    Submitted,
    /// <summary>Approved by the first-stage (program/org) approver; awaiting final super-admin sign-off.</summary>
    FirstStageApproved,
    Approved,
    Rejected
}

public enum EntryType
{
    /// <summary>Regular work time logged against a project/task.</summary>
    Work,
    /// <summary>Absence entry (e.g. Annual Leave) logged against an absence code.</summary>
    Absence
}

public enum WorkArrangement
{
    NotApplicable,
    Remote,
    InPerson
}

public enum ClientLocation
{
    NotApplicable,
    ClientOffice,
    OutsideClientOffice
}

public enum MobilityStatus
{
    NotApplicable,
    Fifo,
    InCountry
}

public enum AbsenceDurationType
{
    FullDay,
    HalfDay
}

public enum WorkStageStatus
{
    Active,
    Inactive
}
