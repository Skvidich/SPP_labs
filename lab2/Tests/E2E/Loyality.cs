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
    public class LoyaltyDiscountScenario : IUseSharedContext
    {
        public GlobalContext Context { get; set; }

        [ClassInitialize]
        public static void Setup(GlobalContext ctx)
        {
            var inventory = new InventoryService();
            inventory.AddStock("PHONE", "Smartphone", 500m, 10);

            var orderService = new OrderService(inventory, new PaymentGateway(), new NotificationService());

            ctx.SetData("Service_Order", orderService);
            ctx.SetData("Sku", "PHONE");
        }

        [TestMethod]
        [Order(1)]
        public async Task PurchaseWithLoyaltyPoints()
        {
            Console.WriteLine("[Step 1] User with 150 points buys item...");

            var orderService = Context.GetData<OrderService>("Service_Order");
            var sku = Context.GetData<string>("Sku");

            var user = new User { LoyaltyPoints = 150 };
            Context.SetData("User", user);

            var items = new List<OrderItem> { new OrderItem { Sku = sku, Quantity = 1 } };

            Order order = await orderService.CheckoutAsync(user, items);
            Context.SetData("ResultOrder", order);
        }

        [TestMethod]
        [Order(2)]
        public void VerifyDiscountApplied()
        {
            var order = Context.GetData<Order>("ResultOrder");

            decimal expectedDiscount = 50m;
            decimal expectedTotal = 450m;

            Console.WriteLine($"[Step 2] Checking Discount: {order.DiscountApplied}");

            if (order.DiscountApplied != expectedDiscount)
                throw new Exception($"Discount error! Expected {expectedDiscount}, got {order.DiscountApplied}");

            if (order.TotalAmount != expectedTotal)
                throw new Exception($"Total amount error! Expected {expectedTotal}, got {order.TotalAmount}");
        }

        [TestMethod]
        [Order(3)]
        public void VerifyPointsDeducted()
        {
            var user = Context.GetData<User>("User");

            Console.WriteLine($"[Step 3] Checking Points: {user.LoyaltyPoints}");

            if (user.LoyaltyPoints != 140)
                throw new Exception($"Points error! Expected 140, got {user.LoyaltyPoints}");
        }
    }
}