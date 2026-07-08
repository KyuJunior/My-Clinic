using System.Collections.Generic;
using System.Threading.Tasks;
using MedicalApp.Models;

namespace MedicalApp.Services
{
    public interface IQueueService
    {
        Task<IEnumerable<QueueEntry>> GetActiveQueueAsync();
        Task AddToQueueAsync(int patientId, string patientName);
        Task UpdateQueueStatusAsync(int patientId, string status);
        Task CompleteQueueEntryAsync(int patientId);
        Task RemoveFromQueueAsync(int queueEntryId);
        Task<int> GetCompletedCountTodayAsync();
    }
}
