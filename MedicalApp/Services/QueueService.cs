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

        public async Task AddToQueueAsync(int patientId, string patientName, string doctorName = "Dr. Yaser")
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existing = await context.QueueEntries
                .FirstOrDefaultAsync(q => q.PatientId == patientId && q.Status != "Completed");
                
            if (existing != null)
            {
                existing.Status = "Pending";
                existing.DoctorName = doctorName;
                context.QueueEntries.Update(existing);
            }
            else
            {
                var entry = new QueueEntry
                {
                    PatientId = patientId,
                    PatientName = patientName,
                    Status = "Pending",
                    DoctorName = doctorName
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

        public async Task<int> GetCompletedCountTodayAsync(string? doctorName = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var today = System.DateTime.Today;
            var query = context.QueueEntries
                .Where(q => q.Status == "Completed" && q.CreatedAt >= today);
            if (!string.IsNullOrEmpty(doctorName))
            {
                query = query.Where(q => q.DoctorName == doctorName);
            }
            return await query.CountAsync();
        }
    }
}
