using DataCenterManagement.IRepository;
using DataCenterManagement.Models;
using DataCenterManagement.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenterManagement.Repository
{
    public class CanBoRepository : ICanBoRepository
    {

        Task<List<CanBo>> ICanBoRepository.GetAllAsync()
        {
            throw new NotImplementedException();
        }
    }
}
