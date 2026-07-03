using System.Collections.Generic;
using System.Threading.Tasks;
using FocusTrack.Business.Interfaces;
using FocusTrack.Data.Entities;
using FocusTrack.Data.Repositories;

namespace FocusTrack.Business.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly INotificationService _notificationService;

        public CategoryService(
            ICategoryRepository categoryRepository,
            IApplicationRepository applicationRepository,
            ISessionRepository sessionRepository,
            INotificationService notificationService)
        {
            _categoryRepository = categoryRepository;
            _applicationRepository = applicationRepository;
            _sessionRepository = sessionRepository;
            _notificationService = notificationService;
        }

        public Task<List<Category>> GetCategoriesAsync() => _categoryRepository.GetAllAsync();

        public Task<Category> AddCategoryAsync(string name, string colorHex)
            => _categoryRepository.AddAsync(new Category
            {
                Name = name,
                ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#808080" : colorHex,
                DailyGoalMinutes = 0,
                IsSystemDefault = false
            });

        public Task ClassifyApplicationAsync(int applicationId, int? categoryId)
            => _applicationRepository.SetCategoryAsync(applicationId, categoryId);

        public Task SetIgnoredAsync(int applicationId, bool isIgnored)
            => _applicationRepository.SetIgnoredAsync(applicationId, isIgnored);

        public Task SetDailyGoalAsync(int categoryId, int minutes)
            => _categoryRepository.UpdateGoalAsync(categoryId, minutes);

        public async Task CheckGoalsAsync(int profileId)
        {
            var totals = await _sessionRepository.GetTodayTotalsAsync(profileId);

            foreach (var total in totals)
            {
                if (total.GoalMinutes <= 0) continue;           // no goal set
                if (total.TotalMinutes < total.GoalMinutes) continue; // goal not exceeded

                bool alreadyNotified = await _categoryRepository.HasNotifiedTodayAsync(total.CategoryId);
                if (alreadyNotified) continue;

                var category = new Category
                {
                    Id = total.CategoryId,
                    Name = total.CategoryName,
                    ColorHex = total.ColorHex,
                    DailyGoalMinutes = total.GoalMinutes
                };

                _notificationService.ShowGoalExceededNotification(category, total.TotalMinutes);
                await _categoryRepository.RecordNotificationAsync(total.CategoryId);
            }
        }
    }
}
