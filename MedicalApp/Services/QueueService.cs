using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedicalApp.Data;
using MedicalApp.Models;

namespace MedicalApp.Services
{
    public class QueueService : IQueueService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public QueueService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<QueueEntry>> GetActiveQueueAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.QueueEntries
                .Include(q => q.Patient)
                .Where(q => q.Status != "Completed")
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task AddToQueueAsync(int patientId, string patientName)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existing = await context.QueueEntries
                .FirstOrDefaultAsync(q => q.PatientId == patientId && q.Status != "Completed");
                
            if (existing != null)
            {
                existing.Status = "Pending";
                context.QueueEntries.Update(existing);
            }
            else
            {
                var entry = new QueueEntry
                {
                    PatientId = patientId,
                    PatientName = patientName,
                    Status = "Pending"
                };
                await context.QueueEntries.AddAsync(entry);
            }
            await context.SaveChangesAsync();
        }

        public async Task UpdateQueueStatusAsync(int patientId, string status)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var entry = await context.QueueEntries
                .FirstOrDefaultAsync(q => q.PatientId == patientId && q.Status != "Completed");
            if (entry != null)
            {
                entry.Status = status;
                context.QueueEntries.Update(entry);
                await context.SaveChangesAsync();
            }
        }

        public async Task CompleteQueueEntryAsync(int patientId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var entry = await context.QueueEntries
                .FirstOrDefaultAsync(q => q.PatientId == patientId && q.Status != "Completed");
            if (entry != null)
            {
                entry.Status = "Completed";
                context.QueueEntries.Update(entry);
                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromQueueAsync(int queueEntryId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var entry = await context.QueueEntries.FindAsync(queueEntryId);
            if (entry != null)
            {
                context.QueueEntries.Remove(entry);
                await context.SaveChangesAsync();
            }
        }

        public async Task<int> GetCompletedCountTodayAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var today = System.DateTime.Today;
            return await context.QueueEntries
                .Where(q => q.Status == "Completed" && q.CreatedAt >= today)
                .CountAsync();
        }
    }
}
