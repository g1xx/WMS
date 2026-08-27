using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Warehouse.Application.DTOs;
using Warehouse.Domain;

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

        // Admin-only, exactly like RegisterBrigadier below. This was open self-registration
        // until now, which made locking down the read endpoints mostly theatre: anyone on
        // the internet could create a Worker account and then read everything those
        // controllers protect. On a publicly reachable deployment, creating an account is
        // an administrative act, not a self-service one. Neither frontend calls this.
        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            return await RegisterUserWithRole(registerDto, RoleNames.Worker);
        }

        // Admin-only: creates an account with the elevated Brigadier role. Without this
        // guard, anyone unauthenticated could self-register as a supervisor and unlock
        // every [Authorize(Roles = RoleNames.BrigadierOrAdmin)] endpoint in the app.
        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost("register-brigadier")]
        public async Task<IActionResult> RegisterBrigadier(RegisterDto registerDto)
        {
            return await RegisterUserWithRole(registerDto, RoleNames.Brigadier);
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

        // The one endpoint that can never require a token: it's where tokens come from.
        // Explicit under the fallback policy in Program.cs, which otherwise makes every
        // unattributed endpoint authenticated — including this one, locking everybody out.
        [AllowAnonymous]
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
        // Staff only. This was previously unattributed, i.e. anonymous: anyone who learned a
        // supervisor's badge id could mint an elevated Brigadier/Admin token without logging
        // in at all — and the demo help panel publishes that badge by design. Elevation now
        // requires an existing staff session to elevate FROM, which is how the terminal
        // already calls it (fetchSupervisorAuthHeader goes through axiosClient, so the
        // worker's own token is attached). Integration is excluded and must never elevate.
        [Authorize(Roles = RoleNames.AnyStaff)]
        [HttpPost("supervisor-override")]
        public async Task<ActionResult> SupervisorOverride(SupervisorOverrideDto dto)
        {
            if (!Guid.TryParse(dto.BadgeBarcode, out var badgeId))
                return Unauthorized("Invalid badge or missing permissions.");

            var user = await _userManager.FindByIdAsync(badgeId.ToString());
            if (user == null)
                return Unauthorized("Invalid badge or missing permissions.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(RoleNames.Brigadier) && !roles.Contains(RoleNames.Admin))
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