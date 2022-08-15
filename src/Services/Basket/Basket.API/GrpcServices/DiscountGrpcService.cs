using Discount.Grpc.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.GrpcServices
{
    public class DiscountGrpcService
    {
        private readonly Discount.Grpc.Protos.Discount.DiscountClient _discountClient;

        public DiscountGrpcService(Discount.Grpc.Protos.Discount.DiscountClient discountClient)
        {
            _discountClient = discountClient ?? throw new ArgumentNullException(nameof(discountClient));
        }

        public async Task<CouponModel> GetDiscount(string productName)
        {
            var discountRequest = new Discount.Grpc.Protos.GetDiscountRequest {ProductName = productName };
            return await _discountClient.GetDiscountAsync(discountRequest);
        }
    }
}
