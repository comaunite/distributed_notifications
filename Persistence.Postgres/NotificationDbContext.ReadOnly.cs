using Microsoft.EntityFrameworkCore;

namespace Persistence.Postgres;

public class ReadOnlyNotificationDbContext(DbContextOptions options) : NotificationDbContext(options)
{

}