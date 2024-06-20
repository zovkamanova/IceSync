using IceSync.Data;
using IceSync.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IceSync.Services
{
    public class SyncService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SyncService> _logger;
        private Timer _timer;

        public SyncService(IServiceScopeFactory scopeFactory, ILogger<SyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SyncService is starting.");

            // Schedule the background task to run every 30 minutes
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SyncService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _logger.LogInformation("SyncService is working.");

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var universalLoaderService = scope.ServiceProvider.GetRequiredService<IUniversalLoaderService>();

                // Get workflows from the API
                var apiWorkflows = universalLoaderService.GetWorkflowsAsync().Result;

                // Get workflows from the local database
                var localWorkflows = dbContext.Workflows.ToList();

                // Synchronize workflows
                SynchronizeWorkflows(apiWorkflows, localWorkflows, dbContext);
            }
        }
        private void SynchronizeWorkflows(List<Workflow> apiWorkflows, List<Workflow> localWorkflows, AppDbContext dbContext)
        {
            // Inserting new workflows from API that haven't been found in the local database
            foreach (var apiWorkflow in apiWorkflows)
            {
                var existingWorkflow = localWorkflows.FirstOrDefault(w => w.Id == apiWorkflow.Id);

                if (existingWorkflow == null)
                {
                    dbContext.Workflows.Add(apiWorkflow);
                    _logger.LogInformation($"Added a new workflow: {apiWorkflow.Id} - {apiWorkflow.Name} to the database.");
                }
                else
                {
                    existingWorkflow.Id = apiWorkflow.Id;
                    existingWorkflow.Name = apiWorkflow.Name;
                    existingWorkflow.IsActive = apiWorkflow.IsActive;
                    existingWorkflow.MultiExecBehavior = apiWorkflow.MultiExecBehavior;
                    _logger.LogInformation($"Workflow with ID: {apiWorkflow.Id} was succefully updated in the database!");
                }
            }

            // Delete workflows from the local database that are haven't been found in the API
            foreach (var localWorkflow in localWorkflows)
            {
                var existingApiWorkflow = apiWorkflows.FirstOrDefault(w => w.Id == localWorkflow.Id);

                if (existingApiWorkflow == null)
                {
                    try
                    {
                        dbContext.Workflows.Remove(localWorkflow);
                        _logger.LogInformation($"Deleted workflow: {localWorkflow.Id} - {localWorkflow.Name} from the database.");
                    }
                    catch (DbUpdateException ex)
                    {
                        _logger.LogError($"Failed to delete workflow {localWorkflow.Id} - {localWorkflow.Name}. Error: {ex.Message}");
                    }
                }
            }

            try
            {
                dbContext.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Failed to save changes to the database. Error: {ex.Message}");
            }
        }
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
