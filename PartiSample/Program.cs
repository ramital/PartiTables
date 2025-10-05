using Microsoft.Extensions.DependencyInjection;
using PartiSample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Clear();
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║      PartiTables Demo Application     ║");
        Console.WriteLine("║   Azure Table Storage Made Easy       ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        // Display demo menu
        DemoRegistry.DisplayMenu();
        
        // Get user selection
        var choice = Console.ReadLine();

        // Handle "Run All" option
        if (choice == "0")
        {
            var dummyServices = new ServiceCollection();
            var sp = dummyServices.BuildServiceProvider();
            
            try
            {
                await DemoRegistry.RunAllDemosAsync(sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n Error running demos: {ex.Message}");
                Console.WriteLine($"   {ex.StackTrace}");
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
            return;
        }

        // Handle single demo selection
        var selectedDemo = DemoRegistry.GetDemo(choice ?? "1");

        if (selectedDemo == null)
        {
            Console.WriteLine($"\n⚠️  Invalid choice '{choice}'. Running default demo...");
            selectedDemo = DemoRegistry.Demos[0];
        }

        Console.Clear();
        Console.WriteLine($"▶ Running: {selectedDemo.Name}");
        Console.WriteLine($"  {selectedDemo.Description}\n");

        // Setup dependency injection with selected demo
        var services = new ServiceCollection();
        DemoRegistry.ConfigureServices(services, selectedDemo);
        var serviceProvider = services.BuildServiceProvider();

        // Run the demo
        try
        {
            await selectedDemo.RunAction(serviceProvider);
            Console.WriteLine("\n✅ Demo completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n Demo failed: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
        }

        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }
}