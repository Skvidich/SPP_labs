using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SampleApp.Models;
using SampleApp.Services;
using TestFramework.Attributes;
using TestFramework.Context;

namespace Tests.E2E
{
    [TestClass]
    [TestE2E] 
    [Category("e2e")]
    public class SuccessfulPurchaseScenario : IUseSharedContext
    {
        public GlobalContext Context { get; set; }

        [ClassInitialize]
        public static void SetupServices(GlobalContext ctx)
        {
            Console.WriteLine(">>> [Setup] Initializing Real Services...");

            var inventory = new InventoryService();
            var payment = new PaymentGateway();
            var notification = new NotificationService();
            var orderService = new OrderService(inventory, payment, notification);

            inventory.AddStock("LAPTOP", "Gaming Laptop", 100m, 10); 

            ctx.SetData("Service_Inventory", inventory);
            ctx.SetData("Service_Order", orderService);

            ctx.SetData("Sku", "LAPTOP");
            ctx.SetData("InitialStock", 10);
        }

        [TestMethod]
        [Order(1)]
        public void CreateCustomer()
        {
            Console.WriteLine("[Step 1] Creating Customer...");
            var user = new User
            {
                Name = "John Doe",
                Email = "john@example.com",
                LoyaltyPoints = 0
            };
            Context.SetData("User", user);
        }

        [TestMethod]
        [Order(2)]
        public async Task ProcessCheckout()
        {
            Console.WriteLine("[Step 2] Processing Checkout...");

            var orderService = Context.GetData<OrderService>("Service_Order");
            var user = Context.GetData<User>("User");
            var sku = Context.GetData<string>("Sku");

            var items = new List<OrderItem>
            {
                new OrderItem { Sku = sku, Quantity = 2 }
            };

            Order resultOrder = await orderService.CheckoutAsync(user, items);

            Context.SetData("ResultOrder", resultOrder);
        }

        [TestMethod]
        [Order(3)]
        public void VerifyOrderStatus()
        {
            Console.WriteLine("[Step 3] Verifying Order Status...");
            var order = Context.GetData<Order>("ResultOrder");

            if (order.Status != OrderStatus.Completed)
                throw new Exception($"Expected Completed, but got {order.Status}");

            if (order.TotalAmount != 200m)
                throw new Exception($"Expected Total 200, but got {order.TotalAmount}");
        }

        [TestMethod]
        [Order(4)]
        public void VerifyInventoryDecreased()
        {
            Console.WriteLine("[Step 4] Verifying Inventory...");

            var inventory = Context.GetData<InventoryService>("Service_Inventory");
            var sku = Context.GetData<string>("Sku");

            var product = inventory.GetProduct(sku);

            if (product.StockQuantity != 8)
                throw new Exception($"Stock error! Expected 8, got {product.StockQuantity}");
        }

        [TestMethod]
        [Order(5)]
        public void VerifyLoyaltyPointsAdded()
        {
            Console.WriteLine("[Step 5] Verifying Loyalty Points...");
            var user = Context.GetData<User>("User");

            if (user.LoyaltyPoints != 20)
                throw new Exception($"Loyalty error! Expected 20 points, got {user.LoyaltyPoints}");
        }
    }
}