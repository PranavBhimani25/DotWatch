# DotWatch - ASP.NET Core Performance Monitoring Dashboard

A comprehensive performance monitoring solution for **ASP.NET Core MVC** applications built with **Prometheus** and **Grafana**. This project provides real-time insights into application performance, system metrics, and infrastructure health specifically designed for .NET applications.

## 📸 Preview 
![image alt](https://github.com/PranavBhimani25/DotWatch/blob/55d12d286f5edb3feff4bfac7f080dde2eb314f3/DotWatch/wwwroot/Images/Screenshot%202025-05-31%20174546.png)
![image alt](https://github.com/PranavBhimani25/DotWatch/blob/55d12d286f5edb3feff4bfac7f080dde2eb314f3/DotWatch/wwwroot/Images/Screenshot2025_1.png)
![image alt](https://github.com/PranavBhimani25/DotWatch/blob/55d12d286f5edb3feff4bfac7f080dde2eb314f3/DotWatch/wwwroot/Images/Screenshot%202025-05-31%20174526.png)


## 🚀 Features

- **ASP.NET Core Integration**: Built-in metrics collection for .NET applications
- **Real-time Monitoring**: Live application and system metrics
- **Custom MVC Metrics**: Controller performance, request tracking, and response times
- **Visual Dashboards**: Interactive Grafana dashboards tailored for .NET applications
- **Health Checks**: ASP.NET Core health check endpoints integration
- **Exception Tracking**: Monitor application errors and exceptions
- **Database Performance**: Entity Framework and SQL Server metrics
- **Memory & GC Monitoring**: .NET garbage collection and memory usage tracking

## 🛠️ Tech Stack

- **ASP.NET Core MVC** (.NET 6/7/8)
- **Prometheus**: Time-series database for metrics collection
- **Grafana**: Visualization and dashboarding platform
- **prometheus-net**: .NET client library for Prometheus
- **prometheus-net.AspNetCore**: ASP.NET Core integration
- **Docker**: Containerized deployment
- **Entity Framework Core**: Database monitoring (optional)

## 📋 Prerequisites

- **.NET SDK** (6.0 or later)
- **Docker and Docker Compose** (recommended)
- **Visual Studio 2022** or **VS Code**
- **SQL Server** (if using database monitoring)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/PranavBhimani25/DotWatch.git
cd DotWatch
```

### 2. Run with Docker Compose

```bash
# Start the entire monitoring stack
docker-compose up -d
```

### 3. Run ASP.NET Core Application

```bash
# Navigate to the web application directory
cd src/DotWatch.Web

# Restore packages
dotnet restore

# Run the application
dotnet run
```

### 4. Access Services

- **ASP.NET Core App**: http://localhost:5000
- **Grafana Dashboard**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **App Metrics**: http://localhost:5000/metrics

## ⚙️ ASP.NET Core Configuration

### 1. Package Installation

Add the following NuGet packages to your ASP.NET Core project:

```xml
<PackageReference Include="prometheus-net" Version="8.0.1" />
<PackageReference Include="prometheus-net.AspNetCore" Version="8.0.1" />
<PackageReference Include="prometheus-net.AspNetCore.HealthChecks" Version="8.0.1" />
```

### 2. Program.cs Configuration

```csharp
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContext<ApplicationDbContext>() // If using EF Core
    .AddSqlServer(connectionString); // If using SQL Server

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Prometheus metrics middleware
app.UseHttpMetrics(); // Collect HTTP metrics
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Expose metrics endpoint
app.MapMetrics(); // Exposes /metrics endpoint

// Health checks endpoint
app.MapHealthChecks("/health");

app.Run();
```

### 3. Custom Metrics in Controllers

```csharp
using Prometheus;

public class HomeController : Controller
{
    private static readonly Counter RequestsTotal = Metrics
        .CreateCounter("dotwatch_requests_total", "Total requests", "controller", "action");
    
    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("dotwatch_request_duration_seconds", "Request duration", "controller", "action");

    public IActionResult Index()
    {
        using (RequestDuration.WithLabels("Home", "Index").NewTimer())
        {
            RequestsTotal.WithLabels("Home", "Index").Inc();
            
            // Your controller logic here
            return View();
        }
    }
}
```

### 4. Database Monitoring (Entity Framework)

```csharp
// In your DbContext
public class ApplicationDbContext : DbContext
{
    private static readonly Counter DatabaseQueriesTotal = Metrics
        .CreateCounter("dotwatch_database_queries_total", "Total database queries", "operation");
    
    private static readonly Histogram DatabaseQueryDuration = Metrics
        .CreateHistogram("dotwatch_database_query_duration_seconds", "Database query duration", "operation");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(query => 
        {
            DatabaseQueriesTotal.WithLabels("query").Inc();
        });
    }
}
```

## 🐳 Docker Configuration

### docker-compose.yml

```yaml
version: '3.8'

services:
  # ASP.NET Core Application
  dotwatch-web:
    build:
      context: .
      dockerfile: src/DotWatch.Web/Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - prometheus
    networks:
      - monitoring

  # Prometheus
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
      - ./prometheus/alert-rules.yml:/etc/prometheus/alert-rules.yml
    networks:
      - monitoring

  # Grafana
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - ./grafana/provisioning:/etc/grafana/provisioning
      - ./grafana/dashboards:/var/lib/grafana/dashboards
    networks:
      - monitoring

networks:
  monitoring:
    driver: bridge
```

### ASP.NET Core Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/DotWatch.Web/DotWatch.Web.csproj", "src/DotWatch.Web/"]
RUN dotnet restore "src/DotWatch.Web/DotWatch.Web.csproj"
COPY . .
WORKDIR "/src/src/DotWatch.Web"
RUN dotnet build "DotWatch.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotWatch.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotWatch.Web.dll"]
```

## 📊 Prometheus Configuration

### prometheus.yml

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "alert-rules.yml"

scrape_configs:
  # ASP.NET Core Application
  - job_name: 'dotwatch-app'
    static_configs:
      - targets: ['dotwatch-web:80']
    metrics_path: '/metrics'
    scrape_interval: 30s

  # Prometheus itself
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  # Node Exporter (if running)
  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']
```

### ASP.NET Core Specific Alert Rules

```yaml
groups:
  - name: aspnetcore-alerts
    rules:
      - alert: HighRequestLatency
        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High request latency detected"
          description: "95th percentile latency is above 1 second"

      - alert: HighRequestRate
        expr: rate(http_requests_total[5m]) > 100
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High request rate detected"

      - alert: ApplicationDown
        expr: up{job="dotwatch-app"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "ASP.NET Core application is down"
```

## 📈 Grafana Dashboards

### ASP.NET Core Dashboard Panels

1. **Request Rate**: `rate(http_requests_total[5m])`
2. **Response Time**: `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))`
3. **Error Rate**: `rate(http_requests_total{status=~"5.."}[5m])`
4. **GC Collections**: `rate(dotnet_collection_count_total[5m])`
5. **Memory Usage**: `dotnet_total_memory_bytes`
6. **Active Connections**: `dotnet_kestrel_current_connections`

### Dashboard JSON Import

Import the pre-configured dashboard from `grafana/dashboards/aspnetcore-dashboard.json`

## 🔧 Custom Metrics Examples

### Business Logic Metrics

```csharp
public class OrderController : Controller
{
    private static readonly Counter OrdersProcessed = Metrics
        .CreateCounter("orders_processed_total", "Total orders processed", "status");
    
    private static readonly Gauge ActiveOrders = Metrics
        .CreateGauge("orders_active", "Currently active orders");

    [HttpPost]
    public async Task<IActionResult> CreateOrder(OrderModel model)
    {
        try
        {
            ActiveOrders.Inc();
            
            // Process order logic
            await _orderService.CreateOrder(model);
            
            OrdersProcessed.WithLabels("success").Inc();
            return RedirectToAction("Success");
        }
        catch (Exception ex)
        {
            OrdersProcessed.WithLabels("error").Inc();
            throw;
        }
        finally
        {
            ActiveOrders.Dec();
        }
    }
}
```

### Database Connection Monitoring

```csharp
public class DatabaseMetrics
{
    private static readonly Gauge DatabaseConnections = Metrics
        .CreateGauge("database_connections_active", "Active database connections");
    
    private static readonly Counter DatabaseExceptions = Metrics
        .CreateCounter("database_exceptions_total", "Total database exceptions", "type");

    public void TrackConnection()
    {
        DatabaseConnections.Inc();
    }

    public void ReleaseConnection()
    {
        DatabaseConnections.Dec();
    }
}
```

## 📁 Project Structure

```
DotWatch/
├── src/
│   └── DotWatch.Web/                 # ASP.NET Core MVC Application
│       ├── Controllers/              # MVC Controllers with metrics
│       ├── Models/                   # View models
│       ├── Views/                    # MVC Views
│       ├── Services/                 # Business services
│       ├── Data/                     # Entity Framework context
│       ├── Program.cs                # Application entry point
│       └── Dockerfile                # Container configuration
├── prometheus/
│   ├── prometheus.yml                # Prometheus configuration
│   └── alert-rules.yml               # Alert rules for ASP.NET Core
├── grafana/
│   ├── provisioning/                 # Grafana provisioning
│   └── dashboards/                   # Custom dashboards
│       └── aspnetcore-dashboard.json # ASP.NET Core specific dashboard
├── docker-compose.yml                # Complete stack deployment
└── README.md                         # This file
```

## 🐛 Troubleshooting

### Common Issues

1. **Metrics endpoint not accessible**
   ```bash
   # Check if the endpoint is exposed
   curl http://localhost:5000/metrics
   ```

2. **No data in Grafana**
   - Verify Prometheus is scraping the ASP.NET Core app
   - Check Prometheus targets: http://localhost:9090/targets
   - Ensure the `/metrics` endpoint is accessible

3. **High memory usage**
   - Monitor GC metrics
   - Check for memory leaks in custom metrics
   - Review metric cardinality

### Debugging Metrics

```csharp
// Add logging to see metrics collection
public void ConfigureServices(IServiceCollection services)
{
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.AddDebug();
    });
}
```

## 🚀 Performance Tips

### Metric Collection Best Practices

1. **Avoid high cardinality metrics**
2. **Use appropriate metric types** (Counter, Gauge, Histogram)
3. **Implement sampling for high-frequency events**
4. **Monitor metric collection overhead**

### ASP.NET Core Optimization

```csharp
// Configure Kestrel for better performance
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxRequestBodySize = 10 * 1024;
});
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Pranav Bhimani**
- GitHub: [@PranavBhimani25](https://github.com/PranavBhimani25)

## 🙏 Acknowledgments

- Microsoft ASP.NET Core team
- prometheus-net library contributors
- Prometheus and Grafana communities

---

## 📞 Support

For ASP.NET Core specific issues:
1. Check the [Microsoft Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
2. Review [prometheus-net documentation](https://github.com/prometheus-net/prometheus-net)
3. Create an issue with application logs and configuration details

---

**Happy Monitoring with ASP.NET Core! 🚀📊**
