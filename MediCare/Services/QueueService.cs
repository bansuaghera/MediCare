using System.Collections.Concurrent;
using MediCare.Models;

namespace MediCare.Services
{
    public class QueueService
    {
        // Dictionary to store current token being served by each doctor
        // Key: DoctorId, Value: CurrentTokenNumber
        private static readonly ConcurrentDictionary<int, string> _currentTokens = new ConcurrentDictionary<int, string>();

        public void SetCurrentToken(int doctorId, string tokenNumber)
        {
            _currentTokens[doctorId] = tokenNumber;
        }

        public void ClearCurrentToken(int doctorId)
        {
            _currentTokens.TryRemove(doctorId, out _);
        }

        public string GetCurrentToken(int doctorId)
        {
            return _currentTokens.TryGetValue(doctorId, out var token) ? token : "None";
        }

        public ConcurrentDictionary<int, string> GetAllCurrentTokens()
        {
            return _currentTokens;
        }
    }
}
