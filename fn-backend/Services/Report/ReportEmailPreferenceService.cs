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
            LastSentAt = pref.LastSentAt
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
                CreatedAt = DateTime.UtcNow
            };
            _context.ReportEmailPreferences.Add(pref);
        }
        else
        {
            pref.AutoSendEnabled = dto.AutoSendEnabled;
            pref.Frequency = dto.Frequency;
            pref.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new ReportEmailPreferenceDto
        {
            Id = pref.Id,
            UserId = pref.UserId,
            AutoSendEnabled = pref.AutoSendEnabled,
            Frequency = pref.Frequency,
            LastSentAt = pref.LastSentAt
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
            LastSentAt = p.LastSentAt
        }).ToList();
    }
}
