using Microsoft.EntityFrameworkCore;
using Notification.Service.Data;
using Notification.Service.Entities;

namespace Notification.Service.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        // Historial de notificaciones del usuario
        app.MapGet("/notifications/{userId:guid}", async (
            Guid userId,
            NotificationDbContext dbContext) =>
        {
            var notifications = await dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Results.Ok(notifications);
        });

        // Actualizar preferencias de notificación
        app.MapPost("/notifications/preferences", async (
            NotificationPreferenceRequest request,
            NotificationDbContext dbContext) =>
        {
            var preference = await dbContext.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == request.UserId);

            if (preference is null)
            {
                preference = new NotificationPreference(request.UserId);
                dbContext.NotificationPreferences.Add(preference);
            }

            preference.Update(
                request.EmailEnabled,
                request.SignalREnabled,
                request.LowScoreThreshold);

            await dbContext.SaveChangesAsync();

            return Results.Ok(preference);
        });
    }
}

public record NotificationPreferenceRequest(
    Guid UserId,
    bool EmailEnabled,
    bool SignalREnabled,
    int LowScoreThreshold);