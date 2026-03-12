using fs_backend.DTO;
using fs_backend.Identity;
using fn_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public interface IReportEmailPreferenceService
{
    Task<ReportEmailPreferenceDto?> GetPreferenceAsync(string userId);
    Task<ReportEmailPreferenceDto> UpdatePreferenceAsync(string userId, UpdateReportEmailPreferenceDto dto);
    Task<List<ReportEmailPreferenceDto>> GetAllEnabledPreferencesAsync();
    Task<List<ReportEmailPreferenceDto>> GetPendingPreferencesAsync();
    Task MarkAsSentAsync(string userId);
}

public class ReportEmailPreferenceService : IReportEmailPreferenceService
{
    private readonly ApplicationDbContext _context;

    public ReportEmailPreferenceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportEmailPreferenceDto?> GetPreferenceAsync(string userId)
    {
        var pref = await _context.ReportEmailPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null)
            return null;

        return new ReportEmailPreferenceDto
        {
            Id = pref.Id,
            UserId = pref.UserId,
            AutoSendEnabled = pref.AutoSendEnabled,
            Frequency = pref.Frequency,
            LastSentAt = pref.LastSentAt,
            NextSendAt = pref.NextSendAt,
            IncludeDashboard = pref.IncludeDashboard,
            IncludeFinancial = pref.IncludeFinancial,
            IncludePerformance = pref.IncludePerformance,
            IncludeClients = pref.IncludeClients,
            IncludeProjects = pref.IncludeProjects,
            IncludeEmployees = pref.IncludeEmployees
        };
    }

    public async Task<ReportEmailPreferenceDto> UpdatePreferenceAsync(string userId, UpdateReportEmailPreferenceDto dto)
    {
        var pref = await _context.ReportEmailPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null)
        {
            pref = new ReportEmailPreference
            {
                UserId = userId,
                AutoSendEnabled = dto.AutoSendEnabled,
                Frequency = dto.Frequency,
                IncludeDashboard = dto.IncludeDashboard,
                IncludeFinancial = dto.IncludeFinancial,
                IncludePerformance = dto.IncludePerformance,
                IncludeClients = dto.IncludeClients,
                IncludeProjects = dto.IncludeProjects,
                IncludeEmployees = dto.IncludeEmployees,
                CreatedAt = DateTime.UtcNow
            };
            _context.ReportEmailPreferences.Add(pref);
        }
        else
        {
            pref.AutoSendEnabled = dto.AutoSendEnabled;
            pref.Frequency = dto.Frequency;
            pref.IncludeDashboard = dto.IncludeDashboard;
            pref.IncludeFinancial = dto.IncludeFinancial;
            pref.IncludePerformance = dto.IncludePerformance;
            pref.IncludeClients = dto.IncludeClients;
            pref.IncludeProjects = dto.IncludeProjects;
            pref.IncludeEmployees = dto.IncludeEmployees;
            pref.UpdatedAt = DateTime.UtcNow;

            // Si cambió la frecuencia y AutoSendEnabled está activo, recalcular NextSendAt
            if (pref.AutoSendEnabled && pref.NextSendAt.HasValue)
            {
                var oldFrequency = (await _context.ReportEmailPreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId))?.Frequency;

                if (!string.IsNullOrEmpty(oldFrequency) && oldFrequency != dto.Frequency)
                {
                    pref.NextSendAt = dto.Frequency switch
                    {
                        "daily" => DateTime.UtcNow.AddDays(1),
                        "weekly" => DateTime.UtcNow.AddDays(7),
                        "biweekly" => DateTime.UtcNow.AddDays(14),
                        "monthly" => DateTime.UtcNow.AddMonths(1),
                        _ => DateTime.UtcNow.AddDays(7)
                    };
                }
            }
        }

        await _context.SaveChangesAsync();

        return new ReportEmailPreferenceDto
        {
            Id = pref.Id,
            UserId = pref.UserId,
            AutoSendEnabled = pref.AutoSendEnabled,
            Frequency = pref.Frequency,
            LastSentAt = pref.LastSentAt,
            NextSendAt = pref.NextSendAt,
            IncludeDashboard = pref.IncludeDashboard,
            IncludeFinancial = pref.IncludeFinancial,
            IncludePerformance = pref.IncludePerformance,
            IncludeClients = pref.IncludeClients,
            IncludeProjects = pref.IncludeProjects,
            IncludeEmployees = pref.IncludeEmployees
        };
    }

    public async Task<List<ReportEmailPreferenceDto>> GetAllEnabledPreferencesAsync()
    {
        var prefs = await _context.ReportEmailPreferences
            .Where(p => p.AutoSendEnabled)
            .ToListAsync();

        return prefs.Select(p => new ReportEmailPreferenceDto
        {
            Id = p.Id,
            UserId = p.UserId,
            AutoSendEnabled = p.AutoSendEnabled,
            Frequency = p.Frequency,
            LastSentAt = p.LastSentAt,
            NextSendAt = p.NextSendAt,
            IncludeDashboard = p.IncludeDashboard,
            IncludeFinancial = p.IncludeFinancial,
            IncludePerformance = p.IncludePerformance,
            IncludeClients = p.IncludeClients,
            IncludeProjects = p.IncludeProjects,
            IncludeEmployees = p.IncludeEmployees
        }).ToList();
    }

    public async Task<List<ReportEmailPreferenceDto>> GetPendingPreferencesAsync()
    {
        var now = DateTime.UtcNow;
        var prefs = await _context.ReportEmailPreferences
            .Where(p => p.AutoSendEnabled && (p.NextSendAt == null || p.NextSendAt <= now))
            .ToListAsync();

        return prefs.Select(p => new ReportEmailPreferenceDto
        {
            Id = p.Id,
            UserId = p.UserId,
            AutoSendEnabled = p.AutoSendEnabled,
            Frequency = p.Frequency,
            LastSentAt = p.LastSentAt,
            NextSendAt = p.NextSendAt,
            IncludeDashboard = p.IncludeDashboard,
            IncludeFinancial = p.IncludeFinancial,
            IncludePerformance = p.IncludePerformance,
            IncludeClients = p.IncludeClients,
            IncludeProjects = p.IncludeProjects,
            IncludeEmployees = p.IncludeEmployees
        }).ToList();
    }

    public async Task MarkAsSentAsync(string userId)
    {
        var pref = await _context.ReportEmailPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null) return;

        var now = DateTime.UtcNow;
        pref.LastSentAt = now;
        
        pref.NextSendAt = pref.Frequency switch
        {
            "daily" => now.AddDays(1),
            "weekly" => now.AddDays(7),
            "biweekly" => now.AddDays(14),
            "monthly" => now.AddMonths(1),
            _ => now.AddDays(7)
        };

        pref.UpdatedAt = now;
        await _context.SaveChangesAsync();
    }
}
