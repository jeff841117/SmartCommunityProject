using sql.Models;
using sql.Repositories;

using System.Text;

namespace sql.Services
{
    // ReservationService 是預約流程的協調中心。
    // Controller 負責接請求，Repository 負責查資料，
    // Service 則負責決定流程怎麼走、回傳什麼給前端。
    public class ReservationService
    {
        private readonly ReservationRepository _reservationRepository;
        private readonly AdminActionLogService _adminActionLogService;

        public ReservationService(
            ReservationRepository reservationRepository,
            AdminActionLogService adminActionLogService)
        {
            _reservationRepository = reservationRepository;
            _adminActionLogService = adminActionLogService;
        }

        // 建立預約前，先確認是否已登入。
        public ReservationResult MakeReservation(byte equipmentId, CurrentUser currentUser)
        {
            if (!currentUser.IsAuthenticated)
            {
                return new ReservationResult
                {
                    Success = false,
                    Message = "請先登入系統"
                };
            }

            return _reservationRepository.CreateReservation(equipmentId, currentUser.ReservationUserKey);
        }

        // 未來時段預約和立即預約分開處理，
        // 這樣後面規則變複雜時，不會把兩種流程混在一起。
        public ReservationResult CreateFutureReservation(FutureReservationRequestViewModel request, CurrentUser currentUser)
        {
            if (!currentUser.IsAuthenticated)
            {
                return new ReservationResult
                {
                    Success = false,
                    Message = "請先登入系統"
                };
            }

            return _reservationRepository.CreateScheduledReservation(request, currentUser.ReservationUserKey);
        }

        public bool CancelReservation(int reservationId, CurrentUser currentUser)
        {
            if (!currentUser.IsAuthenticated)
            {
                return false;
            }

            return _reservationRepository.CancelReservation(reservationId, currentUser.ReservationUserKey);
        }

        public bool EndUsage(int reservationId, CurrentUser currentUser)
        {
            if (!currentUser.IsAuthenticated)
            {
                return false;
            }

            return _reservationRepository.EndUsage(reservationId, currentUser.ReservationUserKey);
        }

        public bool ForceEndUsage(int reservationId, CurrentUser currentUser)
        {
            if (!currentUser.IsManager)
            {
                return false;
            }

            var success = _reservationRepository.ForceEndUsage(reservationId, currentUser.UserId);
            if (success && currentUser.UserId.HasValue)
            {
                _adminActionLogService.LogForceEndUsage(currentUser.UserId.Value, reservationId);
            }

            return success;
        }

        public bool ForceCancelScheduledReservation(int reservationId, CurrentUser currentUser)
        {
            if (!currentUser.IsManager)
            {
                return false;
            }

            var success = _reservationRepository.ForceCancelScheduledReservation(reservationId, currentUser.UserId);
            if (success && currentUser.UserId.HasValue)
            {
                _adminActionLogService.LogForceCancelScheduledReservation(currentUser.UserId.Value, reservationId);
            }

            return success;
        }

        // 管理者調整時段時，仍沿用和一般未來預約相同的推算規則。
        // 這樣才不會出現前台說不行、後台卻硬改成功的邏輯落差。
        public ReservationResult ForceRescheduleScheduledReservation(
            AdminRescheduleReservationFormViewModel form,
            CurrentUser currentUser)
        {
            if (!currentUser.IsManager)
            {
                return new ReservationResult
                {
                    Success = false,
                    Message = "您沒有管理員權限"
                };
            }

            var result = _reservationRepository.RescheduleScheduledReservation(form, currentUser.UserId);
            if (result.Success && currentUser.UserId.HasValue)
            {
                _adminActionLogService.LogForceRescheduleScheduledReservation(
                    currentUser.UserId.Value,
                    form.ReservationId,
                    form.ReservationDate,
                    form.SelectedSlotStartTime,
                    result.QueueExpected);
            }

            return result;
        }

        public ReservationAdjustmentPreviewResponse PreviewRescheduleScheduledReservation(
            AdminRescheduleReservationFormViewModel form,
            CurrentUser currentUser)
        {
            if (!currentUser.IsManager)
            {
                return new ReservationAdjustmentPreviewResponse
                {
                    CanReschedule = false,
                    Message = "您沒有管理員權限"
                };
            }

            return _reservationRepository.PreviewRescheduleScheduledReservation(form);
        }

