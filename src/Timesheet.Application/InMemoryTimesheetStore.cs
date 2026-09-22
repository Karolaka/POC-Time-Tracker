using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Timesheet.Domain;

namespace Timesheet.Application;

/// <summary>
/// Singleton in-memory data store for the POC "Carcass" stage. Replace with real persistence later.
/// Timesheets are additionally persisted to a JSON file in the temp folder so state survives app
/// restarts and can be inspected/edited manually between switching accounts. Each entry is keyed by
/// UserId + WeekStarting (and each TimeEntry by Date), so edits made externally map back correctly.
///
/// On a truly fresh run (no runtime state file yet), initial "to approve" timesheets are loaded from
/// the checked-in seed-timesheets.json file (dates expressed as relative offsets from the current
/// week/day so they remain valid whenever the app is first run).
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

    private readonly string? _seedFilePath;

    public List<AppUser> Users { get; } = [];
    public List<Project> Projects { get; } = [];
    public List<WorkTask> Tasks { get; } = [];
    public List<AbsenceCode> AbsenceCodes { get; } = [];
    public List<TimesheetWeek> Timesheets { get; } = [];

    public InMemoryTimesheetStore(string? seedFilePath = null)
    {
        _seedFilePath = seedFilePath;
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

    /// <summary>Resolves a user's display name from their id, falling back to the raw id if not found.</summary>
    public string GetUserDisplayName(string? userId)
        => Users.FirstOrDefault(u => u.Id == userId)?.DisplayName ?? userId ?? "-";

    /// <summary>Resolves a project's display name from its id, falling back to the raw id if not found.</summary>
    public string GetProjectName(string? projectId)
        => Projects.FirstOrDefault(p => p.Id == projectId)?.Name ?? projectId ?? "-";

    /// <summary>Total hours logged across all work entries in a timesheet week.</summary>
    public static decimal GetTotalHours(TimesheetWeek timesheet)
        => timesheet.Entries.Sum(e => e.Hours);

    /// <summary>Groups a timesheet's entries by project, summing hours per project (e.g. "Metrolink Signalling Upgrade: 8h").</summary>
    public string GetProjectHoursBreakdown(TimesheetWeek timesheet)
    {
        var groups = timesheet.Entries
            .Where(e => e.Type == EntryType.Work)
            .GroupBy(e => e.ProjectId)
            .Select(g => $"{GetProjectName(g.Key)}: {g.Sum(e => e.Hours):0.##}h");

        return string.Join(", ", groups);
    }

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

        SeedTimesheetsFromFile();
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

    /// <summary>Loads initial "to approve" timesheets from the seed-timesheets.json file, resolving
    /// weekOffset/dayOffset into real dates relative to the current week. Falls back to a minimal
    /// built-in default if the seed file is missing or invalid, so the app is never empty on first run.</summary>
    private void SeedTimesheetsFromFile()
    {
        var currentWeekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1));

        if (_seedFilePath is not null && File.Exists(_seedFilePath))
        {
            try
            {
                var json = File.ReadAllText(_seedFilePath);
                var root = JsonNode.Parse(json)?.AsObject();
                var timesheetsNode = root?["timesheets"]?.AsArray();

                if (timesheetsNode is not null)
                {
                    foreach (var node in timesheetsNode)
                    {
                        if (node is null) continue;
                        var timesheet = ParseSeedTimesheet(node.AsObject(), currentWeekStart);
                        if (timesheet is not null)
                        {
                            Timesheets.Add(timesheet);
                        }
                    }

                    if (Timesheets.Count > 0)
                    {
                        return;
                    }
                }
            }
            catch (JsonException)
            {
                // Fall through to the built-in default below if the seed file is malformed.
            }
        }

        SeedDefaultTimesheet(currentWeekStart);
    }

    private static TimesheetWeek? ParseSeedTimesheet(JsonObject obj, DateOnly currentWeekStart)
    {
        var id = obj["id"]?.GetValue<string>();
        var userId = obj["userId"]?.GetValue<string>();
        var organizationText = obj["organization"]?.GetValue<string>();
        var weekOffset = obj["weekOffset"]?.GetValue<int>() ?? 0;
        var statusText = obj["status"]?.GetValue<string>();

        if (id is null || userId is null || organizationText is null ||
            !Enum.TryParse<OrganizationCode>(organizationText, out var organization))
        {
            return null;
        }

        var status = Enum.TryParse<TimesheetStatus>(statusText, out var parsedStatus) ? parsedStatus : TimesheetStatus.Draft;
        var weekStart = currentWeekStart.AddDays(weekOffset * 7);

        var entries = new List<TimeEntry>();
        var entriesNode = obj["entries"]?.AsArray();
        if (entriesNode is not null)
        {
            foreach (var entryNode in entriesNode)
            {
                if (entryNode is null) continue;
                var entryObj = entryNode.AsObject();

                var dayOffset = entryObj["dayOffset"]?.GetValue<int>() ?? 0;
                var typeText = entryObj["type"]?.GetValue<string>();
                var type = Enum.TryParse<EntryType>(typeText, out var parsedType) ? parsedType : EntryType.Work;
                var hours = entryObj["hours"]?.GetValue<decimal>() ?? 0;
                var workArrangementText = entryObj["workArrangement"]?.GetValue<string>();
                var workArrangement = Enum.TryParse<WorkArrangement>(workArrangementText, out var parsedWorkArrangement) ? parsedWorkArrangement : WorkArrangement.NotApplicable;
                var clientLocationText = entryObj["clientLocation"]?.GetValue<string>();
                var clientLocation = Enum.TryParse<ClientLocation>(clientLocationText, out var parsedClientLocation) ? parsedClientLocation : ClientLocation.NotApplicable;

                entries.Add(new TimeEntry
                {
                    Date = weekStart.AddDays(dayOffset),
                    Type = type,
                    ProjectId = entryObj["projectId"]?.GetValue<string>(),
                    TaskId = entryObj["taskId"]?.GetValue<string>(),
                    AbsenceCodeId = entryObj["absenceCodeId"]?.GetValue<string>(),
                    Hours = hours,
                    WorkArrangement = workArrangement,
                    ClientLocation = clientLocation
                });
            }
        }

        return new TimesheetWeek
        {
            Id = id,
            UserId = userId,
            Organization = organization,
            WeekStarting = weekStart,
            Status = status,
            Entries = entries
        };
    }

    /// <summary>Minimal built-in fallback used only if seed-timesheets.json is missing/invalid, so the
    /// app never starts with a completely empty approvals queue.</summary>
    private void SeedDefaultTimesheet(DateOnly weekStart)
    {
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
