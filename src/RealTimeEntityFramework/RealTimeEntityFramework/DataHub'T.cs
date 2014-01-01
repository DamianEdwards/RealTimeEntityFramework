using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    public abstract class DataHub<TDbContext> where TDbContext : DbContext
    {
        
    }
}
