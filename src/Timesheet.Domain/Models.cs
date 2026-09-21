namespace Timesheet.Domain;

public class AppUser
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required OrganizationCode Organization { get; init; }
    public required List<RoleType> Roles { get; init; }

    /// <summary>
    /// Organizations this user can approve timesheets for, when holding the Approver role.
    /// Enables cross-partner approval, e.g. an AECOM approver reviewing Jacobs timesheets.
    /// Empty/omitted = approver's own organization only.
    /// </summary>
    public List<OrganizationCode> ApproverScope { get; init; } = [];
}

public class Project
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Organizations that book time against this project (the program-level scope).</summary>
    public List<OrganizationCode> ParticipatingOrganizations { get; init; } = [];
}

public class WorkTask
{
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required string Name { get; init; }
    public WorkStageStatus Status { get; init; } = WorkStageStatus.Active;
}

public class AbsenceCode
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}

public class TimeEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateOnly Date { get; set; }
    public EntryType Type { get; set; } = EntryType.Work;
    public string? ProjectId { get; set; }
    public string? TaskId { get; set; }
    public string? AbsenceCodeId { get; set; }
    public decimal Hours { get; set; }
    public WorkArrangement WorkArrangement { get; set; } = WorkArrangement.NotApplicable;
    public ClientLocation ClientLocation { get; set; } = ClientLocation.NotApplicable;
    public MobilityStatus Mobility { get; set; } = MobilityStatus.NotApplicable;
    public string? Notes { get; set; }
}

public class TimesheetWeek
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required OrganizationCode Organization { get; init; }
    public required DateOnly WeekStarting { get; init; }
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    public List<TimeEntry> Entries { get; init; } = [];
    public string? RejectionReason { get; set; }

    /// <summary>User id of the first-stage (program/org) approver.</summary>
    public string? ApprovedByUserId { get; set; }

    /// <summary>User id of the final/second-stage super-admin approver.</summary>
    public string? FinalApprovedByUserId { get; set; }
}