        // 背景服務與手動清理都會用到這個方法。
        public void AutoCompleteExpiredReservations()
        {
            _reservationRepository.AutoCompleteExpiredReservations();
        }

        // 整理「我的預約」頁面所需資料：使用中、排隊中、歷史紀錄。
        public UserReservationsResponse GetUserReservations(CurrentUser currentUser)
        {
            if (!currentUser.IsAuthenticated)
            {
                return new UserReservationsResponse();
            }

            var taiwanTime = GetTaiwanTime();
            var historyRetentionCutoff = taiwanTime.AddMonths(-1);
            _reservationRepository.DeleteHistoryReservationsOlderThan(currentUser.ReservationUserKey, historyRetentionCutoff);
            var scheduledReservations = _reservationRepository.GetScheduledReservations(currentUser.ReservationUserKey);
            var activeReservations = _reservationRepository.GetActiveReservations(currentUser.ReservationUserKey);
            var waitingReservations = _reservationRepository.GetWaitingQueues(currentUser.ReservationUserKey);
            var historyReservations = _reservationRepository.GetHistoryReservations(currentUser.ReservationUserKey);

            // 這裡是 Service 層很典型的工作：
            // Repository 只負責把資料拿回來，
            // 但前端需要的「剩餘時間」是經過計算的，所以在這裡補上最合適。
            var activeWithRemainingTime = activeReservations.Select(r =>
            {
                var remainingTime = CalculateRemainingTimeTaiwan(
                    r.StartTime,
                    r.AvailableTime
                );

                // 這裡改成回傳強型別 DTO 後，
                // 新手可以把它理解成「補資料」而不是「拼字典」。
                // 好處是欄位拼錯時，編譯階段就能先發現。
                return new ActiveReservationItem
                {
                    Id = r.Id,
                    EquipmentId = r.EquipmentId,
                    EquipmentName = r.EquipmentName,
                    UserId = r.UserId,
                    StartTime = r.StartTime,
                    AvailableTime = r.AvailableTime,
                    ReservationTime = r.ReservationTime,
                    Status = r.Status,
                    RemainingTime = remainingTime,
                    StatusText = string.IsNullOrWhiteSpace(r.StatusText)
                        ? ReservationDisplayHelper.GetStatusText(r.Status)
                        : r.StatusText,
                    StatusCssClass = string.IsNullOrWhiteSpace(r.StatusCssClass)
                        ? ReservationDisplayHelper.GetStatusCssClass(r.Status)
                        : r.StatusCssClass
                };
            }).ToList();

            return new UserReservationsResponse
            {
                ScheduledReservations = scheduledReservations,
                ActiveReservations = activeWithRemainingTime,
                WaitingReservations = waitingReservations,
                HistoryReservations = historyReservations,
                ServerTaiwanTime = taiwanTime.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public ReservationDashboardResponse GetReservationDashboard(ReservationDashboardFilter? filter = null)
        {
            var normalizedFilter = NormalizeDashboardFilter(filter);
            var taiwanTime = GetTaiwanTime();
            var activeReservations = _reservationRepository.GetAllActiveReservations()
                .Select(r =>
                {
                    var remainingTime = CalculateRemainingTimeTaiwan(r.StartTime, r.AvailableTime);
                    r.RemainingTime = remainingTime;
                    r.StatusText = string.IsNullOrWhiteSpace(r.StatusText)
                        ? ReservationDisplayHelper.GetStatusText(r.Status)
                        : r.StatusText;
                    r.StatusCssClass = string.IsNullOrWhiteSpace(r.StatusCssClass)
                        ? ReservationDisplayHelper.GetStatusCssClass(r.Status)
                        : r.StatusCssClass;
                    return r;
                })
                .ToList();

            var scheduledReservations = _reservationRepository.GetAllScheduledReservations();
            var waitingReservations = _reservationRepository.GetAllWaitingReservations();

            // 先由 Repository 把後台總覽需要的完整資料查齊，
            // 再由 Service 統一套用篩選，這樣條件規則會集中在同一層。
            if (!string.IsNullOrWhiteSpace(normalizedFilter.EquipmentKeyword))
            {
                scheduledReservations = scheduledReservations
                    .Where(r => r.EquipmentName.Contains(normalizedFilter.EquipmentKeyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                activeReservations = activeReservations
                    .Where(r => r.EquipmentName.Contains(normalizedFilter.EquipmentKeyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                waitingReservations = waitingReservations
                    .Where(r => r.EquipmentName.Contains(normalizedFilter.EquipmentKeyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter.UserKeyword))
            {
                scheduledReservations = scheduledReservations
                    .Where(r => r.UserId.Contains(normalizedFilter.UserKeyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                activeReservations = activeReservations
                    .Where(r => r.UserId.Contains(normalizedFilter.UserKeyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                waitingReservations = waitingReservations
                    .Where(r => r.UserId.Contains(normalizedFilter.UserKeyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (normalizedFilter.ScheduledStatus.HasValue)
            {
                scheduledReservations = scheduledReservations
                    .Where(r => r.Status == normalizedFilter.ScheduledStatus.Value)
                    .ToList();
            }

            if (normalizedFilter.WaitingQueueType.HasValue)
            {
                waitingReservations = waitingReservations
                    .Where(r => r.QueueType == normalizedFilter.WaitingQueueType.Value)
                    .ToList();
            }

            var equipmentSummaries = BuildEquipmentDashboardSummaries(
                scheduledReservations,
                activeReservations,
                waitingReservations);

            if (normalizedFilter.RiskOnly)
            {
                equipmentSummaries = equipmentSummaries
                    .Where(x => x.WaitingCount > 0 || x.QueueExpectedCount > 0 || x.RiskyScheduledCount > 0)
                    .ToList();

                var riskyEquipmentIds = equipmentSummaries
                    .Select(x => x.EquipmentId)
                    .ToHashSet();

                scheduledReservations = scheduledReservations
                    .Where(x => riskyEquipmentIds.Contains(x.EquipmentId))
                    .ToList();
                activeReservations = activeReservations
                    .Where(x => riskyEquipmentIds.Contains(x.EquipmentId))
                    .ToList();
                waitingReservations = waitingReservations
                    .Where(x => riskyEquipmentIds.Contains(x.EquipmentId))
                    .ToList();
            }

            return new ReservationDashboardResponse
            {
                ScheduledReservations = scheduledReservations,
                ActiveReservations = activeReservations,
                WaitingReservations = waitingReservations,
                EquipmentSummaries = equipmentSummaries,
                ServerTaiwanTime = taiwanTime.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public EquipmentReservationChainResponse GetEquipmentReservationChain(byte equipmentId)
        {
            var chain = _reservationRepository.GetEquipmentReservationChain(equipmentId);

            chain.ActiveReservations = chain.ActiveReservations
                .Select(r =>
                {
                    r.RemainingTime = CalculateRemainingTimeTaiwan(r.StartTime, r.AvailableTime);
                    r.StatusText = string.IsNullOrWhiteSpace(r.StatusText)
                        ? ReservationDisplayHelper.GetStatusText(r.Status)
                        : r.StatusText;
                    r.StatusCssClass = string.IsNullOrWhiteSpace(r.StatusCssClass)
                        ? ReservationDisplayHelper.GetStatusCssClass(r.Status)
                        : r.StatusCssClass;
                    return r;
                })
                .ToList();

            return chain;
        }

        // 單設備匯出沿用和設備鏈相同的整理結果，
        // 這樣管理者看到的內容與匯出的內容就不會出現差異。
        public byte[] ExportEquipmentReservationChainAsCsv(byte equipmentId)
        {
            var chain = GetEquipmentReservationChain(equipmentId);
            var csv = new StringBuilder();

            csv.AppendLine("設備鏈摘要");
            csv.AppendLine("設備,未來預約,使用中,排隊中");
            csv.AppendLine(string.Join(",",
                EscapeCsv(chain.EquipmentName),
                EscapeCsv(chain.ScheduledReservations.Count.ToString()),
                EscapeCsv(chain.ActiveReservations.Count.ToString()),
                EscapeCsv(chain.WaitingReservations.Count.ToString())));
            csv.AppendLine();

            csv.AppendLine("設備,區塊,會員,狀態或類型,時間一,時間二,附加資訊");

            foreach (var reservation in chain.ScheduledReservations)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(chain.EquipmentName),
                    EscapeCsv("未來預約"),
                    EscapeCsv(reservation.UserId),
                    EscapeCsv(reservation.StatusText),
                    EscapeCsv(reservation.ReservedStartTime.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv(reservation.ReservedEndTime.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv(reservation.RiskSummary)));
            }

            foreach (var reservation in chain.ActiveReservations)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(chain.EquipmentName),
                    EscapeCsv("使用中"),
                    EscapeCsv(reservation.UserId),
                    EscapeCsv(reservation.StatusText),
                    EscapeCsv(reservation.StartTime.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv($"{reservation.RemainingTime} 分鐘"),
                    EscapeCsv(string.Empty)));
            }

            foreach (var queue in chain.WaitingReservations)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(chain.EquipmentName),
                    EscapeCsv("排隊中"),
                    EscapeCsv(queue.UserId),
                    EscapeCsv(queue.QueueTypeText),
                    EscapeCsv(queue.QueueTime.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv($"第 {queue.Position} 位"),
                    EscapeCsv(string.Empty)));
            }

            var bom = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(csv.ToString());
            return [.. bom, .. content];
        }

        // 預約前的快速檢查：這裡不會真的建立預約，
        // 只是告訴前端目前是否可預約、設備是否已滿、是否在開放時間。
        public EquipmentAvailabilityResponse CheckEquipmentAvailability(byte equipmentId)
        {
            var equipment = _reservationRepository.GetEquipmentById(equipmentId);
            if (equipment == null)
            {
                return new EquipmentAvailabilityResponse
                {
                    IsAvailable = false,
                    CanReserve = false,
                    IsFull = false,
                    Message = "設備不存在",
                    ServerTaiwanTime = GetTaiwanTime().ToString("yyyy-MM-dd HH:mm:ss")
                };
            }

            var taiwanTime = GetTaiwanTime();
            var currentTimeOfDay = taiwanTime.TimeOfDay;
            var isInOperatingHours = currentTimeOfDay >= equipment.OpenTime &&
                                     currentTimeOfDay <= equipment.CloseTime;
            var currentUsers = _reservationRepository.GetCurrentUsers(equipmentId);
            var reservedCapacityCount = _reservationRepository.GetReservedCapacityCount(
                equipmentId,
                taiwanTime,
                taiwanTime.AddMinutes(equipment.AvailableTime));
            var effectiveCapacity = Math.Max(0, equipment.MaxUsers - reservedCapacityCount);
            var isWithinCapacity = currentUsers < effectiveCapacity;

            string message;
            if (!isInOperatingHours)
            {
                message = $"非開放時間（台灣時間: {taiwanTime:HH:mm}，開放時段: {equipment.OpenTime:hh\\:mm}-{equipment.CloseTime:hh\\:mm}）";
            }
            else if (!isWithinCapacity)
            {
                message = reservedCapacityCount > 0 && currentUsers < equipment.MaxUsers
                    ? "後續時段已有未來預約保留名額，現在建立將改為加入排隊"
                    : "設備已滿，點擊預約將加入排隊";
            }
            else
            {
                message = "可預約";
            }

            return new EquipmentAvailabilityResponse
            {
                IsAvailable = isInOperatingHours,
                CanReserve = isInOperatingHours,
                IsFull = !isWithinCapacity,
                Message = message,
                CurrentUsers = currentUsers,
                MaxUsers = equipment.MaxUsers,
                AverageUsageTime = equipment.AvailableTime,
                ServerTaiwanTime = taiwanTime.ToString("yyyy-MM-dd HH:mm:ss"),
                OpenTime = equipment.OpenTime.ToString(@"hh\:mm"),
                CloseTime = equipment.CloseTime.ToString(@"hh\:mm")
            };
        }

        // 統一取得台灣時間，避免時區計算散落在各處。
        private DateTime GetTaiwanTime()
        {
            try
            {
                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(8);
            }
        }

        // 剩餘時間 = 可使用分鐘數 - 已過去分鐘數。
        // 特別注意：如果 StartTime 是 UTC，要先換成台灣時間再算。
        private int CalculateRemainingTimeTaiwan(DateTime startTime, int availableTime)
        {
            try
            {
                var taiwanTime = GetTaiwanTime();

                if (startTime.Kind == DateTimeKind.Utc)
                {
                    var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                    startTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, taiwanTimeZone);
                }

                var elapsedMinutes = (int)(taiwanTime - startTime).TotalMinutes;
                var remaining = availableTime - elapsedMinutes;
                return Math.Max(0, remaining);
            }
            catch
            {
                return 0;
            }
        }

        // 匯出沿用和後台總覽相同的篩選規則，
        // 這樣管理者匯出的資料就會和畫面看到的內容一致。
        public byte[] ExportReservationDashboardAsCsv(ReservationDashboardFilter? filter = null)
        {
            var dashboard = GetReservationDashboard(filter);
            var csv = new StringBuilder();

            csv.AppendLine("總覽摘要");
            csv.AppendLine("匯出時間,未來預約總數,使用中總數,排隊中總數,設備摘要筆數");
            csv.AppendLine(string.Join(",",
                EscapeCsv(dashboard.ServerTaiwanTime),
                EscapeCsv(dashboard.ScheduledReservations.Count.ToString()),
                EscapeCsv(dashboard.ActiveReservations.Count.ToString()),
                EscapeCsv(dashboard.WaitingReservations.Count.ToString()),
                EscapeCsv(dashboard.EquipmentSummaries.Count.ToString())));
            csv.AppendLine();

            csv.AppendLine("設備摘要");
            csv.AppendLine("設備,未來預約,使用中,排隊中,預約排隊風險,高風險預約,壓力等級,摘要說明");
            foreach (var summary in dashboard.EquipmentSummaries)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(summary.EquipmentName),
                    EscapeCsv(summary.ScheduledCount.ToString()),
                    EscapeCsv(summary.ActiveCount.ToString()),
                    EscapeCsv(summary.WaitingCount.ToString()),
                    EscapeCsv(summary.QueueExpectedCount.ToString()),
                    EscapeCsv(summary.RiskyScheduledCount.ToString()),
                    EscapeCsv(summary.PressureLevel),
                    EscapeCsv(summary.PressureSummary)));
            }

            csv.AppendLine();
            csv.AppendLine("區塊,設備,會員,狀態或類型,時間一,時間二,附加資訊");

            foreach (var reservation in dashboard.ScheduledReservations)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv("未來預約"),
                    EscapeCsv(reservation.EquipmentName),
                    EscapeCsv(reservation.UserId),
                    EscapeCsv(reservation.StatusText),
                    EscapeCsv(reservation.ReservedStartTime.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv(reservation.ReservedEndTime.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv(reservation.RiskSummary)));
            }

            foreach (var reservation in dashboard.ActiveReservations)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv("使用中"),
                    EscapeCsv(reservation.EquipmentName),
                    EscapeCsv(reservation.UserId),
                    EscapeCsv(reservation.StatusText),
                    EscapeCsv(reservation.StartTime.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv($"{reservation.RemainingTime} 分鐘"),
                    EscapeCsv(string.Empty)));
            }

            foreach (var queue in dashboard.WaitingReservations)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv("排隊中"),
                    EscapeCsv(queue.EquipmentName),
                    EscapeCsv(queue.UserId),
                    EscapeCsv(queue.QueueTypeText),
                    EscapeCsv(queue.QueueTime.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv($"第 {queue.Position} 位"),
                    EscapeCsv(string.Empty)));
            }

            // 加上 UTF-8 BOM，讓 Excel 開啟時能正確顯示中文。
            var bom = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(csv.ToString());
            return [.. bom, .. content];
        }

        private static ReservationDashboardFilter NormalizeDashboardFilter(ReservationDashboardFilter? filter)
        {
            return new ReservationDashboardFilter
            {
                EquipmentKeyword = filter?.EquipmentKeyword?.Trim(),
                UserKeyword = filter?.UserKeyword?.Trim(),
                ScheduledStatus = filter?.ScheduledStatus > 0 ? filter?.ScheduledStatus : null,
                WaitingQueueType = filter?.WaitingQueueType > 0 ? filter?.WaitingQueueType : null,
                RiskOnly = filter?.RiskOnly == true
            };
        }

        private static string EscapeCsv(string value)
        {
            var normalized = value.Replace("\"", "\"\"");
            return $"\"{normalized}\"";
        }

        // 這裡先把三種清單整理成「每台設備一筆」的摘要，
        // 讓後台驗收時不用先翻完整表格，就能知道哪台設備目前壓力最大。
        private static List<EquipmentDashboardSummaryItem> BuildEquipmentDashboardSummaries(
            List<ScheduledReservationItem> scheduledReservations,
            List<ActiveReservationItem> activeReservations,
            List<WaitingReservationItem> waitingReservations)
        {
            var summaryMap = new Dictionary<byte, EquipmentDashboardSummaryItem>();

            foreach (var reservation in scheduledReservations)
            {
                var summary = GetOrCreateSummary(summaryMap, reservation.EquipmentId, reservation.EquipmentName);
                summary.ScheduledCount++;

                if (reservation.QueueExpected)
                {
                    summary.QueueExpectedCount++;
                }

                if (!string.IsNullOrWhiteSpace(reservation.RiskSummary))
                {
                    summary.RiskyScheduledCount++;
                }
            }

            foreach (var reservation in activeReservations)
            {
                var summary = GetOrCreateSummary(summaryMap, reservation.EquipmentId, reservation.EquipmentName);
                summary.ActiveCount++;
            }

            foreach (var queue in waitingReservations)
            {
                var summary = GetOrCreateSummary(summaryMap, queue.EquipmentId, queue.EquipmentName);
                summary.WaitingCount++;
            }

            return summaryMap.Values
                .Select(ApplyPressureSummary)
                .OrderByDescending(x => x.WaitingCount)
                .ThenByDescending(x => x.QueueExpectedCount)
                .ThenByDescending(x => x.RiskyScheduledCount)
                .ThenBy(x => x.EquipmentName)
                .ToList();
        }

        private static EquipmentDashboardSummaryItem GetOrCreateSummary(
            Dictionary<byte, EquipmentDashboardSummaryItem> summaryMap,
            byte equipmentId,
            string equipmentName)
        {
            if (summaryMap.TryGetValue(equipmentId, out var existing))
            {
                return existing;
            }

            var created = new EquipmentDashboardSummaryItem
            {
                EquipmentId = equipmentId,
                EquipmentName = equipmentName
            };
            summaryMap[equipmentId] = created;
            return created;
        }

        // 壓力等級不是精密排程演算法，而是給管理者快速掃描用的風險摘要。
        // 目標是讓驗收時先看到「哪台設備最需要關注」，再決定是否展開明細。
        private static EquipmentDashboardSummaryItem ApplyPressureSummary(EquipmentDashboardSummaryItem summary)
        {
            if (summary.WaitingCount >= 3 || summary.QueueExpectedCount >= 2 || summary.RiskyScheduledCount >= 3)
            {
                summary.PressureLevel = "高壓";
                summary.PressureCssClass = "bg-danger";
                summary.PressureSummary = BuildPressureSummary(summary, "目前排隊與預約衝突明顯，建議優先查看設備鏈。");
                return summary;
            }

            if (summary.WaitingCount > 0 || summary.QueueExpectedCount > 0 || summary.RiskyScheduledCount > 0 || summary.ActiveCount >= 2)
            {
                summary.PressureLevel = "中壓";
                summary.PressureCssClass = "bg-warning text-dark";
                summary.PressureSummary = BuildPressureSummary(summary, "已有壅塞跡象，建議留意後續排隊與未來預約變化。");
                return summary;
            }

            summary.PressureLevel = "低壓";
            summary.PressureCssClass = "bg-success";
            summary.PressureSummary = "目前設備負載穩定，尚未出現明顯排隊或預約衝突。";
            return summary;
        }

        // 把數字翻成一句容易讀的摘要，讓管理者不用先心算就能理解壓力來源。
        private static string BuildPressureSummary(EquipmentDashboardSummaryItem summary, string defaultSummary)
        {
            var reasons = new List<string>();

            if (summary.WaitingCount > 0)
            {
                reasons.Add($"目前有 {summary.WaitingCount} 筆排隊");
            }

            if (summary.QueueExpectedCount > 0)
            {
                reasons.Add($"有 {summary.QueueExpectedCount} 筆預約預計轉排隊");
            }

            if (summary.RiskyScheduledCount > 0)
            {
                reasons.Add($"有 {summary.RiskyScheduledCount} 筆未來預約帶風險");
            }

            if (reasons.Count == 0)
            {
                return defaultSummary;
            }

            return $"{string.Join("，", reasons)}。{defaultSummary}";
        }
    }

}
