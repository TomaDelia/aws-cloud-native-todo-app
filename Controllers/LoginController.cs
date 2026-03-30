using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Dtos;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/login")]

    public class LoginController : ControllerBase
    {
        private readonly ToDoContext _context;

        public LoginController(ToDoContext context)
        {
            _context = context;
        }



        //post per il login
        [HttpPost]
        public IActionResult Login([FromBody] LoginDto dto)
        {

            // Cerco l'utente nel DB
            var user = _context.Users.FirstOrDefault(u => u.Username == dto.Username);
            if (user == null)
                return Unauthorized();
            if (!VerifyPassword(dto.Password, user.PasswordHash))
                return Unauthorized();

            //stringa per generare un token unico
            //GUID ha 128 bit di entropia
            user.Token = Guid.NewGuid().ToString();

            //salvo il token appena generato nel db
            _context.SaveChanges();

            //imposto il token nei cookie
            Response.Cookies.Append("token", user.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true,
                //tempo di scadenza del token (2h)
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            //Salvo info in sessione
            //HttpContext.Session.SetInt32("UserId", user.Id);
            //HttpContext.Session.SetInt32("IsAdmin", user.IsAdmin ? 1 : 0);

            // Risposta
            return Ok(new { message = "Login effettuato", token = user.Token});
        }

        // Metodo privato della classe per confronto password
        private bool VerifyPassword(string passwordInput, string storedHash)
        {
            // Per lo stage, confronto semplice
            return passwordInput == storedHash;
        }


        //metodo Logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            //richiedi il token dai cookie e salvalo in "token"
            var token = Request.Cookies["token"];
            //se la stringa è vuoto non autorizzato
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            var user = _context.Users.FirstOrDefault(u => u.Token == token);
            if (user == null) 
                return Unauthorized();

            user.Token = "";
            _context.SaveChanges();

            Response.Cookies.Delete("token");

            return Ok(new { message = "Logout effettuato" });
        }

        //metodo POST per registrazione
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            // controllo  se ci sono campi vuoti
            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                return BadRequest("Campi non validi");

            // controllo password uguali
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Le password non coincidono");

            // controllo username già esistente
            var existingUser = _context.Users
                .FirstOrDefault(u => u.Username == dto.Username);

            if (existingUser != null)
                return BadRequest("Username già esistente");

            // creazione nuovo utente
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = dto.Password,
                IsAdmin = false,
                Token = ""
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "Registrazione completata" });
        }


    }
}