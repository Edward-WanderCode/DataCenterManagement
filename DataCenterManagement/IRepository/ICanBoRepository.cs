using DataCenterManagement.Models;

namespace DataCenterManagement.IRepository
{
    public interface ICanBoRepository
    {
        Task<List<CanBo>> GetAllAsync();
    }
}