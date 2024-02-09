using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ItemService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors("AllowAnyOrigin");

app.MapGet("/", async (CancellationToken ct, ItemService service, HttpContext ctx) =>
{
	var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

    //timeOut
	cts.CancelAfter(TimeSpan.FromSeconds(10));
	
    ctx.Response.Headers.Add("Content-Type", "text/event-stream");
    
    while (!cts.IsCancellationRequested)
    {
        var item = await service.WaitForNewItem();
        
        await ctx.Response.WriteAsync($"data: ");
        await JsonSerializer.SerializeAsync(ctx.Response.Body, item);
        await ctx.Response.WriteAsync($"\n\n");
        await ctx.Response.Body.FlushAsync();
            
        service.Reset();

        //Cancelar 
		//cts.Cancel();

	}
});

app.Run();