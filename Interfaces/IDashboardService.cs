using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface IDashboardService
{
    Task<DashboardDataDTO> GetDashboardDataAsync();
}