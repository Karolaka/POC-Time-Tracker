using System.Text.Json;
using System.Text.Json.Serialization;
using Timesheet.Domain;

namespace Timesheet.Application;

/// <summary>
/// Singleton in-memory data store for the POC "Carcass" stage. Replace with real persistence later.
/// Timesheets are additionally persisted to a JSON file in the temp folder so state survives app
/// restarts and can be inspected/edited manually between switching accounts. Each entry is keyed by
/// UserId + WeekStarting (and each TimeEntry by Date), so edits made externally map back correctly.
/// </summary>
public class InMemoryTimesheetStore
{
    private static readonly string PersistedFilePath =
        Path.Combine(Path.GetTempPath(), "Timesheet.Web", "timesheets.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public List<AppUser> Users { get; } = [];
    public List<Project> Projects { get; } = [];
    public List<WorkTask> Tasks { get; } = [];
    public List<AbsenceCode> AbsenceCodes { get; } = [];
    public List<TimesheetWeek> Timesheets { get; } = [];

    public InMemoryTimesheetStore()
    {
        SeedReferenceData();
        LoadTimesheetsOrSeed();
    }

    /// <summary>Returns timesheets an approver is permitted to see, honoring cross-org ApproverScope.</summary>
    public IEnumerable<TimesheetWeek> GetApprovableTimesheets(AppUser approver)
    {
        var scope = approver.ApproverScope.Count > 0
            ? approver.ApproverScope
            : [approver.Organization];

        return Timesheets.Where(t => scope.Contains(t.Organization));
    }

    /// <summary>Returns timesheets awaiting final (second-stage) sign-off across all organizations.
    /// A FinalApprover reviews items already approved by a first-stage program/org approver.</summary>
    public IEnumerable<TimesheetWeek> GetFinalApprovableTimesheets()
        => Timesheets.Where(t => t.Status == TimesheetStatus.FirstStageApproved);

    /// <summary>Persists the current in-memory Timesheets list to a JSON file so it survives restarts
    /// and can be reviewed/edited manually (e.g. to simulate a different account seeing submitted data).</summary>
    public void SaveTimesheets()
    {
        var directory = Path.GetDirectoryName(PersistedFilePath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(Timesheets, JsonOptions);
        File.WriteAllText(PersistedFilePath, json);
    }

    private void LoadTimesheetsOrSeed()
    {
        if (File.Exists(PersistedFilePath))
        {
            try
            {
                var json = File.ReadAllText(PersistedFilePath);
                var loaded = JsonSerializer.Deserialize<List<TimesheetWeek>>(json, JsonOptions);
                if (loaded is not null)
                {
                    Timesheets.AddRange(loaded);
                    return;
                }
            }
            catch (JsonException)
            {
                // Fall through to seed data if the persisted file is corrupt/malformed.
            }
        }

        SeedTimesheets();
        SaveTimesheets();
    }

    private void SeedReferenceData()
    {
        Users.AddRange(
        [
            new AppUser { Id = "u1", DisplayName = "Alice Jacobs", Organization = OrganizationCode.Jacobs, Roles = [RoleType.TimeEntry, RoleType.Approver], ApproverScope = [OrganizationCode.Jacobs] },
            new AppUser { Id = "u2", DisplayName = "Bob Rumble", Organization = OrganizationCode.Rumble, Roles = [RoleType.TimeEntry] },
            new AppUser { Id = "u3", DisplayName = "Carol Admin", Organization = OrganizationCode.Jacobs, Roles = [RoleType.Administrator] },
            new AppUser { Id = "u4", DisplayName = "Dan Viewer", Organization = OrganizationCode.AECOM, Roles = [RoleType.ProgramViewer] },
            // Cross-org approver: AECOM manager approving Jacobs + AECOM + Metrolink timesheets on the program.
            new AppUser { Id = "u5", DisplayName = "Erin AECOM (Program Approver)", Organization = OrganizationCode.AECOM, Roles = [RoleType.Approver], ApproverScope = [OrganizationCode.AECOM, OrganizationCode.Jacobs, OrganizationCode.Metrolink] },
            // First-stage approver dedicated to Rumble-related entries.
            new AppUser { Id = "u6", DisplayName = "Frank Rumble (Program Approver)", Organization = OrganizationCode.Rumble, Roles = [RoleType.Approver], ApproverScope = [OrganizationCode.Rumble] },
            // Super admin / final approver: second-stage sign-off across all organizations, regardless of who approved first.
            new AppUser { Id = "u7", DisplayName = "Grace SuperAdmin (Final Approver)", Organization = OrganizationCode.Jacobs, Roles = [RoleType.FinalApprover] }
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
    }

    private void SeedTimesheets()
    {
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

        Timesheets.Add(new TimesheetWeek
        {
            Id = "ts2",
            UserId = "u2",
            Organization = OrganizationCode.Rumble,
            WeekStarting = weekStart,
            Status = TimesheetStatus.Submitted,
            Entries =
            [
                new TimeEntry { Date = weekStart, Type = EntryType.Work, ProjectId = "p2", TaskId = "t3", Hours = 8, WorkArrangement = WorkArrangement.InPerson, ClientLocation = ClientLocation.ClientOffice },
                new TimeEntry { Date = weekStart.AddDays(1), Type = EntryType.Work, ProjectId = "p2", TaskId = "t3", Hours = 8, WorkArrangement = WorkArrangement.InPerson, ClientLocation = ClientLocation.ClientOffice }
            ]
        });
    }
}