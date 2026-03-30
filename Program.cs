using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

// Servizi base
builder.Services.AddControllers();
//modificato affinchè funzioni sia in locale che su aws
//(appsettings.development.json è la stringa di connessione per il locale)

//se esiste una variabile d'ambiente (AWS), usa quella. Altrimenti usa il file locale (Development).
var connectionString =
  Environment.GetEnvironmentVariable("DB_CONNECTION")
  ?? builder.Configuration.GetConnectionString("DefaultConnection");

//diciamo al backend di usare SQL Server con la stringa trovata sopra
builder.Services.AddDbContext<ToDoContext>(options =>
  options.UseSqlServer(connectionString)
);

//legge le opzioni AWS dal file di configurazione (regione, chiavi, ecc.)
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
//registra il servizio S3 così possiamo "iniettarlo" nel costruttore del Controller
builder.Services.AddAWSService<IAmazonS3>();


//CONFIGURAZIONE CORS: Permette al Frontend (CloudFront) di bussare al Backend (ECS)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.WithOrigins("https://d17vbcq73r96m2.cloudfront.net") //solo il sito cloudfront può chiamare l'API
           .AllowAnyMethod() //permette GET, POST, PUT, DELETE
           .AllowAnyHeader()//permette l'invio di intestazioni (es. Content-Type)
           .AllowCredentials(); // FONDAMENTALE per i cookie
    });
});




// Servizi per sessione -> **PRIMA di Build**

//Crea una memoria temporanea nel server per salvare chi è loggato
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); //la sessione scade dopo 30 min di inattività
    options.Cookie.HttpOnly = true; //il cookie non è leggibile da script malevoli (sicurezza)
    options.Cookie.IsEssential = true;  //necessario per il funzionamento del sito

    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None; //permette il passaggio del cookie tra domini diversi (CloudFront -> API Gateway)
});


//comando per registrare il filtro
builder.Services.AddScoped<AuthFilter>();


var app = builder.Build();

 
app.UseRouting(); //Capisce quale rotta è stata chiamata
app.UseCors("AllowAll"); //controlla se il chiamante è autorizzato (CORS)
app.UseSession();   //attiva la gestione delle sessioni, prima di MapControllers PER FORZA

//  Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//disattivo perchè asp.net reindirizza ad https
//app.UseHttpsRedirection();


//collega i file dei Controller alle rotte API (es. api/todos)
app.MapControllers();

app.MapGet("/health", () => Results.Ok("Vivo e vegeto!"));

//per container
app.Run("http://0.0.0.0:8080");