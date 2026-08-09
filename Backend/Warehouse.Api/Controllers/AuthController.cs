using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser<Guid>> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager; 
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<IdentityUser<Guid>> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            return await RegisterUserWithRole(registerDto, "Worker");
        }

        [HttpPost("register-brigadier")]
        public async Task<IActionResult> RegisterBrigadier(RegisterDto registerDto)
        {
            return await RegisterUserWithRole(registerDto, "Brigadier");
        }

        private async Task<IActionResult> RegisterUserWithRole(RegisterDto dto, string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }

            var user = new IdentityUser<Guid>
            {
                UserName = dto.Username,
                Email = dto.Email,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, roleName);

                return Ok(new
                {
                    Message = $"{roleName} successfully created",
                    UserBarcode = user.Id 
                });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null) return Unauthorized("Invalid username or password.");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordValid) return Unauthorized("Invalid username or password.");

            var token = await GenerateJwtToken(user, TimeSpan.FromHours(8));

            return Ok(new { Token = token });
        }

        // Lets a worker's terminal obtain a short-lived, elevated token for a single
        // Brigadier/Admin-gated action (report-defect, report-missing) by having the
        // supervisor scan their own badge on the device, without logging the worker out
        // of their own session. The badge barcode is the supervisor's IdentityUser Id.
        [HttpPost("supervisor-override")]
        public async Task<ActionResult> SupervisorOverride(SupervisorOverrideDto dto)
        {
            if (!Guid.TryParse(dto.BadgeBarcode, out var badgeId))
                return Unauthorized("Invalid badge or missing permissions.");

            var user = await _userManager.FindByIdAsync(badgeId.ToString());
            if (user == null)
                return Unauthorized("Invalid badge or missing permissions.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Brigadier") && !roles.Contains("Admin"))
                return StatusCode(403, "Invalid badge or missing permissions.");

            // Deliberately short-lived: this token exists only to authorize the one
            // elevated call the caller is about to make.
            var token = await GenerateJwtToken(user, TimeSpan.FromMinutes(2));

            return Ok(new { Token = token });
        }

        private async Task<string> GenerateJwtToken(IdentityUser<Guid> user, TimeSpan lifetime)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!)
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(secretKey);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.Add(lifetime),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}