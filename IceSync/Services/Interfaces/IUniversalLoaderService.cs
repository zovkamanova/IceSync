using IceSync.Data;

namespace IceSync.Services.Interfaces
{
    public interface IUniversalLoaderService
    {
        Task<string> GetTokenAsync();
        Task<List<Workflow>> GetWorkflowsAsync();
        Task<bool> RunWorkflowAsync(int workflowId);
    }
}