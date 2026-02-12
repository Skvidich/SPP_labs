using System.Diagnostics;
using SampleApp.Models;
using SampleApp.Services;
using TestFramework.Attributes;
using TestFramework.Context;

namespace Tests.Performance
{
    [TestClass]
    [Category("perf")]
    public class PaymentPerformanceTests : IUseSharedContext
    {
        public GlobalContext Context { get; set; }

        private const int BANK_DELAY_MS = 500;

        private OrderService CreateSlowOrderService()
        {
            var inventory = new InventoryService();
            inventory.AddStock("SLOW_ITEM", "Heavy Processing Item", 100m, 1000);

            var slowPaymentGateway = new SlowPaymentGateway(BANK_DELAY_MS);

            return new OrderService(inventory, slowPaymentGateway, new NotificationService());
        }

        private class SlowPaymentGateway : SampleApp.Interfaces.IPaymentGateway
        {
            private readonly int _delay;
            public SlowPaymentGateway(int delay) { _delay = delay; }

            public async Task<bool> ChargeAsync(string email, decimal amount)
            {
                await Task.Delay(_delay);
                return true;
            }
        }

        [TestMethod]
        public async Task ProcessOrder_1()
        {
            await RunOrderProcess("User 1");
        }

        [TestMethod]
        public async Task ProcessOrder_2()
        {
            await RunOrderProcess("User 2");
        }

        [TestMethod]
        public async Task ProcessOrder_3()
        {
            await RunOrderProcess("User 3");
        }

        [TestMethod]
        public async Task ProcessOrder_4()
        {
            await RunOrderProcess("User 4");
        }

        [TestMethod]
        public async Task ProcessOrder_5()
        {
            await RunOrderProcess("User 5");
        }

        [TestMethod]
        public async Task ProcessOrder_6()
        {
            await RunOrderProcess("User 6");
        }

        [TestMethod]
        public async Task ProcessOrder_7()
        {
            await RunOrderProcess("User 7");
        }

        [TestMethod]
        public async Task ProcessOrder_8()
        {
            await RunOrderProcess("User 8");
        }

        [TestMethod]
        public async Task ProcessOrder_9()
        {
            await RunOrderProcess("User 9");
        }

        [TestMethod]
        public async Task ProcessOrder_10()
        {
            await RunOrderProcess("User 10");
        }

        private async Task RunOrderProcess(string userName)
        {
            var service = CreateSlowOrderService();
            var user = new User { Email = $"{userName.Replace(" ", "")}@test.com" };
            var items = new List<OrderItem> { new OrderItem { Sku = "SLOW_ITEM", Quantity = 1 } };

            Console.WriteLine($"   -> {userName}: Sending payment request to bank...");

            var stopwatch = Stopwatch.StartNew();

            await service.CheckoutAsync(user, items);

            stopwatch.Stop();
            Console.WriteLine($"   <- {userName}: Payment approved in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}