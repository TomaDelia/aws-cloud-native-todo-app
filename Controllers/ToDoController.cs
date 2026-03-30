using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Dtos;
using Amazon.S3;
using Amazon.S3.Model;

//prima di eseguire qualsiasi cosa del controller passa prima da
//AuthFilter
[ServiceFilter(typeof(AuthFilter))]
[ApiController]
[Route("api/todos")]
public class ToDoController : ControllerBase
{
    private readonly ToDoContext _context; // Il tuo database RDS
    private readonly IAmazonS3 _s3Client;   // Il client per S3

    // 2. COSTRUTTORE: Qui AWS "inietta" i servizi pronti all'uso
    // Ho unito i due costruttori in uno solo, altrimenti C# si arrabbia!
    public ToDoController(ToDoContext context, IAmazonS3 s3Client)
    {
        _context = context;
        _s3Client = s3Client; // Ora _s3Client è pronto per essere usato ovunque nel file
    }

    //GET
    [HttpGet]
    public IActionResult GetAll()
    {
        //controllo sessione di user -> con AuthFilter
        //var userId = HttpContext.Session.GetInt32("UserId");
        //var isAdmin = HttpContext.Session.GetInt32("IsAdmin") == 1;

        //if (userId == null)
        //    return Unauthorized();

        //prova del deploy automatico
        return Ok("Backend ToDoList v2.0 - Deploy Automatico Funzionante!");

        var user = (User)HttpContext.Items["User"];

        if (user.IsAdmin == true)
            return Ok(_context.Items.ToList());

        return Ok(
            _context.Items
                .Where(i => i.UserId == user.Id)
                .ToList()
            );
    }

    //POST
    [HttpPost]
    public IActionResult Create([FromBody] ToDoItem newItem)
    {
        //controllo user con la sessione
        //var userId = HttpContext.Session.GetInt32("UserId");

        //if (userId == null)
        //    return Unauthorized();

        var user = (User)HttpContext.Items["User"];

        //controlla che l'utente non abbia lasciato la task vuota
        if (string.IsNullOrWhiteSpace(newItem.Task))
            return BadRequest("Scemo, scrivi qualcosa prima");

        //imposta la (futura) checkbox a non checkata
        newItem.isCompleted = false;
        //imposta la data di creazione
        newItem.CreatedAt = DateTime.Now;

        //associo la nuova task all'utente
        newItem.UserId = user.Id;

        //fatto questo vai avanti e aggiungi la task
        _context.Items.Add(newItem);
        _context.SaveChanges();

        return Ok(newItem);
    }

    //aggiornamento checkbox se checked o meno
    [HttpPut("{id}")]
    public IActionResult ToggleCompleted(int id, [FromBody] UpdateToDoDto dto)
    {
        //controllo user con la sessione
        //var userId = HttpContext.Session.GetInt32("UserId");
        //var isAdmin = HttpContext.Session.GetInt32("IsAdmin") == 1;

        //if (userId == null)
        //    return Unauthorized();

        var user = (User)HttpContext.Items["User"];

        //recupero la task dal db
        var item = _context.Items.FirstOrDefault(i => i.Id == id);

        //controllo se la task esiste
        if (item == null)
            return NotFound("Task non trovata");

        //controllo i permessi
        if (!user.IsAdmin && item.UserId != user.Id)
            return Forbid();

        item.isCompleted = dto.IsCompleted;

        //salvo
        _context.SaveChanges();

        return Ok(item);
    }

    //DELETE
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        //controllo user con la sessione
        //var userId = HttpContext.Session.GetInt32("UserId");
        //var isAdmin = HttpContext.Session.GetInt32("IsAdmin") == 1;

        //if (userId == null)
        //    return Unauthorized();

        var user = (User)HttpContext.Items["User"];

        //cerco la task nel db
        var item = _context.Items.FirstOrDefault(i => i.Id == id);

        //se non esiste do errore
        if (item == null)
            return NotFound("Task non trovata");

        //controllo permessi
        if (!user.IsAdmin && item.UserId != user.Id)
            return Forbid();

        //rimuovo la task
        _context.Items.Remove(item);

        //salvo il cambiamento sul db
        _context.SaveChanges();

        //anche se non c'è nulla
        //da restituire nel delete
        //l'endpoint deve SEMPRE restituire
        //una risposta HTTP
        return NoContent();
    }

    //GET per leggere se l'utente è admin o meno
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var user = (User)HttpContext.Items["User"];
        if (user == null || user.IsAdmin != true) return Forbid();

        try
        {
            // 1. Definiamo la richiesta per S3 (niente URL, solo nomi)
            var request = new GetObjectRequest
            {
                BucketName = "todo-filefrontend",
                Key = "admin/stats.json"
            };

            // 2. Usiamo il client ufficiale di AWS (che usa i permessi IAM che abbiamo messo prima)
            using var response = await _s3Client.GetObjectAsync(request);
            using var reader = new StreamReader(response.ResponseStream);

            var json = await reader.ReadToEndAsync();

            return Content(json, "application/json");
        }
        catch (AmazonS3Exception ex)
        {
            // Se il file non esiste o i permessi falliscono, lo scriviamo nei log
            return StatusCode(500, $"Errore S3: {ex.Message}");
        }
    }
}