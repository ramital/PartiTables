using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PartiTables;
using PartiSample.GetStarted.PartiTables;

var builder = Host.CreateApplicationBuilder(args);

// Setup PartiTables for multiple tables
builder.Services.AddPartiTables(opts =>
{
    opts.ConnectionString = "UseDevelopmentStorage=true";
    opts.TableName = "Default";
});

// Register repositories for different tables
builder.Services.AddPartitionRepository<Customer>();
builder.Services.AddPartitionRepository<User>();

var app = builder.Build();

var customerRepo = app.Services.GetRequiredService<PartitionRepository<Customer>>();
var userRepo = app.Services.GetRequiredService<PartitionRepository<User>>();

//Start using the repositories
var customer = new Customer
{
    CustomerId = "cust-001",
    Name = "John Doe"
};
customer.Orders.Add(new Order { OrderId = "order-001", Amount = 99.99m });

await customerRepo.SaveAsync(customer);

var loaded = await customerRepo.FindAsync("cust-001");
Console.WriteLine($"Loaded {loaded?.Name} with {loaded?.Orders.Count} orders");


var user = new User
{
    UserId = "user-001"
};
user.Tasks.Add(new PartiSample.GetStarted.PartiTables.Task { TaskId = "task-001", load = 100 });

await userRepo.SaveAsync(user);

var loaded2 = await userRepo.FindAsync("user-001");
Console.WriteLine($"Loaded {loaded2?.Tasks.Count} ");

Console.ReadLine();
