using DatingApp.API.Data;
using DatingApp.API.DTOs;
using DatingApp.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace DatingApp.API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;

        public AccountController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AppUser>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        [HttpPost]
        public async Task<ActionResult<AppUser>> Login(LoginDto dto)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.UserName == dto.Username);
            if (user == null)
            {
                return Unauthorized("invalid username");
            }
            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                {
                    return Unauthorized("invalid password");
                }
            }
            return user;
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.AppUsers.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}