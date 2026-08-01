using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskMgmt.Domain.Common;

namespace TaskMgmt.Infrastructure.Persistence.Interceptors;

// Dò domain event đang chờ trên các entity được track ngay TRƯỚC khi SaveChanges ghi DB thật.
// Handler của event chỉ Add() thêm entity (vd. TaskHistory) vào cùng DbContext, không tự gọi
// SaveChanges - nhờ vậy EF Core gộp luôn các thay đổi đó vào transaction đang chạy, đảm bảo
// thay đổi nghiệp vụ và dòng lịch sử luôn được ghi cùng lúc hoặc không ghi gì cả.
public class DispatchDomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var entitiesWithEvents = context.ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
