using ApsSettings.Data;
using ApsSettings.Data.Models;

namespace Bulk_Uploader_Electron.Jobs
{
    public class BatchJobExecution
    {

        public readonly DataContext _context;

        public BatchJobExecution(DataContext dataContext)
        {
            _context = dataContext;
        }

 
    }
}
