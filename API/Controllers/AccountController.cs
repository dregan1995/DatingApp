using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController: BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();

            var appUser = new AppUser{
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            _context.AppUsers.Add(appUser);

            await _context.SaveChangesAsync();

            return new UserDto
            {
                Username = appUser.UserName,
                Token = _tokenService.CreateToken(appUser)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var appUser = await _context.AppUsers.SingleOrDefaultAsync(user => user.UserName == loginDto.Username);

            if (appUser == null) return Unauthorized("User not found");

            using var hmac = new HMACSHA512(appUser.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != appUser.PasswordHash[i]) return Unauthorized("Wrong password");
            }

            return new UserDto
            {
                Username = appUser.UserName,
                Token = _tokenService.CreateToken(appUser)
            };


        }

        private async Task<bool> UserExists(string username)
        {

            return await _context.AppUsers.AnyAsync(m => m.UserName == username);
        }
    }
}