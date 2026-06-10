using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SAP_API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("2.0")]
    public class AuthController : Controller
    {
        private readonly IConfiguration _config;
        public AuthController(IConfiguration config) => _config = config;

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            if (model.Username == "admin" && model.Password == "123456")
            {
                var claims = new[]
        {
            new Claim(ClaimTypes.Name, model.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                // ⚠️ Key phải >= 32 ký tự (256-bit) để dùng HS256
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsMyUltraSecretKeyForJWT256Bit!!"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // ⏰ Thời gian hết hạn token (2h)
                var expires = DateTime.Now.AddHours(2);

                var token = new JwtSecurityToken(
                    issuer: "yourdomain.com",
                    audience: "yourdomain.com",
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // ✅ Trả thêm thời gian hết hạn
                return Ok(new
                {
                    token = tokenString,
                    expiration = expires,
                    expires_in = (int)(expires - DateTime.Now).TotalSeconds
                });
            }

            return Unauthorized("Sai tài khoản hoặc mật khẩu");
        }
    }
    public class LoginModel
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
