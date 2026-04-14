using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface INotificationService
{
    Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<Notification> CreateNotificationAsync(Notification notification);
    Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId);
    Task<int> GetUnreadCountAsync(int userId);
    
    // Document-specific notification helpers
    Task CreateDocumentShareNotificationAsync(int recipientUserId, string documentTitle, string? message = null);
    Task CreateDocumentTaskAttachmentNotificationAsync(int taskOwnerId, string documentTitle, string taskTitle);
    Task CreateDocumentProjectAttachmentNotificationAsync(int projectManagerId, string documentTitle, string projectName);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedDate)
            .Take(50)
            .ToListAsync();
    }

    public async Task<Notification> CreateNotificationAsync(Notification notification)
    {
        notification.CreatedDate = DateTime.UtcNow;
        notification.IsRead = false;

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null) return false;

        // Authorization: Users can only mark their own notifications as read
        if (notification.UserId != requestingUserId)
        {
            return false; // User not authorized to mark this notification as read
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    /// <summary>
    /// Create a notification when a document is shared with a user.
    /// </summary>
    public async Task CreateDocumentShareNotificationAsync(int recipientUserId, string documentTitle, string? message = null)
    {
        var notification = new Notification
        {
            UserId = recipientUserId,
            Title = "Document Shared",
            Message = $"A document '{documentTitle}' has been shared with you." + (string.IsNullOrEmpty(message) ? "" : $"\n\nMessage: {message}"),
            Type = NotificationType.DocumentShareReceived,
            Priority = NotificationPriority.Important,
            IsRead = false,
            CreatedDate = DateTime.UtcNow
        };

        await CreateNotificationAsync(notification);
    }

    /// <summary>
    /// Create a notification when a document is attached to a task.
    /// </summary>
    public async Task CreateDocumentTaskAttachmentNotificationAsync(int taskOwnerId, string documentTitle, string taskTitle)
    {
        var notification = new Notification
        {
            UserId = taskOwnerId,
            Title = "Document Attached to Task",
            Message = $"A document '{documentTitle}' has been attached to the task '{taskTitle}'.",
            Type = NotificationType.DocumentTaskAttached,
            Priority = NotificationPriority.Important,
            IsRead = false,
            CreatedDate = DateTime.UtcNow
        };

        await CreateNotificationAsync(notification);
    }

    /// <summary>
    /// Create a notification when a document is attached to a project.
    /// </summary>
    public async Task CreateDocumentProjectAttachmentNotificationAsync(int projectManagerId, string documentTitle, string projectName)
    {
        var notification = new Notification
        {
            UserId = projectManagerId,
            Title = "Document Attached to Project",
            Message = $"A document '{documentTitle}' has been attached to the project '{projectName}'.",
            Type = NotificationType.DocumentProjectAttached,
            Priority = NotificationPriority.Important,
            IsRead = false,
            CreatedDate = DateTime.UtcNow
        };

        await CreateNotificationAsync(notification);
    }
}
