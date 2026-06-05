using Microsoft.EntityFrameworkCore;

namespace PosSystem.Infrastructure;

public class PosDbContext(DbContext<PosDbContext> options): DbContext(options)
{
    //
}
