using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LoadShedding.Application.Services
{
    public interface IEskomService
    {
        Task<int> GetEskomStage();
    }
}
