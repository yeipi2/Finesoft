namespace fs_front.DTO;

public class ReportEmailPreferenceDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool AutoSendEnabled { get; set; }
    public string Frequency { get; set; } = "weekly";
    public DateTime? LastSentAt { get; set; }
    public DateTime? NextSendAt { get; set; }
    public bool IncludeDashboard { get; set; } = true;
    public bool IncludeFinancial { get; set; } = true;
    public bool IncludePerformance { get; set; } = true;
    public bool IncludeClients { get; set; } = true;
    public bool IncludeProjects { get; set; } = true;
    public bool IncludeEmployees { get; set; } = true;
}

public class UpdateReportEmailPreferenceDto
{
    public bool AutoSendEnabled { get; set; }
    public string Frequency { get; set; } = "weekly";
    public bool IncludeDashboard { get; set; } = true;
    public bool IncludeFinancial { get; set; } = true;
    public bool IncludePerformance { get; set; } = true;
    public bool IncludeClients { get; set; } = true;
    public bool IncludeProjects { get; set; } = true;
    public bool IncludeEmployees { get; set; } = true;
}

public class SendReportEmailRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IncludeDashboard { get; set; } = true;
    public bool IncludeFinancial { get; set; } = true;
    public bool IncludePerformance { get; set; } = true;
    public bool IncludeClients { get; set; } = true;
    public bool IncludeProjects { get; set; } = true;
    public bool IncludeEmployees { get; set; } = true;
}

public class SendReportEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
