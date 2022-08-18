using ShoppingWebApp.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoppingWebApp.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderResponseModel>> GetOrdersByUserName(string userName);
    }
}
