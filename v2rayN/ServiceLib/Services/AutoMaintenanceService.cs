using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceLib.Enums;
using ServiceLib.Handler;
using ServiceLib.Models;

namespace ServiceLib.Services
{
    public class AutoMaintenanceService
    {
        private static readonly string _tag = "AutoMaintenanceService";

        public async Task RunJobAsync(Action<string> updateStatus)
        {
            try
            {
                updateStatus?.Invoke("Start Auto Maintenance Job...");

                // 1. Update Subscription
                updateStatus?.Invoke("Updating subscriptions (No Proxy)...");
                var config = AppHandler.Instance.Config;
                var updateService = new UpdateService();
                
                await updateService.UpdateSubscriptionProcess(config, "", false, (success, msg) => 
                {
                    if (msg != null && (msg.Contains("Fail") || msg.Contains("Error") || success))
                    {
                         // Optional: log or update status
                    }
                });
                updateStatus?.Invoke("Subscription update completed.");

                // 2. Filter servers (exclude goflyway)
                var allProfiles = await AppHandler.Instance.ProfileItems(null) ?? new List<ProfileItem>();
                
                // Exclude Custom (2) and Goflyway (by checking ConfigType enum name string to be safe)
                var targetProfiles = allProfiles.Where(t => t.ConfigType != EConfigType.Custom && t.ConfigType.ToString() != "Goflyway").ToList(); 
                
                if (targetProfiles.Count == 0)
                {
                    updateStatus?.Invoke("No profiles to test.");
                    return;
                }

                // 3. Speedtest (Prioritized)
                updateStatus?.Invoke($"Running Speedtest for {targetProfiles.Count} servers...");
                
                // Use a local dictionary to track results independently
                var speedResults = new ConcurrentDictionary<string, decimal>();
                
                var speedService = new SpeedtestService(config, targetProfiles, ESpeedActionType.Speedtest, GetProgressCallback(targetProfiles.Count, "Speedtest", updateStatus, speedResults));
                if (speedService.ExecuteTask != null)
                {
                    await speedService.ExecuteTask;
                }

                // 4. Delete if speed <= 0
                updateStatus?.Invoke("Analyzing Speedtest results...");
                var toDeleteItems = new List<ProfileItem>();
                var profileExs = await ProfileExHandler.Instance.GetProfileExs();

                foreach (var item in targetProfiles)
                {
                    bool keep = false;
                    decimal currentSpeed = 0;

                    // Priority 1: Check local results captured during test
                    if (speedResults.TryGetValue(item.IndexId, out decimal localSpeed))
                    {
                        currentSpeed = localSpeed;
                        if (localSpeed > 0) keep = true;
                    }
                    // Priority 2: Check ProfileExHandler storage
                    else
                    {
                        var exItem = profileExs.FirstOrDefault(t => t.IndexId == item.IndexId);
                        if (exItem != null)
                        {
                            currentSpeed = exItem.Speed;
                            if (exItem.Speed > 0) keep = true;
                        }
                    }
                    
                    // If we found a positive speed anywhere, we keep it.
                    if (currentSpeed > 0)
                    {
                        keep = true;
                    }
                    
                    if (!keep)
                    {
                        toDeleteItems.Add(item);
                    }
                }

                if (toDeleteItems.Count > 0)
                {
                    // Show a few examples of what is being deleted
                    var examples = string.Join(", ", toDeleteItems.Take(3).Select(i => $"{i.Remarks}"));
                    updateStatus?.Invoke($"Deleting {toDeleteItems.Count} servers (Speed <= 0). Ex: {examples}...");
                    
                    await ConfigHandler.RemoveServer(config, toDeleteItems);
                    
                    // Refresh targetProfiles after deletion
                    allProfiles = await AppHandler.Instance.ProfileItems(null) ?? new List<ProfileItem>();
                    targetProfiles = allProfiles.Where(t => t.ConfigType != EConfigType.Custom && t.ConfigType.ToString() != "Goflyway").ToList();
                }

                if (targetProfiles.Count == 0)
                {
                    updateStatus?.Invoke("No profiles left for Tcping.");
                    return;
                }

                // 5. Tcping (Only measure, do NOT delete)
                updateStatus?.Invoke($"Running Tcping for {targetProfiles.Count} servers...");
                var tcpingService = new SpeedtestService(config, targetProfiles, ESpeedActionType.Tcping, GetProgressCallback(targetProfiles.Count, "Tcping", updateStatus, null));
                if (tcpingService.ExecuteTask != null)
                {
                    await tcpingService.ExecuteTask;
                }
                
                updateStatus?.Invoke("Tcping completed (Results updated).");

                updateStatus?.Invoke("Auto Maintenance Job Completed.");
            }
            catch (Exception ex)
            {
                updateStatus?.Invoke($"Job Failed: {ex.Message}");
                Logging.SaveLog(_tag, ex);
            }
        }

        private Action<SpeedTestResult> GetProgressCallback(int total, string taskName, Action<string> updateStatus, ConcurrentDictionary<string, decimal> speedResults)
        {
            var completed = new ConcurrentDictionary<string, byte>();
            return (res) => 
            {
                // Capture speed result if available
                if (speedResults != null && !string.IsNullOrEmpty(res.Speed))
                {
                     // SpeedtestService might return strings like "10.5 MB/s", "Failed", "Timeout", "-1", "0", etc.
                     // We need to robustly parse the numeric part.
                     
                     // 1. Try direct parsing
                     if (decimal.TryParse(res.Speed, out decimal s))
                     {
                         speedResults.AddOrUpdate(res.IndexId, s, (k, v) => Math.Max(v, s));
                     }
                     else 
                     {
                         // 2. Try parsing by removing non-digit characters (except dot)
                         // Example: "10.5 MB/s" -> "10.5"
                         var numStr = new string(res.Speed.Where(c => char.IsDigit(c) || c == '.').ToArray());
                         if (!string.IsNullOrEmpty(numStr) && decimal.TryParse(numStr, out decimal s2))
                         {
                             speedResults.AddOrUpdate(res.IndexId, s2, (k, v) => Math.Max(v, s2));
                         }
                     }
                }

                // Simple heuristic: if the result contains digit, it is likely a valid result (e.g., "100 ms", "10 MB/s", "-1")
                // Initialization usually sends "Testing..." or "Wait..." which contains no digits (in English/Chinese usually)
                bool isResult = false;
                if (!string.IsNullOrEmpty(res.Delay) && res.Delay.Any(char.IsDigit)) isResult = true;
                if (!string.IsNullOrEmpty(res.Speed) && res.Speed.Any(char.IsDigit)) isResult = true;
                
                if (isResult)
                {
                    completed.TryAdd(res.IndexId, 0);
                    updateStatus?.Invoke($"{taskName}: {completed.Count}/{total}");
                }
            };
        }
    }
}
