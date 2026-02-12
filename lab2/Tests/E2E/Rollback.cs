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
    public class PaymentFailureRollbackScenario : IUseSharedContext
    {
        public GlobalContext Context { get; set; }

        [ClassInitialize]
        public static void Setup(GlobalContext ctx)
        {
            var inventory = new InventoryService();
            inventory.AddStock("GOLD_BAR", "Gold", 2000m, 5);

            var orderService = new OrderService(inventory, new PaymentGateway(), new NotificationService());

            ctx.SetData("Service_Inventory", inventory);
            ctx.SetData("Service_Order", orderService);
            ctx.SetData("Sku", "GOLD_BAR");
        }

        [TestMethod]
        [Order(1)]
        public async Task AttemptExpensivePurchase()
        {
            Console.WriteLine("[Step 1] Attempting to buy expensive item (> $1000)...");

            var orderService = Context.GetData<OrderService>("Service_Order");
            var sku = Context.GetData<string>("Sku");

            var user = new User { Name = "Rich Guy", Email = "rich@test.com", LoyaltyPoints = 50 };
            Context.SetData("User", user);

            var items = new List<OrderItem>
            {
                new OrderItem { Sku = sku, Quantity = 1 }
            };

            Order order = await orderService.CheckoutAsync(user, items);

            Context.SetData("ResultOrder", order);
        }

        [TestMethod]
        [Order(2)]
        public void VerifyOrderFailed()
        {
            var order = Context.GetData<Order>("ResultOrder");
            Console.WriteLine($"[Step 2] Order Status: {order.Status}");

            if (order.Status != OrderStatus.Failed)
                throw new Exception("Order should have failed due to payment limit!");
        }

        [TestMethod]
        [Order(3)]
        public void VerifyStockRolledBack()
        {
            Console.WriteLine("[Step 3] Verifying stock was returned...");

            var inventory = Context.GetData<InventoryService>("Service_Inventory");
            var sku = Context.GetData<string>("Sku");

            var product = inventory.GetProduct(sku);

            if (product.StockQuantity != 5)
                throw new Exception($"Rollback failed! Expected 5 items, found {product.StockQuantity}");
        }

        [TestMethod]
        [Order(4)]
        public void VerifyPointsNotChanged()
        {
            var user = Context.GetData<User>("User");

            if (user.LoyaltyPoints != 50)
                throw new Exception($"Loyalty points corrupted! Expected 50, got {user.LoyaltyPoints}");
        }
    }
}