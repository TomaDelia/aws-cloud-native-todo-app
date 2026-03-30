using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Backend.Models;

public class AuthFilter : IAsyncActionFilter
{
    private readonly ToDoContext _context;

    //Il filtro ha bisogno del DB -> Dependency Injection
    public AuthFilter(ToDoContext context)
    {
        _context = context;
    }

    //Questo metodo viene eseguito PRIMA del controller
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method.ToUpper() == "OPTIONS")
        {
            await next();
            return;
        }
        //Leggo il token dai cookie
        var token = context.HttpContext.Request.Cookies["token"];

        if (string.IsNullOrEmpty(token))
        {
            //Nessun token -> non autenticato
            context.Result = new UnauthorizedResult();
            return;
        }

        //Cerco l'utente con quel token
        var user = _context.Users.FirstOrDefault(u => u.Token == token);

        if (user == null)
        {
            //Token non valido
            context.Result = new UnauthorizedResult();
            return;
        }

        //Salvo l'utente nel contesto della request
        context.HttpContext.Items["User"] = user;

        //Tutto ok → continua verso il controller
        await next();
    }
}
