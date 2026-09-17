using Timesheet.Domain;

namespace Timesheet.Application;

/// <summary>
/// Singleton in-memory data store for the POC "Carcass" stage. Replace with real persistence later.
/// </summary>
public class InMemoryTimesheetStore
{
    public List<AppUser> Users { get; } = [];
    public List<Project> Projects { get; } = [];
    public List<WorkTask> Tasks { get; } = [];
    public List<AbsenceCode> AbsenceCodes { get; } = [];
    public List<TimesheetWeek> Timesheets { get; } = [];

    public InMemoryTimesheetStore()
    {
        Seed();
    }

    /// <summary>Returns timesheets an approver is permitted to see, honoring cross-org ApproverScope.</summary>
    public IEnumerable<TimesheetWeek> GetApprovableTimesheets(AppUser approver)
    {
        var scope = approver.ApproverScope.Count > 0
            ? approver.ApproverScope
            : [approver.Organization];

        return Timesheets.Where(t => scope.Contains(t.Organization));
    }

    private void Seed()
    {
        Users.AddRange(
        [
            new AppUser { Id = "u1", DisplayName = "Alice Jacobs", Organization = OrganizationCode.Jacobs, Roles = [RoleType.TimeEntry, RoleType.Approver], ApproverScope = [OrganizationCode.Jacobs] },
            new AppUser { Id = "u2", DisplayName = "Bob Rumble", Organization = OrganizationCode.Rumble, Roles = [RoleType.TimeEntry] },
            new AppUser { Id = "u3", DisplayName = "Carol Admin", Organization = OrganizationCode.Jacobs, Roles = [RoleType.Administrator] },
            new AppUser { Id = "u4", DisplayName = "Dan Viewer", Organization = OrganizationCode.AECOM, Roles = [RoleType.ProgramViewer] },
            // Cross-org approver: AECOM manager approving Jacobs + AECOM + Metrolink timesheets on the program.
            new AppUser { Id = "u5", DisplayName = "Erin AECOM (Program Approver)", Organization = OrganizationCode.AECOM, Roles = [RoleType.Approver], ApproverScope = [OrganizationCode.AECOM, OrganizationCode.Jacobs, OrganizationCode.Metrolink] }
        ]);

        Projects.AddRange(
        [
            new Project { Id = "p1", Name = "Metrolink Signalling Upgrade", ParticipatingOrganizations = [OrganizationCode.Jacobs, OrganizationCode.Metrolink] },
            new Project { Id = "p2", Name = "Rumble Station Refurb", ParticipatingOrganizations = [OrganizationCode.Rumble, OrganizationCode.AECOM] }
        ]);

        Tasks.AddRange(
        [
            new WorkTask { Id = "t1", ProjectId = "p1", Name = "Design Review" },
            new WorkTask { Id = "t2", ProjectId = "p1", Name = "Site Inspection" },
            new WorkTask { Id = "t3", ProjectId = "p2", Name = "Structural Assessment" }
        ]);

        AbsenceCodes.AddRange(
        [
            new AbsenceCode { Id = "a1", Name = "Annual Leave" },
            new AbsenceCode { Id = "a2", Name = "Sick Leave" }
        ]);

        var weekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1));

        Timesheets.Add(new TimesheetWeek
        {
            Id = "ts1",
            UserId = "u1",
            Organization = OrganizationCode.Jacobs,
            WeekStarting = weekStart,
            Status = TimesheetStatus.Submitted,
            Entries =
            [
                new TimeEntry { Date = weekStart, Type = EntryType.Work, ProjectId = "p1", TaskId = "t1", Hours = 8, WorkArrangement = WorkArrangement.InPerson, ClientLocation = ClientLocation.ClientOffice }
            ]
        });
    }
}
